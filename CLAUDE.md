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

## Architecture

### Core Structure
- **Controllers/**: MVC controllers (HomeController, ReviewsController, AdminController, DiagnosticsController)
- **Data/**: Entity Framework DbContext and database migrations
- **Models/**: Domain models (Company, Review) and view models (CreateUserViewModel, UserViewModel, GooglePlacesModels)
- **Services/**: Business logic services (GooglePlacesService, UserInitializationService)
- **Views/**: Razor views organized by controller
- **Areas/Identity/**: ASP.NET Core Identity scaffolded pages for authentication

### Key Components
- **ApplicationDbContext**: Inherits from IdentityDbContext, manages Companies and Reviews entities with optimized indexing
- **GooglePlacesService**: Integrates with Google Places API for fetching review data
- **ReviewsController**: Main controller for review management with authorization policies
- **UserInitializationService**: Handles role creation and admin user setup

### Domain Models
- **Company**: Represents businesses being tracked (Id, Name, PlaceId, GoogleMapsUrl, IsActive, LastUpdated)
- **Review**: Individual Google reviews (Id, CompanyId, AuthorName, Rating, Text, Time, AuthorUrl, ProfilePhotoUrl)
- Configured with proper Entity Framework relationships and cascade deletion

### Authentication & Authorization
- ASP.NET Core Identity with IdentityUser and IdentityRole
- Role-based authorization with "Admin" and "User" roles
- Email confirmation disabled for development (`RequireConfirmedAccount = false`)
- Authorization policies: "AdminOnly" and "UserOrAdmin"
- Admin-only features for adding companies and refreshing reviews

### Google Places Integration
- HttpClient-based service for Google Places API
- Supports filtered review retrieval (date range, rating filters)
- API connection testing functionality
- Configured through user secrets or appsettings for API key storage

### Database Configuration
- SQL Server with Entity Framework Core
- Connection string: `Server=(localdb)\\mssqllocaldb;Database=aspnet-google_reviews`
- Optimized indexes on Company.PlaceId (unique), Review.CompanyId, and Review.Time
- Cascade delete configured for Company-Review relationship

## Project Configuration

- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **User Secrets**: Configured for sensitive data (aspnet-google_reviews-e84ed60c-faea-43f4-9c5f-616e06badc64)
- **Development URLs**: https://localhost:7019, http://localhost:5008