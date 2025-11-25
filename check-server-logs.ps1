# Check server logs for errors
Write-Host "Checking server logs for login errors" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

# Function to download and display log file
function Get-LogFile($logPath) {
    try {
        $ftpUri = "ftp://$ftpServer/$logPath"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        $responseStream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseStream)
        $logContent = $reader.ReadToEnd()
        $reader.Close()
        $response.Close()

        Write-Host "=== $logPath ===" -ForegroundColor Yellow
        # Show last 50 lines
        $lines = $logContent -split "`n"
        $lastLines = $lines | Select-Object -Last 50
        $lastLines | ForEach-Object { Write-Host $_ }

        return $true
    } catch {
        Write-Host "Could not access $logPath : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Check various log locations
$logPaths = @(
    "logs/stdout",
    "stdout.log",
    "stderr.log",
    "app.log"
)

foreach ($logPath in $logPaths) {
    Get-LogFile $logPath
    Write-Host ""
}