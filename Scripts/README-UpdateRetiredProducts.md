# Update Retired Products to Decommissioned Phase

## Overview

This script updates products that have `operational_status = 6` (Retired) in CMDB but `state = Active` in CMS to set their Phase category value to "Decommissioned".

## Prerequisites

1. **Run the find script first** to generate the list of products:
   ```bash
   dotnet run -- --find-retired-mismatch
   ```
   This creates a log file with the list of products to update.

2. **Review the log file** to confirm which products will be updated.

## Running the Update Script

```bash
dotnet run -- --update-retired-products
```

## What the Script Does

1. **Finds "Decommissioned" category value** - Queries CMS to get the ID of the "Decommissioned" Phase category value

2. **Loads products from log file** - Reads the most recent `retired-cmdb-active-cms-*.log` file to get the list of products to update

3. **For each product:**
   - Gets current product data with all category values
   - Creates a rollback log entry with original category values
   - Removes existing Phase category value (if any)
   - Adds "Decommissioned" Phase category value
   - Preserves all other category values (Type, Channel, Group, etc.)
   - Updates the product via CMS API
   - Logs success/failure

4. **Generates reports:**
   - Update log file: `logs/update-retired-products-YYYYMMDD-HHMMSS.log`
   - Rollback file: `logs/rollback-retired-products-YYYYMMDD-HHMMSS.json`

## Safety Features

- **Confirmation prompt** - Script asks for confirmation before proceeding
- **Rollback logging** - All original category values are saved for rollback
- **Error handling** - Continues processing even if individual products fail
- **Detailed logging** - Logs each step and result
- **Rate limiting** - 500ms delay between updates to avoid overwhelming API

## Rollback

If you need to rollback changes, use the rollback JSON file which contains:
- Original category value IDs for each product
- Original category value details (name, type)

You can use this data to restore products to their original state.

## Configuration

The script reads from `appsettings.json`:
- `FipsSync:Strapi:Test:Endpoint` - CMS API endpoint
- `FipsSync:Strapi:Test:ApiKey` - CMS write API key

## Output

The script outputs:
- Progress for each product being updated
- Summary of successful/failed updates
- Paths to log and rollback files

## Notes

- Product `state` remains "Active" (only category_values are updated)
- Only Phase category value is replaced (one Phase per product)
- All other category values are preserved
- Uses product `documentId` for updates (Strapi v5 requirement)
