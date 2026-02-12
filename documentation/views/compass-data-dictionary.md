# COMPASS Data Dictionary

COMPASS (Compliance, Outcomes, Performance, Assurance, Standards and Strategy) data dictionary defines the fields, data types, and business rules for all data collected within the COMPASS reporting database.

## User Management Data

### UserPermission Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique permission identifier | Auto-generated, primary key |
| `Email` | String (255) | User's email address | Required, unique, valid email format |
| `Name` | String (200) | User's full name | Optional |
| `IsActive` | Boolean | Whether account is active | Default: true |
| `CanAddProduct` | Boolean | Can create new products | Default: false |
| `CanEditProduct` | Boolean | Can modify products | Default: false |
| `CanDeleteProduct` | Boolean | Can delete products | Default: false |
| `CanAddMetric` | Boolean | Can create metrics | Default: false |
| `CanEditMetric` | Boolean | Can modify metrics | Default: false |
| `CanDeleteMetric` | Boolean | Can delete metrics | Default: false |
| `CanAddMilestone` | Boolean | Can create milestones | Default: false |
| `CanEditMilestone` | Boolean | Can modify milestones | Default: false |
| `CanDeleteMilestone` | Boolean | Can delete milestones | Default: false |
| `CanAddUser` | Boolean | Can create user accounts | Default: false |
| `CanEditUser` | Boolean | Can modify user accounts | Default: false |
| `CanViewReports` | Boolean | Can view reports | Default: false |
| `CanSubmitReports` | Boolean | Can submit reports | Default: false |
| `CreatedAt` | DateTime | Creation timestamp | Auto-generated |
| `UpdatedAt` | DateTime | Last update timestamp | Auto-updated |
| `CreatedBy` | String (255) | Creator's email | Optional |
| `UpdatedBy` | String (255) | Last updater's email | Optional |

### ReportingUser Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique user identifier | Auto-generated, primary key |
| `Email` | String (255) | User's email address | Required, unique, valid email format |
| `Name` | String (200) | User's full name | Required |
| `Role` | String (50) | User's role | Required, values: reporting_user, admin, central_operations |
| `IsActive` | Boolean | Whether account is active | Default: true |
| `CreatedAt` | DateTime | Account creation timestamp | Auto-generated |
| `CreatedBy` | String (255) | Account creator's email | Optional |

## Performance Metrics Data

### PerformanceMetric Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique metric identifier | Auto-generated, primary key |
| `UniqueId` | String (50) | Unique identifier | Required, unique |
| `Name` | String (200) | Metric name | Required |
| `Description` | String (1000) | Metric description | Optional |
| `LegalRegulatory` | Boolean | Legal/regulatory requirement | Default: false |
| `Mandate` | String (50) | Legal mandate source | Values: Legal, DSIT, DfE, DDT |
| `StageD` | Boolean | Applies to Discovery stage | Default: false |
| `StageA` | Boolean | Applies to Alpha stage | Default: false |
| `StageB` | Boolean | Applies to Beta stage | Default: false |
| `StageL` | Boolean | Applies to Live stage | Default: false |
| `StageR` | Boolean | Applies to Retired stage | Default: false |
| `Notice` | String (2000) | Additional notice text | Optional |
| `ReportableInPhase` | String | JSON string of phase IDs | Optional |
| `Category` | String (50) | Metric category | Required |
| `Measure` | String (50) | Measurement type | Required, values: number, decimal, options_list, boolean |
| `Mandatory` | Boolean | Whether metric is mandatory | Default: false |
| `ValidationCriteria` | String (2000) | JSON validation rules | Optional |
| `CanReportNullReturn` | Boolean | Allow null returns | Default: true |
| `Enabled` | Boolean | Whether metric is enabled | Default: true |
| `CreatedAt` | DateTime | Creation timestamp | Auto-generated |
| `UpdatedAt` | DateTime | Last update timestamp | Auto-updated |
| `CreatedBy` | String (255) | Creator's email | Optional |
| `UpdatedBy` | String (255) | Last updater's email | Optional |

### PerformanceMetricData Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique data identifier | Auto-generated, primary key |
| `PerformanceMetricId` | Integer | Associated metric identifier | Required, foreign key |
| `ProductId` | String (50) | FIPS product identifier | Required |
| `ReportingPeriod` | String (20) | Reporting period | Required, format: "YYYY-MM" |
| `Value` | String (1000) | Submitted value | Optional if IsNullReturn is true |
| `Comment` | String (2000) | Additional comments | Optional |
| `IsNullReturn` | Boolean | Whether this is a null return | Default: false |
| `IsSubmitted` | Boolean | Whether report is submitted | Default: false |
| `SubmittedBy` | String (255) | Submitter's email | Required |
| `SubmittedAt` | DateTime | Submission timestamp | Auto-generated |
| `CreatedAt` | DateTime | Creation timestamp | Auto-generated |
| `UpdatedAt` | DateTime | Last update timestamp | Auto-updated |

## Milestone Data

### Milestone Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique milestone identifier | Auto-generated, primary key |
| `FipsId` | String (50) | FIPS product identifier | Required |
| `Title` | String (200) | Milestone title | Required |
| `Description` | String (1000) | Milestone description | Optional |
| `Status` | String (50) | Milestone status | Required, values: Not Started, In Progress, Completed, Overdue, Cancelled |
| `TargetDate` | DateTime | Target completion date | Optional |
| `ActualDate` | DateTime | Actual completion date | Optional |
| `Priority` | String (50) | Milestone priority | Required, values: High, Medium, Low |
| `CreatedBy` | String (255) | Creator's email | Required |
| `CreatedDate` | DateTime | Creation timestamp | Auto-generated |
| `LastUpdatedBy` | String (255) | Last updater's email | Optional |
| `LastUpdatedDate` | DateTime | Last update timestamp | Optional |
| `ObjectiveId` | Integer | Associated objective identifier | Optional, foreign key |

**Additional Properties:**
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `DueDate` | DateTime | Alias for TargetDate | Calculated field |
| `ProductId` | String (50) | Alias for FipsId | Calculated field |
| `RagStatus` | String (20) | Calculated RAG status | Values: Red, Amber, Green |
| `CreatedAt` | DateTime | Alias for CreatedDate | Calculated field |
| `UpdatedAt` | DateTime | Alias for LastUpdatedDate | Calculated field |
| `ProductName` | String (255) | Product name for display | Optional |

### MilestoneUpdate Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique update identifier | Auto-generated, primary key |
| `MilestoneId` | Integer | Associated milestone identifier | Required, foreign key |
| `UpdateDate` | DateTime | Update timestamp | Auto-generated |
| `UpdateText` | String (2000) | Update description | Required |
| `StatusChange` | String (50) | Status change description | Optional |
| `UpdatedBy` | String (255) | Updater's email | Required |

**Additional Properties:**
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `UpdatedAt` | DateTime | Alias for UpdateDate | Calculated field |
| `Status` | String (50) | Status change from update | Optional |
| `RagStatus` | String (20) | RAG status at update time | Values: Red, Amber, Green |
| `Comment` | String (2000) | Additional comment | Optional |
| `NewDueDate` | DateTime | New due date if changed | Optional |

## Objective Data

### Objective Table
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique objective identifier | Auto-generated, primary key |
| `Reference` | String (50) | Unique reference code | Required, unique |
| `Title` | String (200) | Objective title | Required |
| `Description` | String (1000) | Objective description | Optional |
| `Status` | String (50) | Objective status | Required, values: Active, Completed, Cancelled, On Hold |
| `Type` | String (50) | Objective type | Required, values: DDT Objective, Government Mission, Flagship, Other |
| `CreatedAt` | DateTime | Creation timestamp | Auto-generated |
| `UpdatedAt` | DateTime | Last update timestamp | Auto-updated |
| `CreatedBy` | String (255) | Creator's email | Optional |
| `UpdatedBy` | String (255) | Last updater's email | Optional |

## Legacy Data (Deprecated)

### ReportingMetric Table (Legacy)
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique metric identifier | Auto-generated, primary key |
| `Name` | String (200) | Metric name | Required |
| `Description` | String (1000) | Metric description | Optional |
| `MeasurementType` | String (50) | Type of measurement | Required, values: percentage, number, text |
| `IsMandatory` | Boolean | Whether metric is mandatory | Default: false |
| `AllowNotApplicable` | Boolean | Allow null returns | Default: false |
| `IsActive` | Boolean | Whether metric is active | Default: true |
| `CreatedAt` | DateTime | Creation timestamp | Auto-generated |
| `UpdatedAt` | DateTime | Last update timestamp | Auto-updated |
| `CreatedBy` | String (255) | Creator's email | Optional |
| `UpdatedBy` | String (255) | Last updater's email | Optional |

### ReportingData Table (Legacy)
| Field | Data Type | Description | Business Rules |
|-------|-----------|-------------|----------------|
| `Id` | Integer | Unique data identifier | Auto-generated, primary key |
| `MetricId` | Integer | Associated metric identifier | Required, foreign key |
| `ProductId` | String (50) | Product identifier | Optional |
| `Value` | String (1000) | Submitted value | Optional |
| `ReportingPeriod` | String (20) | Reporting period | Required |
| `Comment` | String (2000) | Additional comments | Optional |
| `SubmittedBy` | String (255) | Submitter's email | Required |
| `SubmittedAt` | DateTime | Submission timestamp | Auto-generated |
| `CreatedAt` | DateTime | Creation timestamp | Auto-generated |
| `UpdatedAt` | DateTime | Last update timestamp | Auto-updated |

## Data Validation Rules

### Email Validation
- Must be valid email format
- Maximum 255 characters
- Must be unique across the system

### Date Validation
- All dates stored in UTC
- Displayed in user's local timezone
- Due dates cannot be in the past when creating new periods

### Numeric Validation
- Percentage values: 0-100
- Count values: Non-negative integers
- Currency values: Non-negative decimals

### Text Validation
- Required fields cannot be empty or whitespace only
- Maximum lengths enforced at database level
- Special characters allowed unless specified otherwise

### Status Values
- RAG Status: Must be Red, Amber, or Green
- Milestone Status: Must be Not Started, In Progress, Completed, Overdue, or Cancelled
- Priority Levels: Must be High, Medium, or Low
- Objective Status: Must be Active, Completed, Cancelled, or On Hold
- Objective Type: Must be DDT Objective, Government Mission, Flagship, or Other

## Data Relationships

### Foreign Key Relationships
- `PerformanceMetricData.PerformanceMetricId` → `PerformanceMetric.Id`
- `Milestone.ObjectiveId` → `Objective.Id`
- `MilestoneUpdate.MilestoneId` → `Milestone.Id`
- `ReportingData.MetricId` → `ReportingMetric.Id` (legacy)

### Unique Constraints
- `UserPermission.Email` - Unique email per permission record
- `ReportingUser.Email` - Unique email per user
- `PerformanceMetric.UniqueId` - Unique identifier per metric
- `Objective.Reference` - Unique reference code per objective

### Indexes
- `ProductAllocation(ProductId, UserEmail)` - Unique constraint (legacy)
- `ReportingUser.Email` - Unique index
- `PerformanceMetric.UniqueId` - Unique index
- `Objective.Reference` - Unique index

## Data Retention and Archiving

### Retention Periods
- Active reporting data: 7 years
- Completed milestones: 5 years
- User activity logs: 2 years
- Deleted records: 30 days (soft delete)

### Archiving Rules
- Data older than retention period moved to archive
- Archived data read-only
- Archive accessible for compliance purposes only

## Data Security and Privacy

### Confidential Data
- Metrics marked as legal/regulatory require special access
- Audit trail maintained for all data access
- Additional encryption for sensitive fields

### Personal Data
- User email addresses treated as personal data
- GDPR compliance for EU users
- Right to deletion and data portability supported

### Access Control
- Role-based access control enforced
- API access requires authentication
- All data access logged for audit purposes
