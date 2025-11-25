# Manual FTP Deployment Guide

Since the automated FTP script is having connection issues, here's how to deploy manually:

## Option 1: Using FileZilla (Recommended)

### Download and Install FileZilla
1. Download FileZilla Client from: https://filezilla-project.org/download.php?type=client
2. Install and run FileZilla

### Connect to Your Server
1. **Host**: `oloflarssonnymans.nu`
2. **Username**: `s018337b`
3. **Password**: `)%N6OY6m6cU"`
4. **Port**: `21` (default FTP port)
5. Click **Quickconnect**

### Upload Your Files
1. **Build your application first**: Run `dotnet publish --configuration Release --output "./publish"`
2. **Navigate** to your project folder in the left panel (Local site)
3. **Go to** the `publish` folder
4. **Select all files** in the publish folder (Ctrl+A)
5. **Drag and drop** or **right-click → Upload** to transfer to the server
6. **Also upload**: `web.config` from your project root

## Option 2: Using WinSCP

### Download and Install WinSCP
1. Download from: https://winscp.net/eng/download.php
2. Install and run WinSCP

### Connect to Your Server
1. **File protocol**: FTP
2. **Host name**: `oloflarssonnymans.nu`
3. **User name**: `s018337b`
4. **Password**: `)%N6OY6m6cU"`
5. **Port number**: `21`
6. Click **Login**

### Upload Your Files
1. **Local directory**: Navigate to your `publish` folder
2. **Remote directory**: Usually `/` or `/public_html` or `/wwwroot`
3. **Select all files** in publish folder
4. **Press F5** or **drag and drop** to upload
5. **Upload web.config** as well

## Option 3: Using Built-in Windows FTP

### Create FTP Script
1. Run the simple deployment: `deploy-to-ftp-simple.bat`
2. This uses Windows built-in FTP client

## Files to Upload

Make sure these key files are uploaded:
- `google_reviews.dll` (main application)
- `google_reviews.exe`
- `web.config` (IIS configuration)
- `appsettings.json` and `appsettings.Production.json`
- All other DLL files from the publish folder
- `wwwroot` folder (static files, CSS, JS)

## Troubleshooting

### If FTP connection fails:
1. **Check credentials** - verify username/password
2. **Try FTPS** - some servers require secure FTP
3. **Check firewall** - your network might block FTP
4. **Contact hosting provider** - they may have specific requirements

### If website doesn't load after upload:
1. **Check web.config** is uploaded
2. **Verify file permissions** on server
3. **Check IIS/server logs** for errors
4. **Ensure .NET runtime** is installed on server

## Production Configuration

After uploading, you may need to:
1. **Update connection strings** in appsettings.Production.json
2. **Configure database** on the production server
3. **Set environment variables** for production
4. **Configure SSL** if using HTTPS

## Testing the Deployment

After upload, test your website at:
`http://oloflarssonnymans.nu`

Default login:
- **Username**: admin@example.com  
- **Password**: AdminPassword123!