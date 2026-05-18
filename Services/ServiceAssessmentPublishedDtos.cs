namespace Compass.Services;

/// <summary>Response from <c>GET /assessments/published/summary</c>.</summary>
public class SasPublishedSummaryResponse
{
    public SasSummaryBlock? Summaries { get; set; }
    public List<SasPublishedAssessmentRow>? Assessments { get; set; }
}

public class SasSummaryBlock
{
    public int TotalAssessments { get; set; }
    public Dictionary<string, int>? ByOutcome { get; set; }
    public Dictionary<string, int>? ByType { get; set; }
    public Dictionary<string, int>? ByPhase { get; set; }
    public Dictionary<string, int>? ByYear { get; set; }
}

public class SasPublishedAssessmentRow
{
    public int AssessmentID { get; set; }
    public string? FIPS_ID { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Phase { get; set; }
    public string? Outcome { get; set; }
    public string? Portfolio { get; set; }
    public DateTime? AssessmentDateTime { get; set; }
}

/// <summary>Response from <c>GET /assessments/published/actions-by-standard</c>.</summary>
public class SasActionsByStandardResponse
{
    public List<SasActionByStandardRow>? ActionsByStandard { get; set; }
    public int TotalStandards { get; set; }
}

public class SasActionByStandardRow
{
    public int Standard { get; set; }
    public string? ActionCount { get; set; }
    public string? AssessmentCount { get; set; }
}

/// <summary>Response from <c>GET /product/{fipsId}</c>.</summary>
public class SasProductAssessmentsResponse
{
    public string? Fips_Id { get; set; }
    public List<SasProductAssessmentRow>? Assessments { get; set; }
    public int Count { get; set; }
}

public class SasProductAssessmentRow
{
    public int AssessmentID { get; set; }
    public string? FIPS_ID { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Phase { get; set; }
    public string? Outcome { get; set; }
    public DateTime? AssessmentDateTime { get; set; }
}
