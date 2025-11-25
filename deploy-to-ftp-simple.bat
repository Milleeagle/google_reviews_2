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

echo Step 1: Loading FTP credentials...
powershell -Command "$creds = Get-Content 'credentials.json' | ConvertFrom-Json; Write-Host 'FTP Server:' $creds.FTP.Server -ForegroundColor Green; Write-Host 'Username:' $creds.FTP.Username -ForegroundColor Green; $env:FTP_SERVER = $creds.FTP.Server; $env:FTP_USERNAME = $creds.FTP.Username; $env:FTP_PASSWORD = $creds.FTP.Password"

echo.
echo Step 2: Building and publishing application...
echo Cleaning previous build...
dotnet clean --configuration Release

echo Building application in Release mode...
dotnet build --configuration Release --no-restore

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
echo Step 4: Creating FTP script...
echo open %FTP_SERVER% > ftp_commands.txt
echo %FTP_USERNAME% >> ftp_commands.txt
echo %FTP_PASSWORD% >> ftp_commands.txt
echo binary >> ftp_commands.txt
echo prompt >> ftp_commands.txt

REM Add upload commands for all files
echo lcd deploy-temp >> ftp_commands.txt
echo mput *.dll >> ftp_commands.txt
echo mput *.exe >> ftp_commands.txt
echo mput *.json >> ftp_commands.txt
echo mput *.config >> ftp_commands.txt
echo mput *.pdb >> ftp_commands.txt

echo quit >> ftp_commands.txt

echo Step 5: Uploading to FTP server...
echo Connecting to %FTP_SERVER%...
ftp -s:ftp_commands.txt

if %errorlevel% equ 0 (
    echo.
    echo ✅ Basic files uploaded successfully!
    echo.
    echo ℹ️  Note: This uploads core files only.
    echo    For complete deployment, use WinSCP or FileZilla
    echo    to upload the entire deploy-temp folder contents.
    echo.
    echo Your application should be available at: http://%FTP_SERVER%
) else (
    echo.
    echo ⚠️  FTP upload may have encountered issues.
    echo    Check the FTP server credentials and try again.
)

echo.
echo Step 6: Cleaning up...
if exist "deploy-temp" rmdir /s /q "deploy-temp"
if exist "ftp_commands.txt" del "ftp_commands.txt"

echo.
echo Deployment process completed!
pause