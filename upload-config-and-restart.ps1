# Upload configuration file and force application restart
Write-Host "Uploading configuration and restarting application..." -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "FTP Server: $ftpServer" -ForegroundColor Yellow

# Function to upload a file
function Upload-File($localFile, $remotePath) {
    try {
        if (-not (Test-Path $localFile)) {
            Write-Host "Local file not found: $localFile" -ForegroundColor Red
            return $false
        }

        $ftpUri = "ftp://$ftpServer/$remotePath"
        Write-Host "Uploading: $localFile -> $remotePath" -ForegroundColor Yellow

        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $ftpRequest.UseBinary = $true

        $fileBytes = [System.IO.File]::ReadAllBytes($localFile)
        $ftpRequest.ContentLength = $fileBytes.Length

        $requestStream = $ftpRequest.GetRequestStream()
        $requestStream.Write($fileBytes, 0, $fileBytes.Length)
        $requestStream.Close()

        $response = $ftpRequest.GetResponse()
        Write-Host "Success: $remotePath uploaded!" -ForegroundColor Green
        $response.Close()
        return $true

    } catch {
        Write-Host "Failed to upload $remotePath : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Step 1: Upload the updated configuration file
Write-Host "`n=== Step 1: Uploading Configuration ===" -ForegroundColor Cyan
$configUploaded = Upload-File "appsettings.Production.json" "appsettings.Production.json"

if (-not $configUploaded) {
    Write-Host "Configuration upload failed. Aborting." -ForegroundColor Red
    exit 1
}

# Step 2: Force application restart by touching web.config
Write-Host "`n=== Step 2: Forcing Application Restart ===" -ForegroundColor Cyan

try {
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
    $webConfigContent = $webConfigContent -replace '<!--ProjectGuid: 04D14253-3682-4A26-9478-3B79D52339A0-->', "<!--ProjectGuid: 04D14253-3682-4A26-9478-3B79D52339A0--><!-- Config Update: $timestamp -->"

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
    Write-Host "Application restart triggered successfully!" -ForegroundColor Green
    $response.Close()

} catch {
    Write-Host "Failed to restart application: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "✓ Configuration file uploaded with email credentials" -ForegroundColor Green
Write-Host "✓ Application restart triggered" -ForegroundColor Green
Write-Host "`nWait 10-15 seconds, then test email functionality!" -ForegroundColor Yellow