# Deploy Critical Files - Upload only the essential files that failed
param()

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Critical Files Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Load FTP credentials
try {
    $creds = Get-Content 'credentials.json' | ConvertFrom-Json
    $ftpServer = $creds.FTP.Server
    $ftpUsername = $creds.FTP.Username
    $ftpPassword = $creds.FTP.Password

    Write-Host "FTP Server: " -NoNewline
    Write-Host $ftpServer -ForegroundColor Green
    Write-Host "Username: " -NoNewline
    Write-Host $ftpUsername -ForegroundColor Green
} catch {
    Write-Host "Error reading credentials.json: " -ForegroundColor Red -NoNewline
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Uploading critical files that contain your new email functionality..." -ForegroundColor Yellow

# Define critical files that likely failed in the previous deployment
$criticalFiles = @(
    "google_reviews.dll",           # Your main application code with email fixes!
    "google_reviews.exe",           # Application executable
    "google_reviews.pdb"            # Debug symbols
)

$publishDir = ".\publish"
$successCount = 0
$failCount = 0

foreach ($file in $criticalFiles) {
    $localPath = Join-Path $publishDir $file

    if (Test-Path $localPath) {
        try {
            Write-Host "Uploading: " -NoNewline
            Write-Host $file -ForegroundColor Yellow -NoNewline

            # Create FTP request
            $ftpUri = "ftp://$ftpServer/$file"
            $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
            $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
            $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
            $ftpRequest.UsePassive = $true
            $ftpRequest.UseBinary = $true

            # Read local file and upload
            $fileBytes = [System.IO.File]::ReadAllBytes($localPath)
            $ftpRequest.ContentLength = $fileBytes.Length

            $requestStream = $ftpRequest.GetRequestStream()
            $requestStream.Write($fileBytes, 0, $fileBytes.Length)
            $requestStream.Close()

            $response = $ftpRequest.GetResponse()
            Write-Host " ✓" -ForegroundColor Green
            $response.Close()
            $successCount++

        } catch {
            Write-Host " ✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
            $failCount++
        }
    } else {
        Write-Host "File not found: " -NoNewline -ForegroundColor Red
        Write-Host $file -ForegroundColor Red
        $failCount++
    }
}

Write-Host ""
Write-Host "Upload Summary:" -ForegroundColor Cyan
Write-Host "✓ Success: $successCount files" -ForegroundColor Green
Write-Host "✗ Failed:  $failCount files" -ForegroundColor Red

if ($successCount -gt 0) {
    Write-Host ""
    Write-Host "Critical files uploaded! Your email functionality should now be available." -ForegroundColor Green
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Restart your web application if possible" -ForegroundColor White
    Write-Host "   2. Test the email functionality on your production site" -ForegroundColor White
    Write-Host "   3. Check the browser console for any JavaScript errors" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "All uploads failed. The production app may be running and locking these files." -ForegroundColor Red
    Write-Host "Try stopping the application first, then re-running this script." -ForegroundColor Yellow
}