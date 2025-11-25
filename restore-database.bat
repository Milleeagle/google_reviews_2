@echo off
echo Restoring database from backup file...
echo.

if not exist "database-backup.bak" (
    echo ERROR: database-backup.bak file not found!
    echo Please ensure you have copied the backup file to this directory.
    pause
    exit /b 1
)

echo Found database-backup.bak file.
echo Restoring database...

REM First create the database if it doesn't exist
sqlcmd -S "(localdb)\mssqllocaldb" -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'aspnet-google_reviews') CREATE DATABASE [aspnet-google_reviews]" -o restore-log.txt

REM Restore the database from backup
sqlcmd -S "(localdb)\mssqllocaldb" -Q "RESTORE DATABASE [aspnet-google_reviews] FROM DISK = '%~dp0database-backup.bak' WITH REPLACE" -o restore-log.txt

if %errorlevel% equ 0 (
    echo.
    echo ✅ Database successfully restored!
    echo.
    echo You can now run the application with:
    echo   dotnet restore
    echo   dotnet run
    echo.
) else (
    echo.
    echo ❌ Restore failed. Check restore-log.txt for details.
    echo.
    echo Alternative: Try running 'dotnet ef database update' instead
    echo.
)

pause