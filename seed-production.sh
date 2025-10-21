#!/bin/bash

# Seed Production Database from SQLite
echo "================================================"
echo "Seed Production Database (compass)"
echo "================================================"
echo ""
echo "This will seed reference data from SQLite to Azure SQL compass database"
echo ""

read -p "Do you want to proceed? (yes/no): " -r
echo ""
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]
then
    echo "Seeding cancelled."
    exit 1
fi

export ASPNETCORE_ENVIRONMENT=Production
dotnet run --seed-from-sqlite --environment Production

echo ""
echo "✓ Production database seeding complete!"

