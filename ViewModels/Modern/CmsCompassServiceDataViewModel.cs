namespace Compass.ViewModels.Modern;

public sealed class CmsCompassServiceDataViewModel
{
    public string CmsApiBaseUrl { get; set; } = "";
    public DateTime GeneratedAtUtc { get; set; }
    public bool CmsFetchSucceeded { get; set; }
    public string? CmsFetchError { get; set; }

    public CmsCompassServiceDataSummary Summary { get; set; } = new();
    public IReadOnlyList<CmsCompassLookupComparisonRow> LookupComparisons { get; set; } = [];
    public IReadOnlyList<CmsCompassProductComparisonRow> Products { get; set; } = [];
    public IReadOnlyList<CmsCompassProductComparisonRow> SyncableProducts { get; set; } = [];
    public IReadOnlyList<CmsCompassValueMappingGroup> ValueMappingGroups { get; set; } = [];
    public CmsCompassSyncResultViewModel? LastSyncResult { get; set; }
}

public sealed class CmsCompassServiceDataSummary
{
    public int CmsLiveWithCmdbId { get; set; }
    public int CompassLiveWithCmdbId { get; set; }
    public int Matched { get; set; }
    public int MatchedIdentical { get; set; }
    public int MatchedWithDifferences { get; set; }
    public int CmsOnly { get; set; }
    public int CompassOnly { get; set; }
    public int CmsLiveMissingCmdbId { get; set; }
    public int CompassLiveMissingCmdbId { get; set; }
}

public sealed class CmsCompassLookupComparisonRow
{
    public string Label { get; set; } = "";
    public int CmsCount { get; set; }
    public int CompassCount { get; set; }
    public IReadOnlyList<string> CmsOnlyNames { get; set; } = [];
    public IReadOnlyList<string> CompassOnlyNames { get; set; } = [];
}

public enum CmsCompassProductMatchStatus
{
    MatchedIdentical,
    MatchedWithDifferences,
    CmsOnly,
    CompassOnly
}

public sealed class CmsCompassProductComparisonRow
{
    public string? CmdbId { get; set; }
    public Guid? CompassProductId { get; set; }
    public CmsCompassProductMatchStatus MatchStatus { get; set; }

    public string? CmsTitle { get; set; }
    public string? CompassTitle { get; set; }
    public string? CmsFipsId { get; set; }
    public string? CmsState { get; set; }
    public string? CompassStatus { get; set; }
    public string? CmsProductUrl { get; set; }
    public string? CompassProductUrl { get; set; }

    public string? CmsPhase { get; set; }
    public string? CompassPhase { get; set; }
    public string CmsChannels { get; set; } = "";
    public string CompassChannels { get; set; } = "";
    public string CmsTypes { get; set; } = "";
    public string CompassTypes { get; set; } = "";
    public string CmsUserGroups { get; set; } = "";
    public string CompassUserGroups { get; set; } = "";
    public string CmsBusinessAreas { get; set; } = "";
    public string CompassBusinessAreas { get; set; } = "";

    public IReadOnlyList<string> Differences { get; set; } = [];

    public bool CanSyncFromCms =>
        CompassProductId.HasValue
        && MatchStatus is CmsCompassProductMatchStatus.MatchedIdentical
            or CmsCompassProductMatchStatus.MatchedWithDifferences;
}

public sealed class CmsCompassValueMappingGroup
{
    public string FieldKey { get; set; } = "";
    public string FieldLabel { get; set; } = "";
    public string CompassLookupLabel { get; set; } = "";
    public IReadOnlyList<CmsCompassValueMappingRow> Rows { get; set; } = [];
}

public sealed class CmsCompassValueMappingRow
{
    public string FieldKey { get; set; } = "";
    public string CmsValueName { get; set; } = "";
    public int? SuggestedCompassId { get; set; }
    public string? SuggestedCompassName { get; set; }
    public bool HasExactNameMatch { get; set; }
    public IReadOnlyList<CmsCompassLookupOption> CompassOptions { get; set; } = [];
}

public sealed class CmsCompassLookupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class CmsCompassSyncRequest
{
    public bool SyncPhase { get; set; }
    public bool SyncChannel { get; set; }
    public bool SyncType { get; set; }
    public bool SyncBusinessArea { get; set; }
    public bool SyncUserGroup { get; set; }
    public bool DryRun { get; set; }
    public IReadOnlyList<string> CmdbIds { get; set; } = [];
    /// <summary>fieldKey → cmsValueName → compass lookup id (null = unmapped).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int?>> Mappings { get; set; }
        = new Dictionary<string, IReadOnlyDictionary<string, int?>>();
}

public sealed class CmsCompassSyncResultViewModel
{
    public bool DryRun { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<CmsCompassSyncProductResult> Results { get; set; } = [];
}

public sealed class CmsCompassSyncProductResult
{
    public string CmdbId { get; set; } = "";
    public string ProductTitle { get; set; } = "";
    public bool Success { get; set; }
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public IReadOnlyList<string> Changes { get; set; } = [];
    public IReadOnlyList<string> Errors { get; set; } = [];
}
