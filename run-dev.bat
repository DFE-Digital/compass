@echo off
REM FIPS Reporting Platform - Development Launch Script for Windows

echo 🚀 Starting FIPS Reporting Platform...

REM Check if Node.js is installed
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ Node.js is not installed. Please install Node.js first.
    pause
    exit /b 1
)

REM Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ .NET SDK is not installed. Please install .NET 8.0 SDK first.
    pause
    exit /b 1
)

REM Install npm dependencies if node_modules doesn't exist
if not exist "node_modules" (
    echo 📦 Installing npm dependencies...
    npm install
)

REM Build CSS
echo 🎨 Building CSS...
npm run build-css

REM Run the application
echo 🌐 Starting application on port 5500...
echo    HTTP:  http://localhost:5500
echo    HTTPS: https://localhost:5501
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run

pause
