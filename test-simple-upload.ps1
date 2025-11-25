# Test simple file upload
Write-Host "Testing simple file upload" -ForegroundColor Cyan

$creds = Get-Content 'credentials.json' | ConvertFrom-Json
$ftpServer = $creds.FTP.Server
$ftpUsername = $creds.FTP.Username
$ftpPassword = $creds.FTP.Password

# Create a simple test file
$testContent = "TEST FILE - $(Get-Date)"
$testFile = "test-deploy.txt"
Set-Content -Path $testFile -Value $testContent

Write-Host "Created test file: $testFile"

# Try uploading to root
try {
    $ftpUri = "ftp://$ftpServer/$testFile"
    Write-Host "Uploading to: $ftpUri"

    $ftpRequest = [System.Net.FtpWebRequest]::Create($ftpUri)
    $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
    $ftpRequest.UseBinary = $false

    $fileBytes = [System.IO.File]::ReadAllText($testFile)
    $ftpRequest.ContentLength = $fileBytes.Length

    $requestStream = $ftpRequest.GetRequestStream()
    $writer = New-Object System.IO.StreamWriter($requestStream)
    $writer.Write($fileBytes)
    $writer.Close()

    $response = $ftpRequest.GetResponse()
    Write-Host "SUCCESS: Test file uploaded!" -ForegroundColor Green
    $response.Close()

} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
}

# Clean up
Remove-Item $testFile -Force