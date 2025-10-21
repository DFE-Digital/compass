#!/bin/bash

# Compass Database Migration Script
# This script migrates data from local SQLite to Azure SQL Production database

echo "================================================"
echo "Compass Production Database Migration"
echo "================================================"
echo ""
echo "This will migrate data from your local SQLite database (compass.db)"
echo "to the Azure SQL Production database."
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

# Run the migration
echo "Starting migration..."
echo ""
dotnet run --migrate-data

echo ""
echo "Migration complete!"

