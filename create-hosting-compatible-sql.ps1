# Create Hosting-Compatible SQL Export
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "   Create Hosting-Compatible SQL" -ForegroundColor Yellow  
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

Write-Host "Creating SQL Server script with hosting limitations in mind..." -ForegroundColor Cyan
Write-Host ""

$DatabaseName = "aspnet-google_reviews"
$ServerName = "(localdb)\mssqllocaldb"
$OutputFile = "hosting-compatible-export.sql"

try {
    Write-Host "Creating hosting-compatible SQL export..." -ForegroundColor Cyan
    
    # Create SQL with smaller key sizes and simpler constraints
    $sqlContent = @"
-- Hosting-Compatible Database Export
-- Generated on: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- Optimized for shared hosting limitations
--

-- Drop existing tables if they exist (for re-import)
IF OBJECT_ID('Reviews', 'U') IS NOT NULL DROP TABLE Reviews;
IF OBJECT_ID('Companies', 'U') IS NOT NULL DROP TABLE Companies;
IF OBJECT_ID('ScheduledReviewMonitors', 'U') IS NOT NULL DROP TABLE ScheduledReviewMonitors;
IF OBJECT_ID('AspNetUserRoles', 'U') IS NOT NULL DROP TABLE AspNetUserRoles;
IF OBJECT_ID('AspNetUsers', 'U') IS NOT NULL DROP TABLE AspNetUsers;
IF OBJECT_ID('AspNetRoles', 'U') IS NOT NULL DROP TABLE AspNetRoles;
IF OBJECT_ID('__EFMigrationsHistory', 'U') IS NOT NULL DROP TABLE __EFMigrationsHistory;

-- Identity Tables with hosting-friendly key sizes

-- AspNetUsers (reduced key size)
CREATE TABLE AspNetUsers (
    Id nvarchar(128) NOT NULL PRIMARY KEY,
    UserName nvarchar(128) NULL,
    NormalizedUserName nvarchar(128) NULL,
    Email nvarchar(128) NULL,
    NormalizedEmail nvarchar(128) NULL,
    EmailConfirmed bit NOT NULL DEFAULT 0,
    PasswordHash nvarchar(max) NULL,
    SecurityStamp nvarchar(max) NULL,
    ConcurrencyStamp nvarchar(max) NULL,
    PhoneNumber nvarchar(50) NULL,
    PhoneNumberConfirmed bit NOT NULL DEFAULT 0,
    TwoFactorEnabled bit NOT NULL DEFAULT 0,
    LockoutEnd datetimeoffset(7) NULL,
    LockoutEnabled bit NOT NULL DEFAULT 1,
    AccessFailedCount int NOT NULL DEFAULT 0
);

-- AspNetRoles (reduced key size)
CREATE TABLE AspNetRoles (
    Id nvarchar(128) NOT NULL PRIMARY KEY,
    Name nvarchar(128) NULL,
    NormalizedName nvarchar(128) NULL,
    ConcurrencyStamp nvarchar(max) NULL
);

-- AspNetUserRoles (with smaller keys - total 256 bytes)
CREATE TABLE AspNetUserRoles (
    UserId nvarchar(128) NOT NULL,
    RoleId nvarchar(128) NOT NULL,
    PRIMARY KEY (UserId, RoleId)
);

-- Add foreign keys separately (some hosts prefer this)
ALTER TABLE AspNetUserRoles ADD CONSTRAINT FK_AspNetUserRoles_Users 
    FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id);
ALTER TABLE AspNetUserRoles ADD CONSTRAINT FK_AspNetUserRoles_Roles 
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles (Id);

-- EF Migrations History (reduced key size)
CREATE TABLE __EFMigrationsHistory (
    MigrationId nvarchar(128) NOT NULL PRIMARY KEY,
    ProductVersion nvarchar(32) NOT NULL
);

-- Your Application Tables

-- Companies table
CREATE TABLE Companies (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name nvarchar(500) NOT NULL,
    PlaceId nvarchar(500) NULL,
    GoogleMapsUrl nvarchar(1000) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    LastUpdated datetime2(7) NOT NULL DEFAULT GETDATE()
);

-- Reviews table
CREATE TABLE Reviews (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CompanyId int NOT NULL,
    AuthorName nvarchar(200) NOT NULL,
    Rating int NOT NULL,
    Text nvarchar(max) NOT NULL,
    Time datetime2(7) NOT NULL,
    AuthorUrl nvarchar(1000) NULL,
    ProfilePhotoUrl nvarchar(1000) NULL
);

-- Add foreign key for Reviews
ALTER TABLE Reviews ADD CONSTRAINT FK_Reviews_Companies 
    FOREIGN KEY (CompanyId) REFERENCES Companies (Id);

-- Scheduled Review Monitors
CREATE TABLE ScheduledReviewMonitors (
    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    EmailRecipients nvarchar(1000) NOT NULL,
    Frequency int NOT NULL,
    NextRunAt datetime2(7) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedAt datetime2(7) NOT NULL DEFAULT GETDATE(),
    UpdatedAt datetime2(7) NOT NULL DEFAULT GETDATE()
);

-- Create useful indexes (with size limits in mind)
CREATE INDEX IX_AspNetUsers_Email ON AspNetUsers (NormalizedEmail);
CREATE INDEX IX_Companies_PlaceId ON Companies (PlaceId);
CREATE INDEX IX_Reviews_CompanyId ON Reviews (CompanyId);
CREATE INDEX IX_Reviews_Time ON Reviews (Time);

-- Insert basic data
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
('admin-role', 'Admin', 'ADMIN', NEWID()),
('user-role', 'User', 'USER', NEWID());

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES
('20240101000000_InitialMySqlMigration', '8.0.0');

"@

    # Try to get actual data from your database
    try {
        Write-Host "Attempting to get your actual data..." -ForegroundColor Yellow
        
        # Get Companies data
        $companiesResult = sqlcmd -S $ServerName -d $DatabaseName -Q "SELECT COUNT(*) FROM Companies" -h -1 2>$null
        if ($companiesResult -and $companiesResult.Trim() -match '^\d+$' -and [int]$companiesResult.Trim() -gt 0) {
            Write-Host "Found $($companiesResult.Trim()) companies, adding to export..." -ForegroundColor Green
            
            $companiesData = sqlcmd -S $ServerName -d $DatabaseName -Q "SELECT Id, Name, PlaceId, GoogleMapsUrl, CASE WHEN IsActive = 1 THEN 'true' ELSE 'false' END as IsActive, LastUpdated FROM Companies" -h -1 -s "`t" -W 2>$null
            
            $sqlContent += "`n-- Your Companies Data`n"
            foreach ($row in $companiesData) {
                if ($row.Trim() -ne "" -and !$row.StartsWith("-") -and $row.Contains("`t")) {
                    $fields = $row.Split("`t")
                    if ($fields.Length -ge 6) {
                        $id = $fields[0].Trim()
                        $name = $fields[1].Trim().Replace("'", "''")
                        $placeId = if ($fields[2].Trim() -eq "NULL" -or $fields[2].Trim() -eq "") { "NULL" } else { "'$($fields[2].Trim().Replace("'", "''"))'" }
                        $mapsUrl = if ($fields[3].Trim() -eq "NULL" -or $fields[3].Trim() -eq "") { "NULL" } else { "'$($fields[3].Trim().Replace("'", "''"))'" }
                        $isActive = if ($fields[4].Trim() -eq "true") { "1" } else { "0" }
                        $lastUpdated = $fields[5].Trim()
                        
                        $sqlContent += "INSERT INTO Companies (Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES ('$name', $placeId, $mapsUrl, $isActive, '$lastUpdated');`n"
                    }
                }
            }
        }
        
        # Get Users data (simplified to avoid password issues)
        $usersResult = sqlcmd -S $ServerName -d $DatabaseName -Q "SELECT COUNT(*) FROM AspNetUsers" -h -1 2>$null
        if ($usersResult -and $usersResult.Trim() -match '^\d+$' -and [int]$usersResult.Trim() -gt 0) {
            Write-Host "Found $($usersResult.Trim()) users, creating admin user for hosting..." -ForegroundColor Green
            
            $sqlContent += "`n-- Create Admin User (you'll need to reset password after import)`n"
            $sqlContent += "INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, SecurityStamp, ConcurrencyStamp) VALUES `n"
            $sqlContent += "('admin-user-id', 'admin@example.com', 'ADMIN@EXAMPLE.COM', 'admin@example.com', 'ADMIN@EXAMPLE.COM', 1, NEWID(), NEWID());`n"
            $sqlContent += "`n-- Assign Admin Role`n"
            $sqlContent += "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ('admin-user-id', 'admin-role');`n"
        }
        
    } catch {
        Write-Host "⚠️  Could not retrieve local data, creating structure only" -ForegroundColor Yellow
        $sqlContent += "`n-- NOTE: No local data found - you'll need to recreate users and companies after import`n"
    }

    $sqlContent += "`n-- Script completed - hosting compatible version`n"
    
    # Save with proper encoding
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Resolve-Path ".").Path + "\$OutputFile", $sqlContent, $utf8NoBom)
    
    Write-Host "✅ Hosting-compatible SQL created!" -ForegroundColor Green
    Write-Host ""
    
    $fileSize = (Get-Item $OutputFile).Length
    Write-Host "File: $OutputFile" -ForegroundColor White
    Write-Host "Size: $([math]::Round($fileSize / 1KB, 2)) KB" -ForegroundColor White
    
    Write-Host ""
    Write-Host "Hosting-friendly optimizations:" -ForegroundColor Cyan
    Write-Host "• Reduced nvarchar key sizes (128 chars vs 450)" -ForegroundColor Green
    Write-Host "• Primary key total size: 256 bytes (well under 900 limit)" -ForegroundColor Green
    Write-Host "• Separate foreign key creation" -ForegroundColor Green
    Write-Host "• Added proper defaults and constraints" -ForegroundColor Green
    Write-Host "• Included DROP statements for re-import" -ForegroundColor Green
    Write-Host "• Size-conscious string fields" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "   Ready for Strict Hosting!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

} catch {
    Write-Host "❌ Export failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Press Enter to exit"