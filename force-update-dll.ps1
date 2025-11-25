# Force update the DLL by deleting and re-uploading
Write-Host "Force Updating google_reviews.dll" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

$dllFile = "google_reviews.dll"
$localDll = ".\publish\$dllFile"

if (Test-Path $localDll) {
    # Step 1: Try to delete the old DLL
    try {
        Write-Host "Attempting to delete old DLL..." -ForegroundColor Yellow

        $ftpUri = "ftp://$ftpServer/$dllFile"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $response = $ftpRequest.GetResponse()
        Write-Host "Old DLL deleted successfully!" -ForegroundColor Green
        $response.Close()

    } catch {
        Write-Host "Could not delete old DLL (might be locked): $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "Trying direct overwrite..." -ForegroundColor Yellow
    }

    # Step 2: Upload new DLL
    try {
        Write-Host "Uploading new DLL..." -ForegroundColor Yellow

        $ftpUri = "ftp://$ftpServer/$dllFile"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $fileBytes = [System.IO.File]::ReadAllBytes($localDll)
        $ftpRequest.ContentLength = $fileBytes.Length

        $requestStream = $ftpRequest.GetRequestStream()
        $requestStream.Write($fileBytes, 0, $fileBytes.Length)
        $requestStream.Close()

        $response = $ftpRequest.GetResponse()
        Write-Host "SUCCESS: New DLL uploaded!" -ForegroundColor Green
        Write-Host "Your email functionality should now be available!" -ForegroundColor Green
        $response.Close()

    } catch {
        Write-Host "Upload failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "The application may still be running and locking the DLL file." -ForegroundColor Yellow
    }

} else {
    Write-Host "Local DLL not found: $localDll" -ForegroundColor Red
}

Write-Host "Update attempt completed!" -ForegroundColor Cyan