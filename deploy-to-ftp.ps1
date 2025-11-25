# Google Reviews - FTP Deployment Script
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Google Reviews - FTP Deployment" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

# Check if credentials.json exists
if (!(Test-Path "credentials.json")) {
    Write-Host "ERROR: credentials.json file not found!" -ForegroundColor Red
    Write-Host "Please ensure you have the credentials file in this directory." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

try {
    Write-Host "Step 1: Loading FTP credentials..." -ForegroundColor Cyan
    $creds = Get-Content 'credentials.json' | ConvertFrom-Json
    $ftpServer = $creds.FTP.Server
    $ftpUsername = $creds.FTP.Username
    $ftpPassword = $creds.FTP.Password
    
    Write-Host "FTP Server: " -NoNewline
    Write-Host $ftpServer -ForegroundColor Green
    Write-Host "Username: " -NoNewline
    Write-Host $ftpUsername -ForegroundColor Green
    Write-Host ""

    Write-Host "Step 2: Building and publishing application..." -ForegroundColor Cyan
    Write-Host "Cleaning previous build and publish directories..."
    
    # Remove all publish and deployment directories
    if (Test-Path "publish") {
        Remove-Item "publish" -Recurse -Force
        Write-Host "Removed publish directory"
    }
    if (Test-Path "deploy-temp") {
        Remove-Item "deploy-temp" -Recurse -Force
        Write-Host "Removed deploy-temp directory"
    }
    if (Test-Path "bin") {
        Remove-Item "bin" -Recurse -Force
        Write-Host "Removed bin directory"
    }
    if (Test-Path "obj") {
        Remove-Item "obj" -Recurse -Force
        Write-Host "Removed obj directory"
    }
    
    & dotnet clean --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        throw "Clean failed"
    }

    Write-Host "Restoring NuGet packages..."
    & dotnet restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed"
    }

    Write-Host "Building application in Release mode..."
    & dotnet build --configuration Release
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }

    Write-Host "Publishing application..."
    & dotnet publish --configuration Release --output "./publish" --no-build
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed"
    }

    Write-Host ""
    Write-Host "Step 3: Creating deployment package..." -ForegroundColor Cyan
    if (Test-Path "deploy-temp") {
        Remove-Item "deploy-temp" -Recurse -Force
    }
    New-Item -ItemType Directory -Name "deploy-temp" | Out-Null

    # Copy published files
    Copy-Item "publish\*" "deploy-temp\" -Recurse
    
    # Copy additional files
    if (Test-Path "appsettings.json") { Copy-Item "appsettings.json" "deploy-temp\" }
    if (Test-Path "web.config") { Copy-Item "web.config" "deploy-temp\" }

    Write-Host ""
    Write-Host "Step 4: Uploading to FTP server..." -ForegroundColor Cyan
    Write-Host "Connecting to FTP server: $ftpServer" -ForegroundColor Yellow

    # Function to upload file
    function Upload-File {
        param($localPath, $remotePath, $ftpServer, $username, $password)
        
        try {
            $ftpRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$remotePath")
            $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($username, $password)
            $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $ftpRequest.UseBinary = $true
            
            $fileContent = [System.IO.File]::ReadAllBytes($localPath)
            $ftpRequest.ContentLength = $fileContent.Length
            
            $requestStream = $ftpRequest.GetRequestStream()
            $requestStream.Write($fileContent, 0, $fileContent.Length)
            $requestStream.Close()
            
            $response = $ftpRequest.GetResponse()
            Write-Host "✅ Uploaded: $remotePath" -ForegroundColor Green
            $response.Close()
            return $true
        }
        catch {
            Write-Host "❌ Failed to upload $remotePath : $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }

    # Test FTP connection first
    try {
        $testRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/")
        $testRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $testRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        
        $response = $testRequest.GetResponse()
        Write-Host "✅ FTP connection successful!" -ForegroundColor Green
        $response.Close()
    }
    catch {
        Write-Host "❌ FTP connection failed: $($_.Exception.Message)" -ForegroundColor Red
        throw "FTP connection failed"
    }

    # Get all files to upload
    $files = Get-ChildItem -Path "deploy-temp" -Recurse -File
    $totalFiles = $files.Count
    $uploadedFiles = 0
    $failedFiles = 0

    Write-Host "Starting file upload ($totalFiles files)..." -ForegroundColor Yellow

    foreach ($file in $files) {
        $relativePath = $file.FullName.Substring((Resolve-Path "deploy-temp").Path.Length + 1)
        $remotePath = $relativePath.Replace('\', '/')
        
        if (Upload-File $file.FullName $remotePath $ftpServer $ftpUsername $ftpPassword) {
            $uploadedFiles++
        } else {
            $failedFiles++
        }
        
        # Show progress
        $progress = [math]::Round(($uploadedFiles + $failedFiles) / $totalFiles * 100, 1)
        Write-Host "Progress: $progress% ($($uploadedFiles + $failedFiles)/$totalFiles)" -ForegroundColor Cyan
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Deployment Summary:" -ForegroundColor Yellow
    Write-Host "Total files: $totalFiles" -ForegroundColor White
    Write-Host "Successfully uploaded: $uploadedFiles" -ForegroundColor Green
    Write-Host "Failed uploads: $failedFiles" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Yellow

    if ($failedFiles -eq 0) {
        Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
        Write-Host "Your application should now be available at: http://$ftpServer" -ForegroundColor Cyan
    } else {
        Write-Host "⚠️  Deployment completed with some errors." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Step 5: Cleaning up..." -ForegroundColor Cyan
    if (Test-Path "deploy-temp") {
        Remove-Item "deploy-temp" -Recurse -Force
    }

    Write-Host ""
    Write-Host "Deployment process completed!" -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Cleanup on error
    if (Test-Path "deploy-temp") {
        Remove-Item "deploy-temp" -Recurse -Force
    }
}

Write-Host ""
Read-Host "Press Enter to exit"