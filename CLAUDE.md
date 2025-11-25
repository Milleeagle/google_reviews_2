# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 web application called `google_reviews_2` that implements a Google Reviews tracking system. The application provides user authentication, Google Places API integration, and comprehensive review management functionality for companies.

## Development Commands

- **Build the project**: `dotnet build`
- **Run the application**: `dotnet run` (runs on https://localhost:7019 and http://localhost:5008)
- **Restore packages**: `dotnet restore`
- **Run database migrations**: `dotnet ef database update`
- **Create new migration**: `dotnet ef migrations add <MigrationName>`
- **Clean build artifacts**: `dotnet clean`

### Database Management Scripts
- **Setup MySQL database**: `create-mysql-migration.bat` or `create-mysql-migration.ps1`
- **Export MySQL database**: `export-mysql-database.bat` or `export-mysql-database.ps1`
- **Export for hosting**: `export-database-for-hosting.bat` or `export-database-for-hosting.ps1`
- **Switch database provider**: `switch-database-fixed.ps1`
- **Test database connections**: `test-mssql-connection.ps1`

### Deployment & Management Scripts
- **Deploy to FTP**: `deploy-to-ftp.bat` or `deploy-to-ftp.ps1` ⚠️ **NOT WORKING - Use manual deployment**
- **Setup credentials**: `setup-credentials.bat`
- **Enable debug mode**: `enable-debug-mode.ps1`
- **Disable debug mode**: `disable-debug-mode.ps1`
- **Switch database**: `switch-database-fixed.ps1` - Interactive database provider switcher
- **FTP testing**: `test-ftp-connection.ps1`, `test-ftp-login.ps1`, `test-ftp-structure.ps1`

### ⚠️ DEPLOYMENT ISSUE - MANUAL PROCESS REQUIRED
**Problem**: Automated PowerShell FTP deployment scripts don't properly update the live application.
**Solution**: Use manual deployment process:
1. Use Visual Studio "Publish" feature to create publish folder
2. Manually upload publish folder contents via FTP client
3. This ensures all files are uploaded correctly and application restarts

**Status**: Manual deployment is working correctly. Automated PowerShell scripts need debugging.
See `DEPLOYMENT-NOTES.md` for detailed information.

## Architecture

### Core Structure
- **Controllers/**: MVC controllers (HomeController, ReviewsController, AdminController, DiagnosticsController, DriveController, ScheduledMonitorController, ApiController, BatchProgressController)
- **Data/**: Entity Framework DbContext and database migrations
- **Models/**: Domain models (Company, Review, ScheduledReviewMonitor) and view models (CreateUserViewModel, UserViewModel, GooglePlacesModels)
- **Services/**: Business logic services (GooglePlacesService, UserInitializationService, ScheduledMonitorService, EmailService, GoogleDriveService, BatchProgressService)
- **Views/**: Razor views organized by controller
- **Areas/Identity/**: ASP.NET Core Identity scaffolded pages for authentication

### Key Components
- **ApplicationDbContext**: Inherits from IdentityDbContext, manages Companies, Reviews, and ScheduledReviewMonitors entities with optimized indexing
- **GooglePlacesService**: Integrates with Google Places API for fetching review data
- **ScheduledMonitorService**: Handles automated review monitoring and reporting system
- **EmailService**: SMTP-based email service for sending review reports and alerts
- **GoogleDriveService**: Integration with Google Drive and Sheets APIs for data export
- **BatchProgressService**: Real-time progress tracking for batch operations using concurrent dictionary storage
- **ScheduledMonitorBackgroundService**: Background service that processes scheduled monitors automatically
- **ReviewsController**: Main controller for review management with authorization policies
- **UserInitializationService**: Handles role creation and admin user setup

### Domain Models
- **Company**: Represents businesses being tracked (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated)
- **Review**: Individual Google reviews (Id, CompanyId, AuthorName, Rating, Text, Time, AuthorUrl, ProfilePhotoUrl)
- **ScheduledReviewMonitor**: Automated monitoring configuration (Id, Name, EmailRecipients, Frequency, NextRunAt, IsActive)
- **ScheduledMonitorCompany**: Junction table linking monitors to companies
- **ScheduledMonitorExecution**: Execution history and results for scheduled monitors
- Configured with proper Entity Framework relationships and cascade deletion

### Authentication & Authorization
- ASP.NET Core Identity with IdentityUser and IdentityRole
- Role-based authorization with "Admin" and "User" roles
- Email confirmation disabled for development (`RequireConfirmedAccount = false`)
- Authorization policies: "AdminOnly" and "UserOrAdmin"
- Admin-only features for adding companies and refreshing reviews

### Google Services Integration
- **Google Places API**: HttpClient-based service for fetching review data with filtering support (date range, rating filters)
- **Google Drive API**: Service for file management and data export capabilities
- **Google Sheets API**: Integration for exporting review data to Google Sheets
- API connection testing functionality for all services
- OAuth2 authentication for Drive/Sheets services via service account credentials
- Configured through user secrets or appsettings for API keys and credentials

### Database Configuration
- **Current Setup**: SQL Server with Entity Framework Core (Microsoft.EntityFrameworkCore.SqlServer)
- **Local Development**: `Server=(localdb)\\mssqllocaldb;Database=GoogleReviews_Dev;Trusted_Connection=true;MultipleActiveResultSets=true`
- **Production**: `Server=81.95.105.76;Database=e003918;User Id=e003918a;Password=Searchminds123!;TrustServerCertificate=true;ConnectRetryCount=3;`
- **Database Switching**: Use `switch-database-fixed.ps1` to switch between local MySQL, cloud MySQL, or cloud MSSQL
- **MySQL Option Available**: Scripts exist to switch to MySQL/MariaDB using Pomelo.EntityFrameworkCore.MySql (not currently active)
- Optimized indexes on Company.PlaceId (unique), Review.CompanyId, Review.Time, ScheduledReviewMonitor.NextRunAt, and ScheduledMonitorExecution.ExecutedAt
- Cascade delete configured for Company-Review and ScheduledReviewMonitor relationships

### Scheduled Monitoring System
- **Background Service**: Automatically processes scheduled monitors at configured intervals
- **Email Reporting**: Sends HTML-formatted email reports with review summaries and alerts
- **Monitor Configuration**: Supports daily, weekly, and monthly monitoring frequencies
- **Execution Tracking**: Maintains history of all monitor executions with success/failure status

## Project Configuration

- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **User Secrets**: Configured for sensitive data (aspnet-google_reviews-e84ed60c-faea-43f4-9c5f-616e06badc64)
- **Development URLs**: https://localhost:7019, http://localhost:5008
- **Key Dependencies**:
  - Microsoft.EntityFrameworkCore.SqlServer (8.0.10) - Currently active database provider
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore (8.0.10)
  - Microsoft.AspNetCore.Identity.UI (8.0.10)
  - Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore (8.0.10)
  - Microsoft.EntityFrameworkCore.Tools (8.0.10)
  - Google.Apis.Drive.v3 (1.68.0.3421)
  - Google.Apis.Sheets.v4 (1.68.0.3424)
  - Google.Apis.Auth.AspNetCore3 (1.68.0)

## Database Provider Switching

The project supports multiple database providers through switching scripts:

- **Current**: SQL Server (configured in Program.cs with `UseSqlServer`)
- **Available**: MySQL/MariaDB via Pomelo provider (scripts available but requires code changes)
- **Switch Command**: `switch-database-fixed.ps1` - Interactive script to change database connections
- **Connection Strings**: Configured in appsettings.json with DefaultConnection, CloudConnection, and SqlServerConnection options
# important-instruction-reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.