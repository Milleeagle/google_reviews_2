# Simple FTP check
Write-Host "Checking FTP server contents..." -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "FTP Server: $ftpServer" -ForegroundColor Yellow

try {
    $ftpUri = "ftp://$ftpServer/"
    Write-Host "Listing root directory: $ftpUri" -ForegroundColor Cyan

    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

    $response = $ftpRequest.GetResponse()
    $responseStream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($responseStream)
    $listing = $reader.ReadToEnd()
    $reader.Close()
    $response.Close()

    Write-Host "=== ROOT DIRECTORY LISTING ===" -ForegroundColor Green
    $listing -split "`n" | ForEach-Object {
        if ($_.Trim() -ne "") {
            Write-Host $_.Trim()
        }
    }
    Write-Host "===============================" -ForegroundColor Green

} catch {
    Write-Host "Failed to list directory: $($_.Exception.Message)" -ForegroundColor Red
}

# Check for google_reviews.dll specifically
try {
    $ftpUri = "ftp://$ftpServer/google_reviews.dll"
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

    $response = $ftpRequest.GetResponse()
    $size = $response.ContentLength
    $response.Close()
    Write-Host "✓ google_reviews.dll found - Size: $size bytes" -ForegroundColor Green

} catch {
    Write-Host "✗ google_reviews.dll not found" -ForegroundColor Red
}