# Accessibility issues and statement service

## Overview

The accessibility issues and statement service helps product teams capture accessibility defects, track remediation commitments, and publish legally compliant accessibility statements. It combines structured issue management inside Compass with automation for the Department for Education (DfE) central accessibility statement platform.

- **Product scope:** Any product enrolled through the accessibility admin area (`ProductAccessibility` records).
- **Primary users:** Accessibility administrators (Super admin/Admin roles) and product accessibility contacts.
- **External dependency:** DfE accessibility statements service (`https://accessibility-statements.education.gov.uk`).

## Core components

### Accessibility issue registry

- Issues are stored on the `AccessibilityIssue` entity (`Compass/Models/AccessibilityIssue.cs`).
- Each issue belongs to a single `ProductAccessibility` record via `ProductAccessibilityId`.
- Issues can be raised against WCAG criteria, best practice breaches, or usability findings.
- Status pipeline supports `open`, `in_progress`, `resolved`, and `wont_fix`.
- Comments (`IssueComment`), history (`IssueHistory`), WCAG links (`IssueWcagCriterion`), and retest requests (`AccessibilityRetestRequest`) provide full traceability.

### Statement management

- `ProductAccessibility` keeps the current statement URL, verification status, and SLA settings.
- Automatic verification checks confirm that the public product URL links to the generated statement URL (`/s/{FipsId}`).
- Administrators can override verification manually when automation cannot access gated environments.
- Statement readiness drives dashboard flags inside the accessibility admin panel.

## Data model reference

### `AccessibilityIssue`

| Property | Description |
| --- | --- |
| `Id` | Primary key. |
| `ProductAccessibilityId` | Foreign key back to the enrolled product. |
| `IssueType` | `WCAG`, `Best Practice`, or `Usability`. Defaults to `WCAG`. |
| `IssueTitle` | Free-text title for best practice and usability issues. |
| `WcagCriteria` / `WcagLevel` / `WcagVersion` | Deprecated string fields retained for backwards compatibility. |
| `IdentifiedDate` | When the issue was detected (required). |
| `IdentifiedVia` | Source of discovery (Audit, Testing, Feedback, Complaint, GDS). |
| `IssueDescription` | Detailed narrative for the issue. |
| `IsResolving` | Indicates whether remediation is planned. |
| `PlannedResolutionDate` | Target fix date when `IsResolving` is true. |
| `NonResolutionReason` | Mandatory justification when `IsResolving` is false. |
| `ActualResolutionDate` | Completion timestamp once resolved. |
| `ResolutionNotes` / `VerificationNotes` | Remediation and verification evidence. |
| `Status` | Current workflow state (`open`, `in_progress`, `resolved`, `wont_fix`). |
| `CreatedAt` / `CreatedBy` | Audit trail for issue creation. |
| `UpdatedAt` / `UpdatedBy` | Audit trail for the latest change. |
| `IsDeleted` | Soft delete flag; issues are never hard-deleted. |

### Related entities

- `ProductAccessibility` – Enrolment meta-data for the product (statement URL, SLA, contacts).
- `IssueComment` – Rich-text commentary with author tracking.
- `IssueHistory` – Field-level change log for the issue lifecycle.
- `IssueWcagCriterion` – Join table to WCAG catalogue records for structured reporting.
- `AccessibilityRetestRequest` – Captures retest cycles when fixes need validation.

## Issue lifecycle

1. **Capture:** Admin users raise issues from the accessibility workspace (`/Apps/Accessibility`). WCAG issues can be linked to one or more criteria using the GOV.UK WCAG taxonomy.
2. **Triage:** Ensure `IdentifiedVia`, `IssueDescription`, and remediation intent fields are complete. Attach planned dates when remediation is underway.
3. **Remediate:** Update `Status` to `in_progress` when work begins. Add comments as the team works on the defect and update planned dates if schedules slip.
4. **Verify:** Record `ActualResolutionDate` and populate `ResolutionNotes` once fixes ship. Add verification evidence and move the issue to `resolved`.
5. **Exception handling:** For issues that will not be fixed, set `IsResolving` to `false`, provide `NonResolutionReason`, and move the `Status` to `wont_fix`. The decision is surfaced in statement outputs.

Compass keeps all historical updates so that accessibility audits and FOI requests can be evidenced quickly.

## Statement workflow

1. **Enrolment:** Navigate to **Admin → Accessibility service** and enrol a product. The system creates a `ProductAccessibility` entry seeded with defaults and contacts from the CMS.
2. **Configure statement settings:** Provide SLA response days, complaint contact email, WCAG target version/level, and (optionally) an existing statement URL.
3. **Generate a hosted statement:** Compass prepopulates content using templates under `compass/docs/*.md`. The public statement URL format is `https://accessibility-statements.education.gov.uk/s/{FipsId}`.
4. **Verify installation:** Supply the live product URL. Automatic checks scan the HTML to confirm the hosted statement link is present. If automation cannot confirm (for example behind authentication), admins can mark the statement as manually verified.
5. **Ongoing monitoring:** The admin dashboard highlights products with missing statements, failing verification, overdue remediation (`PlannedResolutionDate < Today`), or unresolved SLA breaches.

## Roles and permissions

- Only Super admin and Admin roles may access the accessibility service controller (`Admin/AccessibilityServiceController`).
- Product accessibility contacts (from CMS) can view issues and statements but cannot amend enrolment settings unless granted admin rights.
- Audit events and verification actions are logged with the authenticated user identity for accountability.

## Reporting and integrations

- The business area and performance reporting suites surface accessibility metrics (enrolment counts, open issue totals, compliance rate).
- Accessibility dashboards reuse the same `AccessibilityIssue` and `ProductAccessibility` data, ensuring a single source of truth.
- Integration with the Products API keeps product metadata (phase, business area, contacts) synchronised daily.

## Operational considerations

- **Data hygiene:** Encourage teams to keep `IssueDescription`, remediation notes, and verification evidence current. Dashboards flag stale issues automatically.
- **Templates:** Update the markdown statement templates (`compass/docs/statement.md` and variants) when legislation or guidance changes.
- **Backups:** Accessibility data sits inside the primary Compass database and is included in nightly Azure SQL backups.
- **Escalation:** Use comments and history entries to demonstrate follow-up when remediation deadlines slip. Link issues to delivery risks where appropriate.

## Related documentation

- `compass/Views/Apps/Accessibility/_Issues.cshtml` – UI implementation for managing issues.
- `compass/Controllers/Admin/AccessibilityServiceController.cs` – Admin workflows for enrolment, verification, and monitoring.
- `compass/Views/Apps/Accessibility/_Statement.cshtml` – Statement verification interface.
- `compass/docs/statement*.md` – Published statement templates.


