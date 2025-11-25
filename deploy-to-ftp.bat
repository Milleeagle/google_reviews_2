@echo off
echo ========================================
echo   Google Reviews - FTP Deployment
echo ========================================
echo.

REM Check if credentials.json exists
if not exist "credentials.json" (
    echo ERROR: credentials.json file not found!
    echo Please ensure you have the credentials file in this directory.
    pause
    exit /b 1
)

echo Step 1: Loading FTP credentials from credentials.json...
powershell -Command "try { $creds = Get-Content 'credentials.json' | ConvertFrom-Json; $ftpServer = $creds.FTP.Server; $ftpUsername = $creds.FTP.Username; $ftpPassword = $creds.FTP.Password; Write-Host 'FTP Server: ' -NoNewline; Write-Host $ftpServer -ForegroundColor Green; Write-Host 'Username: ' -NoNewline; Write-Host $ftpUsername -ForegroundColor Green; [Environment]::SetEnvironmentVariable('FTP_SERVER', $ftpServer, 'Process'); [Environment]::SetEnvironmentVariable('FTP_USERNAME', $ftpUsername, 'Process'); [Environment]::SetEnvironmentVariable('FTP_PASSWORD', $ftpPassword, 'Process'); } catch { Write-Host 'Error reading credentials.json: ' -ForegroundColor Red -NoNewline; Write-Host $_.Exception.Message -ForegroundColor Red; exit 1; }"

if %errorlevel% neq 0 (
    echo Failed to load FTP credentials.
    pause
    exit /b 1
)

echo.
echo Step 2: Building and publishing application...
echo Cleaning previous build and publish directories...

REM Remove all publish and deployment directories
if exist "publish" (
    rmdir /s /q "publish"
    echo Removed publish directory
)
if exist "deploy-temp" (
    rmdir /s /q "deploy-temp"
    echo Removed deploy-temp directory
)
if exist "bin" (
    rmdir /s /q "bin"
    echo Removed bin directory
)
if exist "obj" (
    rmdir /s /q "obj"
    echo Removed obj directory
)

dotnet clean --configuration Release

echo Restoring NuGet packages...
dotnet restore

if %errorlevel% neq 0 (
    echo Package restore failed!
    pause
    exit /b 1
)

echo Building application in Release mode...
dotnet build --configuration Release

if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Publishing application...
dotnet publish --configuration Release --output "./publish" --no-build

if %errorlevel% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Step 3: Creating deployment package...
if exist "deploy-temp" rmdir /s /q "deploy-temp"
mkdir "deploy-temp"

REM Copy published files
xcopy "publish\*" "deploy-temp\" /E /I /H /Y

REM Copy additional files that might be needed
if exist "appsettings.json" copy "appsettings.json" "deploy-temp\"
if exist "web.config" copy "web.config" "deploy-temp\"

echo.
echo Step 4: Uploading to FTP server...
powershell -Command "& {
    try {
        $ftpServer = [Environment]::GetEnvironmentVariable('FTP_SERVER');
        $ftpUsername = [Environment]::GetEnvironmentVariable('FTP_USERNAME');
        $ftpPassword = [Environment]::GetEnvironmentVariable('FTP_PASSWORD');
        
        Write-Host 'Connecting to FTP server: ' -NoNewline; Write-Host $ftpServer -ForegroundColor Yellow;
        
        # Create FTP client
        $ftp = [System.Net.FtpWebRequest]::Create(\"ftp://$ftpServer/\");
        $ftp.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword);
        $ftp.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory;
        
        # Test connection
        try {
            $response = $ftp.GetResponse();
            Write-Host '✅ FTP connection successful!' -ForegroundColor Green;
            $response.Close();
        }
        catch {
            Write-Host '❌ FTP connection failed: ' -ForegroundColor Red -NoNewline;
            Write-Host $_.Exception.Message -ForegroundColor Red;
            exit 1;
        }
        
        # Function to upload file
        function Upload-File {
            param($localPath, $remotePath)
            
            try {
                $ftpRequest = [System.Net.FtpWebRequest]::Create(\"ftp://$ftpServer/$remotePath\");
                $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword);
                $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile;
                $ftpRequest.UseBinary = \$true;
                
                $fileContent = [System.IO.File]::ReadAllBytes($localPath);
                $ftpRequest.ContentLength = $fileContent.Length;
                
                $requestStream = $ftpRequest.GetRequestStream();
                $requestStream.Write($fileContent, 0, $fileContent.Length);
                $requestStream.Close();
                
                $response = $ftpRequest.GetResponse();
                Write-Host \"✅ Uploaded: $remotePath\" -ForegroundColor Green;
                $response.Close();
                return \$true;
            }
            catch {
                Write-Host \"❌ Failed to upload $remotePath : \" -ForegroundColor Red -NoNewline;
                Write-Host $_.Exception.Message -ForegroundColor Red;
                return \$false;
            }
        }
        
        # Function to create directory
        function Create-Directory {
            param($remotePath)
            
            try {
                $ftpRequest = [System.Net.FtpWebRequest]::Create(\"ftp://$ftpServer/$remotePath\");
                $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword);
                $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory;
                
                $response = $ftpRequest.GetResponse();
                $response.Close();
                return \$true;
            }
            catch {
                # Directory might already exist, which is fine
                return \$true;
            }
        }
        
        Write-Host 'Starting file upload...' -ForegroundColor Yellow;
        
        # Get all files to upload
        $files = Get-ChildItem -Path 'deploy-temp' -Recurse -File;
        $totalFiles = $files.Count;
        $uploadedFiles = 0;
        $failedFiles = 0;
        
        foreach ($file in $files) {
            $relativePath = $file.FullName.Substring((Resolve-Path 'deploy-temp').Path.Length + 1);
            $remotePath = $relativePath.Replace('\\', '/');
            
            # Create directory if needed
            $remoteDir = [System.IO.Path]::GetDirectoryName($remotePath).Replace('\\', '/');
            if ($remoteDir -and $remoteDir -ne '.') {
                Create-Directory $remoteDir;
            }
            
            # Upload file
            if (Upload-File $file.FullName $remotePath) {
                $uploadedFiles++;
            } else {
                $failedFiles++;
            }
            
            # Show progress
            $progress = [math]::Round(($uploadedFiles + $failedFiles) / $totalFiles * 100, 1);
            Write-Host \"Progress: $progress% ($($uploadedFiles + $failedFiles)/$totalFiles)\" -ForegroundColor Cyan;
        }
        
        Write-Host '';
        Write-Host '========================================' -ForegroundColor Yellow;
        Write-Host 'Deployment Summary:' -ForegroundColor Yellow;
        Write-Host \"Total files: $totalFiles\" -ForegroundColor White;
        Write-Host \"Successfully uploaded: $uploadedFiles\" -ForegroundColor Green;
        Write-Host \"Failed uploads: $failedFiles\" -ForegroundColor Red;
        Write-Host '========================================' -ForegroundColor Yellow;
        
        if ($failedFiles -eq 0) {
            Write-Host '🎉 Deployment completed successfully!' -ForegroundColor Green;
            Write-Host \"Your application should now be available at: http://$ftpServer\" -ForegroundColor Cyan;
        } else {
            Write-Host '⚠️  Deployment completed with some errors.' -ForegroundColor Yellow;
        }
    }
    catch {
        Write-Host 'Deployment failed: ' -ForegroundColor Red -NoNewline;
        Write-Host $_.Exception.Message -ForegroundColor Red;
        exit 1;
    }
}"

echo.
echo Step 5: Cleaning up temporary files...
if exist "deploy-temp" rmdir /s /q "deploy-temp"

echo.
echo Deployment process completed!
echo.
pause