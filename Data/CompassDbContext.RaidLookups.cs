using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Data;

public partial class CompassDbContext
{
    public DbSet<ActionStatus> ActionStatuses { get; set; }
    public DbSet<ActionPriority> ActionPriorities { get; set; }
    public DbSet<ActionType> ActionTypes { get; set; }
    public DbSet<ActionCategory> ActionCategories { get; set; }
    public DbSet<ActionImpactLevel> ActionImpactLevels { get; set; }
    public DbSet<ActionReminderFrequency> ActionReminderFrequencies { get; set; }
    public DbSet<ActionEscalationThreshold> ActionEscalationThresholds { get; set; }
    public DbSet<RaidEvidenceType> RaidEvidenceTypes { get; set; }
    public DbSet<GovernanceBoard> GovernanceBoards { get; set; }

    public DbSet<RiskStatus> RiskStatuses { get; set; }
    public DbSet<RiskPriority> RiskPriorities { get; set; }
    public DbSet<RiskLikelihood> RiskLikelihoods { get; set; }
    public DbSet<RiskImpactLevel> RiskImpactLevels { get; set; }
    public DbSet<RiskProximity> RiskProximities { get; set; }
    public DbSet<RiskCategory> RiskCategories { get; set; }

    public DbSet<IssueStatus> IssueStatuses { get; set; }
    public DbSet<IssuePriority> IssuePriorities { get; set; }
    public DbSet<IssueSeverity> IssueSeverities { get; set; }
    public DbSet<IssueCategory> IssueCategories { get; set; }

    public DbSet<DecisionStatus> DecisionStatuses { get; set; }
    public DbSet<DecisionPriority> DecisionPriorities { get; set; }
    public DbSet<DecisionOutcome> DecisionOutcomes { get; set; }
    public DbSet<DecisionImplementationStatus> DecisionImplementationStatuses { get; set; }

    public DbSet<ActionTag> ActionTags { get; set; }
    public DbSet<IssueTag> IssueTags { get; set; }
    public DbSet<RiskTag> RiskTags { get; set; }
    public DbSet<DecisionTag> DecisionTags { get; set; }
}
