# Business area feature

## Overview

All RAID entities (Risks, Issues, Actions, Milestones) can now be classified by Business Area. Business areas are dynamically loaded from the FIPS CMS using category values with category_type = "Business area", ensuring consistency across the platform.

## Database changes

### Added fields

A `BusinessArea` field (TEXT/NVARCHAR(100), nullable) has been added to:
- `Risks` table
- `Issues` table
- `Milestones` table
- `Actions` table

### Migration

**Migration name:** `AddBusinessAreaToRAIDEntities`

**Migration timestamp:** 20251017200522

Applied successfully to the database.

## Data source

Business areas are fetched from the FIPS CMS via the Products API:
- **Endpoint:** `/api/category-values`
- **Filter:** `category_type.name = "Business area"`
- **Sorting:** By `sort_order` (ascending), then by `name`
- **Caching:** Results cached for 1 hour to improve performance

### CMS configuration

To add or modify business areas:
1. Log in to FIPS CMS Admin
2. Navigate to Category Values
3. Create/edit entries with Category Type = "Business area"
4. Set appropriate sort order for display ordering
5. Changes appear in Compass within 1 hour (or clear cache immediately)

## Implementation details

### Service layer

**New method added to `IProductsApiService`:**
```csharp
Task<List<string>> GetBusinessAreasAsync();
```

**Implementation in `ProductsApiService`:**
- Queries CMS for category values where category_type = "Business area"
- Orders by sort_order then name
- Returns list of business area names
- Implements 1-hour caching
- Graceful error handling

### Model updates

Updated C# models:
- `Risk.cs` - Added `BusinessArea` property
- `Issue.cs` - Added `BusinessArea` property
- `Milestone.cs` - Added `BusinessArea` property
- `Action.cs` - Added `BusinessArea` property

All properties include `[MaxLength(100)]` validation attribute.

### Controller updates

All RAID controllers now:
1. Call `GetBusinessAreasAsync()` in Create and Edit actions
2. Populate `ViewBag.BusinessAreas` with business area list
3. Include `BusinessArea` in `[Bind]` attributes for POST actions
4. Update `BusinessArea` when saving entities

**Updated controllers:**
- `RiskController`
- `IssueController`
- `MilestoneController`
- `ActionController`

### View changes

#### Create and Edit forms

All Create and Edit views include a business area dropdown:

```html
<div class="form-group">
    <label asp-for="BusinessArea" class="control-label">Business area</label>
    <select asp-for="BusinessArea" class="form-control">
        <option value="">-- Select business area (optional) --</option>
        @if (ViewBag.BusinessAreas != null)
        {
            @foreach (var area in ViewBag.BusinessAreas)
            {
                <option value="@area">@area</option>
            }
        }
    </select>
</div>
```

**Updated Create views:**
- `Risk/Create.cshtml`
- `Issue/Create.cshtml`
- `Milestone/Create.cshtml`
- `Action/Create.cshtml`

**Updated Edit views:**
- `Risk/Edit.cshtml`
- `Issue/Edit.cshtml`
- `Milestone/Edit.cshtml`
- `Action/Edit.cshtml`

#### Details views

All Details views display the business area:

```html
<dt class="col-sm-3">Business area</dt>
<dd class="col-sm-9">@(Model.BusinessArea ?? "-")</dd>
```

**Updated views:**
- `Risk/Details.cshtml`
- `Issue/Details.cshtml`
- `Milestone/Details.cshtml`
- `Action/Details.cshtml`

## Use cases

### Organizational structure

Business areas typically represent:
- Functional teams (e.g., "Digital Services", "Infrastructure", "Security")
- Departments (e.g., "HR", "Finance", "Operations")
- Programme areas (e.g., "Cloud Migration", "Digital Transformation")
- Business units (e.g., "Teaching & Learning", "Student Services")

### Reporting and filtering

Teams can:
- Track RAID items by business area
- Report on risk exposure per area
- Identify which areas have most issues
- Allocate resources based on area workload
- Filter views by business area (future enhancement)

### Example scenarios

**Business area tracking:**
- Risk: "API capacity limits" assigned to "Infrastructure" business area
- Issue: "User authentication failures" assigned to "Security" business area
- Milestone: "Complete migration" assigned to "Cloud Migration" business area
- Action: "Review security policies" assigned to "Security" business area

**Cross-area coordination:**
- Items can belong to both a product AND a business area
- Strategic objectives may span multiple business areas
- Some items may have neither product nor business area

## Field classification summary

RAID items can now be classified using:
1. **Strategic objective** - Top-level strategic goal (optional)
2. **Product** - Specific FIPS product by FipsId (optional)
3. **Business area** - Organizational unit from CMS Groups (optional)
4. **Category** - Free-text categorisation (optional)

This provides multiple dimensions for organizing and reporting on RAID items.

## Data integrity

- Business areas validated against CMS data when loading forms
- Values fetched fresh on each form load (with 1-hour caching)
- No database foreign key constraint (groups managed in CMS)
- BusinessArea stored as string to match CMS format
- Optional field - can be left blank

## Performance

### Caching strategy

Business areas are cached for 1 hour:
- **First load:** Queries CMS API
- **Subsequent loads:** Served from memory cache
- **Cache expiry:** After 1 hour or application restart
- **Error handling:** Returns empty list on API failure

### Cache invalidation

To force immediate update:
- Restart Compass application
- Wait for 1-hour cache expiry
- Or implement manual cache clear endpoint (future enhancement)

## Future enhancements

Potential additions:
1. **Filtering by business area** - Filter Index pages by selected area
2. **Business area dashboard** - Aggregate view per area
3. **Business area owners** - Assign responsible users to areas
4. **Cross-area dependencies** - Track dependencies between areas
5. **Business area analytics** - Workload and performance metrics per area
6. **Bulk operations** - Update multiple items' business area
7. **Business area hierarchy** - Support for nested areas (if CMS supports)

## Integration with other features

### Reports

The Reports section could be enhanced to:
- Show RAID counts per business area
- Compare health scores across areas
- Identify problematic business areas
- Trend analysis by area

### Analysis dashboard

Future analysis could include:
- Which business areas have highest risk scores
- Which areas have most blocked issues
- Which areas are most overdue on actions
- Area-specific problem scores

## Consistency with FIPS platform

Business areas from CMS ensure:
- **Single source of truth** - All systems use same business areas
- **Automatic updates** - Changes in CMS propagate to Compass
- **Data consistency** - No discrepancies between systems
- **Centralized management** - Business areas managed in one place (CMS)

## Accessibility

The business area dropdown:
- Follows GOV.UK design system patterns
- Includes proper labels and validation
- Supports keyboard navigation
- Displays in consistent location across all forms
- Optional selection (not required)

---

**Created:** 17 October 2025  
**Migration:** 20251017200522_AddBusinessAreaToRAIDEntities  
**CMS Integration:** category_type = "Business area"  
**Cache duration:** 1 hour  
**Version:** 1.0

