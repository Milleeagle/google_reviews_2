# FTP Connection Diagnostic Script
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   FTP Connection Diagnostic Tool" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

try {
    # Load credentials
    $creds = Get-Content 'credentials.json' | ConvertFrom-Json
    $ftpServer = $creds.FTP.Server
    $ftpUsername = $creds.FTP.Username
    $ftpPassword = $creds.FTP.Password
    
    Write-Host "Testing FTP Connection..." -ForegroundColor Cyan
    Write-Host "Server: $ftpServer" -ForegroundColor White
    Write-Host "Username: $ftpUsername" -ForegroundColor White
    Write-Host "Password: [HIDDEN]" -ForegroundColor White
    Write-Host ""

    # Test 1: Basic FTP connection with different methods
    Write-Host "Test 1: Basic FTP Connection" -ForegroundColor Yellow
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/")
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.KeepAlive = $false
        $ftpRequest.UseBinary = $true
        
        $response = $ftpRequest.GetResponse()
        $streamReader = New-Object System.IO.StreamReader($response.GetResponseStream())
        $directories = $streamReader.ReadToEnd()
        $streamReader.Close()
        $response.Close()
        
        Write-Host "✅ FTP Connection Successful!" -ForegroundColor Green
        Write-Host "Directory listing:" -ForegroundColor Green
        Write-Host $directories
    }
    catch {
        Write-Host "❌ Basic FTP failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 2: Try with passive mode disabled
    Write-Host ""
    Write-Host "Test 2: FTP with UsePassive = False" -ForegroundColor Yellow
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/")
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.UsePassive = $false
        $ftpRequest.KeepAlive = $false
        
        $response = $ftpRequest.GetResponse()
        Write-Host "✅ FTP with UsePassive=False works!" -ForegroundColor Green
        $response.Close()
    }
    catch {
        Write-Host "❌ UsePassive=False failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 3: Try with different port
    Write-Host ""
    Write-Host "Test 3: FTP on port 21 (explicit)" -ForegroundColor Yellow
    try {
        $ftpRequest = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer`:21/")
        $ftpRequest.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $ftpRequest.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory
        $ftpRequest.KeepAlive = $false
        
        $response = $ftpRequest.GetResponse()
        Write-Host "✅ FTP on port 21 works!" -ForegroundColor Green
        $response.Close()
    }
    catch {
        Write-Host "❌ Port 21 failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    # Test 4: Test with Windows FTP command
    Write-Host ""
    Write-Host "Test 4: Windows FTP Command Line Test" -ForegroundColor Yellow
    
    # Create FTP script
    @"
open $ftpServer
$ftpUsername
$ftpPassword
ls
quit
"@ | Out-File -FilePath "test_ftp.txt" -Encoding ASCII

    try {
        $ftpOutput = & ftp -s:test_ftp.txt 2>&1
        Write-Host "FTP Command Output:" -ForegroundColor White
        Write-Host $ftpOutput -ForegroundColor Gray
        
        if ($ftpOutput -match "230.*logged") {
            Write-Host "✅ Windows FTP command works!" -ForegroundColor Green
        }
        else {
            Write-Host "❌ Windows FTP command failed" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Windows FTP test failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    finally {
        if (Test-Path "test_ftp.txt") { Remove-Item "test_ftp.txt" }
    }

    # Test 5: Ping test
    Write-Host ""
    Write-Host "Test 5: Server Connectivity (Ping)" -ForegroundColor Yellow
    try {
        $ping = Test-Connection -ComputerName $ftpServer -Count 2 -Quiet
        if ($ping) {
            Write-Host "✅ Server is reachable via ping" -ForegroundColor Green
        }
        else {
            Write-Host "❌ Server is not reachable via ping" -ForegroundColor Red
        }
    }
    catch {
        Write-Host "❌ Ping test failed: $($_.Exception.Message)" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Diagnostic Complete!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Recommendations:" -ForegroundColor Cyan
    Write-Host "1. Check if server requires FTPS (secure FTP)" -ForegroundColor White
    Write-Host "2. Verify username/password are correct" -ForegroundColor White  
    Write-Host "3. Check if passive/active mode matters" -ForegroundColor White
    Write-Host "4. Try using FileZilla or WinSCP manually first" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Diagnostic failed: $($_.Exception.Message)" -ForegroundColor Red
}

Read-Host "Press Enter to exit"