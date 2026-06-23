using System.Security.Claims;
using Compass.ViewModels.Modern;

namespace Compass.Services.DdtStandards;

public interface IDdtStandardsWorkflowService
{
    Task<DdtStandardEditViewModel> BuildEditViewModelAsync(int? standardId, ClaimsPrincipal user, CancellationToken ct = default);

    Task<DdtStandardWorkflowContextViewModel> BuildWorkflowContextAsync(int standardId, ClaimsPrincipal user, CancellationToken ct = default);

    Task<DdtStandardDetailToolbarViewModel> BuildDetailToolbarAsync(int standardId, ClaimsPrincipal user, CancellationToken ct = default);

    Task<DdtStandardHistoryViewModel?> BuildHistoryViewModelAsync(int standardId, CancellationToken ct = default);

    Task<WorkflowOperationResult> EnsureDraftForEditAsync(int publishedStandardId, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> SaveDraftAsync(DdtStandardDraftInput input, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> SubmitForReviewAsync(int id, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> ApproveAsync(int id, string? comment, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> RejectAsync(int id, string reason, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> PublishAsync(int id, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> UnpublishAsync(int id, string reason, ClaimsPrincipal user, CancellationToken ct = default);

    Task<WorkflowOperationResult> DeleteDraftAsync(int id, ClaimsPrincipal user, CancellationToken ct = default);
}

public sealed class WorkflowOperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SuccessMessage { get; init; }
    public int? StandardId { get; init; }

    public static WorkflowOperationResult Ok(int? standardId, string? message = null) =>
        new() { Success = true, StandardId = standardId, SuccessMessage = message };

    public static WorkflowOperationResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public sealed class DdtStandardDraftInput
{
    public int? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Purpose { get; set; }
    public string? Criteria { get; set; }
    public string? HowToMeet { get; set; }
    public string? Governance { get; set; }
    public string? LegalBasis { get; set; }
    public bool LegalStandard { get; set; }
    public int? ValidityPeriod { get; set; }
    public string? RelatedGuidance { get; set; }
    public List<int>? CategoryIds { get; set; }
    public List<int>? PhaseIds { get; set; }
    public string? OwnerObjectIds { get; set; }
    public string? ContactObjectIds { get; set; }
}
