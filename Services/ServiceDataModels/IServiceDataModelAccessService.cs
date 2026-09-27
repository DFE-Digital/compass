using Compass.Data;

namespace Compass.Services.ServiceDataModels;

public interface IServiceDataModelAccessService
{
    Task<bool> CanManageModelsAsync(string email);

    Task<bool> CanAccessProductAsync(CompassDbContext db, string email, Guid productId);

    Task<bool> CanEditCensusAsync(CompassDbContext db, string email, Guid productId);

    Task<bool> CanReviewCensusAsync(CompassDbContext db, string email, Guid productId);

    Task<IReadOnlyList<Guid>> FilterAccessibleProductIdsAsync(
        CompassDbContext db,
        string email,
        IEnumerable<Guid> productIds);
}
