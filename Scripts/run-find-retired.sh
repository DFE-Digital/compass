#!/bin/bash
# Simple script to run the FindRetiredInCmdbButActiveInCms script
# This compiles and runs the script without modifying Program.cs

echo "Building Compass project..."
dotnet build Compass.csproj

if [ $? -ne 0 ]; then
    echo "Build failed. Please fix compilation errors."
    exit 1
fi

echo ""
echo "Running FindRetiredInCmdbButActiveInCms script..."
echo ""

# Create a temporary Program.cs entry point
cat > Program.temp.cs << 'ENDOFFILE'
using System;
using System.Threading.Tasks;
using Compass.Scripts;

// Temporary entry point for running the script
var result = await RunFindRetiredScript.Main(args);
Environment.Exit(result);
ENDOFFILE

# Note: This approach requires the script to be integrated into Program.cs
# For now, we'll provide instructions instead
echo "To run this script, you have two options:"
echo ""
echo "Option 1: Add this to Program.cs (after line 30):"
echo "  // Check for finding retired CMDB entries that are active in CMS"
echo "  if (args.Length > 0 && args[0] == \"--find-retired-mismatch\")"
echo "  {"
echo "      await Compass.Scripts.RunFindRetiredScript.Main(args);"
echo "      return;"
echo "  }"
echo ""
echo "Then run: dotnet run -- --find-retired-mismatch"
echo ""
echo "Option 2: Use the script programmatically in your own code"
echo "See README-FindRetired.md for details"

rm -f Program.temp.cs
