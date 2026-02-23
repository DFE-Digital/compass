using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandTriage;

/// <summary>
/// Audit event for demand triage actions.
/// Every state transition and significant action is recorded here.
/// </summary>
public class DemandTriageAuditEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DemandTriageRequestId { get; set; }

    [ForeignKey(nameof(DemandTriageRequestId))]
    public DemandTriageRequest DemandTriageRequest { get; set; } = null!;

    /// <summary>
    /// Action code, e.g. demand.created, demand.submitted, exploratory.started,
    /// exploratory.completed, scoring.started, scoring.finalised,
    /// triage.sent, triage.recorded, triage.override_used, demand.closed,
    /// demand.returned, demand.admin_override
    /// </summary>
    [Required]
    [StringLength(80)]
    public string Action { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ActorEmail { get; set; }

    [StringLength(255)]
    public string? ActorDisplayName { get; set; }

    [Required]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Status before the action</summary>
    [StringLength(60)]
    public string? FromStatus { get; set; }

    /// <summary>Status after the action</summary>
    [StringLength(60)]
    public string? ToStatus { get; set; }

    /// <summary>JSON snapshot of the entity before the change</summary>
    public string? BeforeJson { get; set; }

    /// <summary>JSON snapshot of the entity after the change</summary>
    public string? AfterJson { get; set; }

    /// <summary>Optional free-text note (e.g. override reason, return reason)</summary>
    public string? Notes { get; set; }
}

/// <summary>Well-known audit action codes.</summary>
public static class DemandAuditActions
{
    public const string Created = "demand.created";
    public const string Updated = "demand.updated";
    public const string Submitted = "demand.submitted";
    public const string Returned = "demand.returned";
    public const string Closed = "demand.closed";
    public const string AdminOverride = "demand.admin_override";

    public const string ExploratoryStarted = "exploratory.started";
    public const string ExploratoryCompleted = "exploratory.completed";

    public const string ScoringStarted = "scoring.started";
    public const string ScoringFinalised = "scoring.finalised";
    public const string ScorecardUnlocked = "scoring.unlocked";

    public const string TriageSent = "triage.sent";
    public const string TriageRecorded = "triage.recorded";
    public const string TriageOverrideUsed = "triage.override_used";

    public const string ProjectCreated = "project.created_from_demand";
}
