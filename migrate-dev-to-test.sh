#!/bin/bash

# Migrate business areas and phases from Dev to Test
# This script migrates only reference data (lookup tables) from Development to Test environment

echo "=== Migrating Business Areas and Phases: Dev → Test ==="
echo ""

# Run the migration with reference-only flag
dotnet run --migrate-sql --source Development --target Test --reference-only

echo ""
echo "✓ Migration script completed"
