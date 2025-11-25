# Clean SQL Server Script for Web Hosting (Version 2)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Clean SQL Server Script v2" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$InputFile = Read-Host "Enter the path to your SSMS-generated .sql file"
$OutputFile = "database-sqlserver-hosting-clean.sql"

if (!(Test-Path $InputFile)) {
    Write-Host "❌ File not found: $InputFile" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

try {
    Write-Host "Reading and aggressively cleaning SQL script..." -ForegroundColor Cyan
    
    $content = Get-Content $InputFile -Raw
    
    Write-Host "Original file size: $([math]::Round((Get-Item $InputFile).Length / 1KB, 2)) KB" -ForegroundColor White
    
    # More aggressive cleaning for web hosting compatibility
    Write-Host "Applying comprehensive hosting-friendly modifications..." -ForegroundColor Yellow
    
    $cleanedContent = $content
    
    # Remove all escape characters and clean newlines
    $cleanedContent = $cleanedContent -replace "\\n", "`n"
    $cleanedContent = $cleanedContent -replace "\\r", "`r"
    $cleanedContent = $cleanedContent -replace "\\t", "`t"
    
    # Remove database creation and management statements
    $cleanedContent = $cleanedContent -replace "(?s)USE \[master\].*?(?=CREATE TABLE|INSERT INTO|\z)", ""
    $cleanedContent = $cleanedContent -replace "(?s)CREATE DATABASE.*?(?=CREATE TABLE|INSERT INTO|\z)", ""
    $cleanedContent = $cleanedContent -replace "USE \[.*?\]", ""
    $cleanedContent = $cleanedContent -replace "(?s)ALTER DATABASE.*?(?=CREATE TABLE|INSERT INTO|\z)", ""
    
    # Remove advanced SQL Server features
    $cleanedContent = $cleanedContent -replace "(?s)IF \(1 = FULLTEXTSERVICEPROPERTY.*?end", ""
    $cleanedContent = $cleanedContent -replace "EXEC \[.*?\]\.\[dbo\]\.\[sp_fulltext_database\].*?", ""
    
    # Simplify table creation - remove advanced options
    $cleanedContent = $cleanedContent -replace "WITH \(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF\)", ""
    $cleanedContent = $cleanedContent -replace "WITH \(PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON\)", ""
    $cleanedContent = $cleanedContent -replace "ON \[PRIMARY\]", ""
    $cleanedContent = $cleanedContent -replace "\) ON \[PRIMARY\]", ")"
    
    # Replace GO statements with semicolons
    $cleanedContent = $cleanedContent -replace "\r?\nGO\r?\n", ";\n"
    $cleanedContent = $cleanedContent -replace "\r?\nGO$", ";"
    $cleanedContent = $cleanedContent -replace "^GO\r?\n", ""
    
    # Remove schema qualifiers that might cause issues
    $cleanedContent = $cleanedContent -replace "\[dbo\]\.", ""
    
    # Remove comments that might contain problematic characters
    $cleanedContent = $cleanedContent -replace "/\*.*?\*/", ""
    
    # Clean up identity and computed column specifications that might not be supported
    $cleanedContent = $cleanedContent -replace "IDENTITY\(1,1\)", "IDENTITY(1,1)"
    
    # Remove SET statements that might not be supported
    $cleanedContent = $cleanedContent -replace "SET ANSI_NULLS.*?\n", ""
    $cleanedContent = $cleanedContent -replace "SET QUOTED_IDENTIFIER.*?\n", ""
    
    # Clean up multiple semicolons and empty lines
    $cleanedContent = $cleanedContent -replace ";;+", ";"
    $cleanedContent = $cleanedContent -replace "\n\s*\n\s*\n+", "`n`n"
    $cleanedContent = $cleanedContent -replace "^\s*\n+", ""
    
    # Add simple, hosting-friendly header
    $header = @"
-- SQL Server Database Script (Hosting Compatible)
-- Cleaned for maximum hosting compatibility
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

-- Simple table creation and data insertion
-- Compatible with most SQL Server hosting providers

"@

    $footer = @"

-- Import completed
-- Remember to update your connection strings
"@

    $finalContent = $header + $cleanedContent.Trim() + $footer
    
    # Final cleanup - ensure proper line endings
    $finalContent = $finalContent -replace "\r\n", "`n"
    $finalContent = $finalContent -replace "\r", "`n"
    $finalContent = $finalContent -replace "\n", "`r`n"
    
    # Save the cleaned script with UTF-8 encoding
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Resolve-Path ".").Path + "\$OutputFile", $finalContent, $utf8NoBom)
    
    Write-Host "✅ Ultra-cleaned script created!" -ForegroundColor Green
    Write-Host ""
    
    $outputSize = (Get-Item $OutputFile).Length
    Write-Host "Ultra-clean file: $OutputFile" -ForegroundColor White
    Write-Host "Size: $([math]::Round($outputSize / 1KB, 2)) KB" -ForegroundColor White
    
    # Show what was removed/fixed
    Write-Host ""
    Write-Host "Aggressive modifications made:" -ForegroundColor Cyan
    Write-Host "✓ Fixed escape characters" -ForegroundColor Green
    Write-Host "✓ Removed all database creation statements" -ForegroundColor Green
    Write-Host "✓ Removed advanced table options" -ForegroundColor Green
    Write-Host "✓ Simplified schema references" -ForegroundColor Green
    Write-Host "✓ Replaced GO with semicolons" -ForegroundColor Green
    Write-Host "✓ Removed problematic SET statements" -ForegroundColor Green
    Write-Host "✓ Cleaned up comments and formatting" -ForegroundColor Green
    Write-Host "✓ Ensured proper line endings" -ForegroundColor Green
    
    # Show preview
    Write-Host ""
    Write-Host "File preview (first 20 lines):" -ForegroundColor Cyan
    $previewLines = Get-Content $OutputFile -Head 20
    foreach ($line in $previewLines) {
        if ($line.Trim() -ne "") {
            Write-Host $line -ForegroundColor Gray
        }
    }
    Write-Host "..." -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Ready for MSSQL Hosting!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "This ultra-cleaned version should work with most SQL Server hosts." -ForegroundColor Cyan
    Write-Host "If you still get errors, your host may require:" -ForegroundColor Yellow
    Write-Host "• Importing schema (tables) separately from data" -ForegroundColor White
    Write-Host "• Using their specific import wizard" -ForegroundColor White
    Write-Host "• Breaking the script into smaller chunks" -ForegroundColor White

}
catch {
    Write-Host ""
    Write-Host "❌ Cleaning failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Error details: $($_.Exception.InnerException)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"