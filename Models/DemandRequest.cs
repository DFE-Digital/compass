using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a demand request submitted by policy or delivery teams for DDaT support
/// </summary>
public class DemandRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique reference number for the request (e.g., DR-2025-001)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty;

    // Applicant Details (Auto-captured from Microsoft login)
    [Required]
    [StringLength(255)]
    public string ApplicantName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string ApplicantEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string BusinessArea { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string SeniorResponsibleOfficer { get; set; } = string.Empty; // Must be G6+

    // Portfolio and Context
    public bool? HasPortfolioSupport { get; set; }

    [StringLength(100)]
    public string? PortfolioName { get; set; }

    [StringLength(50)]
    public string? PortfolioPrioritisation { get; set; } // Yes/No/Not sure

    // Request Summary
    [Required]
    [StringLength(120)]
    public string ProposedTitle { get; set; } = string.Empty; // 10-120 characters

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string OverviewAndBusinessNeed { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? PreviousResearchOrInsight { get; set; }

    [StringLength(50)]
    public string WillCreateOrChangeDigitalService { get; set; } = string.Empty; // Yes/No/Unsure

    [Column(TypeName = "nvarchar(max)")]
    public string? DigitalServiceDetails { get; set; }

    // Strategic Alignment
    [StringLength(50)]
    public string IsManifestoOrStatutory { get; set; } = string.Empty; // Yes/No/Unsure

    [Column(TypeName = "nvarchar(max)")]
    public string? ManifestoStatutoryDetails { get; set; }

    public bool? SupportsOpportunityMissionPillar { get; set; }
    
    [StringLength(500)]
    public string? OpportunityMissionPillars { get; set; } // Comma-separated

    public bool? SupportsDdatStrategicTheme { get; set; }

    [StringLength(500)]
    public string? DdatStrategicThemes { get; set; } // Comma-separated

    // Impact and Risk
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ExpectedBenefits { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string RiskIfNotDelivered { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? PredictedRiskLevel { get; set; }

    [MaxLength(20)]
    public string? RiskLevelOverride { get; set; }

    [MaxLength(20)]
    public string? ImpactLevel { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ImpactSummary { get; set; }

    [NotMapped]
    public string? EffectiveRiskLevel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RiskLevelOverride))
            {
                return RiskLevelOverride;
            }

            return string.IsNullOrWhiteSpace(PredictedRiskLevel)
                ? null
                : PredictedRiskLevel;
        }
    }

    // Funding and Headcount
    public bool? HasFunding { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FundingAmount { get; set; }

    [StringLength(200)]
    public string? FundingSource { get; set; }

    [StringLength(200)]
    public string? FundingDuration { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? FundingNotes { get; set; }

    public bool? HasHeadcount { get; set; }

    public int? NumberOfFTE { get; set; }

    [StringLength(500)]
    public string? RolesProvided { get; set; }

    [StringLength(200)]
    public string? HeadcountDuration { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? HeadcountNotes { get; set; }

    // Delivery
    public bool? HasTargetDeliveryDate { get; set; }

    public DateTime? TargetDeliveryDate { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? DeliveryTimescales { get; set; }

    // Triage
    public bool? IsSubmittedToTriage { get; set; }

    public DateTime? TriageSubmittedAt { get; set; }

    public int? TriageMeetingId { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? TriageNotes { get; set; }

    // Conversion
    public int? ConvertedProjectId { get; set; }

    public DateTime? ConvertedToProjectAt { get; set; }

    // Status and Workflow
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Draft"; // Draft/Submitted/Under Review/Approved/Deferred/Rejected

    [StringLength(100)]
    public string? AssignedToEmail { get; set; }

    [StringLength(255)]
    public string? AssignedToName { get; set; }

    [StringLength(50)]
    public string? CurrentPhase { get; set; } // Explore/Triage/Delivery

    // Declaration
    public bool DeclarationConfirmed { get; set; }

    // Sensitive Request Flag
    public bool IsSensitiveRequest { get; set; }

    // Timestamps
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime? DecisionAt { get; set; }

    [StringLength(100)]
    public string? ReviewedBy { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ReviewNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? DecisionNotes { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? StatusChangeReason { get; set; }

    public DateTime? NextReviewDate { get; set; }

    // Navigation properties
    public ICollection<DemandRequestContact> Contacts { get; set; } = new List<DemandRequestContact>();
    public DemandRequestPrioritisation? Prioritisation { get; set; }
    public ICollection<DemandRequestNote> Notes { get; set; } = new List<DemandRequestNote>();
    public ICollection<DemandRequestAssessment> Assessments { get; set; } = new List<DemandRequestAssessment>();
    public ICollection<DemandRequestRiskType> RiskTypeLinks { get; set; } = new List<DemandRequestRiskType>();
    public ICollection<DemandRequestSectionCompletion> SectionCompletions { get; set; } = new List<DemandRequestSectionCompletion>();
    public TriageMeeting? TriageMeeting { get; set; }
    public Project? ConvertedProject { get; set; }
}

/// <summary>
/// Points of contact for a demand request
/// </summary>
public class DemandRequestContact
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandRequestId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Role { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("DemandRequestId")]
    public DemandRequest? DemandRequest { get; set; }
}

/// <summary>
/// Prioritisation scoring for a demand request
/// </summary>
public class DemandRequestPrioritisation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandRequestId { get; set; }

    // Strategic Alignment (scores 1-5)
    public int StatutoryManifestoScore { get; set; } // Required by law or ministerial commitment
    public int OpportunityMissionScore { get; set; } // Supports departmental mission
    public int DdatStrategicThemeScore { get; set; } // Aligns to digital strategy

    // User Impact (scores 1-5)
    public int ScaleOfUsersScore { get; set; } // Size and reach of user base
    public int EvidenceOfUserNeedScore { get; set; } // Quality of research and insight

    // Risk & Urgency (scores 1-5)
    public int RiskIfNotDeliveredScore { get; set; } // Legal, financial, or reputational
    public int TargetDeliveryUrgencyScore { get; set; } // Immediacy of delivery date

    // Feasibility (scores 1-5)
    public int FundingAvailableScore { get; set; } // Funding confirmed vs none
    public int HeadcountAvailableScore { get; set; } // Resourcing confirmed
    public int PortfolioFitScore { get; set; } // Reuse or duplication

    // Value & Outcome (scores 1-5)
    public int ExpectedBenefitsScore { get; set; } // Potential measurable outcomes

    // Weighted totals
    public int StrategicAlignmentTotal { get; set; }
    public int UserImpactTotal { get; set; }
    public int RiskUrgencyTotal { get; set; }
    public int FeasibilityTotal { get; set; }
    public int ValueOutcomeTotal { get; set; }

    // Overall priority score (out of 100)
    public int TotalPriorityScore { get; set; }

    // Tier assignment
    [Required]
    [StringLength(50)]
    public string PriorityTier { get; set; } = "Tier 4 – Low"; // Tier 1-4

    [Column(TypeName = "nvarchar(max)")]
    public string? ScoringNotes { get; set; }

    [StringLength(100)]
    public string? ScoredBy { get; set; }

    public DateTime? ScoredAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("DemandRequestId")]
    public DemandRequest DemandRequest { get; set; } = null!;
}

