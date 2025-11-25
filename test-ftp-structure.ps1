# Test FTP Structure and Permissions
Write-Host "Testing FTP Connection and Structure" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "Server: $ftpServer" -ForegroundColor Green

try {
    # Test basic connection by listing directory
    $ftpUri = "ftp://$ftpServer/"
    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

    $response = $ftpRequest.GetResponse()
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)

    Write-Host "Directory listing:" -ForegroundColor Yellow
    $content = $reader.ReadToEnd()
    Write-Host $content

    $reader.Close()
    $stream.Close()
    $response.Close()

    Write-Host "FTP connection successful!" -ForegroundColor Green
} catch {
    Write-Host "FTP connection failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Try listing specific files that we know should exist
$testFiles = @("google_reviews.exe", "appsettings.json", "web.config")

foreach ($file in $testFiles) {
    try {
        $ftpUri = "ftp://$ftpServer/$file"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        $size = $response.ContentLength
        Write-Host "Found: $file ($size bytes)" -ForegroundColor Green
        $response.Close()
    } catch {
        Write-Host "Not found or locked: $file" -ForegroundColor Red
    }
}