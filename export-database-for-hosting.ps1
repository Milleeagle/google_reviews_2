# Export Database for Web Hosting
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Export Database for Web Hosting" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$OutputFile = "database-export-for-hosting.sql"
$DatabaseName = "aspnet-google_reviews"
$ServerName = "(localdb)\mssqllocaldb"

Write-Host "This will export your database to a .sql script file" -ForegroundColor Cyan
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Server: $ServerName" -ForegroundColor White
Write-Host "Output: $OutputFile" -ForegroundColor White
Write-Host ""

try {
    # Test connection first
    Write-Host "Testing database connection..." -ForegroundColor Yellow
    $testQuery = "SELECT DB_NAME() as CurrentDatabase"
    $result = sqlcmd -S $ServerName -d $DatabaseName -Q $testQuery -h -1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot connect to database"
    }
    
    Write-Host "✅ Database connection successful" -ForegroundColor Green
    Write-Host ""

    # Start building the SQL export file
    Write-Host "Generating SQL export file..." -ForegroundColor Cyan
    
    $header = @"
-- Database Export for Web Hosting
-- Generated on: $(Get-Date)
-- Source Database: $DatabaseName
-- Source Server: $ServerName
--
-- This file contains the complete database structure and data
-- for import to your web hosting provider.
--

-- Disable foreign key checks during import
-- (Comment out if your hosting provider doesn't support this)
-- SET FOREIGN_KEY_CHECKS = 0;

"@
    
    $header | Out-File -FilePath $OutputFile -Encoding UTF8
    
    # Get all tables (excluding EF migration history)
    Write-Host "Getting table list..." -ForegroundColor Yellow
    $tablesQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT LIKE '__EF%' ORDER BY TABLE_NAME"
    $tables = sqlcmd -S $ServerName -d $DatabaseName -Q $tablesQuery -h -1 -W
    
    $tableList = @()
    foreach ($table in $tables) {
        $table = $table.Trim()
        if ($table -and $table -ne "") {
            $tableList += $table
        }
    }
    
    Write-Host "Found $($tableList.Count) tables to export" -ForegroundColor White
    
    # For each table, export structure and data
    foreach ($table in $tableList) {
        Write-Host "Processing table: $table" -ForegroundColor Cyan
        
        # Add table comment
        "-- Table: $table" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        "" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
        
        # Get table data
        try {
            $dataQuery = "SELECT * FROM [$table]"
            $tableData = sqlcmd -S $ServerName -d $DatabaseName -Q $dataQuery -s "," -W
            
            if ($tableData) {
                "-- Data for table: $table" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
                $tableData | Out-File -FilePath $OutputFile -Append -Encoding UTF8
                "" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
            }
        }
        catch {
            Write-Host "⚠️  Could not export data for table: $table" -ForegroundColor Yellow
        }
    }
    
    # Add footer
    $footer = @"

-- Re-enable foreign key checks
-- SET FOREIGN_KEY_CHECKS = 1;

-- Export completed on: $(Get-Date)
"@
    
    $footer | Out-File -FilePath $OutputFile -Append -Encoding UTF8
    
    Write-Host ""
    Write-Host "✅ Basic export completed!" -ForegroundColor Green
    Write-Host "File created: $OutputFile" -ForegroundColor White
    
    # Check file size
    $fileSize = (Get-Item $OutputFile).Length
    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
    
    Write-Host "File size: $fileSizeMB MB" -ForegroundColor White
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "   IMPORTANT NOTES" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "⚠️  The generated file contains basic data export but may not include:" -ForegroundColor Yellow
    Write-Host "   - Complete table structure (CREATE TABLE statements)" -ForegroundColor White
    Write-Host "   - Indexes, foreign keys, constraints" -ForegroundColor White
    Write-Host "   - Stored procedures, functions, triggers" -ForegroundColor White
    Write-Host ""
    Write-Host "📋 For a complete SQL script, use SQL Server Management Studio:" -ForegroundColor Cyan
    Write-Host "   1. Open SSMS and connect to: $ServerName" -ForegroundColor White
    Write-Host "   2. Right-click '$DatabaseName' → Tasks → Generate Scripts..." -ForegroundColor White
    Write-Host "   3. Choose 'Script entire database and all database objects'" -ForegroundColor White
    Write-Host "   4. Advanced Options → Types of data to script: 'Schema and data'" -ForegroundColor White
    Write-Host "   5. Save as: $OutputFile" -ForegroundColor White
    Write-Host ""
    Write-Host "📦 To compress for upload (if file is large):" -ForegroundColor Cyan
    Write-Host "   Use 7-Zip or WinRAR to create: database-export-for-hosting.sql.gz" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ Export failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please ensure:" -ForegroundColor Yellow
    Write-Host "1. LocalDB is running" -ForegroundColor White
    Write-Host "2. Database '$DatabaseName' exists" -ForegroundColor White
    Write-Host "3. You have permissions to access the database" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to exit"