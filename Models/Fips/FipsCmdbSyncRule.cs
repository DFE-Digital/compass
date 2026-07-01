using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.Fips;

/// <summary>Where to read text when evaluating a CMDB sync rule.</summary>
public static class FipsCmdbSyncRuleScopes
{
    public const string Title = "Title";
    public const string Description = "Description";
    public const string ParentName = "ParentName";
    public const string UserDescription = "UserDescription";
    /// <summary>Title, CMDB description, parent name, and Compass user description combined.</summary>
    public const string MappedText = "MappedText";
    /// <summary>JSON snapshot of the CMDB row (same as stored on <see cref="CMDBProduct.LastCmdbSnapshotJson"/>).</summary>
    public const string RawJson = "RawJson";
    /// <summary><c>service_classification</c> from the CMDB JSON (string or ServiceNow reference object).</summary>
    public const string ServiceClassification = "ServiceClassification";
    /// <summary><c>operational_status</c> from the CMDB JSON (string/number/reference object).</summary>
    public const string OperationalStatus = "OperationalStatus";

    public static readonly IReadOnlyList<string> All =
    [
        Title, Description, ParentName, UserDescription, MappedText, RawJson, ServiceClassification, OperationalStatus
    ];
}

public static class FipsCmdbSyncRuleMatchKinds
{
    public const string Contains = "Contains";
    public const string Regex = "Regex";

    public static readonly IReadOnlyList<string> All = [Contains, Regex];
}

/// <summary>What happens when a CMDB sync rule matches.</summary>
public static class FipsCmdbSyncRuleActions
{
    public const string SetStatus = "SetStatus";
    public const string SetEnterpriseService = "SetEnterpriseService";

    public static readonly IReadOnlyList<string> All = [SetStatus, SetEnterpriseService];
}

[Table("FipsCmdbSyncRules")]
public class FipsCmdbSyncRule
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    [Required]
    [MaxLength(40)]
    public string FieldScope { get; set; } = FipsCmdbSyncRuleScopes.MappedText;

    [Required]
    [MaxLength(20)]
    public string MatchKind { get; set; } = FipsCmdbSyncRuleMatchKinds.Contains;

    [Required]
    [MaxLength(2000)]
    public string Pattern { get; set; } = "";

    /// <summary>Status applied when the rule matches (typically <see cref="CMDBProductStatus.Rejected"/> or <see cref="CMDBProductStatus.Inactive"/>).</summary>
    public CMDBProductStatus TargetStatus { get; set; } = CMDBProductStatus.Rejected;

    /// <summary>
    /// <see cref="FipsCmdbSyncRuleActions.SetStatus"/> — first matching rule sets <see cref="TargetStatus"/>.
    /// <see cref="FipsCmdbSyncRuleActions.SetEnterpriseService"/> — sets <see cref="CMDBProduct.IsEnterpriseService"/> to true (in addition to status rules).
    /// </summary>
    [Required]
    [MaxLength(30)]
    public string Action { get; set; } = FipsCmdbSyncRuleActions.SetStatus;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
