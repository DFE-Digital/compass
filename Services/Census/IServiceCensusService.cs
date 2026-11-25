using Compass.ViewModels.Modern;

namespace Compass.Services.Census;

public interface IServiceCensusService
{
    Task<ServiceCensusProductListViewModel> ListProductsAsync(
        string userEmail, bool showAll, string? search, string? contact, CancellationToken ct);

    Task<ServiceCensusTaskListViewModel?> GetTaskListAsync(
        Guid productId, string userEmail, bool showAll, CancellationToken ct);

    Task<ServiceCensusQuestionViewModel?> GetQuestionAsync(
        Guid productId, string taskKey, string userEmail, bool showAll, CancellationToken ct);

    Task<(bool Ok, string? Error, string? RedirectTaskKey)> SaveQuestionAsync(
        Guid productId, string taskKey, string userEmail, ServiceCensusSaveRequest request, CancellationToken ct);

    Task<ServiceCensusSubmitViewModel?> GetSubmitAsync(
        Guid productId, string userEmail, bool showAll, CancellationToken ct);

    Task<(bool Ok, string? Error)> SubmitAsync(Guid productId, string userEmail, CancellationToken ct);

    Task<(bool Ok, string? Error)> ReopenAsync(Guid productId, string userEmail, CancellationToken ct);

    Task<(bool Ok, string? Error)> ResetAsync(Guid productId, string userEmail, CancellationToken ct);

    Task<ServiceCensusReportViewModel> GetReportAsync(string? businessArea, CancellationToken ct, bool includeSubmissions = false);

    Task<ServiceCensusAnalysisViewModel> GetAnalysisAsync(
        string? businessArea,
        string? view,
        string? questionKey,
        Guid? productId,
        bool submittedOnly,
        CancellationToken ct,
        bool? groupByAnswer = null);

    Task<ServiceCensusPromptViewModel?> GetPromptAsync(string userEmail, CancellationToken ct);

    Task<IReadOnlyList<ServiceCensusProductRowViewModel>> SuggestProductsAsync(
        string userEmail, bool showAll, string? search, int limit, CancellationToken ct);

    Task<IReadOnlyList<CensusDependencySuggestion>> SuggestDependenciesAsync(
        string? search, Guid? excludeProductId, int limit, CancellationToken ct);
}
