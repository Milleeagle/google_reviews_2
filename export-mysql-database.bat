@echo off
echo ========================================
echo   Export MySQL Database for Hosting
echo ========================================
echo.

set DATABASE_NAME=aspnet_google_reviews
set OUTPUT_FILE=database-export-for-hosting.sql
set COMPRESSED_FILE=database-export-for-hosting.sql.gz

echo This will export your MySQL database to a .sql file for hosting import.
echo.
echo Database: %DATABASE_NAME%
echo Output File: %OUTPUT_FILE%
echo.

REM Check if mysqldump is available
mysqldump --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ mysqldump not found!
    echo.
    echo Please ensure MySQL is installed and mysqldump is in your PATH.
    echo You can usually find it in: C:\Program Files\MySQL\MySQL Server X.X\bin\
    echo.
    pause
    exit /b 1
)

echo Exporting database with mysqldump...
echo.

REM Export the database structure and data
mysqldump -u root -p --single-transaction --routines --triggers %DATABASE_NAME% > %OUTPUT_FILE%

if %errorlevel% neq 0 (
    echo ❌ Export failed!
    echo.
    echo Please check:
    echo 1. MySQL server is running
    echo 2. Database '%DATABASE_NAME%' exists
    echo 3. MySQL root password is correct
    echo.
    pause
    exit /b 1
)

echo ✅ Database exported successfully!
echo.

REM Check file size
for %%A in (%OUTPUT_FILE%) do set FILE_SIZE=%%~zA
set /a FILE_SIZE_MB=%FILE_SIZE% / 1048576

echo File: %OUTPUT_FILE%
echo Size: %FILE_SIZE_MB% MB

if %FILE_SIZE_MB% GTR 50 (
    echo.
    echo ⚠️  File is large (%FILE_SIZE_MB% MB). Consider compressing it:
    echo.
    echo To compress to .gz format:
    echo 1. Install 7-Zip
    echo 2. Right-click %OUTPUT_FILE% ^> 7-Zip ^> Add to archive
    echo 3. Choose gzip format and rename to: %COMPRESSED_FILE%
    echo.
    echo Or use command line if you have gzip:
    echo gzip -c %OUTPUT_FILE% ^> %COMPRESSED_FILE%
    echo.
)

echo.
echo ========================================
echo   Upload Instructions
echo ========================================
echo.
echo 1. Login to your hosting control panel
echo 2. Go to MySQL/Database section
echo 3. Create a new database (note the name, user, password)
echo 4. Import %OUTPUT_FILE% (or %COMPRESSED_FILE% if compressed)
echo 5. Update your production appsettings.json with the hosting database details
echo.
echo Production connection string format:
echo "Server=YOUR_MYSQL_HOST;Database=YOUR_DATABASE_NAME;User=YOUR_USER;Password=YOUR_PASSWORD;Port=3306;"
echo.
pause