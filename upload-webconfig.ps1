# Upload web.config to fix static file serving
Write-Host "Uploading updated web.config to fix static file serving" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

# Upload web.config
try {
    $ftpUri = "ftp://$ftpServer/web.config"
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
    $ftpRequest.UseBinary = $true

    $fileBytes = [System.IO.File]::ReadAllBytes('.\web.config')
    $ftpRequest.ContentLength = $fileBytes.Length

    $requestStream = $ftpRequest.GetRequestStream()
    $requestStream.Write($fileBytes, 0, $fileBytes.Length)
    $requestStream.Close()

    $response = $ftpRequest.GetResponse()
    Write-Host "web.config uploaded successfully!" -ForegroundColor Green
    $response.Close()
} catch {
    Write-Host "Failed to upload web.config: $($_.Exception.Message)" -ForegroundColor Red
}