# Automated FTP Deployment Script
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Automated FTP Deployment" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$ftpServer = "oloflarssonnymans.nu"
$ftpUsername = "s018430b"
$ftpPassword = "T9v!pQ7mZ2#rL4xA"
$publishPath = ".\publish"

# Function to get FTP file listing
function Get-FtpListing {
    param($path = "")
    try {
        $request = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$path")
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::ListDirectory

        $response = $request.GetResponse()
        $stream = $response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $files = @()
        while (($line = $reader.ReadLine()) -ne $null) {
            $files += $line
        }
        $reader.Close()
        $response.Close()
        return $files
    } catch {
        Write-Host "Error listing directory $path : $_" -ForegroundColor Red
        return @()
    }
}

# Function to delete FTP file
function Delete-FtpFile {
    param($path)
    try {
        $request = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$path")
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::DeleteFile

        $response = $request.GetResponse()
        $response.Close()
        Write-Host "  ✓ Deleted file: $path" -ForegroundColor Green
        return $true
    } catch {
        # Silently ignore errors (might be a directory)
        return $false
    }
}

# Function to delete FTP directory (recursively)
function Delete-FtpDirectory {
    param($path)

    # First, get all items in the directory
    $items = Get-FtpListing $path

    foreach ($item in $items) {
        $fullPath = if ($path) { "$path/$item" } else { $item }

        # Try to delete as file first
        if (-not (Delete-FtpFile $fullPath)) {
            # If it fails, it might be a directory, recurse
            Delete-FtpDirectory $fullPath
        }
    }

    # Now try to remove the directory itself
    try {
        $request = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$path")
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::RemoveDirectory

        $response = $request.GetResponse()
        $response.Close()
        Write-Host "  ✓ Deleted directory: $path" -ForegroundColor Green
    } catch {
        # Silently ignore errors
    }
}

# Function to create FTP directory
function Create-FtpDirectory {
    param($path)
    try {
        $request = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$path")
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory

        $response = $request.GetResponse()
        $response.Close()
        return $true
    } catch {
        return $false
    }
}

# Function to upload file
function Upload-FtpFile {
    param($localPath, $remotePath)

    try {
        $request = [System.Net.FtpWebRequest]::Create("ftp://$ftpServer/$remotePath")
        $request.Credentials = New-Object System.Net.NetworkCredential($ftpUsername, $ftpPassword)
        $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
        $request.UseBinary = $true
        $request.KeepAlive = $false

        $fileContent = [System.IO.File]::ReadAllBytes($localPath)
        $request.ContentLength = $fileContent.Length

        $requestStream = $request.GetRequestStream()
        $requestStream.Write($fileContent, 0, $fileContent.Length)
        $requestStream.Close()

        $response = $request.GetResponse()
        $response.Close()

        return $true
    } catch {
        Write-Host "  ✗ Failed to upload $remotePath : $_" -ForegroundColor Red
        return $false
    }
}

# Step 1: Delete all existing files on FTP
Write-Host "Step 1: Cleaning FTP server..." -ForegroundColor Cyan
Write-Host "Getting list of files to delete..." -ForegroundColor Yellow

$rootFiles = Get-FtpListing ""
$deletedCount = 0

foreach ($item in $rootFiles) {
    if ($item -and $item -ne "." -and $item -ne "..") {
        Write-Host "Processing: $item" -ForegroundColor Yellow

        # Try to delete as file
        if (Delete-FtpFile $item) {
            $deletedCount++
        } else {
            # It's a directory, delete recursively
            Delete-FtpDirectory $item
            $deletedCount++
        }
    }
}

Write-Host "Deleted $deletedCount items from FTP server" -ForegroundColor Green
Write-Host ""

# Step 2: Upload new files
Write-Host "Step 2: Uploading new files from $publishPath..." -ForegroundColor Cyan

if (-not (Test-Path $publishPath)) {
    Write-Host "ERROR: Publish path $publishPath does not exist!" -ForegroundColor Red
    exit 1
}

$files = Get-ChildItem -Path $publishPath -Recurse -File
$totalFiles = $files.Count
$uploadedFiles = 0
$failedFiles = 0

Write-Host "Found $totalFiles files to upload" -ForegroundColor Yellow
Write-Host ""

# Keep track of directories we've created
$createdDirs = @{}

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring((Resolve-Path $publishPath).Path.Length + 1)
    $remotePath = $relativePath.Replace('\', '/')

    # Create parent directories if needed
    $parentDir = Split-Path $remotePath -Parent
    if ($parentDir) {
        $dirs = $parentDir.Split('/')
        $currentPath = ""
        foreach ($dir in $dirs) {
            $currentPath = if ($currentPath) { "$currentPath/$dir" } else { $dir }
            if (-not $createdDirs.ContainsKey($currentPath)) {
                Create-FtpDirectory $currentPath | Out-Null
                $createdDirs[$currentPath] = $true
            }
        }
    }

    # Upload file
    Write-Host "Uploading: $remotePath" -ForegroundColor Gray
    if (Upload-FtpFile $file.FullName $remotePath) {
        $uploadedFiles++
    } else {
        $failedFiles++
    }

    # Show progress every 10 files
    if (($uploadedFiles + $failedFiles) % 10 -eq 0) {
        $progress = [math]::Round(($uploadedFiles + $failedFiles) / $totalFiles * 100, 1)
        Write-Host "  Progress: $progress% ($($uploadedFiles + $failedFiles)/$totalFiles)" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "Deployment Summary:" -ForegroundColor Yellow
Write-Host "Total files: $totalFiles" -ForegroundColor White
Write-Host "Successfully uploaded: $uploadedFiles" -ForegroundColor Green
Write-Host "Failed uploads: $failedFiles" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Yellow

if ($failedFiles -eq 0) {
    Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️  Deployment completed with $failedFiles errors." -ForegroundColor Yellow
}

Write-Host ""
