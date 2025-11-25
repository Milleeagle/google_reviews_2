# Force application restart after deployment
Write-Host "Forcing application restart..." -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

# Method 1: Touch web.config to force app restart
try {
    Write-Host "Touching web.config to trigger app restart..." -ForegroundColor Yellow

    # Download current web.config
    $ftpUri = "ftp://$ftpServer/web.config"
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

    $response = $ftpRequest.GetResponse()
    $responseStream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($responseStream)
    $webConfigContent = $reader.ReadToEnd()
    $reader.Close()
    $response.Close()

    # Add a timestamp comment to force change
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $webConfigContent = $webConfigContent -replace '<!--ProjectGuid: 04D14253-3682-4A26-9478-3B79D52339A0-->', "<!--ProjectGuid: 04D14253-3682-4A26-9478-3B79D52339A0--><!-- Restart: $timestamp -->"

    # Upload modified web.config
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
    $ftpRequest.UseBinary = $true

    $fileBytes = [System.Text.Encoding]::UTF8.GetBytes($webConfigContent)
    $ftpRequest.ContentLength = $fileBytes.Length

    $requestStream = $ftpRequest.GetRequestStream()
    $requestStream.Write($fileBytes, 0, $fileBytes.Length)
    $requestStream.Close()

    $response = $ftpRequest.GetResponse()
    Write-Host "web.config touched successfully - app should restart!" -ForegroundColor Green
    $response.Close()

} catch {
    Write-Host "Failed to touch web.config: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Application restart triggered. Wait 10-15 seconds then check your website." -ForegroundColor Cyan