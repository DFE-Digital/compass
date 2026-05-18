namespace Compass.Services;

/// <summary>Response from <c>GET /assessors/summary</c>.</summary>
public class SasAssessorsSummaryResponse
{
    public List<SasAssessorSummaryRow>? Assessors { get; set; }
}

public class SasAssessorSummaryRow
{
    public int? UserID { get; set; }
    /// <summary>SAS may return null when the assessor record is not linked in the assessor table.</summary>
    public int? AssessorID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
    public string? PrimaryRole { get; set; }
    public int? DepartmentID { get; set; }
    public string? DepartmentName { get; set; }
    public int AssessmentCount { get; set; }
    public Dictionary<string, int>? Outcomes { get; set; }
    public List<SasAssessorAssessmentRow>? Assessments { get; set; }
}

public class SasAssessorAssessmentRow
{
    public int AssessmentID { get; set; }
    public string? Name { get; set; }
    public string? PanelRole { get; set; }
    public string? Type { get; set; }
    public string? Phase { get; set; }
    public string? Status { get; set; }
    public string? Outcome { get; set; }
    public DateTime? AssessmentDateTime { get; set; }
}
