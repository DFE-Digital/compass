# COMPASS

**Compliance, Outcomes, Performance, Assurance, Standards and Strategy**

Strategic delivery and compliance management platform for the Department for Education (DfE).

## Overview

COMPASS is an enterprise-grade reporting and compliance platform that unifies reporting of performance, milestones, risks, issues, governance, and assurance across DfE products and services. It provides a central hub for product governance, performance tracking, and strategic oversight.

## Key Features

### Work Reporting
- **Project Management**: Track projects, deliverables, and milestones
- **Monthly Updates**: Submit and manage monthly status reports with deadlines
- **RAID Management**: Manage Risks, Actions, Issues, and Decisions
- **Performance Metrics**: Track and report on performance indicators
- **RAG Status**: Monitor project health with Red, Amber, Green indicators

### Standards & Compliance
- **Service Standards**: Track compliance with GOV.UK Service Standards (1-14)
- **Technology Code of Practice**: Manage adherence to TCoP principles
- **DDT Standards**: Digital, Data and Technology standards tracking
- **Functional Standards**: Cross-government standards compliance
- **Accessibility**: Manage accessibility statements and compliance

### Enterprise Reporting
- **Central Operations Dashboard**: Aggregated views of all projects and services
- **Monthly Summaries**: Period-based reporting and completion tracking
- **Performance Reporting**: Service-level performance metrics and KPIs
- **Analytics & Insights**: Dashboards and data exports (CSV, Excel)

### User Management
- **Entra ID Integration**: Single sign-on with Azure Active Directory
- **Role-Based Access Control (RBAC)**: Granular permissions and feature access
- **Group Management**: Organize users into groups with shared permissions
- **Audit Logging**: Complete audit trail of all system changes

## Technology Stack

- **Framework**: .NET 8.0 (ASP.NET Core MVC)
- **Database**: SQL Server (Azure SQL) / SQLite (development)
- **ORM**: Entity Framework Core 9.0
- **Authentication**: Microsoft Identity Web (Entra ID/Azure AD)
- **Frontend**: 
  - Razor Views with GOV.UK Design System components
  - SCSS compiled with Sass
  - JavaScript (vanilla JS with some jQuery)
  - Chart.js for data visualization
- **APIs**: RESTful API with versioned endpoints (`/api/v1/`)
- **Logging**: Serilog with file logging
- **Notifications**: GOV.UK Notify integration
- **Caching**: In-memory caching for performance

## Prerequisites

- **.NET SDK**: 8.0.400 or later (see `global.json`)
- **Node.js**: 16+ (for SCSS compilation)
- **SQL Server**: Azure SQL Server or local SQL Server instance
- **Azure AD/Entra ID**: App registration with appropriate permissions

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd compass
```

### 2. Configure Application Settings

Copy the base configuration and update for your environment:

```bash
# Development
cp appsettings.Development.json appsettings.Development.local.json

# Update connection strings and Azure AD settings
```

Key configuration sections:

- **ConnectionStrings**: Database connection string
- **AzureAd**: Entra ID authentication settings
- **CmsApi**: CMS API endpoints and keys
- **FeatureFlags**: Enable/disable features

### 3. Install Dependencies

```bash
# .NET dependencies (restored automatically on build)
dotnet restore

# Node.js dependencies for SCSS compilation
npm install
```

### 4. Build CSS

The project requires SCSS to be compiled to CSS before running:

```bash
npm run build-css
```

Or watch for changes during development:

```bash
npm run watch-css
```

### 5. Run Database Migrations

The application automatically applies migrations on startup, but you can also run them manually:

```bash
dotnet ef database update
```

### 6. Seed Initial Data (Optional)

The application seeds initial data on first run:
- Service Standards (GOV.UK Service Standards 1-14)
- DDaT Professions
- Technology Code of Practice
- Statement Templates
- RBAC groups and features

### 7. Run the Application

```bash
dotnet run
```

The application will start on `http://localhost:5500` in development mode.

## Project Structure

```
compass/
├── Controllers/          # MVC controllers
│   ├── Api/             # API controllers (versioned)
│   └── Admin/           # Admin area controllers
├── Models/              # Entity models and data models
├── Views/               # Razor views
│   ├── Project/         # Project management views
│   ├── CentralOps/      # Central Operations dashboards
│   └── Documentation/   # Help and documentation
├── Services/            # Business logic services
├── Data/                # Database context and migrations
├── Middlewares/         # HTTP middleware components
├── Helpers/             # Utility helper classes
├── ViewModels/          # View model classes
├── Attributes/          # Custom authorization attributes
├── wwwroot/             # Static files (CSS, JS, images)
│   ├── scss/            # Source SCSS files
│   └── css/             # Compiled CSS (generated)
├── Migrations/          # Entity Framework migrations
└── Properties/          # Launch settings and assembly info
```

## Command-Line Utilities

COMPASS includes several command-line utilities for database operations:

### Database Cleanup
```bash
dotnet run -- --clean-database
```

### Data Migration (SQLite → SQL Server)
```bash
dotnet run -- --migrate-data
```

### Seed from SQLite
```bash
dotnet run -- --seed-from-sqlite --environment Development
```

### Seed GDD Framework
```bash
dotnet run -- --seed-gdd-framework --environment Development --csv-file path/to/file.csv
```

### Query GDD Roles
```bash
dotnet run -- --query-gdd-roles --environment Development
```

### Query Skills
```bash
dotnet run -- --query-skills --environment Development
```

### Environment Migration (Azure SQL → Azure SQL)
```bash
dotnet run -- --migrate-sql --source Development --target Production
```

## Development

### CSS Development

SCSS files are in `wwwroot/scss/` and compiled to `wwwroot/css/`. The build process automatically compiles CSS before building the application.

Watch mode for CSS development:
```bash
npm run watch-css
```

### Database Migrations

Create a new migration:
```bash
dotnet ef migrations add MigrationName
```

Apply migrations:
```bash
dotnet ef database update
```

### Running Tests

*Note: Add test project information here when available*

### API Documentation

API endpoints are available at `/api/v1/`. Key API controllers:

- `/api/v1/Surveys` - User satisfaction surveys
- `/api/v1/Projects` - Project data
- `/api/v1/Accessibility` - Accessibility statements
- `/api/v1/PerformanceMetrics` - Performance reporting
- `/api/v1/Chatbot` - Chatbot integration

API authentication is handled via bearer tokens (see `ApiAuthenticationMiddleware`).

## Configuration

### Environment Variables

Set `ASPNETCORE_ENVIRONMENT` to:
- `Development` - Local development (SQLite, relaxed security)
- `Test` - Test environment (Azure SQL)
- `Production` - Production environment (Azure SQL, full security)

### Feature Flags

Control feature availability via `appsettings.json`:

```json
{
  "FeatureFlags": {
    "EnableDemandManagement": false,
    "EnableRAIDManagement": false,
    "EnableCentralOps": false
  }
}
```

### Authentication

COMPASS uses Entra ID (Azure AD) for authentication. Configure in `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "education.gov.uk",
    "TenantId": "",
    "ClientId": "",
    "CallbackPath": "/signin-oidc"
  }
}
```

## Deployment

### Build for Production

```bash
dotnet publish -c Release -o ./publish
```

### Azure App Service

The application is configured for Azure App Service deployment with:
- Azure SQL Database
- Application Insights integration
- Managed Identity for Azure resources

### Environment-Specific Settings

- `appsettings.Development.json` - Development settings
- `appsettings.Test.json` - Test environment
- `appsettings.Production.json` - Production settings

## Key Concepts

### Monthly Updates

Projects submit monthly status reports by the 5th working day of the following month. The system automatically calculates due dates based on working days (excluding UK bank holidays).

### Reporting Periods

The system displays the previous month's update until within 10 days of the current month's due date, then switches to the current period.

### RAG Status

Projects and milestones use Red, Amber, Green (RAG) status indicators:
- **Green**: On track, no issues
- **Amber**: Some concerns, monitoring required
- **Amber-Green**: Mostly on track with minor issues
- **Amber-Red**: Significant concerns
- **Red**: Critical issues, action required

### Permissions

COMPASS uses a role-based access control (RBAC) system:
- **Groups**: Collections of users with shared permissions
- **Features**: Functional areas of the application
- **Permissions**: Read, Write, Delete, Admin per feature

## Contributing

1. Create a feature branch from `main`
2. Make your changes
3. Ensure CSS is compiled (`npm run build-css`)
4. Run database migrations if needed
5. Submit a pull request

## Documentation

- User documentation is available in the application at `/Documentation`
- API documentation: See `/api/v1/` endpoints
- Code documentation: XML comments in source code

## Support

For issues, questions, or contributions, please contact the development team or create an issue in the repository.

## License

*Add license information here*

## Acknowledgments

- GOV.UK Design System components
- Ministry of Justice Frontend components
- DfE Digital Operations team

