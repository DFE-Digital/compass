using Compass.Models.Fips;

namespace Compass.Services.Aiss;

public interface IAissProductAccessibilityService
{
    /// <summary>
    /// Resolves the AISS service for a Compass service register product and loads summary plus open issues.
    /// AISS matches services by Compass <c>uniqueId</c> (numeric register id), not CMS document id.
    /// </summary>
    Task<FipsProductAissAccessibility> LoadForProductAsync(
        int registerNumericUniqueId,
        Guid registerProductId,
        CancellationToken cancellationToken = default);
}
