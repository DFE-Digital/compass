# Performance reporting management - admin functions

## Overview

Admin functions for managing monthly performance reporting metrics in Compass. These tools allow Admin and Super admin users to configure reporting requirements, set exclusions, manage due dates, and perform bulk operations.

## Access

**Route**: `/Admin/PerformanceReportingManagement`

**Permissions**: Super Admin or Central Operations Admin group membership required

## Integration with performance reporting

The system automatically filters products based on period exclusions and business area configurations:

- **Performance Metrics list**: Products are only shown if reporting is required for the current period
- **Product History**: Only periods requiring reporting are displayed for each product
- **Reporting hierarchy** is automatically enforced

If a period is excluded and no business area override exists, **no products will appear** in the performance reporting lists for that period.

## Features

### 1. Period exclusions (base level)

Ignore entire reporting periods at the base level. Business area configurations can override these exclusions.

**Route**: `/Admin/PerformanceReportingManagement/PeriodExclusions`

**Use case**: Exclude months from reporting due to system migrations, holiday periods, or other organisation-wide reasons.

**Override logic**: If a business area is configured to report for an excluded period, products in that business area will still be required to report (business area configs take precedence).

**Database table**: `PerformanceReportingPeriodExclusions`

### 2. Business area reporting configuration

Configure when reporting metrics are applicable for different business areas/portfolios.

**Route**: `/Admin/PerformanceReportingManagement/BusinessAreaConfig`

**Features**:
- Select business areas from CMS (via API integration)
- Set applicable from/until dates
- Override period-level exclusions
- Add notes explaining reporting requirements
- Support for indefinite applicability

**Override logic**: Business area configurations override period exclusions. If a period is excluded but a business area is configured to report, products in that business area must still report.

**Database table**: `PerformanceReportingBusinessAreaConfigs`

### 3. Product exclusions

Exclude specific products from performance reporting requirements.

**Route**: `/Admin/PerformanceReportingManagement/ProductExclusions`

**Features**:
- Select products from dropdown (populated from FIPS API)
- Specify exclusion reason (required)
- Set exclusion period (from/until dates)
- Support for indefinite exclusions
- Products excluded won't be required to submit metrics

**Database table**: `PerformanceReportingProductExclusions`

### 4. Due date calendar overrides

Set forward-look calendar for when monthly performance metrics are due, overriding the default "3rd working day" rule.

**Route**: `/Admin/PerformanceReportingManagement/DueDateOverrides`

**Features**:
- Create specific due dates for reporting periods
- Override the automatic 3rd working day calculation
- Add reasoning for overrides (e.g., "Bank holiday adjustment")
- Active/inactive status management
- Automatic integration with `ReturnStatusService`

**Database table**: `PerformanceReportingDueDateOverrides`

### 5. Bulk deletion

Remove all metric submissions for a given month across all products.

**Route**: `/Admin/PerformanceReportingManagement/BulkDelete`

**Features**:
- View all reporting periods with submission counts
- Delete all submissions for a selected month
- Resets return status to "Upcoming"
- Confirmation modal to prevent accidental deletions
- Audit logging of all deletions

## Reporting hierarchy and override logic

The system uses a hierarchical approach to determine reporting requirements:

```
1. Period exclusion (base level)
   ↓ If period is excluded, no reporting required
   
2. Business area configuration (overrides period exclusions)
   ↓ If business area is configured to report, reporting IS required
   
3. Product exclusions (final override)
   ↓ If product is specifically excluded, no reporting required
```

### Examples

**Example 1: Period excluded, no business area config**
- December 2025 is marked as excluded (system migration)
- No business areas configured for December 2025
- Result: **No products need to report for December 2025**

**Example 2: Period excluded, but business area configured**
- December 2025 is marked as excluded
- "Infrastructure" business area is configured to report from December 2025
- Result: **Infrastructure products must report, other business areas do not**

**Example 3: Period not excluded, product excluded**
- January 2026 is not excluded
- Product ABC-123 is excluded from January 2026
- Result: **All products report except ABC-123**

## Database schema

### PerformanceReportingPeriodExclusions

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| Year | int | Year of reporting period |
| Month | int | Month of reporting period (1-12) |
| Reason | string(1000) | Reason for exclusion |
| Notes | string(2000) | Optional additional notes |
| IsActive | bool | Whether exclusion is active |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |
| CreatedBy | string(255) | Creator email |
| UpdatedBy | string(255) | Last updater email |

**Indexes**:
- Unique index on (Year, Month)
- Index on IsActive

### PerformanceReportingDueDateOverrides

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| ReportingYear | int | Year of reporting period |
| ReportingMonth | int | Month of reporting period (1-12) |
| DueDate | DateTime | Override due date |
| Reason | string(500) | Optional reason for override |
| IsActive | bool | Whether override is active |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |
| CreatedBy | string(255) | Creator email |
| UpdatedBy | string(255) | Last updater email |

**Indexes**:
- Unique index on (ReportingYear, ReportingMonth)
- Index on IsActive

### PerformanceReportingBusinessAreaConfigs

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| BusinessAreaName | string(255) | Business area name (from CMS) |
| ApplicableFromYear | int | Applicable from year |
| ApplicableFromMonth | int | Applicable from month |
| ApplicableUntilYear | int? | Applicable until year (nullable) |
| ApplicableUntilMonth | int? | Applicable until month (nullable) |
| IsActive | bool | Whether config is active |
| Notes | string(1000) | Optional notes |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |
| CreatedBy | string(255) | Creator email |
| UpdatedBy | string(255) | Last updater email |

**Indexes**:
- Index on BusinessAreaName
- Index on IsActive

### PerformanceReportingProductExclusions

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| FipsId | string(50) | Product FIPS ID |
| ProductName | string(255) | Product name (denormalised) |
| ExclusionReason | string(1000) | Reason for exclusion |
| ExclusionFromYear | int | Exclusion from year |
| ExclusionFromMonth | int | Exclusion from month |
| ExclusionUntilYear | int? | Exclusion until year (nullable) |
| ExclusionUntilMonth | int? | Exclusion until month (nullable) |
| IsActive | bool | Whether exclusion is active |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime | Last update timestamp |
| CreatedBy | string(255) | Creator email |
| UpdatedBy | string(255) | Last updater email |

**Indexes**:
- Index on FipsId
- Index on IsActive
- Index on (FipsId, IsActive)

## Integration

### Due date calculation

The `ReturnStatusService.GetReturnDueDate()` method automatically:

1. Checks for an active override for the reporting period
2. Uses the override date if found
3. Falls back to the 3rd working day calculation if no override exists

### CMS integration

Business area names are fetched from the CMS API:

**Endpoint**: `{CmsApiBaseUrl}/category-values?filters[category_type][name][$eq]=Business area&fields[0]=name&fields[1]=sort_order&sort=sort_order:asc`

The dropdown is populated dynamically when creating business area configurations.

## Files created

### Models
- `Models/PerformanceReportingDueDateOverride.cs`
- `Models/PerformanceReportingBusinessAreaConfig.cs`
- `Models/PerformanceReportingProductExclusion.cs`
- `Models/PerformanceReportingPeriodExclusion.cs`

### Controllers
- `Controllers/Admin/PerformanceReportingManagementController.cs`

### Views
- `Views/Admin/PerformanceReportingManagement/Index.cshtml`
- `Views/Admin/PerformanceReportingManagement/PeriodExclusions.cshtml`
- `Views/Admin/PerformanceReportingManagement/CreatePeriodExclusion.cshtml`
- `Views/Admin/PerformanceReportingManagement/EditPeriodExclusion.cshtml`
- `Views/Admin/PerformanceReportingManagement/BulkDelete.cshtml`
- `Views/Admin/PerformanceReportingManagement/DueDateOverrides.cshtml`
- `Views/Admin/PerformanceReportingManagement/CreateDueDateOverride.cshtml`
- `Views/Admin/PerformanceReportingManagement/EditDueDateOverride.cshtml`
- `Views/Admin/PerformanceReportingManagement/BusinessAreaConfig.cshtml`
- `Views/Admin/PerformanceReportingManagement/CreateBusinessAreaConfig.cshtml`
- `Views/Admin/PerformanceReportingManagement/EditBusinessAreaConfig.cshtml`
- `Views/Admin/PerformanceReportingManagement/ProductExclusions.cshtml`
- `Views/Admin/PerformanceReportingManagement/CreateProductExclusion.cshtml`
- `Views/Admin/PerformanceReportingManagement/EditProductExclusion.cshtml`

### Modified files
- `Data/CompassDbContext.cs` - Added DbSets and entity configurations
- `Services/ReturnStatusService.cs` - Updated to use calendar overrides
- `Views/Admin/Index.cshtml` - Added navigation card
- `Controllers/DemandManagementController.cs` - Fixed missing using statement

## Security and audit

- All actions require authentication
- Access restricted to users in "Super Admin" or "Central Operations Admin" groups
- All modifications are logged with user email and timestamp
- Soft deletes via IsActive flag (preserves audit trail)
- Controller actions log important events via ILogger

## Migrations applied

1. `AddPerformanceReportingManagementTables` - Initial three tables (due dates, business areas, product exclusions)
2. `AddPerformanceReportingPeriodExclusions` - Period exclusions table

Both migrations have been applied to the development database.

## Future enhancements

Consider these potential improvements:

1. **Reporting integration**: Automatically filter excluded products/periods from reporting lists
2. **Email notifications**: Notify service owners when due dates are overridden or periods excluded
3. **Bulk operations**: Allow bulk product exclusions via CSV upload
4. **Reporting dashboard**: Show metrics on exclusions and overrides usage
5. **Validation rules**: Add business rules to prevent conflicting configurations
6. **API endpoints**: Expose reporting configuration via API for external systems
7. **Historical tracking**: Show which products were affected by exclusions in reporting

