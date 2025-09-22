#!/bin/bash
# Development startup script for FIPS Reporting
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --urls "http://localhost:5500"