namespace Compass.Services;

public interface IAccessibilityIssuesService
{
    /// <summary>
    /// Get all issues for a given document ID (product)
    /// </summary>
    Task<IssuesResponse?> GetIssuesByDocumentIdAsync(string documentId);

    /// <summary>
    /// Get all issues with optional filters
    /// </summary>
    Task<IssuesResponse?> GetIssuesAsync(
        int? serviceId = null,
        string? documentId = null,
        string? status = null,
        string? wcagCriteria = null);

    /// <summary>
    /// Get a single issue by ID
    /// </summary>
    Task<IssueDto?> GetIssueAsync(int id);

    /// <summary>
    /// Get issue summary for a service
    /// </summary>
    Task<IssueSummaryDto?> GetIssueSummaryAsync(int serviceId);

    /// <summary>
    /// Get all services
    /// </summary>
    Task<ServicesResponse?> GetServicesAsync(bool? isActive = null, string? documentId = null);

    /// <summary>
    /// Get a service by ID
    /// </summary>
    Task<ServiceDto?> GetServiceAsync(int id);

    /// <summary>
    /// Get a service by DocumentId
    /// </summary>
    Task<ServiceDto?> GetServiceByDocumentIdAsync(string documentId);

    /// <summary>
    /// Get a service with its issues
    /// </summary>
    Task<ServiceWithIssuesDto?> GetServiceWithIssuesAsync(
        int id,
        string? status = null,
        string? wcagCriteria = null);

    /// <summary>
    /// Get service summary statistics
    /// </summary>
    Task<ServiceSummaryDto?> GetServiceSummaryAsync(int id);
}

public class IssuesResponse
{
    public int Count { get; set; }
    public List<IssueDto> Issues { get; set; } = new();
}

public class IssueDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WhereIssueOccurs { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? IssueType { get; set; }
    public string? SourceOfDiscovery { get; set; }
    public string? PlannedFixDate { get; set; }
    public string? ReasonNotFixed { get; set; }
    public AssignedToDto? AssignedTo { get; set; }
    public ServiceDto? Service { get; set; }
    public List<WcagCriterionDto> WcagCriteria { get; set; } = new();
    public bool IsOverdue { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class AssignedToDto
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class WcagCriterionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

public class ServicesResponse
{
    public int Count { get; set; }
    public List<ServiceDto> Services { get; set; } = new();
}

public class ServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public int NumericId { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool StatementInstalled { get; set; }
    public string? StatementInstalledAt { get; set; }
    public string? LastReviewedDate { get; set; }
    public string? TestMethod { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string UpdatedAt { get; set; } = string.Empty;
}

public class ServiceWithIssuesDto
{
    public ServiceDto Service { get; set; } = new();
    public IssuesResponse Issues { get; set; } = new();
}

public class IssueSummaryDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public int TotalIssues { get; set; }
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public int OverdueCount { get; set; }
    public int LevelACount { get; set; }
    public int LevelAACount { get; set; }
    public int LevelAAACount { get; set; }
    public int BestPracticeCount { get; set; }
}

public class ServiceSummaryDto
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public int NumericId { get; set; }
    public bool IsActive { get; set; }
    public bool StatementInstalled { get; set; }
    public int TotalIssues { get; set; }
    public int OpenCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public int OverdueCount { get; set; }
    public int LevelACount { get; set; }
    public int LevelAACount { get; set; }
    public int LevelAAACount { get; set; }
    public int BestPracticeCount { get; set; }
}
