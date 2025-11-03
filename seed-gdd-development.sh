#!/bin/bash

# Seed GDD Framework data into development database

echo "Seeding GDD Framework into development database..."
echo ""

dotnet run --seed-gdd-framework --environment Development --csv-file "https://cf-production-data-exports.s3.eu-west-2.amazonaws.com/exports/Role%20and%20skill%20content%20-%20Capability%20Framework%20-%20Government%20Digital%20and%20Data%20profession2025-10-28_13-40-49.csv"

