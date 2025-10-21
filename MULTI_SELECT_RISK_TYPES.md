# Multi-select risk types feature

## Overview

Risks can now be classified with multiple risk types simultaneously. This recognises that many risks span multiple categories and allows for more accurate classification and reporting.

## Database changes

### Previous structure (single risk type)
- Risks table had `RiskTypeId` foreign key column
- One-to-many relationship (one risk → one risk type)

### New structure (multiple risk types)
- Removed `RiskTypeId` column from Risks table
- Created `RiskRiskTypes` junction table
- Many-to-many relationship (one risk → many risk types)

### RiskRiskTypes junction table

| Column     | Type                 | Description                            |
| ---------- | -------------------- | -------------------------------------- |
| RiskId     | INTEGER / INT        | Foreign key to Risks table (PK part 1) |
| RiskTypeId | INTEGER / INT        | Foreign key to RiskTypes table (PK part 2) |
| CreatedAt  | DATETIME / DATETIME2 | UTC timestamp when association created |

**Primary key:** Composite key on (RiskId, RiskTypeId)

**Indexes:**
- Index on `RiskTypeId` for efficient reverse lookups

**Foreign keys:**
- `RiskId` → `Risks.Id` (CASCADE delete - when risk deleted, associations deleted)
- `RiskTypeId` → `RiskTypes.Id` (RESTRICT delete - cannot delete risk type if in use)

## User interface changes

### Create risk form

**Before:** Single dropdown selection
- Only one risk type could be selected
- Required navigation away to change type

**After:** Checkbox list
- Multiple risk types can be selected
- Each risk type shows name and summary
- All active risk types displayed
- Optional - can create risk without selecting any types

**Example:**
```html
☑ Strategy risk
  Poorly defined or outdated strategic direction.

☐ Technology risk
  Technology failures or insufficient system resilience.

☑ Security risk
  Cyber, data, or physical security breaches.
```

### Edit risk form

**Same checkbox interface as create:**
- Pre-selected checkboxes show currently assigned types
- Can add or remove types
- Changes saved when form submitted

### Details/view risk page

**Before:** Single risk type displayed

**After:** List of all assigned risk types
- Bullet list format
- Each type shows name and summary
- Shows "-" if no types assigned

## Controller changes

### RiskController

**Create action:**
- Accepts `int[] selectedRiskTypes` parameter
- After saving risk, creates RiskRiskType entries for each selected type
- Transaction ensures consistency

**Edit action:**
- Loads existing risk types via `Include(r => r.RiskRiskTypes)`
- Populates `ViewBag.SelectedRiskTypeIds` for checkboxes
- Accepts `int[] selectedRiskTypes` parameter
- Removes all existing associations
- Creates new associations for selected types
- Single database transaction

**Details action:**
- Eager loads risk types: `.Include(r => r.RiskRiskTypes).ThenInclude(rrt => rrt.RiskType)`
- Risk types available in view via navigation properties

### AdminController

**DeleteRiskType actions:**
- Updated to check `RiskRiskTypes` table instead of `Risks.RiskTypeId`
- Counts associations: `_context.RiskRiskTypes.CountAsync(rrt => rrt.RiskTypeId == id)`
- Prevents deletion if any associations exist

## Model changes

### Risk.cs

**Removed:**
```csharp
public int? RiskTypeId { get; set; }

[ForeignKey(nameof(RiskTypeId))]
public RiskType? RiskType { get; set; }
```

**Added:**
```csharp
public ICollection<RiskRiskType> RiskRiskTypes { get; set; } = new List<RiskRiskType>();
```

### New model: RiskRiskType.cs

```csharp
public class RiskRiskType
{
    [Required]
    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required]
    public int RiskTypeId { get; set; }

    [ForeignKey(nameof(RiskTypeId))]
    public RiskType RiskType { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

## Entity Framework configuration

### CompassDbContext.cs

**Junction table configuration:**
```csharp
// Composite primary key
modelBuilder.Entity<RiskRiskType>()
    .HasKey(rrt => new { rrt.RiskId, rrt.RiskTypeId });

// Risk relationship (cascade delete)
modelBuilder.Entity<RiskRiskType>()
    .HasOne(rrt => rrt.Risk)
    .WithMany(r => r.RiskRiskTypes)
    .HasForeignKey(rrt => rrt.RiskId)
    .OnDelete(DeleteBehavior.Cascade);

// RiskType relationship (restrict delete)
modelBuilder.Entity<RiskRiskType>()
    .HasOne(rrt => rrt.RiskType)
    .WithMany()
    .HasForeignKey(rrt => rrt.RiskTypeId)
    .OnDelete(DeleteBehavior.Restrict);

// Index for efficient lookups
modelBuilder.Entity<RiskRiskType>()
    .HasIndex(rrt => rrt.RiskTypeId);
```

## Migration

**Migration name:** `ChangeRiskTypeToManyToMany` (20251017202224)

**Changes:**
1. Dropped `IX_Risks_RiskTypeId` index
2. Created `RiskRiskTypes` junction table
3. Created composite primary key on (RiskId, RiskTypeId)
4. Created foreign keys with appropriate cascade behaviour
5. Created index on RiskTypeId
6. Removed `RiskTypeId` column from Risks table (data loss)

**Note:** Any existing RiskTypeId assignments were lost during migration. Risks must be re-classified if previously typed.

## Use cases

### Example 1: Technology project risk
A risk related to a cloud migration project might be classified as:
- ✓ Technology risk (system performance concerns)
- ✓ Project/Programme risk (delivery timeline concerns)
- ✓ Financial risk (budget overrun concerns)

### Example 2: Data breach risk
A cybersecurity risk might be:
- ✓ Security risk (unauthorised access)
- ✓ Legal risk (GDPR compliance)
- ✓ Reputational risk (loss of trust)
- ✓ Information risk (data integrity)

### Example 3: Organisational change risk
A restructuring risk might be:
- ✓ People risk (staff concerns, capacity)
- ✓ Governance risk (unclear accountability)
- ✓ Operations risk (process disruption)

## Benefits

1. **More accurate classification**
   - Reflects reality that risks often span multiple categories
   - No need to force-fit into single category

2. **Better reporting**
   - Risks appear in multiple category reports
   - More comprehensive view of risk landscape
   - Easier to identify cross-cutting themes

3. **Improved risk analysis**
   - Can analyse risks that affect multiple areas
   - Identify which risk types frequently co-occur
   - Better understanding of systemic risks

4. **Flexibility**
   - Can add/remove types as risk evolves
   - No limit on number of types assigned
   - Still optional - can have zero types

## Reporting considerations

When reporting on risks by type:
- A risk with 3 types will appear in 3 category reports
- Count of risks per type may sum to more than total risks
- Use DISTINCT counts when calculating unique risk numbers

**Example query considerations:**
```csharp
// Count risks with Technology type (may include same risk in multiple categories)
var techRiskCount = await _context.RiskRiskTypes
    .Where(rrt => rrt.RiskTypeId == technologyTypeId)
    .Count();

// Count unique risks with Technology type
var uniqueTechRisks = await _context.RiskRiskTypes
    .Where(rrt => rrt.RiskTypeId == technologyTypeId)
    .Select(rrt => rrt.RiskId)
    .Distinct()
    .Count();
```

## Data integrity

**Protected relationships:**
- Cannot delete risk type if any risks are assigned to it
- Deleting a risk automatically removes its type associations (cascade)
- Composite primary key prevents duplicate associations

**Validation:**
- No validation currently enforces minimum/maximum number of types
- Future enhancement: could require at least one type
- Future enhancement: could limit maximum number (e.g., 5 types max)

## Future enhancements

Potential improvements:
1. **Primary/secondary types** - Flag one type as primary
2. **Type weighting** - Indicate relative importance of each type
3. **Suggested types** - AI-suggested types based on description
4. **Type combinations** - Predefined sets for common scenarios
5. **Reporting views** - Filter/group by type combinations
6. **Analytics** - Most common type combinations
7. **Validation rules** - Require certain types for certain risk scores

## User guidance

**When to use multiple types:**
- Risk genuinely spans multiple risk categories
- Different aspects of risk fit different categories
- For reporting in multiple category views

**When to use single type:**
- Risk clearly fits one category
- Secondary categorisations not significant
- Simpler classification preferred

**When to use no types:**
- Risk doesn't fit standard taxonomy
- Custom categorisation via Category field
- Types to be assigned later

## Accessibility

Checkbox list implementation:
- Follows GOV.UK design patterns
- Proper label associations (`for` attribute)
- Keyboard navigable
- Screen reader compatible
- Clear visual indication of selection
- Summary text provides context

---

**Created:** 17 October 2025  
**Migration:** 20251017202224_ChangeRiskTypeToManyToMany  
**Relationship:** Many-to-many via RiskRiskTypes junction table  
**Version:** 2.0

