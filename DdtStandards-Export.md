# DDT Standards - Complete Code Export

This document provides a comprehensive export of all DDT Standards views, methods, code, services, and database models.

## Table of Contents

1. [Overview](#overview)
2. [Views](#views)
3. [Controller Methods](#controller-methods)
4. [Services](#services)
5. [Database Models](#database-models)
6. [View Models](#view-models)
7. [Database Schema](#database-schema)

---

## Overview

The DDT Standards module in COMPASS provides a comprehensive system for managing Digital Data Technology (DDT) standards. It includes:

- **Standards Management**: Create, edit, review, approve, and publish standards
- **Workflow**: Draft → Under Review → Approved → Published
- **Products**: Manage approved and tolerated products for standards
- **Exceptions**: Track exceptions to standards
- **Comments**: Threaded commenting system for standards
- **Categories & Phases**: Categorization and phase allocation
- **Owners & Contacts**: User management for standards

---

## Views

### Index.cshtml
**Location**: `Views/DdtStandards/Index.cshtml`  
**Model**: `DdtStandardsManageViewModel`  
**Purpose**: Main management page showing standards by stage (drafts, in-review, for-approval, published, unpublished)

**Features**:
- Navigation sidebar with stage filters
- Filtering by search, category, creator, owner, contact, legal standard
- Separate sections for "My" and "All" standards
- Standards Managers section for approved products and exceptions management
- Uses `_StandardsCard` partial for displaying standards

### Create.cshtml
**Location**: `Views/DdtStandards/Create.cshtml`  
**Model**: `DdtStandard` (via ViewBag)  
**Purpose**: Create/edit form for standards

**Sections**:
1. **Basic Details**: Title, Summary
2. **Purpose**: Purpose and rationale (markdown)
3. **Criteria**: List of criteria (JSON array)
4. **How to Meet**: Guidance on meeting requirements (markdown)
5. **Application**: Phase selection
6. **Categorisation**: Categories and sub-categories
7. **Compliance**: Governance, legal basis, validity period, related guidance
8. **Owners & Contacts**: User picker for owners and contacts
9. **Products**: Approved and tolerated products
10. **Exemptions**: Exception linking
11. **Submission**: Submit for review

**Features**:
- Multi-section form with navigation
- Autosave functionality
- User picker integration
- Product search and selection
- Markdown support for rich text fields
- Comments panel
- Guidance panel

### Details.cshtml
**Location**: `Views/DdtStandards/Details.cshtml`  
**Model**: `DdtStandard`  
**Purpose**: View published standard details

**Sections**:
- Standard Summary (Reference, Version, Published date, Updated date)
- Summary
- Categories
- Purpose
- Criteria
- How to meet this standard
- Declaring conformance
- Governance
- Legal basis
- Related guidance
- Phases
- Approved products (table)
- Tolerated products (table)
- Standard owners (table)
- Standard contacts (table)
- Related standards sidebar

**Features**:
- Sortable tables for products, owners, contacts
- Actions panel for owners/contacts/Standards Managers (Make a change, Unpublish)
- Breadcrumb navigation
- Warning banner for draft standards

### Exceptions.cshtml
**Location**: `Views/DdtStandards/Exceptions.cshtml`  
**Model**: `List<DdtStandardException>`  
**Purpose**: Manage exceptions to standards

**Features**:
- Table of exceptions with sortable columns
- Create/Edit exception modals
- Filter by standard, product, status
- Shows: Title, Standard, Product, Status, Granted date, Expires date
- Delete functionality

### Published.cshtml
**Location**: `Views/DdtStandards/Published.cshtml`  
**Model**: `IEnumerable<DdtStandard>`  
**Purpose**: Public view of published standards

**Features**:
- Filter by search and category
- "My published" and "All published" sections
- Uses `_StandardsCard` partial

### Unpublished.cshtml
**Location**: `Views/DdtStandards/Unpublished.cshtml`  
**Model**: `IEnumerable<DdtStandard>`  
**Purpose**: View unpublished standards

**Features**:
- Filter by search and category
- "My unpublished" and "All unpublished" sections
- Uses `_StandardsCard` partial

### Unpublish.cshtml
**Location**: `Views/DdtStandards/Unpublish.cshtml`  
**Model**: `DdtStandard`  
**Purpose**: Unpublish a standard

**Features**:
- Standard information display
- Reason textarea (required)
- Warning message
- Confirmation dialog

### Preview.cshtml
**Location**: `Views/DdtStandards/Preview.cshtml`  
**Model**: `DdtStandard`  
**Purpose**: Preview standard (standalone layout)

**Features**:
- Standalone HTML layout (no main layout)
- Shows standard content in preview format
- Metadata section

### ApprovedProducts.cshtml
**Location**: `Views/DdtStandards/ApprovedProducts.cshtml`  
**Model**: `DdtStandardsManageViewModel`  
**Purpose**: Manage approved and tolerated products

**Features**:
- Product table with sortable columns
- Create/Edit product modals
- Shows: Name, Provider, Version, Approval Status, Standards
- Assign products to standards
- Product type badges (Approved/Tolerated)

### _StandardsCard.cshtml
**Location**: `Views/DdtStandards/_StandardsCard.cshtml`  
**Model**: `dynamic`  
**Purpose**: Reusable partial for displaying standards in a card/table

**Features**:
- Sortable table
- Columns: Title, Created by (optional), Version, Categories, Owners, Contacts, Updated
- Actions dropdown for Standards Managers (optional)
- Modal dialogs for:
  - Change Title
  - Set Owners
  - Set Contacts
  - Set Categories
  - Set Sub-Categories
  - Set Legacy Reference

---

## Controller Methods

### DdtStandardsController

**Location**: `Controllers/DdtStandardsController.cs`

#### Main Actions

**Index** (`GET /DdtStandards`)
- Parameters: `view`, `search`, `category`, `creator`, `owner`, `contact`, `legalStandard`
- Returns: `DdtStandardsManageViewModel`
- Purpose: Main management page with filtering

**Published** (`GET /DdtStandards/Published`)
- Parameters: `search`, `category`
- Returns: Published standards view
- Purpose: Public view of published standards

**Unpublished** (`GET /DdtStandards/Unpublished`)
- Parameters: `search`, `category`
- Returns: Unpublished standards view
- Purpose: View unpublished standards

**Create** (`GET /DdtStandards/Create`)
- Parameters: `id` (optional, for editing)
- Returns: Create/edit form
- Purpose: Display create/edit form

**Create** (`POST /DdtStandards/Create`)
- Parameters: Full standard form data
- Returns: Redirect to Index or Details
- Purpose: Save draft standard

**Autosave** (`POST /DdtStandards/Autosave`)
- Parameters: Standard form data
- Returns: JSON response
- Purpose: Autosave draft while editing

**Details** (`GET /DdtStandards/Details/{id}`)
- Parameters: `id`
- Returns: Standard details view
- Purpose: View standard details

**Preview** (`GET /DdtStandards/Preview/{id}`)
- Parameters: `id`
- Returns: Preview view
- Purpose: Preview standard

**Edit** (`GET /DdtStandards/Edit/{id}`)
- Parameters: `id`
- Returns: Edit form
- Purpose: Load standard for editing

**Edit** (`POST /DdtStandards/Edit`)
- Parameters: Standard form data
- Returns: Redirect
- Purpose: Update standard

**Delete** (`POST /DdtStandards/Delete/{id}`)
- Parameters: `id`
- Returns: Redirect
- Purpose: Soft delete standard

#### Workflow Actions

**SubmitForReview** (`POST /DdtStandards/SubmitForReview/{id}`)
- Parameters: `id`
- Returns: Redirect
- Purpose: Submit draft for review (changes stage to "Under Review")

**Approve** (`POST /DdtStandards/Approve/{id}`)
- Parameters: `id`, `comment` (optional)
- Returns: Redirect
- Purpose: Approve standard (changes stage to "Approved")

**Reject** (`POST /DdtStandards/Reject/{id}`)
- Parameters: `id`, `reason`
- Returns: Redirect
- Purpose: Reject standard (changes stage to "Rejected")

**Publish** (`POST /DdtStandards/Publish/{id}`)
- Parameters: `id`
- Returns: Redirect
- Purpose: Publish standard (changes stage to "Published", sets `IsPublished = true`, `PublishedAt = DateTime.UtcNow`)

**Unpublish** (`GET /DdtStandards/Unpublish/{id}`)
- Parameters: `id`
- Returns: Unpublish form
- Purpose: Display unpublish form

**Unpublish** (`POST /DdtStandards/Unpublish/{id}`)
- Parameters: `id`, `reason`
- Returns: Redirect
- Purpose: Unpublish standard (creates audit log, changes stage to "Unpublished")

**MakeChange** (`POST /DdtStandards/MakeChange/{id}`)
- Parameters: `id`
- Returns: Redirect to Create with new standard
- Purpose: Create new draft from published standard (creates child standard)

#### Comments Actions

**AddComment** (`POST /DdtStandards/AddComment/{id}`)
- Parameters: `id`, `title`, `comments`, `commentType`, `field`, `parentCommentId` (optional)
- Returns: JSON or Redirect
- Purpose: Add comment to standard (supports threaded comments)

**GetComments** (`GET /DdtStandards/GetComments/{id}`)
- Parameters: `id`
- Returns: JSON
- Purpose: Get comments for standard

**ResolveComment** (`POST /DdtStandards/ResolveComment/{id}`)
- Parameters: `id`, `commentId`
- Returns: Redirect
- Purpose: Mark comment as resolved

**UnresolveComment** (`POST /DdtStandards/UnresolveComment/{id}`)
- Parameters: `id`, `commentId`
- Returns: Redirect
- Purpose: Mark comment as unresolved

#### Products Management

**ApprovedProducts** (`GET /DdtStandards/ApprovedProducts`)
- Returns: Approved products management view
- Purpose: Manage products

**CreateProduct** (`POST /DdtStandards/CreateProduct`)
- Parameters: `name`, `description`, `provider`, `version`, `approvalStatus`
- Returns: Redirect
- Purpose: Create new product

**UpdateProduct** (`POST /DdtStandards/UpdateProduct`)
- Parameters: `id`, `name`, `description`, `provider`, `version`, `approvalStatus`
- Returns: Redirect
- Purpose: Update product

**AssignProductToStandard** (`POST /DdtStandards/AssignProductToStandard`)
- Parameters: `productId`, `standardId`, `productType`, `notes`
- Returns: Redirect
- Purpose: Assign product to standard (Approved or Tolerated)

#### Exceptions Management

**Exceptions** (`GET /DdtStandards/Exceptions`)
- Returns: Exceptions management view
- Purpose: Manage exceptions

**CreateException** (`POST /DdtStandards/CreateException`)
- Parameters: `title`, `standardId`, `description`, `reason`, `productId`, `fipsProductId`, `status`, `grantedAt`, `expiresAt`, `notes`
- Returns: Redirect
- Purpose: Create exception

**UpdateException** (`POST /DdtStandards/UpdateException`)
- Parameters: `id`, `title`, `standardId`, `description`, `reason`, `productId`, `fipsProductId`, `status`, `grantedAt`, `expiresAt`, `notes`
- Returns: Redirect
- Purpose: Update exception

**DeleteException** (`POST /DdtStandards/DeleteException`)
- Parameters: `id`
- Returns: Redirect
- Purpose: Delete exception

**GetExceptions** (`GET /DdtStandards/GetExceptions`)
- Parameters: `search` (optional)
- Returns: JSON
- Purpose: Get exceptions (for search/autocomplete)

#### Standards Management (Standards Managers only)

**UpdateStandardTitle** (`POST /DdtStandards/UpdateStandardTitle`)
- Parameters: `id`, `title`
- Returns: Redirect
- Purpose: Update standard title

**UpdateStandardOwners** (`POST /DdtStandards/UpdateStandardOwners`)
- Parameters: `id`, `ownerIds[]`
- Returns: Redirect
- Purpose: Update standard owners

**UpdateStandardContacts** (`POST /DdtStandards/UpdateStandardContacts`)
- Parameters: `id`, `contactIds[]`
- Returns: Redirect
- Purpose: Update standard contacts

**UpdateStandardCategories** (`POST /DdtStandards/UpdateStandardCategories`)
- Parameters: `id`, `categoryIds[]`
- Returns: Redirect
- Purpose: Update standard categories

**UpdateStandardSubCategories** (`POST /DdtStandards/UpdateStandardSubCategories`)
- Parameters: `id`, `subCategoryIds[]`
- Returns: Redirect
- Purpose: Update standard sub-categories

**UpdateStandardLegacyReference** (`POST /DdtStandards/UpdateStandardLegacyReference`)
- Parameters: `id`, `legacyReference`
- Returns: Redirect
- Purpose: Update legacy reference

#### Migration

**MigrateFromCms** (`POST /DdtStandards/MigrateFromCms`)
- Parameters: `skipExisting` (default: true)
- Returns: JSON
- Purpose: Migrate standards from external CMS API

---

## Services

### IStandardsCmsApiService / StandardsCmsApiService

**Location**: `Services/IStandardsCmsApiService.cs`, `Services/StandardsCmsApiService.cs`

**Purpose**: Integration with external Standards CMS API (Strapi)

**Methods**:

**GetAsync<T>** (`Task<T?> GetAsync<T>(string endpoint, TimeSpan? cacheDuration = null)`)
- Generic method for API calls
- Supports caching
- Returns deserialized JSON response

**GetStandardsAsync** (`Task<List<StandardDto>> GetStandardsAsync(bool? published = null, string? search = null, string? category = null, string? stage = null, TimeSpan? cacheDuration = null)`)
- Get list of standards from CMS
- Supports filtering by published status, search, category, stage
- Returns list of `StandardDto`

**GetStandardByIdAsync** (`Task<StandardDto?> GetStandardByIdAsync(int id, TimeSpan? cacheDuration = null)`)
- Get single standard by ID
- Populates related data (stage, categories, sub-categories, phases, owners, contacts)
- Returns `StandardDto` or null

**GetStandardByDocumentIdAsync** (`Task<StandardDto?> GetStandardByDocumentIdAsync(string documentId, TimeSpan? cacheDuration = null)`)
- Get standard by document ID (for draft standards)
- Returns `StandardDto` or null

**GetCategoriesAsync** (`Task<List<StandardCategoryDto>> GetCategoriesAsync(TimeSpan? cacheDuration = null)`)
- Get all categories
- Returns list of `StandardCategoryDto`

**GetStagesAsync** (`Task<List<StandardStageDto>> GetStagesAsync(TimeSpan? cacheDuration = null)`)
- Get all stages
- Returns list of `StandardStageDto`

**GetSubCategoriesByIdsAsync** (`Task<List<StandardSubCategoryDto>> GetSubCategoriesByIdsAsync(List<int> subCategoryIds, TimeSpan? cacheDuration = null)`)
- Get sub-categories by IDs
- Populates category relation
- Returns list of `StandardSubCategoryDto`

**Configuration**:
- Base URL: `StandardsCmsApi:BaseUrl` (default: `https://dfe-standards-cms-217ce4e280a0.herokuapp.com/api/`)
- API Key: `StandardsCmsApi:ReadApiKey`
- Timeout: 30 seconds
- Uses `IMemoryCache` for caching

---

## Database Models

### DdtStandard

**Location**: `Models/DdtStandard.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `LegacyId` (string?, max 100) - Legacy ID from external CMS
- `LegacyReference` (string?, max 200) - Legacy reference
- `StandardUuid` (string, required, max 36) - UUID for standards as code
- `ParentStandardId` (int?) - Parent standard (if created from "Make a change")
- `ParentStandard` (DdtStandard?) - Navigation property
- `Title` (string, required, max 500) - Standard title
- `Slug` (string, required, max 500) - URL-friendly slug
- `Summary` (string?, max 2000) - Brief summary
- `Purpose` (string?, nvarchar(max)) - Purpose and rationale (markdown)
- `Criteria` (string?, nvarchar(max)) - Criteria (JSON array)
- `HowToMeet` (string?, nvarchar(max)) - How to meet (markdown)
- `Governance` (string?, nvarchar(max)) - Governance info (markdown)
- `GovernanceApproval` (bool) - Governance approval flag
- `Version` (string, required, max 20) - Semantic version (default: "0.1.0")
- `PreviousVersion` (string?, max 20) - Previous version
- `Stage` (string, required, max 50) - Lifecycle stage (default: "Draft")
- `DraftCreated` (DateTime, required) - Draft creation date
- `FirstPublished` (DateTime?) - First publication date
- `LastUpdated` (DateTime?) - Last update timestamp
- `LegalStandard` (bool) - Is legal requirement
- `LegalBasis` (string?, max 1000) - Legal basis
- `ValidityPeriod` (int?) - Validity period in months
- `RelatedGuidance` (string?, nvarchar(max)) - Related guidance (markdown)
- `IsModified` (bool) - Modified since last publication
- `IsPublished` (bool) - Is published
- `PublishedAt` (DateTime?) - Publication date
- `CreatorUserId` (int?) - Creator user ID
- `CreatorUser` (User?) - Navigation property
- `CreatedAt` (DateTime, required) - Created timestamp
- `UpdatedAt` (DateTime, required) - Updated timestamp
- `IsDeleted` (bool) - Soft delete flag

**Collections**:
- `Owners` (ICollection<DdtStandardOwner>)
- `Contacts` (ICollection<DdtStandardContact>)
- `Categories` (ICollection<DdtStandardCategory>)
- `SubCategories` (ICollection<DdtStandardSubCategory>)
- `Phases` (ICollection<DdtStandardPhase>)
- `ValidationRules` (ICollection<DdtStandardValidationRule>)
- `Versions` (ICollection<DdtStandardVersion>)
- `Comments` (ICollection<DdtStandardComment>)
- `Products` (ICollection<DdtStandardProduct>)
- `Exceptions` (ICollection<DdtStandardException>)
- `AuditLogs` (ICollection<AuditLog>)
- `UnpublishAudits` (ICollection<DdtStandardUnpublishAudit>)

### DdtStandardOwner

**Location**: `Models/DdtStandardOwner.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `UserId` (int, required, FK)
- `User` (User) - Navigation property
- `Role` (string?, max 50) - Owner role
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardContact

**Location**: `Models/DdtStandardContact.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `UserId` (int, required, FK)
- `User` (User) - Navigation property
- `Role` (string?, max 50) - Contact role
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardCategory

**Location**: `Models/DdtStandardCategory.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `CategoryId` (int, required, FK)
- `Category` (StandardCategory) - Navigation property
- `ExternalCategoryId` (int?) - External category ID (legacy)
- `CreatedAt` (DateTime, required)

### DdtStandardSubCategory

**Location**: `Models/DdtStandardSubCategory.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `SubCategoryId` (int, required, FK)
- `SubCategory` (StandardSubCategory) - Navigation property
- `CreatedAt` (DateTime, required)

### DdtStandardPhase

**Location**: `Models/DdtStandardPhase.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `PhaseLookupId` (int, required, FK)
- `PhaseLookup` (PhaseLookup) - Navigation property
- `Enabled` (bool) - Phase enabled flag
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardProduct

**Location**: `Models/DdtStandardProduct.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `StandardProductId` (int, required, FK)
- `StandardProduct` (StandardProduct) - Navigation property
- `ProductType` (string, required, max 50) - "Approved" or "Tolerated"
- `Notes` (string?, max 1000) - Optional notes
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardException

**Location**: `Models/DdtStandardException.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `Title` (string, required, max 500) - Exception title
- `Description` (string?, nvarchar(max)) - Detailed description
- `Reason` (string?, max 2000) - Reason for exception
- `StandardProductId` (int?, FK) - Related product
- `StandardProduct` (StandardProduct?) - Navigation property
- `FipsProductId` (string?, max 50) - FIPS Product ID
- `Status` (string, required, max 50) - "Active", "Expired", "Revoked"
- `GrantedAt` (DateTime, required) - When exception was granted
- `ExpiresAt` (DateTime?) - Expiration date
- `GrantedByUserId` (int?, FK) - User who granted exception
- `GrantedByUser` (User?) - Navigation property
- `CreatedByUserId` (int?, FK) - User who created record
- `CreatedByUser` (User?) - Navigation property
- `Notes` (string?, nvarchar(max)) - Additional notes
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardComment

**Location**: `Models/DdtStandardComment.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `Title` (string, required, max 500) - Comment title
- `Comments` (string, required, nvarchar(max)) - Comment content
- `CommentType` (string?, max 50) - Comment type
- `Field` (string?, max 100) - Field being commented on
- `ParentCommentId` (int?, FK) - Parent comment (for threading)
- `ParentComment` (DdtStandardComment?) - Navigation property
- `Replies` (ICollection<DdtStandardComment>) - Child comments
- `IsResolved` (bool) - Resolved flag
- `ResolvedAt` (DateTime?) - Resolution timestamp
- `ResolvedByUserId` (int?, FK) - User who resolved
- `ResolvedByUser` (User?) - Navigation property
- `CreatedByUserId` (int?, FK) - Comment author
- `CreatedByUser` (User?) - Navigation property
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardVersion

**Location**: `Models/DdtStandardVersion.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `Version` (string, required, max 20) - Version number
- `VersionData` (string?, nvarchar(max)) - Serialized version data
- `CreatedAt` (DateTime, required)

### DdtStandardValidationRule

**Location**: `Models/DdtStandardValidationRule.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `RuleType` (string, required, max 50) - Rule type
- `RuleData` (string?, nvarchar(max)) - Rule configuration (JSON)
- `CreatedAt` (DateTime, required)
- `UpdatedAt` (DateTime, required)

### DdtStandardUnpublishAudit

**Location**: `Models/DdtStandardUnpublishAudit.cs`

**Properties**:
- `Id` (int, PK, Identity)
- `DdtStandardId` (int, required, FK)
- `DdtStandard` (DdtStandard) - Navigation property
- `Reason` (string, required, nvarchar(max)) - Unpublish reason
- `UnpublishedByUserId` (int?, FK) - User who unpublished
- `UnpublishedByUser` (User?) - Navigation property
- `UnpublishedAt` (DateTime, required) - Unpublish timestamp

---

## View Models

### DdtStandardsManageViewModel

**Location**: `ViewModels/DdtStandardsManageViewModel.cs`

**Properties**:
- `MyDrafts` (List<DdtStandard>)
- `AllDrafts` (List<DdtStandard>)
- `MyInReview` (List<DdtStandard>)
- `AllInReview` (List<DdtStandard>)
- `MyForApproval` (List<DdtStandard>)
- `AllForApproval` (List<DdtStandard>)
- `MyPublished` (List<DdtStandard>)
- `AllPublished` (List<DdtStandard>)
- `MyUnpublished` (List<DdtStandard>)
- `AllUnpublished` (List<DdtStandard>)
- `Stages` (List<string>)
- `Categories` (List<string>)
- `Creators` (List<(int Id, string Name)>)
- `Owners` (List<(int Id, string Name)>)
- `Contacts` (List<(int Id, string Name)>)
- `CurrentSearch` (string?)
- `CurrentStage` (string?)
- `CurrentCategory` (string?)
- `CurrentCreator` (int?)
- `CurrentOwner` (int?)
- `CurrentContact` (int?)
- `CurrentLegalStandard` (bool?)
- `ActiveView` (string) - Active navigation view (default: "drafts")

---

## Database Schema

### DbSets in CompassDbContext

**Location**: `Data/CompassDbContext.cs`

```csharp
public DbSet<DdtStandard> DdtStandards { get; set; }
public DbSet<DdtStandardOwner> DdtStandardOwners { get; set; }
public DbSet<DdtStandardContact> DdtStandardContacts { get; set; }
public DbSet<DdtStandardPhase> DdtStandardPhases { get; set; }
public DbSet<DdtStandardValidationRule> DdtStandardValidationRules { get; set; }
public DbSet<DdtStandardVersion> DdtStandardVersions { get; set; }
public DbSet<DdtStandardCategory> DdtStandardCategories { get; set; }
public DbSet<DdtStandardSubCategory> DdtStandardSubCategories { get; set; }
public DbSet<DdtStandardComment> DdtStandardComments { get; set; }
public DbSet<DdtStandardProduct> DdtStandardProducts { get; set; }
public DbSet<DdtStandardException> DdtStandardExceptions { get; set; }
public DbSet<DdtStandardUnpublishAudit> DdtStandardUnpublishAudits { get; set; }
```

### Key Relationships

1. **DdtStandard** → **DdtStandardOwner** (One-to-Many)
2. **DdtStandard** → **DdtStandardContact** (One-to-Many)
3. **DdtStandard** → **DdtStandardCategory** (One-to-Many) → **StandardCategory** (Many-to-One)
4. **DdtStandard** → **DdtStandardSubCategory** (One-to-Many) → **StandardSubCategory** (Many-to-One)
5. **DdtStandard** → **DdtStandardPhase** (One-to-Many) → **PhaseLookup** (Many-to-One)
6. **DdtStandard** → **DdtStandardProduct** (One-to-Many) → **StandardProduct** (Many-to-One)
7. **DdtStandard** → **DdtStandardException** (One-to-Many)
8. **DdtStandard** → **DdtStandardComment** (One-to-Many, with self-referencing for threading)
9. **DdtStandard** → **DdtStandardVersion** (One-to-Many)
10. **DdtStandard** → **DdtStandardValidationRule** (One-to-Many)
11. **DdtStandard** → **DdtStandardUnpublishAudit** (One-to-Many)
12. **DdtStandard** → **DdtStandard** (Self-referencing for ParentStandard)

### Indexes

- `DdtStandards.Title` - Unique constraint
- `DdtStandards.Slug` - Unique constraint
- `DdtStandards.StandardUuid` - Unique constraint
- `DdtStandards.LegacyId` - Index (if exists)

---

## Workflow States

### Stage Values

- **Draft** - Initial state, being created/edited
- **Under Review** - Submitted for review
- **Approved** - Approved by standards forum
- **Published** - Published and visible to all users
- **Rejected** - Rejected during review
- **Unpublished** - Previously published, now unpublished
- **Archived** - Archived (not currently used)

### Stage Transitions

1. **Draft** → **Under Review** (via `SubmitForReview`)
2. **Under Review** → **Approved** (via `Approve`)
3. **Under Review** → **Rejected** (via `Reject`)
4. **Approved** → **Published** (via `Publish`)
5. **Published** → **Unpublished** (via `Unpublish`)
6. **Published** → **Draft** (via `MakeChange` - creates new draft)

---

## Permissions

### Standards Managers Group

Users in the "Standards Managers" group have additional permissions:

- Manage approved products
- Manage exceptions
- Update standard metadata (title, owners, contacts, categories, sub-categories, legacy reference)
- View all standards regardless of ownership

### Standard Owners/Contacts

- Can edit their own standards (if in Draft stage)
- Can perform actions on published standards (Make a change, Unpublish)
- Can view their standards in "My" sections

---

## API Integration

### Standards CMS API

The system integrates with an external Standards CMS API (Strapi) for:

- Migrating standards from external CMS
- Fetching categories, stages, sub-categories
- Syncing standard data

**Configuration**:
- Base URL: `StandardsCmsApi:BaseUrl`
- API Key: `StandardsCmsApi:ReadApiKey`
- Caching: Uses `IMemoryCache` with configurable duration

---

## Notes

- All timestamps use UTC
- Soft deletes are used (`IsDeleted` flag)
- Audit logging is implemented via `AuditLog` entity
- Markdown is supported in: Purpose, HowToMeet, Governance, RelatedGuidance
- JSON is used for: Criteria (array of strings)
- User picker integration for owners/contacts uses Entra ID search
- Product search uses autocomplete with StandardProduct lookup
- Exception search supports FIPS Product ID lookup

---

*Generated: 2025-01-29*
*Last Updated: Based on current codebase state*

