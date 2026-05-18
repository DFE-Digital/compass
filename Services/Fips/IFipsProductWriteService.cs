using Compass.Models.Fips;

namespace Compass.Services.Fips;

public sealed class FipsProductWriteOutcome
{
    public bool NotFound { get; init; }
    public bool Forbidden { get; init; }
    /// <summary>Field labels that changed (empty if nothing to save).</summary>
    public List<string> Changes { get; init; } = new();
}

public interface IFipsProductWriteService
{
    /// <summary>
    /// Updates user-facing FIPS fields. When <paramref name="requireServiceOwnerManager"/> is true, only contacts with
    /// <see cref="CMDBProductContact.CanManage"/> and matching email may update.
    /// </summary>
    Task<FipsProductWriteOutcome> TryUpdateAsync(
        Guid productId,
        string actorEmail,
        string? auditChangedByDisplay,
        bool requireServiceOwnerManager,
        string? userDescription,
        int? phaseId,
        string? productURL,
        int[]? businessAreaIds,
        int[]? channelIds,
        int[]? userGroupIds,
        int[]? typeIds,
        int[]? categorisationItemIds = null,
        int? reportingContactUserId = null,
        /// <summary>When null, <see cref="CMDBProduct.IsEnterpriseService"/> is left unchanged.</summary>
        bool? isEnterpriseService = null,
        CancellationToken cancellationToken = default);

    /// <summary>Changes product status with the same permission rule as <see cref="TryUpdateAsync"/>.</summary>
    Task<FipsProductWriteOutcome> TryChangeStatusAsync(
        Guid productId,
        string actorEmail,
        string? auditChangedByDisplay,
        bool requireServiceOwnerManager,
        CMDBProductStatus newStatus,
        CancellationToken cancellationToken = default);

    /// <summary>Updates only the product URL (for API / integrations; no service-owner check).</summary>
    Task<FipsProductWriteOutcome> TryUpdateProductUrlOnlyAsync(
        Guid productId,
        string actorEmail,
        string? auditChangedByDisplay,
        string? productUrl,
        CancellationToken cancellationToken = default);
}
