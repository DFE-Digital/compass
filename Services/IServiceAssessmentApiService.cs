namespace Compass.Services;

public interface IServiceAssessmentApiService
{
    Task<ServiceAssessmentResponse?> GetActionsByStandardAsync();

    Task<SasPublishedSummaryResponse?> GetPublishedSummaryAsync(CancellationToken cancellationToken = default);

    Task<SasActionsByStandardResponse?> GetPublishedActionsByStandardAsync(CancellationToken cancellationToken = default);

    Task<SasAssessorsSummaryResponse?> GetAssessorsSummaryAsync(CancellationToken cancellationToken = default);

    Task<SasProductAssessmentsResponse?> GetAssessmentsByProductIdAsync(
        string productFipsId,
        CancellationToken cancellationToken = default);
}

public class ServiceAssessmentResponse
{
    public List<Assessment>? Assessments { get; set; }
}

public class Assessment
{
    public int AssessmentID { get; set; }
    public string? AssessmentName { get; set; }
    public string? AssessmentStatus { get; set; }
    public string? AssessmentType { get; set; }
    public string? AssessmentOutcome { get; set; }
    public string? AssessmentPhase { get; set; }
    public List<ActionsByStandard>? ActionsByStandard { get; set; }
}

public class ActionsByStandard
{
    public int Standard { get; set; }
    public string? StandardTitle { get; set; }
    public string? StandardOutcome { get; set; }
    public List<ActionItem>? Actions { get; set; }
}

public class ActionItem
{
    public string? ActionID { get; set; }
    public int AssessmentID { get; set; }
    public int Standard { get; set; }
    public string? StandardTitle { get; set; }
    public string? StandardOutcome { get; set; }
    public string? Comments { get; set; }
    public string? Status { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? Created { get; set; }
    public int? AssignedTo { get; set; }
    public string? UniqueID { get; set; }
    public DateTime? EstimatedResolutionDate { get; set; }
}

