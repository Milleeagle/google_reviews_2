-- Hosting-Compatible Database Export
-- Generated on: 2025-09-12 18:57:43
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

-- NOTE: No local data found - you'll need to recreate users and companies after import

-- Script completed - hosting compatible version
