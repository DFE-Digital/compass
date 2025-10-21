#!/bin/bash

# Compass Development Startup Script

echo "Starting Compass development environment..."

# Build CSS from SCSS
echo "Building CSS..."
npm run build-css

# Run the application
echo "Starting application on http://localhost:5500..."
dotnet run --environment Development

