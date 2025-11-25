# Check Views directory structure
Write-Host "Checking Views Directory" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

$directories = @("Views", "Views/Reviews", "wwwroot")

foreach ($dir in $directories) {
    try {
        Write-Host "Checking: $dir" -ForegroundColor Yellow

        $ftpUri = "ftp://$ftpServer/$dir/"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        $stream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)

        Write-Host "Contents of $dir :" -ForegroundColor White
        $content = $reader.ReadToEnd()
        Write-Host $content

        $reader.Close()
        $stream.Close()
        $response.Close()

    } catch {
        Write-Host "$dir not found or access denied: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host "---" -ForegroundColor Gray
}