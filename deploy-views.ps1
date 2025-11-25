# Upload Views with new email functionality
Write-Host "Uploading View Files with Email Functionality" -ForegroundColor Cyan

# Load credentials
$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

Write-Host "Server: $ftpServer" -ForegroundColor Green

# Upload the critical view file that contains new email functionality
$viewFile = "Views/Reviews/ReviewMonitor.cshtml"
$localViewPath = $viewFile
$remoteViewPath = "Views/Reviews/ReviewMonitor.cshtml"

if (Test-Path $localViewPath) {
    Write-Host "Uploading: $viewFile" -ForegroundColor Yellow

    try {
        # Create directory structure if needed
        $ftpDirUri = "ftp://$ftpServer/Views/Reviews"
        try {
            $dirRequest = [System.Net.FtpWebRequest]::Create($ftpDirUri)
            $dirRequest.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
            $dirRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
            $dirResponse = $dirRequest.GetResponse()
            $dirResponse.Close()
        } catch {
            # Directory might already exist, ignore error
        }

        # Upload file
        $ftpUri = "ftp://$ftpServer/$remoteViewPath"
        $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)

        $fileBytes = [System.IO.File]::ReadAllBytes($localViewPath)
        $ftpRequest.ContentLength = $fileBytes.Length

        $requestStream = $ftpRequest.GetRequestStream()
        $requestStream.Write($fileBytes, 0, $fileBytes.Length)
        $requestStream.Close()

        $response = $ftpRequest.GetResponse()
        Write-Host "Success: $viewFile" -ForegroundColor Green
        $response.Close()
    }
    catch {
        Write-Host "Failed: $viewFile - $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "File not found: $viewFile" -ForegroundColor Red
}

Write-Host "View upload completed!" -ForegroundColor Cyan