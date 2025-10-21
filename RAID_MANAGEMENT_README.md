# Product governance implementation

## Overview

This document describes the RAID (Risks, Actions, Issues, Decisions) management system that has been implemented in the Compass application. The system provides comprehensive tracking and management capabilities for objectives, risks, issues, milestones, and actions.

## Database schema

The implementation follows SQLite conventions with the following core entities:

### Core entities

#### Objectives
Strategic goals or initiatives that can contain risks, issues, milestones, and actions.

**Key fields:**
- Title, Description
- Owner (user reference)
- Start date, End date
- Status: proposed, active, paused, completed, cancelled
- RAG status: red, amber, green
- Success measures
- Progress percentage (0-100)
- Soft delete support

#### Risks
Potential events that could impact objectives.

**Key fields:**
- Title, Description, Category
- Owner (user reference)
- Impact rating (1-5)
- Likelihood rating (1-5)
- Risk score (calculated: impact × likelihood)
- Proximity date (when risk likely to materialise)
- Response: avoid, mitigate, transfer, accept
- Residual impact and likelihood (after treatment)
- Target date
- Status: open, treating, monitoring, closed
- Notes

#### Issues
Problems that have occurred and need resolution.

**Key fields:**
- Title, Description, Category
- Owner (user reference)
- Severity: low, medium, high, critical
- Priority: low, medium, high
- Detected date
- Target resolution date
- Status: open, in_progress, blocked, resolved, closed
- Resolution summary
- Workaround
- Blocked flag
- Closed date

#### Milestones
Key delivery points with defined completion criteria.

**Key fields:**
- Name, Description
- Owner (user reference)
- Baseline due date (original plan)
- Due date (current plan)
- Actual date (when completed)
- Status: not_started, on_track, at_risk, delayed, complete, cancelled
- Progress percentage (0-100)
- External reference
- Notes

#### Actions
Tasks to be completed, can be standalone or linked to risks, issues, or milestones.

**Key fields:**
- Title, Description
- Assigned to (user reference)
- Priority: low, medium, high
- Status: not_started, in_progress, blocked, done, cancelled
- Start date, Due date, Completed date
- Parent action (for sub-tasks)
- Evidence URL
- Notes

### Relationships

#### Junction tables
The system uses junction tables to support many-to-many relationships:

- **RiskAction**: Links risks to their mitigation actions
- **IssueAction**: Links issues to their resolution actions
- **MilestoneAction**: Links milestones to their delivery actions
- **MilestoneRisk**: Links risks to milestones they affect
- **MilestoneIssue**: Links issues to milestones they affect

#### Hierarchical relationships
- Objectives can contain: Risks, Issues, Milestones, Actions
- Actions can have: Sub-actions (parent-child relationship)

### Database indexes

The following indexes have been created for optimal query performance:

**Objectives:**
- Status
- RAG status
- Owner user ID

**Risks:**
- Objective ID
- Status
- Risk score (descending)
- Proximity date

**Issues:**
- Objective ID
- Status
- Severity + Priority (composite)
- Target resolution date

**Milestones:**
- Objective ID
- Status
- Due date

**Actions:**
- Objective ID
- Assigned to user ID
- Status + Priority (composite)
- Due date

**Junction tables:**
- All have composite primary keys on their foreign key pairs
- Individual indexes on action IDs for reverse lookups

## Application structure

### Models

Location: `/Models/`

- `Objective.cs` - Objective entity with navigation properties
- `Risk.cs` - Risk entity with score calculation
- `Issue.cs` - Issue entity with severity tracking
- `Milestone.cs` - Milestone entity with progress tracking
- `Action.cs` - Action entity with hierarchical support
- `RiskAction.cs` - Risk-Action junction
- `IssueAction.cs` - Issue-Action junction
- `MilestoneAction.cs` - Milestone-Action junction
- `MilestoneRisk.cs` - Milestone-Risk junction
- `MilestoneIssue.cs` - Milestone-Issue junction

### Database context

Location: `/Data/CompassDbContext.cs`

The `CompassDbContext` has been updated to include:
- DbSets for all RAID entities
- DbSets for all junction tables
- EF Core relationship configuration
- Index configuration
- Cascade delete policies

### Controllers

Location: `/Controllers/`

Each entity has a dedicated controller with full CRUD operations:

- `ObjectiveController.cs` - Manage objectives
- `RiskController.cs` - Manage risks with risk scoring
- `IssueController.cs` - Manage issues with severity tracking
- `MilestoneController.cs` - Manage milestones with progress tracking
- `ActionController.cs` - Manage actions with parent-child support

**Controller features:**
- Authorization required on all actions
- Soft delete implementation
- Related entity filtering (e.g., view risks for a specific objective)
- Audit timestamps (created_at, updated_at)
- Success/error messaging via TempData
- Comprehensive error logging

### Views

Location: `/Views/`

Each entity has a complete set of views:

#### Objective views
- `Index.cshtml` - List all objectives with RAG status and progress
- `Create.cshtml` - Create new objective
- `Edit.cshtml` - Edit existing objective
- `Delete.cshtml` - Confirm deletion
- `Details.cshtml` - View objective details with related items dashboard

#### Other entity views
- `Risk/Index.cshtml` - Risk register with risk score highlighting
- `Issue/Index.cshtml` - Issue log with severity and blocked flags
- `Milestone/Index.cshtml` - Milestone schedule with overdue highlighting
- `Action/Index.cshtml` - Action log with priority and status

**View features:**
- Responsive Bootstrap/AdminLTE design
- GOV.UK design system compliance
- Breadcrumb navigation
- Status badges with appropriate colour coding
- Progress bars for percentage tracking
- Contextual filtering (by objective, risk, issue, milestone)
- Quick action buttons
- Accessible markup with ARIA labels

### Navigation

The Product governance section has been added to the main sidebar navigation (`/Views/Shared/_Layout.cshtml`):

- Objectives
- Risks
- Issues
- Milestones
- Actions

Each menu item is marked as active when viewing the corresponding section.

## Migration

A database migration has been created and applied:

**Migration name:** `AddRAIDManagement`

**Migration file:** `/Migrations/[timestamp]_AddRAIDManagement.cs`

To apply the migration:
```bash
dotnet ef database update
```

To rollback:
```bash
dotnet ef database update [PreviousMigrationName]
```

## Usage examples

### Creating an objective with associated risks

1. Navigate to **Product Governance > Objectives**
2. Click **Add new objective**
3. Fill in objective details (title, description, owner, dates, etc.)
4. Click **Create objective**
5. From the objective details page, click **Add risk**
6. Fill in risk details including impact and likelihood ratings
7. The risk score is automatically calculated

### Linking actions to risks

1. Navigate to the risk details page
2. View associated actions in the related items section
3. Create new actions directly from the risk context
4. Actions can also be created separately and linked via junction tables

### Tracking milestone progress

1. Navigate to **Product Governance > Milestones**
2. Milestones show due dates, progress percentage, and status
3. Overdue milestones are highlighted in red
4. Update progress percentage as work progresses
5. Record actual completion date when milestone is achieved

### Managing issue resolution

1. Navigate to **Product Governance > Issues**
2. Issues are sorted by severity and target resolution date
3. Mark issues as **Blocked** to flag dependencies
4. Link resolution actions to track remediation work
5. Record resolution summary when closing issues

## Data conventions

### Status values

**Objective status:**
- `proposed` - Initial proposal stage
- `active` - Currently being worked on
- `paused` - Temporarily stopped
- `completed` - Successfully finished
- `cancelled` - No longer pursuing

**Risk status:**
- `open` - Identified but not yet being treated
- `treating` - Mitigation actions in progress
- `monitoring` - Being watched
- `closed` - No longer a threat

**Issue status:**
- `open` - Newly identified
- `in_progress` - Being worked on
- `blocked` - Cannot proceed
- `resolved` - Fixed but not verified
- `closed` - Verified and closed

**Milestone status:**
- `not_started` - Not yet begun
- `on_track` - Progressing as planned
- `at_risk` - May miss deadline
- `delayed` - Behind schedule
- `complete` - Delivered
- `cancelled` - No longer required

**Action status:**
- `not_started` - Not yet begun
- `in_progress` - Being worked on
- `blocked` - Cannot proceed
- `done` - Completed
- `cancelled` - No longer required

### RAG ratings

- `red` - Significant issues or high risk
- `amber` - Some concerns or medium risk
- `green` - On track or low risk

### Priority levels

- `high` - Urgent, needs immediate attention
- `medium` - Important, normal priority
- `low` - Can be delayed if needed

### Severity levels (Issues)

- `critical` - System down or major impact
- `high` - Significant impact
- `medium` - Moderate impact
- `low` - Minor impact

### Risk ratings

Both impact and likelihood use a 1-5 scale:
- 1 = Very low
- 2 = Low
- 3 = Medium
- 4 = High
- 5 = Very high

**Risk score** = Impact × Likelihood (1-25)
- 15-25: High risk (red)
- 10-14: Medium risk (amber)
- 1-9: Low risk (green)

## Soft delete

All entities support soft delete:
- Records are marked as deleted (`is_deleted = true`)
- Deleted records are excluded from normal queries
- Records can be permanently deleted or restored if needed
- Updated timestamp is set when soft deleting

## Audit trail

All entities include audit timestamps:
- `created_at` - UTC timestamp when record was created
- `updated_at` - UTC timestamp when record was last modified

These are automatically set by the controllers.

## Security

- All controllers require authentication via `[Authorize]` attribute
- CSRF protection via `[ValidateAntiForgeryToken]` on POST actions
- SQL injection protection via EF Core parameterisation
- XSS protection via Razor automatic encoding

## Future enhancements

Potential improvements for future iterations:

1. **Reporting and dashboards**
   - Risk heat maps
   - Milestone timeline visualisations
   - Action burndown charts
   - Issue aging reports

2. **Enhanced features**
   - Email notifications for overdue items
   - Bulk operations (update multiple items)
   - Import/export functionality
   - Document attachments
   - Comments/activity log
   - Workflow approvals

3. **Integration**
   - Link to external project management tools
   - API endpoints for programmatic access
   - Calendar integration for milestones

4. **Analytics**
   - Trend analysis
   - Predictive risk scoring
   - Performance metrics
   - SLA tracking

## Support

For questions or issues with the Product governance system, please contact the Compass development team.

---

**Created:** 17 October 2025  
**Author:** AI Assistant  
**Version:** 1.0

