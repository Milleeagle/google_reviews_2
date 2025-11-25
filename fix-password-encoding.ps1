# Fix Password Encoding for XML
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Fix Password XML Encoding" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "The password contains special characters that need proper XML encoding." -ForegroundColor Cyan
Write-Host "Original password: H(658e&`6l2T" -ForegroundColor White
Write-Host ""

try {
    Write-Host "Creating web.config with properly encoded password..." -ForegroundColor Yellow
    
    # Properly XML-encode the password: H(658e&`6l2T
    # & becomes &amp;
    # ` becomes &#96; 
    $encodedPassword = "H(658e&amp;&#96;6l2T"
    
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
          <environmentVariable name="ConnectionStrings__DefaultConnection" value="Server=81.95.105.76;Database=e003918;User Id=e003918a;Password=$encodedPassword;TrustServerCertificate=true;ConnectRetryCount=3;" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
    
    $webConfig | Out-File -FilePath "web.config" -Encoding UTF8
    
    Write-Host "✅ Web.config with encoded password created!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Password encoding:" -ForegroundColor Cyan
    Write-Host "Original: H(658e&`6l2T" -ForegroundColor White
    Write-Host "Encoded:  $encodedPassword" -ForegroundColor White
    Write-Host ""
    Write-Host "Deploy this fix:" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File deploy-to-ftp.ps1" -ForegroundColor White
    Write-Host ""
    Write-Host "This should resolve the login failed error!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Failed to create web.config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"