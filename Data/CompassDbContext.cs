using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Data;
using System.Text.Json;
using Compass.Models;
using Compass.Services;

namespace Compass.Data;

public partial class CompassDbContext : DbContext
{
    private readonly IAuditContextProvider _auditContextProvider;
    private bool _suppressAuditLogging;
    private bool _auditSchemaChecked;
    private bool _auditColumnsAvailable = true;

    public CompassDbContext(DbContextOptions<CompassDbContext> options) : this(options, new NullAuditContextProvider())
    {
    }

    public CompassDbContext(
        DbContextOptions<CompassDbContext> options,
        IAuditContextProvider auditContextProvider) : base(options)
    {
        _auditContextProvider = auditContextProvider;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Suppress pending model changes warning for now
        optionsBuilder.ConfigureWarnings(warnings => 
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Configure default string length for SQL Server (to avoid nvarchar(max) for indexed columns)
        configurationBuilder.Properties<string>()
            .HaveMaxLength(450); // SQL Server index key size limit
    }

    // User management
    public DbSet<User> Users { get; set; }
    public DbSet<UserBusinessAreaRoleAssignment> UserBusinessAreaRoleAssignments { get; set; }
    public DbSet<UserPreference> UserPreferences { get; set; }
    
    // Role-based access control
    public DbSet<Group> Groups { get; set; }
    public DbSet<Feature> Features { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<GroupFeaturePermission> GroupFeaturePermissions { get; set; }
    
    // API Management
    public DbSet<ApiToken> ApiTokens { get; set; }
    public DbSet<ApiTokenPermission> ApiTokenPermissions { get; set; }
    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
    
    // Operational reports
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
    
    // Functional standards
    public DbSet<FunctionalStandard> FunctionalStandards { get; set; }
    public DbSet<FunctionalStandardTheme> FunctionalStandardThemes { get; set; }
    public DbSet<PracticeArea> PracticeAreas { get; set; }
    public DbSet<Criterion> Criteria { get; set; }
    
    // Delivery reporting
    public DbSet<ProductReturn> ProductReturns { get; set; }
    public DbSet<ProductMetricValue> ProductMetricValues { get; set; }
    
    // Performance reporting management
    public DbSet<PerformanceReportingDueDateOverride> PerformanceReportingDueDateOverrides { get; set; }
    public DbSet<PerformanceReportingBusinessAreaConfig> PerformanceReportingBusinessAreaConfigs { get; set; }
    public DbSet<PerformanceReportingProductExclusion> PerformanceReportingProductExclusions { get; set; }
    public DbSet<PerformanceReportingPeriodExclusion> PerformanceReportingPeriodExclusions { get; set; }
    
    // KPI management
    public DbSet<Kpi> Kpis { get; set; }
    public DbSet<KpiDataPoint> KpiDataPoints { get; set; }

    // Enterprise reporting - Functional Standard Assessments
    public DbSet<FunctionalStandardAssessment> FunctionalStandardAssessments { get; set; }
    public DbSet<AssessmentCriteriaResponse> AssessmentCriteriaResponses { get; set; }
    
    // Organizational structure
    public DbSet<OrganizationalGroup> OrganizationalGroups { get; set; }
    public DbSet<OrganizationalRole> OrganizationalRoles { get; set; }
    public DbSet<GovernmentDepartment> GovernmentDepartments { get; set; }
    
    // Accessibility Management (Apps)
    public DbSet<ProductAccessibility> ProductAccessibilities { get; set; }
    public DbSet<ContactMethod> ContactMethods { get; set; }
    public DbSet<AuditHistory> AuditHistories { get; set; }
    public DbSet<AccessibilityIssue> AccessibilityIssues { get; set; }
    public DbSet<IssueComment> IssueComments { get; set; }
    public DbSet<IssueHistory> IssueHistories { get; set; }
    public DbSet<WcagCriterion> WcagCriteria { get; set; }
    public DbSet<IssueWcagCriterion> IssueWcagCriteria { get; set; }
    public DbSet<AccessibilityRetestRequest> AccessibilityRetestRequests { get; set; }
    public DbSet<StatementVerificationRequest> StatementVerificationRequests { get; set; }
    public DbSet<AccessibilityEmailConfiguration> AccessibilityEmailConfigurations { get; set; }
    public DbSet<StatementTemplate> StatementTemplates { get; set; }
    
    // Notifications
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    public DbSet<NotificationRule> NotificationRules { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }
    
    // Enterprise reporting - Enterprise Metrics
    public DbSet<EnterpriseMetric> EnterpriseMetrics { get; set; }
    public DbSet<EnterpriseReturn> EnterpriseReturns { get; set; }
    public DbSet<EnterpriseMetricValue> EnterpriseMetricValues { get; set; }
    
    // Staff Role Return
    public DbSet<StaffRoleReturn> StaffRoleReturns { get; set; }
    public DbSet<StaffRoleReturnSkill> StaffRoleReturnSkills { get; set; }
    public DbSet<GddRole> GddRoles { get; set; }
    public DbSet<Skill> Skills { get; set; }
    
    // Learning & Development (L&D)
    public DbSet<TrainingCourse> TrainingCourses { get; set; }
    public DbSet<TrainingRecord> TrainingRecords { get; set; }
    public DbSet<TrainingRequest> TrainingRequests { get; set; }
    public DbSet<UserProfessionalProfile> UserProfessionalProfiles { get; set; }
    public DbSet<HOPS> HOPS { get; set; }
    public DbSet<TrainingNudge> TrainingNudges { get; set; }
    public DbSet<LearningBudget> LearningBudgets { get; set; }
    
    // Product Governance
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<Risk> Risks { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Milestone> Milestones { get; set; }
    public DbSet<Models.Action> Actions { get; set; }
    public DbSet<Decision> Decisions { get; set; }
    public DbSet<Comment> Comments { get; set; }

    // Surveys (Apps)
    public DbSet<FipsService> Services { get; set; }
    public DbSet<SurveyTemplate> SurveyTemplates { get; set; }
    public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
    public DbSet<SurveyOption> SurveyOptions { get; set; }
    public DbSet<ResponseScale> ResponseScales { get; set; }
    public DbSet<ResponseScaleOption> ResponseScaleOptions { get; set; }
    public DbSet<JourneyStep> JourneySteps { get; set; }
    public DbSet<SurveyInstance> SurveyInstances { get; set; }
    public DbSet<SurveyResponse> SurveyResponses { get; set; }
    public DbSet<ResponseAnswer> ResponseAnswers { get; set; }
    public DbSet<ScoreSnapshot> ScoreSnapshots { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    
    // Project Management
    public DbSet<Project> Projects { get; set; }
    public DbSet<Mission> Missions { get; set; }
    public DbSet<FundingSource> FundingSources { get; set; }
    public DbSet<ProjectRagHistory> ProjectRagHistories { get; set; }
    public DbSet<ProjectSuccess> ProjectSuccesses { get; set; }
    public DbSet<ProjectOutcome> ProjectOutcomes { get; set; }
    public DbSet<ProjectNeed> ProjectNeeds { get; set; }
    public DbSet<ProjectProblemStatement> ProjectProblemStatements { get; set; }
    public DbSet<ProjectProblemStatementHistory> ProjectProblemStatementHistories { get; set; }
    public DbSet<ProjectMission> ProjectMissions { get; set; }
    public DbSet<ProjectFundingAllocation> ProjectFundingAllocations { get; set; }
    public DbSet<ProjectResourceFunding> ProjectResourceFundings { get; set; }
    public DbSet<ProjectResourceFundingHistory> ProjectResourceFundingHistories { get; set; }
    public DbSet<ProjectContact> ProjectContacts { get; set; }
    public DbSet<ProjectObjective> ProjectObjectives { get; set; }
    public DbSet<Dependency> Dependencies { get; set; }
    public DbSet<ProjectProduct> ProjectProducts { get; set; }
    public DbSet<ProjectDraft> ProjectDrafts { get; set; }
    
    // RAID Lookups
    public DbSet<RiskType> RiskTypes { get; set; }
    public DbSet<RiskTier> RiskTiers { get; set; }
    public DbSet<ActionSource> ActionSources { get; set; }
    
    // Help Chatbot
    public DbSet<ChatConversation> ChatConversations { get; set; }
    
    // Project Lookups
    public DbSet<BusinessAreaLookup> BusinessAreaLookups { get; set; }
    public DbSet<PhaseLookup> PhaseLookups { get; set; }
    public DbSet<DeliveryPriority> DeliveryPriorities { get; set; }
    public DbSet<KpiCategory> KpiCategories { get; set; }
    public DbSet<ActivityTypeLookup> ActivityTypeLookups { get; set; }
    public DbSet<DirectorateLookup> DirectorateLookups { get; set; }
    public DbSet<RiskAppetiteLookup> RiskAppetiteLookups { get; set; }
    
    // Project relationships
    public DbSet<ProjectStatusUpdate> ProjectStatusUpdates { get; set; }
    public DbSet<ProjectMonthlyUpdate> ProjectMonthlyUpdates { get; set; }
    public DbSet<MonthlyUpdateNarrative> MonthlyUpdateNarratives { get; set; }
    public DbSet<ProjectWeeklySuccessUpdate> ProjectWeeklySuccessUpdates { get; set; }
    public DbSet<MonthlyStatusReport> MonthlyStatusReports { get; set; }
    public DbSet<MonthlyStatusReportTimescaleConfig> MonthlyStatusReportTimescaleConfigs { get; set; }
    public DbSet<MonthlyUpdateDeadlineConfig> MonthlyUpdateDeadlineConfigs { get; set; }
    public DbSet<ProjectSeniorResponsibleOfficer> ProjectSeniorResponsibleOfficers { get; set; }
    public DbSet<ProjectServiceOwner> ProjectServiceOwners { get; set; }
    public DbSet<ProjectDirectorate> ProjectDirectorates { get; set; }
    public DbSet<ProjectArtefact> ProjectArtefacts { get; set; }
    public DbSet<ProjectBudgetOwner> ProjectBudgetOwners { get; set; }
    public DbSet<ProjectPmoContact> ProjectPmoContacts { get; set; }
    public DbSet<ProjectWatchlist> ProjectWatchlists { get; set; }
    
    // RAID Junction Tables
    public DbSet<RiskAction> RiskActions { get; set; }
    public DbSet<RiskRiskType> RiskRiskTypes { get; set; }
    public DbSet<IssueAction> IssueActions { get; set; }
    public DbSet<RiskDecision> RiskDecisions { get; set; }
    public DbSet<IssueDecision> IssueDecisions { get; set; }
    public DbSet<IssueRisk> IssueRisks { get; set; }
    public DbSet<ActionDecision> ActionDecisions { get; set; }
    public DbSet<MilestoneAction> MilestoneActions { get; set; }
    public DbSet<MilestoneRisk> MilestoneRisks { get; set; }
    public DbSet<MilestoneIssue> MilestoneIssues { get; set; }
    public DbSet<MilestoneUpdate> MilestoneUpdates { get; set; }
    
    // Demand Management
    public DbSet<DemandRequest> DemandRequests { get; set; }
    public DbSet<DemandRequestContact> DemandRequestContacts { get; set; }
    public DbSet<DemandRequestPrioritisation> DemandRequestPrioritisations { get; set; }
    public DbSet<DemandRequestNote> DemandRequestNotes { get; set; }
    public DbSet<DemandRequestAssessment> DemandRequestAssessments { get; set; }
    public DbSet<DemandRequestSectionCompletion> DemandRequestSectionCompletions { get; set; }
    public DbSet<DemandRequestRiskType> DemandRequestRiskTypes { get; set; }
    public DbSet<TriageMeeting> TriageMeetings { get; set; }
    
    // DDT Standards Management
    public DbSet<DdtStandard> DdtStandards { get; set; }
    public DbSet<DdtStandardOwner> DdtStandardOwners { get; set; }
    public DbSet<DdtStandardContact> DdtStandardContacts { get; set; }
    public DbSet<DdtStandardPhase> DdtStandardPhases { get; set; }
    public DbSet<DdtStandardValidationRule> DdtStandardValidationRules { get; set; }
    public DbSet<DdtStandardVersion> DdtStandardVersions { get; set; }
    public DbSet<DdtStandardCategory> DdtStandardCategories { get; set; }
    public DbSet<DdtStandardSubCategory> DdtStandardSubCategories { get; set; }
    public DbSet<DdtStandardComment> DdtStandardComments { get; set; }
    public DbSet<DdtStandardProduct> DdtStandardProducts { get; set; }
    public DbSet<DdtStandardException> DdtStandardExceptions { get; set; }
    public DbSet<DdtStandardUnpublishAudit> DdtStandardUnpublishAudits { get; set; }
    
    // Standards Configuration
    public DbSet<StandardCategory> StandardCategories { get; set; }
    public DbSet<StandardSubCategory> StandardSubCategories { get; set; }
    public DbSet<StandardProduct> StandardProducts { get; set; }
    
    // Service Standards (GOV.UK Service Standards)
    public DbSet<ServiceStandard> ServiceStandards { get; set; }
    public DbSet<ServiceStandardPhaseGuidance> ServiceStandardPhaseGuidance { get; set; }
    public DbSet<DdatProfession> DdatProfessions { get; set; }
    public DbSet<ServiceStandardProfession> ServiceStandardProfessions { get; set; }
    
    // Technology Code of Practice
    public DbSet<TechnologyCodeOfPractice> TechnologyCodeOfPractice { get; set; }
    public DbSet<TechnologyCodeOfPracticeProfession> TechnologyCodeOfPracticeProfessions { get; set; }
    public DbSet<TechnologyCodeOfPracticePhaseGuidance> TechnologyCodeOfPracticePhaseGuidance { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (_suppressAuditLogging)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        var auditEntries = PrepareAuditEntries();
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        AppendAuditEntries(auditEntries, acceptAllChangesOnSuccess);
        return result;
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        if (_suppressAuditLogging)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var auditEntries = PrepareAuditEntries();
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        await AppendAuditEntriesAsync(auditEntries, acceptAllChangesOnSuccess, cancellationToken);
        return result;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesAsync(true, cancellationToken);

    private List<AuditLog> PrepareAuditEntries()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditLog>();
        var timestamp = DateTime.UtcNow;
        var currentUserId = _auditContextProvider.UserId;
        var currentUserName = _auditContextProvider.UserName;
        var currentUserEmail = _auditContextProvider.UserEmail;
        var ipAddress = _auditContextProvider.IpAddress;
        var userAgent = _auditContextProvider.UserAgent;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            var entityName = entry.Metadata.ClrType.Name;
            var primaryKey = GetPrimaryKey(entry);
            var audit = new AuditLog
            {
                Entity = entityName,
                EntityId = primaryKey,
                EntityReference = entry.Entity.ToString(),
                Action = entry.State switch
                {
                    EntityState.Added => "Create",
                    EntityState.Modified => "Update",
                    EntityState.Deleted => "Delete",
                    _ => entry.State.ToString()
                },
                ChangedUtc = timestamp,
                ChangedBy = currentUserName,
                ChangedByUserId = currentUserId,
                ChangedByEmail = currentUserEmail,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    audit.AfterJson = SerializePropertyValues(entry.Properties, includeOriginalValues: false);
                    break;
                case EntityState.Deleted:
                    audit.BeforeJson = SerializePropertyValues(entry.Properties, includeOriginalValues: true);
                    break;
                case EntityState.Modified:
                    audit.BeforeJson = SerializeChangedValues(entry.Properties, useOriginalValues: true);
                    audit.AfterJson = SerializeChangedValues(entry.Properties, useOriginalValues: false);
                    break;
            }

            auditEntries.Add(audit);
        }

        return auditEntries;
    }

    private string SerializePropertyValues(IEnumerable<PropertyEntry> properties, bool includeOriginalValues)
    {
        var dictionary = new Dictionary<string, object?>();
        foreach (var property in properties)
        {
            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            dictionary[property.Metadata.Name] = includeOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;
        }

        return JsonSerializer.Serialize(dictionary);
    }

    private string SerializeChangedValues(IEnumerable<PropertyEntry> properties, bool useOriginalValues)
    {
        var dictionary = new Dictionary<string, object?>();
        foreach (var property in properties)
        {
            if (!property.IsModified)
            {
                continue;
            }

            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            dictionary[property.Metadata.Name] = useOriginalValues
                ? property.OriginalValue
                : property.CurrentValue;
        }

        return JsonSerializer.Serialize(dictionary);
    }

    private string GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .Select(p => p.CurrentValue?.ToString() ?? p.OriginalValue?.ToString())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return key ?? string.Empty;
    }

    private bool EnsureAuditColumnsAvailable()
    {
        if (_auditSchemaChecked)
        {
            return _auditColumnsAvailable;
        }

        var connection = Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;

        try
        {
            if (!wasOpen)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'AuditLogs' 
                  AND COLUMN_NAME IN ('AfterJson','BeforeJson','ChangedByEmail','ChangedByUserId','EntityReference','IpAddress','UserAgent')";

            var count = Convert.ToInt32(command.ExecuteScalar() ?? 0);
            _auditColumnsAvailable = count >= 7;
        }
        catch
        {
            _auditColumnsAvailable = false;
        }
        finally
        {
            _auditSchemaChecked = true;
            if (!wasOpen && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
        }

        return _auditColumnsAvailable;
    }

    private static bool IsMissingAuditColumnException(Exception? exception)
    {
        if (exception == null)
        {
            return false;
        }

        if (exception is SqlException sqlException && sqlException.Number == 207)
        {
            return true;
        }

        return IsMissingAuditColumnException(exception.InnerException);
    }

    private void AppendAuditEntries(IEnumerable<AuditLog> auditEntries, bool acceptAllChangesOnSuccess)
    {
        var entries = auditEntries.ToList();
        if (!entries.Any())
        {
            return;
        }

        if (!EnsureAuditColumnsAvailable())
        {
            return;
        }

        try
        {
            _suppressAuditLogging = true;
            AuditLogs.AddRange(entries);
            base.SaveChanges(acceptAllChangesOnSuccess);
        }
        catch (Exception ex) when (IsMissingAuditColumnException(ex))
        {
            _auditColumnsAvailable = false;
        }
        finally
        {
            _suppressAuditLogging = false;
        }
    }

    private async Task AppendAuditEntriesAsync(IEnumerable<AuditLog> auditEntries, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        var entries = auditEntries.ToList();
        if (!entries.Any())
        {
            return;
        }

        if (!EnsureAuditColumnsAvailable())
        {
            return;
        }

        try
        {
            _suppressAuditLogging = true;
            AuditLogs.AddRange(entries);
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (Exception ex) when (IsMissingAuditColumnException(ex))
        {
            _auditColumnsAvailable = false;
        }
        finally
        {
            _suppressAuditLogging = false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<UserBusinessAreaRoleAssignment>()
            .HasIndex(a => new { a.UserId, a.BusinessAreaKey, a.Role })
            .IsUnique();

        modelBuilder.Entity<UserBusinessAreaRoleAssignment>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.AzureObjectId)
            .IsUnique()
            .HasFilter("[AzureObjectId] IS NOT NULL");

        // Surveys configuration
        modelBuilder.Entity<FipsService>()
            .HasIndex(s => s.FipsId)
            .IsUnique();

        modelBuilder.Entity<FipsService>()
            .HasMany(s => s.SurveyInstances)
            .WithOne(si => si.Service)
            .HasForeignKey(si => si.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyTemplate>()
            .HasIndex(t => new { t.Name, t.Version })
            .IsUnique();

        modelBuilder.Entity<SurveyQuestion>()
            .HasOne(q => q.Template)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.SurveyTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SurveyQuestion>()
            .HasIndex(q => new { q.SurveyTemplateId, q.Code })
            .IsUnique();

        modelBuilder.Entity<SurveyOption>()
            .HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResponseScaleOption>()
            .HasOne(o => o.Scale)
            .WithMany(s => s.Options)
            .HasForeignKey(o => o.ResponseScaleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JourneyStep>()
            .HasOne(js => js.Template)
            .WithMany(t => t.JourneySteps)
            .HasForeignKey(js => js.SurveyTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<JourneyStep>()
            .HasIndex(js => new { js.SurveyTemplateId, js.Ordinal })
            .IsUnique();

        modelBuilder.Entity<SurveyInstance>()
            .HasOne(si => si.Template)
            .WithMany()
            .HasForeignKey(si => si.SurveyTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SurveyInstance>()
            .HasIndex(si => new { si.ServiceId, si.IsActive })
            .HasFilter("[IsActive] = 1");

        modelBuilder.Entity<SurveyResponse>()
            .HasOne(r => r.SurveyInstance)
            .WithMany(si => si.Responses)
            .HasForeignKey(r => r.SurveyInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResponseAnswer>()
            .HasOne(ra => ra.SurveyResponse)
            .WithMany(r => r.Answers)
            .HasForeignKey(ra => ra.SurveyResponseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ResponseAnswer>()
            .HasOne(ra => ra.SurveyQuestion)
            .WithMany()
            .HasForeignKey(ra => ra.SurveyQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.AzureObjectId)
            .IsUnique()
            .HasFilter("[AzureObjectId] IS NOT NULL");

        modelBuilder.Entity<UserPreference>()
            .Property(p => p.DashboardLayout)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.AfterJson)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.BeforeJson)
            .HasColumnType("nvarchar(max)");

        // Configure PerformanceMetric entity
        modelBuilder.Entity<PerformanceMetric>()
            .HasIndex(pm => pm.Identifier)
            .IsUnique();

        // Configure FunctionalStandard relationships
        modelBuilder.Entity<FunctionalStandard>()
            .Property(fs => fs.Id)
            .ValueGeneratedNever(); // User-defined ID

        modelBuilder.Entity<FunctionalStandardTheme>()
            .HasOne(t => t.FunctionalStandard)
            .WithMany(fs => fs.Themes)
            .HasForeignKey(t => t.FunctionalStandardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PracticeArea>()
            .HasOne(pa => pa.Theme)
            .WithMany(t => t.PracticeAreas)
            .HasForeignKey(pa => new { pa.FunctionalStandardId, pa.ThemeId })
            .HasPrincipalKey(t => new { t.FunctionalStandardId, t.ThemeId })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FunctionalStandardTheme>()
            .HasIndex(t => new { t.FunctionalStandardId, t.ThemeId })
            .IsUnique();

        modelBuilder.Entity<Criterion>()
            .HasOne(c => c.PracticeArea)
            .WithMany(pa => pa.Criteria)
            .HasForeignKey(c => new { c.FunctionalStandardId, c.ThemeId, c.PracticeAreaId })
            .HasPrincipalKey(pa => new { pa.FunctionalStandardId, pa.ThemeId, pa.PracticeAreaId })
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ProductReturn
        // Note: Index is non-unique initially to allow NULLs during migration
        // Will be made unique after data migration populates DocumentIds
        modelBuilder.Entity<ProductReturn>()
            .HasIndex(pr => new { pr.ProductDocumentId, pr.Year, pr.Month });

        modelBuilder.Entity<ProductReturn>()
            .HasIndex(pr => pr.FipsId);

        modelBuilder.Entity<ProductReturn>()
            .HasMany(pr => pr.MetricValues)
            .WithOne(mv => mv.ProductReturn)
            .HasForeignKey(mv => mv.ProductReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure ProductMetricValue
        modelBuilder.Entity<ProductMetricValue>()
            .HasOne(mv => mv.PerformanceMetric)
            .WithMany()
            .HasForeignKey(mv => mv.PerformanceMetricId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductMetricValue>()
            .HasIndex(mv => new { mv.ProductReturnId, mv.PerformanceMetricId })
            .IsUnique();

        // Configure PerformanceReportingDueDateOverride
        modelBuilder.Entity<PerformanceReportingDueDateOverride>()
            .HasIndex(prdo => new { prdo.ReportingYear, prdo.ReportingMonth })
            .IsUnique();

        modelBuilder.Entity<PerformanceReportingDueDateOverride>()
            .HasIndex(prdo => prdo.IsActive);

        // Configure PerformanceReportingBusinessAreaConfig
        modelBuilder.Entity<PerformanceReportingBusinessAreaConfig>()
            .HasIndex(prbac => prbac.BusinessAreaName);

        modelBuilder.Entity<PerformanceReportingBusinessAreaConfig>()
            .HasIndex(prbac => prbac.IsActive);

        // Configure PerformanceReportingProductExclusion
        // Note: Index is non-unique initially to allow NULLs during migration
        // Will be made unique after data migration populates DocumentIds
        modelBuilder.Entity<PerformanceReportingProductExclusion>()
            .HasIndex(prpe => prpe.ProductDocumentId);

        modelBuilder.Entity<PerformanceReportingProductExclusion>()
            .HasIndex(prpe => prpe.FipsId);

        modelBuilder.Entity<PerformanceReportingProductExclusion>()
            .HasIndex(prpe => prpe.IsActive);

        modelBuilder.Entity<PerformanceReportingProductExclusion>()
            .HasIndex(prpe => new { prpe.ProductDocumentId, prpe.IsActive });

        // Configure PerformanceReportingPeriodExclusion
        modelBuilder.Entity<PerformanceReportingPeriodExclusion>()
            .HasIndex(prpe => new { prpe.Year, prpe.Month })
            .IsUnique();

        modelBuilder.Entity<PerformanceReportingPeriodExclusion>()
            .HasIndex(prpe => prpe.IsActive);

        // Configure FunctionalStandardAssessment
        modelBuilder.Entity<FunctionalStandardAssessment>()
            .HasOne(fsa => fsa.FunctionalStandard)
            .WithMany()
            .HasForeignKey(fsa => fsa.FunctionalStandardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FunctionalStandardAssessment>()
            .HasMany(fsa => fsa.CriteriaResponses)
            .WithOne(acr => acr.Assessment)
            .HasForeignKey(acr => acr.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure AssessmentCriteriaResponse
        modelBuilder.Entity<AssessmentCriteriaResponse>()
            .HasOne(acr => acr.Criterion)
            .WithMany()
            .HasForeignKey(acr => new { acr.FunctionalStandardId, acr.ThemeId, acr.PracticeAreaId, acr.CriteriaCode })
            .HasPrincipalKey(c => new { c.FunctionalStandardId, c.ThemeId, c.PracticeAreaId, c.CriteriaCode })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssessmentCriteriaResponse>()
            .HasIndex(acr => new { acr.AssessmentId, acr.FunctionalStandardId, acr.ThemeId, acr.PracticeAreaId, acr.CriteriaCode })
            .IsUnique();

        // Configure EnterpriseMetric
        modelBuilder.Entity<EnterpriseMetric>()
            .HasIndex(em => em.Identifier)
            .IsUnique();

        // Configure EnterpriseReturn
        modelBuilder.Entity<EnterpriseReturn>()
            .HasIndex(er => new { er.Year, er.Month })
            .IsUnique();

        modelBuilder.Entity<EnterpriseReturn>()
            .HasMany(er => er.MetricValues)
            .WithOne(emv => emv.EnterpriseReturn)
            .HasForeignKey(emv => emv.EnterpriseReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure EnterpriseMetricValue
        modelBuilder.Entity<EnterpriseMetricValue>()
            .HasOne(emv => emv.EnterpriseMetric)
            .WithMany()
            .HasForeignKey(emv => emv.EnterpriseMetricId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EnterpriseMetricValue>()
            .HasIndex(emv => new { emv.EnterpriseReturnId, emv.EnterpriseMetricId })
            .IsUnique();

        // Configure RAID entities
        
        // Objective indexes
        modelBuilder.Entity<Objective>()
            .HasIndex(o => o.Status);
        
        modelBuilder.Entity<Objective>()
            .HasIndex(o => o.RagStatus);
        
        modelBuilder.Entity<Objective>()
            .HasIndex(o => o.OwnerUserId);

        // Risk configuration
        modelBuilder.Entity<Risk>()
            .HasOne(r => r.Objective)
            .WithMany(o => o.Risks)
            .HasForeignKey(r => r.ObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Risk>()
            .HasOne(r => r.RiskTier)
            .WithMany()
            .HasForeignKey(r => r.RiskTierId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.ObjectiveId);
        
        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.RiskTierId);

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.FipsId);

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.ProductDocumentId);

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.RiskScore)
            .IsDescending();

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.ProximityDate);

        // Issue configuration
        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Objective)
            .WithMany(o => o.Issues)
            .HasForeignKey(i => i.ObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.ObjectiveId);

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.FipsId);

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.ProductDocumentId);

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.Status);

        modelBuilder.Entity<Issue>()
            .HasIndex(i => new { i.Severity, i.Priority });

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.TargetResolutionDate);

        // Milestone configuration
        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.Objective)
            .WithMany(o => o.Milestones)
            .HasForeignKey(m => m.ObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.ObjectiveId);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.FipsId);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.ProductDocumentId);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.Status);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.DueDate);

        // KPI configuration
        modelBuilder.Entity<Kpi>()
            .Property(k => k.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.Code)
            .HasMaxLength(50);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.Category)
            .HasMaxLength(100);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.UnitOfMeasure)
            .HasMaxLength(50);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.Frequency)
            .HasMaxLength(50);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.ReportingStage)
            .HasMaxLength(200);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.Status)
            .HasMaxLength(50);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.AssignedToEntityId)
            .HasMaxLength(100);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.EntityType)
            .HasMaxLength(20);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.ProductDocumentId)
            .HasMaxLength(100);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.ProductFipsId)
            .HasMaxLength(50);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.Description)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Kpi>()
            .Property(k => k.CalculationMethod)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Kpi>()
            .Property(k => k.Thresholds)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Kpi>()
            .Property(k => k.DataSource)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Kpi>()
            .Property(k => k.ValidationRule)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.Code)
            .IsUnique();

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => new { k.AssignedToEntityId, k.EntityType });

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.ProjectId);

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.ObjectiveId);

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.MilestoneId);

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.OwnerUserId);

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.Active);

        modelBuilder.Entity<Kpi>()
            .HasIndex(k => k.ProductDocumentId);

        modelBuilder.Entity<Kpi>()
            .Property(k => k.TargetValue)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Kpi>()
            .HasOne(k => k.Project)
            .WithMany(p => p.Kpis)
            .HasForeignKey(k => k.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Kpi>()
            .HasOne(k => k.Objective)
            .WithMany(o => o.Kpis)
            .HasForeignKey(k => k.ObjectiveId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Kpi>()
            .HasOne(k => k.Milestone)
            .WithMany(m => m.Kpis)
            .HasForeignKey(k => k.MilestoneId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Kpi>()
            .HasOne(k => k.OwnerUser)
            .WithMany()
            .HasForeignKey(k => k.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // KPI performance data configuration
        modelBuilder.Entity<KpiDataPoint>()
            .HasOne(dp => dp.Kpi)
            .WithMany(k => k.PerformanceData)
            .HasForeignKey(dp => dp.KpiId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<KpiDataPoint>()
            .HasOne(dp => dp.SubmittedByUser)
            .WithMany()
            .HasForeignKey(dp => dp.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<KpiDataPoint>()
            .HasIndex(dp => dp.KpiId);

        modelBuilder.Entity<KpiDataPoint>()
            .HasIndex(dp => dp.ReportingPeriodStart);

        modelBuilder.Entity<KpiDataPoint>()
            .Property(dp => dp.SubmissionStatus)
            .HasMaxLength(30);

        modelBuilder.Entity<KpiDataPoint>()
            .Property(dp => dp.ValueNarrative)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<KpiDataPoint>()
            .Property(dp => dp.Notes)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<KpiDataPoint>()
            .Property(dp => dp.Value)
            .HasPrecision(20, 4);
 
        // Action configuration
        modelBuilder.Entity<Models.Action>()
            .HasOne(a => a.Objective)
            .WithMany(o => o.Actions)
            .HasForeignKey(a => a.ObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Action>()
            .HasOne(a => a.ParentAction)
            .WithMany(a => a.SubActions)
            .HasForeignKey(a => a.ParentActionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Action>()
            .HasOne(a => a.ActionSource)
            .WithMany()
            .HasForeignKey(a => a.ActionSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.ObjectiveId);
        
        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.ActionSourceId);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.FipsId);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.ProductDocumentId);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.AssignedToEmail);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => new { a.Status, a.Priority });

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.DueDate);

        modelBuilder.Entity<Models.Action>()
            .HasOne(a => a.Decision)
            .WithMany(d => d.Actions)
            .HasForeignKey(a => a.DecisionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Decision configuration
        modelBuilder.Entity<Decision>()
            .HasOne(d => d.Objective)
            .WithMany(o => o.Decisions)
            .HasForeignKey(d => d.ObjectiveId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Decision>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Decisions)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Decision>()
            .HasOne(d => d.OwnerUser)
            .WithMany()
            .HasForeignKey(d => d.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.ObjectiveId);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.ProjectId);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.OwnerUserId);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.FipsId);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.ProductDocumentId);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.Status);

        modelBuilder.Entity<Decision>()
            .HasIndex(d => d.DecisionDate);

        // Junction table configurations
        
        // RiskAction
        modelBuilder.Entity<RiskAction>()
            .HasKey(ra => new { ra.RiskId, ra.ActionId });

        modelBuilder.Entity<RiskAction>()
            .HasOne(ra => ra.Risk)
            .WithMany(r => r.RiskActions)
            .HasForeignKey(ra => ra.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RiskAction>()
            .HasOne(ra => ra.Action)
            .WithMany(a => a.RiskActions)
            .HasForeignKey(ra => ra.ActionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RiskAction>()
            .HasIndex(ra => ra.ActionId);

        // IssueAction
        modelBuilder.Entity<IssueAction>()
            .HasKey(ia => new { ia.IssueId, ia.ActionId });

        modelBuilder.Entity<IssueAction>()
            .HasOne(ia => ia.Issue)
            .WithMany(i => i.IssueActions)
            .HasForeignKey(ia => ia.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueAction>()
            .HasOne(ia => ia.Action)
            .WithMany(a => a.IssueActions)
            .HasForeignKey(ia => ia.ActionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueAction>()
            .HasIndex(ia => ia.ActionId);

        // RiskDecision
        modelBuilder.Entity<RiskDecision>()
            .HasKey(rd => new { rd.RiskId, rd.DecisionId });

        modelBuilder.Entity<RiskDecision>()
            .HasOne(rd => rd.Risk)
            .WithMany(r => r.RiskDecisions)
            .HasForeignKey(rd => rd.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RiskDecision>()
            .HasOne(rd => rd.Decision)
            .WithMany(d => d.RiskDecisions)
            .HasForeignKey(rd => rd.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RiskDecision>()
            .HasIndex(rd => rd.DecisionId);

        // IssueDecision
        modelBuilder.Entity<IssueDecision>()
            .HasKey(id => new { id.IssueId, id.DecisionId });

        modelBuilder.Entity<IssueDecision>()
            .HasOne(id => id.Issue)
            .WithMany(i => i.IssueDecisions)
            .HasForeignKey(id => id.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueDecision>()
            .HasOne(id => id.Decision)
            .WithMany(d => d.IssueDecisions)
            .HasForeignKey(id => id.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueDecision>()
            .HasIndex(id => id.DecisionId);

        // ActionDecision
        modelBuilder.Entity<ActionDecision>()
            .HasKey(ad => new { ad.ActionId, ad.DecisionId });

        modelBuilder.Entity<ActionDecision>()
            .HasOne(ad => ad.Action)
            .WithMany(a => a.ActionDecisions)
            .HasForeignKey(ad => ad.ActionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActionDecision>()
            .HasOne(ad => ad.Decision)
            .WithMany(d => d.ActionDecisions)
            .HasForeignKey(ad => ad.DecisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActionDecision>()
            .HasIndex(ad => ad.DecisionId);

        // IssueRisk
        modelBuilder.Entity<IssueRisk>()
            .HasKey(ir => new { ir.IssueId, ir.RiskId });

        modelBuilder.Entity<IssueRisk>()
            .HasOne(ir => ir.Issue)
            .WithMany(i => i.IssueRisks)
            .HasForeignKey(ir => ir.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueRisk>()
            .HasOne(ir => ir.Risk)
            .WithMany(r => r.IssueRisks)
            .HasForeignKey(ir => ir.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueRisk>()
            .HasIndex(ir => ir.RiskId);

        // MilestoneAction
        modelBuilder.Entity<MilestoneAction>()
            .HasKey(ma => new { ma.MilestoneId, ma.ActionId });

        modelBuilder.Entity<MilestoneAction>()
            .HasOne(ma => ma.Milestone)
            .WithMany(m => m.MilestoneActions)
            .HasForeignKey(ma => ma.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MilestoneAction>()
            .HasOne(ma => ma.Action)
            .WithMany(a => a.MilestoneActions)
            .HasForeignKey(ma => ma.ActionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MilestoneAction>()
            .HasIndex(ma => ma.ActionId);

        // MilestoneRisk
        modelBuilder.Entity<MilestoneRisk>()
            .HasKey(mr => new { mr.MilestoneId, mr.RiskId });

        modelBuilder.Entity<MilestoneRisk>()
            .HasOne(mr => mr.Milestone)
            .WithMany(m => m.MilestoneRisks)
            .HasForeignKey(mr => mr.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MilestoneRisk>()
            .HasOne(mr => mr.Risk)
            .WithMany(r => r.MilestoneRisks)
            .HasForeignKey(mr => mr.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MilestoneRisk>()
            .HasIndex(mr => mr.RiskId);

        // MilestoneIssue
        modelBuilder.Entity<MilestoneIssue>()
            .HasKey(mi => new { mi.MilestoneId, mi.IssueId });

        modelBuilder.Entity<MilestoneIssue>()
            .HasOne(mi => mi.Milestone)
            .WithMany(m => m.MilestoneIssues)
            .HasForeignKey(mi => mi.MilestoneId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MilestoneIssue>()
            .HasOne(mi => mi.Issue)
            .WithMany(i => i.MilestoneIssues)
            .HasForeignKey(mi => mi.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MilestoneIssue>()
            .HasIndex(mi => mi.IssueId);

        // Risk-RiskType junction table
        modelBuilder.Entity<RiskRiskType>()
            .HasKey(rrt => new { rrt.RiskId, rrt.RiskTypeId });

        modelBuilder.Entity<RiskRiskType>()
            .HasOne(rrt => rrt.Risk)
            .WithMany(r => r.RiskRiskTypes)
            .HasForeignKey(rrt => rrt.RiskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RiskRiskType>()
            .HasOne(rrt => rrt.RiskType)
            .WithMany()
            .HasForeignKey(rrt => rrt.RiskTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RiskRiskType>()
            .HasIndex(rrt => rrt.RiskTypeId);

        // RAID Lookups
        modelBuilder.Entity<RiskType>()
            .HasIndex(rt => rt.Code)
            .IsUnique();

        modelBuilder.Entity<RiskType>()
            .HasIndex(rt => rt.IsActive);

        modelBuilder.Entity<RiskTier>()
            .HasIndex(rt => rt.Code)
            .IsUnique();

        modelBuilder.Entity<RiskTier>()
            .HasIndex(rt => rt.IsActive);
        
        modelBuilder.Entity<RiskTier>()
            .HasIndex(rt => rt.SortOrder);

        modelBuilder.Entity<ActionSource>()
            .HasIndex(a_s => a_s.Code)
            .IsUnique();

        modelBuilder.Entity<ActionSource>()
            .HasIndex(a_s => a_s.IsActive);
        
        modelBuilder.Entity<ActionSource>()
            .HasIndex(a_s => a_s.SortOrder);

        // Project Lookups
        modelBuilder.Entity<BusinessAreaLookup>()
            .HasIndex(ba => ba.Name)
            .IsUnique();

        modelBuilder.Entity<BusinessAreaLookup>()
            .HasIndex(ba => ba.IsActive);

        modelBuilder.Entity<BusinessAreaLookup>()
            .HasIndex(ba => ba.SortOrder);

        modelBuilder.Entity<PhaseLookup>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<PhaseLookup>()
            .HasIndex(p => p.IsActive);

        modelBuilder.Entity<PhaseLookup>()
            .HasIndex(p => p.SortOrder);

        modelBuilder.Entity<KpiCategory>()
            .HasIndex(k => k.Code)
            .IsUnique();

        modelBuilder.Entity<KpiCategory>()
            .HasIndex(k => k.IsActive);

        modelBuilder.Entity<KpiCategory>()
            .HasIndex(k => k.SortOrder);

        modelBuilder.Entity<DeliveryPriority>()
            .HasIndex(dp => dp.Name)
            .IsUnique();

        modelBuilder.Entity<DeliveryPriority>()
            .HasIndex(dp => dp.IsActive);

        modelBuilder.Entity<DeliveryPriority>()
            .HasIndex(dp => dp.SortOrder);

        // User preferences
        modelBuilder.Entity<UserPreference>()
            .HasOne(up => up.User)
            .WithOne()
            .HasForeignKey<UserPreference>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // API Token configuration
        modelBuilder.Entity<ApiToken>()
            .HasIndex(at => at.Token)
            .IsUnique();

        modelBuilder.Entity<ApiToken>()
            .HasIndex(at => at.IsActive);

        modelBuilder.Entity<ApiToken>()
            .HasIndex(at => at.CreatedAt);

        modelBuilder.Entity<ApiToken>()
            .HasIndex(at => at.ExpiresAt);

        modelBuilder.Entity<ApiTokenPermission>()
            .HasOne(atp => atp.ApiToken)
            .WithMany(at => at.Permissions)
            .HasForeignKey(atp => atp.ApiTokenId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApiTokenPermission>()
            .HasIndex(atp => new { atp.ApiTokenId, atp.Resource })
            .IsUnique();

        modelBuilder.Entity<ApiRequestLog>()
            .HasOne(arl => arl.ApiToken)
            .WithMany(at => at.RequestLogs)
            .HasForeignKey(arl => arl.ApiTokenId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(arl => arl.RequestTimestamp);

        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(arl => arl.ApiTokenId);

        modelBuilder.Entity<ApiRequestLog>()
            .HasIndex(arl => arl.IsSuccess);

        // ProductAccessibility configuration
        // Note: Index is non-unique initially to allow NULLs during migration
        // Will be made unique after data migration populates DocumentIds
        modelBuilder.Entity<ProductAccessibility>()
            .HasIndex(pa => pa.ProductDocumentId);

        modelBuilder.Entity<ProductAccessibility>()
            .HasIndex(pa => pa.FipsId);

        // AccessibilityIssue configuration
        modelBuilder.Entity<AccessibilityIssue>()
            .Property(ai => ai.IssueDescription)
            .HasColumnType("nvarchar(max)");

        // IssueHistory configuration - allow unlimited length for OldValue and NewValue
        modelBuilder.Entity<IssueHistory>()
            .Property(ih => ih.OldValue)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<IssueHistory>()
            .Property(ih => ih.NewValue)
            .HasColumnType("nvarchar(max)");

        // Accessibility retest request configuration
        modelBuilder.Entity<AccessibilityRetestRequest>()
            .HasOne(rr => rr.AccessibilityIssue)
            .WithMany(ai => ai.RetestRequests)
            .HasForeignKey(rr => rr.AccessibilityIssueId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AccessibilityRetestRequest>()
            .HasIndex(rr => rr.AccessibilityIssueId);

        modelBuilder.Entity<AccessibilityRetestRequest>()
            .HasIndex(rr => rr.IsCompleted);

        modelBuilder.Entity<AccessibilityRetestRequest>()
            .HasIndex(rr => rr.RequestedAt);

        // Accessibility email configuration
        modelBuilder.Entity<AccessibilityEmailConfiguration>()
            .HasIndex(ec => ec.Purpose);

        modelBuilder.Entity<AccessibilityEmailConfiguration>()
            .HasIndex(ec => ec.IsActive);

        modelBuilder.Entity<AccessibilityEmailConfiguration>()
            .HasIndex(ec => new { ec.Purpose, ec.EmailAddress })
            .IsUnique();

        // StatementTemplate configuration
        modelBuilder.Entity<StatementTemplate>()
            .HasIndex(st => new { st.Name, st.Version })
            .IsUnique();

        modelBuilder.Entity<StatementTemplate>()
            .HasIndex(st => st.Name);

        modelBuilder.Entity<StatementTemplate>()
            .HasIndex(st => st.IsActive);

        modelBuilder.Entity<StatementTemplate>()
            .HasIndex(st => st.CreatedAt);

        modelBuilder.Entity<StatementTemplate>()
            .Property(st => st.Content)
            .HasMaxLength(int.MaxValue)
            .HasColumnType("nvarchar(max)");

        // ========================================
        // PROJECT MANAGEMENT CONFIGURATION
        // ========================================

        // Mission configuration
        modelBuilder.Entity<Mission>()
            .HasIndex(m => m.Status);

        modelBuilder.Entity<Mission>()
            .HasIndex(m => m.OwnerUserId);

        // Project configuration
        modelBuilder.Entity<Project>()
            .HasIndex(p => p.RagStatus);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Status);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.IsFlagship);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.StartDate);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.TargetDeliveryDate);

        // Configure decimal precision for FTE fields
        modelBuilder.Entity<Project>()
            .Property(p => p.TotalPermFte)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Project>()
            .Property(p => p.TotalMspFte)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Project>()
            .Property(p => p.ServiceUsers)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<Project>()
            .Property(p => p.Aim)
            .HasColumnType("nvarchar(max)");

        // ProjectRagHistory configuration
        modelBuilder.Entity<ProjectRagHistory>()
            .HasOne(prh => prh.Project)
            .WithMany(p => p.RagHistory)
            .HasForeignKey(prh => prh.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectRagHistory>()
            .HasIndex(prh => prh.ProjectId);

        modelBuilder.Entity<ProjectRagHistory>()
            .HasIndex(prh => prh.ChangedAt);

        // ProjectSuccess configuration
        modelBuilder.Entity<ProjectSuccess>()
            .HasOne(ps => ps.Project)
            .WithMany(p => p.Successes)
            .HasForeignKey(ps => ps.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectSuccess>()
            .HasIndex(ps => ps.ProjectId);

        modelBuilder.Entity<ProjectSuccess>()
            .HasIndex(ps => ps.RecordedAt);

        // ProjectOutcome configuration
        modelBuilder.Entity<ProjectOutcome>()
            .HasOne(po => po.Project)
            .WithMany(p => p.Outcomes)
            .HasForeignKey(po => po.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectOutcome>()
            .HasIndex(po => po.ProjectId);

        modelBuilder.Entity<ProjectOutcome>()
            .HasIndex(po => po.SortOrder);

        modelBuilder.Entity<ProjectOutcome>()
            .HasIndex(po => po.ConfidenceLevel);

        // ProjectMission configuration
        modelBuilder.Entity<ProjectMission>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectMissions)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMission>()
            .HasOne(pm => pm.Mission)
            .WithMany()
            .HasForeignKey(pm => pm.MissionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectMission>()
            .HasIndex(pm => pm.ProjectId);

        modelBuilder.Entity<ProjectMission>()
            .HasIndex(pm => pm.MissionId);

        // ProjectFundingAllocation configuration
        modelBuilder.Entity<ProjectFundingAllocation>()
            .HasOne(pfa => pfa.Project)
            .WithMany(p => p.FundingAllocations)
            .HasForeignKey(pfa => pfa.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectFundingAllocation>()
            .HasOne(pfa => pfa.FundingSource)
            .WithMany()
            .HasForeignKey(pfa => pfa.FundingSourceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectFundingAllocation>()
            .HasIndex(pfa => pfa.ProjectId);

        modelBuilder.Entity<ProjectFundingAllocation>()
            .HasIndex(pfa => pfa.FundingSourceId);

        // ProjectContact configuration
        modelBuilder.Entity<ProjectContact>()
            .HasOne(pc => pc.Project)
            .WithMany(p => p.ProjectContacts)
            .HasForeignKey(pc => pc.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectContact>()
            .HasOne(pc => pc.User)
            .WithMany()
            .HasForeignKey(pc => pc.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjectContact>()
            .HasIndex(pc => pc.ProjectId);

        modelBuilder.Entity<ProjectContact>()
            .HasIndex(pc => pc.SortOrder);

        modelBuilder.Entity<ProjectContact>()
            .HasIndex(pc => pc.Role);

        modelBuilder.Entity<ProjectContact>()
            .HasIndex(pc => pc.TeamStatus);

        // ProjectStatusUpdate configuration
        modelBuilder.Entity<ProjectStatusUpdate>()
            .HasOne(psu => psu.Project)
            .WithMany(p => p.StatusUpdates)
            .HasForeignKey(psu => psu.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectStatusUpdate>()
            .HasOne(psu => psu.CreatedByUser)
            .WithMany()
            .HasForeignKey(psu => psu.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<ProjectStatusUpdate>()
            .HasOne(psu => psu.UpdatedByUser)
            .WithMany()
            .HasForeignKey(psu => psu.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjectStatusUpdate>()
            .HasIndex(psu => psu.ProjectId);

        modelBuilder.Entity<ProjectStatusUpdate>()
            .HasIndex(psu => psu.CreatedAt);

        modelBuilder.Entity<ProjectStatusUpdate>()
            .Property(psu => psu.Narrative)
            .HasColumnType("nvarchar(max)");

        // MonthlyStatusReport configuration
        modelBuilder.Entity<MonthlyStatusReport>()
            .HasOne(msr => msr.Project)
            .WithMany()
            .HasForeignKey(msr => msr.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MonthlyStatusReport>()
            .HasOne(msr => msr.CreatedByUser)
            .WithMany()
            .HasForeignKey(msr => msr.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<MonthlyStatusReport>()
            .HasIndex(msr => new { msr.ProjectId, msr.ReportingYear, msr.ReportingMonth })
            .IsUnique();

        modelBuilder.Entity<MonthlyStatusReport>()
            .HasIndex(msr => new { msr.ReportingYear, msr.ReportingMonth });

        modelBuilder.Entity<MonthlyStatusReport>()
            .Property(msr => msr.Narrative)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<MonthlyStatusReport>()
            .Property(msr => msr.MilestoneProgress)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<MonthlyStatusReport>()
            .Property(msr => msr.DeliverableProgress)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<MonthlyStatusReport>()
            .Property(msr => msr.KeyAchievements)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<MonthlyStatusReport>()
            .Property(msr => msr.Challenges)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<MonthlyStatusReport>()
            .Property(msr => msr.NextMonthOutlook)
            .HasColumnType("nvarchar(max)");

        // MonthlyStatusReportTimescaleConfig configuration
        modelBuilder.Entity<MonthlyStatusReportTimescaleConfig>()
            .HasIndex(tsc => tsc.IsDefault)
            .HasFilter("[IsDefault] = 1");

        modelBuilder.Entity<MonthlyStatusReportTimescaleConfig>()
            .HasIndex(tsc => tsc.IsActive);

        // ProjectArtefact configuration
        modelBuilder.Entity<ProjectArtefact>()
            .HasOne(pa => pa.Project)
            .WithMany(p => p.Artefacts)
            .HasForeignKey(pa => pa.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectArtefact>()
            .HasOne(pa => pa.CreatedByUser)
            .WithMany()
            .HasForeignKey(pa => pa.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        modelBuilder.Entity<ProjectArtefact>()
            .HasOne(pa => pa.UpdatedByUser)
            .WithMany()
            .HasForeignKey(pa => pa.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProjectArtefact>()
            .HasIndex(pa => pa.ProjectId);

        modelBuilder.Entity<ProjectArtefact>()
            .HasIndex(pa => pa.CreatedAt);

        modelBuilder.Entity<ProjectArtefact>()
            .HasIndex(pa => pa.IsDeleted);

        modelBuilder.Entity<ProjectArtefact>()
            .Property(pa => pa.Description)
            .HasColumnType("nvarchar(max)");

        // ProjectSeniorResponsibleOfficer configuration
        modelBuilder.Entity<ProjectSeniorResponsibleOfficer>()
            .HasOne(psro => psro.Project)
            .WithMany(p => p.SeniorResponsibleOfficers)
            .HasForeignKey(psro => psro.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectSeniorResponsibleOfficer>()
            .HasOne(psro => psro.User)
            .WithMany()
            .HasForeignKey(psro => psro.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectSeniorResponsibleOfficer>()
            .HasIndex(psro => psro.ProjectId);

        modelBuilder.Entity<ProjectSeniorResponsibleOfficer>()
            .HasIndex(psro => new { psro.ProjectId, psro.UserId })
            .IsUnique();

        // ProjectServiceOwner configuration
        modelBuilder.Entity<ProjectServiceOwner>()
            .HasOne(pso => pso.Project)
            .WithMany(p => p.ServiceOwners)
            .HasForeignKey(pso => pso.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectServiceOwner>()
            .HasOne(pso => pso.User)
            .WithMany()
            .HasForeignKey(pso => pso.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectServiceOwner>()
            .HasIndex(pso => pso.ProjectId);

        modelBuilder.Entity<ProjectServiceOwner>()
            .HasIndex(pso => new { pso.ProjectId, pso.UserId })
            .IsUnique();

        // ProjectDirectorate configuration
        modelBuilder.Entity<ProjectDirectorate>()
            .HasOne(pd => pd.Project)
            .WithMany(p => p.Directorates)
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectDirectorate>()
            .HasOne(pd => pd.DirectorateLookup)
            .WithMany()
            .HasForeignKey(pd => pd.DirectorateLookupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectDirectorate>()
            .HasIndex(pd => pd.ProjectId);

        modelBuilder.Entity<ProjectDirectorate>()
            .HasIndex(pd => new { pd.ProjectId, pd.DirectorateLookupId })
            .IsUnique();

        // ProjectBudgetOwner configuration
        modelBuilder.Entity<ProjectBudgetOwner>()
            .HasOne(pbo => pbo.Project)
            .WithMany(p => p.BudgetOwners)
            .HasForeignKey(pbo => pbo.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectBudgetOwner>()
            .HasOne(pbo => pbo.BusinessAreaLookup)
            .WithMany()
            .HasForeignKey(pbo => pbo.BusinessAreaLookupId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectBudgetOwner>()
            .HasIndex(pbo => pbo.ProjectId);

        modelBuilder.Entity<ProjectBudgetOwner>()
            .HasIndex(pbo => new { pbo.ProjectId, pbo.BusinessAreaLookupId })
            .IsUnique();

        // ProjectPmoContact configuration
        modelBuilder.Entity<ProjectPmoContact>()
            .HasOne(ppc => ppc.Project)
            .WithMany(p => p.PmoContacts)
            .HasForeignKey(ppc => ppc.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectPmoContact>()
            .HasOne(ppc => ppc.User)
            .WithMany()
            .HasForeignKey(ppc => ppc.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectPmoContact>()
            .HasIndex(ppc => ppc.ProjectId);

        modelBuilder.Entity<ProjectPmoContact>()
            .HasIndex(ppc => new { ppc.ProjectId, ppc.UserId })
            .IsUnique();

        // ActivityTypeLookup configuration
        modelBuilder.Entity<ActivityTypeLookup>()
            .HasIndex(at => at.Name)
            .IsUnique();

        modelBuilder.Entity<ActivityTypeLookup>()
            .HasIndex(at => at.IsActive);

        // DirectorateLookup configuration
        modelBuilder.Entity<DirectorateLookup>()
            .HasIndex(dl => dl.Name)
            .IsUnique();

        modelBuilder.Entity<DirectorateLookup>()
            .HasIndex(dl => dl.IsActive);

        // RiskAppetiteLookup configuration
        modelBuilder.Entity<RiskAppetiteLookup>()
            .HasIndex(ral => ral.Name)
            .IsUnique();

        modelBuilder.Entity<RiskAppetiteLookup>()
            .HasIndex(ral => ral.IsActive);

        // ProjectObjective configuration
        modelBuilder.Entity<ProjectObjective>()
            .HasOne(po => po.Project)
            .WithMany(p => p.ProjectObjectives)
            .HasForeignKey(po => po.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectObjective>()
            .HasOne(po => po.Objective)
            .WithMany()
            .HasForeignKey(po => po.ObjectiveId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectObjective>()
            .HasIndex(po => po.ProjectId);

        modelBuilder.Entity<ProjectObjective>()
            .HasIndex(po => po.ObjectiveId);

        // Dependency configuration
        modelBuilder.Entity<Dependency>()
            .HasIndex(d => new { d.SourceEntityType, d.SourceEntityId });

        modelBuilder.Entity<Dependency>()
            .HasIndex(d => new { d.TargetEntityType, d.TargetEntityId });

        modelBuilder.Entity<Dependency>()
            .HasIndex(d => d.Status);

        modelBuilder.Entity<Dependency>()
            .HasIndex(d => d.DependencyType);

        // ProjectProduct configuration
        modelBuilder.Entity<ProjectProduct>()
            .HasOne(pp => pp.Project)
            .WithMany(p => p.ProjectProducts)
            .HasForeignKey(pp => pp.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectProduct>()
            .HasIndex(pp => pp.ProjectId);

        modelBuilder.Entity<ProjectProduct>()
            .HasIndex(pp => pp.ProductDocumentId);

        modelBuilder.Entity<ProjectProduct>()
            .HasIndex(pp => pp.ProductFipsId);

        // ProjectDraft configuration
        modelBuilder.Entity<ProjectDraft>()
            .HasIndex(pd => pd.SessionId);

        modelBuilder.Entity<ProjectDraft>()
            .HasIndex(pd => pd.UserEmail);

        modelBuilder.Entity<ProjectDraft>()
            .HasIndex(pd => pd.IsConfirmed);

        modelBuilder.Entity<ProjectDraft>()
            .HasIndex(pd => pd.CreatedAt);

        // Configure decimal precision for FTE fields
        modelBuilder.Entity<ProjectDraft>()
            .Property(pd => pd.TotalPermFte)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ProjectDraft>()
            .Property(pd => pd.TotalMspFte)
            .HasPrecision(10, 2);

        // FundingSource configuration
        modelBuilder.Entity<FundingSource>()
            .HasIndex(fs => fs.Code)
            .IsUnique();

        modelBuilder.Entity<FundingSource>()
            .HasIndex(fs => fs.IsActive);

        modelBuilder.Entity<FundingSource>()
            .HasIndex(fs => fs.SortOrder);

        // Update existing models to include Project relationships
        modelBuilder.Entity<Milestone>()
            .HasOne(m => m.Project)
            .WithMany(p => p.Milestones)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.ProjectId);

        modelBuilder.Entity<Risk>()
            .HasOne(r => r.Project)
            .WithMany(p => p.Risks)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Risk>()
            .HasIndex(r => r.ProjectId);

        modelBuilder.Entity<Issue>()
            .HasOne(i => i.Project)
            .WithMany(p => p.Issues)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Issue>()
            .HasIndex(i => i.ProjectId);

        modelBuilder.Entity<Models.Action>()
            .HasOne(a => a.Project)
            .WithMany(p => p.Actions)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.ProjectId);

        // Update Objective to include Mission relationship
        modelBuilder.Entity<Objective>()
            .HasOne(o => o.Mission)
            .WithMany(m => m.Objectives)
            .HasForeignKey(o => o.MissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Objective>()
            .HasIndex(o => o.MissionId);
        
        // Configure StaffRoleReturn
        modelBuilder.Entity<StaffRoleReturn>()
            .HasOne(srr => srr.User)
            .WithMany()
            .HasForeignKey(srr => srr.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<StaffRoleReturn>()
            .HasOne(srr => srr.GddRole)
            .WithMany(role => role.StaffRoleReturns)
            .HasForeignKey(srr => srr.GddRoleId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<StaffRoleReturn>()
            .HasIndex(srr => srr.UserId);
        
        modelBuilder.Entity<StaffRoleReturn>()
            .HasIndex(srr => new { srr.UserId, srr.Year })
            .IsUnique();
        
        modelBuilder.Entity<StaffRoleReturn>()
            .HasMany(srr => srr.SecondarySkills)
            .WithOne(srr => srr.StaffRoleReturn)
            .HasForeignKey(srr => srr.StaffRoleReturnId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure StaffRoleReturnSkill
        modelBuilder.Entity<StaffRoleReturnSkill>()
            .HasOne(srs => srs.Skill)
            .WithMany(s => s.StaffRoleReturns)
            .HasForeignKey(srs => srs.SkillId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<StaffRoleReturnSkill>()
            .HasIndex(srs => new { srs.StaffRoleReturnId, srs.SkillId })
            .IsUnique();
        
        // Configure GddRole
        modelBuilder.Entity<GddRole>()
            .HasIndex(role => new { role.RoleFamily, role.RoleName, role.RoleLevel })
            .IsUnique();
        
        // GddRole.Description needs to be unlimited (nvarchar(max)) for long descriptions from CSV
        modelBuilder.Entity<GddRole>()
            .Property(r => r.Description)
            .HasMaxLength(int.MaxValue) // Override default MaxLength(450)
            .HasColumnType("nvarchar(max)");
        
        // Configure Skill
        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.SkillName)
            .IsUnique();
        
        // Skill.Description needs to be unlimited (nvarchar(max)) for long descriptions from CSV
        modelBuilder.Entity<Skill>()
            .Property(s => s.Description)
            .HasMaxLength(int.MaxValue) // Override default MaxLength(450)
            .HasColumnType("nvarchar(max)");

        // ========================================
        // ROLE-BASED ACCESS CONTROL CONFIGURATION
        // ========================================

        // Group configuration
        modelBuilder.Entity<Group>()
            .HasIndex(g => g.Name)
            .IsUnique();

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.IsActive);

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.IsSystemGroup);

        // Feature configuration
        modelBuilder.Entity<Feature>()
            .HasIndex(f => f.Code)
            .IsUnique();

        modelBuilder.Entity<Feature>()
            .HasIndex(f => f.Name);

        modelBuilder.Entity<Feature>()
            .HasIndex(f => f.IsActive);

        // UserGroup configuration (many-to-many: User ↔ Group)
        modelBuilder.Entity<UserGroup>()
            .HasIndex(ug => new { ug.UserId, ug.GroupId })
            .IsUnique();

        modelBuilder.Entity<UserGroup>()
            .HasOne(ug => ug.User)
            .WithMany(u => u.UserGroups)
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGroup>()
            .HasOne(ug => ug.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserGroup>()
            .HasIndex(ug => ug.UserId);

        modelBuilder.Entity<UserGroup>()
            .HasIndex(ug => ug.GroupId);

        // GroupFeaturePermission configuration (many-to-many: Group ↔ Feature with Permission)
        modelBuilder.Entity<GroupFeaturePermission>()
            .HasIndex(gfp => new { gfp.GroupId, gfp.FeatureId, gfp.Permission })
            .IsUnique();

        modelBuilder.Entity<GroupFeaturePermission>()
            .HasOne(gfp => gfp.Group)
            .WithMany(g => g.GroupFeaturePermissions)
            .HasForeignKey(gfp => gfp.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupFeaturePermission>()
            .HasOne(gfp => gfp.Feature)
            .WithMany(f => f.GroupFeaturePermissions)
            .HasForeignKey(gfp => gfp.FeatureId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupFeaturePermission>()
            .HasIndex(gfp => gfp.GroupId);

        modelBuilder.Entity<GroupFeaturePermission>()
            .HasIndex(gfp => gfp.FeatureId);

        modelBuilder.Entity<GroupFeaturePermission>()
            .HasIndex(gfp => gfp.Permission);
        
        // ========================================
        // DEMAND MANAGEMENT CONFIGURATION
        // ========================================
        
        // DemandRequest configuration
        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.ReferenceNumber)
            .IsUnique();
        
        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.Status);
        
        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.AssignedToEmail);
        
        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.SubmittedAt);
        
        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.ApplicantEmail);

        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.TriageMeetingId);

        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.IsSubmittedToTriage);

        modelBuilder.Entity<DemandRequest>()
            .HasIndex(dr => dr.ConvertedProjectId);

        modelBuilder.Entity<DemandRequest>()
            .HasOne(dr => dr.TriageMeeting)
            .WithMany(tm => tm.DemandRequests)
            .HasForeignKey(dr => dr.TriageMeetingId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<DemandRequest>()
            .HasOne(dr => dr.ConvertedProject)
            .WithMany()
            .HasForeignKey(dr => dr.ConvertedProjectId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // DemandRequestContact configuration
        modelBuilder.Entity<DemandRequestContact>()
            .HasOne(drc => drc.DemandRequest)
            .WithMany(dr => dr.Contacts)
            .HasForeignKey(drc => drc.DemandRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DemandRequestContact>()
            .HasIndex(drc => drc.DemandRequestId);
        
        // DemandRequestPrioritisation configuration
        modelBuilder.Entity<DemandRequestPrioritisation>()
            .HasOne(drp => drp.DemandRequest)
            .WithOne(dr => dr.Prioritisation)
            .HasForeignKey<DemandRequestPrioritisation>(drp => drp.DemandRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DemandRequestPrioritisation>()
            .HasIndex(drp => drp.TotalPriorityScore)
            .IsDescending();
        
        modelBuilder.Entity<DemandRequestPrioritisation>()
            .HasIndex(drp => drp.PriorityTier);
        
        // DemandRequestNote configuration
        modelBuilder.Entity<DemandRequestNote>()
            .HasOne(drn => drn.DemandRequest)
            .WithMany(dr => dr.Notes)
            .HasForeignKey(drn => drn.DemandRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DemandRequestNote>()
            .HasIndex(drn => drn.DemandRequestId);
        
        // DemandRequestAssessment configuration
        modelBuilder.Entity<DemandRequestAssessment>()
            .HasOne(dra => dra.DemandRequest)
            .WithMany(dr => dr.Assessments)
            .HasForeignKey(dra => dra.DemandRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DemandRequestAssessment>()
            .HasIndex(dra => new { dra.DemandRequestId, dra.AssessmentType });
        
        // DemandRequestSectionCompletion configuration
        modelBuilder.Entity<DemandRequestSectionCompletion>()
            .HasOne(drsc => drsc.DemandRequest)
            .WithMany(dr => dr.SectionCompletions)
            .HasForeignKey(drsc => drsc.DemandRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DemandRequestSectionCompletion>()
            .HasIndex(drsc => new { drsc.DemandRequestId, drsc.SectionName })
            .IsUnique();

        // DemandRequestRiskType configuration
        modelBuilder.Entity<DemandRequestRiskType>()
            .HasKey(drrt => new { drrt.DemandRequestId, drrt.RiskTypeId });

        modelBuilder.Entity<DemandRequestRiskType>()
            .HasOne(drrt => drrt.DemandRequest)
            .WithMany(dr => dr.RiskTypeLinks)
            .HasForeignKey(drrt => drrt.DemandRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DemandRequestRiskType>()
            .HasOne(drrt => drrt.RiskType)
            .WithMany(rt => rt.DemandRequestLinks)
            .HasForeignKey(drrt => drrt.RiskTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DemandRequestRiskType>()
            .HasIndex(drrt => drrt.RiskTypeId);

        modelBuilder.Entity<DemandRequestRiskType>()
            .Property(drrt => drrt.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
 
        // TriageMeeting configuration
        modelBuilder.Entity<TriageMeeting>()
            .HasIndex(tm => tm.StartAt);

        modelBuilder.Entity<TriageMeeting>()
            .HasIndex(tm => tm.EndAt);

        modelBuilder.Entity<TriageMeeting>()
            .HasIndex(tm => tm.IsActive);

        modelBuilder.Entity<TriageMeeting>()
            .Property(tm => tm.Title)
            .HasMaxLength(150);

        // DdatProfession configuration - ignore RoleGroup property until migration is created
        modelBuilder.Entity<DdatProfession>()
            .Ignore(d => d.RoleGroup);

        // ========================================
        // LEARNING & DEVELOPMENT (L&D) CONFIGURATION
        // ========================================

        // TrainingCourse configuration
        modelBuilder.Entity<TrainingCourse>()
            .HasIndex(tc => tc.Active);

        modelBuilder.Entity<TrainingCourse>()
            .HasIndex(tc => tc.Title);

        modelBuilder.Entity<TrainingCourse>()
            .HasIndex(tc => tc.Provider);

        // TrainingRecord configuration
        modelBuilder.Entity<TrainingRecord>()
            .HasOne(tr => tr.User)
            .WithMany()
            .HasForeignKey(tr => tr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingRecord>()
            .HasOne(tr => tr.Course)
            .WithMany(tc => tc.TrainingRecords)
            .HasForeignKey(tr => tr.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingRecord>()
            .HasIndex(tr => tr.UserId);

        modelBuilder.Entity<TrainingRecord>()
            .HasIndex(tr => tr.CourseId);

        modelBuilder.Entity<TrainingRecord>()
            .HasIndex(tr => tr.Status);

        modelBuilder.Entity<TrainingRecord>()
            .HasIndex(tr => tr.DateAttended);

        // TrainingRequest configuration
        modelBuilder.Entity<TrainingRequest>()
            .HasOne(tr => tr.User)
            .WithMany()
            .HasForeignKey(tr => tr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingRequest>()
            .HasOne(tr => tr.Course)
            .WithMany(tc => tc.TrainingRequests)
            .HasForeignKey(tr => tr.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingRequest>()
            .HasOne(tr => tr.Decision)
            .WithMany()
            .HasForeignKey(tr => tr.DecisionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TrainingRequest>()
            .HasIndex(tr => tr.UserId);

        modelBuilder.Entity<TrainingRequest>()
            .HasIndex(tr => tr.CourseId);

        modelBuilder.Entity<TrainingRequest>()
            .HasIndex(tr => tr.Status);

        modelBuilder.Entity<TrainingRequest>()
            .HasIndex(tr => tr.CreatedAt);

        // UserProfessionalProfile configuration
        modelBuilder.Entity<UserProfessionalProfile>()
            .HasOne(upp => upp.User)
            .WithMany()
            .HasForeignKey(upp => upp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserProfessionalProfile>()
            .HasIndex(upp => upp.UserId)
            .IsUnique();

        modelBuilder.Entity<UserProfessionalProfile>()
            .HasIndex(upp => upp.Profession);

        // HOPS configuration
        modelBuilder.Entity<HOPS>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HOPS>()
            .HasIndex(h => h.UserId);

        modelBuilder.Entity<HOPS>()
            .HasIndex(h => h.Profession);

        modelBuilder.Entity<HOPS>()
            .HasIndex(h => new { h.UserId, h.Profession })
            .IsUnique();

        // TrainingNudge configuration
        modelBuilder.Entity<TrainingNudge>()
            .HasOne(tn => tn.User)
            .WithMany()
            .HasForeignKey(tn => tn.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainingNudge>()
            .HasOne(tn => tn.Course)
            .WithMany()
            .HasForeignKey(tn => tn.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TrainingNudge>()
            .HasIndex(tn => tn.UserId);

        modelBuilder.Entity<TrainingNudge>()
            .HasIndex(tn => new { tn.UserId, tn.IsActive });

        // LearningBudget configuration
        modelBuilder.Entity<LearningBudget>()
            .HasIndex(lb => new { lb.FinancialYear, lb.IsActive })
            .IsUnique()
            .HasFilter("[IsActive] = 1"); // Only one active budget per FY

        modelBuilder.Entity<LearningBudget>()
            .HasIndex(lb => lb.FinancialYear);

    }
}

file sealed class NullAuditContextProvider : IAuditContextProvider
{
    public string? UserId => null;
    public string? UserEmail => null;
    public string? UserName => null;
    public string? IpAddress => null;
    public string? UserAgent => null;
}

