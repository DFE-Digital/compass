# Risks report improvements summary

## Overview

The Risks report view (`Views/Reports/Risks.cshtml`) has been significantly improved to provide better usability, more comprehensive data visibility, and a cleaner layout.

## Key improvements

### 1. Layout redesign

**Before:** Horizontal button group for grouping, full-width results
**After:** Sidebar + content layout (like dashboard)

**Benefits:**
- **Left sidebar** (col-xxl-2 col-xl-2 col-lg-3 col-md-4): Vertical navigation for group-by options
- **Right content** (col-xxl-10 col-xl-10 col-lg-9 col-md-8): Results display
- Cleaner navigation with icons
- Active state clearly indicated
- More space-efficient

### 2. Impact & Likelihood matrix improvements

**Before:**
- Cells showed count, score, and "View" button
- Unequal column widths
- Cluttered appearance

**After:**
- Count only, displayed as clickable badge
- Equal column widths (16.66% each)
- Cleaner, more scannable
- Badge colour-coded by risk level
- Hover effects on badges
- Shows "0" for empty cells

**CSS enhancements:**
```css
.risk-matrix-table {
    table-layout: fixed; /* Equal column widths */
}

.risk-matrix-cell {
    padding: 1.5rem 0.5rem;
    min-height: 80px;
}

.risk-matrix-cell .badge:hover {
    transform: scale(1.1);
    box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
}
```

### 3. Business area grouping improvements

**Before:**
- Card-based layout
- Only showed areas with risks
- Manual sorting

**After:**
- Clean table layout
- **Shows ALL business areas** (even with 0 risks)
- Sorted by **total risks first, then high risks** (descending)
- Columns: Business area | Total | High | Medium | Low
- Total count is clickable badge linking to modal
- High/Medium/Low shown as colour-coded badges
- Shows "-" for zero counts

**Benefits:**
- Easy to identify which areas have most risks
- Complete visibility (no hidden areas)
- Consistent sorting makes patterns clear
- More compact (fits more on screen)

### 4. Product grouping improvements

**Before:**
- Card-based layout
- Showed all products
- Individual product cards

**After:**
- Clean table layout
- **Only shows products WITH risks** (not empty products)
- Sorted by total risks, then high risks (descending)
- Columns: Product | Total | High | Medium | Low | Actions
- Product name with FIPS ID as subtitle
- "View product" external link button
- Includes "No product assigned" row at bottom if applicable

**Benefits:**
- Focus on products that need attention
- Easy to see which products have most risk
- Quick link to product details
- More compact display

### 5. Risk type grouping improvements

**Before:**
- Card-based layout
- Only showed types with risks
- Inconsistent sorting

**After:**
- Clean table layout
- **Shows ALL risk types** (even with 0 risks)
- Alphabetically sorted
- Same column structure as other tables
- Total count as clickable badge
- Includes "No type assigned" row if applicable

**Benefits:**
- Complete visibility of risk types
- Easy to compare counts
- Identify unclassified risks

### 6. Risk tier grouping improvements

**Before:**
- Card-based layout
- Only showed tiers with risks

**After:**
- Clean table layout
- **Shows ALL risk tiers** (even with 0 risks)
- Alphabetically sorted
- Consistent with other table views
- Includes "No tier assigned" row if applicable

**Benefits:**
- Complete visibility
- Easy tier comparison
- Identify unclassified risks

### 7. Status grouping improvements

**Before:**
- Card-based layout
- Only showed statuses in use

**After:**
- Clean table layout
- **Shows ALL statuses** (new, open, treating, monitoring, closed)
- Status name capitalised
- Total badge colour matches status (red for new/open, amber for treating, blue for monitoring, green for closed)
- Same column structure

**Benefits:**
- Complete status overview
- Easy to see distribution
- Colour-coded for quick understanding

### 8. Summary cards improvement

**Before:** Small-box cards
**After:** Info-box cards

More modern, consistent with AdminLTE design system.

## Technical changes

### Files modified

1. **`Views/Reports/Risks.cshtml`** (~720 lines)
   - Sidebar navigation layout
   - Improved impact/likelihood matrix
   - Table-based groupings for all views
   - Consistent badge styling
   - Clickable badges for filtering

2. **`Controllers/ReportsController.cs`** (Lines 627-680)
   - Added BusinessAreas to ViewBag
   - Added RiskTypes to ViewBag
   - Added RiskTiers to ViewBag
   - Data loaded from database for complete lists

3. **`wwwroot/css/custom.css`** (Lines 124-188)
   - Risk matrix table styling
   - Equal column widths
   - Cell padding and min-height
   - Badge hover effects
   - Mobile responsiveness

### Controller data changes

The `Risks` action now provides:
```csharp
ViewBag.BusinessAreas  // All distinct business areas
ViewBag.RiskTypes      // All risk types from lookup
ViewBag.RiskTiers      // All risk tiers from lookup
ViewBag.Products       // All products
```

This ensures views can show complete lists even for items with 0 count.

## Usability improvements

### Before
- Hard to compare different groupings
- Cards wasted vertical space
- Missing information (empty categories not shown)
- Inconsistent layouts between views
- Difficult to identify priorities

### After
- Consistent table layout across all views
- Complete data visibility
- Smart sorting (most risks at top)
- Colour-coded for quick scanning
- Compact, space-efficient
- Clickable badges for drilling down
- Clear visual hierarchy

## Accessibility

- Semantic table structure
- Proper heading hierarchy
- ARIA labels maintained
- Keyboard navigation works
- Screen reader friendly
- Colour not sole indicator (badges have text)

## Mobile responsiveness

- Tables scroll horizontally on small screens
- Sidebar becomes full-width on mobile
- Risk matrix font size reduces
- Badge sizes adjust appropriately
- All functionality maintained

## Future enhancements

Potential improvements (not included):
1. **Export functionality**: Download as CSV/Excel
2. **Drill-down filters**: Click badge to go to filtered Risk/Index view
3. **Trend indicators**: Show if counts increasing/decreasing
4. **Date range filter**: View risks by date range
5. **Print-friendly version**: Optimised for PDF export
6. **Chart visualisations**: Bar/pie charts for groupings

## Testing recommendations

1. **Test each grouping:**
   - Impact & Likelihood matrix
   - Business area table
   - Product table
   - Risk type table
   - Risk tier table
   - Status table

2. **Test edge cases:**
   - No risks in database
   - All risks in one category
   - Business areas with 0 risks
   - Products with no risks
   - Unassigned risks (no type, tier, product, or business area)

3. **Test interactions:**
   - Click badges to view risk details in modal
   - Click product external link
   - Switch between groupings
   - Mobile responsive behaviour

4. **Test sorting:**
   - Business areas sorted correctly (most risks first)
   - Products sorted correctly (most risks first)
   - Types/Tiers/Statuses in correct order

## Summary

The Risks report is now:
- ✅ More professional and modern
- ✅ Complete data visibility (no hidden categories)
- ✅ Better sorted for priority identification
- ✅ Consistent layout across all views
- ✅ More compact and scannable
- ✅ Easier to use and understand
- ✅ Mobile responsive
- ✅ Zero linting errors

Users can now quickly identify:
- Which products/business areas have most risks
- Complete risk distribution across all categories
- High-risk areas requiring attention
- Categories with zero risks (gaps in classification)

The improvements make the report a more effective tool for risk management and decision-making.

