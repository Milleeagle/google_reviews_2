# Disable Debug Mode (Back to Production)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Disable Debug Mode" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Creating production web.config..." -ForegroundColor Cyan

try {
    $webConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\google_reviews.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    $webConfig | Out-File -FilePath "web.config" -Encoding UTF8
    
    Write-Host "✅ Production web.config created!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Deploy this to disable debug mode:" -ForegroundColor Cyan
    Write-Host "powershell -ExecutionPolicy Bypass -File deploy-to-ftp.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "Debug mode is now disabled - your site is secure again!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Failed to create web.config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"