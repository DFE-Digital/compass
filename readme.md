# FIPS Reporting Platform

A comprehensive .NET Core web application for government digital service reporting and milestone tracking.

## Features

- **Entra ID Authentication**: Secure single sign-on with Microsoft Entra ID
- **Role-Based Access Control**: Admin and reporting user roles with appropriate permissions
- **CMS Integration**: Real-time integration with FIPS CMS API for product data
- **Metrics Management**: Configurable reporting metrics with conditional applicability
- **Milestone Tracking**: Comprehensive milestone management with RAG status and updates
- **Product Allocation**: Manual product allocation to reporting users
- **Data Reporting**: Periodic and ad-hoc reporting capabilities

## Quick Start

### Prerequisites
- .NET 8.0 SDK
- Node.js and npm
- SQLite (development) or Azure SQL Database (production)

### Development Setup
1. Clone the repository
2. Install dependencies: `npm install`
3. Build CSS: `npm run build-css`
4. Configure `appsettings.json` with your settings
5. **Apply database migrations**: `dotnet ef database update`
6. Run the application: `dotnet run`

The application will be available at:
- HTTP: http://localhost:5500

### Database Setup

The application uses Entity Framework Core migrations for database management:

- **Development**: SQLite database (`reporting.db`)
- **Production**: Azure SQL Database
- **Migrations**: Managed via EF Core migrations (see [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md))

**Important**: Always run `dotnet ef database update` after pulling changes that include new migrations.

### Configuration
- Update `CmsApi:BaseUrl` and `CmsApi:ReadApiKey` for CMS integration
- Configure `AzureAd` settings for Entra ID authentication
- Set appropriate connection strings for your environment

## Architecture

Built with:
- ASP.NET Core MVC
- Entity Framework Core
- Microsoft Entra ID
- Bootstrap 5
- SCSS styling

## Documentation

Comprehensive documentation is available in the `/documentation` folder, including:
- Platform overview and architecture
- API integration details
- Security features
- Deployment guidelines
- User guides

## Support

For questions or issues, contact the FIPS development team.
