# Enable Debug Mode with Production Database
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Debug Mode + Production Database" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "This will enable detailed error messages while keeping the production database." -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Creating debug web.config with production database..." -ForegroundColor Yellow
    
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
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
          <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=81.95.105.76;Database=e003918;User Id=e003918a;Password=H(658e&amp;`6l2T;TrustServerCertificate=true;ConnectRetryCount=3;" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    $webConfig | Out-File -FilePath "web.config" -Encoding UTF8
    
    Write-Host "✅ Debug web.config created!" -ForegroundColor Green
    Write-Host ""
    Write-Host "This configuration:" -ForegroundColor Cyan
    Write-Host "• Environment: Development (shows detailed errors)" -ForegroundColor White
    Write-Host "• Database: Production MSSQL (81.95.105.76)" -ForegroundColor White
    Write-Host "• Logging: Enabled for troubleshooting" -ForegroundColor White
    Write-Host ""
    Write-Host "Deploy this debug version:" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File deploy-to-ftp.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "You'll now see detailed errors while using the production database!" -ForegroundColor Green
    Write-Host ""
    Write-Host "⚠️  Remember to disable debug mode when done debugging!" -ForegroundColor Red
    
} catch {
    Write-Host "❌ Failed to create web.config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"