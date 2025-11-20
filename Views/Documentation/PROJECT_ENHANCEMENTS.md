# Project Feature Enhancements

## Overview

This document describes the enhancements made to the Project feature to support additional tracking fields and status updates.

## New Features

### 1. Status Updates

A new `ProjectStatusUpdate` table allows users to add narrative status updates to projects:

- **Fields:**
  - `Narrative` (free text)
  - `CreatedAt` (timestamp)
  - `CreatedByUserId` (user who created the update)
  - `UpdatedAt` (optional timestamp)
  - `UpdatedByUserId` (user who last updated, if applicable)

- **Permissions:**
  - All users can create and edit status updates
  - Only Admins can delete status updates

### 2. Senior Responsible Officer (SRO)

Multiple Azure/Entra users can be assigned as Senior Responsible Officers:

- Many-to-many relationship via `ProjectSeniorResponsibleOfficer` table
- Uses Azure user lookup (same as existing User model)

### 3. Phase Dates

Planned and actual dates for each project phase:

- **Discovery:**
  - `DiscoveryStartDatePlanned`, `DiscoveryStartDateActual`
  - `DiscoveryEndDatePlanned`, `DiscoveryEndDateActual`

- **Alpha:**
  - `AlphaStartDatePlanned`, `AlphaStartDateActual`
  - `AlphaEndDatePlanned`, `AlphaEndDateActual`

- **Private Beta:**
  - `PrivateBetaStartDatePlanned`, `PrivateBetaStartDateActual`
  - `PrivateBetaEndDatePlanned`, `PrivateBetaEndDateActual`

- **Public Beta:**
  - `PublicBetaStartDatePlanned`, `PublicBetaStartDateActual`
  - `PublicBetaEndDatePlanned`, `PublicBetaEndDateActual`

### 4. Activity Type

Single selection from admin-managed lookup table (`ActivityTypeLookup`).

### 5. Directorate

Multiple selection from admin-managed lookup table (`DirectorateLookup`), using checkboxes.

### 6. Budget Owner

Multiple selection from existing `BusinessAreaLookup` table.

### 7. Risk Appetite

Single selection from admin-managed lookup table (`RiskAppetiteLookup`).

### 8. Service Users

Free text field to describe who uses the service.

### 9. Internal/External

Two separate boolean checkboxes:
- `IsInternal`
- `IsExternal`

Both can be selected if applicable.

### 10. PMO Contact

Multiple Azure/Entra users can be assigned as PMO contacts via `ProjectPmoContact` table.

## Database Changes

### New Tables

1. `ProjectStatusUpdates`
2. `ProjectSeniorResponsibleOfficers`
3. `ProjectDirectorates`
4. `ProjectBudgetOwners`
5. `ProjectPmoContacts`
6. `ActivityTypeLookups`
7. `DirectorateLookups`
8. `RiskAppetiteLookups`

### Modified Tables

- `Projects` - Added new fields for phase dates, activity type, risk appetite, service users, and internal/external flags

## CSV Import Approach

To facilitate importing data from the CSV file (`BuRT - Compass Transfer list.csv`), the following approach is recommended:

### Option 1: Manual Import via Admin Interface (Recommended for initial import)

1. Create an admin page at `/Admin/ImportProjects` that:
   - Allows uploading a CSV file
   - Shows a preview of the data to be imported
   - Maps CSV columns to Project fields
   - Shows validation errors before import
   - Allows selective import (skip rows with errors)

2. **CSV Mapping Strategy:**
   - Map CSV columns to Project fields:
     - `Deliverable` → `Title`
     - `DeliverableID` → `HistoricBuRTId`
     - `CurrentStatusUpdate` → Create `ProjectStatusUpdate` entry
     - `SRO` → Lookup users and create `ProjectSeniorResponsibleOfficer` entries
     - `CurrentDeliveryPhase` → `Phase`
     - `DiscStartDate`, `AlphaStartDate`, etc. → Phase date fields
     - `ActivityType` → Lookup `ActivityTypeLookup` and set `ActivityTypeLookupId`
     - `Directorate` → Lookup `DirectorateLookup` and create `ProjectDirectorate` entries
     - `Group(BudgetOwner)` → Lookup `BusinessAreaLookup` and create `ProjectBudgetOwner` entries
     - `RiskAppetite` → Lookup `RiskAppetiteLookup` and set `RiskAppetiteLookupId`
     - `UsersOfService` → `ServiceUsers`
     - `ExternalInternal` → Parse and set `IsInternal`/`IsExternal`
     - `PMO Contact` → Lookup users and create `ProjectPmoContact` entries

3. **Import Service:**
   - Create `ProjectImportService` that:
     - Parses CSV file
     - Validates data
     - Maps CSV columns to Project fields
     - Handles lookups (users, activity types, etc.)
     - Creates projects and related entities
     - Returns import results with errors/warnings

### Option 2: Automated Import Script

Create a console application or PowerShell script that:
- Reads the CSV file
- Uses the same mapping logic as Option 1
- Can be run on-demand or scheduled
- Logs import results

### Implementation Steps for CSV Import

1. **Create Import Service:**
   ```csharp
   public class ProjectImportService
   {
       // Methods:
       // - ParseCsvFile(Stream csvStream)
       // - ValidateProjectData(CsvRow row)
       // - MapCsvRowToProject(CsvRow row)
       // - ImportProjects(List<CsvRow> rows)
   }
   ```

2. **Create Import Controller:**
   ```csharp
   public class ProjectImportController : Controller
   {
       // Actions:
       // - GET: Import (show upload form)
       // - POST: Import (handle file upload and import)
       // - GET: ImportPreview (show preview before import)
   }
   ```

3. **Create Import View:**
   - File upload form
   - Preview table showing mapped data
   - Validation error display
   - Import progress/status

4. **Handle Edge Cases:**
   - Missing or invalid data
   - Duplicate projects (by HistoricBuRTId)
   - Users not found in system
   - Lookup values not found (create or skip)
   - Large file imports (batch processing)

## Next Steps

1. **Run Migration:**
   ```bash
   dotnet ef migrations add AddProjectEnhancements --project compass
   dotnet ef database update --project compass
   ```

2. **Add Admin Settings:**
   - Add controller actions for ActivityType, Directorate, and RiskAppetite lookups
   - Create admin views for managing these lookups

3. **Update Project Views:**
   - Add form fields for all new properties in Create/Edit views
   - Add display of status updates in Project detail view
   - Add management UI for many-to-many relationships (SRO, Directorates, Budget Owners, PMO Contacts)

4. **Implement CSV Import:**
   - Create import service
   - Create import controller and views
   - Test with sample data

5. **Update Project Controller:**
   - Handle saving of new fields
   - Handle many-to-many relationships
   - Add status update CRUD operations

