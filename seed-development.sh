#!/bin/bash

# Seed Development Database from SQLite
echo "================================================"
echo "Seed Development Database (compass-dev)"
echo "================================================"
echo ""
echo "This will seed reference data from SQLite to Azure SQL compass-dev database"
echo ""

read -p "Do you want to proceed? (yes/no): " -r
echo ""
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]
then
    echo "Seeding cancelled."
    exit 1
fi

export ASPNETCORE_ENVIRONMENT=Development
dotnet run --seed-from-sqlite --environment Development

echo ""
echo "✓ Development database seeding complete!"

