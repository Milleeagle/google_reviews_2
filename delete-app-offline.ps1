# Load FTP credentials
if (Test-Path "credentials.json") {
    $credentials = Get-Content "credentials.json" | ConvertFrom-Json
    $ftpServer = $credentials.ftpServer
    $ftpUsername = $credentials.ftpUsername
    $ftpPassword = $credentials.ftpPassword
} else {
    Write-Host "ERROR: credentials.json not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Deleting app_offline.htm from FTP server..." -ForegroundColor Yellow

try {
    $ftp = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/app_offline.htm")
    $ftp.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
    $ftp.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile

    $response = $ftp.GetResponse()
    Write-Host "SUCCESS: app_offline.htm deleted successfully!" -ForegroundColor Green
    $response.Close()
} catch {
    if ($_.Exception.Message -like "*file not found*" -or $_.Exception.Message -like "*550*") {
        Write-Host "app_offline.htm was already deleted or doesn't exist." -ForegroundColor Yellow
    } else {
        Write-Host "ERROR deleting app_offline.htm: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "Application should now restart with the new version..." -ForegroundColor Green