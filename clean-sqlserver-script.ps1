# Clean SQL Server Script for Web Hosting
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Clean SQL Server Script" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$InputFile = Read-Host "Enter the path to your SSMS-generated .sql file"
$OutputFile = "database-sqlserver-hosting.sql"

if (!(Test-Path $InputFile)) {
    Write-Host "❌ File not found: $InputFile" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

try {
    Write-Host "Reading and cleaning SQL script..." -ForegroundColor Cyan
    
    $content = Get-Content $InputFile -Raw
    
    Write-Host "Original file size: $([math]::Round((Get-Item $InputFile).Length / 1KB, 2)) KB" -ForegroundColor White
    
    # Clean the SQL script for web hosting
    Write-Host "Applying hosting-friendly modifications..." -ForegroundColor Yellow
    
    # Remove problematic statements
    $cleanedContent = $content
    
    # Remove database creation and USE statements
    $cleanedContent = $cleanedContent -replace "USE \[master\]", ""
    $cleanedContent = $cleanedContent -replace "(?s)CREATE DATABASE.*?(?=CREATE TABLE|INSERT INTO|$)", ""
    $cleanedContent = $cleanedContent -replace "USE \[.*?\]", ""
    
    # Remove database-level settings
    $cleanedContent = $cleanedContent -replace "ALTER DATABASE.*?GO", ""
    $cleanedContent = $cleanedContent -replace "IF \(1 = FULLTEXTSERVICEPROPERTY.*?GO", ""
    
    # Replace GO statements with semicolons
    $cleanedContent = $cleanedContent -replace "\r?\nGO\r?\n", ";\n"
    $cleanedContent = $cleanedContent -replace "\r?\nGO$", ";"
    
    # Remove file path references
    $cleanedContent = $cleanedContent -replace "FILENAME = N'[^']*'", "FILENAME = N'DATABASE_NAME'"
    
    # Remove script date comments (they can cause issues)
    $cleanedContent = $cleanedContent -replace "/\*\*\*\*\*\* Object:.*?Script Date:.*?\*\*\*\*\*\*/", ""
    
    # Clean up multiple semicolons and empty lines
    $cleanedContent = $cleanedContent -replace ";;+", ";"
    $cleanedContent = $cleanedContent -replace "\n\s*\n\s*\n", "\n\n"
    
    # Add hosting-friendly header
    $header = @"
-- SQL Server Database Script for Web Hosting
-- Cleaned and optimized for hosting environments
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
--
-- Instructions:
-- 1. Create a new database in your hosting control panel
-- 2. Run this script against the new database
-- 3. Update your connection string with hosting details
--

-- Disable constraints temporarily for import
-- (Comment out if not supported by your hosting)
-- EXEC sp_msforeachtable "ALTER TABLE ? NOCHECK CONSTRAINT all";

"@

    $footer = @"

-- Re-enable constraints after import
-- (Comment out if not supported by your hosting)
-- EXEC sp_msforeachtable "ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all";

-- Script completed successfully
"@

    $finalContent = $header + $cleanedContent + $footer
    
    # Save the cleaned script
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Resolve-Path ".").Path + "\$OutputFile", $finalContent, $utf8NoBom)
    
    Write-Host "✅ Cleaned script created!" -ForegroundColor Green
    Write-Host ""
    
    $outputSize = (Get-Item $OutputFile).Length
    Write-Host "Cleaned file: $OutputFile" -ForegroundColor White
    Write-Host "Size: $([math]::Round($outputSize / 1KB, 2)) KB" -ForegroundColor White
    
    # Show what was removed
    Write-Host ""
    Write-Host "Modifications made:" -ForegroundColor Cyan
    Write-Host "✓ Removed CREATE DATABASE statements" -ForegroundColor Green
    Write-Host "✓ Removed USE database statements" -ForegroundColor Green
    Write-Host "✓ Replaced GO statements with semicolons" -ForegroundColor Green
    Write-Host "✓ Removed file path references" -ForegroundColor Green
    Write-Host "✓ Removed database-level settings" -ForegroundColor Green
    Write-Host "✓ Added hosting-friendly headers" -ForegroundColor Green
    
    # Show preview
    Write-Host ""
    Write-Host "File preview (first 15 lines):" -ForegroundColor Cyan
    $previewLines = Get-Content $OutputFile -Head 15
    foreach ($line in $previewLines) {
        Write-Host $line -ForegroundColor Gray
    }
    Write-Host "..." -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Ready for MSSQL Hosting!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Upload instructions:" -ForegroundColor Cyan
    Write-Host "1. Create a new database in your hosting control panel" -ForegroundColor White
    Write-Host "2. Use the hosting's query tool or SQL import feature" -ForegroundColor White
    Write-Host "3. Run the cleaned script: $OutputFile" -ForegroundColor White
    Write-Host "4. Update connection strings with hosting database details" -ForegroundColor White
    Write-Host ""
    Write-Host "If you still get errors, your hosting may need:" -ForegroundColor Yellow
    Write-Host "• Tables and data to be imported separately" -ForegroundColor White
    Write-Host "• Schema created first, then data imported" -ForegroundColor White
    Write-Host "• Manual removal of additional unsupported statements" -ForegroundColor White

}
catch {
    Write-Host ""
    Write-Host "❌ Cleaning failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"