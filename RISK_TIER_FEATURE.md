# Risk tier feature

## Overview

Risk tiers classify risks by their scope and governance level, from tactical project risks through to strategic departmental and cross-government systemic risks. This enables appropriate escalation, ownership, and reporting at each governance tier.

## Purpose

Risk tier classification supports:
- **Appropriate governance** - Right risks reported at right level
- **Escalation management** - Clear path for escalating risks upward
- **Resource allocation** - Focus effort where most impactful
- **Reporting structure** - Align with organisational hierarchy
- **Ownership clarity** - Define who manages risks at each level

## Database structure

### RiskTier table

| Column      | Type                   | Description                                            |
| ----------- | ---------------------- | ------------------------------------------------------ |
| Id          | INTEGER / INT IDENTITY | Primary key                                            |
| Code        | TEXT / NVARCHAR(50)    | Unique identifier (e.g., `PROJECT`, `DEPARTMENT`)      |
| Name        | TEXT / NVARCHAR(100)   | Display name (e.g., "Project-level risk")              |
| Description | TEXT / NVARCHAR(MAX)   | Full definition with governance details and examples   |
| Summary     | TEXT / NVARCHAR(200)   | Concise scope description                              |
| SortOrder   | INTEGER / INT          | Display order (ascending: project→cross-government)    |
| IsActive    | INTEGER / BIT          | Whether tier appears in dropdowns (1=active, 0=inactive) |
| CreatedAt   | DATETIME / DATETIME2   | UTC timestamp when record created                      |
| UpdatedAt   | DATETIME / DATETIME2   | UTC timestamp when last updated                        |

**Indexes:**
- Unique index on `Code`
- Index on `IsActive` for filtering
- Index on `SortOrder` for ordering

### Risk table integration

The `Risks` table includes:
```sql
RiskTierId INTEGER / INT NULL
CONSTRAINT FK_Risks_RiskTiers FOREIGN KEY (RiskTierId) REFERENCES RiskTiers(Id)
```

Risk tier is optional - risks can exist without a tier assignment.

## Seeded risk tiers

The following 5 risk tiers are pre-populated (in hierarchical order):

| Order | Code             | Name                             | Summary                                  |
| ----- | ---------------- | -------------------------------- | ---------------------------------------- |
| 1     | PROJECT          | Project-level risk               | Within a discrete project or service     |
| 2     | PROGRAMME        | Programme-level risk             | Across a set of related projects         |
| 3     | PORTFOLIO        | Portfolio-level risk             | Across multiple programmes or business area |
| 4     | DEPARTMENT       | Department-level risk            | Across the entire department (e.g., DfE) |
| 5     | CROSS_GOVERNMENT | Cross-government / systemic risk | Across multiple departments or sectors   |

### Detailed tier descriptions

#### 1. Project-level risk (SortOrder: 1)
**Scope:** Within a discrete project or service

**Description:** Tactical risks that directly affect the delivery of a single project, product, or service. Managed day-to-day by delivery teams.

**Examples:**
- Supplier delays
- Resource gaps
- Accessibility issues
- Technology bugs
- Dependency on another service

**Typical owners:** Project managers, product owners, delivery leads

---

#### 2. Programme-level risk (SortOrder: 2)
**Scope:** Across a set of related projects

**Description:** Strategic and operational risks that threaten delivery across multiple projects in a programme. Managed by programme boards and senior responsible owners (SROs).

**Examples:**
- Conflicting priorities across projects
- Funding shortfalls
- Interdependencies
- Benefits not realised

**Typical owners:** Programme SROs, programme boards

---

#### 3. Portfolio-level risk (SortOrder: 3)
**Scope:** Across multiple programmes or a business area

**Description:** Risks affecting the achievement of strategic objectives within a portfolio (e.g., Digital Portfolio, Transformation Portfolio).

**Examples:**
- Competing resource demand
- Cross-programme technology dependencies
- Change fatigue

**Typical owners:** Portfolio directors, portfolio boards

---

#### 4. Department-level risk (SortOrder: 4)
**Scope:** Across the entire department (e.g., DfE)

**Description:** High-level or strategic risks that could impact departmental objectives or public reputation. Usually captured in the Departmental Risk Register.

**Examples:**
- Failure to deliver strategic outcomes
- Budgetary control
- Workforce capability
- Cyber security
- Major policy change

**Typical owners:** Executive board, departmental risk committee

---

#### 5. Cross-government / systemic risk (SortOrder: 5)
**Scope:** Across multiple departments or sectors

**Description:** Risks that transcend departmental boundaries and could impact public sector capability, continuity, or reputation. Managed by Cabinet Office / HMT via Government Risk Register.

**Examples:**
- Major data breach across departments
- Shared platform failure (e.g., Notify, GOV.UK)
- Policy shifts
- Economic shocks

**Typical owners:** Cabinet Office, HM Treasury, cross-government boards

## Admin functionality

### Navigate to Settings

**Path:** Administration > Settings > Risk tiers

### View risk tiers

The list page shows:
- Sort order (hierarchical sequence)
- Code (unique identifier)
- Name (display name)
- Summary (scope description)
- Status badge (Active/Inactive)
- Edit and Delete actions

Sorted by `SortOrder` then `Name`.

### Create risk tier

1. Click **Create new** button
2. Complete form:
   - **Code**: Unique uppercase identifier (underscores allowed)
   - **Name**: Display name
   - **Summary**: One-line scope description
   - **Description**: Full definition with governance details and examples
   - **Sort order**: Numeric position in hierarchy
   - **Active**: Toggle visibility
3. Click **Create**

### Edit risk tier

1. Click **Edit** on tier row
2. Modify fields as needed
3. Update sort order to reposition in hierarchy
4. Click **Update**

### Delete risk tier

1. Click **Delete** on tier row
2. Review tier details and risk count
3. Confirm deletion if no risks assigned

**Protection:** Cannot delete tier if risks are using it.

## Using risk tiers

### Assigning tier to risk

**Risk Create/Edit form:**
- Single-select dropdown
- Shows only active tiers
- Ordered by sort order (Project → Cross-government)
- Optional selection
- Includes summary description below dropdown

**Form layout:**
```
Risk types (checkboxes - multiple selection)
  ☐ Strategy risk
  ☐ Technology risk
  ...

Risk tier (dropdown - single selection)
  [  Select risk tier (optional)  ▼]
```

### Viewing risk tier

**Risk Details page:**
- Displays tier name in bold
- Shows summary description
- Indicates scope clearly
- "-" if no tier assigned

### Use cases

#### Escalation workflow
1. Risk starts as **Project-level**
2. Impact/likelihood increases
3. Re-assigned to **Programme-level**
4. If severe, escalated to **Department-level**
5. Tracked at appropriate governance tier

#### Cross-cutting risk
- **Technology platform failure** might be Department-level
- **Shared service outage** might be Cross-government
- **Individual bug** remains Project-level

## Governance implications

### Risk tier determines:

**Project-level:**
- Managed in daily standups/team meetings
- Reported in project status reports
- Owner: Delivery manager

**Programme-level:**
- Managed in programme boards
- Reported to SRO
- Owner: Programme manager

**Portfolio-level:**
- Managed in portfolio boards
- Reported to portfolio director
- Owner: Portfolio leads

**Department-level:**
- Managed in departmental risk committee
- Reported to executive board
- Owner: Deputy/Director General

**Cross-government:**
- Managed via Cabinet Office processes
- Reported to Government Risk Register
- Owner: Permanent Secretary / Cabinet Office

## Reporting by tier

### Hierarchical views

Reports can be structured:
```
Department-level risks (4)
  ├─ Portfolio-level risks (12)
  │   ├─ Programme-level risks (28)
  │   │   └─ Project-level risks (67)
```

### Tier-specific metrics

Dashboard could show:
- Count of risks at each tier
- Average risk score by tier
- Overdue actions per tier
- Escalation trends (movement between tiers)

### Aggregation

Risk reporting enables:
- **Bottom-up:** Project risks rolled up to programme view
- **Top-down:** Departmental risks decomposed to portfolios
- **Cross-cutting:** Risks spanning multiple tiers
- **Escalation tracking:** Risks moving up governance tiers

## Integration with Risk model

### Risk classification fields summary

Each risk can now be classified by:
1. **Strategic Objective** - Optional link to objectives (Admin section)
2. **Product (FipsId)** - Optional link to FIPS products
3. **Business Area** - Optional CMS Business area category
4. **Risk Types** - Multiple checkboxes (Strategy, Technology, etc.)
5. **Risk Tier** - Single dropdown (Project/Programme/Portfolio/Department/Cross-gov)
6. **Category** - Free-text field for additional categorization

This provides rich multi-dimensional classification for reporting and analysis.

## Best practices

### Selecting appropriate tier

**Ask:**
- What governance body should oversee this risk?
- What level of authority needed to mitigate?
- Who has budget/resource to address this?
- What's the scope of impact?

**Guidelines:**
- Start at lowest appropriate tier
- Escalate upward if impact grows
- Don't over-escalate tactical risks
- Don't under-report strategic risks

### Managing tier changes

**Escalation triggers:**
- Risk score exceeds threshold
- Impact scope widens
- Mitigations require higher authority
- Benefits from senior visibility

**De-escalation criteria:**
- Risk successfully mitigated
- Scope reduced to lower tier
- Appropriate governance established below

### Sort order conventions

Maintain logical hierarchy:
- 1-10: Project tier
- 11-20: Programme tier
- 21-30: Portfolio tier
- 31-40: Department tier
- 41-50: Cross-government tier

This allows inserting new tiers within each level if needed.

## Admin management

### Activating/deactivating tiers

**When to deactivate:**
- Organisational restructure changes tiers
- Tier no longer used
- Temporary suspension of tier

**Effect:**
- Removed from dropdowns
- Still visible on existing risks
- Can be reactivated later

### Deletion protection

Cannot delete tier if:
- Any risks assigned to it
- Must reassign those risks first
- Shows count of dependent risks

### Editing tiers

**Can safely edit:**
- Name and descriptions
- Summary text
- Sort order
- Active status

**Be cautious editing:**
- Code (if used in integrations)
- Sort order (affects hierarchy)

## Migration history

1. **AddRiskTier** (20251017202433)
   - Created `RiskTiers` table
   - Seeded 5 governance tiers
   - Added `RiskTierId` to Risks
   - Created indexes

## Future enhancements

Potential additions:

1. **Escalation tracking**
   - Audit trail of tier changes
   - Escalation reason field
   - Escalation date tracking

2. **Tier-specific fields**
   - Different required fields per tier
   - Tier-specific workflows
   - Approval processes per tier

3. **Automated escalation**
   - Rules-based tier assignment
   - Auto-suggest tier based on score
   - Notification on escalation

4. **Reporting dashboards**
   - Risks by tier visualization
   - Escalation trends over time
   - Tier migration patterns

5. **Parent risk linking**
   - Link project risk to programme risk
   - Track risk decomposition
   - Hierarchical risk trees

## Accessibility

Risk tier dropdown:
- GOV.UK design system compliant
- Proper label associations
- Keyboard navigable
- Help text explains purpose
- Optional (not required)

---

**Created:** 17 October 2025  
**Migration:** 20251017202433_AddRiskTier  
**Tiers seeded:** 5 (Project, Programme, Portfolio, Department, Cross-government)  
**Relationship:** One-to-many (each risk has one tier)  
**Version:** 1.0

