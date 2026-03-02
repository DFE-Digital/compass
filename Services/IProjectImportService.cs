using Compass.Models;

namespace Compass.Services;

public interface IProjectImportService
{
    Task<ProjectImportResult> ImportProjectsFromCsvAsync(Stream csvStream, int? currentUserId = null, CancellationToken cancellationToken = default);
    Task<ProjectImportPreview> PreviewImportAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

public class ProjectImportResult
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ProjectImportError> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ProjectImportError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RowData { get; set; }
}

public class ProjectImportPreview
{
    public List<CsvProjectRow> Rows { get; set; } = new();
    public List<ProjectImportError> ValidationErrors { get; set; } = new();
    public Dictionary<string, string> FieldMapping { get; set; } = new(); // CSV Column -> Project Field
    public List<string> CsvColumns { get; set; } = new(); // All CSV column headers found
    public List<string> MappedColumns { get; set; } = new(); // CSV columns that are mapped
    public List<string> UnmappedColumns { get; set; } = new(); // CSV columns that are not mapped
    public List<string> MissingRequiredColumns { get; set; } = new(); // Required columns not found
}

public class CsvProjectRow
{
    public int RowNumber { get; set; }
    public string? Deliverable { get; set; }
    public string? DeliverableID { get; set; }
    public string? CurrentStatusUpdate { get; set; }
    public string? SRO { get; set; }
    public string? CurrentDeliveryPhase { get; set; }
    public string? DiscStartDate { get; set; }
    public string? AlphaStartDate { get; set; }
    public string? PrivateBetaStartDate { get; set; }
    public string? PublicBetaStartDate { get; set; }
    public string? ActivityType { get; set; }
    public string? Directorate { get; set; }
    public string? PolicyArea { get; set; }
    public string? BudgetOwner { get; set; }
    public string? RiskAppetite { get; set; }
    public string? ServiceUsers { get; set; }
    public string? ExternalInternal { get; set; }
    public string? PmoContact { get; set; }
    public string? PurposeBenefits { get; set; }
    public string? CurrentRAG { get; set; }
    // Add other CSV columns as needed
}

