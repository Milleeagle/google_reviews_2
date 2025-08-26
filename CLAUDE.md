# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is an ASP.NET Core 8.0 web application called `google_reviews` that provides user authentication and basic web functionality. The application uses Entity Framework Core with SQL Server for data persistence and ASP.NET Core Identity for user management.

## Development Commands

- **Build the project**: `dotnet build`
- **Run the application**: `dotnet run` (runs on https://localhost:7019 and http://localhost:5008)
- **Restore packages**: `dotnet restore`
- **Run database migrations**: `dotnet ef database update`
- **Create new migration**: `dotnet ef migrations add <MigrationName>`
- **Clean build artifacts**: `dotnet clean`

## Architecture

### Core Structure
- **Controllers/**: MVC controllers handling HTTP requests (currently only HomeController)
- **Data/**: Entity Framework DbContext and database migrations
- **Models/**: Data models and view models
- **Views/**: Razor views organized by controller (Home/, Shared/)
- **Areas/Identity/**: ASP.NET Core Identity scaffolded pages for authentication

### Key Components
- **ApplicationDbContext**: Inherits from IdentityDbContext, provides database access
- **Program.cs**: Application startup configuration using minimal hosting model
- **Identity Integration**: User registration/login requires email confirmation (`RequireConfirmedAccount = true`)

### Database Configuration
- Uses SQL Server with Entity Framework Core
- Connection string configured in appsettings.json under "DefaultConnection"
- Includes ASP.NET Core Identity schema for user management

### Authentication & Authorization
- ASP.NET Core Identity with IdentityUser
- Email confirmation required for new accounts
- User secrets configured for sensitive data (UserSecretsId in .csproj)

## Project Configuration

- **Target Framework**: .NET 8.0
- **Nullable Reference Types**: Enabled
- **Implicit Usings**: Enabled
- **Development Environment**: Configured in launchSettings.json
- **Static Files**: Uses standard .NET 8.0 static file handling (`UseStaticFiles()`)