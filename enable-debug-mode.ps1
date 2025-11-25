# Enable Debug Mode for Production Debugging
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Enable Debug Mode for Production" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "⚠️  WARNING: This will enable detailed error messages on your live site!" -ForegroundColor Red
Write-Host "Only use this temporarily for debugging, then disable it!" -ForegroundColor Red
Write-Host ""

$continue = Read-Host "Continue? (Y/N)"
if ($continue -ne "Y" -and $continue -ne "y") {
    Write-Host "Operation cancelled." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit
}

try {
    Write-Host "Creating web.config with debug mode enabled..." -ForegroundColor Cyan
    
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
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    $webConfig | Out-File -FilePath "web.config" -Encoding UTF8
    
    Write-Host "✅ web.config created with debug mode enabled!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Now deploy this to see detailed errors:" -ForegroundColor Cyan
    Write-Host "powershell -ExecutionPolicy Bypass -File deploy-to-ftp.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "After debugging, remember to disable debug mode!" -ForegroundColor Yellow
    Write-Host "Run: powershell -ExecutionPolicy Bypass -File disable-debug-mode.ps1" -ForegroundColor White
    
} catch {
    Write-Host "❌ Failed to create web.config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"