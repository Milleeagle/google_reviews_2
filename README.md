# Google Reviews Tracker

ASP.NET Core 8.0 application for tracking and monitoring Google Reviews for multiple companies.

## Quick Start

### Configuration Management

This project uses a split configuration system to keep credentials safe:

- **appsettings.Local.json** - Contains real credentials (NOT in git)
- **appsettings.Development.json** - Safe placeholder values (in git)
- **appsettings.Production.json** - Safe placeholder values (in git)

#### Swap Between Configs

```powershell
# Use local config with real credentials for development
./swap-config.ps1 local

# Use safe config with placeholders (before committing to git)
./swap-config.ps1 safe
```

### Run the Application

```bash
# Make sure you're using local config first
./swap-config.ps1 local

# Run the application
dotnet run
```

Access at: https://localhost:7019

### Default Admin Credentials
- Email: admin@example.com
- Password: AdminPassword123!

## Features

- **Company Management**: Track businesses with Google Place IDs
- **Review Collection**:
  - Google Places API integration
  - Web scraping (Selenium) - free alternative
  - Parallel batch processing (10 companies at once)
- **Excel Import/Export**: Bulk import companies from spreadsheets
- **Review Monitoring**: Automated reports with email alerts
- **Customer Segmentation**: Current vs Potential customers
- **Duplicate Cleanup**: Find and merge duplicate entries

## Tech Stack

- ASP.NET Core 8.0
- Entity Framework Core (SQL Server)
- ASP.NET Core Identity
- Google Places API
- Google Drive/Sheets API
- Selenium WebDriver (ChromeDriver)
- ClosedXML (Excel)
- SMTP Email

## Database

Currently configured for SQL Server. Connection strings in appsettings files.

```bash
# Apply migrations
dotnet ef database update
```

## Deployment

See `MANUAL-DEPLOYMENT-GUIDE.md` for production deployment instructions.

⚠️ **Note**: Automated PowerShell FTP deployment scripts are not working. Use manual FTP upload via Visual Studio Publish feature.

## Security

**IMPORTANT**: Never commit `appsettings.Local.json` to git. It's already in `.gitignore`.

Before committing code:
```powershell
./swap-config.ps1 safe
git add .
git commit -m "your message"
```

After pulling code:
```powershell
./swap-config.ps1 local
```

## Configuration Required

Edit `appsettings.Local.json` with your credentials:

- Google Places API Key
- Google Service Account JSON
- Database connection strings
- SMTP email credentials
- Admin user password

## Scripts

Various PowerShell scripts are available for:
- Database management
- Deployment
- Configuration switching
- Testing connections