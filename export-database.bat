@echo off
echo Exporting database to SQL backup file...
echo.

REM Connect to LocalDB and export the database
sqlcmd -S "(localdb)\mssqllocaldb" -d "aspnet-google_reviews" -Q "BACKUP DATABASE [aspnet-google_reviews] TO DISK = '%~dp0database-backup.bak' WITH FORMAT, INIT" -o export-log.txt

if %errorlevel% equ 0 (
    echo.
    echo ✅ Database successfully backed up to: database-backup.bak
    echo.
    echo To restore on the other machine:
    echo 1. Copy database-backup.bak to the other machine
    echo 2. Run restore-database.bat
    echo.
) else (
    echo.
    echo ❌ Backup failed. Check export-log.txt for details.
    echo.
)

pause