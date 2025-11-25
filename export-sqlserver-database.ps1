# Export SQL Server Database for Hosting
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Export SQL Server Database" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$DatabaseName = "aspnet-google_reviews"
$ServerName = "(localdb)\mssqllocaldb"
$OutputFile = "database-sqlserver-export.sql"
$BackupFile = "database-sqlserver-backup.bak"

Write-Host "This will export your SQL Server database for hosting upload." -ForegroundColor Cyan
Write-Host ""
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Server: $ServerName" -ForegroundColor White
Write-Host ""

try {
    # Check if sqlcmd is available
    Write-Host "Checking for SQL Server tools..." -ForegroundColor Yellow
    $sqlcmdTest = & sqlcmd -? 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ sqlcmd not found. Installing SQL Server Command Line Utilities..." -ForegroundColor Yellow
        Write-Host "Please download and install SQL Server Command Line Utilities from:" -ForegroundColor Red
        Write-Host "https://docs.microsoft.com/en-us/sql/tools/sqlcmd-utility" -ForegroundColor Cyan
        throw "sqlcmd is required for SQL Server export"
    }
    Write-Host "✅ sqlcmd found" -ForegroundColor Green

    Write-Host ""
    Write-Host "Export options:" -ForegroundColor Cyan
    Write-Host "1. Create .BAK backup file (recommended for MSSQL hosting)" -ForegroundColor White
    Write-Host "2. Generate .SQL script file (for manual import)" -ForegroundColor White
    Write-Host "3. Both backup and script files" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Choose export type (1, 2, or 3)"

    if ($choice -eq "1" -or $choice -eq "3") {
        Write-Host ""
        Write-Host "Creating SQL Server backup file..." -ForegroundColor Cyan
        
        $backupQuery = @"
BACKUP DATABASE [$DatabaseName] 
TO DISK = '$((Get-Location).Path)\$BackupFile'
WITH FORMAT, INIT, 
     NAME = '$DatabaseName Full Backup',
     COMPRESSION;
"@
        
        $result = sqlcmd -S $ServerName -Q $backupQuery -o "backup-log.txt"
        
        if ($LASTEXITCODE -eq 0 -and (Test-Path $BackupFile)) {
            Write-Host "✅ Backup file created successfully!" -ForegroundColor Green
            
            $backupSize = (Get-Item $BackupFile).Length
            $backupSizeMB = [math]::Round($backupSize / 1MB, 2)
            Write-Host "Backup file: $BackupFile" -ForegroundColor White
            Write-Host "Size: $backupSizeMB MB" -ForegroundColor White
        } else {
            Write-Host "❌ Backup creation failed. Check backup-log.txt for details." -ForegroundColor Red
        }
    }

    if ($choice -eq "2" -or $choice -eq "3") {
        Write-Host ""
        Write-Host "Generating SQL script (this may take time for large databases)..." -ForegroundColor Cyan
        
        # Create a comprehensive SQL export
        $scriptHeader = @"
-- SQL Server Database Export
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- Database: $DatabaseName
-- Source Server: $ServerName
--
-- Instructions:
-- 1. Create a new database on your hosting server
-- 2. Run this script against the new database
-- 3. Update your connection string
--

USE [$DatabaseName];
GO

-- Export all data from all tables
"@
        
        $scriptHeader | Out-File -FilePath $OutputFile -Encoding UTF8
        
        # Get all tables
        $tablesQuery = "SELECT TABLE_SCHEMA + '.' + TABLE_NAME as FullTableName FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME NOT LIKE '__EF%'"
        $tables = sqlcmd -S $ServerName -d $DatabaseName -Q $tablesQuery -h -1 -W
        
        $tableList = @()
        foreach ($table in $tables) {
            $table = $table.Trim()
            if ($table -and $table -ne "") {
                $tableList += $table
            }
        }
        
        Write-Host "Found $($tableList.Count) tables to export" -ForegroundColor White
        
        foreach ($table in $tableList) {
            Write-Host "Processing table: $table" -ForegroundColor Gray
            
            "-- Data for table: $table" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
            "" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
            
            # Export table data as INSERT statements
            try {
                $dataQuery = "SELECT * FROM $table"
                $tableData = sqlcmd -S $ServerName -d $DatabaseName -Q $dataQuery -s "," -W -h -1
                
                if ($tableData) {
                    "-- Raw data for $table (you may need to convert this to INSERT statements)" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
                    $tableData | Out-File -FilePath $OutputFile -Append -Encoding UTF8
                    "" | Out-File -FilePath $OutputFile -Append -Encoding UTF8
                }
            }
            catch {
                Write-Host "⚠️  Could not export data for table: $table" -ForegroundColor Yellow
            }
        }
        
        if (Test-Path $OutputFile) {
            Write-Host "✅ SQL script created!" -ForegroundColor Green
            
            $scriptSize = (Get-Item $OutputFile).Length
            $scriptSizeMB = [math]::Round($scriptSize / 1MB, 2)
            Write-Host "Script file: $OutputFile" -ForegroundColor White
            Write-Host "Size: $scriptSizeMB MB" -ForegroundColor White
        }
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Export Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    if (Test-Path $BackupFile) {
        Write-Host "📦 Backup File: $BackupFile" -ForegroundColor Cyan
        Write-Host "   Use this for MSSQL hosting providers that support .BAK restore" -ForegroundColor White
    }
    
    if (Test-Path $OutputFile) {
        Write-Host "📋 Script File: $OutputFile" -ForegroundColor Cyan
        Write-Host "   Use this for manual database import" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "Next Steps for MSSQL Hosting:" -ForegroundColor Yellow
    Write-Host "1. Check if your host supports .BAK file restore (preferred)" -ForegroundColor White
    Write-Host "2. If not, use SQL Server Management Studio to:" -ForegroundColor White
    Write-Host "   - Generate proper CREATE and INSERT scripts" -ForegroundColor White
    Write-Host "   - Export as .sql with schema and data" -ForegroundColor White
    Write-Host "3. Update connection strings to use MSSQL hosting details" -ForegroundColor White
    Write-Host ""
    Write-Host "For better SQL script generation, use SSMS:" -ForegroundColor Cyan
    Write-Host "Tasks → Generate Scripts → Schema and Data → Script to File" -ForegroundColor White

}
catch {
    Write-Host ""
    Write-Host "❌ Export failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Ensure LocalDB is running" -ForegroundColor White
    Write-Host "2. Verify database name: $DatabaseName" -ForegroundColor White
    Write-Host "3. Install SQL Server Command Line Utilities" -ForegroundColor White
    Write-Host "4. Consider using SQL Server Management Studio for complex exports" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to exit"