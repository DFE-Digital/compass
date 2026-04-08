using System.ComponentModel.DataAnnotations;
using Compass.Models.DemandTriage;

namespace Compass.ViewModels.DemandTriage;

// ── Register / demand list ────────────────────────────────────────────────────

public class DemandTriageRegisterViewModel
{
    public List<DemandTriageRequest> Requests { get; set; } = new();
    public string? StatusFilter { get; set; }
    public bool IsDemandManagement { get; set; }
    public bool IsCentralOpsAdmin { get; set; }
    public string CurrentUserEmail { get; set; } = string.Empty;
}

// ── Capture form ─────────────────────────────────────────────────────────────

public class DemandCaptureFormViewModel
{
    public int Id { get; set; }
    public string RequestReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Request name")]
    public string? RequestName { get; set; }

    [Display(Name = "Your full name")]
    public string? RequesterFullName { get; set; }

    [Display(Name = "Department group")]
    public string? DepartmentGroup { get; set; }

    [Display(Name = "DDT portfolio support")]
    public string? DdtPortfolioSupport { get; set; }

    [Display(Name = "Points of contact")]
    public string? PointsOfContact { get; set; }

    [Display(Name = "Senior responsible officer (G6+)")]
    public string? SroName { get; set; }

    [Display(Name = "Proposed request title")]
    public string? ProposedRequestTitle { get; set; }

    [Display(Name = "Request overview")]
    public string? RequestOverview { get; set; }

    [Display(Name = "Previous insight / research conducted")]
    public string? PreviousResearch { get; set; }

    [Display(Name = "Supporting manifesto commitment or statute law")]
    public string? ManifestoOrStatute { get; set; }

    /// <summary>Mission pillar IDs (persisted as comma-separated IDs on the request record).</summary>
    public List<int> SelectedMissionIds { get; set; } = new();

    /// <summary>Priority outcome (objective) IDs (persisted as comma-separated IDs on the request record).</summary>
    public List<int> SelectedObjectiveIds { get; set; } = new();

    [Display(Name = "Expected measurable benefits")]
    public string? ExpectedBenefits { get; set; }

    [Display(Name = "Risk / consequence if not delivered")]
    public string? RiskConsequence { get; set; }

    [Display(Name = "Funding provided for delivery")]
    public string? FundingProvided { get; set; }

    [Display(Name = "Headcount provided for delivery")]
    public string? HeadcountProvided { get; set; }

    [Display(Name = "Headcount details")]
    public string? HeadcountDetails { get; set; }

    [Display(Name = "Target delivery date")]
    [DataType(DataType.Date)]
    public DateTime? TargetDeliveryDate { get; set; }

    [Display(Name = "New or significantly changed digital service?")]
    public bool? NewOrChangedDigitalService { get; set; }

    [Display(Name = "Details of new / changed digital service")]
    public string? NewOrChangedServiceDetails { get; set; }

    [Display(Name = "Public-facing digital service?")]
    public bool? PublicFacingDigitalService { get; set; }

    public int? BusinessCaseId { get; set; }

    public List<string> ValidationErrors { get; set; } = new();
}

// ── Exploratory review ────────────────────────────────────────────────────────

public class ExploratoryReviewViewModel
{
    public int DemandTriageRequestId { get; set; }
    public string RequestReference { get; set; } = string.Empty;
    public string RequestName { get; set; } = string.Empty;

    [Display(Name = "Summary findings")]
    public string? SummaryFindings { get; set; }

    [Display(Name = "Key risks")]
    public string? KeyRisks { get; set; }

    [Display(Name = "Dependencies")]
    public string? Dependencies { get; set; }

    [Display(Name = "Recommendation to proceed")]
    public bool? RecommendationToProceed { get; set; }

    [Display(Name = "Reason for not proceeding")]
    public string? ReasonNotProceeding { get; set; }

    public bool IsCompleted { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

// ── Scoring wizard ────────────────────────────────────────────────────────────

public class ScoringWizardViewModel
{
    public int DemandTriageRequestId { get; set; }
    public string RequestReference { get; set; } = string.Empty;
    public string RequestName { get; set; } = string.Empty;

    /// <summary>Current section: 1, 2, 3, or 4</summary>
    public int Section { get; set; } = 1;

    /// <summary>Answers keyed by question code. Multi-select uses comma-separated values.</summary>
    public Dictionary<string, string> Answers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Free-text answers keyed by question code.</summary>
    public Dictionary<string, string> FreeTexts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Live score display
    public int StrategicAlignmentScore { get; set; }
    public int UrgencyScore { get; set; }
    public int FundingScore { get; set; }
    public int RiceScore { get; set; }
    public int TotalScore { get; set; }
    public string? SuggestionBand { get; set; }

    public bool ScorecardIsLocked { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

// ── Triage outcome ────────────────────────────────────────────────────────────

public class TriageOutcomeViewModel
{
    public int DemandTriageRequestId { get; set; }
    public string RequestReference { get; set; } = string.Empty;
    public string RequestName { get; set; } = string.Empty;

    // Score summary (read-only display)
    public int TotalScore { get; set; }
    public int StrategicAlignmentScore { get; set; }
    public int UrgencyScore { get; set; }
    public int FundingScore { get; set; }
    public int RiceScore { get; set; }
    public string? StrategicAlignmentBand { get; set; }
    public string? UrgencyBand { get; set; }
    public string? FundingBand { get; set; }
    public string? RiceBand { get; set; }
    public string? SuggestionBand { get; set; }

    [Display(Name = "Proposed outcome")]
    public string? OutcomeSelection { get; set; }

    [Display(Name = "Outcome summary / recommendation")]
    public string? OutcomeSummary { get; set; }

    [Display(Name = "Which area is the demand going to?")]
    public string? RoutedToArea { get; set; }

    [Display(Name = "Override reason")]
    public string? OverrideReason { get; set; }

    public bool OverrideRequired { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

// ── Request detail (full view) ────────────────────────────────────────────────

public class DemandTriageDetailViewModel
{
    public DemandTriageRequest Request { get; set; } = null!;

    /// <summary>Resolved mission pillar titles when stored values are numeric IDs.</summary>
    public List<string> MissionPillarLabels { get; set; } = new();

    /// <summary>Resolved priority outcome titles when stored values are numeric IDs.</summary>
    public List<string> PriorityOutcomeLabels { get; set; } = new();

    /// <summary>Original free text when mission pillar field could not be parsed as IDs.</summary>
    public string? MissionPillarsLegacyText { get; set; }

    /// <summary>Original free text when strategic theme field could not be parsed as IDs.</summary>
    public string? StrategicThemeLegacyText { get; set; }
    public bool IsDemandManagement { get; set; }
    public bool IsCentralOpsAdmin { get; set; }
    public bool IsOwner { get; set; }
    public string CurrentUserEmail { get; set; } = string.Empty;

    // Available actions
    public bool CanEdit { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanStartExploratoryReview { get; set; }
    public bool CanCompleteExploratoryReview { get; set; }
    public bool CanReturnForClarification { get; set; }
    public bool CanStartScoring { get; set; }
    public bool CanFinaliseScorecard { get; set; }
    public bool CanSendToTriage { get; set; }
    public bool CanRecordOutcome { get; set; }
    public bool CanClose { get; set; }
    public bool CanAdminOverride { get; set; }
    public bool CanUnlockScorecard { get; set; }
    public bool CanCreateProject { get; set; }
}

// ── Return for clarification ──────────────────────────────────────────────────

public class ReturnForClarificationViewModel
{
    public int DemandTriageRequestId { get; set; }
    public string RequestReference { get; set; } = string.Empty;
    public string RequestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A reason for returning is required.")]
    [Display(Name = "Reason for returning")]
    public string? Reason { get; set; }
}

// ── Admin override ────────────────────────────────────────────────────────────

public class AdminOverrideViewModel
{
    public int DemandTriageRequestId { get; set; }
    public string RequestReference { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a new status.")]
    [Display(Name = "New status")]
    public string? NewStatus { get; set; }

    [Required(ErrorMessage = "A reason is required for admin override.")]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }
}
