# Google Reviews Project Setup Instructions

## Quick Setup for Development

### 1. Clone the Repository
```bash
git clone https://github.com/Milleeagle/google_reviews_2.git
cd google_reviews_2
```

### 2. Apply Credentials
- Copy the `credentials.json` file to the project root directory
- Run the setup script: `setup-credentials.bat`
- Or manually copy the values from `credentials.json` to `appsettings.Development.json`

### 3. Setup Database
```bash
dotnet restore
dotnet ef database update
```

### 4. Run the Application
```bash
dotnet run
```

The application will be available at:
- HTTPS: https://localhost:7019
- HTTP: http://localhost:5008

## Default Login
- **Username**: admin@example.com
- **Password**: AdminPassword123!

## Features Available

### ✅ Review Management
- Import companies from Google Sheets
- Extract Place IDs from Google Maps URLs
- Fetch and display Google Reviews
- Manual review monitoring and reporting

### ✅ Scheduled Monitoring (NEW!)
- **Daily/Weekly/Monthly** scheduled review checks
- **Email alerts** with beautiful HTML reports
- **Configurable filters** by star rating and time period
- **Smart company selection** (all or specific companies)
- **Background processing** service runs every 5 minutes
- **Execution history** tracking with delivery status

## Configuration Details

### Email Settings (Gmail)
The project is configured for Gmail SMTP with:
- **Host**: smtp.gmail.com
- **Port**: 587
- **Username**: emil.jones@searchminds.se
- **App Password**: (16-character Gmail app password)

### Google APIs
- **Places API**: For fetching business reviews
- **Drive API**: For accessing shared Google Sheets
- **Sheets API**: For parsing company data

## Scheduled Monitoring System

### How to Use:
1. Go to **"Scheduled Monitors"** in the admin menu
2. Click **"Create Scheduled Monitor"**
3. Configure:
   - **Schedule**: Daily at 9:00 AM, Weekly on Mondays, etc.
   - **Companies**: All companies or specific selection
   - **Criteria**: 3 stars and below, last 7 days
   - **Email**: Where to send reports
4. **Save** and the system will automatically monitor and send email reports

### Email Reports Include:
- 📊 Executive summary with statistics
- ⚠️ Companies requiring attention
- 📝 Specific bad reviews with details
- 🎯 Recommended actions to take

### Background Service:
- Runs every **5 minutes** checking for due monitors
- Sends **beautiful HTML emails** with detailed reports
- Tracks **execution history** and email delivery status
- Handles errors gracefully and continues running

## Development Notes
- The `credentials.json` file contains all real API keys and passwords
- This file is **never committed** to Git (included in .gitignore)
- GitHub repository uses placeholder values for security
- Use the setup script to quickly restore credentials on new machines

## Architecture
- **ASP.NET Core 8.0** with Entity Framework Core
- **SQL Server** database with Identity authentication
- **Background services** for scheduled processing
- **Bootstrap 5** responsive UI
- **Google APIs** integration for reviews and sheets
- **SMTP email** service with HTML templates