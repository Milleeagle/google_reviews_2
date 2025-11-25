# Create Simple SQL Export (Manual Approach)
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Create Simple SQL Export" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Instead of cleaning complex SSMS scripts, let's create a simple export" -ForegroundColor Cyan
Write-Host "that focuses only on the essential data for hosting." -ForegroundColor Cyan
Write-Host ""

$DatabaseName = "aspnet-google_reviews"
$ServerName = "(localdb)\mssqllocaldb"
$OutputFile = "simple-database-export.sql"

Write-Host "Database: $DatabaseName" -ForegroundColor White
Write-Host "Server: $ServerName" -ForegroundColor White
Write-Host "Output: $OutputFile" -ForegroundColor White
Write-Host ""

try {
    Write-Host "Creating simple, hosting-compatible SQL export..." -ForegroundColor Cyan
    
    # Create a simple header
    $sqlContent = @"
-- Simple Database Export for Web Hosting
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- Database: $DatabaseName
-- 
-- This script contains only essential tables and data
-- Compatible with most SQL Server hosting providers
--

-- Essential Identity Tables (simplified)

-- AspNetUsers table
CREATE TABLE AspNetUsers (
    Id nvarchar(450) NOT NULL PRIMARY KEY,
    UserName nvarchar(256) NULL,
    NormalizedUserName nvarchar(256) NULL,
    Email nvarchar(256) NULL,
    NormalizedEmail nvarchar(256) NULL,
    EmailConfirmed bit NOT NULL,
    PasswordHash nvarchar(max) NULL,
    SecurityStamp nvarchar(max) NULL,
    ConcurrencyStamp nvarchar(max) NULL,
    PhoneNumber nvarchar(max) NULL,
    PhoneNumberConfirmed bit NOT NULL,
    TwoFactorEnabled bit NOT NULL,
    LockoutEnd datetimeoffset(7) NULL,
    LockoutEnabled bit NOT NULL,
    AccessFailedCount int NOT NULL
);

-- AspNetRoles table
CREATE TABLE AspNetRoles (
    Id nvarchar(450) NOT NULL PRIMARY KEY,
    Name nvarchar(256) NULL,
    NormalizedName nvarchar(256) NULL,
    ConcurrencyStamp nvarchar(max) NULL
);

-- AspNetUserRoles table
CREATE TABLE AspNetUserRoles (
    UserId nvarchar(450) NOT NULL,
    RoleId nvarchar(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id) ON DELETE CASCADE
);

-- EF Migrations History
CREATE TABLE __EFMigrationsHistory (
    MigrationId nvarchar(150) NOT NULL PRIMARY KEY,
    ProductVersion nvarchar(32) NOT NULL
);

-- Your Application Tables

-- Companies table
CREATE TABLE Companies (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name nvarchar(max) NOT NULL,
    PlaceId nvarchar(max) NULL,
    GoogleMapsUrl nvarchar(max) NULL,
    IsActive bit NOT NULL,
    LastUpdated datetime2(7) NOT NULL
);

-- Reviews table
CREATE TABLE Reviews (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CompanyId int NOT NULL,
    AuthorName nvarchar(max) NOT NULL,
    Rating int NOT NULL,
    Text nvarchar(max) NOT NULL,
    Time datetime2(7) NOT NULL,
    AuthorUrl nvarchar(max) NULL,
    ProfilePhotoUrl nvarchar(max) NULL,
    CONSTRAINT FK_Reviews_Companies FOREIGN KEY (CompanyId) REFERENCES Companies (Id) ON DELETE CASCADE
);

-- Scheduled Review Monitors (if exists)
CREATE TABLE ScheduledReviewMonitors (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name nvarchar(max) NOT NULL,
    EmailRecipients nvarchar(max) NOT NULL,
    Frequency int NOT NULL,
    NextRunAt datetime2(7) NOT NULL,
    IsActive bit NOT NULL,
    CreatedAt datetime2(7) NOT NULL,
    UpdatedAt datetime2(7) NOT NULL
);

-- Insert basic roles
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
('admin-role-id', 'Admin', 'ADMIN', NEWID()),
('user-role-id', 'User', 'USER', NEWID());

-- Insert EF Migration record (adjust as needed)
INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES
('20240101000000_Initial', '8.0.0');

"@

    Write-Host "Attempting to get actual data from your database..." -ForegroundColor Yellow
    
    # Try to get actual data from companies table
    try {
        Write-Host "Getting Companies data..." -ForegroundColor Gray
        $companiesData = sqlcmd -S $ServerName -d $DatabaseName -Q "SELECT Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated FROM Companies" -h -1 -s "|" -W 2>$null
        
        if ($companiesData) {
            $sqlContent += "`n-- Your Companies Data`n"
            foreach ($row in $companiesData) {
                if ($row.Trim() -ne "" -and !$row.StartsWith("-")) {
                    $fields = $row.Split("|")
                    if ($fields.Length -ge 6) {
                        $id = $fields[0].Trim()
                        $name = $fields[1].Trim().Replace("'", "''")
                        $placeId = $fields[2].Trim().Replace("'", "''")
                        $mapsUrl = $fields[3].Trim().Replace("'", "''")
                        $isActive = if ($fields[4].Trim() -eq "1") { "1" } else { "0" }
                        $lastUpdated = $fields[5].Trim()
                        
                        $sqlContent += "INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES ($id, '$name', '$placeId', '$mapsUrl', $isActive, '$lastUpdated');`n"
                    }
                }
            }
        }
        
        Write-Host "Getting Users data..." -ForegroundColor Gray
        $usersData = sqlcmd -S $ServerName -d $DatabaseName -Q "SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash FROM AspNetUsers" -h -1 -s "|" -W 2>$null
        
        if ($usersData) {
            $sqlContent += "`n-- Your Users Data`n"
            foreach ($row in $usersData) {
                if ($row.Trim() -ne "" -and !$row.StartsWith("-")) {
                    $fields = $row.Split("|")
                    if ($fields.Length -ge 7) {
                        $id = $fields[0].Trim().Replace("'", "''")
                        $username = $fields[1].Trim().Replace("'", "''")
                        $normalizedUsername = $fields[2].Trim().Replace("'", "''")
                        $email = $fields[3].Trim().Replace("'", "''")
                        $normalizedEmail = $fields[4].Trim().Replace("'", "''")
                        $emailConfirmed = if ($fields[5].Trim() -eq "1") { "1" } else { "0" }
                        $passwordHash = $fields[6].Trim().Replace("'", "''")
                        
                        $sqlContent += "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES ('$id', '$username', '$normalizedUsername', '$email', '$normalizedEmail', $emailConfirmed, '$passwordHash', NEWID(), NEWID(), NULL, 0, 0, 1, 0);`n"
                    }
                }
            }
        }
        
    } catch {
        Write-Host "⚠️  Could not retrieve data from local database - creating structure only" -ForegroundColor Yellow
    }

    $sqlContent += "`n-- Script completed successfully`n"
    
    # Save the file
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Resolve-Path ".").Path + "\$OutputFile", $sqlContent, $utf8NoBom)
    
    Write-Host "✅ Simple SQL export created!" -ForegroundColor Green
    Write-Host ""
    
    $fileSize = (Get-Item $OutputFile).Length
    Write-Host "File: $OutputFile" -ForegroundColor White
    Write-Host "Size: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor White
    
    Write-Host ""
    Write-Host "This simple script includes:" -ForegroundColor Cyan
    Write-Host "• Essential Identity tables" -ForegroundColor White
    Write-Host "• Your application tables (Companies, Reviews)" -ForegroundColor White
    Write-Host "• Basic roles (Admin, User)" -ForegroundColor White
    Write-Host "• Your actual data (if found)" -ForegroundColor White
    Write-Host "• No complex SQL Server features" -ForegroundColor White
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Ready for Hosting Import!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "This simple script should work with any SQL Server host." -ForegroundColor Cyan
    
} catch {
    Write-Host "❌ Export failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"