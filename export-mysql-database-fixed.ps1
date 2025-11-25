# Export MySQL Database for Hosting (Fixed Path Version)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Export MySQL Database for Hosting" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$DatabaseName = "aspnet_google_reviews"
$OutputFile = "database-export-for-hosting.sql"
$CompressedFile = "database-export-for-hosting.sql.gz"

# Define possible MySQL paths
$MySqlPaths = @(
    "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
    "C:\Program Files\MySQL\MySQL Server 8.1\bin\mysqldump.exe",
    "C:\Program Files\MySQL\MySQL Server 9.0\bin\mysqldump.exe",
    "C:\mysql\bin\mysqldump.exe",
    "C:\xampp\mysql\bin\mysqldump.exe"
)

Write-Host "This will export your MySQL database to a .sql file for hosting import." -ForegroundColor Cyan
Write-Host ""
Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Output File: $OutputFile" -ForegroundColor White
Write-Host ""

try {
    # Find mysqldump
    Write-Host "Looking for mysqldump..." -ForegroundColor Yellow
    $mysqldumpPath = $null
    
    foreach ($path in $MySqlPaths) {
        if (Test-Path $path) {
            $mysqldumpPath = $path
            Write-Host "✅ Found mysqldump at: $path" -ForegroundColor Green
            break
        }
    }
    
    if (-not $mysqldumpPath) {
        throw "mysqldump not found in common MySQL installation paths. Please ensure MySQL is installed."
    }

    # Test mysqldump
    $versionTest = & $mysqldumpPath --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "mysqldump found but not working properly"
    }

    # Get MySQL password
    Write-Host ""
    Write-Host "Please enter MySQL root password:" -ForegroundColor Yellow
    $password = Read-Host -AsSecureString
    $passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

    Write-Host ""
    Write-Host "Exporting database with mysqldump..." -ForegroundColor Cyan
    
    # Build mysqldump command with full path
    if ([string]::IsNullOrEmpty($passwordText)) {
        # No password
        & $mysqldumpPath -u root --single-transaction --routines --triggers $DatabaseName > $OutputFile
    } else {
        # With password
        & $mysqldumpPath -u root -p"$passwordText" --single-transaction --routines --triggers $DatabaseName > $OutputFile
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Database export failed. Please check MySQL connection and database name."
    }

    # Check if file was created and has content
    if (-not (Test-Path $OutputFile)) {
        throw "Export file was not created"
    }

    $fileSize = (Get-Item $OutputFile).Length
    if ($fileSize -eq 0) {
        throw "Export file is empty - check MySQL connection and permissions"
    }

    Write-Host "✅ Database exported successfully!" -ForegroundColor Green
    Write-Host ""

    $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
    Write-Host "File: $OutputFile" -ForegroundColor White
    Write-Host "Size: $fileSizeMB MB" -ForegroundColor White

    # Show first few lines of the export to verify it looks correct
    Write-Host ""
    Write-Host "Export file preview (first 10 lines):" -ForegroundColor Cyan
    Get-Content $OutputFile -Head 10 | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
    Write-Host "..." -ForegroundColor Gray

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
    Write-Host "   Ready for Hosting Upload!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Upload Instructions:" -ForegroundColor Cyan
    Write-Host "1. Login to your hosting control panel" -ForegroundColor White
    Write-Host "2. Go to MySQL/Database section" -ForegroundColor White
    Write-Host "3. Create a new database (note the name, user, password)" -ForegroundColor White
    Write-Host "4. Import $OutputFile (or compress to .gz if large)" -ForegroundColor White
    Write-Host "5. Update appsettings.Production.json with hosting database details" -ForegroundColor White
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
    Write-Host "3. Check MySQL root password is correct" -ForegroundColor White
    Write-Host "4. Ensure MySQL user has export permissions" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to exit"