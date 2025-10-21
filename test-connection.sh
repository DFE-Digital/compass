#!/bin/bash
echo "Testing Azure SQL Connection..."
echo "Server: s186d01-dops-compass.database.windows.net"
echo "Database: compass"
echo ""

# Test if we can at least connect to the server
nc -zv s186d01-dops-compass.database.windows.net 1433 2>&1
