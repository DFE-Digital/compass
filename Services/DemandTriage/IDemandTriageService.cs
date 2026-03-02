using Compass.Models.DemandTriage;

namespace Compass.Services.DemandTriage;

public interface IDemandTriageService
{
    // ── Demand request CRUD ──────────────────────────────────────────────────
    Task<DemandTriageRequest> CreateDraftAsync(string ownerEmail, string ownerName);
    Task<DemandTriageRequest?> GetByIdAsync(int id, bool includeAll = true);
    Task<List<DemandTriageRequest>> GetAllAsync(string? statusFilter = null, string? ownerEmail = null);
    Task UpdateCaptureFormAsync(int id, DemandTriageRequest updates, string actorEmail, string actorName);

    // ── Workflow transitions ─────────────────────────────────────────────────
    Task SubmitAsync(int id, string actorEmail, string actorName);
    Task StartExploratoryReviewAsync(int id, string actorEmail, string actorName);
    Task SaveExploratoryReviewAsync(int id, DemandExploratoryReview data, string actorEmail, string actorName);
    Task CompleteExploratoryReviewAsync(int id, string actorEmail, string actorName);
    Task ReturnForClarificationAsync(int id, string reason, string actorEmail, string actorName);
    Task StartScoringAsync(int id, string actorEmail, string actorName);
    Task SaveAnswersAsync(int id, List<AnswerInput> answers, string actorEmail, string actorName);
    Task<ScorecardCalculationResult> FinaliseScorecard(int id, string actorEmail, string actorName);
    Task SendToTriageAsync(int id, string actorEmail, string actorName);
    Task RecordTriageOutcomeAsync(int id, DemandTriageOutcome outcome, string actorEmail, string actorName);
    Task CloseAsync(int id, string actorEmail, string actorName);
    Task AdminOverrideStatusAsync(int id, string newStatus, string reason, string actorEmail, string actorName);
    Task UnlockScorecardAsync(int id, string actorEmail, string actorName);

    // ── Project creation ─────────────────────────────────────────────────────
    Task<int> CreateProjectFromDemandAsync(int id, string actorEmail, string actorName);

    // ── Reference generation ─────────────────────────────────────────────────
    Task<string> GenerateNextReferenceAsync();

    // ── Validation ───────────────────────────────────────────────────────────
    Task<List<string>> ValidateForSubmissionAsync(int id);
    Task<List<string>> ValidateForScoringFinalisationAsync(int id);
}

/// <summary>Input model for saving scoring answers.</summary>
public class AnswerInput
{
    public string QuestionCode { get; set; } = string.Empty;
    public string? AnswerValue { get; set; }
    public string? FreeText { get; set; }
}
