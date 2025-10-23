using Microsoft.EntityFrameworkCore;
using Compass.Models;

namespace Compass.Data;

public class CompassDbContext : DbContext
{
    public CompassDbContext(DbContextOptions<CompassDbContext> options) : base(options)
    {
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
    public DbSet<UserPreference> UserPreferences { get; set; }
    
    // API Management
    public DbSet<ApiToken> ApiTokens { get; set; }
    public DbSet<ApiTokenPermission> ApiTokenPermissions { get; set; }
    public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
    
    // Performance metrics
    public DbSet<PerformanceMetric> PerformanceMetrics { get; set; }
    
    // Functional standards
    public DbSet<FunctionalStandard> FunctionalStandards { get; set; }
    public DbSet<FunctionalStandardTheme> FunctionalStandardThemes { get; set; }
    public DbSet<PracticeArea> PracticeAreas { get; set; }
    public DbSet<Criterion> Criteria { get; set; }
    
    // Product reporting
    public DbSet<ProductReturn> ProductReturns { get; set; }
    public DbSet<ProductMetricValue> ProductMetricValues { get; set; }
    
    // Enterprise reporting - Functional Standard Assessments
    public DbSet<FunctionalStandardAssessment> FunctionalStandardAssessments { get; set; }
    public DbSet<AssessmentCriteriaResponse> AssessmentCriteriaResponses { get; set; }
    
    // Enterprise reporting - Enterprise Metrics
    public DbSet<EnterpriseMetric> EnterpriseMetrics { get; set; }
    public DbSet<EnterpriseReturn> EnterpriseReturns { get; set; }
    public DbSet<EnterpriseMetricValue> EnterpriseMetricValues { get; set; }
    
    // Product Governance
    public DbSet<Objective> Objectives { get; set; }
    public DbSet<Risk> Risks { get; set; }
    public DbSet<Issue> Issues { get; set; }
    public DbSet<Milestone> Milestones { get; set; }
    public DbSet<Models.Action> Actions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    
    // Project Management
    public DbSet<Project> Projects { get; set; }
    public DbSet<Mission> Missions { get; set; }
    public DbSet<FundingSource> FundingSources { get; set; }
    public DbSet<ProjectRagHistory> ProjectRagHistories { get; set; }
    public DbSet<ProjectSuccess> ProjectSuccesses { get; set; }
    public DbSet<ProjectOutcome> ProjectOutcomes { get; set; }
    public DbSet<ProjectMission> ProjectMissions { get; set; }
    public DbSet<ProjectFundingAllocation> ProjectFundingAllocations { get; set; }
    public DbSet<ProjectContact> ProjectContacts { get; set; }
    public DbSet<ProjectObjective> ProjectObjectives { get; set; }
    public DbSet<Dependency> Dependencies { get; set; }
    public DbSet<ProjectProduct> ProjectProducts { get; set; }
    public DbSet<ProjectDraft> ProjectDrafts { get; set; }
    
    // RAID Lookups
    public DbSet<RiskType> RiskTypes { get; set; }
    public DbSet<RiskTier> RiskTiers { get; set; }
    public DbSet<ActionSource> ActionSources { get; set; }
    
    // RAID Junction Tables
    public DbSet<RiskAction> RiskActions { get; set; }
    public DbSet<RiskRiskType> RiskRiskTypes { get; set; }
    public DbSet<IssueAction> IssueActions { get; set; }
    public DbSet<MilestoneAction> MilestoneActions { get; set; }
    public DbSet<MilestoneRisk> MilestoneRisks { get; set; }
    public DbSet<MilestoneIssue> MilestoneIssues { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

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
        modelBuilder.Entity<ProductReturn>()
            .HasIndex(pr => new { pr.FipsId, pr.Year, pr.Month })
            .IsUnique();

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
            .HasIndex(m => m.Status);

        modelBuilder.Entity<Milestone>()
            .HasIndex(m => m.DueDate);

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
            .HasIndex(a => a.AssignedToEmail);

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => new { a.Status, a.Priority });

        modelBuilder.Entity<Models.Action>()
            .HasIndex(a => a.DueDate);

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
            .HasIndex(pc => pc.ProjectId);

        modelBuilder.Entity<ProjectContact>()
            .HasIndex(pc => pc.SortOrder);

        modelBuilder.Entity<ProjectContact>()
            .HasIndex(pc => pc.Role);

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
    }
}

