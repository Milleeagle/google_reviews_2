@echo off
echo ========================================
echo   Export SQL Server Database
echo ========================================
echo.

set DATABASE_NAME=aspnet-google_reviews
set SERVER_NAME=(localdb)\mssqllocaldb
set BACKUP_FILE=database-sqlserver-backup.bak

echo This will export your SQL Server database for hosting.
echo.
echo Database: %DATABASE_NAME%
echo Server: %SERVER_NAME%
echo Output: %BACKUP_FILE%
echo.

REM Check if sqlcmd is available
sqlcmd -? >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ sqlcmd not found!
    echo.
    echo Please install SQL Server Command Line Utilities:
    echo https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility
    echo.
    pause
    exit /b 1
)

echo Creating SQL Server backup...
echo.

REM Create backup with compression
sqlcmd -S "%SERVER_NAME%" -Q "BACKUP DATABASE [%DATABASE_NAME%] TO DISK = '%~dp0%BACKUP_FILE%' WITH FORMAT, INIT, NAME = '%DATABASE_NAME% Full Backup', COMPRESSION;" -o backup-log.txt

if %errorlevel% equ 0 (
    echo.
    echo ✅ Backup created successfully!
    echo.
    echo File: %BACKUP_FILE%
    for %%A in (%BACKUP_FILE%) do set FILE_SIZE=%%~zA
    set /a FILE_SIZE_MB=%FILE_SIZE% / 1048576
    echo Size: %FILE_SIZE_MB% MB
    echo.
    echo ========================================
    echo   Upload Instructions
    echo ========================================
    echo.
    echo For MSSQL Hosting:
    echo 1. Check if your host supports .BAK file restore
    echo 2. Upload %BACKUP_FILE% via hosting control panel
    echo 3. Restore database from backup file
    echo 4. Update connection strings with hosting details
    echo.
    echo If .BAK restore not supported:
    echo 1. Use SQL Server Management Studio
    echo 2. Right-click database → Tasks → Generate Scripts
    echo 3. Include Schema and Data
    echo 4. Save as .sql file for manual import
    echo.
) else (
    echo.
    echo ❌ Backup failed!
    echo Check backup-log.txt for details
    echo.
    echo Possible issues:
    echo 1. LocalDB not running
    echo 2. Database name incorrect
    echo 3. Permission issues
    echo.
)

pause