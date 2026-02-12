# RAID product linking

## Overview

Risks, Issues, Actions, and Milestones can now be optionally linked to specific products by their FIPS ID. This allows teams to track RAID items at both the strategic objective level and the individual product level.

## Database changes

### Added fields

A `FipsId` field (TEXT/NVARCHAR(50), nullable) has been added to:
- `Risks` table
- `Issues` table
- `Milestones` table
- `Actions` table

### Indexes

Created indexes on `FipsId` for all four tables to enable efficient product-based filtering:
- `IX_Risks_FipsId`
- `IX_Issues_FipsId`
- `IX_Milestones_FipsId`
- `IX_Actions_FipsId`

### Migration

**Migration name:** `AddFipsIdToRAIDEntities`

Applied successfully to the database.

## Implementation details

### Model changes

Updated C# models:
- `Risk.cs` - Added `FipsId` property
- `Issue.cs` - Added `FipsId` property
- `Milestone.cs` - Added `FipsId` property
- `Action.cs` - Added `FipsId` property

All properties include `[MaxLength(50)]` validation attribute.

### Controller updates

All RAID controllers now:
1. Inject `IProductsApiService` dependency
2. Load products via `GetProductsAsync(null)` in Create and Edit actions
3. Populate `ViewBag.Products` with product list
4. Include `FipsId` in `[Bind]` attributes for POST actions
5. Update `FipsId` when saving entities

**Updated controllers:**
- `RiskController`
- `IssueController`
- `MilestoneController`
- `ActionController`

### View changes

#### Create and Edit forms

All Create and Edit views now include a product dropdown:

```html
<div class="form-group">
    <label asp-for="FipsId" class="control-label">Product</label>
    <select asp-for="FipsId" class="form-control">
        <option value="">-- Select product (optional) --</option>
        @if (ViewBag.Products != null)
        {
            @foreach (var product in ViewBag.Products)
            {
                <option value="@product.FipsId">@product.Title (@product.FipsId)</option>
            }
        }
    </select>
</div>
```

**Updated views:**
- `Risk/Create.cshtml` and `Risk/Edit.cshtml`
- `Issue/Create.cshtml` and `Issue/Edit.cshtml`
- `Milestone/Create.cshtml` and `Milestone/Edit.cshtml`
- `Action/Create.cshtml` and `Action/Edit.cshtml`

#### Details views

All Details views display the linked product:

```html
<dt class="col-sm-3">Product</dt>
<dd class="col-sm-9">@(Model.FipsId ?? "-")</dd>
```

**Updated views:**
- `Risk/Details.cshtml`
- `Issue/Details.cshtml`
- `Milestone/Details.cshtml`
- `Action/Details.cshtml`

## Use cases

### Product-specific RAID tracking

Teams can now:
1. Create risks/issues/actions/milestones specific to a product
2. Filter RAID items by product (future enhancement)
3. Report on RAID items at product level
4. Track product-specific delivery milestones

### Dual tracking

RAID items can be linked to:
- **Strategic objective only** - Enterprise-wide initiatives
- **Product only** - Product-specific concerns
- **Both objective and product** - Strategic work affecting specific products
- **Neither** - Standalone items

### Example scenarios

**Product-specific risk:**
- Objective: (none)
- Product: DfE Sign-in (S142)
- Title: "API rate limits may impact authentication"
- Use case: Product team tracking technical risks

**Strategic risk with product impact:**
- Objective: "Improve authentication security"
- Product: DfE Sign-in (S142)
- Title: "Two-factor authentication rollout risks"
- Use case: Strategic initiative affecting specific product

**Enterprise-wide issue:**
- Objective: "Cloud migration programme"
- Product: (none)
- Title: "Azure capacity constraints"
- Use case: Infrastructure issue affecting all products

## Future enhancements

Potential additions:
1. Product filter on Index pages (show all risks for a specific product)
2. Product-based dashboard/reporting
3. Bulk operations by product
4. Product owner notifications
5. Cross-product dependency tracking

## Data integrity

- FipsId is validated against the products API when loading dropdowns
- Products are fetched fresh on each form load to ensure current data
- No database foreign key constraint (products are managed in external CMS)
- FipsId stored as string to match product identifier format

---

**Created:** 17 October 2025  
**Migration:** 20251017193900_AddFipsIdToRAIDEntities  
**Version:** 1.0

