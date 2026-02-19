# Summary: What's Needed to Update Retired Products

## Current Situation
- **145 products** have `operational_status = 6` (Retired) in CMDB but `state = Active` in CMS
- These products need their Phase category value set to "Decommissioned"

## What Needs to Be Done

### 1. Find "Decommissioned" Category Value ID

**API Call:**
```
GET {CMS_BASE_URL}/api/category-values?filters[category_type][name][$eq]=Phase&filters[name][$eq]=Decommissioned&filters[publishedAt][$notNull]=true&filters[enabled]=true
```

**Expected Response:**
```json
{
  "data": [
    {
      "id": 123,  // <-- This is what we need
      "name": "Decommissioned",
      "category_type": {
        "name": "Phase"
      }
    }
  ]
}
```

### 2. For Each of the 145 Products

**Step 1: Get Current Product**
```
GET {CMS_BASE_URL}/api/products?filters[fips_id][$eq]={FipsId}&populate[category_values][fields][0]=id&populate[category_values][populate][category_type][fields][0]=name
```

**Step 2: Process Category Values**
- Extract all current category value IDs
- Find the Phase category value (if any) and remove it
- Add the "Decommissioned" Phase category value ID
- Keep all other category values (Type, Channel, Group, etc.)

**Step 3: Update Product**
```
PUT {CMS_BASE_URL}/api/products/{documentId}
Headers:
  Authorization: Bearer {WriteApiKey}
  Content-Type: application/json

Body:
{
  "data": {
    "fips_id": "{FipsId}",
    "category_values": [list of all category value IDs]
  }
}
```

### 3. Key Implementation Points

**Critical Requirements:**
1. **Must preserve `fips_id`** - Include it in the update payload to prevent regeneration
2. **Must include ALL category values** - Strapi clears category_values if not included in PUT
3. **Use `documentId` not `id`** - Strapi v5 uses documentId for updates
4. **Replace Phase, preserve others** - Remove existing Phase, add "Decommissioned", keep Type/Channel/Group

**Category Value Logic:**
```csharp
// Pseudocode
var currentCategoryValueIds = product.CategoryValues.Select(cv => cv.Id).ToList();
var phaseCategoryValue = product.CategoryValues
    .FirstOrDefault(cv => cv.CategoryType?.Name == "Phase");

if (phaseCategoryValue != null)
{
    currentCategoryValueIds.Remove(phaseCategoryValue.Id);
}

currentCategoryValueIds.Add(decommissionedPhaseCategoryValueId);

// Update with all IDs
```

### 4. Configuration

From `appsettings.json`:
- `CmsApi:BaseUrl` - e.g., "https://fips-cms-test.azurewebsites.net/api/"
- `CmsApi:WriteApiKey` - Write API key for authentication

### 5. Safety Considerations

**Before Running:**
- ✅ Verify "Decommissioned" category value exists and get its ID
- ✅ Test with 1-2 products first
- ✅ Create backup/audit log of all changes
- ✅ Add error handling for each product update
- ✅ Add rate limiting (delays between updates)

**During Execution:**
- Log each product being updated
- Log category values before and after
- Continue on error (don't stop entire batch)
- Track success/failure counts

**After Execution:**
- Verify all 145 products have Phase = "Decommissioned"
- Verify other category values are preserved
- Check for any errors in logs

### 6. Questions to Confirm

1. **Should we change product `state` from "Active"?** 
   - Currently plan only updates `category_values`
   - Product state can remain "Active" if that's acceptable

2. **Replace or add Phase?**
   - Plan assumes **replace** (remove old Phase, add "Decommissioned")
   - Alternative: Add "Decommissioned" alongside existing Phase (if multiple allowed)

3. **Batch size?**
   - Recommend batches of 10-20 products with 1-2 second delays
   - Or process all 145 sequentially with delays

4. **Rollback plan?**
   - Should we log original category values for rollback?
   - Or is this a one-way operation?

### 7. Sample Data Flow

**Product Before:**
- FipsId: "XXI-924"
- Category Values: 
  - Phase: "Live" (ID: 50)
  - Type: "Service" (ID: 100)
  - Channel: "Web" (ID: 200)

**Product After:**
- FipsId: "XXI-924" (preserved)
- Category Values:
  - Phase: "Decommissioned" (ID: 999) ← replaced
  - Type: "Service" (ID: 100) ← preserved
  - Channel: "Web" (ID: 200) ← preserved

**Update Payload:**
```json
{
  "data": {
    "fips_id": "XXI-924",
    "category_values": [999, 100, 200]
  }
}
```

## Next Steps

1. **Confirm approach** - Review this plan and confirm details
2. **Create update script** - Implement the update logic
3. **Test with 1-2 products** - Verify it works correctly
4. **Run on all 145 products** - Execute the full update
5. **Verify results** - Check all products were updated correctly
