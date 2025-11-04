# Compass – Features, Modules, Data Models & RBAC Reference

## 1. Introduction

Compass (Compliance, Outcomes, Performance, Assurance, Standards, and Strategy) is a strategic platform for the Department for Education (DfE) supporting government service reporting, milestone tracking, administrative management, and product governance. This document provides a comprehensive overview of all major functions, data models, actions, permissions, and workflows – enabling requirements analysis and RBAC (Role-Based Access Control) design.

---

## 2. Platform Modules & Core Features

### 2.1 Admin Module
- **Create/manage reporting metrics**: Performance KPIs configurable by stage, compliance, and validation requirements.
- **User management**: Create/edit users, assign granular permissions (per product/metric/report/milestone), enable/disable accounts.
- **Objective management**: Strategic goals, assignable to milestones and used for alignment.
- **Admin dashboard**: System-wide stats and quick links.
- **Full audit trail** for all administrative actions.

### 2.2 Reporting Module
- **Submit/manage monthly reports**: Role-based, per-product or in bulk.
- **Dashboard**: Overview of assignments, completion status, due dates, RAG (Red/Amber/Green) states.
- **Performance metric completion**: Guided entry & validation of KPI data.
- **Report/submission management**: Individual or multiple services at once, status tracking, deadline monitoring.

### 2.3 Milestones Module
- **Define/configure milestones**: Title, description, target date.
- **Progress and RAG tracking**: Updates, comments, indicators.
- **Timeline & dependency management**: Deadline, links to objectives, priority, relationships.
- **Approval, notification & export**: History, comments, exports.

### 2.4 Product Governance (RAID) – Objectives, Risks, Issues, Decisions (Milestones), Actions
- **Track objectives**: Strategic context, RAG, status.
- **Risks**: Categorisation, likelihood/impact, scoring, ownership, mitigation actions.
- **Issues**: Severity, resolution status, priorities.
- **Actions**: Tasks to resolve risks/issues, stand-alone or parent/child.
- **Relationships**: Many-to-many via junction tables, e.g. link actions to risks/issues/milestones.
- **Full CRUD** and status/presentation by context.

---

## 3. Data Models – Detailed

### 3.1 User & Permission Models
- **UserPermission**: RBAC flags – per user/email, can view/add/edit/delete products, metrics, milestones, users, reports.
- **ReportingUser**: Core user info, role (`admin`, `reporting_user`, `central_operations`), status.

### 3.2 Metric Models
- **PerformanceMetric**: Central definitions (name, compliance, validation), assigned to multiple service/product stages.
- **PerformanceMetricData**: Metric value submission (by metric, product, period, user).

### 3.3 Milestone/Objectives Models
- **Milestone**: Delivery goal – title, description, status, RAG, due dates, progress %, priority. Linked to objectives and products.
- **MilestoneUpdate**: Progress comments, status history, user attribution, timestamp.
- **Objective**: Broad/strategic aims, assigned to milestones, with type, RAG/status, governance link.

### 3.4 RAID Models
- **Risk**: Title, description, category, owner, likelihood, impact, score, status. Linked to mitigation actions and milestones.
- **Issue**: Title, category, severity, priority, target dates, status, blocked flag, related actions.
- **Action**: Standalone or linked task (to risk/issue/milestone/objective); parent/child relationships for subtasks, plus assigned user, priority, evidence field, status, notes.

### 3.5 Relationships
- **Junction tables**: Many-to-many between actions and risks/milestones/issues (e.g. `RiskAction`, `MilestoneAction` ...)
- **Hierarchy**: Objectives can contain risks/issues/milestones/actions; actions may have sub-actions.
- **Assignment**: Users mapped by email/ID via CMS `product_contacts` and Compass user tables.

### 3.6 Audit & Validation
- **Fields**: `CreatedAt`, `UpdatedAt` for all entities, soft delete/enable flags, user attribution.
- **Validation**: Required field and string length enforcement, DB constraints, property-level checks.

---

## 4. Application Services

- **Admin Services**: User, permission, objective, metric, milestone CRUD.
- **Product/Reporting Services**: Metrics, reporting period, return submission (with business logic, validation, duplicate prevention).
- **CMS Integration**: Fetches roles/product contacts/user assignments from central CMS for both permissions and product eligibility.
- **Notification Service**: Emails & alerts for overdue, new, completed, blocked items.
- **API Layer**: REST endpoints secured by Azure AD (for reporting, metrics, status, export, product info).
- **Audit Logging**: Full action traceability for compliance and support.

---

## 5. Views (UI)

### 5.1 Dashboards
- **Personal dashboard**: Aggregates products/risks/issues/actions/milestones assigned to or owned by user. Filter by type/status/urgency etc.
- **Admin dashboard**: System stats, quick links, user/permission/product overview.

### 5.2 Entity Index/Detail Views
- **Index**: Lists of objectives, risks, issues, actions, milestones with stats, RAG, status.
- **Create/Edit/Delete/Details**: Full resource CRUD with validation and contextual status, linking and navigation between related entities.
- **Bulk/Quick Actions**: For reporting and submission, milestone and action entry, product governance.

### 5.3 Filter & Navigation
- **By product, ownership, dates, RAG, status, priority, severity etc.**
- **Breadcrumbs, sidebar nav, search, contextual cards, summary stats.**
- **Clear states, quick create, one-click navigation between related records.**

### 5.4 Visualisation
- **Progress bars**, **RAG status badges**, overdue/urgent highlighting, empty state explanations, summary cards.
- **Exports**: Download milestone/product/reporting results.

---

## 6. RBAC Model & Requirements Touchpoints

### 6.1 Roles (from codebase/config)
- **Admin** – Full access to all products, metrics, milestones, users, reports, and permission management.
- **Reporting User** – Can view and submit reports for assigned products/services.
- **Service Owner** – Responsible for completing monthly reports, progress, milestone, risk and objective submission for owned areas.
- **Central Operations** – May have broad access across multiple products/services for oversight.
- **Product Contact** – Specific permission per product/service assignment, driven from CMS.

### 6.2 Permissions
- **Scope**: Read/Create/Update/Delete for each resource (product, objective, metric, milestone, risk, issue, action, user).
- **Assignment**: At the user and product level, optionally via role or flag. (Example: a user may edit milestones only for Product Alpha, but view all.)
- **Implementation**: `UserPermission` model, CMS imports, controller-level `[Authorize]` attributes, in-view UI checks for quick actions and editing.
- **Auditability**: All permission changes and sensitive actions are auditable.
- **Soft delete**: Deactivated entities not deleted, just hidden based on permission.

### 6.3 User Action Map (Common RBAC requirements)
| Resource           | List/View | Create | Edit | Delete | Submit | Assign/Approve |
|--------------------|-----------|--------|------|--------|--------|----------------|
| Metrics            | ✓         | ✓      | ✓    | ✓      | ✓      | -              |
| Milestones         | ✓         | ✓      | ✓    | ✓      | -      | ✓              |
| Reports            | ✓         | -      | -    | -      | ✓      | -              |
| Users/Permissions  | ✓         | ✓      | ✓    | ✓      | -      | -              |
| Risks              | ✓         | ✓      | ✓    | ✓      | -      | ✓              |
| Issues             | ✓         | ✓      | ✓    | ✓      | -      | ✓              |
| Actions            | ✓         | ✓      | ✓    | ✓      | -      | ✓              |
| Objectives         | ✓         | ✓      | ✓    | ✓      | -      | -              |

---

## 7. Entity Relationships (Summary)

- **UserPermission (1:1) ReportingUser** (email mapping)
- **Objective (1:N) Milestone**
- **PerformanceMetric (1:N) PerformanceMetricData**
- **Milestone (1:N) MilestoneUpdate**
- **Many-to-many**: Risk–Action, Issue–Action, Milestone–Action, Milestone–Risk, Milestone–Issue (junction tables)

---

## 8. Security & Integrity Principles

- **Authentication**: Azure Entra ID (Microsoft Identity); required for all controllers.
- **Controller-level enforcement**: `[Authorize]` required, method-level permission checks.
- **CSRF, XSS, SQLi**: All remediated per .NET best practice (anti-forgery tokens, model validation, parameterisation).
- **Comprehensive audit trail** for user actions.
- **Soft delete**: Entities marked deleted/disabled are hidden, not removed, and recoverable where appropriate.
- **CMS Integration**: Products, roles, and assignments synched with central CMS.

---

## 9. Additional Notes

- **Extensibility**: Designed to support additional roles, resource types, action types, and workflow/approval expansions.
- **Support**: For issues, contact the Compass dev team.
- **Last updated**: October 2025


