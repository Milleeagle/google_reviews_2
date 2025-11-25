# Upload Critical Files
Write-Host "Critical Files Deployment" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "Server: $ftpServer" -ForegroundColor Green

# Critical files
$files = @("google_reviews.dll", "google_reviews.exe")
$publishDir = ".\publish"

foreach ($file in $files) {
    $localPath = Join-Path $publishDir $file

    if (Test-Path $localPath) {
        Write-Host "Uploading: $file" -ForegroundColor Yellow

        try {
            $ftpUri = "ftp://$ftpServer/$file"
            $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
            $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

            $fileBytes = [System.IO.File]::ReadAllBytes($localPath)
            $ftpRequest.ContentLength = $fileBytes.Length

            $requestStream = $ftpRequest.GetRequestStream()
            $requestStream.Write($fileBytes, 0, $fileBytes.Length)
            $requestStream.Close()

            $response = $ftpRequest.GetResponse()
            Write-Host "Success: $file" -ForegroundColor Green
            $response.Close()
        }
        catch {
            Write-Host "Failed: $file - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

Write-Host "Upload completed!" -ForegroundColor Cyan