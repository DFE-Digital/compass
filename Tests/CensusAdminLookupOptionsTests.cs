using Compass.Data;
using Compass.Models.Fips;
using Compass.Services.ServiceDataModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Compass.Tests;

public class CensusAdminLookupOptionsTests
{
    [Fact]
    public async Task Resolve_FipsUserGroups_IncludesNestedAndInactiveLikeAdmin()
    {
        await using var db = CreateDb();
        db.FipsUserGroups.AddRange(
            new FipsUserGroup { Id = 10, Name = "Root", ParentId = null, DisplayOrder = 1, Active = true },
            new FipsUserGroup { Id = 11, Name = "Inactive parent", ParentId = 10, DisplayOrder = 1, Active = false },
            new FipsUserGroup { Id = 12, Name = "Nested child", ParentId = 11, DisplayOrder = 1, Active = true },
            new FipsUserGroup { Id = 13, Name = "Active sibling", ParentId = 10, DisplayOrder = 2, Active = true });
        await db.SaveChangesAsync();

        var options = await CensusAdminLookupOptions.ResolveAsync(db, CensusAdminLookupOptions.FipsUserGroups);

        Assert.Equal(4, options.Count);
        Assert.Contains(options, o => o.ValueKey == "10" && o.Label == "Root");
        Assert.Contains(options, o => o.ValueKey == "11" && o.Label == "— Inactive parent");
        Assert.Contains(options, o => o.ValueKey == "12" && o.Label == "—— Nested child");
        Assert.Contains(options, o => o.ValueKey == "13" && o.Label == "— Active sibling");
    }

    private static CompassDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CompassDbContext(options);
        typeof(CompassDbContext)
            .GetField("_suppressAuditLogging", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(db, true);
        return db;
    }
}
