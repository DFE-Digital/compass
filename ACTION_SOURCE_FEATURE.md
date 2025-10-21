# Action source feature

## Overview

Action sources track where actions originated from, enabling better traceability, reporting, and understanding of action drivers across the organisation. This is particularly important for actions created by third-party assessors or external review teams.

## Purpose

Action source classification supports:
- **Traceability** - Track origin of each action
- **Reporting** - Analyze actions by source type
- **External actions** - Identify third-party generated actions
- **Context** - Understand why action was created
- **Integration** - Support for external assessment services

## Database structure

### ActionSource table

| Column      | Type                   | Description                                          |
| ----------- | ---------------------- | ---------------------------------------------------- |
| Id          | INTEGER / INT IDENTITY | Primary key                                          |
| Code        | TEXT / NVARCHAR(50)    | Unique identifier (e.g., `RISK`, `SERVICE_ASSESSMENT`) |
| Name        | TEXT / NVARCHAR(100)   | Display name (e.g., "Risk", "Service Assessment")    |
| Description | TEXT / NVARCHAR(MAX)   | Full definition with context and examples            |
| Summary     | TEXT / NVARCHAR(200)   | Concise one-line description                         |
| SortOrder   | INTEGER / INT          | Display order in dropdowns                           |
| IsActive    | INTEGER / BIT          | Whether source appears in dropdowns                  |
| CreatedAt   | DATETIME / DATETIME2   | UTC timestamp when created                           |
| UpdatedAt   | DATETIME / DATETIME2   | UTC timestamp when last updated                      |

**Indexes:**
- Unique index on `Code`
- Index on `IsActive`
- Index on `SortOrder`

### Action table integration

The `Actions` table includes:
```sql
ActionSourceId INTEGER / INT NULL
CONSTRAINT FK_Actions_ActionSources FOREIGN KEY (ActionSourceId) REFERENCES ActionSources(Id)
```

Action source is optional - actions can exist without a source assignment.

## Seeded action sources

The following 4 action sources are pre-populated:

| Order | Code               | Name                | Summary                          |
| ----- | ------------------ | ------------------- | -------------------------------- |
| 1     | RISK               | Risk                | Action arising from a risk       |
| 2     | ISSUE              | Issue               | Action arising from an issue     |
| 3     | MILESTONE          | Milestone           | Action arising from a milestone  |
| 4     | SERVICE_ASSESSMENT | Service Assessment  | Action from service assessment   |

### Detailed source descriptions

#### 1. Risk (SortOrder: 1)
**Code:** `RISK`

**Description:** Action created to mitigate, treat, or manage a risk. These actions are typically focused on reducing impact or likelihood of risk materialisation.

**Summary:** Action arising from a risk

**Examples:**
- Implement additional security controls
- Update disaster recovery procedures
- Conduct risk assessment workshop
- Transfer risk via insurance

**Typical context:** Risk treatment and mitigation activities

---

#### 2. Issue (SortOrder: 2)
**Code:** `ISSUE`

**Description:** Action created to resolve, workaround, or close an issue. These actions address problems that have already materialised and require remediation.

**Summary:** Action arising from an issue

**Examples:**
- Fix critical bug in production
- Restore service availability
- Implement temporary workaround
- Root cause analysis investigation

**Typical context:** Issue resolution and remediation

---

#### 3. Milestone (SortOrder: 3)
**Code:** `MILESTONE`

**Description:** Action created to deliver a milestone or track progress towards milestone completion. These actions are typically project or programme delivery activities.

**Summary:** Action arising from a milestone

**Examples:**
- Complete user research phase
- Deploy to production environment
- Obtain stakeholder sign-off
- Deliver training materials

**Typical context:** Project/programme delivery tasks

---

#### 4. Service Assessment (SortOrder: 4)
**Code:** `SERVICE_ASSESSMENT`

**Description:** Action created as a result of a service assessment review. These actions may be created by external assessment teams or third-party reviewers and typically address recommendations or compliance requirements.

**Summary:** Action from service assessment

**Examples:**
- Address accessibility findings
- Improve performance metrics
- Implement security recommendations
- Update documentation per review

**Typical context:** Service standard compliance, external reviews

**Special note:** These actions may be created via API by third-party services or imported from assessment platforms.

## Admin functionality

### Navigate to Settings

**Path:** Administration > Settings > Action sources

### View action sources

The list page displays:
- Sort order
- Code (unique identifier)
- Name (display name)
- Summary (concise description)
- Status badge (Active/Inactive)
- Edit and Delete actions

Sorted by `SortOrder` then `Name`.

### Create action source

1. Click **Create new** button
2. Complete form:
   - **Code**: Unique uppercase identifier (underscores allowed)
   - **Name**: Display name
   - **Summary**: One-line description
   - **Description**: Full definition with examples
   - **Sort order**: Numeric display position
   - **Active**: Toggle visibility
3. Click **Create**

**Validation:**
- Code must be unique
- Code and Name are required

### Edit action source

1. Click **Edit** on source row
2. Modify fields as needed
3. Update sort order if reordering needed
4. Click **Update**

### Delete action source

1. Click **Delete** on source row
2. Review details and action count
3. Confirm deletion if no actions assigned

**Protection:** Cannot delete source if any actions reference it.

## Using action sources

### Assigning source to action

**Action Create/Edit form:**
- Single-select dropdown
- Shows only active sources
- Ordered by sort order
- Optional selection
- Includes help text: "Where did this action originate from?"

**Form position:**
- Below parent action selection
- Above business area field
- In same row as business area (responsive layout)

### Viewing action source

**Action Details page:**
- Displays source name in bold
- Shows summary description below
- Indicates origin clearly
- "-" if no source assigned

## Use cases

### Example 1: Risk mitigation action
```
Title: Implement multi-factor authentication
Source: Risk
Description: Mitigate security risk RISK-001
```

### Example 2: Issue resolution action
```
Title: Fix user login timeout issue
Source: Issue
Description: Resolve ISSUE-045 reported by users
```

### Example 3: Milestone delivery action
```
Title: Complete beta user testing
Source: Milestone
Description: Deliver Milestone M-12 user testing phase
```

### Example 4: External assessment action
```
Title: Improve page load performance to meet standards
Source: Service Assessment
Description: Action from GDS service assessment - Performance requirement
Created by: External assessment team
```

## Relationship to junction tables

**Note:** Action sources are separate from the junction tables:
- **RiskActions junction** - Links specific actions to specific risks
- **IssueActions junction** - Links specific actions to specific issues
- **MilestoneActions junction** - Links specific actions to specific milestones

**Action Source** indicates the general category/origin, while junction tables provide explicit relationships.

**Example:**
An action might have:
- **Action Source:** "Risk" (general category)
- **RiskActions entries:** Links to RISK-001 and RISK-003 (specific relationships)

## Reporting capabilities

### Actions by source

Reports can show:
- Count of actions per source type
- Completion rates by source
- Overdue actions by source
- Source distribution across products/areas

### Integration tracking

Service Assessment source enables:
- Identify externally generated actions
- Track compliance action progress
- Report on assessment follow-up
- Distinguish internal vs external actions

### Trend analysis

Analyze:
- Which sources generate most actions
- Completion rates vary by source
- Source changes over time
- Effort distribution across sources

## Third-party integration

### Service Assessment actions

Actions with `SERVICE_ASSESSMENT` source can be:
- Created via API by assessment platforms
- Imported from assessment reports
- Tracked separately in compliance views
- Linked to assessment outcomes

**Future API considerations:**
```csharp
POST /api/actions
{
    "title": "Improve accessibility contrast ratios",
    "actionSourceCode": "SERVICE_ASSESSMENT",
    "externalRef": "ASSESS-2025-001",
    "assignedToEmail": "user@education.gov.uk",
    ...
}
```

## Admin management

### Activation/deactivation

**When to deactivate:**
- Source no longer in use
- Assessment programme ended
- Temporary suspension

**Effect:**
- Hidden from dropdowns
- Still visible on existing actions
- Can be reactivated

### Deletion protection

Cannot delete if:
- Any actions assigned to it
- Must reassign actions first
- Shows count of dependent actions

## Best practices

### Selecting source

**Guidelines:**
- Select source that best represents origin
- Use SERVICE_ASSESSMENT for external reviews
- Use RISK/ISSUE/MILESTONE when explicitly linked
- Leave blank if origin unclear or mixed

### When to add new sources

Consider adding new source types for:
- Audit findings
- User feedback themes
- Architectural decision records
- Security reviews
- Compliance requirements
- Innovation initiatives

## Migration history

1. **AddActionSource** (20251017203342)
   - Created `ActionSources` table
   - Seeded 4 source types
   - Added `ActionSourceId` to Actions
   - Created indexes

2. **UpdateActionSourceRelationship** (20251017203451)
   - Updated foreign key configuration
   - Set RESTRICT delete behavior

## Complete action classification

Each Action can now be classified by:
1. **Strategic Objective** - Link to organisational goals (optional)
2. **Product (FipsId)** - Link to FIPS products (optional)
3. **Business Area** - Organisational unit from CMS (optional)
4. **Action Source** - Origin/driver of action (optional)
5. **Parent Action** - Sub-task relationship (optional)

**Plus explicit junction relationships:**
- Linked Risks (via RiskActions)
- Linked Issues (via IssueActions)
- Linked Milestones (via MilestoneActions)

This provides comprehensive traceability and context for every action.

## Settings dashboard updated

The Settings index page now includes three lookup types:
1. **Risk types** - Risk category taxonomy (13 types)
2. **Risk tiers** - Governance level classification (5 tiers)
3. **Action sources** - Action origin tracking (4 sources)

All follow consistent patterns:
- Full CRUD operations
- Active/inactive toggle
- Sort order management
- Deletion protection
- Unique code validation

---

**Created:** 17 October 2025  
**Migrations:** AddActionSource (20251017203342), UpdateActionSourceRelationship (20251017203451)  
**Sources seeded:** 4 (Risk, Issue, Milestone, Service Assessment)  
**Relationship:** One-to-many (each action has one source)  
**Third-party support:** Yes (SERVICE_ASSESSMENT for external actions)  
**Version:** 1.0

