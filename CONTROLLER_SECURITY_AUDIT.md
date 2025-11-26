# Controller Security Audit

This document provides a comprehensive overview of all controllers and their authorization requirements.

## Authorization Patterns Used

1. **`[Authorize]`** - Requires authenticated user (any logged-in user)
2. **`[Authorize(Roles = "Admin,SuperAdmin")]`** - Requires Admin or SuperAdmin role (legacy - uses UserRole enum)
3. **`[RequireSuperAdmin]`** - Requires membership in "Super admin" group
4. **`[RequireCentralOpsAdmin]`** - Requires membership in "Super admin" OR "Central Operations Admin" group
5. **`[RequireApiPermission]`** - Requires API token with specific resource/operation permission
6. **`[AllowAnonymous]`** - No authorization required
7. **Manual checks** - Some controllers use `IsAuthorizedAsync()` helper methods

---

## Controllers by Authorization Type

### 1. Super Admin Only (API Management)

**`AdminController`** - `/Admin/*`
- **Class-level**: `[Authorize]`
- **API Token Management** (all methods require `[RequireSuperAdmin]`):
  - `ApiTokens()` - GET `/Admin/ApiTokens`
  - `CreateApiToken()` - GET `/Admin/CreateApiToken`
  - `CreateApiToken()` - POST `/Admin/CreateApiToken`
  - `ConfigurePermissions()` - GET `/Admin/ConfigurePermissions/{id}`
  - `SavePermissions()` - POST `/Admin/SavePermissions`
  - `RecycleApiToken()` - POST `/Admin/RecycleApiToken`
  - `ToggleApiToken()` - POST `/Admin/ToggleApiToken`
  - `DeleteApiToken()` - POST `/Admin/DeleteApiToken`
  - `ApiLogs()` - GET `/Admin/ApiLogs`
- **All other methods**: Only `[Authorize]` (authenticated users)

**Required Group**: "Super admin"

---

### 2. Central Operations Admin (or Super Admin)

**`CentralOpsController`** - `/CentralOps/*`
- **Class-level**: `[Authorize]` + `[RequireCentralOpsAdmin]`
- **All methods** require membership in "Super admin" OR "Central Operations Admin" group
- **Exception**: `AccessDenied()` - `[AllowAnonymous]`

**Required Groups**: "Super admin" OR "Central Operations Admin"

---

**`Admin/GroupManagementController`** - `/Admin/UserManagement/*`
- **Class-level**: `[Authorize]`
- **All methods** use `IsSuperAdminOrAdminAsync()` helper:
  - Checks "Super admin" OR "Central Operations Admin" group
- Methods:
  - `Index()` - GET `/Admin/UserManagement`
  - `Groups()` - GET `/Admin/UserManagement/Groups`
  - `CreateGroup()` - GET/POST `/Admin/UserManagement/CreateGroup`
  - `EditGroup()` - GET/POST `/Admin/UserManagement/EditGroup/{id}`
  - `DeleteGroup()` - POST `/Admin/UserManagement/DeleteGroup/{id}`
  - `ManageUsers()` - GET/POST `/Admin/UserManagement/ManageUsers/{id}`
  - `UserPermissions()` - GET `/Admin/UserManagement/UserPermissions/{id}`
  - `CreateFeature()` - GET/POST `/Admin/UserManagement/CreateFeature`
  - `EditFeature()` - GET/POST `/Admin/UserManagement/EditFeature/{id}`
  - `DeleteFeature()` - POST `/Admin/UserManagement/DeleteFeature/{id}`
  - `AddUserToGroup()` - POST `/Admin/UserManagement/AddUserToGroup`
  - `RemoveUserFromGroup()` - POST `/Admin/UserManagement/RemoveUserFromGroup`
  - `UpdateGroupPermissions()` - POST `/Admin/UserManagement/UpdateGroupPermissions`

**Required Groups**: "Super admin" OR "Central Operations Admin"

---

**`Admin/PerformanceReportingManagementController`** - `/Admin/PerformanceReportingManagement/*`
- **Class-level**: `[Authorize]`
- **All methods** use `IsAuthorizedAsync()` helper:
  - Checks "Super admin" OR "Central Operations Admin" group
- Methods:
  - `Index()` - GET `/Admin/PerformanceReportingManagement`
  - `BulkDelete()` - GET/POST `/Admin/PerformanceReportingManagement/BulkDelete`
  - `DueDateOverrides()` - GET `/Admin/PerformanceReportingManagement/DueDateOverrides`
  - `CreateDueDateOverride()` - GET/POST `/Admin/PerformanceReportingManagement/CreateDueDateOverride`
  - `EditDueDateOverride()` - GET/POST `/Admin/PerformanceReportingManagement/EditDueDateOverride/{id}`
  - `DeleteDueDateOverride()` - POST `/Admin/PerformanceReportingManagement/DeleteDueDateOverride/{id}`
  - `BusinessAreaConfig()` - GET `/Admin/PerformanceReportingManagement/BusinessAreaConfig`
  - `CreateBusinessAreaConfig()` - GET/POST `/Admin/PerformanceReportingManagement/CreateBusinessAreaConfig`
  - `EditBusinessAreaConfig()` - GET/POST `/Admin/PerformanceReportingManagement/EditBusinessAreaConfig/{id}`
  - `DeleteBusinessAreaConfig()` - POST `/Admin/PerformanceReportingManagement/DeleteBusinessAreaConfig/{id}`
  - `ProductExclusions()` - GET `/Admin/PerformanceReportingManagement/ProductExclusions`
  - `CreateProductExclusion()` - GET/POST `/Admin/PerformanceReportingManagement/CreateProductExclusion`
  - `EditProductExclusion()` - GET/POST `/Admin/PerformanceReportingManagement/EditProductExclusion/{id}`
  - `DeleteProductExclusion()` - POST `/Admin/PerformanceReportingManagement/DeleteProductExclusion/{id}`
  - `PeriodExclusions()` - GET `/Admin/PerformanceReportingManagement/PeriodExclusions`
  - `CreatePeriodExclusion()` - GET/POST `/Admin/PerformanceReportingManagement/CreatePeriodExclusion`
  - `EditPeriodExclusion()` - GET/POST `/Admin/PerformanceReportingManagement/EditPeriodExclusion/{id}`
  - `DeletePeriodExclusion()` - POST `/Admin/PerformanceReportingManagement/DeletePeriodExclusion`

**Required Groups**: "Super admin" OR "Central Operations Admin"

---

**`Admin/NotificationTemplatesController`** - `/Admin/NotificationTemplates/*`
- **Class-level**: `[Authorize]`
- **All methods** use `IsAuthorizedAsync()` helper:
  - Checks "Super admin" OR "Central Operations Admin" group
- Methods:
  - `Index()` - GET `/Admin/NotificationTemplates`
  - `Details()` - GET `/Admin/NotificationTemplates/Details/{id}`
  - `Create()` - GET/POST `/Admin/NotificationTemplates/Create`
  - `Edit()` - GET/POST `/Admin/NotificationTemplates/Edit/{id}`
  - `Delete()` - GET/POST `/Admin/NotificationTemplates/Delete/{id}`

**Required Groups**: "Super admin" OR "Central Operations Admin"

---

**`Admin/NotificationRulesController`** - `/Admin/NotificationRules/*`
- **Class-level**: `[Authorize]`
- **All methods** use `IsAuthorizedAsync()` helper:
  - Checks "Super admin" OR "Central Operations Admin" group
- Methods:
  - `Index()` - GET `/Admin/NotificationRules`
  - `Details()` - GET `/Admin/NotificationRules/Details/{id}`
  - `Create()` - GET/POST `/Admin/NotificationRules/Create`
  - `Edit()` - GET/POST `/Admin/NotificationRules/Edit/{id}`
  - `Delete()` - GET/POST `/Admin/NotificationRules/Delete/{id}`

**Required Groups**: "Super admin" OR "Central Operations Admin"

---

### 3. Legacy Role-Based Authorization (Admin/SuperAdmin Roles)

**`ProjectController`** - `/Project/*`
- **Class-level**: `[Authorize]`
- **Specific methods** use `[Authorize(Roles = "Admin,SuperAdmin")]`:
  - Line 2386: Some admin action
  - Line 2710: Some admin action
- **Note**: These use legacy UserRole enum, not group-based

**Required**: UserRole.Admin OR UserRole.SuperAdmin (legacy)

---

**`Admin/StandardsConfigController`** - `/Admin/StandardsConfig/*`
- **Class-level**: `[Authorize(Roles = "Admin,SuperAdmin")]`
- **All methods** require Admin or SuperAdmin role (legacy)

**Required**: UserRole.Admin OR UserRole.SuperAdmin (legacy)

---

**`Admin/StandardProductsController`** - `/Admin/StandardProducts/*`
- **Class-level**: `[Authorize(Roles = "Admin,SuperAdmin")]`
- **All methods** require Admin or SuperAdmin role (legacy)

**Required**: UserRole.Admin OR UserRole.SuperAdmin (legacy)

---

**`DdtStandardsController`** - `/DdtStandards/*`
- **Class-level**: `[Authorize]`
- **Specific methods** use `[Authorize(Roles = "Admin,SuperAdmin")]`:
  - Line 1154: Some admin action
  - Line 1204: Some admin action
  - Line 1256: Some admin action
  - Line 1320: Some admin action

**Required**: UserRole.Admin OR UserRole.SuperAdmin (legacy)

---

### 4. Authenticated Users Only (No Specific Group Required)

**`AdminController`** - `/Admin/*` (non-API token methods)
- **Class-level**: `[Authorize]`
- **Most methods** only require authentication:
  - `Index()` - GET `/Admin`
  - `ChatbotConversations()` - GET `/Admin/ChatbotConversations`
  - `RaidSettings()` - GET `/Admin/RaidSettings`
  - `CreateRaidLookup()` - POST `/Admin/CreateRaidLookup`
  - `UpdateRaidLookup()` - POST `/Admin/UpdateRaidLookup`
  - `DeleteRaidLookup()` - POST `/Admin/DeleteRaidLookup`
  - `SeedRaidLookupDefaults()` - POST `/Admin/SeedRaidLookupDefaults`
  - `Users()` - GET `/Admin/Users`
  - `CreateUser()` - GET/POST `/Admin/CreateUser`
  - `EditUser()` - GET/POST `/Admin/EditUser/{id}`
  - `DeleteUser()` - GET/POST `/Admin/DeleteUser/{id}`
  - `UserSatisfaction()` - GET `/Admin/UserSatisfaction`
  - `ResponseScales()` - GET `/Admin/ResponseScales`
  - `CreateResponseScale()` - POST `/Admin/CreateResponseScale`
  - `AddScaleOption()` - POST `/Admin/AddScaleOption`
  - `UpdateScaleOption()` - POST `/Admin/UpdateScaleOption`
  - `DeleteResponseScale()` - POST `/Admin/DeleteResponseScale`
  - `UserSatisfactionQuestions()` - GET `/Admin/UserSatisfactionQuestions`
  - `CreateUssTemplate()` - POST `/Admin/CreateUssTemplate`
  - `AddUssQuestion()` - POST `/Admin/AddUssQuestion`
  - `UpdateUssQuestion()` - POST `/Admin/UpdateUssQuestion`
  - `AddQuestionOption()` - POST `/Admin/AddQuestionOption`
  - `UpdateQuestionOption()` - POST `/Admin/UpdateQuestionOption`
  - `DeleteQuestionOption()` - POST `/Admin/DeleteQuestionOption`
  - `UserSatisfactionResponses()` - GET `/Admin/UserSatisfactionResponses`
  - `Objectives()` - GET `/Admin/Objectives`
  - `ObjectiveDetails()` - GET `/Admin/ObjectiveDetails/{id}`
  - `CreateObjective()` - GET/POST `/Admin/CreateObjective`
  - `EditObjective()` - GET/POST `/Admin/EditObjective/{id}`
  - `DeleteObjective()` - GET/POST `/Admin/DeleteObjective/{id}`
  - `Settings()` - GET `/Admin/Settings`
  - `KpiCategories()` - GET `/Admin/KpiCategories`
  - `CreateKpiCategory()` - POST `/Admin/CreateKpiCategory`
  - `UpdateKpiCategory()` - POST `/Admin/UpdateKpiCategory`
  - `DeleteKpiCategory()` - POST `/Admin/DeleteKpiCategory`
  - `RiskTypes()` - GET `/Admin/RiskTypes`
  - `CreateRiskType()` - GET/POST `/Admin/CreateRiskType`
  - `EditRiskType()` - GET/POST `/Admin/EditRiskType/{id}`
  - `DeleteRiskType()` - GET/POST `/Admin/DeleteRiskType/{id}`
  - `RiskTiers()` - GET `/Admin/RiskTiers`
  - And many more...

**Required**: Authenticated user only

---

**`Admin/AccessibilityServiceController`** - `/Admin/AccessibilityService/*`
- **Class-level**: `[Authorize]`
- **All methods** only require authentication

**Required**: Authenticated user only

---

**All other controllers** with `[Authorize]` only:
- `DdtReportsController` - `/DdtReports/*`
- `MilestonesUpdatesSuccessesController` - `/MilestonesUpdatesSuccesses/*`
- `TasksController` - `/Tasks/*`
- `AnalysisController` - `/Analysis/*`
- `HelpController` - `/Help/*`
- `DemandManagementController` - `/DemandManagement/*`
- `HomeController` - `/Home/*`
- `RaidController` - `/Raid/*`
- `UserLeadershipController` - `/UserLeadership/*`
- `UsersController` - `/Users/*`
- `UserController` - `/User/*`
- `StandardController` - `/Standard/*`
- `StaffRoleReturnController` - `/StaffRoleReturn/*`
- `RiskController` - `/Risk/*`
- `ProfileController` - `/Profile/*`
- `ProductReportingController` - `/ProductReporting/*`
- `PerformanceMetricController` - `/PerformanceMetric/*`
- `PeopleSearchController` - `/PeopleSearch/*`
- `IssueController` - `/Issue/*`
- `ActionController` - `/Action/*`
- `AppsController` - `/Apps/*`
- `MilestoneController` - `/Milestone/*`
- `ProductsController` - `/Products/*`
- `FunctionalStandardController` - `/FunctionalStandard/*`
- `GovernmentDepartmentController` - `/GovernmentDepartment/*`
- `EnterpriseReportingController` - `/EnterpriseReporting/*`
- `EnterpriseMetricController` - `/EnterpriseMetric/*`
- `OrganizationalController` - `/Organizational/*`
- `DocumentationController` - `/Documentation/*`
- `UserSatisfactionSurveysController` - `/UserSatisfactionSurveys/*`
- `AccessibilityController` - `/Accessibility/*`
- `DdtStandardsController` - `/DdtStandards/*` (except admin methods)

**Required**: Authenticated user only

---

### 5. API Controllers (Token-Based Authorization)

**All API controllers** use `[RequireApiPermission]` attribute:
- `Api/V1/DdtStandardsController` - `/api/v1/ddt-standards/*`
- `Api/V1/SurveysController` - `/api/v1/surveys/*`
- `Api/V1/StatementTemplatesController` - `/api/v1/statement-templates/*`
- `Api/V1/Admin/TemplatesController` - `/api/v1/admin/templates/*`
- `Api/V1/Admin/TemplateQuestionsController` - `/api/v1/admin/*`
- `Api/V1/Admin/SurveyInstancesController` - `/api/v1/admin/survey-instances/*`
- `Api/V1/Admin/ServicesController` - `/api/v1/admin/services/*`
- `Api/V1/Admin/JourneyController` - `/api/v1/admin/templates/{id}/journey/*`
- `Api/V1/AccessibilityController` - `/api/v1/accessibility/*`
- `Api/V1/PerformanceMetricsController` - `/api/v1/performance-metrics/*`
- `Api/V1/FunctionalStandardsController` - `/api/v1/functional-standards/*`
- `Api/V1/EnterpriseMetricsController` - `/api/v1/enterprise-metrics/*`
- `Api/V1/MilestonesController` - `/api/v1/milestones/*`
- `Api/V1/RisksController` - `/api/v1/risks/*`
- `Api/V1/ActionsController` - `/api/v1/actions/*`
- `Api/V1/IssuesController` - `/api/v1/issues/*`

**Required**: Valid API token with appropriate permissions

---

### 6. Other API Controllers (User Authentication)

**`Api/V1/ChatbotController`** - `/api/v1/chatbot/*`
- **Class-level**: `[Authorize]`
- Requires authenticated user

**`Api/ProductsController`** - `/api/products/*`
- **Class-level**: `[Authorize]`
- Requires authenticated user

**`Api/EntitiesController`** - `/api/entities/*`
- **Class-level**: `[Authorize]`
- Requires authenticated user

**`Api/StaffController`** - `/api/staff/*`
- Requires authenticated user

**`ApiController`** - `/api/*`
- **Class-level**: `[Authorize]`
- Requires authenticated user

**`CommentsApiController`** - `/api/comments/*`
- **Class-level**: `[Authorize]`
- Requires authenticated user

**`ActionSourceItemsApiController`** - `/api/action-source-items/*`
- **Class-level**: `[Authorize]`
- Requires authenticated user

---

## Summary by Required Group

### "Super admin" Group Required
- `/Admin/ApiTokens/*` (all API token management)

### "Super admin" OR "Central Operations Admin" Groups Required
- `/CentralOps/*` (all routes)
- `/Admin/UserManagement/*` (all routes)
- `/Admin/PerformanceReportingManagement/*` (all routes)
- `/Admin/NotificationTemplates/*` (all routes)
- `/Admin/NotificationRules/*` (all routes)

### Legacy Role-Based (UserRole.Admin OR UserRole.SuperAdmin)
- `/Admin/StandardsConfig/*` (all routes)
- `/Admin/StandardProducts/*` (all routes)
- `/DdtStandards/*` (specific admin methods)
- `/Project/*` (specific admin methods)

### Authenticated Users Only
- All other `/Admin/*` routes (except those listed above)
- Most application controllers
- Most API controllers (except token-based ones)

---

## Recommendations

1. **Consider migrating legacy role-based authorization** (`[Authorize(Roles = "Admin,SuperAdmin")]`) to group-based authorization for consistency.

2. **Review AdminController methods** - Many admin functions only require authentication. Consider if they should require specific groups.

3. **Standardize authorization patterns** - Some controllers use helper methods (`IsAuthorizedAsync()`), others use attributes. Consider standardizing on attributes where possible.

4. **Document group requirements** - Ensure all groups are properly documented and users understand which groups grant which permissions.

---

## Group Definitions

- **"Super admin"**: Full system access including API management
- **"Central Operations Admin"**: Administrative access to most features (except API management)
- **Other groups**: Feature-specific permissions managed through GroupFeaturePermissions

