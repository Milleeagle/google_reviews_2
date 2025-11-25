-- Simple Database Export for Web Hosting
-- Generated on: 2025-09-12 18:51:41
-- Database: aspnet-google_reviews
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

-- Your Companies Data
INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES (410d4cd6-0201-4dc1-8171-af7ef400287d, 'Krögers', 'ChIJZfgXlGTRV0YRUIVdUWnXc1M', 'NULL', 1, '2025-08-29 12:25:43.6916154');
INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES (916fdb13-78ea-4b63-a6d4-79b8797fa05f, 'Familjeterapeuterna Syd AB', 'ChIJ4WAYH4y9U0YRXe06sXvxMGo', 'https://www.google.com/maps/place/Familjeterapeuterna+Syd+AB/@57.701789,12.745419,739562m/data=!3m1!1e3!4m7!3m6!1s0x4653bd8c1f1860e1:0x6a30f17bb13aed5d!8m2!3d55.6071144!4d12.9992093!15sChNmYW1pbGpldGVyYXBldXRlcm5hkgEPcHN5Y2hvdGhlcmFwaXN04AEA!16s%2Fg%2F1yh9', 1, '2025-09-05 14:02:27.2266918');
INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES (9fa9ceb4-f6f4-4db0-807e-3645421f1e99, 'Familjeterapeuterna Syd Södermalm Sthlm', '0x465f9dc1dcaa0959', 'https://www.google.com/maps/place/Familjeterapeuterna+Syd+S%C3%B6dermalm+Stockholm/@57.701789,12.745419,887082m/data=!3m1!1e3!4m6!3m5!1s0x465f9dc1dcaa0959:0x4ea0cba4f202b6b0!8m2!3d59.3101675!4d18.0816643!16s%2Fg%2F11h_q_fnl4?entry=ttu&g_ep=EgoyMDI1MDgyNS4w', 1, '2025-09-05 13:40:20.8282934');
INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES (bc386aab-736f-4376-a3d7-6efef0cc196d, 'Searchminds', 'ChIJnytgooPRV0YRTCdMAyVMLt4', 'NULL', 1, '2025-08-29 08:25:28.4751350');
INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES (bf4590c1-9d25-4c22-b1be-62d119be16a7, 'Familjeterapeuterna syd uppsala', 'NULL', 'https://www.google.com/maps/place/Familjeterapeuterna+Syd+Uppsala/@57.701789,12.745419,887082m/data=!3m1!1e3!4m6!3m5!1s0x465fcbf4645ebf1b:0x6e46d4af10277fbe!8m2!3d59.8544159!4d17.6521015!16s%2Fg%2F11sghl559j?entry=ttu&g_ep=EgoyMDI1MDgyNS4wIKXMDSoASAFQAw%3D', 1, '2025-09-05 13:45:58.1484705');
INSERT INTO Companies (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated) VALUES (d278a99f-8369-445f-805b-cd3ba5e2c637, 'familjeterapeuterna syd lund', 'ChIJKdrs-aKXU0YRPgCk6_o75X0', 'https://www.google.com/maps/place/Familjeterapeuterna+Syd+Lund/@57.701789,12.745419,887082m/data=!3m1!1e3!4m6!3m5!1s0x465397a2f9ecda29:0x7de53bfaeba4003e!8m2!3d55.7014365!4d13.2002702!16s%2Fg%2F11pdpvp7h4?entry=ttu&g_ep=EgoyMDI1MDgyNS4wIKXMDSoASAFQAw%3D%3D', 1, '2025-09-05 14:04:29.2806771');

-- Your Users Data
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES ('00088137-3d25-4beb-b56b-847b4dc780ec', 'admin@example.com', 'ADMIN@EXAMPLE.COM', 'admin@example.com', 'ADMIN@EXAMPLE.COM', 1, 'AQAAAAIAAYagAAAAENx+pd6nCN1Gv4lZGqDQ3OzUEgqaYrKCpGkBUWjx1dNTR5aTqeemT3lfl5S6nq3BrQ==', NEWID(), NEWID(), NULL, 0, 0, 1, 0);

-- Script completed successfully
