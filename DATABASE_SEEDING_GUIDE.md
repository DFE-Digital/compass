# Database seeding guide

## Overview

The Compass application now uses Azure SQL for both development and production environments. This guide explains how to seed reference data from your local SQLite database into Azure SQL.

## What gets seeded

The seeding process migrates the following reference data:

### Always seeded (if not already present):
1. **Risk Tiers** (5 records)
   - Tier 1-4 + Not Tiered
   - Critical priority ordering

2. **Risk Types** (13 records)
   - Technical, Security, Compliance, Resource, Financial, etc.
   - Standard risk categorisation

### Migrated from SQLite (if source database provided):
3. **Enterprise Metrics** (2 records from your SQLite)
4. **Functional Standards** (1 record)
   - Functional Standard Themes (4 records)
   - Practice Areas (7 records)
   - Criteria (78 records)
5. **Objectives** (20 records from your SQLite)

## Database configuration

### Development: `compass-dev`
```
Server: s186d01-dops-compass.database.windows.net
Database: compass-dev
```

### Production: `compass`
```
Server: s186d01-dops-compass.database.windows.net
Database: compass
```

## How to seed

### Option 1: Using helper scripts (recommended)

**Seed Development:**
```bash
./seed-development.sh
```

**Seed Production:**
```bash
./seed-production.sh
```

### Option 2: Direct command

**Development:**
```bash
dotnet run --seed-from-sqlite --environment Development
```

**Production:**
```bash
dotnet run --seed-from-sqlite --environment Production
```

## What happens during seeding

1. **Connects to SQLite** (`compass.db`) as source
2. **Connects to Azure SQL** (compass-dev or compass) as target
3. **Checks for existing data** - won't duplicate if data already exists
4. **Migrates data** in dependency order:
   - Risk Tiers
   - Risk Types
   - Enterprise Metrics
   - Functional Standards → Themes → Practice Areas → Criteria
   - Objectives

## First-time setup workflow

### 1. Apply migrations to create schema

**Development:**
```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update
```

**Production:**
```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
```

### 2. Seed reference data

**Development:**
```bash
./seed-development.sh
```

**Production:**
```bash
./seed-production.sh
```

### 3. Verify

Connect to Azure SQL and verify the data:
```sql
SELECT COUNT(*) FROM RiskTiers;
SELECT COUNT(*) FROM RiskTypes;
SELECT COUNT(*) FROM FunctionalStandards;
SELECT COUNT(*) FROM Objectives;
```

## Files created

- `Data/CompassDbSeeder.cs` - Main seeding logic
- `SeedFromSQLite.cs` - Command-line utility
- `seed-development.sh` - Development seeding script
- `seed-production.sh` - Production seeding script

## Notes

- Seeding is **idempotent** - it won't duplicate data if run multiple times
- Risk Tiers and Risk Types have default values if SQLite source isn't available
- Functional Standards and Objectives are only seeded from SQLite
- The SQLite database (`compass.db`) is kept as backup until you're confident with Azure SQL

## Troubleshooting

**"No source database provided"**
- Check that `compass.db` exists in the project root
- Verify `CompassDb_SQLite_Backup` connection string in appsettings

**"Failed to connect to Azure SQL"**
- Verify your IP is in the Azure SQL firewall rules
- Check connection strings in appsettings.Development.json / appsettings.Production.json

**"Data already exists, skipping seed"**
- This is normal - seeding won't duplicate data
- To re-seed, you'd need to manually delete the data first

