# Planning and demand - Technical documentation

## Overview

The Planning and demand module in Compass manages the complete lifecycle of demand requests from initial submission through to project conversion. It provides a structured workflow for capturing, assessing, prioritising, and triaging requests for DDaT (Digital, Data and Technology) support from policy and delivery teams.

The system is feature-flagged and can be enabled/disabled via configuration (`FeatureFlags:EnableDemandManagement`). Access is controlled through Microsoft Entra ID group membership and leadership role assignments.

## Architecture

### Feature flag

The Planning and demand module is controlled by a feature flag:

```csharp
FeatureFlags:EnableDemandManagement = true/false
```

All controller actions check this flag via `IsDemandManagementEnabled()` and return `NotFound` if disabled.

### Access control

Access to Planning and demand is restricted to users in specific groups or with leadership roles:

**Allowed groups:**
- Demand Triage
- HOP (Head of Profession)
- Central Operations Admin
- Super Admin
- Admin

**Allowed leadership roles:**
- Deputy Director / SRO
- Director General
- C-Level
- Permanent Secretary

The `HasDemandManagementAccessAsync()` method checks both group membership and leadership roles via the `IPermissionService`.

## Data models

### DemandRequest

The core entity representing a demand request. Key properties:

**Identity and reference:**
- `Id` (int, auto-generated primary key)
- `ReferenceNumber` (string, max 50) - Unique reference (e.g., DR-2025-001)

**Applicant information (auto-captured from Microsoft login):**
- `ApplicantName` (string, max 255, required)
- `ApplicantEmail` (string, max 255, required)
- `BusinessArea` (string, max 100, required)
- `SeniorResponsibleOfficer` (string, max 255, required) - Must be G6+

**Portfolio context:**
- `HasPortfolioSupport` (bool?)
- `PortfolioName` (string, max 100)
- `PortfolioPrioritisation` (string, max 50) - Yes/No/Not sure

**Request details:**
- `ProposedTitle` (string, max 120, required) - 10-120 characters
- `OverviewAndBusinessNeed` (nvarchar(max), required)
- `PreviousResearchOrInsight` (nvarchar(max))
- `WillCreateOrChangeDigitalService` (string, max 50) - Yes/No/Unsure
- `DigitalServiceDetails` (nvarchar(max))

**Strategic alignment:**
- `IsManifestoOrStatutory` (string, max 50) - Yes/No/Unsure
- `ManifestoStatutoryDetails` (nvarchar(max))
- `SupportsOpportunityMissionPillar` (bool?)
- `OpportunityMissionPillars` (string, max 500) - Comma-separated
- `SupportsDdatStrategicTheme` (bool?)
- `DdatStrategicThemes` (string, max 500) - Comma-separated

**Impact and risk:**
- `ExpectedBenefits` (nvarchar(max), required)
- `RiskIfNotDelivered` (nvarchar(max), required)
- `PredictedRiskLevel` (string, max 20) - Calculated from linked risk types
- `RiskLevelOverride` (string, max 20) - Manual override
- `ImpactLevel` (string, max 20)
- `ImpactSummary` (nvarchar(max))

**Effective risk level calculation:**
The `EffectiveRiskLevel` property (not mapped to database) returns:
1. `RiskLevelOverride` if set
2. `PredictedRiskLevel` if available
3. `null` otherwise

**Funding and headcount:**
- `HasFunding` (bool?)
- `FundingAmount` (decimal(18,2)?)
- `FundingSource` (string, max 200)
- `FundingDuration` (string, max 200)
- `FundingNotes` (nvarchar(max))
- `HasHeadcount` (bool?)
- `NumberOfFTE` (int?)
- `RolesProvided` (string, max 500)
- `HeadcountDuration` (string, max 200)
- `HeadcountNotes` (nvarchar(max))

**Delivery:**
- `HasTargetDeliveryDate` (bool?)
- `TargetDeliveryDate` (DateTime?)
- `DeliveryTimescales` (nvarchar(max))

**Triage:**
- `IsSubmittedToTriage` (bool?)
- `TriageSubmittedAt` (DateTime?)
- `TriageMeetingId` (int?) - Foreign key to TriageMeeting
- `TriageNotes` (nvarchar(max))

**Conversion:**
- `ConvertedProjectId` (int?) - Foreign key to Project
- `ConvertedToProjectAt` (DateTime?)

**Status and workflow:**
- `Status` (string, max 50, required, default "Draft") - Draft/Submitted/Under Review/Approved/Deferred/Rejected
- `AssignedToEmail` (string, max 100)
- `AssignedToName` (string, max 255)
- `CurrentPhase` (string, max 50) - Explore/Triage/Delivery

**Other:**
- `DeclarationConfirmed` (bool)
- `IsSensitiveRequest` (bool)

**Timestamps:**
- `CreatedAt` (DateTime, required, default UtcNow)
- `UpdatedAt` (DateTime, required, default UtcNow)
- `SubmittedAt` (DateTime?)
- `ReviewedAt` (DateTime?)
- `DecisionAt` (DateTime?)
- `NextReviewDate` (DateTime?)

**Audit fields:**
- `ReviewedBy` (string, max 100)
- `ReviewNotes` (nvarchar(max))
- `DecisionNotes` (nvarchar(max))
- `StatusChangeReason` (nvarchar(max))

**Navigation properties:**
- `Contacts` (ICollection<DemandRequestContact>)
- `Prioritisation` (DemandRequestPrioritisation?)
- `Notes` (ICollection<DemandRequestNote>)
- `Assessments` (ICollection<DemandRequestAssessment>)
- `RiskTypeLinks` (ICollection<DemandRequestRiskType>)
- `SectionCompletions` (ICollection<DemandRequestSectionCompletion>)
- `TriageMeeting` (TriageMeeting?)
- `ConvertedProject` (Project?)

### DemandRequestContact

Represents points of contact for a demand request:

- `Id` (int, auto-generated)
- `DemandRequestId` (int, required, foreign key)
- `Name` (string, max 255, required)
- `Email` (string, max 255, required)
- `Role` (string, max 100)
- `CreatedAt` (DateTime, required, default UtcNow)

### DemandRequestPrioritisation

Stores prioritisation scoring for a demand request:

**Individual scores (1-5 scale):**
- `StatutoryManifestoScore` - Required by law or ministerial commitment
- `OpportunityMissionScore` - Supports departmental mission
- `DdatStrategicThemeScore` - Aligns to digital strategy
- `ScaleOfUsersScore` - Size and reach of user base
- `EvidenceOfUserNeedScore` - Quality of research and insight
- `RiskIfNotDeliveredScore` - Legal, financial, or reputational
- `TargetDeliveryUrgencyScore` - Immediacy of delivery date
- `FundingAvailableScore` - Funding confirmed vs none
- `HeadcountAvailableScore` - Resourcing confirmed
- `PortfolioFitScore` - Reuse or duplication
- `ExpectedBenefitsScore` - Potential measurable outcomes

**Weighted totals:**
- `StrategicAlignmentTotal` = (StatutoryManifestoScore + OpportunityMissionScore + DdatStrategicThemeScore) × 2
- `UserImpactTotal` = (ScaleOfUsersScore + EvidenceOfUserNeedScore) × 2
- `RiskUrgencyTotal` = (RiskIfNotDeliveredScore + TargetDeliveryUrgencyScore) × 2
- `FeasibilityTotal` = (FundingAvailableScore + HeadcountAvailableScore + PortfolioFitScore) × 1
- `ValueOutcomeTotal` = ExpectedBenefitsScore × 1

**Overall score:**
- `TotalPriorityScore` (int) - Calculated as: (rawTotal / 90.0) × 100, rounded to integer
- `PriorityTier` (string, max 50, default "Tier 4 – Low"):
  - Tier 1 – Critical: Score ≥ 80
  - Tier 2 – High: Score ≥ 60
  - Tier 3 – Medium: Score ≥ 40
  - Tier 4 – Low: Score < 40

**Metadata:**
- `ScoringNotes` (nvarchar(max))
- `ScoredBy` (string, max 100)
- `ScoredAt` (DateTime?)

### DemandRequestNote

Notes added to a demand request:

- `Id` (int, auto-generated)
- `DemandRequestId` (int, required, foreign key)
- `NoteText` (nvarchar(max), required)
- `CreatedByEmail` (string, max 100)
- `CreatedByName` (string, max 255)
- `CreatedAt` (DateTime, required, default UtcNow)

### DemandRequestAssessment

Assessment information for specific sections:

- `Id` (int, auto-generated)
- `DemandRequestId` (int, required, foreign key)
- `AssessmentType` (string, max 50) - ResearchAndEvidence, NeedsAssessment, Recommendations, PrioritisationAssessment, Outcome
- `AssessmentContent` (nvarchar(max))
- `AssessedByEmail` (string, max 100)
- `AssessedByName` (string, max 255)
- `CreatedAt` (DateTime, required, default UtcNow)
- `UpdatedAt` (DateTime, required, default UtcNow)

### DemandRequestSectionCompletion

Tracks completion status of sections:

- `Id` (int, auto-generated)
- `DemandRequestId` (int, required, foreign key)
- `SectionName` (string, max 50, required) - Overview, StrategicAlignment, ImpactAndRisk, FundingAndHeadcount, DeliveryPlanning, ResearchAndEvidence, NeedsAssessment, Recommendations, PrioritisationAssessment, Outcome
- `CompletionStatus` (string, max 20, default "ToDo") - ToDo, InProgress, Completed
- `CompletedByEmail` (string, max 100)
- `CompletedByName` (string, max 255)
- `CompletedAt` (DateTime?)
- `CompletionNotes` (nvarchar(max))
- `LatestErrorMessage` (nvarchar(max)) - Stores validation errors
- `CreatedAt` (DateTime, required, default UtcNow)
- `UpdatedAt` (DateTime, required, default UtcNow)

### DemandRequestRiskType

Junction table linking demand requests to risk types:

- `DemandRequestId` (int, required, foreign key)
- `RiskTypeId` (int, required, foreign key)
- `CreatedAt` (DateTime, required, default UtcNow)

### BusinessCase

Represents a business case:

- `Id` (int, auto-generated)
- `BusinessCaseId` (string, max 50, required) - Unique identifier
- `Title` (string, max 255, required)
- `Description` (nvarchar(max))
- `RequestorEmail` (string, max 255) - Entra user ID stored as email
- `RequestorName` (string, max 255)
- `Date` (DateTime?)
- `BusinessArea` (string, max 100)
- `CreatedAt` (DateTime, required, default UtcNow)
- `UpdatedAt` (DateTime, required, default UtcNow)

**Navigation properties:**
- `DdtFeedbacks` (ICollection<BusinessCaseDdtFeedback>)
- `Reviewers` (ICollection<BusinessCaseReviewer>)
- `BusinessCaseProjects` (ICollection<BusinessCaseProject>)
- `BusinessCaseProducts` (ICollection<BusinessCaseProduct>)

### TriageMeeting

Represents a triage meeting:

- `Id` (int, auto-generated)
- `Title` (string, max 150, required)
- `StartAt` (DateTime, required)
- `EndAt` (DateTime, required)
- `Description` (string, max 500)
- `Location` (string, max 255)
- `ChairName` (string, max 255)
- `ChairEmail` (string, max 100)
- `ChairObjectId` (string, max 255)
- `IsActive` (bool, default true)
- `CreatedAt` (DateTime, required, default UtcNow)
- `UpdatedAt` (DateTime, required, default UtcNow)

**Navigation properties:**
- `DemandRequests` (ICollection<DemandRequest>)

## Workflow and status management

### Status values

The system uses the following status values:

- **Draft** - Request is being created/edited
- **Submitted** - Request has been submitted for review
- **Under Review** - Request is being reviewed
- **Approved** - Request has been approved
- **Deferred** - Request has been deferred
- **Rejected** - Request has been rejected

Status values are stored in the `DemandRequest.Status` field and can also be managed through lookup tables (`DemandRequestStatus`).

### Workflow stages

The system defines workflow stages that map to statuses:

1. **Draft** - Initial creation
2. **Submitted** - Submitted for review
3. **Explore** - Exploration phase
4. **Prioritisation** - Being prioritised/scored
5. **Triage** - In triage meeting
6. **Delivery** - Approved and in delivery
7. **Run** - In run/maintenance phase
8. **Deferred** - Deferred
9. **Rejected** - Rejected

The `DetermineWorkflowStageKey()` method maps status values to stage keys.

### Phase tracking

Requests can be in different phases:

- **Explore** - Exploration and discovery
- **Triage** - In triage process
- **Delivery** - Approved and being delivered

Stored in `DemandRequest.CurrentPhase`.

## Request creation and management

### Creating a request

**Endpoint:** `POST /DemandManagement/CreateRequest`

**Access:** All authenticated users (no group requirement)

**Process:**
1. User fills out the create request form
2. System auto-captures applicant information from Microsoft login
3. Validates required fields
4. Generates reference number (format: DR-YYYY-NNN)
5. Creates `DemandRequest` with status "Draft"
6. Creates associated `DemandRequestContact` records
7. Initialises section completions

**Reference number generation:**
- Format: `DR-{YYYY}-{NNN}` where YYYY is current year and NNN is sequential number
- Generated by finding the highest existing reference number for the current year and incrementing

### Editing a request

**Endpoint:** `POST /DemandManagement/Edit`

**Access:** Request creator or users with Demand Management access

**Process:**
1. Loads existing request with all related entities
2. Validates user has permission to edit
3. Updates fields from model
4. Updates contacts (adds new, removes deleted, updates existing)
5. Updates `UpdatedAt` timestamp
6. Saves changes

### Section-based editing

The system uses a section-based editing approach where fields are updated individually via AJAX.

**Endpoint:** `POST /DemandManagement/UpdateSectionField`

**Parameters:**
- `id` (int) - Demand request ID
- `sectionKey` (string) - Section identifier
- `fieldKey` (string) - Field identifier
- Additional parameters based on field type

**Sections:**
- `Overview` - Basic request information
- `StrategicAlignment` - Strategic alignment details
- `ImpactAndRisk` - Impact and risk assessment
- `FundingAndHeadcount` - Funding and resource requirements
- `DeliveryPlanning` - Delivery timescales

**Field update logic:**
Each section/field combination has specific validation and update logic in `TryUpdateSectionField()`:

1. Validates field value based on type (string, bool, int, decimal, DateTime, array)
2. Checks if value has changed
3. Updates field value
4. Triggers related calculations (e.g., risk level recalculation)
5. Returns success/error response

**Example field updates:**

**StrategicAlignment section:**
- `IsManifestoOrStatutory` - Updates boolean and clears details if set to false
- `ManifestoStatutoryDetails` - Updates text field
- `SupportsOpportunityMissionPillar` - Updates boolean and manages comma-separated pillar list
- `SupportsDdatStrategicTheme` - Updates boolean and manages comma-separated theme list

**ImpactAndRisk section:**
- `OverviewAndBusinessNeed` - Updates required text field
- `ExpectedBenefits` - Updates required text field
- `RiskIfNotDelivered` - Updates required text field and triggers risk level recalculation
- `RiskTypes` - Manages many-to-many relationship with RiskType entities
- `RiskLevelOverride` - Allows manual override of calculated risk level
- `ImpactDetails` - Updates impact level and summary

**FundingAndHeadcount section:**
- `HasFunding` - Updates boolean and clears funding fields if false
- `FundingAmount` - Updates decimal value
- `FundingSource` - Updates text field
- `FundingDuration` - Updates text field
- `FundingNotes` - Updates text field
- `HasHeadcount` - Updates boolean and clears headcount fields if false
- `NumberOfFTE` - Updates integer value
- `RolesProvided` - Updates text field
- `HeadcountDuration` - Updates text field
- `HeadcountNotes` - Updates text field

**DeliveryPlanning section:**
- `HasTargetDeliveryDate` - Updates boolean and manages date field
- `TargetDeliveryDate` - Updates DateTime value
- `DeliveryTimescales` - Updates text field

### Section completion

**Endpoint:** `POST /DemandManagement/CompleteSection`

**Process:**
1. Validates section completion eligibility
2. Checks required fields are populated
3. Creates or updates `DemandRequestSectionCompletion` record
4. Sets `CompletionStatus` to "Completed"
5. Records completion metadata (who, when, notes)
6. Updates `UpdatedAt` timestamp

**Section completion eligibility:**
The system validates that required fields are populated before allowing completion. Missing fields are tracked in `SectionMissingFields` dictionary.

**Section status tracking:**
- `ToDo` - Section not started
- `InProgress` - Section partially completed
- `Completed` - Section fully completed

### Risk level calculation

The system automatically calculates predicted risk level based on linked risk types:

**Process:**
1. Retrieves all linked `RiskType` entities via `DemandRequestRiskType` junction table
2. Extracts severity values from linked risk types
3. Normalises severity values (High/Medium/Low)
4. Selects highest severity level
5. If no risk types linked but `RiskIfNotDelivered` is populated, defaults to "Medium"
6. Stores in `PredictedRiskLevel`

**Risk level priority:**
- Low = 0
- Medium = 1
- High = 2

The `CalculatePredictedRiskLevel()` method implements this logic.

**Risk level override:**
Users can manually override the calculated risk level via `RiskLevelOverride`. The `EffectiveRiskLevel` property returns the override if set, otherwise the predicted level.

## Prioritisation and scoring

### Scoring a request

**Endpoint:** `POST /DemandManagement/ScoreRequest`

**Access:** Users with Demand Management access

**Process:**
1. Loads request with existing prioritisation data
2. Validates scoring model (all scores must be 1-5)
3. Calculates weighted totals:
   - Strategic Alignment: (StatutoryManifesto + OpportunityMission + DdatStrategicTheme) × 2
   - User Impact: (ScaleOfUsers + EvidenceOfUserNeed) × 2
   - Risk & Urgency: (RiskIfNotDelivered + TargetDeliveryUrgency) × 2
   - Feasibility: (FundingAvailable + HeadcountAvailable + PortfolioFit) × 1
   - Value & Outcome: ExpectedBenefits × 1
4. Calculates total priority score: (rawTotal / 90.0) × 100, rounded
5. Assigns priority tier based on score:
   - Tier 1 – Critical: ≥ 80
   - Tier 2 – High: ≥ 60
   - Tier 3 – Medium: ≥ 40
   - Tier 4 – Low: < 40
6. Creates or updates `DemandRequestPrioritisation` record
7. Updates request status to "Under Review" if currently "Submitted"
8. Marks "PrioritisationAssessment" section as completed

**Score validation:**
All individual scores must be between 1 and 5. The `ValidatePrioritisationModel()` method checks this.

### Prioritisation view

**Endpoint:** `GET /DemandManagement/Prioritisation`

**Access:** Users with Demand Management access

**Features:**
- Lists all requests with prioritisation data
- Filterable by portfolio and search term
- Displays priority tier, score, and status
- Shows requests without prioritisation scores

## Triage process

### Triage meetings

**Endpoint:** `GET /DemandManagement/ManageMeetings`

**Access:** Users with Demand Management access

**Features:**
- Create, edit, and manage triage meetings
- Set meeting title, date/time, location, description
- Assign meeting chair
- Mark meetings as active/inactive

**Endpoint:** `POST /DemandManagement/SaveTriageMeeting`

**Process:**
1. Validates meeting data
2. Creates or updates `TriageMeeting` record
3. Sets `IsActive` flag
4. Saves changes

### Submitting to triage

**Endpoint:** `POST /DemandManagement/SubmitToTriage`

**Access:** Request creator or users with Demand Management access

**Process:**
1. Validates request exists
2. Validates triage meeting exists and is active
3. Sets `IsSubmittedToTriage` to true
4. Sets `TriageMeetingId`
5. Sets `TriageSubmittedAt` to current timestamp
6. Optionally sets `TriageNotes`
7. Updates request status if needed
8. Saves changes

### Removing from triage

**Endpoint:** `POST /DemandManagement/RemoveFromTriage`

**Access:** Users with Demand Management access

**Process:**
1. Validates request exists
2. Clears triage-related fields:
   - `IsSubmittedToTriage` = false
   - `TriageMeetingId` = null
   - `TriageSubmittedAt` = null
   - `TriageNotes` = null
3. Saves changes

### Triage view

**Endpoint:** `GET /DemandManagement/Triage`

**Access:** Users with Demand Management access

**Features:**
- Displays triage meetings grouped by month
- Shows requests submitted to each meeting
- Shows requests awaiting scheduling
- Allows navigation between months
- Displays assessment information for each request

## Business cases

### Creating a business case

**Endpoint:** `POST /DemandManagement/CreateBusinessCase`

**Access:** Users with Demand Management access

**Process:**
1. Validates business case data
2. Generates unique `BusinessCaseId`
3. Creates `BusinessCase` record
4. Sets requestor information from current user
5. Saves changes

### Business case details

**Endpoint:** `GET /DemandManagement/BusinessCaseDetails`

**Access:** Users with Demand Management access

**Features:**
- Displays business case information
- Shows linked projects
- Shows linked products (from FIPS CMS)
- Displays DDT feedback
- Shows reviewers

### Linking projects

**Endpoint:** `POST /DemandManagement/LinkProject`

**Process:**
1. Validates business case and project exist
2. Checks if link already exists
3. Creates `BusinessCaseProject` junction record
4. Saves changes

**Endpoint:** `POST /DemandManagement/UnlinkProject`

**Process:**
1. Validates business case project link exists
2. Removes `BusinessCaseProject` record
3. Saves changes

### DDT feedback

**Endpoint:** `POST /DemandManagement/AddDdtFeedback`

**Process:**
1. Validates business case exists
2. Validates feedback text (max 4000 characters)
3. Creates `BusinessCaseDdtFeedback` record
4. Sets feedback provider from current user
5. Saves changes

## Assessment and notes

### Adding notes

**Endpoint:** `POST /DemandManagement/AddNote`

**Access:** Request creator or users with Demand Management access

**Process:**
1. Validates request exists
2. Validates note text is not empty
3. Creates `DemandRequestNote` record
4. Sets creator information from current user
5. Saves changes

### Updating assessments

**Endpoint:** `POST /DemandManagement/UpdateAssessment`

**Access:** Users with Demand Management access

**Process:**
1. Validates request exists
2. Validates assessment type
3. Creates or updates `DemandRequestAssessment` record
4. Sets assessor information from current user
5. Updates `UpdatedAt` timestamp
6. Saves changes

**Assessment types:**
- `ResearchAndEvidence`
- `NeedsAssessment`
- `Recommendations`
- `PrioritisationAssessment`
- `Outcome`

## Status management

### Updating status

**Endpoint:** `POST /DemandManagement/UpdateStatus`

**Access:** Users with Demand Management access

**Process:**
1. Validates request exists
2. Validates new status value
3. Updates `Status` field
4. Sets `StatusChangeReason`
5. Sets `ReviewedBy` to current user
6. Sets `ReviewedAt` to current timestamp
7. Optionally sets `NextReviewDate`
8. Updates `DecisionAt` if status is Approved/Deferred/Rejected
9. Updates `DecisionNotes` if provided
10. Saves changes

### Assignment management

**Endpoint:** `POST /DemandManagement/UpdateAssignment`

**Access:** Users with Demand Management access

**Process:**
1. Validates request exists
2. If `clearAssignment` is true:
   - Sets `AssignedToEmail` = null
   - Sets `AssignedToName` = null
3. Otherwise:
   - Sets `AssignedToEmail` from parameter
   - Sets `AssignedToName` from parameter
4. Saves changes

## Conversion to projects

### Converting a request

**Endpoint:** `POST /DemandManagement/ConvertToProject`

**Access:** Users with Demand Management access

**Process:**
1. Validates request exists
2. Validates request is in approved state
3. Creates new `Project` record:
   - Sets project name from `ProposedTitle`
   - Sets description from `OverviewAndBusinessNeed`
   - Sets status to "Active"
   - Sets other project fields from request data
4. Links request to project:
   - Sets `ConvertedProjectId`
   - Sets `ConvertedToProjectAt` to current timestamp
5. Updates request status if needed
6. Saves changes

## Reporting

### Reporting dashboard

**Endpoint:** `GET /DemandManagement/Reporting`

**Access:** Users with Demand Management access

**Statistics displayed:**
- Total requests
- Requests by status (Draft, Submitted, Under Review, Approved, Deferred, Rejected)
- Requests by priority tier (Tier 1-4)
- Portfolio breakdown
- Average time to first response (in days)

**Data aggregation:**
- Queries all `DemandRequest` records
- Includes `Prioritisation` data
- Calculates statistics using LINQ aggregations
- Groups by portfolio for breakdown

## CSV import

### Import process

**Endpoint:** `GET /DemandManagement/ImportCsv`

**Access:** Users with Demand Management access

**Process:**
1. User uploads CSV file
2. System parses CSV using CsvHelper
3. Displays field mapping interface
4. User maps CSV columns to demand request fields
5. System validates mapped data
6. Creates `DemandRequest` records for each row
7. Displays import results

**Endpoint:** `POST /DemandManagement/UploadCsv`

**Process:**
1. Validates file is CSV format
2. Reads CSV file into memory
3. Extracts column headers
4. Stores mapping in session
5. Redirects to mapping page

**Endpoint:** `POST /DemandManagement/ProcessCsvImport`

**Process:**
1. Retrieves field mappings from form
2. Reads CSV from session
3. For each row:
   - Creates `DemandRequest` model
   - Maps fields according to mapping
   - Validates data
   - Creates record if valid
4. Collects errors and successes
5. Displays import results

## View models

### DemandWorkflowViewModel

Main view model for demand request workflow display:

- `Request` (DemandRequest) - The request being viewed
- `Stages` (IEnumerable<DemandWorkflowStageViewModel>) - Workflow stages
- `Tasks` (IEnumerable<DemandWorkflowTaskViewModel>) - Workflow tasks
- `Activity` (IEnumerable<DemandWorkflowActivityViewModel>) - Activity timeline
- `CurrentStageKey` (string) - Current stage identifier
- `CurrentStageName` (string) - Current stage display name
- `CurrentStageSummary` (string) - Current stage description
- `CompletedTaskCount` (int) - Number of completed tasks
- `TotalTaskCount` (int) - Total number of tasks
- `IsDocumentView` (bool) - Whether in document view mode
- `ActiveSectionKey` (string) - Currently active section
- `SectionCompletionEligibility` (IDictionary<string, bool>) - Section completion eligibility
- `SectionMissingFields` (IDictionary<string, IReadOnlyCollection<string>>) - Missing required fields per section
- `SectionStatusMessages` (IDictionary<string, string?>) - Status messages per section
- `BusinessAreas` (IReadOnlyCollection<string>) - Available business areas
- `MissionPillars` (IReadOnlyCollection<string>) - Available mission pillars
- `StrategicObjectives` (IReadOnlyCollection<string>) - Available strategic objectives
- `RiskTypes` (IReadOnlyCollection<RiskType>) - Available risk types
- `TriageMeetings` (IReadOnlyCollection<TriageMeeting>) - Available triage meetings

### DemandWorkflowStageViewModel

Represents a workflow stage:

- `Key` (string) - Stage identifier
- `Name` (string) - Stage display name
- `Summary` (string) - Stage description
- `Status` (string) - Stage status (upcoming/current/completed)
- `Tasks` (IReadOnlyCollection<DemandWorkflowTaskViewModel>) - Tasks in this stage

### DemandWorkflowTaskViewModel

Represents a workflow task:

- `Key` (string) - Task identifier (matches section name)
- `Title` (string) - Task display name
- `Group` (string) - Task group (Request/Assessment)
- `Description` (string?) - Task description
- `Status` (string) - Task status (ToDo/InProgress/Completed)
- `AssignedTo` (string?) - Person assigned to task
- `DueDate` (DateTime?) - Task due date
- `Url` (string?) - Task URL
- `IsCurrent` (bool) - Whether this is the current task

### DemandWorkflowActivityViewModel

Represents an activity in the timeline:

- `Timestamp` (DateTime) - When activity occurred
- `Title` (string) - Activity title
- `Description` (string) - Activity description
- `Actor` (string?) - Who performed the activity
- `Type` (string) - Activity type (system/status/note)
- `Icon` (string) - Font Awesome icon class

## Technical implementation details

### Database context

The `CompassDbContext` includes the following DbSets:

- `DemandRequests` (DbSet<DemandRequest>)
- `DemandRequestContacts` (DbSet<DemandRequestContact>)
- `DemandRequestPrioritisations` (DbSet<DemandRequestPrioritisation>)
- `DemandRequestNotes` (DbSet<DemandRequestNote>)
- `DemandRequestAssessments` (DbSet<DemandRequestAssessment>)
- `DemandRequestSectionCompletions` (DbSet<DemandRequestSectionCompletion>)
- `DemandRequestRiskTypes` (DbSet<DemandRequestRiskType>)
- `BusinessCases` (DbSet<BusinessCase>)
- `BusinessCaseDdtFeedbacks` (DbSet<BusinessCaseDdtFeedback>)
- `BusinessCaseReviewers` (DbSet<BusinessCaseReviewer>)
- `BusinessCaseProjects` (DbSet<BusinessCaseProject>)
- `BusinessCaseProducts` (DbSet<BusinessCaseProduct>)
- `TriageMeetings` (DbSet<TriageMeeting>)

### Helper methods

**NormaliseTriLevel(string? value):**
Normalises risk/impact level values to "High", "Medium", or "Low". Accepts various formats:
- "High"/"H"/"3" → "High"
- "Medium"/"Med"/"M"/"2" → "Medium"
- "Low"/"L"/"1" → "Low"

**CalculatePredictedRiskLevel(DemandRequest request):**
Calculates predicted risk level from linked risk types. Returns highest severity level, or "Medium" if no risk types but risk description exists.

**RefreshPredictedRiskLevel(DemandRequest request):**
Updates the `PredictedRiskLevel` field by calling `CalculatePredictedRiskLevel()`.

**EnsureSectionCompletion(DemandRequest request, string sectionName):**
Ensures a `DemandRequestSectionCompletion` record exists for the given section. Creates if missing, returns existing if present.

**BuildWorkflowTasks(DemandRequest request, string activeSection):**
Builds the list of workflow tasks from section completions. Creates tasks for:
- Strategic alignment
- Impact & risk
- Funding & headcount
- Delivery planning
- Research & evidence
- Needs assessment
- Recommendations
- Notes
- Prioritisation assessment
- Outcome

**BuildWorkflowStages(DemandRequest request, IEnumerable<DemandWorkflowTaskViewModel> tasks):**
Builds workflow stages from tasks. Groups tasks and determines stage status (upcoming/current/completed).

**BuildWorkflowActivity(DemandRequest request):**
Builds activity timeline from request history, status changes, and notes.

**DetermineWorkflowStageKey(string? status):**
Maps status value to workflow stage key.

### Validation

**Field validation:**
Each field update includes validation:
- Required fields must not be empty
- String fields have maximum length constraints
- Numeric fields must be valid numbers
- Date fields must be valid dates
- Boolean fields must be valid boolean values

**Section completion validation:**
Before marking a section as complete, the system:
1. Checks all required fields are populated
2. Validates field values meet constraints
3. Records missing fields in `SectionMissingFields`
4. Sets `LatestErrorMessage` if validation fails

**Prioritisation validation:**
All individual scores must be between 1 and 5. The `ValidatePrioritisationModel()` method validates this.

### Error handling

**Model state errors:**
Validation errors are captured in `ModelState` and displayed to users.

**Section errors:**
Section-specific errors are stored in `DemandRequestSectionCompletion.LatestErrorMessage` and displayed in the UI.

**Exception handling:**
Controller actions use try-catch blocks to handle exceptions. Errors are logged using `ILogger` and user-friendly error messages are displayed.

### Performance considerations

**Eager loading:**
The system uses Entity Framework's `Include()` to eagerly load related entities, reducing database round trips:
- `Include(dr => dr.Prioritisation)`
- `Include(dr => dr.Contacts)`
- `Include(dr => dr.SectionCompletions)`
- `Include(dr => dr.RiskTypeLinks)`

**AsNoTracking:**
Read-only queries use `AsNoTracking()` to improve performance:
- `_context.RiskTypes.AsNoTracking()`

**Query optimisation:**
LINQ queries are structured to execute efficiently on the database, avoiding in-memory operations where possible.

## User interface

### Navigation

The Planning and demand module is accessible from the main navigation menu under "Planning and demand". The navigation includes:

- Overview
- Requests
- Business cases
- Triage
- Prioritisation
- Reporting

### Views

**Overview (`/DemandManagement/Overview`):**
High-level dashboard showing demand management activities.

**Requests (`/DemandManagement/Requests`):**
List of all demand requests with filtering by status, portfolio, and search.

**Details (`/DemandManagement/Details`):**
Detailed view of a single demand request showing all sections and workflow.

**Section (`/DemandManagement/Section`):**
Section-specific editing view for a demand request.

**Create Request (`/DemandManagement/CreateRequest`):**
Form for creating a new demand request.

**Edit (`/DemandManagement/Edit`):**
Form for editing an existing demand request.

**Triage (`/DemandManagement/Triage`):**
Triage meeting management and request review.

**Prioritisation (`/DemandManagement/Prioritisation`):**
List of requests for prioritisation and scoring.

**Score Request (`/DemandManagement/ScoreRequest`):**
Form for scoring a demand request.

**Business Cases (`/DemandManagement/BusinessCases`):**
List of all business cases.

**Business Case Details (`/DemandManagement/BusinessCaseDetails`):**
Detailed view of a business case.

**Reporting (`/DemandManagement/Reporting`):**
Reporting dashboard with statistics.

### AJAX updates

The system uses AJAX for section-based field updates to provide a responsive user experience without full page reloads.

**Update flow:**
1. User edits field in UI
2. JavaScript sends AJAX POST to `/DemandManagement/UpdateSectionField`
3. Server validates and updates field
4. Server returns JSON response
5. JavaScript updates UI with success/error message

## Integration points

### Microsoft Entra ID

The system integrates with Microsoft Entra ID for:
- User authentication
- User information (name, email)
- Group membership checking
- Leadership role checking

### Permission service

The `IPermissionService` is used to:
- Check group membership
- Check leadership roles
- Check super admin status

### Graph service

The `IGraphService` is used to:
- Look up user information
- Resolve user object IDs to names/emails

### FIPS CMS

Business cases can link to products stored in the FIPS CMS via `ProductFipsId`.

## Configuration

### Feature flags

```json
{
  "FeatureFlags": {
    "EnableDemandManagement": true
  }
}
```

### Connection strings

The system uses the standard Compass database connection string configured in `appsettings.json`.

## Security considerations

### Authentication

All Planning and demand endpoints require authentication via `[Authorize]` attribute.

### Authorisation

Access to Planning and demand is restricted to:
- Users in specific Microsoft Entra ID groups
- Users with specific leadership roles
- Super admins

### Data protection

- Sensitive requests can be marked with `IsSensitiveRequest` flag
- User information is stored securely
- Audit trails are maintained for all changes

## Future enhancements

Potential areas for future development:

1. Email notifications for status changes
2. Integration with project management tools
3. Advanced reporting and analytics
4. Workflow customisation
5. Bulk operations
6. API endpoints for external integration
7. Document generation (PDF export)
8. Calendar integration for triage meetings
