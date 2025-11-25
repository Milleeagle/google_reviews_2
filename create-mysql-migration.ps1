# Create MySQL Migration
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Create MySQL Migration" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "This will create a new MySQL migration to replace the SQL Server one." -ForegroundColor Cyan
Write-Host ""
Write-Host "IMPORTANT: Make sure you have MySQL/MariaDB installed and running locally" -ForegroundColor Yellow
Write-Host "before proceeding. Default connection expects MySQL on localhost:3306" -ForegroundColor Yellow
Write-Host "with root user and no password." -ForegroundColor Yellow
Write-Host ""

$continue = Read-Host "Continue? (Y/N)"
if ($continue -ne "Y" -and $continue -ne "y") {
    Write-Host "Operation cancelled." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit
}

try {
    Write-Host ""
    Write-Host "Step 1: Removing old SQL Server migration files..." -ForegroundColor Cyan
    if (Test-Path "Data\Migrations") {
        Remove-Item "Data\Migrations" -Recurse -Force
        Write-Host "Removed old migration files" -ForegroundColor Green
    } else {
        Write-Host "No old migration files found" -ForegroundColor Gray
    }

    Write-Host ""
    Write-Host "Step 2: Restoring NuGet packages (including new MySQL package)..." -ForegroundColor Cyan
    & dotnet restore
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to restore packages"
    }
    Write-Host "✅ Packages restored successfully" -ForegroundColor Green

    Write-Host ""
    Write-Host "Step 3: Creating new MySQL migration..." -ForegroundColor Cyan
    & dotnet ef migrations add InitialMySqlMigration
    
    if ($LASTEXITCODE -ne 0) {
        throw "Migration creation failed. Please check that MySQL server is running and accessible."
    }
    Write-Host "✅ Migration created successfully" -ForegroundColor Green

    Write-Host ""
    Write-Host "Step 4: Updating database with new migration..." -ForegroundColor Cyan
    & dotnet ef database update
    
    if ($LASTEXITCODE -ne 0) {
        throw "Database update failed. Please check MySQL server connection and permissions."
    }
    Write-Host "✅ Database updated successfully" -ForegroundColor Green

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   MySQL Migration Completed!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Test the application locally with MySQL" -ForegroundColor White
    Write-Host "2. Use export-mysql-database script to export for hosting" -ForegroundColor White
    Write-Host "3. Deploy using the existing FTP deployment scripts" -ForegroundColor White
    Write-Host ""
    Write-Host "MySQL Database: aspnet_google_reviews" -ForegroundColor Yellow
    Write-Host "Connection: localhost:3306" -ForegroundColor Yellow
}
catch {
    Write-Host ""
    Write-Host "❌ Migration failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Install MySQL/MariaDB if not installed" -ForegroundColor White
    Write-Host "2. Start MySQL service" -ForegroundColor White
    Write-Host "3. Check connection string in appsettings.json" -ForegroundColor White
    Write-Host "4. Verify MySQL root user has no password or update connection string" -ForegroundColor White
    Write-Host ""
}

Write-Host ""
Read-Host "Press Enter to exit"