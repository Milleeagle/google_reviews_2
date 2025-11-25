@echo off
echo Setting up Google Reviews project credentials...
echo.

if not exist "credentials.json" (
    echo ERROR: credentials.json file not found!
    echo Please ensure you have copied the credentials.json file to this directory.
    pause
    exit /b 1
)

echo Found credentials.json file.
echo Copying settings to appsettings.Development.json...

powershell -Command "& {
    $creds = Get-Content 'credentials.json' | ConvertFrom-Json;
    $settings = Get-Content 'appsettings.Development.json' | ConvertFrom-Json;
    
    $settings.AdminUser = $creds.AdminUser;
    $settings.GooglePlaces = $creds.GooglePlaces;
    $settings.GoogleDrive = $creds.GoogleDrive;
    $settings.Email = $creds.Email;
    
    $settings | ConvertTo-Json -Depth 10 | Set-Content 'appsettings.Development.json';
    Write-Host 'Credentials successfully applied to appsettings.Development.json';
}"

echo.
echo Setup complete! You can now run the application with:
echo   dotnet restore
echo   dotnet ef database update
echo   dotnet run
echo.
pause