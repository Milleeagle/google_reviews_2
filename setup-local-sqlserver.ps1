# Setup Local SQL Server Development Environment
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Setup Local SQL Server Environment" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Setting up SQL Server LocalDB for development to match production..." -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Step 1: Cleaning up old MySQL migrations..." -ForegroundColor Yellow
    if (Test-Path "Data\Migrations") {
        Remove-Item "Data\Migrations" -Recurse -Force
        Write-Host "✅ Removed MySQL migration files" -ForegroundColor Green
    } else {
        Write-Host "✅ No old migrations to remove" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Step 2: Restoring packages..." -ForegroundColor Yellow
    & dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed"
    }
    Write-Host "✅ Packages restored" -ForegroundColor Green

    Write-Host ""
    Write-Host "Step 3: Creating SQL Server migration..." -ForegroundColor Yellow
    & dotnet ef migrations add InitialSqlServerMigration
    if ($LASTEXITCODE -ne 0) {
        throw "Migration creation failed"
    }
    Write-Host "✅ SQL Server migration created" -ForegroundColor Green

    Write-Host ""
    Write-Host "Step 4: Updating local database..." -ForegroundColor Yellow
    & dotnet ef database update
    if ($LASTEXITCODE -ne 0) {
        throw "Database update failed"
    }
    Write-Host "✅ Local SQL Server database created" -ForegroundColor Green

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Setup Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Local Development: SQL Server LocalDB" -ForegroundColor Cyan
    Write-Host "✅ Production Hosting: SQL Server Cloud" -ForegroundColor Cyan
    Write-Host "✅ Consistent MSSQL environment everywhere" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Database Details:" -ForegroundColor White
    Write-Host "• Local: (localdb)\mssqllocaldb" -ForegroundColor Gray
    Write-Host "• Database: aspnet-google_reviews" -ForegroundColor Gray
    Write-Host "• Production: 81.95.105.76 / e003918" -ForegroundColor Gray
    Write-Host ""
    Write-Host "You can now run your application with consistent MSSQL!" -ForegroundColor Green

} catch {
    Write-Host ""
    Write-Host "❌ Setup failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Ensure SQL Server LocalDB is installed" -ForegroundColor White
    Write-Host "2. Verify Entity Framework tools are installed" -ForegroundColor White
    Write-Host "3. Check that the project builds successfully" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to exit"