namespace Compass.Services.Fips;

public sealed class FipsCompletionImportRowResult
{
    public int RowNumber { get; init; }
    public string ProductTitle { get; init; } = "";
    public string? FipsId { get; init; }
    public bool Success { get; set; }
    public List<string> Messages { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}

public sealed class FipsCompletionImportResult
{
    public int TotalRows { get; init; }
    public int UpdatedCount { get; init; }
    public int SkippedCount { get; init; }
    public int FailedCount { get; init; }
    public List<FipsCompletionImportRowResult> Rows { get; init; } = [];
}

public interface IFipsCompletionBulkImportService
{
    Task<FipsCompletionImportResult> ImportAsync(
        IReadOnlyList<FipsCompletionSpreadsheet.ImportRow> rows,
        string actorEmail,
        string? auditDisplayName,
        CancellationToken cancellationToken = default);
}
