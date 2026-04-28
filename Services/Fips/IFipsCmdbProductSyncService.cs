using Compass.Models.Fips;

namespace Compass.Services.Fips;

/// <summary>Incremental progress while <see cref="IFipsCmdbProductSyncService.SyncActiveServiceOfferingsAsync"/> runs (for UI streaming).</summary>
public sealed class FipsCmdbSyncProgressUpdate
{
    public const string PhasePreparing = "preparing";
    public const string PhaseLoadingCmdb = "loading_cmdb";
    public const string PhaseProcessing = "processing";

    public string Phase { get; init; } = "";
    public string? Message { get; init; }
    public int? Processed { get; init; }
    public int? Total { get; init; }
}

public sealed class FipsCmdbProductSyncResult
{
    public int Updated { get; set; }
    public int SkippedRetired { get; set; }
    public int SkippedNoSysId { get; set; }
    /// <summary>No Compass row with matching <see cref="CMDBProduct.CMDBID"/> — entries are never inserted by sync.</summary>
    public int SkippedNoLocalMatch { get; set; }
    /// <summary>Rows whose status was set by an active <see cref="FipsCmdbSyncRule"/> during this run.</summary>
    public int StatusSetByRules { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorSamples { get; } = new();
}

public sealed class FipsCmdbSingleProductSyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public bool StatusSetByRule { get; init; }
}

public interface IFipsCmdbProductSyncService
{
    /// <summary>
    /// Updates existing <see cref="CMDBProduct"/> rows where <see cref="CMDBProduct.CMDBID"/> matches an active CMDB service offering.
    /// Updates title, CMDB description, and CMDB-sourced contacts only. Does not create new products or change other fields.
    /// Inactive (retired) Compass products are skipped. A JSON snapshot of each CMDB row is stored on the product; optional rules may set status to Rejected or Inactive.
    /// </summary>
    Task<FipsCmdbProductSyncResult> SyncActiveServiceOfferingsAsync(
        string triggeredByEmail,
        CancellationToken cancellationToken = default,
        Func<FipsCmdbSyncProgressUpdate, ValueTask>? reportProgress = null);

    /// <summary>
    /// Fetches one CMDB row by <see cref="CMDBProduct.CMDBID"/> and applies the same update as the bulk sync.
    /// Skips when the product is <see cref="CMDBProductStatus.Inactive"/> (same as bulk behaviour).
    /// </summary>
    Task<FipsCmdbSingleProductSyncResult> SyncSingleProductAsync(Guid compassProductId, string triggeredByEmail, CancellationToken cancellationToken = default);
}
