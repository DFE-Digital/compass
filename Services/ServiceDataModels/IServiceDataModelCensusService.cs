using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

public interface IServiceDataModelCensusService
{
    Task<CensusProductAssignmentsViewModel?> GetAssignmentsForProductAsync(Guid productId, string email);

    Task<CensusWorkListViewModel> GetWorkListAsync(
        string email,
        string tab,
        string? statusFilter,
        string? search,
        int? businessAreaId = null,
        int? phaseId = null,
        int? typeId = null,
        int? channelId = null,
        int? userGroupId = null);

    /// <summary>
    /// Ensures a standing Service Census record exists for the product (lazy create) and returns its id.
    /// Returns null when the user cannot access the product, the product is missing, or no census is published.
    /// </summary>
    Task<Guid?> EnsureStandingServiceCensusAsync(Guid productId, string email);

    Task<CensusAssignmentFormViewModel?> GetAssignmentFormAsync(Guid assignmentId, string email);

    /// <param name="markThemeCompleteGroupId">
    /// When set, after saving answers mark that theme complete (by its stable key) for this assignment.
    /// </param>
    Task<CensusSaveAnswersResult> SaveAnswersAsync(
        Guid assignmentId,
        string email,
        IReadOnlyDictionary<Guid, string?> answers,
        DateTime? expectedUpdatedUtc = null,
        Guid? markThemeCompleteGroupId = null);

    Task<CensusActionResult> SubmitAsync(Guid assignmentId, string email);

    Task<CensusActionResult> RequestChangesAsync(Guid assignmentId, string email, string note);

    Task<CensusActionResult> ReviewAsync(Guid assignmentId, string email, string? attestationNote);

    Task RecalculateCompletionAsync(ServiceDataModelAssignment assignment);
}
