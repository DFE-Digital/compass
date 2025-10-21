# Filtering and personalization features

## Overview

RAID index views (Risks, Issues, Actions, Milestones) now support comprehensive filtering and personalization to help users focus on what matters most to them. This includes "Assigned to me" sections, business area preferences, and multi-dimensional filtering.

## User preferences

### UserPreference model

**Table:** `UserPreferences`

| Column                 | Type                 | Description                                   |
| ---------------------- | -------------------- | --------------------------------------------- |
| UserId                 | INTEGER / INT        | Primary key, FK to Users                      |
| PreferredBusinessAreas | TEXT / NVARCHAR(MAX) | Comma-separated list of business areas        |
| CreatedAt              | DATETIME / DATETIME2 | UTC timestamp when created                    |
| UpdatedAt              | DATETIME / DATETIME2 | UTC timestamp when last updated               |

**Relationship:** One-to-one with Users (cascade delete)

### Migration

**Migration name:** `AddUserPreferences` (20251017204059)

Applied successfully ✅

### User settings page

**Location:** Top navbar > User icon > My settings

**Features:**
- View current user information
- Select preferred business areas (multiple checkboxes)
- Save preferences
- Clear explanation of how preferences work

**URL:** `/User/MySettings`

### How preferences work

1. **No preferences set:** User sees all items across all business areas
2. **Preferences set:** Views default to showing items from selected business areas
3. **"Assigned to me":** Always shows assigned items regardless of business area
4. **Override:** Users can still filter to view other business areas or all items

## Index view structure

Each RAID index view is split into two main sections:

### Section 1: Assigned to me

**Purpose:** Quick access to items requiring user's attention

**Filter logic:**
- **Risks:** `OwnerUserId == current user`
- **Issues:** `OwnerUserId == current user`
- **Actions:** `AssignedToUserId == current user`
- **Milestones:** `OwnerUserId == current user`

**Features:**
- Always visible regardless of other filters
- Prominently placed at top
- Shows count of assigned items
- Collapsible card if zero items
- Sorted by priority/urgency

### Section 2: All items (or filtered items)

**Default behavior:**
- If user has business area preferences → Show items from those areas
- If no preferences → Show all items

**Applies user preferences automatically:**
- Filters to preferred business areas
- Shows indicator that filter is applied
- Easy to clear filter to see all items

## Filtering capabilities

### Available filters

All index views support filtering by:

1. **Product (FipsId)**
   - Dropdown of all products
   - Shows items for selected product only
   - Clear filter to see all

2. **Business area**
   - Dropdown of all business areas from CMS
   - Shows items for selected area only
   - Auto-populated from user preferences
   - Clear filter to see all

3. **Assigned to / Owner**
   - Dropdown of all users
   - Shows items assigned to selected user
   - "Assigned to me" separate section

4. **Objective** (if applicable)
   - Dropdown of active objectives
   - Shows items linked to selected objective
   - Context-sensitive

### Filter UI

**Location:** Top of page, above tables

**Design:** Inline form with dropdowns

**Example:**
```html
[Filter by Product ▼] [Filter by Business Area ▼] [Filter by Owner ▼] [Apply Filters] [Clear]
```

**State management:**
- Filters preserved in query string
- Sharable URLs with filters
- Browser back/forward works correctly

### Filter interactions

**Combining filters:**
- Multiple filters applied as AND conditions
- Example: Product X AND Business Area Y AND Owner Z

**Default filters:**
- Business area from user preferences (if set)
- Can be overridden by manual selection
- Visual indicator when preference filter active

## Implementation details

### Controller updates

**All RAID controllers** (Risk, Issue, Action, Milestone) need:

```csharp
public async Task<IActionResult> Index(
    int? objectiveId, 
    string? product, 
    string? businessArea, 
    int? ownerId)
{
    // Get current user
    var userEmail = User.Identity?.Name;
    var currentUser = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == userEmail);
    
    // Get user preferences if no filter specified
    List<string> preferredAreas = new();
    if (string.IsNullOrEmpty(businessArea) && currentUser != null)
    {
        preferredAreas = await UserPreferencesHelper
            .GetPreferredBusinessAreasAsync(_context, userEmail);
    }
    
    // Build "Assigned to me" query
    var myQuery = _context.Risks
        .Include(r => r.OwnerUser)
        .Include(r => r.Objective)
        .Include(r => r.RiskTier)
        .Include(r => r.RiskRiskTypes).ThenInclude(rrt => rrt.RiskType)
        .Where(r => !r.IsDeleted && r.OwnerUserId == currentUser.Id);
    
    // Build "All risks" query
    var allQuery = _context.Risks
        .Include(r => r.OwnerUser)
        .Include(r => r.Objective)
        .Include(r => r.RiskTier)
        .Include(r => r.RiskRiskTypes).ThenInclude(rrt => rrt.RiskType)
        .Where(r => !r.IsDeleted);
    
    // Apply filters
    if (objectiveId.HasValue)
    {
        allQuery = allQuery.Where(r => r.ObjectiveId == objectiveId);
    }
    
    if (!string.IsNullOrEmpty(product))
    {
        allQuery = allQuery.Where(r => r.FipsId == product);
    }
    
    if (!string.IsNullOrEmpty(businessArea))
    {
        allQuery = allQuery.Where(r => r.BusinessArea == businessArea);
    }
    else if (preferredAreas.Any())
    {
        // Apply user preference filter
        allQuery = allQuery.Where(r => preferredAreas.Contains(r.BusinessArea));
        ViewBag.PreferenceFilterApplied = true;
    }
    
    if (ownerId.HasValue)
    {
        allQuery = allQuery.Where(r => r.OwnerUserId == ownerId);
    }
    
    // Execute queries
    var myRisks = await myQuery
        .OrderByDescending(r => r.RiskScore)
        .ToListAsync();
    
    var allRisks = await allQuery
        .OrderByDescending(r => r.RiskScore)
        .ToListAsync();
    
    // Populate filter dropdowns
    ViewBag.MyRisks = myRisks;
    ViewBag.AllRisks = allRisks;
    ViewBag.Products = await GetProductsForFilter();
    ViewBag.BusinessAreas = await _productsApiService.GetBusinessAreasAsync();
    ViewBag.Users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
    ViewBag.Objectives = await _context.Objectives
        .Where(o => !o.IsDeleted)
        .OrderBy(o => o.Title)
        .ToListAsync();
    
    // Current filter values
    ViewBag.CurrentProduct = product;
    ViewBag.CurrentBusinessArea = businessArea;
    ViewBag.CurrentOwnerId = ownerId;
    ViewBag.CurrentObjectiveId = objectiveId;
    
    return View();
}
```

### View structure

**Enhanced index views include:**

1. **Page header** with title and create button

2. **Filter panel** (collapsible card)
   - Product dropdown
   - Business area dropdown
   - Owner/Assigned to dropdown
   - Objective dropdown (if applicable)
   - Apply and Clear buttons
   - Active filter indicators

3. **"Assigned to me" section**
   - Collapsed if zero items
   - Expanded if items present
   - Count badge
   - Same table structure as main table
   - Highlights user's responsibilities

4. **"All items" section** (or "Filtered items")
   - Shows count
   - Indicates if preference filter applied
   - Link to clear business area filter
   - Main data table

### Filter panel HTML example

```html
<div class="card card-outline card-primary collapsed-card">
    <div class="card-header">
        <h3 class="card-title">
            <i class="fas fa-filter"></i> Filters
            @if (hasActiveFilters)
            {
                <span class="badge badge-primary ml-2">Active</span>
            }
        </h3>
        <div class="card-tools">
            <button type="button" class="btn btn-tool" data-card-widget="collapse">
                <i class="fas fa-plus"></i>
            </button>
        </div>
    </div>
    <div class="card-body">
        <form method="get" asp-action="Index">
            <div class="row">
                <div class="col-md-3">
                    <div class="form-group">
                        <label>Product</label>
                        <select name="product" class="form-control">
                            <option value="">-- All products --</option>
                            @foreach (var p in ViewBag.Products)
                            {
                                <option value="@p.FipsId" selected="@(p.FipsId == ViewBag.CurrentProduct)">
                                    @p.Title (@p.FipsId)
                                </option>
                            }
                        </select>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-group">
                        <label>Business area</label>
                        <select name="businessArea" class="form-control">
                            <option value="">-- All areas --</option>
                            @foreach (var area in ViewBag.BusinessAreas)
                            {
                                <option value="@area" selected="@(area == ViewBag.CurrentBusinessArea)">
                                    @area
                                </option>
                            }
                        </select>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-group">
                        <label>Owner</label>
                        <select name="ownerId" class="form-control">
                            <option value="">-- All owners --</option>
                            @foreach (var u in ViewBag.Users)
                            {
                                <option value="@u.Id" selected="@(u.Id == ViewBag.CurrentOwnerId)">
                                    @u.Name
                                </option>
                            }
                        </select>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="form-group">
                        <label>&nbsp;</label>
                        <div>
                            <button type="submit" class="btn btn-primary btn-block">
                                <i class="fas fa-search"></i> Apply
                            </button>
                            <a asp-action="Index" class="btn btn-outline-secondary btn-block">
                                <i class="fas fa-times"></i> Clear
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    </div>
</div>
```

### "Assigned to me" section HTML

```html
<div class="card card-outline card-info">
    <div class="card-header">
        <h3 class="card-title">
            <i class="fas fa-user-check"></i> Risks assigned to me
            <span class="badge badge-info ml-2">@ViewBag.MyRisks.Count</span>
        </h3>
        @if (!ViewBag.MyRisks.Any())
        {
            <div class="card-tools">
                <button type="button" class="btn btn-tool" data-card-widget="collapse">
                    <i class="fas fa-minus"></i>
                </button>
            </div>
        }
    </div>
    <div class="card-body table-responsive p-0">
        <table class="table table-hover text-nowrap">
            <!-- Same structure as main table -->
            <thead><!-- ... --></thead>
            <tbody>
                @foreach (var risk in ViewBag.MyRisks)
                {
                    <!-- Row template -->
                }
            </tbody>
        </table>
    </div>
</div>
```

### Business area preference indicator

```html
@if (ViewBag.PreferenceFilterApplied == true)
{
    <div class="alert alert-info alert-dismissible fade show">
        <i class="fas fa-info-circle"></i>
        <strong>Business area filter applied:</strong> Showing items from your preferred business areas.
        <a asp-action="Index" class="alert-link">View all items</a>
        or
        <a asp-action="MySettings" asp-controller="User" class="alert-link">change preferences</a>.
        <button type="button" class="close" data-dismiss="alert">&times;</button>
    </div>
}
```

## Entity-specific filtering

### Risks

**Assigned field:** `OwnerUserId`

**Filters:**
- Product (FipsId)
- Business Area
- Owner
- Objective
- Risk Tier (future)
- Risk Types (future - multi-select)
- Status (future)

**Default sort:** Risk Score (descending)

**My risks:** Risks where I'm the owner

### Issues

**Assigned field:** `OwnerUserId`

**Filters:**
- Product (FipsId)
- Business Area
- Owner
- Objective
- Severity (future)
- Status (future)
- Blocked flag (future)

**Default sort:** Severity, then detected date

**My issues:** Issues where I'm the owner

### Actions

**Assigned field:** `AssignedToUserId`

**Filters:**
- Product (FipsId)
- Business Area
- Assigned to
- Objective
- Action Source
- Priority (future)
- Status (future)

**Default sort:** Priority, then due date

**My actions:** Actions assigned to me

### Milestones

**Assigned field:** `OwnerUserId`

**Filters:**
- Product (FipsId)
- Business Area
- Owner
- Objective
- Status (future)

**Default sort:** Due date (ascending)

**My milestones:** Milestones where I'm the owner

## Business area preferences

### Setting preferences

1. Click user icon in top navbar
2. Select "My settings"
3. Check desired business areas
4. Click "Save preferences"

### Effect on views

**With preferences set:**
- Risks index: Shows only risks from preferred areas
- Issues index: Shows only issues from preferred areas
- Actions index: Shows only actions from preferred areas
- Milestones index: Shows only milestones from preferred areas

**Always visible:**
- Items assigned to you (regardless of business area)
- Items with no business area set

**Override:**
- Use filter dropdown to select different area
- Select "All areas" to see everything
- Use URL parameters to share specific views

## URL parameters

Filters are implemented as query string parameters for sharability:

```
/Risk?product=FIPS-001&businessArea=Infrastructure&ownerId=5
/Issue?businessArea=Security&severity=critical
/Action?assignedToId=3&status=in_progress
/Milestone?objectiveId=2&status=at_risk
```

**Benefits:**
- Sharable filtered views
- Bookmark specific filters
- Browser back/forward works
- Deep linking support

## Visual indicators

### Active filters

When filters are applied:
- Filter panel shows "Active" badge
- Each active filter highlighted
- Count shows filtered vs total
- Clear indication of what's hidden

**Example:**
```
Showing 12 of 47 risks
Filters: Product=FIPS-001, Business Area=Infrastructure
```

### Preference filters

When business area preference filter is auto-applied:
- Info banner explains preference is active
- Link to view all items
- Link to change preferences
- Different visual style than manual filters

### Empty states

**No assigned items:**
```
No risks assigned to you.
[Collapsed section]
```

**No items match filter:**
```
No risks found matching your filters.
Try clearing some filters or viewing all items.
[Clear filters button]
```

## Performance considerations

### Efficient queries

- Single database query for "my items"
- Single database query for "all items"
- Includes all navigation properties upfront
- Indexes support filtering fields

### Caching

- Business areas cached (1 hour)
- Products list cached per request
- User preferences cached in session (future)
- No repeated DB calls per page

### Pagination (future enhancement)

For large datasets, implement:
- Page size selection (25/50/100)
- Page navigation
- Total count display
- Jump to page

## User experience improvements

### Smart defaults

1. **New user (no preferences):**
   - Sees all items
   - Prompted to set preferences
   - Help text explains benefits

2. **User with preferences:**
   - Auto-filtered to their areas
   - Sees relevant items immediately
   - Can override when needed

3. **User viewing "assigned to me":**
   - Prioritized at top
   - Immediate action focus
   - Cross-area visibility

### Count indicators

All views show:
- Count of "my items"
- Count of visible items
- Count of total items (if filtered)
- Percentage if meaningful

**Example:**
```
Risks assigned to me: 8
All risks: 23 of 156 (filtered by: Infrastructure)
```

### Quick actions

From index views:
- Filter to business area (click area name)
- Filter to product (click product)
- Filter to owner (click owner name)
- Clear all filters (button)
- Export filtered view (future)

## Navigation flow

### Typical user journey

1. **Login** → Lands on homepage
2. **Navigate to Risks** → Auto-filtered to preferred business areas
3. **See "Assigned to me" (8 risks)** → Review owned risks
4. **See "Infrastructure risks" (23)** → Review team risks
5. **Need to see Security risks** → Change filter dropdown
6. **Want to see everything** → Select "All areas"

### Setting preferences journey

1. **Click user icon** → Dropdown appears
2. **Select "My settings"** → Preferences page
3. **Check business areas** → Infrastructure, Security
4. **Save** → Preferences stored
5. **Back to Risks** → Auto-filtered to Infrastructure + Security

## Reporting benefits

### Focused views

Users can quickly answer:
- "What risks am I responsible for?"
- "What's happening in my business area?"
- "What actions are overdue for my team?"
- "Which milestones are at risk in my portfolio?"

### Cross-cutting visibility

- See items across multiple preferred areas
- Spot cross-area dependencies
- Identify systemic issues
- Track portfolio-wide themes

### Team coordination

Managers can:
- Filter by team member to review workload
- See all items for their business area
- Identify unassigned items
- Balance work distribution

## Future enhancements

### Advanced filtering

1. **Status filters** - Filter by open/closed/treating status
2. **Date range** filters - Due date, created date ranges
3. **Risk score ranges** - High/medium/low risk filters
4. **Multi-select filters** - Multiple products, multiple areas
5. **Saved filters** - Save common filter combinations
6. **Filter presets** - Quick filters (e.g., "High risks due this month")

### Personalization

1. **Default sort** preference - User chooses preferred sort order
2. **Column selection** - Show/hide columns
3. **View density** - Compact/comfortable/spacious
4. **Notification preferences** - Email alerts for assigned items
5. **Dashboard cards** - Personalized metrics on homepage

### Bulk operations

From filtered views:
- Bulk assign to user
- Bulk update business area
- Bulk status change
- Bulk export

## Implementation status

✅ **Database:**
- UserPreferences table created
- Migration applied
- Foreign keys configured

✅ **Models:**
- UserPreference model
- FilterViewModel
- Helper methods

✅ **Controllers:**
- UserController for preferences
- Filter logic pattern documented

⏳ **Views (to be implemented):**
- Enhanced Risk index with filters
- Enhanced Issue index with filters
- Enhanced Action index with filters
- Enhanced Milestone index with filters

⏳ **Testing required:**
- Filter combinations
- Preference application
- "Assigned to me" accuracy
- Performance with large datasets

## Accessibility

All filter controls:
- Proper labels
- Keyboard navigable
- Screen reader compatible
- Clear indication of state
- GOV.UK design patterns

Filter sections:
- Can be collapsed/expanded
- Skip links available
- Focus management
- ARIA labels

## Documentation

**Related documentation:**
- `FILTERING_AND_PERSONALIZATION.md` (this file)
- `BUSINESS_AREA_FEATURE.md`
- `RAID_CLASSIFICATION_COMPLETE.md`

## Migration history

**AddUserPreferences** (20251017204059)
- Created UserPreferences table
- One-to-one relationship with Users
- Cascade delete

---

**Created:** 17 October 2025  
**Migration:** 20251017204059_AddUserPreferences  
**Status:** Database and models complete, views in progress  
**Version:** 1.0 (initial implementation)

