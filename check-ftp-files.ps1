# Check what files are actually on the FTP server
Write-Host "Checking FTP server contents..." -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "FTP Server: $ftpServer" -ForegroundColor Yellow
Write-Host "Username: $ftpUsername" -ForegroundColor Yellow

# Function to list FTP directory
function List-FtpDirectory($path = "") {
    try {
        $ftpUri = "ftp://$ftpServer/$path"
        Write-Host "Listing: $ftpUri" -ForegroundColor Cyan

        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectoryDetails
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        $responseStream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseStream)
        $listing = $reader.ReadToEnd()
        $reader.Close()
        $response.Close()

        Write-Host "=== FTP Directory Listing: $path ===" -ForegroundColor Green
        $listing -split "`n" | ForEach-Object {
            if ($_.Trim() -ne "") {
                Write-Host $_.Trim()
            }
        }
        Write-Host "=================================" -ForegroundColor Green
        Write-Host ""

        return $true
    } catch {
        Write-Host "Failed to list $path : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to check if file exists and get details
function Check-FtpFile($filename) {
    try {
        $ftpUri = "ftp://$ftpServer/$filename"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::GetFileSize
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        $size = $response.ContentLength
        $response.Close()

        # Get timestamp
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::GetDateTimestamp
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        $timestamp = $response.LastModified
        $response.Close()

        Write-Host "✓ $filename - Size: $size bytes, Modified: $timestamp" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "✗ $filename - Not found or error: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Check root directory
List-FtpDirectory ""

# Check for specific critical files
Write-Host "Checking critical files:" -ForegroundColor Yellow
$criticalFiles = @(
    "web.config",
    "google_reviews.dll",
    "google_reviews.exe",
    "appsettings.Production.json"
)

foreach ($file in $criticalFiles) {
    Check-FtpFile $file
}

# Check if there are subdirectories we should be aware of
Write-Host "`nTrying common subdirectories:" -ForegroundColor Yellow
$subdirs = @("wwwroot", "bin", "public_html", "www")
foreach ($subdir in $subdirs) {
    if (List-FtpDirectory $subdir) {
        Write-Host "Found subdirectory: $subdir" -ForegroundColor Green
    }
}