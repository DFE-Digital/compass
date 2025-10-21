# Compass - Strategic delivery and compliance management platform

Compass is a strategic delivery and compliance management platform for the Department for Education (DfE).

## Overview

Compass provides:
- Strategic oversight and governance
- Compliance tracking and reporting
- Integration with FIPS and other DfE systems
- API-first architecture for internal and external consumption

## Technology stack

- **Framework**: ASP.NET Core 8.0 (C#)
- **Database**: SQLite (development), Azure SQL (test/production)
- **Authentication**: Azure Entra ID (Microsoft Identity Platform)
- **Frontend**: GOV.UK Design System, MoJ Frontend
- **Styling**: SCSS compiled to CSS

## Prerequisites

- .NET 8.0 SDK
- Node.js (for SCSS compilation)
- SQLite (for local development)

## Getting started

### 1. Install dependencies

```bash
# Install .NET dependencies
dotnet restore

# Install Node.js dependencies for SCSS compilation
npm install
```

### 2. Configure application settings

Copy the example appsettings files and configure with your values:

```bash
# Development settings are in appsettings.Development.json
# Update with your Azure AD and API keys
```

### 3. Build CSS from SCSS

```bash
# Build CSS once
npm run build-css

# Or watch for changes during development
npm run watch-css
```

### 4. Run the application

```bash
# Using the startup script
./run-dev.sh

# Or using dotnet CLI
dotnet run
```

The application will be available at `http://localhost:5500`

## Project structure

```
Compass/
├── Controllers/        # MVC and API controllers
├── Data/              # Database context and migrations
├── Models/            # Data models and view models
├── Services/          # Business logic and external API services
├── Helpers/           # Utility and helper classes
├── Views/             # Razor views
├── wwwroot/           # Static files (CSS, JS, images)
│   ├── css/          # Compiled CSS (from SCSS)
│   ├── scss/         # Source SCSS files
│   ├── js/           # JavaScript files
│   └── images/       # Image assets
├── Program.cs         # Application entry point
└── Compass.csproj     # Project file
```

## Configuration

### Environment-specific settings

- **Development**: `appsettings.Development.json` - Local development with SQLite
- **Test**: `appsettings.Test.json` - Test environment with Azure SQL
- **Production**: `appsettings.Production.json` - Production environment with Azure SQL

### Key configuration sections

- **AzureAd**: Azure Entra ID authentication settings
- **CmsApi**: FIPS CMS API connection settings
- **StandardsCmsApi**: DfE Standards CMS API settings
- **ConnectionStrings**: Database connection strings
- **Caching**: Memory cache configuration

## Authentication

The application uses Azure Entra ID for authentication:

- **Development**: Authentication is bypassed using a development authentication handler
- **Test/Production**: Full Azure Entra ID authentication via OpenID Connect

## API

The application provides both:
- **Web UI**: MVC views for user interaction
- **REST API**: API endpoints for external consumption

API endpoints are available at `/api/{controller}/{action}`

Example health check: `GET /api/api/health`

## Development

### Building CSS

The project uses SCSS for styling. CSS files are automatically built before compilation.

```bash
# Build CSS
npm run build-css

# Watch for SCSS changes
npm run watch-css
```

### Database

In development, the application uses SQLite with a local database file (`compass.db`).

The database is automatically created on first run.

## Deployment

### Azure App Service

The application is designed to be deployed to Azure App Service.

Configuration should be set via:
- Azure App Service application settings (environment variables)
- Azure Key Vault for sensitive values

### Database migrations

When deploying to test/production with Azure SQL:

```bash
# Add a migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update
```

## Security

- Content Security Policy (CSP) with nonces
- HSTS enabled
- X-Frame-Options, X-Content-Type-Options headers
- Rate limiting
- Secure session cookies

## Support

For support or questions, contact the FIPS development team.

