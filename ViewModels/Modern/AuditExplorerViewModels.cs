using System.Text.Json;

namespace Compass.ViewModels.Modern;

/// <summary>
/// Filter inputs accepted by the modern admin Audit explorer
/// (<c>/modern/admin/audit-explorer</c>). All fields are optional; the
/// controller normalises empty strings to <c>null</c>.
/// </summary>
public sealed class AuditExplorerFilterVm
{
    /// <summary>Free-text query matched against entity reference, entity id and action.</summary>
    public string? Q { get; set; }
    /// <summary>Feature bucket key from <see cref="Compass.Services.AuditFeatureMap"/>.</summary>
    public string? Feature { get; set; }
    /// <summary>Exact CLR entity name match (e.g. <c>"Risk"</c>).</summary>
    public string? Entity { get; set; }
    /// <summary>Exact action verb match (e.g. <c>"Create"</c>, <c>"Approved"</c>).</summary>
    public string? Action { get; set; }
    /// <summary>Free-text matched against ChangedBy / ChangedByEmail / ChangedByUserId.</summary>
    public string? User { get; set; }
    /// <summary>Inclusive lower bound (treated as UTC midnight).</summary>
    public DateTime? From { get; set; }
    /// <summary>Inclusive upper bound (treated as UTC end of day).</summary>
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>One row in the Audit explorer table.</summary>
public sealed class AuditExplorerRowVm
{
    public required Guid AuditLogId { get; init; }
    public required DateTime ChangedUtc { get; init; }
    public required string Entity { get; init; }
    public required string EntityFriendly { get; init; }
    public required string FeatureKey { get; init; }
    public required string FeatureName { get; init; }
    public string? EntityId { get; init; }
    public string? EntityReference { get; init; }
    public required string Action { get; init; }
    public required string ActionFriendly { get; init; }
    public required string ActionTagColour { get; init; }
    public string? ChangedBy { get; init; }
    public string? ChangedByEmail { get; init; }
    public bool HasBefore { get; init; }
    public bool HasAfter { get; init; }
}

/// <summary>Top-level model for the Audit explorer list view.</summary>
public sealed class AuditExplorerListVm
{
    public required AuditExplorerFilterVm Filter { get; init; }
    public required IReadOnlyList<AuditExplorerRowVm> Rows { get; init; }
    public required int TotalRows { get; init; }
    public required int TotalPages { get; init; }
    public required IReadOnlyList<(string Key, string Name)> Features { get; init; }
    public required IReadOnlyList<(string Key, string Name)> Entities { get; init; }
    public required IReadOnlyList<string> Actions { get; init; }
    /// <summary>Snapshot of the most-recent timestamp in the table, for context.</summary>
    public DateTime? MostRecentTimestampUtc { get; init; }
}

/// <summary>Status of a single field when comparing a Before/After payload.</summary>
public enum AuditDiffStatus
{
    Added,
    Removed,
    Changed,
    Unchanged,
}

/// <summary>One field row in the before/after diff table.</summary>
public sealed class AuditDiffFieldVm
{
    public required string Field { get; init; }
    public required string FieldFriendly { get; init; }
    public string? Before { get; init; }
    public string? After { get; init; }
    public required AuditDiffStatus Status { get; init; }
}

/// <summary>Model for the per-record Audit explorer detail page.</summary>
public sealed class AuditExplorerDetailVm
{
    public required Guid AuditLogId { get; init; }
    public required DateTime ChangedUtc { get; init; }
    public required string Entity { get; init; }
    public required string EntityFriendly { get; init; }
    public required string FeatureKey { get; init; }
    public required string FeatureName { get; init; }
    public string? EntityId { get; init; }
    public string? EntityReference { get; init; }
    public required string Action { get; init; }
    public required string ActionFriendly { get; init; }
    public required string ActionTagColour { get; init; }
    public string? ChangedBy { get; init; }
    public string? ChangedByEmail { get; init; }
    public string? ChangedByUserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? BeforeJson { get; init; }
    public string? AfterJson { get; init; }
    public string? BeforeJsonPretty { get; init; }
    public string? AfterJsonPretty { get; init; }
    public required IReadOnlyList<AuditDiffFieldVm> DiffFields { get; init; }
    public required string PlainEnglishSummary { get; init; }
    public string? BackUrl { get; init; }
    /// <summary>Url of the previous and next records under the same filter.</summary>
    public string? PreviousUrl { get; init; }
    public string? NextUrl { get; init; }
}
