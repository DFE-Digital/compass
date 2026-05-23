namespace Compass.Services.Aiss;

/// <summary>Deserialised response from AISS <c>GET /api/v1/services/{id}</c> and <c>by-document</c>.</summary>
public sealed class AissServiceDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? ServiceRegisterUniqueId { get; set; }
    public Guid? CompassRegisterProductId { get; set; }
    public string? DocumentId { get; set; }
    public int NumericId { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool StatementInstalled { get; set; }
    public string? StatementInstalledAt { get; set; }
    public string? LastReviewedDate { get; set; }
    public string? TestMethod { get; set; }
}

/// <summary>Deserialised response from AISS <c>GET /api/v1/services/{id}/summary</c>.</summary>
public sealed class AissServiceSummaryDto
{
    public int ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string? DocumentId { get; set; }
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

/// <summary>Deserialised response from AISS <c>GET /api/v1/services/{id}/issues</c>.</summary>
public sealed class AissServiceIssuesApiResponse
{
    public AissServiceDto? Service { get; set; }
    public AissServiceIssuesBlock? Issues { get; set; }
}

public sealed class AissServiceIssuesBlock
{
    public int Count { get; set; }
    public List<AissServiceIssueDto>? Items { get; set; }
}

public sealed class AissServiceIssueDto
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? IssueType { get; set; }
    public string? PlannedFixDate { get; set; }
    public bool IsOverdue { get; set; }
    public string? CreatedAt { get; set; }
    public List<AissServiceIssueWcagDto>? WcagCriteria { get; set; }
}

public sealed class AissServiceIssueWcagDto
{
    public string? Code { get; set; }
    public string? Title { get; set; }
    public string? Level { get; set; }
}

/// <summary>Deserialised response from AISS <c>GET /api/v1/services/by-register/{id}/accessibility</c>.</summary>
public sealed class AissServiceAccessibilityApiResponse
{
    public AissServiceDto? Service { get; set; }
    public AissServiceSummaryDto? Summary { get; set; }
    public AissServiceIssuesBlock? OpenIssues { get; set; }
}
