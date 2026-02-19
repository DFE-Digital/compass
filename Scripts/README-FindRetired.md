# Find Retired CMDB Entries That Are Active in CMS

This script identifies products that have an Operational Status of "Retired" in the CMDB but have a State of "Active" in the CMS.

## Files

- `FindRetiredInCmdbButActiveInCms.cs` - Main script logic
- `RunFindRetiredScript.cs` - Console entry point to run the script

## How to Run

### Option 1: Add to Program.cs (Temporary)

Add this check to `Program.cs` before the WebApplication builder:

```csharp
// Check for finding retired CMDB entries that are active in CMS
if (args.Length > 0 && args[0] == "--find-retired-mismatch")
{
    await Compass.Scripts.RunFindRetiredScript.Main(args);
    return;
}
```

Then run:
```bash
dotnet run -- --find-retired-mismatch
```

### Option 2: Create a Simple Console App

Create a new console project or add this as a separate entry point. The script is self-contained and can be run independently.

### Option 3: Use as a Library

The `FindRetiredInCmdbButActiveInCms` class can be instantiated and used programmatically:

```csharp
var finder = new FindRetiredInCmdbButActiveInCms(
    cmdbEndpoint,
    cmdbUsername,
    cmdbPassword,
    cmsBaseUrl,
    cmsReadApiKey);

var mismatches = await finder.FindMismatchesAsync();
finder.PrintResults(mismatches);
```

## Output

The script will output:
1. A summary of how many retired CMDB entries were found
2. A summary of how many active CMS products were found
3. A table showing all mismatches (products that are retired in CMDB but active in CMS)
4. JSON output for programmatic use

## Configuration

The script reads configuration from `appsettings.json`:
- `FipsSync:Cmdb:Endpoint` - CMDB API endpoint
- `FipsSync:Cmdb:Username` - CMDB username
- `FipsSync:Cmdb:Password` - CMDB password
- `FipsSync:Strapi:Test:Endpoint` - CMS API endpoint
- `FipsSync:Strapi:Test:ApiKey` - CMS API key

## Notes

- The script queries CMDB for entries with `operational_status=Retired`
- The script queries CMS for products with `state=Active`
- Matching is done by `cmdb_sys_id` (CMS) = `sys_id` (CMDB)
- No changes are made to the data - this is a read-only reporting script
