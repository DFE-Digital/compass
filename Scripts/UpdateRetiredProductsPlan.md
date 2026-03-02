# Plan: Update Retired CMDB Products to "Decommissioned" Phase in CMS

## Overview
For products that have `operational_status = 6` (Retired) in CMDB but `state = Active` in CMS, we need to:
1. Set their Phase category value to "Decommissioned" in CMS
2. Preserve all other category values (Type, Channel, Group, etc.)

## What Needs to Be Done

### 1. Find the "Decommissioned" Category Value ID

**Query CMS API:**
```
GET /api/category-values?filters[category_type][name][$eq]=Phase&filters[name][$eq]=Decommissioned&filters[publishedAt][$notNull]=true&filters[enabled]=true
```

This will return the category value with its `id` field, which we'll need for the update.

### 2. For Each Product (113 products from the log file):

**Step 2a: Get Current Product Data**
```
GET /api/products?filters[fips_id][$eq]={FipsId}&populate[category_values][fields][0]=id&populate[category_values][fields][1]=name&populate[category_values][populate][category_type][fields][0]=name
```

**Step 2b: Build Updated Category Values List**
- Get all existing category value IDs from the product
- Identify which category value is the current Phase (if any)
- Remove the old Phase category value ID from the list
- Add the "Decommissioned" Phase category value ID to the list
- Keep all other category values (Type, Channel, Group, etc.)

**Step 2c: Update the Product**
```
PUT /api/products/{documentId}
Authorization: Bearer {WriteApiKey}
Content-Type: application/json

{
  "data": {
    "fips_id": "{FipsId}",
    "category_values": [list of all category value IDs including Decommissioned]
  }
}
```

**Important Notes:**
- Must include `fips_id` in the update payload to prevent it from being regenerated
- Must include ALL category value IDs (not just the new one) - Strapi will clear category_values if not included
- Use the product's `documentId` (not `id`) for the PUT request

### 3. Implementation Details

**Category Value Handling:**
- Products can have multiple category values from different types (Phase, Type, Channel, Group)
- Phase appears to be single-select (one Phase per product based on filtering logic)
- When updating, we should:
  - Remove any existing Phase category value
  - Add "Decommissioned" as the Phase category value
  - Preserve all other category values (Type, Channel, Group, etc.)

**Error Handling:**
- If product not found by FipsId, skip and log
- If "Decommissioned" category value not found, abort and log error
- If update fails, log error but continue with next product
- Track success/failure counts

**Logging:**
- Log each product being updated
- Log the category values before and after update
- Log any errors encountered
- Generate summary report

### 4. Configuration Required

From `appsettings.json`:
- `CmsApi:BaseUrl` - CMS API base URL
- `CmsApi:WriteApiKey` - Write API key for CMS updates

### 5. Sample Update Payload

**Before Update:**
Product has category_values: [123, 456, 789]
- 123 = Phase: "Live"
- 456 = Type: "Service"
- 789 = Channel: "Web"

**After Update:**
Product should have category_values: [999, 456, 789]
- 999 = Phase: "Decommissioned" (replaced 123)
- 456 = Type: "Service" (preserved)
- 789 = Channel: "Web" (preserved)

### 6. Verification

After updates, verify:
- All 113 products have Phase = "Decommissioned"
- Other category values are preserved
- Products still have correct FipsId
- No products were accidentally modified

## Questions to Confirm

1. **Should we also change the product `state` from "Active" to something else?** (Currently only updating category_values)
2. **Should we replace the Phase or add "Decommissioned" alongside existing Phase?** (Plan assumes replace)
3. **Should we update products in batches or all at once?** (Recommend batches of 10-20 with delays)
4. **Should we create a backup/audit log of changes?** (Recommended)
