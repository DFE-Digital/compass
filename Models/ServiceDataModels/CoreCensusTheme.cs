using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.ServiceDataModels;

/// <summary>
/// Shared census theme definition. Every census inherits this base set —
/// there is no per-census copy of themes or questions.
/// </summary>
[Table("CoreCensusThemes")]
public class CoreCensusTheme
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stable machine key matching the schema area key (e.g. purpose, users).</summary>
    [Required, MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Guidance { get; set; }

    public int SortOrder { get; set; }

    /// <summary>
    /// True for the service-offering / entry-details theme. Excluded from the shared census set —
    /// canonical identity lives on the Service Register.
    /// </summary>
    public bool IsServiceOffering { get; set; }

    /// <summary>When false, the theme is hidden from respondents everywhere and excluded from completion.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<CoreCensusThemeField> Fields { get; set; } = new List<CoreCensusThemeField>();
}

[Table("CoreCensusThemeFields")]
public class CoreCensusThemeField
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CoreCensusThemeId { get; set; }

    [ForeignKey(nameof(CoreCensusThemeId))]
    public CoreCensusTheme Theme { get; set; } = null!;

    [Required, MaxLength(100)]
    public string StableKey { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Guidance { get; set; }

    public ServiceDataModelFieldType FieldType { get; set; } = ServiceDataModelFieldType.Text;

    /// <summary>
    /// Original schema type or group kind (e.g. COLLECTION, TEXTAREA) for mapping diagnostics.
    /// </summary>
    [MaxLength(80)]
    public string? SchemaSourceType { get; set; }

    public bool IsMandatory { get; set; }

    public bool CountsTowardsCompletion { get; set; } = true;

    public bool IsReportable { get; set; } = true;

    public int SortOrder { get; set; }

    /// <summary>
    /// When true, the question is hidden from respondents everywhere and excluded from completion.
    /// </summary>
    public bool IsDisabled { get; set; }

    [MaxLength(2000)]
    public string? VisibilityRuleJson { get; set; }

    /// <summary>When set, answers propose or sync a Service Register attribute.</summary>
    [MaxLength(100)]
    public string? CanonicalAttributeKey { get; set; }

    [MaxLength(500)]
    public string? ValidationPattern { get; set; }

    public decimal? MinNumber { get; set; }
    public decimal? MaxNumber { get; set; }

    /// <summary>
    /// When set on a choice or Lookup field, respondent options come from this admin lookup panel key
    /// (e.g. <c>fips-user-groups</c>). Explicit <see cref="Options"/> are used only when unset
    /// or when the lookup has no active values.
    /// </summary>
    [MaxLength(100)]
    public string? OptionsLookupKey { get; set; }

    /// <summary>
    /// For Text / MultilineText: store answers as a JSON array of strings (add another / remove).
    /// For Lookup: multiple select when true, single select when false.
    /// </summary>
    public bool AllowMultiple { get; set; }

    public ICollection<CoreCensusThemeFieldOption> Options { get; set; } = new List<CoreCensusThemeFieldOption>();
}

[Table("CoreCensusThemeFieldOptions")]
public class CoreCensusThemeFieldOption
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CoreCensusThemeFieldId { get; set; }

    [ForeignKey(nameof(CoreCensusThemeFieldId))]
    public CoreCensusThemeField Field { get; set; } = null!;

    [Required, MaxLength(100)]
    public string ValueKey { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Label { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
