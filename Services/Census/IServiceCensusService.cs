using Compass.ViewModels.Modern;

namespace Compass.Services.Census;

public interface IServiceCensusService
{
    Task<ServiceCensusProductListViewModel> ListProductsAsync(
        string userEmail, bool showAll, string? search, CancellationToken ct);

    Task<ServiceCensusTaskListViewModel?> GetTaskListAsync(
        Guid productId, string userEmail, bool showAll, CancellationToken ct);

    Task<ServiceCensusQuestionViewModel?> GetQuestionAsync(
        Guid productId, string taskKey, string userEmail, bool showAll, CancellationToken ct);

    Task<(bool Ok, string? Error, string? RedirectTaskKey)> SaveQuestionAsync(
        Guid productId, string taskKey, string userEmail, ServiceCensusSaveRequest request, CancellationToken ct);

    Task<ServiceCensusSubmitViewModel?> GetSubmitAsync(
        Guid productId, string userEmail, bool showAll, CancellationToken ct);

    Task<(bool Ok, string? Error)> SubmitAsync(Guid productId, string userEmail, CancellationToken ct);

    Task<ServiceCensusReportViewModel> GetReportAsync(string? businessArea, CancellationToken ct, bool includeSubmissions = false);

    Task<ServiceCensusPromptViewModel?> GetPromptAsync(string userEmail, CancellationToken ct);

    Task<IReadOnlyList<ServiceCensusProductRowViewModel>> SuggestProductsAsync(
        string userEmail, bool showAll, string? search, int limit, CancellationToken ct);

    Task<IReadOnlyList<CensusDependencySuggestion>> SuggestDependenciesAsync(
        string? search, Guid? excludeProductId, int limit, CancellationToken ct);
}
