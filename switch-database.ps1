# Database Connection Switcher
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Database Connection Switcher" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Choose database connection:" -ForegroundColor Cyan
Write-Host "1. Local MySQL (localhost - for development)" -ForegroundColor White
Write-Host "2. Cloud MySQL (sql109.infinityfree.com - for testing with MySQL data)" -ForegroundColor White
Write-Host "3. Cloud MSSQL (81.95.105.76 - for testing with SQL Server data)" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Enter choice (1, 2, or 3)"

try {
    $programFile = "Program.cs"
    $content = Get-Content $programFile -Raw

    if ($choice -eq "1") {
        # Switch to local database
        $newContent = $content -replace 'GetConnectionString\("CloudConnection"\)', 'GetConnectionString("DefaultConnection")'
        
        Set-Content $programFile $newContent -Encoding UTF8
        
        Write-Host ""
        Write-Host "✅ Switched to LOCAL MySQL database" -ForegroundColor Green
        Write-Host "   Server: localhost" -ForegroundColor White
        Write-Host "   Database: aspnet_google_reviews" -ForegroundColor White
        Write-Host "   User: root" -ForegroundColor White
        Write-Host ""
        Write-Host "Your application will now use the local MySQL database for development." -ForegroundColor Cyan
    }
    elseif ($choice -eq "2") {
        # Switch to cloud MySQL database
        $newContent = $content -replace 'GetConnectionString\("DefaultConnection"\)', 'GetConnectionString("CloudConnection")'
        $newContent = $newContent -replace 'GetConnectionString\("SqlServerConnection"\)', 'GetConnectionString("CloudConnection")'
        
        Set-Content $programFile $newContent -Encoding UTF8
        
        Write-Host ""
        Write-Host "✅ Switched to CLOUD MySQL database" -ForegroundColor Green
        Write-Host "   Server: sql109.infinityfree.com" -ForegroundColor White
        Write-Host "   Database: if0_39916935_google_reviews" -ForegroundColor White
        Write-Host "   User: if0_39916935" -ForegroundColor White
        Write-Host ""
        Write-Host "Your application will now use the cloud MySQL database." -ForegroundColor Cyan
        Write-Host ""
        Write-Host "⚠️  WARNING: You are now connected to PRODUCTION data!" -ForegroundColor Yellow
        Write-Host "   Be careful when testing - changes will affect live data." -ForegroundColor Yellow
    }
    elseif ($choice -eq "3") {
        # Switch to cloud SQL Server database
        $newContent = $content -replace 'GetConnectionString\("DefaultConnection"\)', 'GetConnectionString("SqlServerConnection")'
        $newContent = $newContent -replace 'GetConnectionString\("CloudConnection"\)', 'GetConnectionString("SqlServerConnection")'
        $newContent = $newContent -replace 'options\.UseMySql\(connectionString, serverVersion\)', 'options.UseSqlServer(connectionString)'
        
        Set-Content $programFile $newContent -Encoding UTF8
        
        Write-Host ""
        Write-Host "✅ Switched to CLOUD SQL SERVER database" -ForegroundColor Green
        Write-Host "   Server: 81.95.105.76" -ForegroundColor White
        Write-Host "   Database: e003918" -ForegroundColor White
        Write-Host "   User: e003918a" -ForegroundColor White
        Write-Host ""
        Write-Host "Your application will now use the cloud SQL Server database." -ForegroundColor Cyan
        Write-Host ""
        Write-Host "⚠️  WARNING: You are now connected to PRODUCTION data!" -ForegroundColor Yellow
        Write-Host "   Be careful when testing - changes will affect live data." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "📋 Note: Database provider switched to SQL Server" -ForegroundColor Cyan
    }
    else {
        Write-Host ""
        Write-Host "❌ Invalid choice. Please run again and choose 1, 2, or 3." -ForegroundColor Red
        exit
    }

    Write-Host ""
    Write-Host "To apply the change:" -ForegroundColor Cyan
    Write-Host "1. Stop your application if running" -ForegroundColor White
    Write-Host "2. Build and run: dotnet run" -ForegroundColor White
    Write-Host "   OR restart from Visual Studio" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "❌ Failed to switch database: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"