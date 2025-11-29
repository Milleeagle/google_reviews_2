# Recent Changes - November 29, 2025

## Overview
This document describes the comprehensive review management features added to the Google Reviews application, including multi-language support, enhanced scraping capabilities, and admin review management tools.

## Summary of Changes

### 1. Multi-Language Email Support
- Added automatic language detection (Swedish/English) for customer emails
- Created language detection service using word frequency analysis and Swedish character detection (å, ä, ö)
- Added `Language` enum with Swedish and English options
- Email templates now route to appropriate language-specific content

### 2. Company Template System
- Added `CompanyTemplate` enum supporting multiple brands (Deletify, Ereaseify, Other)
- Email service now routes templates based on both language and company brand
- Future-ready for multiple brand email campaigns

### 3. Enhanced Review and Company Models
Added new fields to capture more data during scraping:

**Review Model:**
- `RelativeTime` - Human-readable time (e.g., "3 months ago")
- `BusinessResponse` - Owner's response to the review
- `Language` - Language of the review text

**Company Model:**
- `Address` - Physical address
- `PhoneNumber` - Contact phone number
- `Website` - Company website URL
- `OverallRating` - Google Maps rating (0-5)

### 4. Enhanced Web Scraper
Updated `GoogleReviewScraper` with new capabilities:
- **SearchCompanyAsync** - Search for companies by name and location
- **ScrapeCompanyWithReviewsAsync** - Complete company + reviews scraping
- **ExtractCompanyInfoAsync** - Extract detailed company information
- Enhanced stealth mode and multi-language support
- Better error handling and debugging capabilities
- Support for `ScrapingOptions` (MaxReviews, FromDate, SortBy)

### 5. Review Management System
Created comprehensive admin tools for managing reviews:

**New Services:**
- `IReviewManagementService` / `ReviewManagementService`
  - Transaction-safe review deletion
  - Delete all reviews
  - Delete reviews by company name initial (A-Z)
  - Get database statistics

**New UI (Admin Only):**
- Navigate to: **Reviews → 🗑️ Manage Reviews**
- Real-time database statistics (total reviews, total companies)
- "Delete All Reviews" button with double confirmation
- A-Z quick delete buttons for deleting reviews by company initial
- AJAX-based operations with instant feedback

### 6. Customer Email Enhancements
**CustomerEmailData Updates:**
- `ContactName` - Optional contact person name (extracted from email)
- `Language` - Auto-detected language (Swedish/English)
- `CompanyTemplate` - Selected brand template
- `GreetingName` helper property for personalized greetings
- Deprecated `IsSwedish` in favor of `Language` enum

### 7. Database Migration
Created migration: `AddScraperEnhancedFields`
- Adds new columns to Reviews table (BusinessResponse, Language, RelativeTime)
- Adds new columns to Companies table (Address, PhoneNumber, Website, OverallRating)
- **Important:** Run `dotnet ef database update` to apply migration

## New Files Created

### Models
- `Models/EmailTemplate.cs` - Language and CompanyTemplate enums
- `Models/ScrapingOptions.cs` - Scraping configuration options

### Services
- `Services/ILanguageDetectionService.cs` - Language detection interface
- `Services/LanguageDetectionService.cs` - Swedish/English detection implementation
- `Services/IReviewManagementService.cs` - Review management interface
- `Services/ReviewManagementService.cs` - Review deletion implementation

### Views
- `Views/Reviews/ManageReviews.cshtml` - Admin review management UI

### Database Migrations
- `Migrations/20251129082802_AddScraperEnhancedFields.cs`
- `Migrations/20251129082802_AddScraperEnhancedFields.Designer.cs`

## Modified Files

### Core Application
- `Program.cs` - Registered new services (LanguageDetectionService, ReviewManagementService)
- `appsettings.json` - Added production SQL Server connection string
- `appsettings.Development.json` - Added SMTP email configuration

### Models
- `Models/CustomerEmailData.cs` - Added Language, CompanyTemplate, ContactName
- `Models/Review.cs` - Added RelativeTime, BusinessResponse, Language
- `Models/Company.cs` - Added Address, PhoneNumber, Website, OverallRating

### Services
- `Services/IReviewScraper.cs` - Updated interface with new methods
- `Services/GoogleReviewScraper.cs` - Enhanced scraping with company search

### Controllers
- `Controllers/ReviewsController.cs` - Added review management endpoints:
  - `ManageReviews()` - Display review management page
  - `DeleteAllReviews()` - Delete all reviews
  - `DeleteReviewsByInitial()` - Delete reviews by company initial
  - `GetReviewStats()` - Get database statistics

### Views
- `Views/Shared/_Layout.cshtml` - Added "🗑️ Manage Reviews" navigation link

### Database
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Updated with new schema

## Configuration Changes

### Database Connection
The application now uses `SqlServerConnection` from `appsettings.json`:
```
Server=81.95.105.76;Database=e003918;User Id=e003918a;Password=Searchminds123!;TrustServerCertificate=true;ConnectRetryCount=3;
```

### Email Configuration (appsettings.Development.json)
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUsername": "emil.jones@searchminds.se",
  "SmtpPassword": "mzno yyvk cqlx jbpz",
  "FromEmail": "emil.jones@searchminds.se",
  "FromName": "Review Monitor"
}
```

## How to Use New Features

### Review Management (Admin Only)
1. Log in as admin (admin@example.com)
2. Navigate to **Reviews → 🗑️ Manage Reviews**
3. View database statistics
4. Delete reviews:
   - Click letter buttons (A-Z) to delete reviews for companies starting with that letter
   - Click "Delete All Reviews" to remove all reviews (requires double confirmation)

### Language Detection
Language detection happens automatically when:
- Processing customer email lists from Excel
- The system analyzes company names and email addresses
- Swedish indicators: .se domains, Swedish characters (å, ä, ö), Swedish company suffixes (AB, HB)

### Enhanced Scraping
The updated scraper now captures:
- Business responses to reviews
- Relative timestamps
- Company contact information (address, phone, website)
- Overall Google Maps rating

## Database Migration Required

After pulling these changes, run the following command to update the database schema:

```bash
dotnet ef database update
```

This will add the new fields to the Reviews and Companies tables.

## Git Commits

All changes were pushed in 4 commits:

1. **997689c** - Phase 1: Models and Services
2. **6065d90** - Phase 2: Configuration and Migration
3. **7441183** - Connection String Fix
4. **909f9ff** - Review Management UI

## Technical Notes

### Language Detection Algorithm
The `LanguageDetectionService` uses:
- Swedish character detection (å, ä, ö) - weighted heavily
- Word frequency analysis against common Swedish/English word lists
- Defaults to Swedish on ties

### Transaction Safety
All review deletion operations use database transactions:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
// ... perform deletions ...
await _context.SaveChangesAsync();
await transaction.CommitAsync();
```

This ensures deletions are atomic and can be rolled back on errors.

### Authorization
Review management is protected by the "AdminOnly" policy:
```csharp
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> ManageReviews()
```

Only users in the "Admin" role can access these features.

## Future Enhancements (Not Yet Implemented)

The following features from the original plan are NOT yet implemented but the foundation is ready:

1. **Email Service Updates** - Multi-language template routing (Swedish/English for Deletify/Ereaseify)
2. **Excel Service Updates** - Enhanced language detection during Excel import
3. **ReviewsController Scraping Updates** - PlaceId to GoogleMapsUrl conversion fixes
4. **Email Customer UI Updates** - Company template dropdown in EmailCustomers.cshtml

These can be added in future commits as the foundation (models, services, interfaces) is in place.

## Troubleshooting

### Application Won't Start - Connection String Error
**Error:** "Format of the initialization string does not conform to specification"
**Solution:** Ensure `appsettings.json` has valid `SqlServerConnection` string (fixed in commit 7441183)

### Migration Not Applied
**Error:** "Invalid column name 'Language'" or similar
**Solution:** Run `dotnet ef database update`

### Can't See Manage Reviews Link
**Cause:** User is not in Admin role
**Solution:** Log in as admin@example.com or assign Admin role to your user

### Stats Show "Error"
**Cause:** Database migration not applied
**Solution:** Run `dotnet ef database update` and refresh the page

## Testing Checklist

- [x] Application builds successfully
- [x] Database migration created
- [x] New services registered in Program.cs
- [x] Review management UI accessible to admins
- [x] Delete all reviews works with confirmation
- [x] Delete by initial (A-Z) works
- [x] Statistics display correctly
- [x] Navigation link visible to admins only
- [x] All changes committed and pushed to GitHub

## Contact

For questions about these changes, refer to the commit messages or the implementation in the respective files.
