# Filtering and dimensions feature implementation

## Overview

This document describes the comprehensive filtering and dimensions feature implemented across the Risks, Issues, Actions, and Milestones index pages in the Compass application.

## Features implemented

### 1. Summary cards

Each index page now displays three summary cards at the top showing:
- **Open count**: Number of active/open items
- **Closed count**: Number of completed/resolved/closed items  
- **Overdue count**: Number of items past their due/proximity/target resolution date

### 2. Dimension filters

Users can view items by different perspectives:
- **All items**: Default view showing all items
- **Assigned to me**: Shows only items assigned to/owned by the current user
- **My business area**: Shows items from the user's preferred business areas (as set in MySettings)

### 3. Standard filters

All four index pages support:
- **Business area**: Filter by specific business area
- **Status**: Filter by item status (varies by type)
- **Products**: Multi-select filter showing only products that have items assigned
  - Shows distinct FIPS IDs from the database (not all products from CMS API)
  - Only displays products that are actually in use for that type
  - For example, Risk index shows only FIPS IDs that have risks associated with them

### 4. Type-specific filters

#### Risks
- **Min/Max risk score**: Filter risks by score range (1-25)
- **Impact**: Filter by impact rating (1-5)
- **Likelihood**: Filter by likelihood rating (1-5)

#### Issues
- **Severity**: Filter by severity level (low, medium, high, critical)
- **Priority**: Filter by priority (low, medium, high)

#### Actions
- **Action source**: Filter by the source/origin of actions

#### Milestones
- Standard filters only (no type-specific filters)

### 5. Tabbed view

Each index page has two tabs:
- **Active tab**: Shows items that are not completed/closed
- **Closed tab**: Shows completed/resolved/closed items

Filters apply to both tabs, and the tab badges show the count for each tab.

### 6. Multi-select product filter

The products filter uses Select2 for enhanced UX, allowing users to:
- Search for products by typing
- Select multiple products at once
- Clear selections easily

## Technical implementation

### Controllers updated

All four controllers (Risk, Issue, Action, Milestone) have been updated with:
- Filter parameters in the Index action
- User context retrieval for "assigned to me" filtering
- UserPreferences lookup for "my business area" filtering
- Query filtering based on all parameters
- Summary count calculations
- ViewBag population for filter dropdowns and current selections

### Views updated

All four index views have been updated with:
- Summary cards section displaying counts (full width at top)
- Two-column layout with filters on left (col-md-4) and table on right (col-md-8)
- Filters in a sidebar card with vertical stacking for better usability
- Tab navigation with active/closed tabs integrated with the table
- Hidden form fields to preserve context (objectiveId, riskId, etc.)
- JavaScript for tab switching and Select2 initialization
- Responsive layout that adapts to smaller screens

### Filter persistence

Filters are preserved through:
- Query string parameters
- Hidden form fields for context preservation
- Tab state maintenance during filtering

### Layout structure

The index pages use a two-column layout:
- **Summary cards** (full width): Display Open, Closed, and Overdue counts at the top
- **Actions dropdown** (header right): Contains "Add [type]" and "Export to Excel" actions
- **Left sidebar (col-md-3)**: Contains all filters in a vertical stack
  - Shows active filters with badges when any filters are applied
  - Products dropdown shows count of available products for debugging
- **Right column (col-md-9)**: Contains the tabs and data table
  - Table columns reduced for better fit (removed redundant/less critical columns)
  - Product and other metadata shown as sub-text under main fields
- Filters use `form-group` class for proper spacing
- Filter buttons are full-width (`btn-block`) for better mobile experience

### Table column optimizations

To improve readability and fit, table columns have been reduced:

**Risks** (10 → 6 columns):
- Removed: Product (moved under Title), Category, Impact, Likelihood (shown under Risk score)
- Kept: Title, Owner, Risk score, Status, Proximity, Actions

**Issues** (10 → 6 columns):
- Removed: Product (moved under Title), Category, Priority (shown under Severity), Detected
- Kept: Title, Owner, Severity, Status, Target resolution, Actions

**Actions** (8 → 5 columns):
- Removed: Product (moved under Title), Priority (moved under Title), Completed (moved under Due date)
- Kept: Title, Assigned to, Status, Due date, Actions

**Milestones** (8 → 6 columns):
- Removed: Product (moved under Title), Actual date (moved under Due date)
- Kept: Name, Owner, Due date, Status, Progress, Actions

## Usage

### Basic filtering

1. Navigate to any of the index pages (Risks, Issues, Actions, Milestones)
2. Use the dropdown filters at the top to select your criteria
3. Click "Apply filters" or let auto-submit filters apply immediately
4. Click "Clear filters" to reset all filters

### Dimension filtering

1. Select a dimension from the first dropdown:
   - "All items" shows everything
   - "Assigned to me" shows items you own/are assigned to
   - "My business area" shows items from your preferred business areas
2. Other filters can be combined with dimension filters

### Tab switching

1. Click on the "Active" or "Closed" tab to switch views
2. Filters are preserved when switching tabs
3. Badge counts show the total for each tab

### Multi-select products

1. Click on the Products dropdown
2. Type to search for specific products
3. Select multiple products by clicking them
4. Selected products will show as tags
5. Click the × on tags to remove them

## Status values by type

### Risks
- Active: open, treating, monitoring
- Closed: closed

### Issues
- Active: open, in_progress, blocked
- Closed: resolved, closed

### Actions
- Active: not_started, in_progress, blocked
- Closed: done, cancelled

### Milestones
- Active: not_started, on_track, at_risk, delayed
- Closed: complete, cancelled

## Dependencies

- **Select2**: Used for multi-select product dropdowns
- **Bootstrap 4**: For styling and layout
- **AdminLTE**: For summary cards (small-box component)
- **UserPreferences**: For "My business area" dimension filtering

## Notes

- Filters use GET requests so they can be bookmarked and shared
- The product filter uses FipsId for matching
- Business areas are retrieved from the ProductsApiService
- Summary counts reflect all items before filtering (except context filters like objectiveId)
- Overdue calculations use different date fields per type:
  - Risks: ProximityDate
  - Issues: TargetResolutionDate
  - Actions: DueDate
  - Milestones: DueDate

## Active filters display

When any filters are applied, a blue info box appears at the top of the filter sidebar showing:
- Each active filter as a badge
- Clear visual indication of what filtering is currently applied
- Helps users understand why they're seeing specific results

## Product filter implementation

The product filter shows FIPS IDs from the **database**, not from the CMS API:
- Each controller queries its own table for distinct `FipsId` values
- Only shows products that actually have items (risks, issues, actions, or milestones) assigned
- This provides a more relevant filter list and avoids API dependencies
- If showing "0 available", it means no items have FIPS IDs assigned yet
- Product labels show the FIPS ID directly (e.g., "FIPS-001")

**Why this approach:**
1. The CMS API may not always be available or configured
2. Shows only relevant products that have data
3. Faster (no external API call, uses existing database query)
4. More accurate for filtering purposes

## "Assigned to me" section

Each index page displays a dedicated section above the filters/table showing items assigned to the current user:
- **Risks**: Top 10 risks by risk score (excluding closed)
- **Issues**: Top 10 issues by severity (excluding resolved/closed)
- **Actions**: Top 10 actions by priority then due date (excluding done/cancelled)
- **Milestones**: Top 10 milestones by due date (excluding complete/cancelled)

This section:
- Only appears if the user has items assigned to them
- Is independent of the filters applied to the "All [type]" table below
- Shows a compact table with essential columns only
- Uses smaller buttons (`btn-xs`) for actions
- Has a badge showing the count of items

## Risk-specific features

### "New" status
Risks now have a dedicated "New" status for uncategorized risks:
- Default status when creating a risk is "new"
- A risk is considered "New" if its status is "new" OR if it has no Category assigned
- New risks have their own summary card (orange/warning background)
- New risks appear in the Active tab
- The "New" status helps identify risks that need initial categorization

### Risk status values
- **new**: Newly added, not yet categorized
- **open**: Active risk being monitored
- **treating**: Risk mitigation in progress
- **monitoring**: Risk under observation
- **closed**: Risk resolved or no longer relevant

## Build notes

During implementation, Razor compilation required:
- Using `selected="@(condition)"` syntax instead of conditional `? "selected" : ""` for option elements
- Casting the `currentProducts` array to `IEnumerable<string>` to use LINQ Contains method
- Changed products from `List<dynamic>` to `List<string>` after switching from API to database source
- Added product count label for debugging: `Products (@products.Count available)`

## Foreign key constraint fix

Fixed FOREIGN KEY constraint errors in Create/Edit actions across all controllers:
- Added null checks for nullable foreign key fields (ObjectiveId, OwnerUserId, RiskTierId, ActionSourceId, etc.)
- When a foreign key value is 0 (empty select), it's now set to null instead
- This prevents SQLite "FOREIGN KEY constraint failed" errors
- Applied to all Create and Edit actions in Risk, Issue, Action, and Milestone controllers

## Future enhancements

Potential improvements for consideration:
- Save filter preferences per user
- Export filtered results to CSV/Excel
- Advanced date range filtering
- Saved filter templates
- Filter by multiple statuses simultaneously
- Real-time filter updates without page reload

