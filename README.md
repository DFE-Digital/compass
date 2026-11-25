# COMPASS

**Compliance, Outcomes, Performance, Assurance, Standards and Strategy**

Strategic delivery and compliance platform for the Department for Education (DfE). COMPASS is the central hub for product governance, work reporting, performance, RAID, standards compliance, and related operational reporting.

## What it does

- **Work reporting** — projects, milestones, monthly updates, RAG status
- **RAID** — risks, actions, issues, and decisions
- **Performance** — service-level metrics and reporting
- **Standards & compliance** — GOV.UK Service Standards, TCoP, accessibility, and related tracking
- **Central operations** — aggregated dashboards, exports, and oversight views
- **Access control** — Entra ID sign-in with RBAC groups and features

## Stack

| Layer | Choice |
| --- | --- |
| Runtime | .NET 10 (`net10.0`, see `global.json`) |
| Web | ASP.NET Core MVC (Razor) |
| Data | Entity Framework Core 10, Azure SQL (SQL Server) |
| Auth | Microsoft Identity Web / Entra ID |
| UI | GOV.UK Design System, DfE Frontend, SCSS → CSS via Sass |
| Observability | Serilog (file), Application Insights |
| Notifications | GOV.UK Notify |

## Branches and environments

| Branch | Environment | Deploy trigger |
| --- | --- | --- |
| `Development` | Development Azure Web App | automatic on push to `Development` |
| `Test` | Test Azure Web App | automatic on push to `Test` |
| `main` | Production Azure Web App | automatic on push to `main` |

Workflows live under `.github/workflows/`.

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (SDK `10.0.100` or compatible; `global.json` uses `latestFeature` roll-forward)
- [Node.js 20+](https://nodejs.org/) (SCSS build)
- Access to an Azure SQL database (or a local SQL Server) and an Entra ID app registration for sign-in

## Getting started

```bash
git clone https://github.com/DFE-Digital/compass.git
cd compass
dotnet restore
npm install
npm run build-css
```

### Configuration

1. Copy `appsettings.Template.json` and use it as the basis for local overrides (do not commit secrets).
2. Set at least:
   - `ConnectionStrings:DefaultConnection` — SQL Server connection string
   - `AzureAd` / `Entra` — tenant, client id, and secrets for sign-in
3. Optional integrations (CMS, FIPS/CMDB sync, Notify, Application Insights, feature flags) are documented in the template under the matching sections.

For **Development** and **Test** environment configuration files (connection strings, Entra app settings, and related secrets), contact **Andy Jones** ([andy.jones@education.gov.uk](mailto:andy.jones@education.gov.uk)).

Environment is selected via `ASPNETCORE_ENVIRONMENT` (`Development`, `Test`, `Production`). Launch profiles are in `Properties/launchSettings.json`.

### Run

```bash
dotnet run --launch-profile Development
```

App URL (default): [http://localhost:5500](http://localhost:5500)

Migrations to the database are applied on startup. To run them manually:

```bash
dotnet ef database update
```

### CSS during development

```bash
npm run watch-css
```

`npm run build-css` also runs automatically before `dotnet build`.

### Tests

```bash
dotnet test Tests/Compass.Tests.csproj
```

## Repository layout

```
Controllers/     MVC and API controllers
Services/        Business logic
Models/          Domain / EF entities
Views/           Razor views (including Modern UI)
ViewModels/      View models
Data/            DbContext and seed data
Migrations/      EF Core migrations
wwwroot/         Static assets; SCSS in wwwroot/scss
Tests/           xUnit test project
documentation/   Deeper design and integration notes
Scripts/         Operational / one-off scripts
.github/         CI/CD workflows
```

## Further documentation

More detailed notes live in [`documentation/`](documentation/), including FIPS sync, RAID, planning/demand, and performance reporting. In-app help is also available at `/Documentation` when the app is running.

## License

Internal DfE Digital use unless otherwise stated.
