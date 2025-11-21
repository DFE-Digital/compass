using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [MaxLength(20)]
    public string ProjectCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Aim { get; set; }

    public string? StrategicObjectives { get; set; }

    public string? MissionPillars { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? TargetDeliveryDate { get; set; }

    public DateTime? ActualDeliveryDate { get; set; }

    [Required]
    public bool IsFlagship { get; set; } = false;

    [Required]
    public bool IsAiInitiative { get; set; } = false;

    [MaxLength(20)]
    public string? RagStatus { get; set; } // Green, Amber-Green, Amber, Amber-Red, Red

    public string? RagJustification { get; set; }

    public string? PathToGreen { get; set; }

    [MaxLength(50)]
    public string? Phase { get; set; } // Discovery, Alpha, Private beta, Public beta, Live

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    [MaxLength(100)]
    public string? HistoricBuRTId { get; set; }

    public int? PrimaryContactUserId { get; set; }

    [ForeignKey(nameof(PrimaryContactUserId))]
    public User? PrimaryContactUser { get; set; }

    public int? DeliveryPriorityId { get; set; }

    [ForeignKey(nameof(DeliveryPriorityId))]
    public DeliveryPriority? DeliveryPriority { get; set; }

    [MaxLength(500)]
    public string? DeliveryPriorityChangeReason { get; set; }

    // Organizational structure fields
    public int? PrimaryOrganizationalGroupId { get; set; }
    public OrganizationalGroup? PrimaryOrganizationalGroup { get; set; }
    
    public bool IsMultiDepartmentProject { get; set; } = false;
    
    public string? OtherDepartments { get; set; } // JSON array of government department IDs

    public decimal? TotalPermFte { get; set; }

    public decimal? TotalMspFte { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; } = "Active"; // Active, Paused, Completed, Cancelled

    public string? StatusChangeReason { get; set; }

    public bool IsDeleted { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(20)]
    public string? CreationMethod { get; set; } // Manual, Bulk

    // Navigation properties
    public ICollection<ProjectSuccess> Successes { get; set; } = new List<ProjectSuccess>();
    public ICollection<ProjectRagHistory> RagHistory { get; set; } = new List<ProjectRagHistory>();
    public ICollection<ProjectOutcome> Outcomes { get; set; } = new List<ProjectOutcome>();
    public ICollection<ProjectNeed> Needs { get; set; } = new List<ProjectNeed>();
    public ICollection<ProjectProblemStatement> ProblemStatements { get; set; } = new List<ProjectProblemStatement>();
    public ICollection<ProjectMission> ProjectMissions { get; set; } = new List<ProjectMission>();
    public ICollection<ProjectFundingAllocation> FundingAllocations { get; set; } = new List<ProjectFundingAllocation>();
    public ICollection<ProjectResourceFunding> ResourceFunding { get; set; } = new List<ProjectResourceFunding>();
    public ICollection<ProjectResourceFundingHistory> FundingHistory { get; set; } = new List<ProjectResourceFundingHistory>();
    public ICollection<ProjectContact> ProjectContacts { get; set; } = new List<ProjectContact>();
    public ICollection<ProjectObjective> ProjectObjectives { get; set; } = new List<ProjectObjective>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();

    [NotMapped]
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();

    public ICollection<ProjectProduct> ProjectProducts { get; set; } = new List<ProjectProduct>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Risk> Risks { get; set; } = new List<Risk>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Models.Action> Actions { get; set; } = new List<Models.Action>();
    public ICollection<Decision> Decisions { get; set; } = new List<Decision>();
    public ICollection<Kpi> Kpis { get; set; } = new List<Kpi>();

    // New fields for enhanced project tracking
    public ICollection<ProjectStatusUpdate> StatusUpdates { get; set; } = new List<ProjectStatusUpdate>();
    public ICollection<ProjectSeniorResponsibleOfficer> SeniorResponsibleOfficers { get; set; } = new List<ProjectSeniorResponsibleOfficer>();
    public ICollection<ProjectDirectorate> Directorates { get; set; } = new List<ProjectDirectorate>();
    public ICollection<ProjectBudgetOwner> BudgetOwners { get; set; } = new List<ProjectBudgetOwner>();
    public ICollection<ProjectPmoContact> PmoContacts { get; set; } = new List<ProjectPmoContact>();
    public ICollection<ProjectArtefact> Artefacts { get; set; } = new List<ProjectArtefact>();

    // Activity Type
    public int? ActivityTypeLookupId { get; set; }
    [ForeignKey(nameof(ActivityTypeLookupId))]
    public ActivityTypeLookup? ActivityTypeLookup { get; set; }

    // Risk Appetite
    public int? RiskAppetiteLookupId { get; set; }
    [ForeignKey(nameof(RiskAppetiteLookupId))]
    public RiskAppetiteLookup? RiskAppetiteLookup { get; set; }

    // Service Users (free text)
    public string? ServiceUsers { get; set; }

    // Internal/External flags
    public bool IsInternal { get; set; } = false;
    public bool IsExternal { get; set; } = false;

    public bool? IsSubjectToSpendControl { get; set; }

    // Phase dates - Discovery
    public DateTime? DiscoveryStartDatePlanned { get; set; }
    public DateTime? DiscoveryStartDateActual { get; set; }
    public DateTime? DiscoveryEndDatePlanned { get; set; }
    public DateTime? DiscoveryEndDateActual { get; set; }

    // Phase dates - Alpha
    public DateTime? AlphaStartDatePlanned { get; set; }
    public DateTime? AlphaStartDateActual { get; set; }
    public DateTime? AlphaEndDatePlanned { get; set; }
    public DateTime? AlphaEndDateActual { get; set; }

    // Phase dates - Private Beta
    public DateTime? PrivateBetaStartDatePlanned { get; set; }
    public DateTime? PrivateBetaStartDateActual { get; set; }
    public DateTime? PrivateBetaEndDatePlanned { get; set; }
    public DateTime? PrivateBetaEndDateActual { get; set; }

    // Phase dates - Public Beta
    public DateTime? PublicBetaStartDatePlanned { get; set; }
    public DateTime? PublicBetaStartDateActual { get; set; }
    public DateTime? PublicBetaEndDatePlanned { get; set; }
    public DateTime? PublicBetaEndDateActual { get; set; }
}
