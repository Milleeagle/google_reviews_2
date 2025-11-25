# Upload critical files for filtering fixes
Write-Host "Uploading critical filtering fix files..." -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

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
        Write-Host "Success: $remotePath" -ForegroundColor Green
        $response.Close()
        return $true

    } catch {
        Write-Host "Failed: $remotePath - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

$publishPath = ".\publish"

# Critical files for filtering fixes
$criticalFiles = @(
    @{ Local = "$publishPath\google_reviews.dll"; Remote = "google_reviews.dll" },
    @{ Local = "$publishPath\google_reviews.exe"; Remote = "google_reviews.exe" }
)

Write-Host "Attempting to upload critical files for filtering fixes..." -ForegroundColor Cyan

$successCount = 0
$failCount = 0

foreach ($file in $criticalFiles) {
    if (Upload-File $file.Local $file.Remote) {
        $successCount++
    } else {
        $failCount++
    }
    Start-Sleep -Milliseconds 500  # Brief pause between uploads
}

Write-Host "`nUpload Summary:" -ForegroundColor Cyan
Write-Host "Success: $successCount files" -ForegroundColor Green
Write-Host "Failed:  $failCount files" -ForegroundColor Red

if ($successCount -gt 0) {
    Write-Host "`nForcing application restart..." -ForegroundColor Yellow

    # Touch web.config to restart app
    try {
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

        # Add timestamp to force change
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        $webConfigContent = $webConfigContent -replace '<!--ProjectGuid: 04D14253-3682-4A26-9478-3B79D52339A0-->', "<!--ProjectGuid: 04D14253-3682-4A26-9478-3B79D52339A0--><!-- Filtering Fix Deploy: $timestamp -->"

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
        Write-Host "Application restart triggered!" -ForegroundColor Green
        $response.Close()

    } catch {
        Write-Host "Failed to restart application: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host "`nFilterig fixes deployed! Wait 10-15 seconds, then test company selection." -ForegroundColor Green
}