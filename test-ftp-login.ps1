# Simple FTP Login Test
Write-Host "Testing FTP Login..." -ForegroundColor Cyan

try {
    # Load credentials
    $creds = Get-Content 'credentials.json' | ConvertFrom-Json
    $ftpServer = $creds.FTP.Server
    $ftpUsername = $creds.FTP.Username
    $ftpPassword = $creds.FTP.Password
    
    Write-Host "Server: $ftpServer" -ForegroundColor White
    Write-Host "Username: $ftpUsername" -ForegroundColor White
    Write-Host "Password: $ftpPassword" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Attempting login..." -ForegroundColor Yellow
    
    # Simple FTP connection test
    $ftpRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/")
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    
    $response = $ftpRequest.GetResponse()
    
    Write-Host "✅ LOGIN SUCCESSFUL!" -ForegroundColor Green
    Write-Host "Response: $($response.StatusDescription)" -ForegroundColor Green
    
    $response.Close()
}
catch {
    Write-Host "❌ LOGIN FAILED!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Read-Host "Press Enter to exit"