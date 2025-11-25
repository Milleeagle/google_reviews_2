# Export MySQL Database for Hosting
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Export MySQL Database for Hosting" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$DatabaseName = "aspnet_google_reviews"
$OutputFile = "database-export-for-hosting.sql"
$CompressedFile = "database-export-for-hosting.sql.gz"

Write-Host "This will export your MySQL database to a .sql file for hosting import." -ForegroundColor Cyan
Write-Host ""
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Output File: $OutputFile" -ForegroundColor White
Write-Host ""

try {
    # Check if mysqldump is available
    Write-Host "Checking for mysqldump..." -ForegroundColor Yellow
    $mysqldumpTest = & mysqldump --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "mysqldump not found. Please ensure MySQL is installed and mysqldump is in your PATH."
    }
    Write-Host "✅ mysqldump found" -ForegroundColor Green

    # Get MySQL password
    Write-Host ""
    Write-Host "Please enter MySQL root password (leave blank if no password):" -ForegroundColor Yellow
    $password = Read-Host -AsSecureString
    $passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

    Write-Host ""
    Write-Host "Exporting database with mysqldump..." -ForegroundColor Cyan
    
    # Build mysqldump command
    if ([string]::IsNullOrEmpty($passwordText)) {
        # No password
        & mysqldump -u root --single-transaction --routines --triggers $DatabaseName > $OutputFile
    } else {
        # With password
        & mysqldump -u root -p$passwordText --single-transaction --routines --triggers $DatabaseName > $OutputFile
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Database export failed. Please check MySQL connection and database name."
    }

    Write-Host "✅ Database exported successfully!" -ForegroundColor Green
    Write-Host ""

    # Check file size
    $fileSize = (Get-Item $OutputFile).Length
    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)

    Write-Host "File: $OutputFile" -ForegroundColor White
    Write-Host "Size: $fileSizeMB MB" -ForegroundColor White

    if ($fileSizeMB -gt 50) {
        Write-Host ""
        Write-Host "⚠️  File is large ($fileSizeMB MB). Consider compressing it:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To compress to .gz format:" -ForegroundColor Cyan
        Write-Host "1. Install 7-Zip" -ForegroundColor White
        Write-Host "2. Right-click $OutputFile > 7-Zip > Add to archive" -ForegroundColor White
        Write-Host "3. Choose gzip format and rename to: $CompressedFile" -ForegroundColor White
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Upload Instructions" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "1. Login to your hosting control panel" -ForegroundColor Cyan
    Write-Host "2. Go to MySQL/Database section" -ForegroundColor Cyan
    Write-Host "3. Create a new database (note the name, user, password)" -ForegroundColor Cyan
    Write-Host "4. Import $OutputFile (or $CompressedFile if compressed)" -ForegroundColor Cyan
    Write-Host "5. Update your production appsettings.json with the hosting database details" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Production connection string format:" -ForegroundColor Yellow
    Write-Host "Server=YOUR_MYSQL_HOST;Database=YOUR_DATABASE_NAME;User=YOUR_USER;Password=YOUR_PASSWORD;Port=3306;" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "❌ Export failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Make sure MySQL server is running" -ForegroundColor White
    Write-Host "2. Verify database '$DatabaseName' exists" -ForegroundColor White
    Write-Host "3. Check MySQL root password" -ForegroundColor White
    Write-Host "4. Ensure mysqldump is in your PATH" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to exit"