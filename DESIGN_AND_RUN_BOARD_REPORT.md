# Design and Run Board report

## Overview

The Design and Run Board report is a comprehensive reporting solution that combines data from multiple sources to provide a complete view of product status, accessibility compliance, and operational performance metrics.

## Data sources

The report integrates data from:

1. **FIPS product completion data** (from CMS API)
   - Phase assignment
   - Business area assignment
   - Product contacts
   - Product URL
   - User groups

2. **Accessibility management**
   - Enrollment status (from `ProductAccessibility`)
   - Accessibility issues count and status (from `AccessibilityIssue`)
   - Compliance status (compliant/partially compliant/non-compliant)
   - WCAG version and level

3. **Operational performance metrics**
   - perf-ux-1: User experience metric
   - perf-acc-3: Accessibility performance metric
   - Latest submitted values from product returns

## Features

### Summary cards

- Total products in portfolio
- Average FIPS completion percentage
- Number of products enrolled in accessibility management
- Total open accessibility issues

### Business area summary

Breakdown by business area showing:
- Product count per area
- Average completion percentage
- Accessibility enrollment count
- Open accessibility issues count

### Product details table

Comprehensive table with the following columns:
- Product name and FIPS ID
- Phase
- Business area
- FIPS completion percentage
- Accessibility enrollment and compliance status
- Open accessibility issues count
- User experience metric value (perf-ux-1)
- Accessibility metric value (perf-acc-3)

### Filtering capabilities

- Search by product name or FIPS ID
- Filter by business area
- Filter by accessibility status (enrolled/not enrolled/has issues/compliant)
- Filter by FIPS completion percentage ranges

### Sorting

All columns are sortable with visual indicators showing current sort direction.

## Implementation

### Files created

1. **ViewModel**: `/compass/Models/DesignAndRunBoardViewModel.cs`
   - `DesignAndRunBoardViewModel`: Main view model
   - `DesignAndRunBoardItem`: Individual product data
   - `BusinessAreaSummary`: Business area aggregation

2. **Controller action**: `/compass/Controllers/ReportsController.cs`
   - New action: `DesignAndRunBoard()`
   - Fetches and aggregates data from multiple sources

3. **View**: `/compass/Views/DdtReports/DesignAndRunBoard.cshtml`
   - Responsive design with filtering and sorting
   - Colour-coded status indicators
   - Follows DfE design patterns [[memory:8532625]]

4. **Navigation**: Updated `/compass/Views/DdtReports/Index.cshtml`
   - Added link to new report in Organisation Reports section

## Database dependencies

The report relies on the following database tables:
- `ProductAccessibilities`: Accessibility enrollment data
- `AccessibilityIssues`: Issue tracking
- `PerformanceMetrics`: Metric definitions
- `ProductReturns`: Monthly performance data submissions
- `ProductMetricValues`: Submitted metric values

## Performance metrics setup

For the report to display operational metrics, ensure the following performance metrics exist in the database:

### perf-ux-1
- **Identifier**: `perf-ux-1`
- **Title**: User experience metric (e.g., "User satisfaction score")
- **Value type**: Decimal or Percentage
- **Description**: Measures user experience quality

### perf-acc-3
- **Identifier**: `perf-acc-3`
- **Title**: Accessibility performance metric (e.g., "Accessibility compliance score")
- **Value type**: Decimal or Percentage
- **Description**: Tracks accessibility performance

If these metrics don't exist, the report will display "No data" for these columns and show a warning message.

## How to add performance metrics

1. Navigate to **Admin** → **Performance metrics**
2. Click **Create new metric**
3. Set the identifier to `perf-ux-1` or `perf-acc-3`
4. Configure title, description, and validation rules
5. Set applicable phases
6. Save the metric

## Usage

### Accessing the report

1. From the COMPASS home page, navigate to **DDT Reports**
2. Click **View Design and Run Board** under Organisation Reports
3. Or access directly at `/Reports/DesignAndRunBoard`

### Interpreting compliance status

- **Compliant**: No open accessibility issues (but has resolved issues)
- **Partially compliant**: 1-5 open accessibility issues
- **Non-compliant**: More than 5 open accessibility issues
- **No issues recorded**: Enrolled but no issues logged yet
- **Not enrolled**: Product not enrolled in accessibility management

### Colour coding

#### FIPS completion
- Green (≥80%): Good progress
- Yellow (60-79%): Needs attention
- Red (<60%): Requires immediate action

#### Accessibility issues
- Green: 0 open issues
- Yellow: 1-5 open issues
- Red: >5 open issues

## Technical notes

- All data is fetched in a single request for performance
- Uses Entity Framework Core with Include for efficient loading
- Client-side filtering and sorting for responsive interaction
- British English spelling throughout [[memory:8956275]]
- Follows GOV.UK/DfE design patterns

## Future enhancements

Potential improvements for future iterations:
- Export to CSV/Excel
- Historical trend analysis
- Email report scheduling
- Additional operational metrics
- Drill-down to product details
- Custom metric selection

