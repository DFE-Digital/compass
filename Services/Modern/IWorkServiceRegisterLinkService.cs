using Compass.Models.Fips;
using Compass.ViewModels.Modern;

namespace Compass.Services.Modern;

public interface IWorkServiceRegisterLinkService
{
    Task<bool> CanLinkFromWorkItemAsync(int projectId, string userEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Primary contact, work item service owner, or operations console user (super admin, central ops admin, admin).
    /// </summary>
    Task<bool> CanCreateServiceOfferingFromWorkItemAsync(int projectId, string userEmail, CancellationToken cancellationToken = default);

    Task<bool> CanLinkFromServiceRegisterProductAsync(Guid cmdbProductId, string userEmail, CancellationToken cancellationToken = default);

    Task<int> CountLinksForWorkItemAsync(int projectId, CancellationToken cancellationToken = default);

    Task<int> CountLinksForServiceRegisterProductAsync(Guid cmdbProductId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkServiceRegisterLinkRow>> GetLinksForWorkItemAsync(
        int projectId,
        Func<Guid, string> productDetailUrl,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRegisterWorkLinkRow>> GetLinksForServiceRegisterProductAsync(
        Guid cmdbProductId,
        Func<int, string> workDetailUrl,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> LinkAsync(
        int projectId,
        Guid cmdbProductId,
        string userEmail,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string? Error)> UnlinkAsync(
        int projectProductId,
        string userEmail,
        CancellationToken cancellationToken = default);
}
