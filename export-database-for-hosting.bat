@echo off
echo ========================================
echo   Export Database for Web Hosting
echo ========================================
echo.
echo This will export your database to a .sql script file
echo that can be imported to your hosting provider.
echo.

set OUTPUT_FILE=database-export-for-hosting.sql
set DATABASE_NAME=aspnet-google_reviews
set SERVER_NAME=(localdb)\mssqllocaldb

echo Generating SQL script with schema and data...
echo Database: %DATABASE_NAME%
echo Output: %OUTPUT_FILE%
echo.

REM Use sqlcmd to generate a comprehensive SQL script
REM This exports both schema and data

echo -- Exported from %DATABASE_NAME% on %DATE% at %TIME% > "%OUTPUT_FILE%"
echo -- Generated for web hosting import >> "%OUTPUT_FILE%"
echo. >> "%OUTPUT_FILE%"

REM Export table creation scripts
echo Exporting table schemas...
for /f "tokens=*" %%i in ('sqlcmd -S "%SERVER_NAME%" -d "%DATABASE_NAME%" -h -1 -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT LIKE '__EF%%' ORDER BY TABLE_NAME"') do (
    if not "%%i"=="" (
        echo Processing table: %%i
        sqlcmd -S "%SERVER_NAME%" -d "%DATABASE_NAME%" -Q "SELECT 'CREATE TABLE [' + TABLE_NAME + '] (' FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '%%i'" -h -1 >> temp_schema.sql
    )
)

REM Note: This is a basic export. For a complete export with proper CREATE TABLE statements,
REM foreign keys, indexes, etc., we recommend using SQL Server Management Studio or sqlcmd with more advanced scripts

echo.
echo ⚠️  IMPORTANT: For best results, use SQL Server Management Studio:
echo.
echo 1. Open SQL Server Management Studio
echo 2. Connect to: %SERVER_NAME%
echo 3. Right-click database '%DATABASE_NAME%' → Tasks → Generate Scripts...
echo 4. Choose "Script entire database and all database objects"
echo 5. Advanced Options:
echo    - Types of data to script: "Schema and data"
echo    - Script for Server Version: Choose your hosting provider's version
echo 6. Save to file: %OUTPUT_FILE%
echo.
echo Alternatively, you can use the SqlPackage utility:
echo SqlPackage.exe /Action:Export /SourceConnectionString:"Server=%SERVER_NAME%;Database=%DATABASE_NAME%;Integrated Security=true;" /TargetFile:database-export.bacpac
echo.

REM Clean up temp files
if exist temp_schema.sql del temp_schema.sql
if exist table-list.txt del table-list.txt

echo ========================================
echo   Manual Export Instructions
echo ========================================
echo.
echo Since you need a proper .sql file for hosting, please:
echo.
echo 1. Install SQL Server Management Studio (SSMS) if not already installed
echo 2. Connect to: %SERVER_NAME%
echo 3. Generate Scripts wizard (as described above)
echo 4. The output file should be named: %OUTPUT_FILE%
echo.
echo Once you have the .sql file, you can:
echo - Upload it directly if it's under your host's size limit
echo - Compress it to .sql.gz using 7-Zip or WinRAR if it's too large
echo.
pause