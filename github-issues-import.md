
# COMPASS GitHub Issues Import File

This file contains all features and tasks for COMPASS, organized by Epic and Feature. Use this file with GitHub Copilot to create issues and sub-issues.

---

## EPIC: Dashboard and Home

### Feature: Personalised Dashboard Overview

**Description:** Create a personalised dashboard that displays summary cards showing counts of products, issues, risks, actions, and milestones with visual indicators for overdue and urgent items.

**User Story:** As a COMPASS user, I want to see a personalised dashboard when I log in, so that I can quickly understand what items require my attention.

**Tasks:**
- [ ] Create dashboard controller action and view
- [ ] Implement summary cards component showing counts for:
  - [ ] Products (where user is contact)
  - [ ] Issues (owned by user)
  - [ ] Risks (owned by user)
  - [ ] Actions (assigned to user)
  - [ ] Milestones (owned by user)
- [ ] Display open/total counts with proportions
- [ ] Highlight overdue items in red
- [ ] Highlight urgent items (due within 7 days) in amber
- [ ] Make cards clickable and navigate to relevant views
- [ ] Display personalised greeting with user's name
- [ ] Add loading states and error handling
- [ ] Write unit tests for dashboard queries
- [ ] Write integration tests for dashboard view

**Acceptance Criteria:**
- Dashboard displays summary cards showing counts of products, issues, risks, actions, and milestones
- Summary cards show open/total counts with proportions
- Overdue items are highlighted in red
- Urgent items (due within 7 days) are highlighted in amber
- Cards are clickable and navigate to relevant views
- Dashboard shows personalised greeting with user's name

---

### Feature: Tabbed Dashboard Navigation

**Description:** Implement horizontal tabbed navigation on the dashboard to switch between different views (Products, Issues, Risks, Actions, Milestones).

**User Story:** As a COMPASS user, I want to switch between different views using tabs, so that I can focus on specific item types.

**Tasks:**
- [ ] Design and implement horizontal tab component
- [ ] Create tab structure for:
  - [ ] Overview tab
  - [ ] Products tab
  - [ ] Issues tab
  - [ ] Risks tab
  - [ ] Actions tab
  - [ ] Milestones tab
- [ ] Implement badge counts on each tab
- [ ] Highlight overdue items in badge counts (red)
- [ ] Implement tab switching without page reload (AJAX/SPA)
- [ ] Keep filters visible and accessible when switching tabs
- [ ] Visually distinguish active tab
- [ ] Add keyboard navigation support
- [ ] Write accessibility tests (ARIA labels, keyboard navigation)
- [ ] Write unit tests for tab switching logic

**Acceptance Criteria:**
- Horizontal tabs are displayed at the top of the dashboard
- Each tab shows a badge count with overdue items highlighted in red
- Clicking a tab switches the view without page reload
- Filters remain visible and accessible when switching tabs
- Active tab is visually distinguished

---

### Feature: Products View on Dashboard

**Description:** Display products where the user is a contact with quick action buttons to view related RAID items.

**User Story:** As a product contact, I want to see all products where I'm a contact on my dashboard, so that I can quickly access products I'm responsible for.

**Tasks:**
- [ ] Query products where user is a contact (from FIPS CMS)
- [ ] Display product list with:
  - [ ] FIPS ID
  - [ ] Product title
  - [ ] Phase
  - [ ] User's role
- [ ] Add quick action buttons for each product:
  - [ ] View risks
  - [ ] View issues
  - [ ] View actions
  - [ ] View milestones
- [ ] Add link to full product reporting area
- [ ] Add link to add new products
- [ ] Implement empty state when no products
- [ ] Add loading and error states
- [ ] Write tests for product query logic

**Acceptance Criteria:**
- Products view shows FIPS ID, title, phase, and user's role
- Quick action buttons to view related risks, issues, actions, and milestones for each product
- Link to full product reporting area
- Link to add new products
- Products list is unfiltered (shows all products where user is a contact)

---

### Feature: Issues View on Dashboard

**Description:** Display all issues owned by the user with filtering options by status, severity, and time period.

**User Story:** As a COMPASS user, I want to see all issues I own on my dashboard with filtering options, so that I can prioritise and manage my issues effectively.

**Tasks:**
- [ ] Query issues owned by user (by email)
- [ ] Implement filtering by:
  - [ ] Status (dropdown)
  - [ ] Severity (dropdown)
  - [ ] Time period (All time, Overdue, Due today, Due this week, Due this month, Due next month)
- [ ] Display issue list with:
  - [ ] Severity badge
  - [ ] Status badge
  - [ ] Target resolution date
  - [ ] Blocking status indicator
- [ ] Highlight overdue issues in red
- [ ] Add links to edit each issue
- [ ] Add link to create new issue
- [ ] Implement auto-submit filters
- [ ] Add clear filters button
- [ ] Implement empty state message
- [ ] Write tests for issue filtering logic

**Acceptance Criteria:**
- Issues view shows all issues owned by the user
- Filterable by status, severity, and time period
- Displays severity, status, and target resolution date
- Overdue issues highlighted in red
- Shows blocking status
- Links to edit each issue
- Link to add new issue

---

### Feature: Risks View on Dashboard

**Description:** Display all risks owned by the user with filtering by status, risk score, and time period.

**User Story:** As a risk owner, I want to see all risks I own on my dashboard with filtering, so that I can monitor and manage risks effectively.

**Tasks:**
- [ ] Query risks where user is the owner (by email)
- [ ] Implement filtering by:
  - [ ] Status (dropdown)
  - [ ] Risk score range (min/max sliders or inputs)
  - [ ] Time period (All time, Overdue, Due today, Due this week, Due this month, Due next month)
- [ ] Display risk list with:
  - [ ] Risk score (colour-coded)
  - [ ] Impact rating
  - [ ] Likelihood rating
  - [ ] Status badge
- [ ] Colour-code by risk severity:
  - [ ] Red for critical (score 20-25)
  - [ ] Amber for high (score 15-19)
  - [ ] Green for medium/low (score <15)
- [ ] Highlight overdue risks
- [ ] Add links to edit each risk
- [ ] Add link to create new risk
- [ ] Implement auto-submit filters
- [ ] Add clear filters button
- [ ] Write tests for risk filtering and colour-coding

**Acceptance Criteria:**
- Risks view shows all risks where user is the owner (by email)
- Filterable by status, risk score, and time period
- Shows risk score, impact, likelihood, and status
- Colour-coded by risk severity (red for critical, amber for high)
- Highlights overdue risks
- Links to edit each risk
- Link to add new risk

---

### Feature: Actions View on Dashboard

**Description:** Display all actions assigned to the user with filtering by status, priority, and time period.

**User Story:** As a COMPASS user, I want to see all actions assigned to me on my dashboard, so that I can track my tasks and prioritise my work.

**Tasks:**
- [ ] Query actions assigned to user
- [ ] Implement filtering by:
  - [ ] Status (dropdown)
  - [ ] Priority (dropdown)
  - [ ] Time period (All time, Overdue, Due today, Due this week, Due this month, Due next month)
- [ ] Display action list with:
  - [ ] Priority badge
  - [ ] Status badge
  - [ ] Due date
- [ ] Highlight overdue actions in red
- [ ] Highlight urgent actions (due within 7 days) in amber
- [ ] Add links to edit each action
- [ ] Add link to create new action
- [ ] Implement auto-submit filters
- [ ] Add clear filters button
- [ ] Write tests for action filtering logic

**Acceptance Criteria:**
- Actions view shows all actions assigned to the user
- Filterable by status, priority, and time period
- Displays priority, status, and due date
- Overdue actions highlighted in red
- Urgent actions (due within 7 days) highlighted in amber
- Links to edit each action
- Link to add new action

---

### Feature: Milestones View on Dashboard

**Description:** Display all milestones owned by the user with progress tracking and filtering.

**User Story:** As a milestone owner, I want to see all milestones I own on my dashboard with progress tracking, so that I can monitor milestone completion.

**Tasks:**
- [ ] Query milestones owned by user
- [ ] Implement filtering by:
  - [ ] Status (dropdown)
  - [ ] Time period (All time, Overdue, Due today, Due this week, Due this month, Due next month)
- [ ] Display milestone list with:
  - [ ] Status badge
  - [ ] Progress percentage
  - [ ] Visual progress bar
  - [ ] Due date
- [ ] Highlight overdue milestones
- [ ] Highlight urgent milestones (due within 7 days)
- [ ] Add links to edit each milestone
- [ ] Add link to create new milestone
- [ ] Implement auto-submit filters
- [ ] Add clear filters button
- [ ] Write tests for milestone progress calculation

**Acceptance Criteria:**
- Milestones view shows all milestones owned by the user
- Filterable by status and time period
- Shows status, progress percentage, and due date
- Visual progress bars for each milestone
- Highlights overdue and urgent milestones
- Links to edit each milestone
- Link to add new milestone

---

## EPIC: Product Governance (RAID) - Risks

### Feature: Create and Manage Risks

**Description:** Comprehensive risk management with multi-select risk types, risk tiers, impact/likelihood assessment, and risk scoring.

**User Story:** As a product team member, I want to create a new risk with comprehensive details, so that potential threats are identified and tracked.

**Tasks:**
- [ ] Create risk model and database migration
- [ ] Create risk controller with CRUD actions
- [ ] Build risk create form with fields:
  - [ ] Title (required)
  - [ ] Description (required)
  - [ ] Product (multi-select from FIPS CMS)
  - [ ] Risk types (multi-select checkboxes - 13 types)
  - [ ] Risk tier (single-select dropdown - 5 tiers)
  - [ ] Business area (dropdown from FIPS CMS)
  - [ ] Impact (1-5 scale with info modal)
  - [ ] Likelihood (1-5 scale with info modal)
  - [ ] Risk owner (staff autocomplete)
  - [ ] Proximity date (date picker)
  - [ ] Response strategy (dropdown: avoid, mitigate, transfer, accept)
  - [ ] Status (dropdown)
- [ ] Implement automatic risk score calculation (Impact × Likelihood)
- [ ] Display calculated risk score prominently
- [ ] Add info modals for impact/likelihood guidance
- [ ] Implement staff autocomplete for risk owner
- [ ] Add form validation
- [ ] Create risk detail view
- [ ] Create risk edit view
- [ ] Create risk index view with filtering
- [ ] Implement soft delete
- [ ] Add audit trail logging
- [ ] Write unit tests for risk score calculation
- [ ] Write integration tests for risk CRUD operations

**Acceptance Criteria:**
- Form includes all required fields: title, description, product, risk types, risk tier, impact, likelihood
- Multi-select checkboxes for risk types (13 standard categories)
- Risk score automatically calculated as Impact × Likelihood
- Staff autocomplete for risk owner assignment
- Business area classification from FIPS CMS
- Validation prevents submission with missing required fields
- Info modals provide guidance for impact/likelihood scales

---

### Feature: Multi-Select Risk Types

**Description:** Allow risks to be classified with multiple risk types simultaneously using checkboxes.

**User Story:** As a risk manager, I want to assign multiple risk types to a single risk, so that I can accurately classify risks that span multiple categories.

**Tasks:**
- [ ] Create RiskRiskType junction table
- [ ] Update risk model to include many-to-many relationship with RiskType
- [ ] Update risk form to use checkboxes instead of radio buttons
- [ ] Load all 13 risk types from lookup table
- [ ] Handle multiple selections on form submit
- [ ] Save junction table entries on risk create/update
- [ ] Display all selected risk types on risk detail view
- [ ] Update risk index to filter by risk types
- [ ] Write tests for multi-select functionality
- [ ] Write tests for risk type filtering

**Acceptance Criteria:**
- Risk type selection uses checkboxes (not radio buttons)
- Multiple risk types can be selected simultaneously
- All 13 standard risk types available: Strategy, Governance, Operations, Legal, Property, Financial, Commercial, People, Technology, Information, Security, Project/Programme, Reputational
- Selected risk types are saved and displayed on risk detail view

---

### Feature: Risk Tier Classification

**Description:** Classify risks by governance tier (Project, Programme, Portfolio, Department, Cross-government).

**User Story:** As a risk manager, I want to classify risks by governance tier, so that risks are escalated to the appropriate governance level.

**Tasks:**
- [ ] Create RiskTier lookup table with 5 tiers
- [ ] Seed risk tier data:
  - [ ] Project-level
  - [ ] Programme-level
  - [ ] Portfolio-level
  - [ ] Department-level
  - [ ] Cross-government/systemic
- [ ] Add RiskTier foreign key to Risk model
- [ ] Update risk form with risk tier dropdown
- [ ] Display risk tier on risk detail view
- [ ] Add risk tier filter to risk index
- [ ] Update risk reports to include tier
- [ ] Write tests for risk tier assignment

**Acceptance Criteria:**
- Five risk tiers available: Project, Programme, Portfolio, Department, Cross-government
- Risk tier is a required field
- Tier selection determines escalation path
- Risks can be filtered by tier in reports

---

### Feature: Risk Assessment with Impact and Likelihood

**Description:** Assess risks using 1-5 scales for impact and likelihood with automatic risk score calculation.

**User Story:** As a risk assessor, I want to assess risks using impact and likelihood scales, so that risks are prioritised appropriately.

**Tasks:**
- [ ] Design impact scale (1-5) with descriptions:
  - [ ] 1 - Very low: Minimal impact on delivery or outcomes
  - [ ] 2 - Low: Minor impact, easily manageable
  - [ ] 3 - Medium: Moderate impact requiring intervention
  - [ ] 4 - High: Significant impact affecting key objectives
  - [ ] 5 - Very high: Severe impact threatening success
- [ ] Design likelihood scale (1-5) with percentages:
  - [ ] 1 - Very low: 0-10% chance
  - [ ] 2 - Low: 11-30% chance
  - [ ] 3 - Medium: 31-50% chance
  - [ ] 4 - High: 51-80% chance
  - [ ] 5 - Very high: 81-100% chance
- [ ] Create info modals with scale descriptions and examples
- [ ] Implement risk score calculation (Impact × Likelihood)
- [ ] Display risk score prominently on form and detail view
- [ ] Add risk score colour-coding (red/amber/green)
- [ ] Update risk index to show risk scores
- [ ] Add risk score range filter
- [ ] Write tests for risk score calculation
- [ ] Write tests for risk score colour-coding

**Acceptance Criteria:**
- Impact scale: 1 (Very low) to 5 (Very high) with clear descriptions
- Likelihood scale: 1 (Very low, 0-10%) to 5 (Very high, 81-100%)
- Risk score automatically calculated and displayed
- Info modals provide guidance and examples for each rating
- Risk score determines priority and escalation

---

### Feature: Risk Status Management

**Description:** Update risk status throughout its lifecycle with status tracking and audit trail.

**User Story:** As a risk owner, I want to update risk status throughout its lifecycle, so that stakeholders understand current risk state.

**Tasks:**
- [ ] Define risk status enum/options:
  - [ ] Open
  - [ ] In mitigation
  - [ ] Mitigated
  - [ ] Accepted
  - [ ] Realised
  - [ ] Closed
- [ ] Add status dropdown to risk form
- [ ] Update risk status on edit
- [ ] Display status badge on risk detail and index views
- [ ] Add status change tracking in audit trail
- [ ] Add status filter to risk index
- [ ] Add status-based workflow validation (e.g., can't close without notes)
- [ ] Create status change history view
- [ ] Write tests for status transitions
- [ ] Write tests for status filtering

**Acceptance Criteria:**
- Status options: Open, In mitigation, Mitigated, Accepted, Realised, Closed
- Status can be updated from risk detail/edit page
- Status changes are tracked in audit trail
- Risks can be filtered by status in all views

---

### Feature: Link Risks to Products

**Description:** Link risks to one or more products from FIPS CMS with multi-select capability.

**User Story:** As a risk manager, I want to link risks to one or more products, so that product teams can see risks affecting their products.

**Tasks:**
- [ ] Create RiskProduct junction table
- [ ] Update risk model with many-to-many relationship to Product
- [ ] Integrate FIPS CMS API to fetch products
- [ ] Create multi-select product dropdown with search (Select2)
- [ ] Handle product selection on form submit
- [ ] Save junction table entries on risk create/update
- [ ] Display linked products on risk detail view
- [ ] Update product dashboard to show associated risks
- [ ] Add product filter to risk index
- [ ] Write tests for product linking
- [ ] Write tests for product filtering

**Acceptance Criteria:**
- Multi-select product dropdown with search capability
- Products loaded from FIPS CMS in real-time
- Risks can be linked to multiple products
- Product dashboards show associated risks
- Risk index can filter by product

---

## EPIC: Product Governance (RAID) - Issues

### Feature: Create and Manage Issues

**Description:** Comprehensive issue management with severity, priority, blocking status, and resolution tracking.

**User Story:** As a product team member, I want to create a new issue with severity and priority, so that problems are logged and tracked for resolution.

**Tasks:**
- [ ] Create issue model and database migration
- [ ] Create issue controller with CRUD actions
- [ ] Build issue create form with fields:
  - [ ] Title (required)
  - [ ] Description (required)
  - [ ] Product (multi-select from FIPS CMS)
  - [ ] Severity (dropdown: Critical, High, Medium, Low)
  - [ ] Priority (dropdown: High, Medium, Low)
  - [ ] Business area (dropdown from FIPS CMS)
  - [ ] Issue owner (staff autocomplete)
  - [ ] Target resolution date (date picker)
  - [ ] Status (dropdown)
  - [ ] Blocked flag (checkbox)
  - [ ] Workaround (textarea)
- [ ] Add info modals for severity/priority guidance
- [ ] Implement staff autocomplete for issue owner
- [ ] Add form validation
- [ ] Create issue detail view
- [ ] Create issue edit view
- [ ] Create issue index view with filtering
- [ ] Implement soft delete
- [ ] Add audit trail logging
- [ ] Write unit tests for issue validation
- [ ] Write integration tests for issue CRUD operations

**Acceptance Criteria:**
- Form includes: title, description, product, severity, priority, status, target resolution date
- Severity levels: Critical, High, Medium, Low
- Priority levels: High, Medium, Low
- Blocked flag to indicate when progress cannot continue
- Business area classification
- Staff autocomplete for issue owner
- Validation ensures required fields are completed

---

### Feature: Issue Blocking Status

**Description:** Mark issues as blocked when external factors prevent resolution progress.

**User Story:** As an issue owner, I want to mark issues as blocked, so that stakeholders understand when external factors prevent resolution.

**Tasks:**
- [ ] Add Blocked boolean field to Issue model
- [ ] Add blocked checkbox to issue form
- [ ] Display blocked indicator on issue detail view
- [ ] Add blocked badge/icon to issue index view
- [ ] Highlight blocked issues visually (e.g., warning icon)
- [ ] Add blocked filter to issue index
- [ ] Update issue reports to include blocked status
- [ ] Write tests for blocked status functionality

**Acceptance Criteria:**
- Blocked checkbox available on issue form
- Blocked issues are visually distinguished in lists
- Blocked status shown on issue detail view
- Issues can be filtered by blocked status

---

### Feature: Issue Resolution Tracking

**Description:** Track issue resolution progress with target dates, status updates, and resolution notes.

**User Story:** As an issue owner, I want to track issue resolution progress with target dates, so that issues are resolved in a timely manner.

**Tasks:**
- [ ] Add target resolution date field to Issue model
- [ ] Add date picker to issue form
- [ ] Calculate and display overdue status
- [ ] Highlight overdue issues in red
- [ ] Define issue status workflow:
  - [ ] New
  - [ ] Open
  - [ ] In progress
  - [ ] Resolved
  - [ ] Closed
- [ ] Add resolution notes field
- [ ] Add workaround field for temporary solutions
- [ ] Create status update history
- [ ] Add resolution date tracking
- [ ] Write tests for overdue calculation
- [ ] Write tests for status workflow

**Acceptance Criteria:**
- Target resolution date field with date picker
- Overdue issues highlighted in red
- Status options include: New, Open, In progress, Resolved, Closed
- Resolution notes field for documenting solution
- Workaround field for temporary solutions

---

## EPIC: Product Governance (RAID) - Actions

### Feature: Create and Manage Actions

**Description:** Comprehensive action management with source tracking, parent-child relationships, and linking to risks/issues/milestones.

**User Story:** As a team member, I want to create an action item with due date and priority, so that tasks are tracked and completed.

**Tasks:**
- [ ] Create action model and database migration
- [ ] Create action controller with CRUD actions
- [ ] Build action create form with fields:
  - [ ] Title (required)
  - [ ] Description (required)
  - [ ] Assigned to (staff autocomplete)
  - [ ] Due date (date picker)
  - [ ] Priority (dropdown: High, Medium, Low)
  - [ ] Status (dropdown)
  - [ ] Action source (dropdown: Risk, Issue, Milestone, Service Assessment)
  - [ ] Business area (dropdown from FIPS CMS)
  - [ ] Parent action (dropdown for sub-actions)
  - [ ] Evidence (file upload or link)
  - [ ] Notes (textarea)
- [ ] Implement staff autocomplete for assignment
- [ ] Add action source dropdown
- [ ] Implement parent-child action relationships
- [ ] Add linking to risks, issues, milestones
- [ ] Add form validation
- [ ] Create action detail view
- [ ] Create action edit view
- [ ] Create action index view with filtering
- [ ] Implement soft delete
- [ ] Add audit trail logging
- [ ] Write unit tests for action validation
- [ ] Write integration tests for action CRUD operations

**Acceptance Criteria:**
- Form includes: title, description, assigned to, due date, priority, status, action source
- Action sources: Risk, Issue, Milestone, Service Assessment
- Actions can be linked to risks, issues, or milestones
- Parent-child action relationships for subtasks
- Business area classification
- Staff autocomplete for assignment
- Evidence field for supporting documentation

---

### Feature: Action Source Tracking

**Description:** Track where actions originated from (Risk, Issue, Milestone, Service Assessment).

**User Story:** As an action manager, I want to track where actions originated from, so that I can understand action context and relationships.

**Tasks:**
- [ ] Create ActionSource lookup table
- [ ] Seed action source data:
  - [ ] Risk
  - [ ] Issue
  - [ ] Milestone
  - [ ] Service Assessment
- [ ] Add ActionSource foreign key to Action model
- [ ] Add action source dropdown to action form
- [ ] When source is Risk/Issue/Milestone, show link selector
- [ ] Save linked entity on action create/update
- [ ] Display action source on action detail view
- [ ] Add action source filter to action index
- [ ] Update action reports to include source
- [ ] Write tests for action source assignment

**Acceptance Criteria:**
- Action source dropdown with: Risk, Issue, Milestone, Service Assessment
- When source is selected, can link to specific risk/issue/milestone
- Action source displayed on action detail view
- Actions can be filtered by source type

---

### Feature: Parent-Child Actions

**Description:** Create sub-actions linked to parent actions for breaking down complex tasks.

**User Story:** As an action manager, I want to create sub-actions linked to parent actions, so that complex tasks can be broken down into manageable steps.

**Tasks:**
- [ ] Add ParentActionId foreign key to Action model (self-referencing)
- [ ] Add parent action dropdown to action form
- [ ] Filter parent dropdown to exclude current action and its children
- [ ] Display parent relationship on child action detail view
- [ ] Display child actions list on parent action detail view
- [ ] Support multiple levels of hierarchy
- [ ] Add visual indentation for child actions in lists
- [ ] Optionally auto-complete parent when all children complete
- [ ] Update action reports to show hierarchy
- [ ] Write tests for parent-child relationships
- [ ] Write tests for hierarchy depth limits

**Acceptance Criteria:**
- Parent action dropdown available when creating/editing actions
- Child actions display parent relationship
- Parent actions show list of child actions
- Hierarchy can be multiple levels deep
- Completing all child actions can auto-complete parent (optional)

---

## EPIC: Product Governance (RAID) - Milestones

### Feature: Create and Manage Milestones

**Description:** Comprehensive milestone management with progress tracking, status updates, and strategic objective linking.

**User Story:** As a project manager, I want to create milestones with target dates and progress tracking, so that key deliverables are monitored.

**Tasks:**
- [ ] Create milestone model and database migration
- [ ] Create milestone controller with CRUD actions
- [ ] Build milestone create form with fields:
  - [ ] Title (required)
  - [ ] Description (required)
  - [ ] Product (multi-select from FIPS CMS)
  - [ ] Target date (date picker)
  - [ ] Progress percentage (0-100% slider or input)
  - [ ] Status (dropdown: Not started, In progress, At risk, Complete, Cancelled)
  - [ ] Priority (dropdown: High, Medium, Low)
  - [ ] Business area (dropdown from FIPS CMS)
  - [ ] Milestone owner (staff autocomplete)
  - [ ] Strategic objectives (multi-select)
- [ ] Implement progress percentage with visual progress bar
- [ ] Implement staff autocomplete for milestone owner
- [ ] Add form validation
- [ ] Create milestone detail view
- [ ] Create milestone edit view
- [ ] Create milestone index view with filtering
- [ ] Implement soft delete
- [ ] Add audit trail logging
- [ ] Write unit tests for milestone validation
- [ ] Write integration tests for milestone CRUD operations

**Acceptance Criteria:**
- Form includes: title, description, target date, progress percentage, status, priority
- Progress percentage (0-100%) with visual progress bar
- Status options: Not started, In progress, At risk, Complete, Cancelled
- Business area classification
- Staff autocomplete for milestone owner
- Can link to strategic objectives
- Can link to products

---

### Feature: Milestone Progress Updates

**Description:** Add progress updates with comments and status changes, tracked with timestamps and user attribution.

**User Story:** As a milestone owner, I want to add progress updates with comments, so that stakeholders are informed of milestone status.

**Tasks:**
- [ ] Create MilestoneUpdate model
- [ ] Add update form on milestone detail page
- [ ] Update form fields:
  - [ ] Progress percentage (0-100%)
  - [ ] Status (dropdown)
  - [ ] Comments (textarea)
- [ ] Save update with:
  - [ ] Timestamp
  - [ ] User attribution
  - [ ] Previous values
- [ ] Display update history chronologically on milestone detail view
- [ ] Show status changes in history
- [ ] Add visual timeline of updates
- [ ] Allow editing recent updates (within time limit)
- [ ] Write tests for update creation
- [ ] Write tests for update history display

**Acceptance Criteria:**
- Add update button on milestone detail page
- Update form includes: progress percentage, status, comments
- Updates are timestamped and attributed to user
- Update history displayed chronologically
- Status changes tracked in history

---

### Feature: Link Milestones to Strategic Objectives

**Description:** Link milestones to strategic objectives to demonstrate strategic alignment.

**User Story:** As a strategic planner, I want to link milestones to strategic objectives, so that delivery progress aligns with strategic goals.

**Tasks:**
- [ ] Create MilestoneObjective junction table
- [ ] Update milestone model with many-to-many relationship to Objective
- [ ] Add strategic objective multi-select to milestone form
- [ ] Load objectives from lookup table
- [ ] Save junction table entries on milestone create/update
- [ ] Display linked objectives on milestone detail view
- [ ] Update strategic objective detail view to show linked milestones
- [ ] Add strategic objective filter to milestone index
- [ ] Update milestone reports to include objectives
- [ ] Write tests for objective linking
- [ ] Write tests for objective filtering

**Acceptance Criteria:**
- Strategic objective dropdown on milestone form
- Multiple objectives can be linked to a milestone
- Strategic objective detail view shows linked milestones
- Milestones can be filtered by strategic objective

---

## EPIC: Delivery Reporting - Projects

### Feature: Project Management

**Description:** Comprehensive project management with RAG tracking, milestones, deliverables, outcomes, and dependencies.

**User Story:** As a project manager, I want to create a new project with comprehensive details, so that project delivery is tracked and monitored.

**Tasks:**
- [ ] Create project model and database migration
- [ ] Create project controller with CRUD actions
- [ ] Build project create form with fields:
  - [ ] Name (required)
  - [ ] Description (required)
  - [ ] Objectives (textarea)
  - [ ] Expected benefits (textarea)
  - [ ] Start date (date picker)
  - [ ] Planned end date (date picker)
  - [ ] Actual end date (date picker, optional)
  - [ ] Phase (dropdown: Discovery, Alpha, Beta, Live, Retired)
  - [ ] Status (dropdown: Active, On hold, Completed, Cancelled)
  - [ ] RAG status for delivery (dropdown: Red, Amber, Green)
  - [ ] RAG status for cost (dropdown: Red, Amber, Green)
  - [ ] RAG status for resource (dropdown: Red, Amber, Green)
  - [ ] Funding sources (collection)
  - [ ] Contacts (collection: Project Manager, SRO, Delivery Manager, etc.)
- [ ] Add form validation
- [ ] Create project detail view with tabbed interface:
  - [ ] Overview tab
  - [ ] Milestones tab
  - [ ] Deliverables tab
  - [ ] Outcomes tab
  - [ ] Products tab
  - [ ] Dependencies tab
  - [ ] Issues tab
  - [ ] Successes tab
  - [ ] Strategic alignment tab
  - [ ] Settings tab
- [ ] Create project edit view
- [ ] Create project index view with filtering
- [ ] Implement soft delete
- [ ] Add audit trail logging
- [ ] Write unit tests for project validation
- [ ] Write integration tests for project CRUD operations

**Acceptance Criteria:**
- Form includes: name, description, objectives, expected benefits, start date, planned end date
- Phase selection: Discovery, Alpha, Beta, Live, Retired
- Status selection: Active, On hold, Completed, Cancelled
- RAG status for delivery, cost, and resource dimensions
- Funding source and amount tracking
- Contact assignment (Project Manager, SRO, Delivery Manager, etc.)

---

### Feature: RAG Tracking

**Description:** Track RAG (Red/Amber/Green) status for delivery, cost, and resources with history and commentary.

**User Story:** As a project manager, I want to track RAG status for delivery, cost, and resources, so that project health is visible to stakeholders.

**Tasks:**
- [ ] Add RAG fields to Project model:
  - [ ] DeliveryRAG (enum: Red, Amber, Green)
  - [ ] CostRAG (enum: Red, Amber, Green)
  - [ ] ResourceRAG (enum: Red, Amber, Green)
- [ ] Create RAG history table to track changes
- [ ] Add RAG dropdowns to project form
- [ ] Display RAG status prominently on project overview
- [ ] Add RAG status change form with:
  - [ ] RAG dimension selection
  - [ ] New RAG status
  - [ ] Commentary (required if not Green)
  - [ ] Change date
- [ ] If RAG status is changed and isn't green, prompt for path to green
- [ ] Store history of RAG changes
- [ ] Display RAG history timeline
- [ ] Show previous RAG status
- [ ] Add RAG status filters to project index
- [ ] Update project reports to include RAG status
- [ ] Write tests for RAG status tracking
- [ ] Write tests for RAG history

**Acceptance Criteria:**
- Three RAG dimensions: Delivery, Cost, Resource
- RAG options: Red, Amber, Green
- RAG status displayed prominently on project overview
- RAG status can be updated with commentary
- RAG history tracked for trend analysis
- If RAG status changed and isn't green, prompt for path to green
- Projects can be filtered by RAG status

---

### Feature: Project Milestones and Deliverables

**Description:** Track project milestones and deliverables with dates, status, and completion tracking.

**User Story:** As a project manager, I want to track project milestones and deliverables, so that key outputs are monitored.

**Tasks:**
- [ ] Create ProjectMilestone model
- [ ] Create ProjectDeliverable model
- [ ] Add Milestones tab to project detail view
- [ ] Add Deliverables tab to project detail view
- [ ] Create milestone form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Target date (date picker)
  - [ ] Status (dropdown)
  - [ ] Completion date (date picker, optional)
- [ ] Create deliverable form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Target date (date picker)
  - [ ] Status (dropdown)
  - [ ] Completion date (date picker, optional)
- [ ] Display milestones and deliverables in timeline view
- [ ] Add CRUD operations for milestones and deliverables
- [ ] Link milestones to project milestones (if applicable)
- [ ] Write tests for milestone/deliverable management

**Acceptance Criteria:**
- Milestones tab on project detail page
- Deliverables tab on project detail page
- Can create, edit, and delete milestones and deliverables
- Milestones show target dates and status
- Deliverables can be marked as complete
- Timeline view of milestones and deliverables

---

### Feature: Project Outcomes Tracking

**Description:** Define and track project outcomes with success measures and achievement tracking.

**User Story:** As a project manager, I want to define and track project outcomes with success measures, so that project value is demonstrated.

**Tasks:**
- [ ] Create ProjectOutcome model
- [ ] Add Outcomes tab to project detail view
- [ ] Create outcome form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Success measures (textarea)
  - [ ] Target date (date picker)
  - [ ] Achievement date (date picker, optional)
  - [ ] Status (dropdown)
- [ ] Add quantitative success measure tracking
- [ ] Link outcomes to strategic objectives
- [ ] Display outcomes on project detail view
- [ ] Add CRUD operations for outcomes
- [ ] Create outcome achievement tracking
- [ ] Write tests for outcome management

**Acceptance Criteria:**
- Outcomes tab on project detail page
- Outcome form includes: title, description, success measures, target date
- Outcomes can be marked as achieved
- Success measures can be tracked quantitatively
- Outcomes linked to strategic objectives

---

### Feature: Project Status Updates

**Description:** Add status updates to projects with narrative, RAG status, and timestamp tracking.

**User Story:** As a project manager, I want to add status updates to projects, so that stakeholders are informed of project progress.

**Tasks:**
- [ ] Create ProjectStatusUpdate model
- [ ] Create status update form with:
  - [ ] Narrative (required, rich text)
  - [ ] Created date (date picker)
  - [ ] Created by (auto-populated)
- [ ] Display status updates chronologically
- [ ] Create status update detail view
- [ ] Implement status update editing
- [ ] Implement status update deletion
- [ ] Add status update history
- [ ] Write tests for status update management

**Acceptance Criteria:**
- Can add status updates to projects
- Status updates displayed chronologically
- Status updates can be edited and deleted
- Status update history tracked

---

### Feature: Project Artefacts

**Description:** Manage project artefacts and deliverables with file uploads and documentation links.

**User Story:** As a project manager, I want to manage project artefacts, so that project documentation is organised and accessible.

**Tasks:**
- [ ] Create ProjectArtefact model
- [ ] Create artefact form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Artefact type (dropdown)
  - [ ] File upload or URL
  - [ ] Created date
- [ ] Implement file upload functionality
- [ ] Create artefact list view
- [ ] Create artefact detail view
- [ ] Implement artefact editing
- [ ] Implement artefact deletion
- [ ] Add artefact filtering
- [ ] Write tests for artefact management

**Acceptance Criteria:**
- Can add project artefacts
- Artefacts can have file uploads or URLs
- Artefacts can be edited and deleted
- Artefact list view with filtering

---

### Feature: Project Team Members

**Description:** Manage project team members with roles and contact information.

**User Story:** As a project manager, I want to manage project team members, so that team structure is documented.

**Tasks:**
- [ ] Create ProjectTeamMember model
- [ ] Create team member form with:
  - [ ] Name (required)
  - [ ] Email (required)
  - [ ] Role (required, dropdown)
  - [ ] Start date (date picker)
  - [ ] End date (date picker, optional)
- [ ] Create team member list view
- [ ] Create team member detail view
- [ ] Implement team member editing
- [ ] Implement team member removal
- [ ] Link team members to users (if applicable)
- [ ] Write tests for team member management

**Acceptance Criteria:**
- Can add project team members
- Team members have roles and contact information
- Team members can be edited and removed
- Team member list view available

---

### Feature: Project Successes

**Description:** Record project successes and achievements for recognition and reporting.

**User Story:** As a project manager, I want to record project successes, so that achievements are documented and celebrated.

**Tasks:**
- [ ] Create ProjectSuccess model
- [ ] Create success form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Date (date picker)
  - [ ] Category (dropdown)
- [ ] Create success list view
- [ ] Display successes on project overview
- [ ] Implement success editing
- [ ] Implement success deletion
- [ ] Write tests for success management

**Acceptance Criteria:**
- Can add project successes
- Successes displayed on project view
- Successes can be edited and deleted

---

### Feature: Project Deliverables

**Description:** Track project deliverables with dates, status, and completion tracking.

**User Story:** As a project manager, I want to track project deliverables, so that key outputs are monitored.

**Tasks:**
- [ ] Create ProjectDeliverable model
- [ ] Create deliverable form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Target date (date picker)
  - [ ] Status (dropdown)
  - [ ] Completion date (date picker, optional)
- [ ] Create deliverable list view
- [ ] Display deliverables on project view
- [ ] Implement deliverable editing
- [ ] Implement deliverable deletion
- [ ] Link deliverables to flagship projects (if applicable)
- [ ] Write tests for deliverable management

**Acceptance Criteria:**
- Can add project deliverables
- Deliverables have target dates and status
- Deliverables can be marked as complete
- Deliverables can be edited and deleted

---

### Feature: Flagship Projects

**Description:** Mark projects as flagship projects and manage flagship-deliverable relationships.

**User Story:** As a portfolio manager, I want to mark projects as flagship projects, so that strategic initiatives are highlighted.

**Tasks:**
- [ ] Add IsFlagship flag to Project model
- [ ] Add flagship status toggle to project form
- [ ] Create flagship projects view
- [ ] Implement flagship-deliverable linking
- [ ] Display flagship status on project views
- [ ] Add flagship filtering to project index
- [ ] Write tests for flagship functionality

**Acceptance Criteria:**
- Can mark projects as flagship
- Flagship status displayed on project views
- Can link deliverables to flagship projects
- Can filter projects by flagship status

---

### Feature: Project Strategic Alignment

**Description:** Link projects to strategic objectives and mission pillars for alignment tracking.

**User Story:** As a strategic planner, I want to link projects to strategic objectives, so that delivery aligns with strategic goals.

**Tasks:**
- [ ] Add strategic objectives field to Project model
- [ ] Add mission pillars field to Project model
- [ ] Create strategic alignment edit interface
- [ ] Implement multi-select for strategic objectives
- [ ] Implement multi-select for mission pillars
- [ ] Display strategic alignment on project view
- [ ] Create strategic alignment reports
- [ ] Write tests for strategic alignment

**Acceptance Criteria:**
- Can link projects to strategic objectives
- Can link projects to mission pillars
- Strategic alignment displayed on project view
- Can filter projects by strategic alignment

---

### Feature: Project Funding Management

**Description:** Track project funding with resource types, counts, and percentage allocations.

**User Story:** As a project manager, I want to track project funding, so that financial resources are monitored.

**Tasks:**
- [ ] Create ProjectFunding model
- [ ] Create funding form with:
  - [ ] Resource type (dropdown: Permanent FTE, MSP FTE, etc.)
  - [ ] Count (decimal)
  - [ ] Programme percentage (decimal)
  - [ ] Admin percentage (decimal)
  - [ ] Notes (textarea)
- [ ] Display funding on project view
- [ ] Implement funding editing
- [ ] Calculate total funding
- [ ] Write tests for funding management

**Acceptance Criteria:**
- Can add project funding
- Funding tracked by resource type
- Funding percentages calculated
- Funding displayed on project view

---

### Feature: Project Delivery Phases

**Description:** Track delivery phase dates (Discovery, Alpha, Beta) with planned and actual dates.

**User Story:** As a project manager, I want to track delivery phase dates, so that phase progress is monitored.

**Tasks:**
- [ ] Add delivery phase date fields to Project model:
  - [ ] Discovery start/end (planned/actual)
  - [ ] Alpha start/end (planned/actual)
  - [ ] Private Beta start/end (planned/actual)
  - [ ] Public Beta start/end (planned/actual)
- [ ] Create delivery phases edit interface
- [ ] Display delivery phases on project view
- [ ] Calculate phase durations
- [ ] Track phase delays
- [ ] Write tests for phase tracking

**Acceptance Criteria:**
- Can set planned and actual dates for each phase
- Phase dates displayed on project view
- Phase delays calculated and displayed

---

### Feature: Project Contacts and Governance

**Description:** Manage project contacts including SROs, PMO contacts, budget owners, and directorates.

**User Story:** As a project manager, I want to manage project contacts, so that governance structure is documented.

**Tasks:**
- [ ] Create project contact management interface
- [ ] Add SRO management (multi-select users)
- [ ] Add PMO contacts management (multi-select users)
- [ ] Add budget owners management (multi-select)
- [ ] Add directorates management (multi-select)
- [ ] Add primary contact selection
- [ ] Display contacts on project view
- [ ] Write tests for contact management

**Acceptance Criteria:**
- Can assign SROs to projects
- Can assign PMO contacts to projects
- Can assign budget owners to projects
- Can assign directorates to projects
- Primary contact can be set
- Contacts displayed on project view

---

### Feature: Project Issues Management

**Description:** Track project-specific issues separate from operational issues.

**User Story:** As a project manager, I want to track project issues, so that project problems are logged and resolved.

**Tasks:**
- [ ] Create ProjectIssue model
- [ ] Create project issue form with:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Severity (dropdown)
  - [ ] Status (dropdown)
  - [ ] Target resolution date (date picker)
- [ ] Create project issues list view
- [ ] Display issues on project view (table and card views)
- [ ] Implement issue editing
- [ ] Implement issue deletion
- [ ] Add issue filtering
- [ ] Write tests for project issue management

**Acceptance Criteria:**
- Can add project issues
- Issues displayed on project view
- Issues can be edited and deleted
- Can filter issues by status and severity

---

## EPIC: Delivery Reporting - Performance Metrics

### Feature: Product Performance Metrics Submission

**Description:** Submit performance metric values for products with validation and historical tracking.

**User Story:** As a product owner, I want to submit performance metric values for my product, so that product performance is tracked over time.

**Tasks:**
- [ ] Create PerformanceMetricData model
- [ ] Create metric submission form
- [ ] Load configured metrics for selected product
- [ ] Support different value types:
  - [ ] Text
  - [ ] Number
  - [ ] Decimal
  - [ ] Percentage (0-100%)
- [ ] Implement validation for each value type
- [ ] Add reporting period selection
- [ ] Add commentary field
- [ ] Track submission date and user
- [ ] Prevent duplicate submissions for same period
- [ ] Create submission history view
- [ ] Add edit/delete capabilities for recent submissions
- [ ] Write tests for metric validation
- [ ] Write tests for duplicate prevention

**Acceptance Criteria:**
- Metric submission form shows all configured metrics for the product
- Support for different value types: text, number, decimal, percentage (0-100%)
- Validation ensures values are within acceptable ranges
- Submission date and period tracked
- Submitted by user tracked
- Historical submissions viewable
- Can add commentary to submissions

---

### Feature: Performance Metric Trends

**Description:** View performance metric trends over time with charts and target comparison.

**User Story:** As a product owner, I want to view performance metric trends over time, so that I can identify patterns and trends.

**Tasks:**
- [ ] Create metric trends view
- [ ] Implement line chart component (Chart.js or similar)
- [ ] Query historical metric data
- [ ] Display line chart showing values over time
- [ ] Add date range selection
- [ ] Support multiple metrics on same chart
- [ ] Display target values on charts
- [ ] Create tabular view of historical data
- [ ] Add export capability (Excel, CSV)
- [ ] Add trend indicators (improving, declining, stable)
- [ ] Write tests for trend calculation
- [ ] Write tests for chart rendering

**Acceptance Criteria:**
- Line charts showing metric values over time
- Can select date range for trend analysis
- Multiple metrics can be compared on same chart
- Target values displayed on charts
- Tabular view of historical data
- Export capability for data analysis

---

### Feature: Product Dashboard

**Description:** Comprehensive product dashboard showing all product information, RAID items, metrics, and assessments.

**User Story:** As a product contact, I want to view a comprehensive product dashboard, so that I can see all information about a product in one place.

**Tasks:**
- [ ] Create product dashboard view
- [ ] Fetch product information from FIPS CMS API
- [ ] Display product details:
  - [ ] FIPS ID
  - [ ] Title
  - [ ] Description
  - [ ] Phase
  - [ ] Status
  - [ ] Business area
  - [ ] Contacts
- [ ] Display RAID items section:
  - [ ] Active risks count and list
  - [ ] Active issues count and list
  - [ ] Active actions count and list
  - [ ] Active milestones count and list
- [ ] Display performance metrics section:
  - [ ] Latest metric values
  - [ ] Trend indicators
  - [ ] Link to full metrics view
- [ ] Display functional standards assessments section
- [ ] Display linked projects section
- [ ] Add quick actions to create new RAID items
- [ ] Add loading and error states
- [ ] Write tests for product dashboard queries

**Acceptance Criteria:**
- Product information from FIPS CMS displayed
- RAID items (risks, issues, actions, milestones) for the product
- Performance metrics with latest values and trends
- Functional standards assessments
- Linked projects
- Quick actions to create new RAID items

---

## EPIC: Enterprise Reporting

### Feature: Enterprise Metrics Submission

**Description:** Submit enterprise-level metrics for portfolio-wide performance tracking.

**User Story:** As a central operations team member, I want to submit enterprise-level metrics, so that portfolio-wide performance is tracked.

**Tasks:**
- [ ] Create EnterpriseMetric model
- [ ] Create EnterpriseMetricData model
- [ ] Create enterprise metrics dashboard
- [ ] Load configured enterprise metrics
- [ ] Create metric submission form
- [ ] Support different value types (text, number, decimal, percentage)
- [ ] Add validation
- [ ] Track submission date and user
- [ ] Create submission history view
- [ ] Link metrics to strategic objectives
- [ ] Add edit/delete capabilities
- [ ] Write tests for enterprise metric submission

**Acceptance Criteria:**
- Enterprise metrics dashboard accessible to authorised users
- All configured enterprise metrics displayed
- Support for different value types: text, number, decimal, percentage
- Validation and submission tracking
- Historical data viewable
- Can link to strategic objectives

---

### Feature: Functional Standards Assessment

**Description:** Conduct functional standards assessments for products with criterion-level scoring and evidence capture.

**User Story:** As a product owner, I want to conduct a functional standards assessment for my product, so that compliance with DDaT standards is tracked.

**Tasks:**
- [ ] Create Assessment model
- [ ] Create AssessmentCriterion model
- [ ] Integrate with Standards CMS API (if available)
- [ ] Create assessment form
- [ ] Load standards and criteria
- [ ] Navigate by theme and practice area
- [ ] For each criterion:
  - [ ] Display criterion description
  - [ ] Add scoring/grading dropdown
  - [ ] Add evidence upload/link field
  - [ ] Add notes field
- [ ] Save assessment as draft
- [ ] Submit assessment when complete
- [ ] Create assessment history view
- [ ] Compare assessments over time
- [ ] Write tests for assessment creation
- [ ] Write tests for assessment scoring

**Acceptance Criteria:**
- Assessment form shows all standards and criteria
- Can navigate by theme and practice area
- Each criterion can be scored/graded
- Evidence can be uploaded or linked
- Assessment can be saved as draft
- Assessment can be submitted when complete
- Historical assessments viewable

---

### Feature: Standards Compliance Dashboard

**Description:** View compliance status across all products and standards with visual indicators.

**User Story:** As a compliance manager, I want to view compliance status across all products and standards, so that I can identify compliance gaps.

**Tasks:**
- [ ] Create compliance dashboard view
- [ ] Query all assessments by product
- [ ] Calculate compliance scores by:
  - [ ] Product
  - [ ] Standard
  - [ ] Theme
  - [ ] Practice area
- [ ] Display compliance status with RAG indicators
- [ ] Add filtering by product, standard, theme, practice area
- [ ] Create compliance heat map
- [ ] Add export capability
- [ ] Add trend analysis
- [ ] Write tests for compliance calculation
- [ ] Write tests for compliance dashboard

**Acceptance Criteria:**
- Dashboard shows compliance status by product
- Compliance status by standard/theme
- Visual indicators (RAG) for compliance levels
- Filterable by product, standard, theme, practice area
- Export capability for reporting

---

## EPIC: Reports and Analytics

### Feature: Risks and Issues Report

**Description:** Comprehensive table-based report of all risks and issues with filtering, sorting, and export capabilities.

**User Story:** As a manager, I want to generate a comprehensive risks and issues report, so that I can analyse RAID status across products.

**Tasks:**
- [ ] Create risks and issues report view
- [ ] Query all risks and issues
- [ ] Display in table format with columns:
  - [ ] Title
  - [ ] Product
  - [ ] Owner
  - [ ] Status
  - [ ] Dates
  - [ ] Scores (for risks)
  - [ ] Severity/Priority (for issues)
- [ ] Implement filtering:
  - [ ] By product (multi-select)
  - [ ] By status
  - [ ] By owner
  - [ ] By date range
  - [ ] By type (for risks)
  - [ ] By tier (for risks)
  - [ ] By priority (for issues)
  - [ ] By risk score range
- [ ] Implement sorting by any column
- [ ] Add search functionality across all fields
- [ ] Implement pagination
- [ ] Add export to Excel
- [ ] Add export to PDF
- [ ] Add export to CSV
- [ ] Write tests for report queries
- [ ] Write tests for export functionality

**Acceptance Criteria:**
- Table-based report showing all risks and issues
- All key fields displayed: title, product, owner, status, dates, scores
- Filterable by product, status, owner, date range, type, tier
- Sortable by any column
- Search functionality across all fields
- Pagination for large datasets
- Export to Excel, PDF, or CSV

---

### Feature: RAID Analysis Report

**Description:** Visual analysis of RAID data through charts and graphs showing distributions and trends.

**User Story:** As an analyst, I want to view visual analysis of RAID data, so that I can identify patterns and trends.

**Tasks:**
- [ ] Create analysis report view
- [ ] Implement risk distribution chart (by type, tier, status)
- [ ] Implement risk heat map (impact vs likelihood)
- [ ] Implement issue status breakdown chart
- [ ] Implement action completion rate chart
- [ ] Implement trend analysis over time
- [ ] Implement product comparison charts
- [ ] Add interactive charts with drill-down capability
- [ ] Add filtering options
- [ ] Add export capability for charts
- [ ] Write tests for chart data aggregation
- [ ] Write tests for chart rendering

**Acceptance Criteria:**
- Charts showing risk distribution by type, tier, status
- Risk heat map (impact vs likelihood)
- Issue status breakdown
- Action completion rates
- Trend analysis over time
- Product comparison charts
- Interactive charts with drill-down capability

---

### Feature: Performance Trends Report

**Description:** View performance trends across products with line charts and target comparison.

**User Story:** As a performance analyst, I want to view performance trends across products, so that I can identify improving or declining performance.

**Tasks:**
- [ ] Create performance trends report view
- [ ] Query historical metric data
- [ ] Implement line charts showing metric trends over time
- [ ] Support comparing multiple products or metrics
- [ ] Display target vs actual comparison
- [ ] Add date range selection
- [ ] Add product/metric selection
- [ ] Add export capability
- [ ] Add trend indicators
- [ ] Write tests for trend calculation
- [ ] Write tests for chart rendering

**Acceptance Criteria:**
- Line charts showing metric trends over time
- Can compare multiple products or metrics
- Target vs actual comparison
- Date range selection
- Export capability

---

### Feature: Export Reports

**Description:** Export reports to various formats (Excel, PDF, CSV) with formatting and metadata.

**User Story:** As a report user, I want to export reports to various formats, so that I can share data and perform further analysis.

**Tasks:**
- [ ] Implement Excel export (.xlsx) with formatting
- [ ] Implement PDF export with charts and tables
- [ ] Implement CSV export for data analysis
- [ ] Include applied filters in exported data
- [ ] Include metadata (date, user, filters applied)
- [ ] Preserve formatting in Excel export
- [ ] Include charts in PDF export
- [ ] Add export button to all reports
- [ ] Handle large dataset exports (background job)
- [ ] Write tests for export functionality
- [ ] Write tests for export formatting

**Acceptance Criteria:**
- Export to Excel (.xlsx) with formatting
- Export to PDF with charts and tables
- Export to CSV for data analysis
- Exported data includes applied filters
- Export includes metadata (date, user, filters applied)

---

## EPIC: Administration

### Feature: User Management

**Description:** Create and manage user accounts with role assignment and account activation.

**User Story:** As an administrator, I want to create and manage user accounts, so that access is controlled appropriately.

**Tasks:**
- [ ] Create user management view
- [ ] Create user create form with fields:
  - [ ] Email (required, unique)
  - [ ] Name (required)
  - [ ] Role (dropdown: Administrator, Standard user)
  - [ ] Active (checkbox)
- [ ] Create user edit form
- [ ] Create user list view with search and filtering
- [ ] Implement user creation
- [ ] Implement user editing
- [ ] Implement user deactivation (soft delete)
- [ ] Add audit trail for user management actions
- [ ] Add user activity logging
- [ ] Write tests for user CRUD operations
- [ ] Write tests for role assignment

**Acceptance Criteria:**
- Create user form with email, name, role, active status
- Edit user details
- Deactivate users without deleting
- Role assignment: Administrator or Standard user
- User list with search and filtering
- Audit trail for user management actions

---

### Feature: System Settings - Risk Types

**Description:** Manage risk type lookup values for risk classification.

**User Story:** As an administrator, I want to manage risk type lookup values, so that risk classification options are maintained.

**Tasks:**
- [ ] Create risk types management view
- [ ] Create risk type list view
- [ ] Create risk type create form:
  - [ ] Code (required, unique, uppercase)
  - [ ] Name (required)
  - [ ] Active (checkbox)
- [ ] Create risk type edit form
- [ ] Implement risk type CRUD operations
- [ ] Seed 13 standard risk types
- [ ] Implement soft delete (deactivate)
- [ ] Add validation (unique code)
- [ ] Write tests for risk type management

**Acceptance Criteria:**
- Risk types list with code, name, active status
- Create new risk type with unique code
- Edit existing risk types
- Deactivate risk types (soft delete)
- 13 standard risk types pre-configured

---

### Feature: System Settings - Risk Tiers

**Description:** Manage risk tier lookup values for governance level classification.

**User Story:** As an administrator, I want to manage risk tier lookup values, so that governance levels are configured correctly.

**Tasks:**
- [ ] Create risk tiers management view
- [ ] Create risk tier list view
- [ ] Create risk tier create form:
  - [ ] Code (required, unique, uppercase)
  - [ ] Name (required)
  - [ ] Active (checkbox)
- [ ] Create risk tier edit form
- [ ] Implement risk tier CRUD operations
- [ ] Seed 5 standard risk tiers:
  - [ ] Project-level
  - [ ] Programme-level
  - [ ] Portfolio-level
  - [ ] Department-level
  - [ ] Cross-government/systemic
- [ ] Implement soft delete (deactivate)
- [ ] Add validation (unique code)
- [ ] Write tests for risk tier management

**Acceptance Criteria:**
- Risk tiers list with code, name, active status
- Create, edit, deactivate risk tiers
- Five standard tiers pre-configured: Project, Programme, Portfolio, Department, Cross-government

---

### Feature: System Settings - Action Sources

**Description:** Manage action source lookup values for tracking action origins.

**User Story:** As an administrator, I want to manage action source lookup values, so that action origins are tracked consistently.

**Tasks:**
- [ ] Create action sources management view
- [ ] Create action source list view
- [ ] Create action source create form:
  - [ ] Code (required, unique, uppercase)
  - [ ] Name (required)
  - [ ] Active (checkbox)
- [ ] Create action source edit form
- [ ] Implement action source CRUD operations
- [ ] Seed 4 standard action sources:
  - [ ] Risk
  - [ ] Issue
  - [ ] Milestone
  - [ ] Service Assessment
- [ ] Implement soft delete (deactivate)
- [ ] Add validation (unique code)
- [ ] Write tests for action source management

**Acceptance Criteria:**
- Action sources list with code, name, active status
- Create, edit, deactivate action sources
- Four standard sources pre-configured: Risk, Issue, Milestone, Service Assessment

---

### Feature: Performance Metrics Management

**Description:** Create and configure performance metrics for products with value types and phase assignments.

**User Story:** As an administrator, I want to create and configure performance metrics, so that product teams can track relevant KPIs.

**Tasks:**
- [ ] Create performance metrics management view
- [ ] Create metric create form with fields:
  - [ ] Name (required)
  - [ ] Description
  - [ ] Value type (dropdown: Text, Number, Decimal, Percentage)
  - [ ] Phase assignment (multi-select: Discovery, Alpha, Beta, Live)
  - [ ] Legal/regulatory flag (checkbox)
  - [ ] Validation rules (JSON or structured)
- [ ] Create metric edit form
- [ ] Create metric list view
- [ ] Implement metric CRUD operations
- [ ] Add metric assignment to products
- [ ] Implement validation rules configuration
- [ ] Write tests for metric management
- [ ] Write tests for validation rules

**Acceptance Criteria:**
- Metric form includes: name, description, value type, phase assignment
- Value types: Text, Number, Decimal, Percentage (0-100%)
- Can assign metrics to specific phases (Discovery, Alpha, Beta, Live)
- Legal/regulatory flag for mandatory metrics
- Validation rules configuration
- Metrics can be assigned to products

---

### Feature: Strategic Objectives Management

**Description:** Create and maintain strategic objectives for alignment with products and milestones.

**User Story:** As an administrator, I want to create strategic objectives, so that products and milestones can be aligned to strategic goals.

**Tasks:**
- [ ] Create strategic objectives management view
- [ ] Create objective create form with fields:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Type (dropdown)
  - [ ] Status (dropdown)
  - [ ] RAG status (dropdown: Red, Amber, Green)
- [ ] Create objective edit form
- [ ] Create objective list view with filtering
- [ ] Implement objective CRUD operations
- [ ] Add linking to milestones, risks, and products
- [ ] Create objective detail view showing linked items
- [ ] Write tests for objective management
- [ ] Write tests for objective linking

**Acceptance Criteria:**
- Objective form includes: title, description, type, status, RAG
- Objectives can be linked to milestones, risks, and products
- Objectives list with filtering
- Objective detail view shows linked items

---

## EPIC: Filtering and Personalisation

### Feature: Preferred Business Areas

**Description:** Allow users to set preferred business areas that automatically filter RAID views.

**User Story:** As a COMPASS user, I want to set my preferred business areas in settings, so that RAID views automatically filter to my areas.

**Tasks:**
- [ ] Create user preferences model
- [ ] Create My Settings page
- [ ] Fetch business areas from FIPS CMS
- [ ] Create multi-select business areas dropdown
- [ ] Save selected business areas to user preferences
- [ ] Update RAID index views to auto-filter to preferred areas
- [ ] Add visual indicator when preference filter is applied
- [ ] Add "View all items" override option
- [ ] Add link to change preferences from filtered views
- [ ] Write tests for preference saving
- [ ] Write tests for auto-filtering

**Acceptance Criteria:**
- My Settings page accessible from user menu
- Multi-select business areas dropdown (from FIPS CMS)
- Selected business areas saved to user preferences
- RAID index views auto-filter to preferred areas
- Can override filter to view all items

---

### Feature: Advanced Filtering on Index Pages

**Description:** Implement comprehensive filtering capabilities on all RAID index pages.

**User Story:** As a COMPASS user, I want to use advanced filters on RAID index pages, so that I can find specific items quickly.

**Tasks:**
- [ ] Implement dimension filter (All items, Assigned to me, My business areas)
- [ ] Implement multi-select product filter with search (Select2)
- [ ] Implement status filters (context-sensitive)
- [ ] Implement type-specific filters:
  - [ ] Risk score range for risks
  - [ ] Severity for issues
  - [ ] Priority for actions
  - [ ] Progress for milestones
- [ ] Implement time-based filters (overdue, due soon, etc.)
- [ ] Implement business area filter
- [ ] Add clear filters button
- [ ] Persist filters in URL
- [ ] Add filter summary display
- [ ] Write tests for filtering logic
- [ ] Write tests for filter persistence

**Acceptance Criteria:**
- Dimension filter: All items, Assigned to me, My business areas
- Multi-select product filter with search
- Status filters (context-sensitive)
- Type-specific filters (risk score, severity, priority, etc.)
- Time-based filters (overdue, due soon, etc.)
- Business area filter
- Clear filters button
- Filters persist when navigating

---

### Feature: Save Report Preferences

**Description:** Allow users to save frequently used report filter combinations.

**User Story:** As a report user, I want to save my report filter preferences, so that I can quickly access frequently used views.

**Tasks:**
- [ ] Create SavedReportView model
- [ ] Add "Save filters" button on report pages
- [ ] Create save view dialog with name field
- [ ] Save filter configuration to database
- [ ] Create "Load saved view" dropdown
- [ ] Load saved view and apply filters
- [ ] Add update saved view functionality
- [ ] Add delete saved view functionality
- [ ] Make saved views user-specific
- [ ] Write tests for saved view functionality

**Acceptance Criteria:**
- Save filters button on report pages
- Named saved views
- Load saved view dropdown
- Can update or delete saved views
- Saved views are user-specific

---

## EPIC: Integration and Data

### Feature: Staff Autocomplete

**Description:** Intelligent staff search using Azure Entra ID directory with autocomplete functionality.

**User Story:** As a COMPASS user, I want to search for staff members using autocomplete, so that I can assign owners and contacts accurately.

**Tasks:**
- [ ] Integrate Azure Entra ID Graph API
- [ ] Create staff search service
- [ ] Implement autocomplete component (Select2)
- [ ] Trigger search after 2+ characters typed
- [ ] Display search results with:
  - [ ] Name (prominently)
  - [ ] Email
  - [ ] Job title
  - [ ] Department
- [ ] Implement keyboard navigation
- [ ] Add debouncing for performance
- [ ] Limit results to 20
- [ ] Implement graceful fallback if API unavailable
- [ ] Cache recent searches
- [ ] Write tests for staff search
- [ ] Write tests for autocomplete component

**Acceptance Criteria:**
- Autocomplete triggers after typing 2+ characters
- Searches Azure Entra ID directory
- Displays name, email, job title, department
- Select2 integration for modern UI
- Keyboard navigation support
- Graceful fallback if API unavailable

---

### Feature: FIPS CMS Integration - Real-time Product Data

**Description:** Fetch product data from FIPS CMS API in real-time for up-to-date product information.

**User Story:** As a COMPASS user, I want to see up-to-date product information from FIPS CMS, so that product data is always current.

**Tasks:**
- [ ] Create FIPS CMS API service
- [ ] Implement product data fetching
- [ ] Cache business areas for 1 hour
- [ ] Update product dropdowns to use CMS data
- [ ] Update product detail pages to show CMS data
- [ ] Implement error handling if CMS unavailable
- [ ] Add retry logic for failed requests
- [ ] Add loading states
- [ ] Write tests for API integration
- [ ] Write tests for caching

**Acceptance Criteria:**
- Product data fetched from FIPS CMS API in real-time
- Business areas cached for 1 hour for performance
- Product dropdowns populated from CMS
- Product detail pages show CMS data
- Error handling if CMS unavailable

---

### Feature: FIPS CMS Integration - Product Contact Roles

**Description:** Load product contact roles from FIPS CMS to determine user permissions and product access.

**User Story:** As a COMPASS user, I want to see my product contact roles from FIPS CMS, so that I can access products I'm responsible for.

**Tasks:**
- [ ] Integrate product contact roles from FIPS CMS
- [ ] Load roles: Reporting, Service Owner, Product Owner, etc.
- [ ] Update dashboard to show products where user is a contact
- [ ] Implement permissions based on contact roles
- [ ] Cache contact roles for performance
- [ ] Update product access logic
- [ ] Write tests for role loading
- [ ] Write tests for permission logic

**Acceptance Criteria:**
- Product contact roles loaded from FIPS CMS
- Roles include: Reporting, Service Owner, Product Owner, etc.
- Dashboard shows products where user is a contact
- Permissions based on contact roles

---

### Feature: Form Validation

**Description:** Comprehensive form validation with clear error messages and field-level validation.

**User Story:** As a COMPASS user, I want to receive clear validation messages, so that I can correct errors before submitting.

**Tasks:**
- [ ] Implement required field validation
- [ ] Add visual indicators for required fields
- [ ] Implement data type validation (numbers, dates, emails)
- [ ] Implement range validation for numeric fields
- [ ] Implement unique constraint validation
- [ ] Implement foreign key validation
- [ ] Add field-level validation messages
- [ ] Add form-level validation summary
- [ ] Display validation errors clearly
- [ ] Prevent form submission with errors
- [ ] Write tests for validation rules
- [ ] Write tests for error display

**Acceptance Criteria:**
- Required fields clearly marked
- Validation messages appear on submit
- Field-level validation for data types
- Range validation for numeric fields
- Unique constraint validation
- Foreign key validation

---

### Feature: Contextual Help

**Description:** Provide contextual help on forms with info modals and guidance.

**User Story:** As a COMPASS user, I want to access contextual help on forms, so that I understand what information is required.

**Tasks:**
- [ ] Design info icon component
- [ ] Add info icons next to field labels
- [ ] Create info modal component
- [ ] Add help content for:
  - [ ] Impact/likelihood scales
  - [ ] Severity/priority definitions
  - [ ] Risk response strategies
  - [ ] Status options
  - [ ] Other complex fields
- [ ] Include examples and best practices in help
- [ ] Make modals accessible (keyboard navigation, ARIA labels)
- [ ] Add help text to form fields where appropriate
- [ ] Write tests for help modal functionality
- [ ] Write accessibility tests

**Acceptance Criteria:**
- Info icons next to field labels
- Clicking info icon opens modal with guidance
- Help text includes examples and best practices
- Impact/likelihood guidance with scale descriptions
- Severity/priority definitions
- Risk response strategy guidance

---

## EPIC: Demand Management

### Feature: Demand Request Management

**Description:** Comprehensive demand management system for capturing, triaging, prioritising, and tracking demand requests with full lifecycle management.

**User Story:** As a demand manager, I want to create and manage demand requests, so that new work is captured, assessed, and prioritised appropriately.

**Tasks:**
- [ ] Create DemandRequest model and database migration
- [ ] Create demand request controller with CRUD actions
- [ ] Build demand request create form with sections:
  - [ ] Overview section
  - [ ] Strategic alignment section
  - [ ] Impact and risk section
  - [ ] Delivery planning section
  - [ ] Delivery section
  - [ ] Funding and headcount section
  - [ ] Assessment section
- [ ] Implement section-based form navigation
- [ ] Implement section completion tracking
- [ ] Add demand request status workflow
- [ ] Create demand request detail view with tabbed sections
- [ ] Create demand request edit functionality
- [ ] Create demand request list view with filtering
- [ ] Implement demand request assignment
- [ ] Add notes and comments functionality
- [ ] Implement soft delete
- [ ] Add audit trail logging
- [ ] Write unit tests for demand request validation
- [ ] Write integration tests for demand request CRUD operations

**Acceptance Criteria:**
- Can create demand requests with all required sections
- Section-based form navigation works correctly
- Section completion status tracked
- Demand requests can be assigned to users
- Notes and comments can be added
- Status workflow functions correctly

---

### Feature: Demand Request Triage

**Description:** Triage meetings for reviewing and prioritising demand requests with meeting management and decision tracking.

**User Story:** As a triage meeting organiser, I want to manage triage meetings and review demand requests, so that demand is properly assessed and prioritised.

**Tasks:**
- [ ] Create TriageMeeting model
- [ ] Create triage meeting management interface
- [ ] Create triage view showing requests for review
- [ ] Implement submit to triage functionality
- [ ] Implement remove from triage functionality
- [ ] Add triage notes and decisions
- [ ] Track triage meeting outcomes
- [ ] Add triage meeting scheduling
- [ ] Create triage meeting history
- [ ] Write tests for triage functionality

**Acceptance Criteria:**
- Can create and manage triage meetings
- Demand requests can be submitted to triage
- Triage decisions can be recorded
- Triage meeting history is tracked

---

### Feature: Demand Request Prioritisation

**Description:** Prioritisation and scoring system for demand requests with tier and portfolio management.

**User Story:** As a portfolio manager, I want to prioritise and score demand requests, so that resources are allocated to the most important work.

**Tasks:**
- [ ] Create prioritisation view by tier and portfolio
- [ ] Implement demand request scoring system
- [ ] Create score request form
- [ ] Calculate predicted risk level from risk types
- [ ] Display prioritisation scores
- [ ] Add portfolio filtering
- [ ] Add tier filtering
- [ ] Create prioritisation reports
- [ ] Write tests for prioritisation logic

**Acceptance Criteria:**
- Can view demand requests by tier and portfolio
- Can score demand requests
- Predicted risk level calculated from risk types
- Prioritisation scores displayed and sortable

---

### Feature: Demand Request Reporting

**Description:** Comprehensive reporting for demand requests with analytics and export capabilities.

**User Story:** As a demand manager, I want to view reports on demand requests, so that I can understand demand patterns and make informed decisions.

**Tasks:**
- [ ] Create demand request reporting view
- [ ] Implement filtering by status, portfolio, tier
- [ ] Add search functionality
- [ ] Create analytics and charts
- [ ] Add export capabilities (Excel, PDF, CSV)
- [ ] Create demand request dashboard
- [ ] Add trend analysis
- [ ] Write tests for reporting queries

**Acceptance Criteria:**
- Can filter demand requests by multiple criteria
- Reports show analytics and trends
- Can export reports to various formats
- Dashboard provides overview of demand

---

### Feature: Convert Demand Request to Project

**Description:** Convert approved demand requests into projects for delivery tracking.

**User Story:** As a project manager, I want to convert approved demand requests into projects, so that delivery can be tracked.

**Tasks:**
- [ ] Implement convert to project functionality
- [ ] Map demand request data to project fields
- [ ] Create project from demand request
- [ ] Link project to original demand request
- [ ] Update demand request status after conversion
- [ ] Add conversion history tracking
- [ ] Write tests for conversion logic

**Acceptance Criteria:**
- Can convert demand requests to projects
- Project data populated from demand request
- Project linked to original demand request
- Conversion tracked in history

---

## EPIC: Accessibility Management

### Feature: Product Accessibility Management

**Description:** Comprehensive accessibility management for products including statements, issues, audits, and WCAG compliance tracking.

**User Story:** As a product owner, I want to manage accessibility for my product, so that accessibility requirements are met and issues are tracked.

**Tasks:**
- [ ] Create ProductAccessibility model
- [ ] Create accessibility controller with CRUD actions
- [ ] Build product enrollment functionality
- [ ] Create accessibility dashboard/index view
- [ ] Display summary statistics:
  - [ ] Total enrolled products
  - [ ] Total open issues
  - [ ] Overdue issues
  - [ ] Total audit spend
- [ ] Implement product search and filtering
- [ ] Create product accessibility detail view with tabs:
  - [ ] Overview tab
  - [ ] Issues tab
  - [ ] Audits tab
  - [ ] Settings tab
- [ ] Add WCAG compliance tracking
- [ ] Add accessibility statement verification
- [ ] Implement SLA management
- [ ] Add complaints email configuration
- [ ] Write unit tests for accessibility management
- [ ] Write integration tests

**Acceptance Criteria:**
- Can enroll products for accessibility management
- Dashboard shows summary statistics
- Product detail view shows all accessibility information
- WCAG compliance can be tracked
- Accessibility statements can be verified

---

### Feature: Accessibility Issues Management

**Description:** Track and manage accessibility issues with WCAG criteria linking and resolution tracking.

**User Story:** As an accessibility manager, I want to track accessibility issues, so that problems are identified and resolved.

**Tasks:**
- [ ] Create AccessibilityIssue model
- [ ] Create issue create form with fields:
  - [ ] Title (required)
  - [ ] Description
  - [ ] Issue level (dropdown)
  - [ ] Status (dropdown)
  - [ ] Planned resolution date
  - [ ] WCAG criteria links (multi-select)
- [ ] Link issues to WCAG criteria
- [ ] Create issue detail view
- [ ] Implement issue status workflow
- [ ] Add issue comments functionality
- [ ] Implement issue closure with explanation
- [ ] Add retest request functionality
- [ ] Create all issues view with filtering
- [ ] Add issue search functionality
- [ ] Write tests for issue management

**Acceptance Criteria:**
- Can create accessibility issues
- Issues can be linked to WCAG criteria
- Issue status can be updated
- Issues can be closed with explanation
- Retest requests can be made

---

### Feature: Accessibility Audits Management

**Description:** Track accessibility audits with cost tracking, report links, and audit history.

**User Story:** As an accessibility manager, I want to track accessibility audits, so that audit history and costs are recorded.

**Tasks:**
- [ ] Create AuditHistory model
- [ ] Create audit add form with fields:
  - [ ] Audit date (required)
  - [ ] Audit type (dropdown)
  - [ ] Audited by (text)
  - [ ] Cost (decimal, optional)
  - [ ] Report URL (optional)
  - [ ] Notes (textarea)
- [ ] Create audit list view
- [ ] Display audit history chronologically
- [ ] Calculate total audit spend
- [ ] Add audit filtering
- [ ] Implement audit deletion (soft delete)
- [ ] Write tests for audit management

**Acceptance Criteria:**
- Can add accessibility audits
- Audit history displayed chronologically
- Total audit spend calculated
- Audits can be filtered and searched

---

### Feature: Accessibility Contact Methods

**Description:** Manage contact methods for accessibility complaints and feedback.

**User Story:** As a product owner, I want to manage accessibility contact methods, so that users can report accessibility issues.

**Tasks:**
- [ ] Create ContactMethod model
- [ ] Create contact method add form
- [ ] Support multiple contact types:
  - [ ] Email
  - [ ] Phone
  - [ ] Web form
  - [ ] Other
- [ ] Add contact method list view
- [ ] Implement contact method ordering (sort order)
- [ ] Add contact method deletion
- [ ] Display contact methods on product detail view
- [ ] Write tests for contact method management

**Acceptance Criteria:**
- Can add multiple contact methods
- Contact methods can be ordered
- Contact methods displayed on product view
- Contact methods can be deleted

---

### Feature: Accessibility Heatmap

**Description:** Visual heatmap showing accessibility issues by WCAG criteria and product.

**User Story:** As an accessibility manager, I want to view a heatmap of accessibility issues, so that I can identify patterns and priorities.

**Tasks:**
- [ ] Create heatmap view
- [ ] Query issues grouped by WCAG criteria
- [ ] Query issues grouped by product
- [ ] Implement heatmap visualisation
- [ ] Add filtering by issue level
- [ ] Add filtering by WCAG criteria type
- [ ] Add export capability
- [ ] Write tests for heatmap data aggregation

**Acceptance Criteria:**
- Heatmap displays issues by WCAG criteria
- Heatmap displays issues by product
- Can filter heatmap by various criteria
- Heatmap can be exported

---

## EPIC: DDT Standards Management

### Feature: DDT Standards Creation and Management

**Description:** Create and manage DDT (Digital, Data and Technology) standards with workflow for draft, review, approval, and publishing.

**User Story:** As a standards manager, I want to create and manage DDT standards, so that standards are properly documented and published.

**Tasks:**
- [ ] Create DdtStandard model
- [ ] Create standards controller with CRUD actions
- [ ] Build standard create form with fields:
  - [ ] Title (required)
  - [ ] Summary
  - [ ] Purpose (rich text)
  - [ ] How to meet (rich text)
  - [ ] Governance (rich text)
  - [ ] Categories (multi-select)
  - [ ] Sub-categories (multi-select)
  - [ ] Phases (multi-select)
  - [ ] Owners (multi-select users)
  - [ ] Contacts (multi-select users)
- [ ] Implement standard workflow stages:
  - [ ] Draft
  - [ ] Under Review
  - [ ] Approved
  - [ ] Published
  - [ ] Rejected
  - [ ] Archived
- [ ] Create standard detail view
- [ ] Create standard edit view
- [ ] Create standard list view with filtering
- [ ] Implement standard publishing
- [ ] Add version control
- [ ] Write tests for standard management

**Acceptance Criteria:**
- Can create DDT standards with all required fields
- Standards workflow functions correctly
- Standards can be published
- Version control tracks changes

---

### Feature: DDT Standards Categories and Sub-categories

**Description:** Manage categories and sub-categories for organising standards.

**User Story:** As a standards manager, I want to organise standards by categories and sub-categories, so that standards are easy to find and navigate.

**Tasks:**
- [ ] Create DdtStandardCategory model
- [ ] Create DdtStandardSubCategory model
- [ ] Create category management interface
- [ ] Create sub-category management interface
- [ ] Link standards to categories and sub-categories
- [ ] Display categories and sub-categories on standard views
- [ ] Add category/sub-category filtering
- [ ] Write tests for category management

**Acceptance Criteria:**
- Can create and manage categories
- Can create and manage sub-categories
- Standards can be linked to categories/sub-categories
- Filtering by category/sub-category works

---

### Feature: DDT Standards Approval Workflow

**Description:** Approval workflow for standards with review, approval, and rejection capabilities.

**User Story:** As a standards reviewer, I want to review and approve standards, so that only approved standards are published.

**Tasks:**
- [ ] Implement review submission
- [ ] Create review interface
- [ ] Add approval functionality
- [ ] Add rejection functionality with reasons
- [ ] Track approval history
- [ ] Notify stakeholders of status changes
- [ ] Add comments during review
- [ ] Write tests for approval workflow

**Acceptance Criteria:**
- Standards can be submitted for review
- Reviewers can approve or reject standards
- Rejection reasons are captured
- Approval history is tracked

---

## EPIC: User Leadership Management

### Feature: User Leadership Assignment

**Description:** Assign leadership roles to users for specific business areas with role-based access control.

**User Story:** As an administrator, I want to assign leadership roles to users, so that users have appropriate access to business areas.

**Tasks:**
- [ ] Create UserBusinessAreaRoleAssignment model
- [ ] Create user leadership management interface
- [ ] Build user selection interface
- [ ] Create business area multi-select
- [ ] Create role multi-select (e.g., Portfolio Lead, Business Area Lead)
- [ ] Implement assignment creation
- [ ] Implement assignment deletion
- [ ] Display user leadership assignments
- [ ] Apply leadership-based filtering to views
- [ ] Write tests for leadership assignment

**Acceptance Criteria:**
- Can assign leadership roles to users
- Users can have multiple business area assignments
- Leadership assignments affect access control
- Assignments can be removed

---

## EPIC: User Satisfaction Surveys

### Feature: User Satisfaction Survey Management

**Description:** Manage user satisfaction surveys for products with enrollment, response tracking, and analytics.

**User Story:** As a product owner, I want to track user satisfaction for my product, so that I can measure and improve user experience.

**Tasks:**
- [ ] Create Service model (for enrolled products)
- [ ] Create SurveyInstance model
- [ ] Create SurveyResponse model
- [ ] Create survey management interface
- [ ] Implement product enrollment for surveys
- [ ] Create survey response collection
- [ ] Calculate USS (User Satisfaction Score) metrics
- [ ] Create survey dashboard with:
  - [ ] Enrolled products list
  - [ ] Average scores
  - [ ] Response counts
  - [ ] Non-enrolled products list
- [ ] Add search and filtering
- [ ] Add score range filtering
- [ ] Create survey analytics and trends
- [ ] Write tests for survey management

**Acceptance Criteria:**
- Can enroll products for surveys
- Survey responses can be collected
- USS scores are calculated correctly
- Dashboard shows survey analytics
- Can filter by score range

---

## EPIC: Staff Role Returns

### Feature: Staff Role Return Submission

**Description:** Annual staff role return system for GDD (Government Digital and Data) role reporting with skills tracking.

**User Story:** As a staff member, I want to submit my annual role return, so that my role and skills are recorded for reporting.

**Tasks:**
- [ ] Create StaffRoleReturn model
- [ ] Create GddRole model
- [ ] Create Skill model
- [ ] Create staff role return form with fields:
  - [ ] GDD Role (required, dropdown)
  - [ ] Secondary skills (multi-select)
  - [ ] Year (auto-calculated based on reporting period)
- [ ] Implement reporting period logic (Apr-Mar, due 31 Mar)
- [ ] Calculate due dates automatically
- [ ] Track overdue returns
- [ ] Create role return detail view
- [ ] Implement create/update functionality
- [ ] Add role return history
- [ ] Write tests for role return logic

**Acceptance Criteria:**
- Can submit annual role returns
- Reporting period calculated correctly
- Due dates tracked
- Overdue returns identified
- Role and skills recorded

---

### Feature: GDD Roles and Skills Management

**Description:** Manage GDD roles and skills lookup tables for role return system.

**User Story:** As an administrator, I want to manage GDD roles and skills, so that role returns use correct data.

**Tasks:**
- [ ] Create GDD roles management interface
- [ ] Create skills management interface
- [ ] Implement role family grouping
- [ ] Add active/inactive status
- [ ] Seed standard GDD roles
- [ ] Seed standard skills
- [ ] Write tests for roles and skills management

**Acceptance Criteria:**
- Can create and manage GDD roles
- Can create and manage skills
- Roles organised by role family
- Active/inactive status works

---

## EPIC: DDT Reports

### Feature: DDT Reports Generation

**Description:** Generate comprehensive DDT reports with filtering, analytics, and export capabilities.

**User Story:** As a report user, I want to generate DDT reports, so that I can analyse and share DDT data.

**Tasks:**
- [ ] Create DDT reports controller
- [ ] Implement report generation logic
- [ ] Create report views with filtering
- [ ] Add analytics and charts
- [ ] Implement export to Excel, PDF, CSV
- [ ] Add report scheduling
- [ ] Create report templates
- [ ] Write tests for report generation

**Acceptance Criteria:**
- Can generate DDT reports
- Reports can be filtered
- Reports include analytics
- Reports can be exported

---

## EPIC: Government Department Management

### Feature: Government Department Management

**Description:** Manage government departments for organisational structure and reporting.

**User Story:** As an administrator, I want to manage government departments, so that organisational structure is maintained.

**Tasks:**
- [ ] Create GovernmentDepartment model
- [ ] Create department management interface
- [ ] Implement department CRUD operations
- [ ] Link departments to other entities
- [ ] Add department hierarchy support
- [ ] Write tests for department management

**Acceptance Criteria:**
- Can create and manage government departments
- Departments can be linked to other entities
- Department hierarchy supported

---

## EPIC: Organizational Management

### Feature: Organizational Structure Management

**Description:** Manage organisational structure including departments, teams, and hierarchies.

**User Story:** As an administrator, I want to manage organisational structure, so that the organisation is properly represented in the system.

**Tasks:**
- [ ] Create organizational models
- [ ] Create organizational management interface
- [ ] Implement organizational CRUD operations
- [ ] Support organizational hierarchies
- [ ] Link to business areas
- [ ] Write tests for organizational management

**Acceptance Criteria:**
- Can manage organizational structure
- Hierarchies supported
- Links to business areas work

---

## EPIC: Business Reporting

### Feature: Business Reporting Dashboard

**Description:** Business reporting dashboard with metrics, analytics, and insights.

**User Story:** As a business manager, I want to view business reports, so that I can make informed decisions.

**Tasks:**
- [ ] Create business reporting controller
- [ ] Create business reporting dashboard
- [ ] Implement metrics aggregation
- [ ] Create analytics and charts
- [ ] Add filtering and date range selection
- [ ] Implement export capabilities
- [ ] Add report scheduling
- [ ] Write tests for business reporting

**Acceptance Criteria:**
- Business reporting dashboard displays metrics
- Analytics and charts work correctly
- Can filter and export reports

---

## Notes for Implementation

### Priority Levels
- **P0 (Critical):** Core functionality required for MVP
- **P1 (High):** Important features for initial release
- **P2 (Medium):** Nice-to-have features for enhanced usability
- **P3 (Low):** Future enhancements

### Dependencies
- Features marked with dependencies should be implemented in order
- Integration features depend on external API availability
- Reporting features depend on data collection features

### Testing Requirements
- All features require unit tests
- All features require integration tests
- UI features require accessibility tests
- Critical features require performance tests

### Documentation
- Each feature should have:
  - User story documentation
  - Technical documentation
  - API documentation (if applicable)
  - Update user guide documentation

---

**End of GitHub Issues Import File**

