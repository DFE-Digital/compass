using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Compass.Models.Fips;

namespace Compass.Models.ServiceDataModels;

/// <summary>Reusable configurable data model (e.g. Service Census). Stable key must not be reused for a different meaning.</summary>
[Table("ServiceDataModels")]
public class ServiceDataModel
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? OwnerDisplayName { get; set; }

    [MaxLength(320)]
    public string? OwnerEmail { get; set; }

    [MaxLength(100)]
    public string? Classification { get; set; }

    public bool IsRepeatable { get; set; }

    public bool IsReportable { get; set; } = true;

    public ServiceDataModelLifecycleStatus LifecycleStatus { get; set; } = ServiceDataModelLifecycleStatus.Draft;

    public ServiceDataModelApplicabilityMode ApplicabilityMode { get; set; } = ServiceDataModelApplicabilityMode.AllActive;

    /// <summary>When true, reviewer attestation is required before Reviewed/complete.</summary>
    public bool RequiresReviewerAttestation { get; set; } = true;

    /// <summary>Optional progress label threshold (0–100). Does not alone mean Reviewed/complete.</summary>
    public int? ProgressLabelThresholdPercent { get; set; }

    public int? DefaultDueDaysAfterPublish { get; set; }

    [MaxLength(100)]
    public string? ReviewCadenceLabel { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? CreatedByEmail { get; set; }

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? UpdatedByEmail { get; set; }

    public ICollection<ServiceDataModelVersion> Versions { get; set; } = new List<ServiceDataModelVersion>();
    public ICollection<ServiceDataModelApplicabilityRule> ApplicabilityRules { get; set; } = new List<ServiceDataModelApplicabilityRule>();
    public ICollection<ServiceDataModelExplicitService> ExplicitServices { get; set; } = new List<ServiceDataModelExplicitService>();
}

[Table("ServiceDataModelVersions")]
public class ServiceDataModelVersion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelId { get; set; }

    [ForeignKey(nameof(ServiceDataModelId))]
    public ServiceDataModel Model { get; set; } = null!;

    public int VersionNumber { get; set; } = 1;

    public ServiceDataModelLifecycleStatus Status { get; set; } = ServiceDataModelLifecycleStatus.Draft;

    [MaxLength(1000)]
    public string? ChangeSummary { get; set; }

    /// <summary>Optional period label for repeatable/periodic publishes. Standing models such as Service Census leave this unset.</summary>
    [MaxLength(100)]
    public string? PeriodLabel { get; set; }

    public DateTime? PeriodStartUtc { get; set; }
    public DateTime? PeriodEndUtc { get; set; }

    public DateTime? PublishedUtc { get; set; }

    [MaxLength(320)]
    public string? PublishedByEmail { get; set; }

    public DateTime? RetiredUtc { get; set; }

    [MaxLength(320)]
    public string? RetiredByEmail { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? CreatedByEmail { get; set; }

    /// <summary>Frozen definition JSON snapshot after publish (optional redundancy for reporting).</summary>
    public string? DefinitionSnapshotJson { get; set; }

    public ICollection<ServiceDataModelGroup> Groups { get; set; } = new List<ServiceDataModelGroup>();
    public ICollection<ServiceDataModelAssignment> Assignments { get; set; } = new List<ServiceDataModelAssignment>();
}

[Table("ServiceDataModelGroups")]
public class ServiceDataModelGroup
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelVersionId { get; set; }

    [ForeignKey(nameof(ServiceDataModelVersionId))]
    public ServiceDataModelVersion Version { get; set; } = null!;

    [Required, MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Guidance { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// When true, the theme is hidden from respondents but kept for admins and historical answers.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Legacy: previously recorded when a theme was copied from the core catalogue.
    /// Censuses now inherit the shared base set; this is retained for historical rows only.
    /// </summary>
    [MaxLength(100)]
    public string? CoreThemeKey { get; set; }

    public ICollection<ServiceDataModelField> Fields { get; set; } = new List<ServiceDataModelField>();
}

[Table("ServiceDataModelFields")]
public class ServiceDataModelField
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelGroupId { get; set; }

    [ForeignKey(nameof(ServiceDataModelGroupId))]
    public ServiceDataModelGroup Group { get; set; } = null!;

    [Required, MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Guidance { get; set; }

    public ServiceDataModelFieldType FieldType { get; set; } = ServiceDataModelFieldType.Text;

    public bool IsMandatory { get; set; }

    /// <summary>When false, answered/applicable fields are excluded from field completion %.</summary>
    public bool CountsTowardsCompletion { get; set; } = true;

    public bool IsReportable { get; set; } = true;

    public int SortOrder { get; set; }

    /// <summary>
    /// When true, the question is hidden from respondents but kept for admins and historical answers.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>JSON declarative visibility rule, e.g. {"fieldKey":"has-url","equals":"Yes"}.</summary>
    [MaxLength(2000)]
    public string? VisibilityRuleJson { get; set; }

    /// <summary>When set, answers propose a change to this canonical Service Register attribute instead of silently overwriting it.</summary>
    [MaxLength(100)]
    public string? CanonicalAttributeKey { get; set; }

    [MaxLength(500)]
    public string? ValidationPattern { get; set; }

    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }

    public ICollection<ServiceDataModelFieldOption> Options { get; set; } = new List<ServiceDataModelFieldOption>();
}

[Table("ServiceDataModelFieldOptions")]
public class ServiceDataModelFieldOption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelFieldId { get; set; }

    [ForeignKey(nameof(ServiceDataModelFieldId))]
    public ServiceDataModelField Field { get; set; } = null!;

    [Required, MaxLength(100)]
    public string ValueKey { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

[Table("ServiceDataModelApplicabilityRules")]
public class ServiceDataModelApplicabilityRule
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelId { get; set; }

    [ForeignKey(nameof(ServiceDataModelId))]
    public ServiceDataModel Model { get; set; } = null!;

    /// <summary>Optional CMDB product status filter (New/Active/Inactive/Rejected).</summary>
    public CMDBProductStatus? ProductStatus { get; set; }

    public int? PhaseId { get; set; }

    public int? FipsTypeId { get; set; }

    public int? FipsBusinessAreaId { get; set; }

    public int? FipsDirectorateId { get; set; }
}

[Table("ServiceDataModelExplicitServices")]
public class ServiceDataModelExplicitService
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelId { get; set; }

    [ForeignKey(nameof(ServiceDataModelId))]
    public ServiceDataModel Model { get; set; } = null!;

    [Required]
    public Guid CMDBProductId { get; set; }

    [ForeignKey(nameof(CMDBProductId))]
    public CMDBProduct Product { get; set; } = null!;

    public ServiceDataModelExplicitServiceMode Mode { get; set; } = ServiceDataModelExplicitServiceMode.Include;
}
