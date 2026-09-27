using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Compass.Models.Fips;

namespace Compass.Models.ServiceDataModels;

[Table("ServiceDataModelAssignments")]
public class ServiceDataModelAssignment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelVersionId { get; set; }

    [ForeignKey(nameof(ServiceDataModelVersionId))]
    public ServiceDataModelVersion Version { get; set; } = null!;

    [Required]
    public Guid CMDBProductId { get; set; }

    [ForeignKey(nameof(CMDBProductId))]
    public CMDBProduct Product { get; set; } = null!;

    [MaxLength(100)]
    public string? PeriodLabel { get; set; }

    public DateTime? PeriodStartUtc { get; set; }
    public DateTime? PeriodEndUtc { get; set; }

    public DateTime? DueUtc { get; set; }

    public ServiceDataModelAssignmentStatus Status { get; set; } = ServiceDataModelAssignmentStatus.NotStarted;

    [MaxLength(1000)]
    public string? StatusReason { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastAnsweredUtc { get; set; }

    [MaxLength(320)]
    public string? LastAnsweredByEmail { get; set; }

    public DateTime? SubmittedUtc { get; set; }

    [MaxLength(320)]
    public string? SubmittedByEmail { get; set; }

    public DateTime? ReviewedUtc { get; set; }

    [MaxLength(320)]
    public string? ReviewedByEmail { get; set; }

    [MaxLength(2000)]
    public string? ReviewerAttestationNote { get; set; }

    public DateTime? ChangesRequestedUtc { get; set; }

    [MaxLength(2000)]
    public string? ChangesRequestedNote { get; set; }

    /// <summary>Cached field completion percent; null when not applicable (empty denominator).</summary>
    public decimal? FieldCompletionPercent { get; set; }

    /// <summary>Cached mandatory completion percent; null when not applicable.</summary>
    public decimal? MandatoryCompletionPercent { get; set; }

    public int? CurrentRevisionNumber { get; set; }

    public ICollection<ServiceDataModelSubmission> Submissions { get; set; } = new List<ServiceDataModelSubmission>();

    public ICollection<ServiceDataModelThemeCompletion> ThemeCompletions { get; set; } =
        new List<ServiceDataModelThemeCompletion>();
}

[Table("ServiceDataModelSubmissions")]
public class ServiceDataModelSubmission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelAssignmentId { get; set; }

    [ForeignKey(nameof(ServiceDataModelAssignmentId))]
    public ServiceDataModelAssignment Assignment { get; set; } = null!;

    public int RevisionNumber { get; set; } = 1;

    public bool IsCurrent { get; set; } = true;

    public bool IsSubmittedSnapshot { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? CreatedByEmail { get; set; }

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? UpdatedByEmail { get; set; }

    public DateTime? SubmittedUtc { get; set; }

    [MaxLength(320)]
    public string? SubmittedByEmail { get; set; }

    public ICollection<ServiceDataModelAnswer> Answers { get; set; } = new List<ServiceDataModelAnswer>();
}

[Table("ServiceDataModelAnswers")]
public class ServiceDataModelAnswer
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelSubmissionId { get; set; }

    [ForeignKey(nameof(ServiceDataModelSubmissionId))]
    public ServiceDataModelSubmission Submission { get; set; } = null!;

    /// <summary>
    /// Legacy FK to a copied model field. Nullable once answers are keyed by
    /// <see cref="FieldStableKey"/> against the shared base theme catalogue.
    /// </summary>
    public Guid? ServiceDataModelFieldId { get; set; }

    [ForeignKey(nameof(ServiceDataModelFieldId))]
    public ServiceDataModelField? Field { get; set; }

    /// <summary>
    /// Stable question key in the shared base set. Survives theme catalogue edits and
    /// replaces per-census copied field ids as the durable answer attachment.
    /// </summary>
    [MaxLength(100)]
    public string? FieldStableKey { get; set; }

    /// <summary>Typed value stored as JSON (string, number, bool, string[], reference object).</summary>
    public string? ValueJson { get; set; }

    public bool IsValid { get; set; } = true;

    [MaxLength(500)]
    public string? ValidationMessage { get; set; }

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? UpdatedByEmail { get; set; }
}

[Table("ServiceDataModelProposedRegisterChanges")]
public class ServiceDataModelProposedRegisterChange
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelAssignmentId { get; set; }

    [ForeignKey(nameof(ServiceDataModelAssignmentId))]
    public ServiceDataModelAssignment Assignment { get; set; } = null!;

    /// <summary>Legacy FK to a copied model field; prefer <see cref="FieldStableKey"/>.</summary>
    public Guid? ServiceDataModelFieldId { get; set; }

    [ForeignKey(nameof(ServiceDataModelFieldId))]
    public ServiceDataModelField? Field { get; set; }

    [MaxLength(100)]
    public string? FieldStableKey { get; set; }

    [Required, MaxLength(100)]
    public string CanonicalAttributeKey { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? CurrentRegisterValue { get; set; }

    [MaxLength(2000)]
    public string? ProposedValue { get; set; }

    public ServiceDataModelProposedChangeStatus Status { get; set; } = ServiceDataModelProposedChangeStatus.Pending;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? CreatedByEmail { get; set; }

    public DateTime? ReviewedUtc { get; set; }

    [MaxLength(320)]
    public string? ReviewedByEmail { get; set; }

    [MaxLength(1000)]
    public string? ReviewNote { get; set; }
}

[Table("ServiceDataModelAuditEvents")]
public class ServiceDataModelAuditEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? ActorEmail { get; set; }

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>Metadata only — do not store full answer payloads with sensitive content.</summary>
    [MaxLength(4000)]
    public string? MetadataJson { get; set; }
}
