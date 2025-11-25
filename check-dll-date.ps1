# Check DLL timestamp
Write-Host "Checking DLL timestamps" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

try {
    $ftpUri = "ftp://$ftpServer/google_reviews.dll"
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::GetDateTimestamp
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

    $response = $ftpRequest.GetResponse()
    $serverTime = $response.LastModified
    Write-Host "Server DLL timestamp: $serverTime" -ForegroundColor Yellow
    $response.Close()

} catch {
    Write-Host "Could not get server timestamp: $($_.Exception.Message)" -ForegroundColor Red
}

# Local file timestamp
$localDll = ".\publish\google_reviews.dll"
if (Test-Path $localDll) {
    $localTime = (Get-Item $localDll).LastWriteTime
    Write-Host "Local DLL timestamp:  $localTime" -ForegroundColor Yellow
} else {
    Write-Host "Local DLL not found" -ForegroundColor Red
}