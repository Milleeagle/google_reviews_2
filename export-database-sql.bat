@echo off
echo Exporting database to SQL script file...
echo.

REM Export schema and data using sqlcmd with bcp
sqlcmd -S "(localdb)\mssqllocaldb" -d "aspnet-google_reviews" -Q "EXEC sp_helpdb 'aspnet-google_reviews'" -o database-info.txt

echo Generating SQL script with schema and data...
echo This may take a moment for large databases...

REM Note: For a complete SQL script export, you would typically use SQL Server Management Studio
REM or the SqlPackage utility. This is a simpler alternative.

sqlcmd -S "(localdb)\mssqllocaldb" -d "aspnet-google_reviews" -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'" -h -1 -o table-list.txt

echo.
echo ℹ️  For complete SQL script export, use SQL Server Management Studio:
echo    1. Right-click database → Tasks → Generate Scripts
echo    2. Choose "Script entire database and all database objects"
echo    3. Advanced Options → Types of data to script: "Data only" or "Schema and data"
echo.
echo Or use the backup method with export-database.bat (recommended)
echo.

pause