# Settings navigation enhancement

## Overview

The Settings section now includes a persistent sidebar navigation that makes it easy to switch between different lookup types and provides visual context about what's being managed.

## Layout structure

### Two-column grid layout

All Settings pages now use:
- **col-md-2** - Left sidebar with navigation
- **col-md-10** - Main content area

This creates a consistent, application-like interface for settings management.

### Sidebar navigation (_SettingsSidebar.cshtml)

**Location:** `Views/Admin/Settings/_SettingsSidebar.cshtml`

**Features:**
1. **Lookup types card** (collapsible)
   - Risk types (13 badge)
   - Risk tiers (5 badge)
   - Action sources (4 badge)
   - Active state highlighting
   - Icons for each type

2. **Quick actions card**
   - Settings home link
   - Back to admin link

**Active state detection:**
```csharp
var currentAction = ViewContext.RouteData.Values["Action"]?.ToString() ?? "";
var isRiskTypes = currentAction.Contains("RiskType");
var isRiskTiers = currentAction.Contains("RiskTier");
var isActionSources = currentAction.Contains("ActionSource");
```

The active link is highlighted automatically based on the current route.

## Sidebar HTML structure

```html
<div class="card">
    <div class="card-header">
        <h3 class="card-title">Lookup types</h3>
        <div class="card-tools">
            <button type="button" class="btn btn-tool" data-card-widget="collapse">
                <i class="fas fa-minus"></i>
            </button>
        </div>
    </div>
    <div class="card-body p-0">
        <ul class="nav nav-pills flex-column">
            <li class="nav-item">
                <a href="@Url.Action("RiskTypes", "Admin")" class="nav-link @(isRiskTypes ? "active" : "")">
                    <i class="fas fa-tags"></i> Risk types
                    <span class="badge bg-info float-right">13</span>
                </a>
            </li>
            <!-- ... more items ... -->
        </ul>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h3 class="card-title">Quick actions</h3>
    </div>
    <div class="card-body">
        <a href="@Url.Action("Settings", "Admin")" class="btn btn-outline-primary btn-block btn-sm">
            <i class="fas fa-home"></i> Settings home
        </a>
        <a href="@Url.Action("Users", "Admin")" class="btn btn-outline-secondary btn-block btn-sm mt-2">
            <i class="fas fa-arrow-left"></i> Back to admin
        </a>
    </div>
</div>
```

## Pages updated with sidebar

### Main settings pages (3)
1. ✅ `Settings/Index.cshtml` - Settings dashboard with overview
2. ✅ `Settings/RiskTypes.cshtml` - Risk types list
3. ✅ `Settings/RiskTiers.cshtml` - Risk tiers list
4. ✅ `Settings/ActionSources.cshtml` - Action sources list

### Create/Edit/Delete pages (9)
1. ✅ `Settings/CreateRiskType.cshtml`
2. ✅ `Settings/EditRiskType.cshtml`
3. ✅ `Settings/DeleteRiskType.cshtml`
4. ✅ `Settings/CreateRiskTier.cshtml`
5. ✅ `Settings/EditRiskTier.cshtml`
6. ✅ `Settings/DeleteRiskTier.cshtml`
7. ✅ `Settings/CreateActionSource.cshtml`
8. ✅ `Settings/EditActionSource.cshtml`
9. ✅ `Settings/DeleteActionSource.cshtml`

**Total:** 13 views updated ✅

## Visual design

### Navigation badges

Each lookup type shows a count badge:
- **Risk types:** Blue badge (13 types)
- **Risk tiers:** Green badge (5 tiers)
- **Action sources:** Yellow/warning badge (4 sources)

### Active state

The current page's navigation item is highlighted:
- Different background color
- Visual indication of location
- Maintains context during CRUD operations

### Collapsible panels

Both sidebar cards can be collapsed:
- Users can maximize content area
- State persists during session
- Default to expanded

## Benefits

### User experience

1. **Context awareness** - Always know where you are
2. **Quick switching** - One click to switch between lookup types
3. **Visual feedback** - Count badges show data at a glance
4. **Consistent layout** - Same structure across all pages
5. **Easy navigation** - No need to go back to index

### Scalability

**Adding new lookup types** is straightforward:
1. Add new model and migration
2. Add admin CRUD actions
3. Create views with sidebar partial
4. Add one link to `_SettingsSidebar.cshtml`
5. Update badge count

**Example future additions:**
- Issue types/categories
- Action priorities  
- Milestone statuses
- Response strategies
- Impact/likelihood scales

### Maintainability

**Single source of truth:**
- Sidebar defined once in `_SettingsSidebar.cshtml`
- Changes propagate to all 13 pages
- Consistent icons and labels
- Easy to update counts

## Navigation flow examples

### Example 1: Managing risk types
1. Navigate to **Admin > Settings**
2. Click **Risk types** in sidebar (or info box)
3. Click **Create new**
4. Sidebar remains visible - can switch to Risk tiers
5. Fill form and save
6. Back to Risk types list - sidebar still present
7. Click **Risk tiers** in sidebar
8. Now viewing Risk tiers without returning to index

### Example 2: Editing multiple lookups
1. On **Risk types** page
2. Edit a risk type
3. After saving, click **Action sources** in sidebar
4. Edit an action source
5. Click **Risk tiers** in sidebar
6. Quick navigation between all lookup types

## Responsive design

### Desktop (md and above)
- Sidebar: 2 columns (16.67% width)
- Content: 10 columns (83.33% width)
- Sidebar fully visible

### Tablet/Mobile (sm and below)
- Sidebar: Stacks on top
- Content: Full width below
- Both collapsible for space

## Accessibility

**Sidebar navigation:**
- Proper semantic nav-pills
- Keyboard navigable
- Screen reader compatible
- ARIA roles where needed
- Clear focus indicators

**Quick actions:**
- Large touch targets
- Clear labels
- Icons with text
- Block-level buttons

## Icon choices

Following AdminLTE conventions:
- **Risk types:** `fa-tags` (classification taxonomy)
- **Risk tiers:** `fa-layer-group` (hierarchical levels)
- **Action sources:** `fa-sitemap` (source/origin tracking)
- **Settings home:** `fa-home`
- **Back:** `fa-arrow-left`

## Future enhancements

### Dynamic counts

Instead of hardcoded badge numbers, fetch from database:

```csharp
ViewBag.RiskTypeCount = await _context.RiskTypes.CountAsync();
ViewBag.RiskTierCount = await _context.RiskTiers.CountAsync();
ViewBag.ActionSourceCount = await _context.ActionSources.CountAsync();
```

Display in sidebar:
```html
<span class="badge bg-info float-right">@ViewBag.RiskTypeCount</span>
```

### Active/inactive indicator

Show count of active vs total:
```html
<span class="badge bg-info float-right">11/13</span>
```

### Search within settings

Add search box to sidebar:
- Filter lookup items by name/code
- Quick find across all types
- Jump to specific item

### Recent edits

Show recently edited items:
- Quick access to frequently modified lookups
- Timestamp of last edit
- Direct links

### Favourites/pinned items

Allow admins to pin:
- Frequently accessed lookup types
- Important settings
- Quick access section

## Comparison: Before vs After

### Before
- Settings index with card grid
- Click card to go to list
- Click "Back to settings" to return
- No navigation between types
- Multiple clicks to switch types

### After
- Settings with sidebar navigation
- One-click access to any type
- Always visible navigation
- Context maintained
- Efficient multi-type management
- Count badges at a glance

## Settings dashboard improvements

The Settings index page now includes:

1. **Sidebar navigation** (same as other settings pages)
2. **Info boxes** with counts and quick links
3. **Overview content** explaining lookup values
4. **Management instructions**
5. **Tip about using sidebar**

## Documentation

Related documentation:
- `SETTINGS_AND_LOOKUPS.md` - Overall settings system
- `SETTINGS_NAVIGATION_UPDATE.md` - This file (sidebar navigation)
- Risk/Action specific feature docs

## Implementation checklist

✅ Created `_SettingsSidebar.cshtml` partial view  
✅ Updated Settings Index with sidebar and info boxes  
✅ Updated Risk Types list view with sidebar  
✅ Updated Risk Types Create/Edit/Delete views  
✅ Updated Risk Tiers list view with sidebar  
✅ Updated Risk Tiers Create/Edit/Delete views  
✅ Updated Action Sources list view with sidebar  
✅ Updated Action Sources Create/Edit/Delete views  
✅ All 13 views now use consistent layout  
✅ Build succeeds with 0 errors  

## User feedback

Benefits for administrators:
- **Faster navigation** - Less clicking to manage lookups
- **Better context** - Always see what's available
- **Visual counts** - Know how many entries exist
- **Consistent experience** - Same layout everywhere
- **Scalable** - Easy to add new lookup types

---

**Created:** 17 October 2025  
**Views updated:** 13 (1 index + 3 lists + 9 CRUD)  
**Layout:** col-md-2 sidebar + col-md-10 content  
**Build status:** ✅ Successful  
**Version:** 1.0

