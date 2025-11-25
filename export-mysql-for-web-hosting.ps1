# Export MySQL Database for Web Hosting (Clean Encoding)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Export MySQL for Web Hosting" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$DatabaseName = "aspnet_google_reviews"
$OutputFile = "database-web-hosting.sql"

Write-Host "Creating clean SQL export for web hosting..." -ForegroundColor Cyan
Write-Host ""
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Output File: $OutputFile" -ForegroundColor White
Write-Host ""

try {
    # Find mysqldump
    $MySqlPaths = @(
        "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
        "C:\Program Files\MySQL\MySQL Server 8.1\bin\mysqldump.exe",
        "C:\Program Files\MySQL\MySQL Server 9.0\bin\mysqldump.exe"
    )
    
    $mysqldumpPath = $null
    foreach ($path in $MySqlPaths) {
        if (Test-Path $path) {
            $mysqldumpPath = $path
            Write-Host "✅ Found mysqldump at: $path" -ForegroundColor Green
            break
        }
    }
    
    if (-not $mysqldumpPath) {
        throw "mysqldump not found. Please ensure MySQL is installed."
    }

    # Get password
    Write-Host ""
    Write-Host "Please enter MySQL root password:" -ForegroundColor Yellow
    $password = Read-Host -AsSecureString
    $passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

    Write-Host ""
    Write-Host "Creating web-hosting compatible export..." -ForegroundColor Cyan

    # Clean export with specific options for web hosting
    $arguments = @(
        "-u", "root"
        if ($passwordText) { "-p$passwordText" }
        "--default-character-set=utf8"
        "--single-transaction"
        "--lock-tables=false"
        "--routines"
        "--triggers"
        "--add-drop-table"
        "--add-locks"
        "--extended-insert"
        "--create-options"
        "--disable-keys"
        "--set-charset"
        "--comments"
        $DatabaseName
    ) | Where-Object { $_ -ne $null }

    # Export to temporary file first
    $tempFile = "temp_export.sql"
    
    # Use cmd to ensure proper encoding
    $cmd = "`"$mysqldumpPath`" " + ($arguments -join " ") + " > `"$tempFile`""
    cmd /c $cmd

    if ($LASTEXITCODE -ne 0 -or !(Test-Path $tempFile)) {
        throw "MySQL export failed. Check connection and credentials."
    }

    # Read and clean the file, ensuring UTF-8 without BOM
    Write-Host "Cleaning and formatting SQL file..." -ForegroundColor Yellow
    
    $content = Get-Content $tempFile -Raw -Encoding UTF8
    
    # Clean up the content for web hosting compatibility
    $cleanContent = @"
-- Database Export for Web Hosting
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- Database: $DatabaseName
-- 
-- This file is optimized for web hosting import
--

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

--
-- Database structure and data
--

$content

COMMIT;
"@

    # Save with UTF-8 encoding without BOM
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Resolve-Path ".").Path + "\$OutputFile", $cleanContent, $utf8NoBom)
    
    # Clean up temp file
    Remove-Item $tempFile -Force

    # Verify the file
    if (!(Test-Path $OutputFile)) {
        throw "Failed to create clean export file"
    }

    $fileSize = (Get-Item $OutputFile).Length
    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)

    Write-Host "✅ Clean export created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "File: $OutputFile" -ForegroundColor White
    Write-Host "Size: $fileSizeMB MB" -ForegroundColor White
    Write-Host "Encoding: UTF-8 (no BOM)" -ForegroundColor White

    # Show preview
    Write-Host ""
    Write-Host "File preview:" -ForegroundColor Cyan
    $previewLines = Get-Content $OutputFile -Head 15
    foreach ($line in $previewLines) {
        Write-Host $line -ForegroundColor Gray
    }
    Write-Host "..." -ForegroundColor Gray

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Ready for Web Hosting!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Import settings for your hosting:" -ForegroundColor Cyan
    Write-Host "• Character set: utf-8" -ForegroundColor White
    Write-Host "• Format: SQL" -ForegroundColor White
    Write-Host "• SQL compatibility: NONE (default)" -ForegroundColor White
    Write-Host "• Enable foreign key checks: ✓" -ForegroundColor White
    Write-Host "• Allow partial import: ✓" -ForegroundColor White
    Write-Host ""
    Write-Host "This file should import without encoding errors!" -ForegroundColor Green

}
catch {
    Write-Host ""
    Write-Host "❌ Export failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Clean up any temp files
    if (Test-Path "temp_export.sql") {
        Remove-Item "temp_export.sql" -Force
    }
}

Write-Host ""
Read-Host "Press Enter to exit"