# Test MSSQL Connection
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Test MSSQL Connection" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "This will manually switch to MSSQL and test the connection..." -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Reading Program.cs..." -ForegroundColor Yellow
    $programFile = "Program.cs"
    $content = Get-Content $programFile -Raw
    
    Write-Host "Switching to MSSQL connection..." -ForegroundColor Yellow
    
    # Replace connection string reference
    $newContent = $content -replace 'GetConnectionString\("DefaultConnection"\)', 'GetConnectionString("SqlServerConnection")'
    $newContent = $newContent -replace 'GetConnectionString\("CloudConnection"\)', 'GetConnectionString("SqlServerConnection")'
    
    # Switch from MySQL to SQL Server provider
    $newContent = $newContent -replace 'options\.UseMySql\(connectionString, serverVersion\)', 'options.UseSqlServer(connectionString)'
    
    # Save changes
    Set-Content $programFile $newContent -Encoding UTF8
    
    Write-Host "✅ Successfully switched to MSSQL!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Connection Details:" -ForegroundColor Cyan
    Write-Host "Server: 81.95.105.76" -ForegroundColor White
    Write-Host "Database: e003918" -ForegroundColor White
    Write-Host "Username: e003918a" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Stop your application if running" -ForegroundColor White
    Write-Host "2. Build and run: dotnet run" -ForegroundColor White
    Write-Host "3. Or restart from Visual Studio" -ForegroundColor White
    Write-Host ""
    Write-Host "Your app should now connect to the MSSQL hosting database!" -ForegroundColor Green
    Write-Host "You should see all your imported data (companies, reviews, users)." -ForegroundColor Green
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press Enter to exit..."
Read-Host