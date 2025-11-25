# Upload only critical static files
Write-Host "Uploading critical static files" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

# Function to upload a single file
function Upload-File($localFile, $remotePath) {
    if (Test-Path $localFile) {
        try {
            $ftpUri = "ftp://$ftpServer/$remotePath"
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
            Write-Host "Uploaded: $remotePath" -ForegroundColor Green
            $response.Close()
            return $true
        } catch {
            Write-Host "Failed: $remotePath - $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "Not found: $localFile" -ForegroundColor Yellow
        return $false
    }
}

# Function to create directory
function Create-Dir($dirPath) {
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$dirPath")
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $response = $ftpRequest.GetResponse()
        $response.Close()
    } catch {
        # Directory might exist
    }
}

$publishPath = ".\publish\wwwroot"

# Create directories
Create-Dir "wwwroot"
Create-Dir "wwwroot/css"
Create-Dir "wwwroot/js"
Create-Dir "wwwroot/lib"
Create-Dir "wwwroot/lib/bootstrap"
Create-Dir "wwwroot/lib/bootstrap/dist"
Create-Dir "wwwroot/lib/bootstrap/dist/css"
Create-Dir "wwwroot/lib/bootstrap/dist/js"
Create-Dir "wwwroot/lib/jquery"
Create-Dir "wwwroot/lib/jquery/dist"

# Upload critical files
$files = @(
    @{ Local = "$publishPath\css\site.css"; Remote = "wwwroot/css/site.css" },
    @{ Local = "$publishPath\js\site.js"; Remote = "wwwroot/js/site.js" },
    @{ Local = "$publishPath\google_reviews.styles.css"; Remote = "wwwroot/google_reviews.styles.css" },
    @{ Local = "$publishPath\lib\bootstrap\dist\css\bootstrap.min.css"; Remote = "wwwroot/lib/bootstrap/dist/css/bootstrap.min.css" },
    @{ Local = "$publishPath\lib\bootstrap\dist\js\bootstrap.bundle.min.js"; Remote = "wwwroot/lib/bootstrap/dist/js/bootstrap.bundle.min.js" },
    @{ Local = "$publishPath\lib\jquery\dist\jquery.min.js"; Remote = "wwwroot/lib/jquery/dist/jquery.min.js" },
    @{ Local = "$publishPath\favicon.ico"; Remote = "wwwroot/favicon.ico" }
)

$successCount = 0
$failCount = 0

foreach ($file in $files) {
    if (Upload-File $file.Local $file.Remote) {
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
    Write-Host "Critical static files uploaded! Try refreshing your website." -ForegroundColor Green
}