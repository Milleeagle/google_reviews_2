@echo off
echo ========================================
echo   Create MySQL Migration
echo ========================================
echo.
echo This will create a new MySQL migration to replace the SQL Server one.
echo.
echo IMPORTANT: Make sure you have MySQL/MariaDB installed and running locally
echo before proceeding. Default connection expects MySQL on localhost:3306
echo with root user and no password.
echo.

set /p continue="Continue? (Y/N): "
if /i "%continue%" neq "Y" (
    echo Operation cancelled.
    pause
    exit /b
)

echo.
echo Step 1: Removing old SQL Server migration files...
if exist "Data\Migrations" (
    rmdir /s /q "Data\Migrations"
    echo Removed old migration files
)

echo.
echo Step 2: Restoring NuGet packages (including new MySQL package)...
dotnet restore

if %errorlevel% neq 0 (
    echo Failed to restore packages!
    pause
    exit /b 1
)

echo.
echo Step 3: Creating new MySQL migration...
dotnet ef migrations add InitialMySqlMigration

if %errorlevel% neq 0 (
    echo Migration creation failed!
    echo.
    echo This might be because:
    echo 1. MySQL server is not running
    echo 2. Connection string is incorrect
    echo 3. MySQL user doesn't have permissions
    echo.
    pause
    exit /b 1
)

echo.
echo Step 4: Updating database with new migration...
dotnet ef database update

if %errorlevel% neq 0 (
    echo Database update failed!
    echo.
    echo Please check:
    echo 1. MySQL server is running on localhost:3306
    echo 2. Root user exists and has no password (or update connection string)
    echo 3. MySQL user has permission to create databases
    echo.
    pause
    exit /b 1
)

echo.
echo ✅ MySQL migration completed successfully!
echo.
echo Next steps:
echo 1. Test the application locally with MySQL
echo 2. Use the export-mysql-database.bat script to export for hosting
echo 3. Deploy using the existing FTP deployment scripts
echo.
pause