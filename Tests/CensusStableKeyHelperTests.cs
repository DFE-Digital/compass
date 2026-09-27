using Compass.Data;
using Compass.Helpers;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Compass.Tests;

public class CensusStableKeyHelperTests
{
    [Theory]
    [InlineData("Primary user group type", "primary-user-group-type")]
    [InlineData("  Section Status  ", "section-status")]
    [InlineData("Linked services (list)", "linked-services-list")]
    [InlineData("What's the URL?", "whats-the-url")]
    [InlineData("A---B  C", "a-b-c")]
    public void FromName_Produces_Lowercase_Hyphenated_Slug(string name, string expected) =>
        Assert.Equal(expected, CensusStableKeyHelper.FromName(name));

    [Fact]
    public void EnsureUnique_Appends_Dash2_On_Collision()
    {
        var existing = new[] { "section-status", "purpose" };
        Assert.Equal(
            "section-status-2",
            CensusStableKeyHelper.EnsureUnique("section-status", existing));
        Assert.Equal(
            "section-status-3",
            CensusStableKeyHelper.EnsureUnique(
                "section-status",
                existing.Concat(new[] { "section-status-2" })));
    }

    [Fact]
    public void EnsureUnique_Returns_Base_When_Free() =>
        Assert.Equal(
            "primary-user-group-type",
            CensusStableKeyHelper.EnsureUnique(
                "primary-user-group-type",
                new[] { "section-status" }));
}

public class CoreCensusThemeStableKeyTests
{
    [Fact]
    public async Task CreateTheme_Generates_Key_From_Name_And_Collides_With_Dash2()
    {
        await using var db = CreateDb();
        var service = new CoreCensusThemeService(db, new AllowAllAccess());

        // Seed one theme so EnsureSeeded does not import the full catalogue.
        db.CoreCensusThemes.Add(new CoreCensusTheme
        {
            StableKey = "existing-theme",
            Name = "Existing theme",
            SortOrder = 10,
            IsServiceOffering = false,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var firstId = await service.CreateThemeAsync(
            new ServiceDataModelGroupInput { Name = "My new theme", StableKey = "ignored-client-key" },
            "admin@example.com");
        Assert.NotNull(firstId);
        var first = await db.CoreCensusThemes.SingleAsync(t => t.Id == firstId.Value);
        Assert.Equal("my-new-theme", first.StableKey);

        var secondId = await service.CreateThemeAsync(
            new ServiceDataModelGroupInput { Name = "My new theme" },
            "admin@example.com");
        Assert.NotNull(secondId);
        var second = await db.CoreCensusThemes.SingleAsync(t => t.Id == secondId.Value);
        Assert.Equal("my-new-theme-2", second.StableKey);
    }

    [Fact]
    public async Task UpdateTheme_Does_Not_Change_Existing_StableKey()
    {
        await using var db = CreateDb();
        var service = new CoreCensusThemeService(db, new AllowAllAccess());

        var theme = new CoreCensusTheme
        {
            StableKey = "original-key",
            Name = "Original name",
            SortOrder = 10,
            IsServiceOffering = false,
            IsActive = true
        };
        db.CoreCensusThemes.Add(theme);
        await db.SaveChangesAsync();

        var ok = await service.UpdateThemeAsync(
            theme.Id,
            new ServiceDataModelGroupInput
            {
                Name = "Renamed theme",
                StableKey = "attacker-posted-key"
            },
            "admin@example.com");
        Assert.True(ok);

        var reloaded = await db.CoreCensusThemes.AsNoTracking().SingleAsync(t => t.Id == theme.Id);
        Assert.Equal("original-key", reloaded.StableKey);
        Assert.Equal("Renamed theme", reloaded.Name);
    }

    [Fact]
    public async Task UpdateField_Does_Not_Change_Existing_StableKey()
    {
        await using var db = CreateDb();
        var service = new CoreCensusThemeService(db, new AllowAllAccess());

        var theme = new CoreCensusTheme
        {
            StableKey = "users",
            Name = "Users",
            SortOrder = 10,
            IsServiceOffering = false,
            IsActive = true
        };
        var field = new CoreCensusThemeField
        {
            StableKey = "primary-user-group-type",
            Label = "Primary user group type",
            SortOrder = 1,
            FieldType = ServiceDataModelFieldType.SingleChoice
        };
        theme.Fields.Add(field);
        db.CoreCensusThemes.Add(theme);
        await db.SaveChangesAsync();

        var ok = await service.UpdateFieldAsync(
            field.Id,
            new ServiceDataModelFieldInput
            {
                Label = "Renamed label",
                StableKey = "should-not-apply",
                FieldType = ServiceDataModelFieldType.Text
            },
            "admin@example.com");
        Assert.True(ok);

        var reloaded = await db.CoreCensusThemeFields.AsNoTracking().SingleAsync(f => f.Id == field.Id);
        Assert.Equal("primary-user-group-type", reloaded.StableKey);
        Assert.Equal("Renamed label", reloaded.Label);
        Assert.Equal(ServiceDataModelFieldType.Text, reloaded.FieldType);
    }

    [Fact]
    public async Task RepairCatalog_Does_Not_Overwrite_Admin_MultipleChoice_Type_Or_Label()
    {
        await using var db = CreateDb();
        var service = new CoreCensusThemeService(db, new AllowAllAccess());

        var theme = new CoreCensusTheme
        {
            StableKey = "users",
            Name = "Users",
            SortOrder = 10,
            IsServiceOffering = false,
            IsActive = true
        };
        var field = new CoreCensusThemeField
        {
            StableKey = "primary-user-group-type",
            Label = "Admin renamed primary user group type",
            Guidance = "Admin guidance that must stick",
            SortOrder = 2,
            FieldType = ServiceDataModelFieldType.MultipleChoice,
            OptionsLookupKey = "fips-user-groups"
        };
        theme.Fields.Add(field);
        db.CoreCensusThemes.Add(theme);
        await db.SaveChangesAsync();

        // EnsureSeeded runs catalog repair on every admin load; it must not reset admin edits.
        await service.EnsureSeededAsync();

        var reloaded = await db.CoreCensusThemeFields.AsNoTracking().SingleAsync(f => f.Id == field.Id);
        Assert.Equal(ServiceDataModelFieldType.MultipleChoice, reloaded.FieldType);
        Assert.Equal("Admin renamed primary user group type", reloaded.Label);
        Assert.Equal("Admin guidance that must stick", reloaded.Guidance);
        Assert.Equal("fips-user-groups", reloaded.OptionsLookupKey);
    }

    [Fact]
    public async Task RepairCatalog_Still_Forces_Services_And_ServiceLines_Types()
    {
        await using var db = CreateDb();
        var service = new CoreCensusThemeService(db, new AllowAllAccess());

        var theme = new CoreCensusTheme
        {
            StableKey = "estate",
            Name = "Estate",
            SortOrder = 1,
            IsServiceOffering = false,
            IsActive = true
        };
        theme.Fields.Add(new CoreCensusThemeField
        {
            StableKey = "linked-services",
            Label = "Linked services",
            SortOrder = 1,
            FieldType = ServiceDataModelFieldType.Text
        });
        theme.Fields.Add(new CoreCensusThemeField
        {
            StableKey = "linked-service-lines",
            Label = "Linked service lines",
            SortOrder = 2,
            FieldType = ServiceDataModelFieldType.MultipleChoice
        });
        db.CoreCensusThemes.Add(theme);
        await db.SaveChangesAsync();

        await service.EnsureSeededAsync();

        var services = await db.CoreCensusThemeFields.AsNoTracking()
            .SingleAsync(f => f.StableKey == "linked-services");
        var lines = await db.CoreCensusThemeFields.AsNoTracking()
            .SingleAsync(f => f.StableKey == "linked-service-lines");
        Assert.Equal(ServiceDataModelFieldType.Services, services.FieldType);
        Assert.Equal(ServiceDataModelFieldType.ServiceLines, lines.FieldType);
    }

    private static CompassDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CompassDbContext(options);
        // InMemory cannot run the relational audit column probe on SaveChanges.
        typeof(CompassDbContext)
            .GetField("_suppressAuditLogging", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(db, true);
        return db;
    }

    private sealed class AllowAllAccess : IServiceDataModelAccessService
    {
        public Task<bool> CanManageModelsAsync(string email) => Task.FromResult(true);

        public Task<bool> CanAccessProductAsync(CompassDbContext db, string email, Guid productId) =>
            Task.FromResult(true);

        public Task<bool> CanEditCensusAsync(CompassDbContext db, string email, Guid productId) =>
            Task.FromResult(true);

        public Task<bool> CanReviewCensusAsync(CompassDbContext db, string email, Guid productId) =>
            Task.FromResult(true);

        public Task<IReadOnlyList<Guid>> FilterAccessibleProductIdsAsync(
            CompassDbContext db,
            string email,
            IEnumerable<Guid> productIds) =>
            Task.FromResult<IReadOnlyList<Guid>>(productIds.ToList());
    }
}
