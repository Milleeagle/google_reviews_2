# Fix Production Connection Issue
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Fix Production Database Connection" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "The hosting server is trying to use LocalDB instead of your hosting MSSQL." -ForegroundColor Cyan
Write-Host "Let's fix this by ensuring Production environment is properly set." -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Creating corrected web.config for production..." -ForegroundColor Yellow
    
    $webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\google_reviews.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
          <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=81.95.105.76;Database=e003918;User Id=e003918a;Password=H(658e&amp;`6l2T;TrustServerCertificate=true;ConnectRetryCount=3;" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    $webConfig | Out-File -FilePath "web.config" -Encoding UTF8
    
    Write-Host "✅ Fixed web.config created!" -ForegroundColor Green
    Write-Host ""
    Write-Host "This web.config includes:" -ForegroundColor Cyan
    Write-Host "• Production environment setting" -ForegroundColor White
    Write-Host "• Direct connection string override" -ForegroundColor White
    Write-Host "• Enabled stdout logging for debugging" -ForegroundColor White
    Write-Host ""
    Write-Host "Now deploy this fix:" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File deploy-to-ftp.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "The application should now connect to your MSSQL hosting database!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Failed to create web.config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"