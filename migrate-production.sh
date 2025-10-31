#!/bin/bash

# Compass Production Database Migration Script
# This script applies Entity Framework migrations to the Production database

echo "================================================"
echo "Compass Production Database Migration"
echo "================================================"
echo ""
echo "This will apply pending EF Core migrations to the Production database."
echo ""
echo "Target: s186d01-dops-compass.database.windows.net/compass"
echo ""

# Confirm with user
read -p "Do you want to proceed? (yes/no): " -r
echo ""
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]
then
    echo "Migration cancelled."
    exit 1
fi

# Set environment to Production
export ASPNETCORE_ENVIRONMENT=Production

# Check if dotnet ef is available
if ! command -v dotnet ef &> /dev/null
then
    echo "Error: dotnet ef tool is not installed."
    echo "Please install it with: dotnet tool install --global dotnet-ef"
    exit 1
fi

# Run the migration
echo "Applying migrations to Production database..."
echo ""
dotnet ef database update --project Compass.csproj

if [ $? -eq 0 ]
then
    echo ""
    echo "✓ Migrations applied successfully!"
else
    echo ""
    echo "✗ Migration failed. Please check the error messages above."
    exit 1
fi

