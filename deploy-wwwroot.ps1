# Upload wwwroot static files
Write-Host "Uploading wwwroot static files" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "Server: $ftpServer" -ForegroundColor Green

$publishDir = ".\publish"
$wwwrootDir = Join-Path $publishDir "wwwroot"

if (-not (Test-Path $wwwrootDir)) {
    Write-Host "wwwroot directory not found in publish folder" -ForegroundColor Red
    exit 1
}

# Function to create FTP directory
function Create-FtpDirectory($ftpPath) {
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpPath)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $response = $ftpRequest.GetResponse()
        $response.Close()
        Write-Host "Created directory: $ftpPath" -ForegroundColor Green
    } catch {
        # Directory might already exist, that's OK
    }
}

# Function to upload file
function Upload-File($localPath, $remotePath) {
    try {
        $ftpUri = "ftp://$ftpServer/$remotePath"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $ftpRequest.UseBinary = $true

        $fileBytes = [System.IO.File]::ReadAllBytes($localPath)
        $ftpRequest.ContentLength = $fileBytes.Length

        $requestStream = $ftpRequest.GetRequestStream()
        $requestStream.Write($fileBytes, 0, $fileBytes.Length)
        $requestStream.Close()

        $response = $ftpRequest.GetResponse()
        Write-Host "Uploaded: $remotePath" -ForegroundColor Green
        $response.Close()
        return $true
    } catch {
        Write-Host "Failed to upload $remotePath : $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Create wwwroot directory
Create-FtpDirectory "ftp://$ftpServer/wwwroot"

# Upload all files in wwwroot recursively
$successCount = 0
$failCount = 0

Get-ChildItem -Path $wwwrootDir -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Replace($wwwrootDir, "").TrimStart('\').Replace('\', '/')
    $remotePath = "wwwroot/$relativePath"

    # Create subdirectories if needed
    $remoteDir = Split-Path $remotePath -Parent
    if ($remoteDir -and $remoteDir -ne "wwwroot") {
        Create-FtpDirectory "ftp://$ftpServer/$remoteDir"
    }

    if (Upload-File $_.FullName $remotePath) {
        $successCount++
    } else {
        $failCount++
    }
}

Write-Host ""
Write-Host "Upload Summary:" -ForegroundColor Cyan
Write-Host "Success: $successCount files" -ForegroundColor Green
Write-Host "Failed:  $failCount files" -ForegroundColor Red

if ($successCount -gt 0) {
    Write-Host "wwwroot files uploaded! Your website UI should now work properly." -ForegroundColor Green
}