# Azure SQL migration plan

## Current situation
- All existing migrations (31 files) are SQLite-specific
- Model snapshot has SQLite column types
- Need to transition to Azure SQL for both dev and production

## Plan

### 1. Update configuration files

**appsettings.Development.json** - Point to `compass-dev` on Azure SQL:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:s186d01-dops-compass.database.windows.net,1433;Initial Catalog=compass-dev;...",
  "CompassDb": "Server=tcp:s186d01-dops-compass.database.windows.net,1433;Initial Catalog=compass-dev;..."
}
```

**appsettings.Production.json** - Already configured for `compass`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:s186d01-dops-compass.database.windows.net,1433;Initial Catalog=compass;...",
  "CompassDb": "Server=tcp:s186d01-dops-compass.database.windows.net,1433;Initial Catalog=compass;..."
}
```

### 2. Update Program.cs

Remove the environment-based switching. Always use SQL Server:
```csharp
// Configure database - Use SQL Server for all environments
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CompassDbContext>(options =>
    options.UseSqlServer(connectionString));
```

### 3. Clean migrations

1. Delete all existing SQLite migrations (backup first)
2. Create fresh SQL Server migration: `dotnet ef migrations add InitialAzureSql`
3. This will generate proper SQL Server syntax

### 4. Apply to databases

```bash
# Development
export ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update

# Production  
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
```

### 5. Seed data from SQLite

Create a one-time data seeding script that reads from local `compass.db` and writes to Azure SQL.
This will seed:
- Risk tiers
- Risk types
- Action sources
- Performance metrics
- Functional standards
- Any other reference/config data

### 6. Remove SQLite dependencies

Once everything is working:
- Remove `Microsoft.EntityFrameworkCore.Sqlite` package
- Delete `compass.db` file
- Update `.gitignore` to remove SQLite references

## Benefits

- Consistent database engine across all environments
- Proper SQL Server data types and constraints
- Better performance and Azure integration
- No more environment switching logic

