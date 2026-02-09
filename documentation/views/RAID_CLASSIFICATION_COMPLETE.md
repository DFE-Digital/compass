# RAID classification system - complete implementation

## Overview

The Compass Product Governance system now includes a comprehensive, multi-dimensional classification scheme that enables precise categorisation, governance alignment, and powerful reporting capabilities.

## Classification dimensions

Each Risk, Issue, Action, or Milestone can be classified across **six dimensions**:

### 1. Strategic objective (optional)
- **Type:** Foreign key to Objectives table
- **Purpose:** Link to organisational strategic goals
- **Selection:** Single dropdown
- **Managed in:** Admin > Strategic Objectives

### 2. Product (optional)
- **Type:** String (FipsId)
- **Purpose:** Link to FIPS product portfolio
- **Selection:** Single dropdown
- **Managed in:** FIPS CMS
- **Data source:** Products API

### 3. Business area (optional)
- **Type:** String
- **Purpose:** Organisational unit classification
- **Selection:** Single dropdown
- **Managed in:** FIPS CMS (Category Type = "Business area")
- **Data source:** Products API, cached 1 hour

### 4. Risk types (optional, RISKS ONLY)
- **Type:** Many-to-many relationship via RiskRiskTypes
- **Purpose:** Classify by risk category (Strategy, Technology, Security, etc.)
- **Selection:** Multiple checkboxes (can select 0-13 types)
- **Managed in:** Admin > Settings > Risk types
- **Seeded:** 13 standard types

### 5. Risk tier (optional, RISKS ONLY)
- **Type:** Foreign key to RiskTiers table
- **Purpose:** Classify by governance level and scope
- **Selection:** Single dropdown
- **Managed in:** Admin > Settings > Risk tiers
- **Seeded:** 5 governance tiers (Project → Cross-government)

### 6. Category (optional, ALL ENTITIES)
- **Type:** Free-text string
- **Purpose:** Custom categorisation beyond standard fields
- **Selection:** Text input
- **Managed by:** Users directly

## Settings administration

### Settings dashboard

**Location:** Administration > Settings

**Purpose:** Central hub for managing lookup values

**Available lookups:**
1. **Risk types** - Risk category taxonomy
2. **Risk tiers** - Governance level classification

### Risk types management

**Table:** `RiskTypes` (13 seeded entries)

**Fields:** Code, Name, Description, Summary, IsActive

**Relationship:** Many-to-many with Risks via `RiskRiskTypes` junction table

**UI:** Checkbox list on risk forms (multi-select)

**Features:**
- Full CRUD operations
- Activation/deactivation toggle
- Deletion protection if in use
- Unique code validation

**Seeded types:**
- Strategy, Governance, Operations, Legal, Property
- Financial, Commercial, People, Technology, Information
- Security, Project/Programme, Reputational

### Risk tiers management

**Table:** `RiskTiers` (5 seeded entries)

**Fields:** Code, Name, Description, Summary, SortOrder, IsActive

**Relationship:** One-to-many with Risks (each risk has one tier)

**UI:** Dropdown on risk forms (single-select)

**Features:**
- Full CRUD operations
- Sort order management
- Activation/deactivation toggle
- Deletion protection if in use
- Unique code validation

**Seeded tiers:**
1. Project-level risk
2. Programme-level risk
3. Portfolio-level risk
4. Department-level risk
5. Cross-government / systemic risk

## Complete feature matrix

| Entity    | Objective | Product | Business Area | Risk Types | Risk Tier | Category |
| --------- | --------- | ------- | ------------- | ---------- | --------- | -------- |
| Risk      | ✓         | ✓       | ✓             | ✓ (multi)  | ✓         | ✓        |
| Issue     | ✓         | ✓       | ✓             | ✗          | ✗         | ✓        |
| Milestone | ✓         | ✓       | ✓             | ✗          | ✗         | ✗        |
| Action    | ✓         | ✓       | ✓             | ✗          | ✗         | ✗        |

## Example risk classification

**Risk:** "Cloud platform capacity issues affecting multiple services"

**Classification:**
- **Strategic Objective:** Digital Infrastructure Modernisation
- **Product:** FIPS-GOV-001 (Government Services Platform)
- **Business Area:** Infrastructure
- **Risk Types:** 
  - ✓ Technology risk (platform performance)
  - ✓ Operations risk (service delivery)
  - ✓ Financial risk (cost overruns for capacity)
- **Risk Tier:** Portfolio-level risk (affects multiple programmes)
- **Category:** Infrastructure

This multi-dimensional classification enables:
- Finding all infrastructure risks
- Reporting on technology risks across products
- Tracking portfolio-level governance
- Linking to strategic objectives
- Analyzing risks by business area

## Database schema summary

### New tables created

1. **RiskTypes** - Risk category taxonomy (13 entries)
2. **RiskTiers** - Governance tier levels (5 entries)
3. **RiskRiskTypes** - Junction for many-to-many (Risk ↔ RiskTypes)

### Modified tables

**Risks:**
- Added `BusinessArea` (TEXT/NVARCHAR(100))
- Added `RiskTierId` (INTEGER/INT, nullable, FK to RiskTiers)

**Issues:**
- Added `BusinessArea` (TEXT/NVARCHAR(100))

**Milestones:**
- Added `BusinessArea` (TEXT/NVARCHAR(100))

**Actions:**
- Added `BusinessArea` (TEXT/NVARCHAR(100))

## Migration history

| Order | Migration                          | Timestamp      | Purpose                                    |
| ----- | ---------------------------------- | -------------- | ------------------------------------------ |
| 1     | AddBusinessAreaToRAIDEntities      | 20251017200522 | Added BusinessArea to all RAID entities    |
| 2     | AddRiskTypeLookup                  | 20251017201112 | Created RiskTypes table, seeded 13 types   |
| 3     | AddRiskTypeIdToRisk                | 20251017201320 | Added RiskTypeId FK to Risks (later removed) |
| 4     | ChangeRiskTypeToManyToMany         | 20251017202224 | Changed to many-to-many, created junction  |
| 5     | AddRiskTier                        | 20251017202433 | Created RiskTiers table, seeded 5 tiers    |

All migrations applied successfully ✅

## Services integration

### ProductsApiService enhancements

**New method:**
```csharp
Task<List<string>> GetBusinessAreasAsync();
```

**Features:**
- Queries CMS for category_type = "Business area"
- Cached for 1 hour
- Ordered by sort_order
- Error handling

### IProductsApiService interface

Now includes methods for:
- Products
- Phases
- Business Areas

## Controller updates summary

### RiskController
- Fetches risk types, risk tiers, business areas
- Handles multi-select risk types (int[] parameter)
- Manages RiskRiskType junction entries
- Includes RiskTier and RiskRiskTypes in queries

### IssueController
- Fetches business areas
- No risk-specific fields

### MilestoneController
- Fetches business areas
- No risk-specific fields

### ActionController
- Fetches business areas
- No risk-specific fields

### AdminController
- Settings index action
- RiskTypes CRUD (7 actions)
- RiskTiers CRUD (7 actions)
- Deletion protection logic

## View structure

```
Views/
├── Admin/
│   └── Settings/
│       ├── Index.cshtml (Settings dashboard)
│       ├── RiskTypes.cshtml (List)
│       ├── CreateRiskType.cshtml
│       ├── EditRiskType.cshtml
│       ├── DeleteRiskType.cshtml
│       ├── RiskTiers.cshtml (List)
│       ├── CreateRiskTier.cshtml
│       ├── EditRiskTier.cshtml
│       └── DeleteRiskTier.cshtml
├── Risk/
│   ├── Index.cshtml (includes Product column)
│   ├── Create.cshtml (risk types checkboxes + tier dropdown)
│   ├── Edit.cshtml (risk types checkboxes + tier dropdown)
│   └── Details.cshtml (displays types list + tier + business area)
├── Issue/
│   ├── Create.cshtml (business area dropdown)
│   ├── Edit.cshtml (business area dropdown)
│   └── Details.cshtml (displays business area)
├── Milestone/
│   ├── Create.cshtml (business area dropdown)
│   ├── Edit.cshtml (business area dropdown)
│   └── Details.cshtml (displays business area)
└── Action/
    ├── Create.cshtml (business area dropdown)
    ├── Edit.cshtml (business area dropdown)
    └── Details.cshtml (displays business area)
```

## Navigation structure

```
ADMINISTRATION
├── Users
├── Performance metrics
├── Enterprise metrics
├── Functional standards
├── Strategic objectives
└── Settings
    ├── Settings dashboard
    ├── Risk types
    └── Risk tiers
```

## Reporting capabilities

### By risk type

Risks can be reported showing:
- All Technology risks (may include same risk in multiple categories)
- Risks with both Security AND Legal types
- Most common type combinations
- Types per product

### By risk tier

Risks can be reported showing:
- All Department-level risks
- Escalation trends (tier changes over time)
- Risk count per governance tier
- Average risk score by tier

### By business area

All RAID items can be reported by:
- Business area workload
- Cross-area dependencies
- Area-specific risk exposure

### Combined dimensions

Advanced reporting examples:
- "Show all Security risks at Department-level in Infrastructure area"
- "Show Technology AND Information risks for Product X"
- "Show all Programme-level risks linked to Strategic Objective Y"

## Data integrity and validation

**Unique constraints:**
- RiskType.Code (unique)
- RiskTier.Code (unique)

**Referential integrity:**
- Cannot delete Risk Type if RiskRiskTypes entries exist
- Cannot delete Risk Tier if Risks.RiskTierId references it
- Cascade delete RiskRiskTypes when Risk deleted
- Restrict delete RiskTier when Risks reference it

**Optional fields:**
- All classification fields are optional
- Risks can be created without any classification
- Supports incremental classification

## Performance optimizations

**Indexes created:**
- RiskTypes.Code (unique)
- RiskTypes.IsActive
- RiskTiers.Code (unique)
- RiskTiers.IsActive
- RiskTiers.SortOrder
- RiskRiskTypes.RiskTypeId
- Risks.RiskTierId
- Risks.FipsId
- Issues/Milestones/Actions.FipsId

**Caching:**
- Business Areas cached 1 hour
- Phases cached 1 hour
- SelectLists generated per request
- Eager loading for details views

## Documentation files

1. **BUSINESS_AREA_FEATURE.md** - Business area classification
2. **SETTINGS_AND_LOOKUPS.md** - Settings system overview
3. **MULTI_SELECT_RISK_TYPES.md** - Multi-select risk types
4. **RISK_TIER_FEATURE.md** - Risk tier classification
5. **RAID_CLASSIFICATION_COMPLETE.md** - This comprehensive summary

## Testing recommendations

Test scenarios:

1. **Create risk with all classifications**
   - Select objective, product, business area
   - Check multiple risk types
   - Select risk tier
   - Enter custom category

2. **Edit risk to change classifications**
   - Add/remove risk types
   - Change tier
   - Update business area

3. **Admin: Create new risk type**
   - Verify appears in checkboxes
   - Deactivate, verify hidden
   - Attempt delete with risks assigned

4. **Admin: Create new risk tier**
   - Verify appears in dropdown
   - Verify sort order respected
   - Attempt delete with risks assigned

5. **Business area from CMS**
   - Verify CMS groups load correctly
   - Test caching behavior
   - Verify updates propagate

## Success criteria met

✅ Business Area on all RAID entities (Risk, Issue, Milestone, Action)  
✅ Business Area fetched from CMS (category_type = "Business area")  
✅ Settings section created in Admin navigation  
✅ Risk Types lookup table with CRUD operations  
✅ 13 risk types seeded with definitions  
✅ Risk Types changed to multi-select checkboxes  
✅ Risk Tier lookup table with CRUD operations  
✅ 5 risk tiers seeded with governance details  
✅ Risk Tier added as dropdown on risk forms  
✅ All views updated with new fields  
✅ All migrations applied successfully  
✅ Comprehensive documentation created  
✅ Build succeeds with 0 errors  

## Complete classification example

**Example Risk Entry:**

```
Title: API authentication service degradation

Strategic Objective: Platform Reliability Programme
Product: FIPS-AUTH-001
Business Area: Security & Identity

Risk Types:
☑ Technology risk (service performance issues)
☑ Security risk (authentication vulnerabilities)
☑ Operations risk (service delivery impact)

Risk Tier: Programme-level risk

Category: Critical Infrastructure

Owner: Head of Platform Engineering
Impact: 4 (Major)
Likelihood: 3 (Possible)
Risk Score: 12 (High)
Status: Treating
```

This rich classification enables sophisticated reporting:
- Appears in Technology, Security, AND Operations risk reports
- Tracked at Programme governance level
- Linked to specific product for accountability
- Associated with Security & Identity business area
- Connected to Platform Reliability strategic objective
- Custom category for additional grouping

---

**Implementation date:** 17 October 2025  
**Total migrations:** 5  
**Lookup tables:** 2 (RiskTypes, RiskTiers)  
**Junction tables:** 1 (RiskRiskTypes)  
**Admin views:** 9 (Settings index + 4 per lookup type)  
**Updated controllers:** 5 (Risk, Issue, Milestone, Action, Admin)  
**Updated RAID views:** 24 (Create/Edit/Details × 4 entities)  
**Build status:** ✅ Successful  
**Version:** 1.0

