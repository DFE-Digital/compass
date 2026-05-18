using Compass.Data;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public static class FipsManageDashboardBuilder
{
    public static async Task<FipsManageDashboardViewModel> BuildAsync(
        CompassDbContext context,
        string currentUserEmail,
        string fipsPublicBaseUrl,
        CancellationToken cancellationToken = default)
    {
        var counts = await FipsProductListingHelper.BuildSubNavModelAsync(
            context, "active", currentUserEmail, cancellationToken);

        var serviceLinesCount = 0;
        try
        {
            serviceLinesCount = await context.ServiceLines.AsNoTracking().CountAsync(cancellationToken);
        }
        catch
        {
            // Optional table unavailable in some environments.
        }

        return new FipsManageDashboardViewModel
        {
            MyProductsCount = counts.MyProductsCount,
            ActiveProductsCount = counts.AllProductsCount,
            EnterpriseProductsCount = counts.EnterpriseProductsCount,
            ServiceLinesCount = serviceLinesCount,
            FipsPublicBaseUrl = fipsPublicBaseUrl.TrimEnd('/')
        };
    }
}
