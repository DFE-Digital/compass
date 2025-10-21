# Dashboard feature

## Overview

The Compass homepage has been transformed into a personalised, filterable dashboard that aggregates all items assigned to or owned by the logged-in user. Using a left sidebar/right content layout similar to the Risk index page, the dashboard provides a flexible view of everything the user is responsible for across the platform.

## Layout

The dashboard uses a **col-md-3/col-md-9 layout**:
- **Left sidebar (col-md-3)**: Navigation and filter controls
- **Right content area (col-md-9)**: Selected view with filtered data

## Features

### Left sidebar

**View navigation**
- Overview (summary cards)
- Products (with count)
- Issues (showing open issues count)
- Risks (showing active risks count)
- Actions (showing total count)
- Milestones (showing total count)

**Dynamic filters** (shown when viewing specific item types):
- **Time period**: All time, Overdue, Due today, Due this week, Due this month, Due next month
- **Status filter**: Context-sensitive based on selected view (e.g. open/in progress/blocked for issues)
- **Priority/Severity filter**: Context-sensitive (e.g. critical/high/medium/low for issues)

**Summary stats card**
- Quick reference showing counts for all categories
- Always visible regardless of current view

### Right content area - Overview

When viewing the overview, summary cards display:

- **My products**: Total count, clickable card linking to products view
- **Open/total issues**: Open issue count out of total, clickable card
- **Active/total risks**: Active risk count out of total, clickable card
- **Urgent/total actions**: Urgent (due within 7 days) out of total, clickable card
- **Urgent/total milestones**: Urgent (due within 7 days) out of total, clickable card

### Right content area - Filtered views

When a specific view is selected, the content area displays filtered tables for:

1. **Products view** - Products where the user is a product contact
   - Shows FIPS ID, title, phase, and user's role
   - Quick action buttons to view related risks, issues, actions, and milestones for each product
   - Link to the full product reporting area
   - Link to add new products
   - No filters (products list is unfiltered)

2. **Issues view** - Issues owned by the user
   - Filterable by: status, severity, time period
   - Displays severity, status, and target resolution date
   - Highlights overdue issues in red
   - Shows blocking status
   - Links to edit each issue
   - Link to add new issue

3. **Risks view** - Risks where the user is the owner (by email)
   - Filterable by: status, risk score, time period
   - Shows risk score, impact, likelihood, and status
   - Colour-coded by risk severity
   - Highlights overdue risks
   - Links to edit each risk
   - Link to add new risk

4. **Actions view** - Actions assigned to the user
   - Filterable by: status, priority, time period
   - Displays priority, status, and due date
   - Highlights overdue actions in red
   - Highlights urgent actions (due within 7 days) in amber
   - Links to edit each action
   - Link to add new action

5. **Milestones view** - Milestones owned by the user
   - Filterable by: status, time period
   - Shows status, progress percentage, and due date
   - Visual progress bars
   - Highlights overdue and urgent milestones
   - Links to edit each milestone
   - Link to add new milestone

## User experience

- **Personalised greeting**: Displays the user's name in the header
- **View-based navigation**: Click items in left sidebar to switch between views
- **Context-sensitive filters**: Filters adapt based on the selected view
- **Empty states**: Helpful messages when no items match the current filters
- **Visual indicators**: Colour coding highlights urgent, overdue, and blocked items
- **Clear filters button**: Appears when filters are applied, one click to reset
- **Auto-submit filters**: Filter dropdowns automatically refresh the view
- **Badge counts**: Each view navigation item shows the count for that category
- **Quick actions**: Buttons in card headers to view full lists or add new items
- **Responsive design**: Uses AdminLTE col-md-3/col-md-9 layout pattern

## Technical implementation

### Files created/modified

- **Models/DashboardViewModel.cs**: New view model with all dashboard data and summary calculations
- **Controllers/HomeController.cs**: Updated to fetch personalised data for the current user
- **Views/Home/Index.cshtml**: Complete redesign as an interactive dashboard

### Data fetching

The controller accepts parameters:
- `view`: Determines which view to display (overview/products/issues/risks/actions/milestones)
- `statusFilter`: Filters items by status
- `priorityFilter`: Filters items by priority/severity/risk score
- `dateFilter`: Filters items by time period (all/overdue/today/week/month/next_month)

The controller:
1. Identifies the current user by email from authentication
2. Fetches products from the CMS API and filters for product contacts matching the user's email
3. Builds filtered queries for issues, risks, actions, and milestones based on the selected view and filter parameters
4. Applies appropriate filters only when viewing the specific item type
5. Limits results to 100 items per category (sufficient for dashboard use)
6. Calculates summary statistics for all views (counts, urgent items, etc.)

### Performance

- Products are cached by the ProductsApiService
- Database queries use appropriate indexes on owner/assignee fields
- Filters are applied at the database level for efficiency
- Results are limited to 100 items per category
- Includes related Objective data with `.Include()` for efficiency
- Date filtering uses expression trees for optimal query generation

## User identification

The dashboard identifies users by email address:

- **Products**: Matches `product_contacts.users_permissions_user.email` from CMS
- **Issues**: Matches `OwnerUserId` linked to Compass User table
- **Risks**: Matches `OwnerEmail` field directly (legacy field)
- **Actions**: Matches `AssignedToUserId` linked to Compass User table
- **Milestones**: Matches `OwnerUserId` linked to Compass User table

## Filter behaviour

### Time period filters

- **All time**: No date filtering applied
- **Overdue**: Items with due dates in the past
- **Due today**: Items due within the current day
- **Due this week**: Items due within the next 7 days
- **Due this month**: Items due within the next 30 days  
- **Due next month**: Items due between 30-60 days from now

### Status filters

Context-sensitive options based on item type:
- **Issues**: open, in progress, blocked, resolved, closed
- **Risks**: new, open, treating, monitoring, closed
- **Actions**: not started, in progress, blocked, done, cancelled
- **Milestones**: not started, on track, at risk, delayed, complete, cancelled

### Priority filters

Context-sensitive options:
- **Issues**: Filters by severity (critical, high, medium, low)
- **Risks**: Filters by risk score ranges (critical 15-25, high 10-14, medium 5-9, low 1-4)
- **Actions**: Filters by priority (high, medium, low)

## URL structure

The dashboard uses query parameters for state:
```
/?view=risks&statusFilter=open&priorityFilter=high&dateFilter=overdue
```

This allows:
- Direct linking to specific filtered views
- Browser back/forward navigation
- Bookmarking of useful filter combinations

## Future enhancements

Potential improvements could include:

- Saved filter presets
- Custom date range picker
- Charts and graphs for trend analysis
- Column sorting within tables
- Export functionality for filtered results
- Email digests of dashboard contents
- Team dashboards aggregating multiple users
- Search/text filtering within views

