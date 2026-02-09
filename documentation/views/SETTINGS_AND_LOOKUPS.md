# Settings and lookup values

## Overview

The Settings feature provides administrators with the ability to manage lookup values used throughout Compass. This ensures consistency in how data is classified and categorized while giving administrators control over the available options.

## Current lookups

### Risk types

Risk types provide a standardized taxonomy for classifying risks across the organisation. Based on best practice risk management frameworks, these classifications help teams identify, categorise, and report on risks consistently.

## Database structure

### RiskType table

| Column      | Type                   | Description                                                |
| ----------- | ---------------------- | ---------------------------------------------------------- |
| Id          | INTEGER / INT IDENTITY | Primary key                                                |
| Code        | TEXT / NVARCHAR(50)    | Unique identifier (e.g., `STRATEGY`, `TECHNOLOGY`)         |
| Name        | TEXT / NVARCHAR(100)   | Display name (e.g., `Strategy risk`, `Technology risk`)    |
| Description | TEXT / NVARCHAR(MAX)   | Full definition with examples and guidance                 |
| Summary     | TEXT / NVARCHAR(200)   | Concise one-line description                               |
| IsActive    | INTEGER / BIT          | Whether this type appears in dropdowns (1=active, 0=inactive) |
| CreatedAt   | DATETIME / DATETIME2   | UTC timestamp when record was created                      |
| UpdatedAt   | DATETIME / DATETIME2   | UTC timestamp when record was last updated                 |

**Indexes:**
- Unique index on `Code`
- Index on `IsActive` for filtering active types

## Seeded risk types

The following 13 risk types are pre-populated:

| Code         | Name                   | Summary                                                   |
| ------------ | ---------------------- | --------------------------------------------------------- |
| STRATEGY     | Strategy risk          | Poorly defined or outdated strategic direction            |
| GOVERNANCE   | Governance risk        | Weak oversight, unclear accountability, or poor governance |
| OPERATIONS   | Operations risk        | Failures in internal processes or delivery operations     |
| LEGAL        | Legal risk             | Exposure to legal, regulatory, or contractual liabilities |
| PROPERTY     | Property risk          | Property, estate, or safety management deficiencies       |
| FINANCIAL    | Financial risk         | Ineffective financial control or value-for-money failure  |
| COMMERCIAL   | Commercial risk        | Failures in contracts, suppliers, or commercial management |
| PEOPLE       | People risk            | Leadership, culture, or workforce capacity/capability issues |
| TECHNOLOGY   | Technology risk        | Technology failures or insufficient system resilience     |
| INFORMATION  | Information risk       | Data quality, availability, or misuse issues              |
| SECURITY     | Security risk          | Cyber, data, or physical security breaches                |
| PROJECT      | Project/Programme risk | Programme or project delivery failures                    |
| REPUTATIONAL | Reputational risk      | Loss of stakeholder trust or public confidence            |

## Integration with Risk model

### Risk table updates

The `Risks` table now includes:
```sql
RiskTypeId INTEGER / INT NULL
CONSTRAINT FK_Risks_RiskTypes FOREIGN KEY (RiskTypeId) REFERENCES RiskTypes(Id)
```

### Model changes

**Risk.cs:**
```csharp
public int? RiskTypeId { get; set; }

[ForeignKey(nameof(RiskTypeId))]
public RiskType? RiskType { get; set; }
```

Risk type is optional - risks can be created without selecting a type.

## Admin functionality

### Access Settings

Navigate to: **Administration > Settings**

This displays the Risk types management page.

### View risk types

The main page shows all risk types (active and inactive) with:
- Code (unique identifier)
- Name (display name)
- Summary (concise description)
- Status badge (Active/Inactive)
- Edit and Delete actions

### Create new risk type

1. Click **Create new** button
2. Fill in required fields:
   - **Code**: Unique uppercase identifier (no spaces)
   - **Name**: Display name
   - **Summary**: One-line description (optional)
   - **Description**: Full definition and guidance (optional)
   - **Active**: Toggle whether this type appears in dropdowns
3. Click **Create**

**Validation:**
- Code must be unique
- Code and Name are required
- Code limited to 50 characters
- Name limited to 100 characters
- Summary limited to 200 characters

### Edit risk type

1. Click **Edit** on the risk type row
2. Modify fields as needed
3. Click **Update**

**Notes:**
- Changing the code will fail if another risk type already uses that code
- Can deactivate types to hide them from dropdowns without deleting
- Inactive types remain visible on existing risks

### Delete risk type

1. Click **Delete** on the risk type row
2. Review the risk type details
3. Confirm deletion

**Protection:**
- Cannot delete a risk type that is assigned to any risks
- Warning message shows count of risks using this type
- Must reassign those risks to different types first

### Active vs inactive

**Active risk types:**
- Appear in risk creation/edit dropdowns
- Available for selection when creating new risks
- Recommended for current use

**Inactive risk types:**
- Hidden from dropdowns
- Cannot be selected for new risks
- Still visible on existing risks that use them
- Use for deprecated or legacy classifications

## Using risk types

### Creating a risk with type

1. Navigate to **Product Governance > Risks**
2. Click **Create new**
3. Select risk type from dropdown (optional)
4. Complete other required fields
5. Save

### Viewing risk type on risk details

The risk details page displays:
- Risk type name in bold
- Summary description below (if available)
- Shows "-" if no type selected

### Filtering by risk type (future)

Future enhancements may include:
- Filter risks by type on index page
- Risk type analytics and counts
- Type-specific risk templates

## Best practices

### Naming conventions

**Codes:**
- Use UPPERCASE
- No spaces or special characters
- Descriptive but concise
- Examples: `STRATEGY`, `TECH`, `FINANCE`

**Names:**
- Clear and professional
- Include "risk" suffix for clarity
- Examples: "Strategy risk", "Technology risk"

**Summaries:**
- One concise sentence
- Focus on key characteristics
- Examples provided in seeded data

**Descriptions:**
- Comprehensive definition
- Include examples
- Explain when to use this type
- Guidance on assessment

### Managing changes

**Before deactivating a type:**
1. Check how many risks use it
2. Consider if it's truly obsolete
3. Plan communication to users
4. Deactivate rather than delete

**Before deleting a type:**
1. Verify zero risks use it
2. Export risk type data for records
3. Consider deactivating instead
4. Cannot undo deletion

**When creating new types:**
1. Check if existing types can be used
2. Ensure name is distinct
3. Provide clear summary
4. Add comprehensive description

## Migration history

1. **AddRiskTypeLookup** (20251017201112)
   - Created `RiskTypes` table
   - Seeded 13 standard risk types
   - Created unique index on `Code`
   - Created index on `IsActive`

2. **AddRiskTypeIdToRisk** (20251017201320)
   - Added `RiskTypeId` foreign key to `Risks` table
   - Created index on `RiskTypeId`
   - Configured cascade behavior

## Navigation structure

```
Administration
├── Users
├── Performance metrics
├── Enterprise metrics
├── Functional standards
├── Strategic objectives
└── Settings (→ Risk types)
```

## Future lookup types

The Settings area can be expanded to manage additional lookup types:

### Potential future lookups

- **Issue types**: Categorise issues by type
- **Action priorities**: Standardised priority levels
- **Milestone statuses**: Standard status values
- **Response strategies**: Risk response options
- **Impact scales**: Customisable impact ratings
- **Likelihood scales**: Customisable likelihood ratings

### Implementation pattern

Each new lookup type would follow the same pattern:
1. Create model with Code, Name, Description, Summary, IsActive
2. Add DbSet to context
3. Create migration with seeded data
4. Add admin controller actions
5. Create CRUD views
6. Update Settings navigation
7. Link to parent entities

## API considerations

While currently admin-only, the Settings feature could be extended:
- Read-only API endpoints for lookup values
- Integration with external systems
- Export/import functionality
- Audit trail of changes

## Security

**Access control:**
- Settings page requires authentication
- Standard admin permissions apply
- No additional role-based restrictions currently

**Data integrity:**
- Unique constraint on Code prevents duplicates
- Foreign key prevents orphaned risk types
- Soft delete via IsActive rather than hard delete

## Performance

**Caching:**
- Risk types cached in SelectList on form load
- No server-side caching currently
- Small dataset (< 50 types expected)
- Fast lookups via indexed Code

**Optimization:**
- Only active types loaded in dropdowns
- Eager loading of RiskType on Risk details
- Indexed for efficient queries

## Documentation for users

Administrators should provide guidance to risk owners on:
1. Which risk type to select for common scenarios
2. When multiple types could apply
3. What to do if no type fits
4. How risk types affect reporting

Consider creating:
- Risk type decision tree
- Example risks by type
- FAQ document
- Training materials

---

**Created:** 17 October 2025  
**Migrations:** AddRiskTypeLookup (20251017201112), AddRiskTypeIdToRisk (20251017201320)  
**Features:** CRUD operations, active/inactive toggle, referential integrity  
**Version:** 1.0

