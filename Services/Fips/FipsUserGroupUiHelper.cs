using Compass.Data;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public static class FipsUserGroupUiHelper
{
    public static async Task<List<AdminFipsUserGroupRow>> LoadActiveTreeAsync(
        CompassDbContext context,
        CancellationToken cancellationToken = default)
    {
        var allGroups = await context.FipsUserGroups
            .AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return AdminFipsUserGroupTreeHelper.BuildFlatTree(allGroups);
    }
}
