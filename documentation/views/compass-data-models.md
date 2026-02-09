# COMPASS Data Models

COMPASS (Compliance, Outcomes, Performance, Assurance, Standards and Strategy) data models define the structure and relationships of data collected and managed within the COMPASS reporting database.

## Core Database Models

### User Management

#### UserPermission
Manages user access and permissions within the COMPASS system.

**Purpose**: Controls what actions users can perform within the COMPASS system through granular permission flags.

**Key Relationships**:
- One-to-one with user accounts via email
- Referenced by admin controllers for permission checking

**Core Fields**:
- `Id` (Primary Key)
- `Email` (Unique identifier)
- Permission flags for products, metrics, milestones, users, and reports
- Audit fields (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`)

#### ReportingUser
Basic user information for reporting system users.

**Purpose**: Stores essential user information and role assignments for the reporting system.

**Key Relationships**:
- Referenced by reporting controllers for user identification
- Linked to products via CMS `product_contacts` relationship

**Core Fields**:
- `Id` (Primary Key)
- `Email` (Unique identifier)
- `Name` (Display name)
- `Role` (reporting_user, admin, central_operations)
- `IsActive` (Account status)

### Performance Metrics

#### PerformanceMetric
Defines performance metrics and their configuration.

**Purpose**: Central configuration for all performance metrics that can be reported on by services.

**Key Relationships**:
- One-to-many with `PerformanceMetricData` (submitted data)
- Many-to-many with products via stage flags
- Referenced by reporting controllers for metric display

**Core Fields**:
- `Id` (Primary Key)
- `UniqueId` (Unique identifier for external references)
- `Name`, `Description` (Metric identification)
- `LegalRegulatory`, `Mandate` (Compliance information)
- `StageD`, `StageA`, `StageB`, `StageL`, `StageR` (Service stage applicability)
- `Category`, `Measure` (Classification and data type)
- `ValidationCriteria` (JSON validation rules)
- `Mandatory`, `CanReportNullReturn` (Reporting requirements)

#### PerformanceMetricData
Stores submitted performance metric data.

**Purpose**: Records actual metric values submitted by users for specific reporting periods.

**Key Relationships**:
- Many-to-one with `PerformanceMetric` (metric definition)
- Linked to products via `ProductId`
- Referenced by reporting controllers for data display and submission

**Core Fields**:
- `Id` (Primary Key)
- `PerformanceMetricId` (Foreign Key to PerformanceMetric)
- `ProductId` (FIPS product identifier)
- `ReportingPeriod` (YYYY-MM format)
- `Value` (Submitted metric value)
- `IsNullReturn`, `IsSubmitted` (Submission status)
- `SubmittedBy`, `SubmittedAt` (Audit information)

### Milestones

#### Milestone
Tracks service delivery milestones and objectives.

**Purpose**: Manages project milestones with progress tracking and objective alignment.

**Key Relationships**:
- Many-to-one with `Objective` (strategic alignment)
- One-to-many with `MilestoneUpdate` (progress tracking)
- Linked to products via `FipsId`

**Core Fields**:
- `Id` (Primary Key)
- `FipsId` (Product identifier)
- `Title`, `Description` (Milestone identification)
- `Status` (Not Started, In Progress, Completed, Overdue, Cancelled)
- `TargetDate`, `ActualDate` (Timeline tracking)
- `Priority` (High, Medium, Low)
- `ObjectiveId` (Foreign Key to Objective)

**Additional Properties** (Aliases for compatibility):
- `DueDate` (alias for TargetDate)
- `ProductId` (alias for FipsId)
- `RagStatus` (calculated RAG status)

#### MilestoneUpdate
Tracks milestone progress updates.

**Purpose**: Records progress updates and status changes for milestones.

**Key Relationships**:
- Many-to-one with `Milestone` (parent milestone)
- Referenced by milestone controllers for update history

**Core Fields**:
- `Id` (Primary Key)
- `MilestoneId` (Foreign Key to Milestone)
- `UpdateText` (Update description)
- `StatusChange` (Status change description)
- `UpdatedBy`, `UpdateDate` (Audit information)

### Objectives

#### Objective
Defines strategic objectives that milestones can be linked to.

**Purpose**: Provides strategic context and alignment for milestones and projects.

**Key Relationships**:
- One-to-many with `Milestone` (milestones aligned to objective)
- Referenced by admin controllers for objective management

**Core Fields**:
- `Id` (Primary Key)
- `Reference` (Unique reference code)
- `Title`, `Description` (Objective identification)
- `Status` (Active, Completed, Cancelled, On Hold)
- `Type` (DDT Objective, Government Mission, Flagship, Other)

## Legacy Models (Deprecated)

### ReportingMetric
Legacy metric model being replaced by PerformanceMetric.

**Purpose**: Original metric definition model (deprecated).

**Key Relationships**:
- One-to-many with `ReportingData` (legacy data)
- One-to-many with `MetricCondition` (legacy conditions)

### MetricCondition
Conditions for legacy reporting metrics.

**Purpose**: Defines conditions for when legacy metrics apply (deprecated).

### ProductAllocation
Product allocation to users (legacy model).

**Purpose**: Legacy user-product assignment model (deprecated).

### ReportingData
Legacy reporting data storage.

**Purpose**: Original data submission model (deprecated).

## Entity Relationships

### Primary Relationships
```
UserPermission (1:1) ReportingUser
PerformanceMetric (1:N) PerformanceMetricData
Objective (1:N) Milestone
Milestone (1:N) MilestoneUpdate
```

### Data Flow Relationships
```
CMS Product → Product_Contacts → ReportingUser
PerformanceMetric → PerformanceMetricData → Reporting Period
Objective → Milestone → MilestoneUpdate
```

### Cross-System Integration
- **CMS Integration**: Products and user assignments come from CMS
- **User Authentication**: Azure AD integration for user management
- **Reporting Cycles**: Monthly reporting periods with due dates
- **RAG Status**: Calculated based on progress and deadlines

## Model Characteristics

### Audit Trail
All core models include audit fields:
- `CreatedAt`, `UpdatedAt` (timestamps)
- `CreatedBy`, `UpdatedBy` (user identification)

### Soft Delete Support
Models support soft delete patterns where `IsActive` or `Enabled` flags control visibility.

### Validation Patterns
- Required fields enforced at database level
- String length constraints defined in Entity Framework
- Unique constraints on email addresses and reference codes
- Foreign key relationships with cascade delete where appropriate

### Data Types
- **Identifiers**: Integer primary keys with auto-generation
- **Text Fields**: String with defined maximum lengths
- **Timestamps**: DateTime with UTC storage
- **Boolean Flags**: Default values defined
- **JSON Fields**: Validation rules and configuration stored as JSON strings
