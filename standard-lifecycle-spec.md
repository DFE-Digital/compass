# STANDARD-LIFECYCLE-SPEC.md

DDT Standards – Full Lifecycle, Versioning, Copying, and Publishing Specification
-------------------------------------------------------------------------------

## 1. Purpose

This specification defines the authoritative lifecycle model for DDT Standards within COMPASS. It covers:

- Standard lifecycle stages and state transitions
- Versioning strategy and semantic versioning rules
- Copying standards ("Make a Change" functionality)
- Publishing and unpublishing workflows
- Permission requirements for each operation
- Audit logging and version history

## 2. Lifecycle Stages

### 2.1 Stage Definitions

DDT Standards progress through the following stages:

| Stage | Description | Visibility | Editable By |
|-------|-------------|------------|-------------|
| **Draft** | Initial creation or editing state | Creator, Owners, Contacts, Standards Managers | Creator, Owners, Contacts |
| **Under Review** | Submitted for review by standards forum | All authenticated users | Standards Managers only |
| **Approved** | Approved by standards forum, ready for publication | All authenticated users | Standards Managers only |
| **Published** | Published and visible to all users | All users (public) | Owners, Contacts, Standards Managers (limited actions) |
| **Rejected** | Rejected during review, can be revised | Creator, Owners, Standards Managers | Creator, Owners (to revise) |
| **Unpublished** | Previously published, now unpublished | Standards Managers, Owners, Contacts | Standards Managers only |
| **Archived** | Archived (not currently used) | Standards Managers | Standards Managers only |

### 2.2 Stage Properties

Each stage has associated properties:

- **Draft**: `Stage = "Draft"`, `IsPublished = false`, `PublishedAt = null`
- **Under Review**: `Stage = "Under Review"`, `IsPublished = false`
- **Approved**: `Stage = "Approved"`, `IsPublished = false`
- **Published**: `Stage = "Published"`, `IsPublished = true`, `PublishedAt = DateTime.UtcNow`
- **Rejected**: `Stage = "Rejected"`, `IsPublished = false`
- **Unpublished**: `Stage = "Unpublished"`, `IsPublished = false`, `PublishedAt` preserved for history

## 3. State Transitions

### 3.1 Valid Transitions

```
Draft → Under Review → Approved → Published
  ↓         ↓
Rejected   Rejected
  ↓
Draft (revise and resubmit)

Published → Unpublished
Published → Draft (via "Make a Change" - creates new standard)
```

### 3.2 Transition Rules

#### 3.2.1 Draft → Under Review
- **Action**: `SubmitForReview`
- **Who**: Creator, Owners, Contacts
- **Requirements**:
  - Standard must be in Draft stage
  - At least one owner must be assigned
  - Governance field must be completed
  - Validity period must be set
- **Effects**:
  - `Stage = "Under Review"`
  - `UpdatedAt = DateTime.UtcNow`
  - Audit log entry created

#### 3.2.2 Under Review → Approved
- **Action**: `Approve`
- **Who**: Standards Managers only
- **Requirements**:
  - Standard must be in "Under Review" or "Approved" stage
- **Effects**:
  - `Stage = "Approved"`
  - `UpdatedAt = DateTime.UtcNow`
  - Optional approval comment added
  - **Special Case**: If standard has a parent (created via "Make a Change"):
    - Parent standard is automatically unpublished
    - Parent `Stage = "Unpublished"`
    - Parent `IsPublished = false`
    - Unpublish audit log created for parent
    - New standard is immediately published (see 3.2.4)

#### 3.2.3 Under Review → Rejected
- **Action**: `Reject`
- **Who**: Standards Managers only
- **Requirements**:
  - Standard must be in "Under Review" stage
  - Reason must be provided
- **Effects**:
  - `Stage = "Rejected"`
  - `UpdatedAt = DateTime.UtcNow`
  - Rejection reason stored in comment
  - Standard can be revised and resubmitted

#### 3.2.4 Approved → Published
- **Action**: `Publish`
- **Who**: Admin, SuperAdmin roles only
- **Requirements**:
  - Standard must be in "Approved" stage
- **Effects**:
  - `Stage = "Published"`
  - `IsPublished = true`
  - `PublishedAt = DateTime.UtcNow`
  - `FirstPublished = DateTime.UtcNow` (if not already set)
  - `LastUpdated = DateTime.UtcNow`
  - `IsModified = false`
  - Version incremented (see Section 4)
  - Version history entry created
  - Standard becomes publicly visible

#### 3.2.5 Published → Unpublished
- **Action**: `Unpublish`
- **Who**: Owners, Contacts, Standards Managers
- **Requirements**:
  - Standard must be in "Published" stage
  - Reason must be provided
- **Effects**:
  - `Stage = "Unpublished"`
  - `IsPublished = false`
  - `UpdatedAt = DateTime.UtcNow`
  - Unpublish audit log entry created with reason
  - Standard removed from public view
  - Historical `PublishedAt` date preserved

#### 3.2.6 Published → Draft (Make a Change)
- **Action**: `MakeChange`
- **Who**: Owners, Contacts, Standards Managers
- **Requirements**:
  - Standard must be in "Published" stage
- **Effects**:
  - Creates a **new** standard (child) with:
    - `ParentStandardId` = original standard ID
    - `Stage = "Draft"`
    - `Version` = parent version (no increment yet)
    - All content copied from parent
    - All relationships copied (owners, contacts, categories, sub-categories, phases, products)
    - `IsPublished = false`
    - `PublishedAt = null`
  - Original standard remains published until child is approved
  - When child is approved, parent is automatically unpublished (see 3.2.2)

#### 3.2.7 Rejected → Draft
- **Action**: Edit and resubmit
- **Who**: Creator, Owners
- **Requirements**:
  - Standard must be in "Rejected" stage
- **Effects**:
  - `Stage = "Draft"`
  - Standard can be edited and resubmitted
  - Version may include "-resubmit" suffix for tracking

### 3.3 Invalid Transitions

The following transitions are **not allowed**:

- Draft → Published (must go through review)
- Under Review → Published (must be approved first)
- Approved → Draft (use "Make a Change" instead)
- Unpublished → Published (must create new version via "Make a Change")
- Any transition skipping stages

## 4. Versioning

### 4.1 Semantic Versioning

DDT Standards use **Semantic Versioning** (SemVer) format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes that require significant updates
- **MINOR**: New features or significant additions (backward compatible)
- **PATCH**: Bug fixes, clarifications, minor updates (backward compatible)

**Default Version**: `0.1.0` (for new standards)

### 4.2 Version Increment Rules

#### 4.2.1 Initial Publication
- First publication: `0.1.0` → `1.0.0` (MAJOR increment)
- Standard becomes version 1.0.0 when first published

#### 4.2.2 Subsequent Publications
Version increment is determined by the type of changes:

- **PATCH** (default): `1.0.0` → `1.0.1`
  - Minor clarifications
  - Typo corrections
  - Formatting improvements
  - Non-substantive updates

- **MINOR**: `1.0.0` → `1.1.0`
  - New approved/tolerated products added
  - New exceptions added
  - Additional guidance or examples
  - Non-breaking additions

- **MAJOR**: `1.0.0` → `2.0.0`
  - Breaking changes to criteria
  - Changes to legal requirements
  - Significant changes to "How to Meet"
  - Changes affecting compliance

**Current Implementation**: Defaults to PATCH increment. Future enhancement: automatic detection of change type.

#### 4.2.3 Version Tracking

Each standard maintains:
- `Version`: Current version string
- `PreviousVersion`: Previous version string (set on publish)
- `Versions` collection: Full version history (immutable)

### 4.3 Version History

Version history is stored in `DdtStandardVersion` table:

- **Immutable**: Once created, version entries cannot be modified
- **Snapshot**: Can store full JSON snapshot of standard at publication
- **Metadata**: Includes version type, change summary, change details, breaking change flag
- **Audit**: Tracks who created and published each version

### 4.4 Version Comparison

When a standard is created via "Make a Change":
- Child standard starts with parent's version
- Version is incremented when child is published
- Parent version is preserved in `PreviousVersion` of child

## 5. Copying Standards ("Make a Change")

### 5.1 Purpose

"Make a Change" allows creating a new version of a published standard while keeping the original published until the new version is approved.

### 5.2 Process

1. **User Action**: Owner/Contact/Standards Manager clicks "Make a change" on published standard
2. **System Action**: Creates new standard with:
   - `ParentStandardId` = original standard ID
   - All content fields copied
   - All relationships copied:
     - Owners
     - Contacts
     - Categories
     - Sub-categories
     - Phases
     - Products (approved and tolerated)
     - Exceptions (linked, not copied)
   - `Stage = "Draft"`
   - `Version` = parent version (e.g., "1.0.0")
   - `IsPublished = false`
   - `PublishedAt = null`
   - `FirstPublished = null`
   - `LastUpdated = null`
   - `IsModified = false`
3. **User Action**: Edit the new draft standard
4. **User Action**: Submit for review
5. **System Action**: Standard goes through normal review process
6. **System Action**: When approved, parent is automatically unpublished

### 5.3 Parent-Child Relationship

- **Parent**: Original published standard
- **Child**: New draft created from parent
- **Relationship**: `DdtStandard.ParentStandardId` → `DdtStandard.Id`
- **Behavior**: Only one level of parent-child (no grandparent tracking)

### 5.4 Version Handling in "Make a Change"

- Child starts with parent's version
- When child is published, version is incremented based on change type
- Parent version is stored in child's `PreviousVersion`
- Parent is automatically unpublished when child is approved

## 6. Publishing Workflow

### 6.1 Publication Requirements

Before a standard can be published, it must have:

1. **Required Fields**:
   - Title (unique)
   - Summary
   - Purpose
   - Governance
   - Validity Period
   - At least one Owner

2. **Content Completeness**:
   - All mandatory sections completed
   - No validation errors

3. **Approval**:
   - Must be in "Approved" stage
   - Approved by Standards Manager

### 6.2 Publication Process

1. Standard is in "Approved" stage
2. Admin/SuperAdmin calls `Publish` action
3. System performs:
   - Validates stage is "Approved"
   - Sets `Stage = "Published"`
   - Sets `IsPublished = true`
   - Sets `PublishedAt = DateTime.UtcNow`
   - Sets `FirstPublished` if not already set
   - Increments version
   - Creates version history entry
   - Sets `IsModified = false`
   - Updates `LastUpdated` and `UpdatedAt`
4. Standard becomes publicly visible

### 6.3 Post-Publication

After publication:
- Standard appears in "Published" view
- Standard is visible to all users
- Standard can be viewed via Details page
- Standard can have "Make a Change" action performed
- Standard can be unpublished

## 7. Unpublishing Workflow

### 7.1 Unpublishing Requirements

- Standard must be in "Published" stage
- User must be Owner, Contact, or Standards Manager
- Reason must be provided

### 7.2 Unpublishing Process

1. User navigates to Unpublish page
2. User provides reason (required, max 2000 characters)
3. System performs:
   - Validates stage is "Published"
   - Sets `Stage = "Unpublished"`
   - Sets `IsPublished = false`
   - Preserves `PublishedAt` for history
   - Creates `DdtStandardUnpublishAudit` entry with:
     - Reason
     - UnpublishedByUserId
     - UnpublishedAt timestamp
   - Updates `UpdatedAt`
4. Standard is removed from public view
5. Standard appears in "Unpublished" view

### 7.3 After Unpublishing

- Standard is no longer publicly visible
- Historical data is preserved
- Standard cannot be directly republished
- New version must be created via "Make a Change"

## 8. Permissions

### 8.1 Role-Based Permissions

#### Standards Managers
- **Full Access**: All standards regardless of ownership
- **Can**:
  - Approve standards
  - Reject standards
  - Manage approved products
  - Manage exceptions
  - Update standard metadata (title, owners, contacts, categories, sub-categories, legacy reference)
  - Unpublish standards
  - Make changes to any published standard

#### Standard Owners
- **Own Standards**: Standards where user is listed as owner
- **Can**:
  - Edit own standards (if in Draft stage)
  - Submit own standards for review
  - Make changes to own published standards
  - Unpublish own published standards
  - View own standards in "My" sections

#### Standard Contacts
- **Contact Standards**: Standards where user is listed as contact
- **Can**:
  - Edit standards (if in Draft stage and user is contact)
  - Make changes to published standards (if user is contact)
  - Unpublish published standards (if user is contact)
  - View standards in "My" sections

#### Creators
- **Created Standards**: Standards created by user
- **Can**:
  - Edit own standards (if in Draft stage)
  - Submit own standards for review
  - Revise rejected standards

#### Admins/SuperAdmins
- **Can**:
  - Publish approved standards
  - All Standards Manager permissions

### 8.2 Stage-Based Permissions

| Stage | Creator | Owner | Contact | Standards Manager | Admin |
|-------|---------|-------|---------|-------------------|-------|
| Draft | Edit, Submit | Edit, Submit | Edit | Edit, Approve, Reject | Edit, Approve, Reject, Publish |
| Under Review | View | View | View | Approve, Reject | Approve, Reject, Publish |
| Approved | View | View | View | Approve, Reject | Publish |
| Published | View, Make Change | View, Make Change, Unpublish | View, Make Change, Unpublish | View, Make Change, Unpublish, Manage | View, Make Change, Unpublish, Manage, Publish |
| Rejected | Edit, Resubmit | Edit, Resubmit | View | View | View |
| Unpublished | View | View | View | View, Manage | View, Manage |

## 9. Audit Logging

### 9.1 Audit Events

The following events are logged:

1. **Standard Created**: When draft is first created
2. **Standard Submitted**: When submitted for review
3. **Standard Approved**: When approved by Standards Manager
4. **Standard Rejected**: When rejected with reason
5. **Standard Published**: When published (includes version info)
6. **Standard Unpublished**: When unpublished (includes reason)
7. **Standard Modified**: When content is changed
8. **Standard Deleted**: When soft-deleted

### 9.2 Audit Log Structure

- **Entity**: `AuditLog`
- **Fields**:
  - `DdtStandardId` (FK to standard)
  - `Action` (string: "Created", "Submitted", "Approved", etc.)
  - `UserId` (who performed action)
  - `Timestamp` (when action occurred)
  - `Details` (JSON with additional context)

### 9.3 Unpublish Audit

Special audit table for unpublishing:

- **Entity**: `DdtStandardUnpublishAudit`
- **Fields**:
  - `DdtStandardId`
  - `Version` (version when unpublished)
  - `Reason` (required, user-provided)
  - `UnpublishedByUserId`
  - `UnpublishedAt`

## 10. Version History

### 10.1 Version History Storage

Version history is stored in `DdtStandardVersion` table:

- **Immutable**: Cannot be modified after creation
- **Snapshot**: Optional full JSON snapshot of standard
- **Metadata**: Version number, type, change summary, change details
- **Audit**: Who created and published

### 10.2 Version History Entry Creation

Version history entry is created when:
- Standard is published (via `Publish` action)
- Includes:
  - `VersionNumber`: New version
  - `PreviousVersion`: Previous version
  - `VersionType`: "major", "minor", or "patch"
  - `ChangeSummary`: Brief description
  - `ChangeDetails`: Detailed changes (optional)
  - `Status`: "published"
  - `CreatedByUserId`: User who published
  - `PublishedAt`: Publication timestamp

## 11. Data Integrity

### 11.1 Soft Deletes

- Standards use soft delete (`IsDeleted` flag)
- Deleted standards are not shown in queries
- Historical data is preserved
- Can be restored if needed

### 11.2 Referential Integrity

- Parent-child relationships: `ParentStandardId` → `DdtStandard.Id`
- Cascade delete: Not used (soft deletes)
- Orphaned standards: Parent must exist if `ParentStandardId` is set

### 11.3 Unique Constraints

- `Title`: Must be unique across all standards
- `Slug`: Must be unique (auto-generated from title)
- `StandardUuid`: Must be unique (immutable identifier)

## 12. Timestamps

All timestamps use **UTC**:

- `CreatedAt`: When standard was first created
- `UpdatedAt`: When standard was last modified
- `DraftCreated`: When draft was first created
- `FirstPublished`: When standard was first published (never changes)
- `PublishedAt`: Current publication date (changes on republish)
- `LastUpdated`: Last update timestamp

## 13. Implementation Notes

### 13.1 Current Limitations

1. **Version Increment**: Currently defaults to PATCH increment. Future: automatic detection of change type.
2. **Version History Snapshots**: Snapshot storage is optional, not always populated.
3. **Breaking Change Detection**: Manual process, not automated.
4. **Multi-level Parent-Child**: Only one level supported (no grandparent tracking).

### 13.2 Future Enhancements

1. **Automatic Version Detection**: Analyze changes to determine major/minor/patch
2. **Change Diff**: Show what changed between versions
3. **Version Comparison**: Compare any two versions side-by-side
4. **Rollback**: Ability to rollback to previous version
5. **Draft Versioning**: Track versions even in draft stage
6. **Version Tags**: Add tags like "beta", "rc1", etc.

## 14. Examples

### 14.1 New Standard Lifecycle

1. User creates new standard → `Stage = "Draft"`, `Version = "0.1.0"`
2. User submits for review → `Stage = "Under Review"`
3. Standards Manager approves → `Stage = "Approved"`
4. Admin publishes → `Stage = "Published"`, `Version = "1.0.0"`, `IsPublished = true`

### 14.2 Making a Change

1. User clicks "Make a change" on published standard v1.0.0
2. System creates new standard:
   - `ParentStandardId = 1` (original)
   - `Stage = "Draft"`
   - `Version = "1.0.0"` (same as parent)
   - All content copied
3. User edits and submits → `Stage = "Under Review"`
4. Standards Manager approves → `Stage = "Approved"`
   - **System automatically**: Parent (v1.0.0) → `Stage = "Unpublished"`
5. Admin publishes → `Stage = "Published"`, `Version = "1.0.1"` (PATCH increment)

### 14.3 Rejection and Resubmission

1. Standard in "Under Review" is rejected → `Stage = "Rejected"`
2. Creator edits standard → `Stage = "Draft"`
3. Creator resubmits → `Stage = "Under Review"`
4. Standards Manager approves → `Stage = "Approved"`
5. Admin publishes → `Stage = "Published"`

## 15. Glossary

- **Standard**: A DDT Standard document
- **Stage**: Current lifecycle stage (Draft, Under Review, etc.)
- **Version**: Semantic version number (MAJOR.MINOR.PATCH)
- **Parent Standard**: Original standard from which a new version was created
- **Child Standard**: New standard created via "Make a Change"
- **Publish**: Make standard publicly visible
- **Unpublish**: Remove standard from public view
- **Soft Delete**: Mark as deleted without removing from database
- **Standards Manager**: User with elevated permissions for standards management

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-29  
**Author**: Based on COMPASS codebase analysis
