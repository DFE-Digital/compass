using Compass.Data;
using Compass.Models;
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

    [Fact]
    public void BindableLookups_IncludesNamedAdminPanels_AndIsAlphabetical()
    {
        var lookups = CensusAdminLookupOptions.BindableLookups;

        Assert.Contains(lookups, l => l.Key == "priority-outcomes" && l.DisplayName == "Priority outcomes");
        Assert.Contains(lookups, l => l.Key == "directorates" && l.DisplayName == "Directorates");
        Assert.Contains(lookups, l => l.Key == "mission-pillars" && l.DisplayName == "Mission pillars");
        Assert.Contains(lookups, l => l.Key == "work-tagging" && l.DisplayName == "Thematic tags");
        Assert.Contains(lookups, l => l.Key == CensusAdminLookupOptions.Capabilities);
        Assert.Contains(lookups, l => l.Key == CensusAdminLookupOptions.FipsUserGroups);

        var names = lookups.Select(l => l.DisplayName).ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList(), names);
    }

    [Fact]
    public async Task Resolve_PriorityOutcomesAndMissionPillars_UsesAdminTitles()
    {
        await using var db = CreateDb();
        db.Missions.Add(new Mission { Id = 1, Title = "Mission A", IsDeleted = false });
        db.Missions.Add(new Mission { Id = 2, Title = "Deleted mission", IsDeleted = true });
        db.Objectives.Add(new Objective { Id = 10, Title = "Outcome X", IsDeleted = false });
        db.Objectives.Add(new Objective { Id = 11, Title = "Deleted outcome", IsDeleted = true });
        db.DirectorateLookups.Add(new DirectorateLookup { Id = 5, Name = "Dir One", IsActive = true, SortOrder = 1 });
        db.WorkItemTagLookups.Add(new WorkItemTagLookup { Id = 7, Name = "Tag One", IsActive = true, SortOrder = 1 });
        await db.SaveChangesAsync();

        var pillars = await CensusAdminLookupOptions.ResolveAsync(db, "mission-pillars");
        Assert.Single(pillars);
        Assert.Equal("1", pillars[0].ValueKey);
        Assert.Equal("Mission A", pillars[0].Label);

        var outcomes = await CensusAdminLookupOptions.ResolveAsync(db, "priority-outcomes");
        Assert.Single(outcomes);
        Assert.Equal("10", outcomes[0].ValueKey);
        Assert.Equal("Outcome X", outcomes[0].Label);

        var dirs = await CensusAdminLookupOptions.ResolveAsync(db, "directorates");
        Assert.Contains(dirs, d => d.ValueKey == "5" && d.Label == "Dir One");

        var tags = await CensusAdminLookupOptions.ResolveAsync(db, "work-tagging");
        Assert.Contains(tags, t => t.ValueKey == "7" && t.Label == "Tag One");
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
