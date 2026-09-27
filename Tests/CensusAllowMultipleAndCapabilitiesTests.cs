using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.Models.ServiceDataModels;
using Compass.Services;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Compass.Tests;

public class CensusAllowMultipleAndCapabilitiesTests
{
    [Fact]
    public void AllowMultiple_Text_StoresJsonArrayOfNonBlankStrings()
    {
        var field = new CoreCensusThemeField
        {
            FieldType = ServiceDataModelFieldType.Text,
            AllowMultiple = true
        };

        var stored = CensusAnswerValueNormalizer.Normalize(
            field,
            JsonSerializer.Serialize(new[] { " first ", "", "second", "  " }));

        Assert.Equal(JsonSerializer.Serialize(new[] { "first", "second" }), stored);
        Assert.True(ServiceDataModelCompletionCalculator.IsAnswered(
            new ServiceDataModelFieldAnswerInput(
                Guid.NewGuid(), "outcomes", ServiceDataModelFieldType.Text,
                false, true, null, stored)));
    }

    [Fact]
    public void AllowMultiple_Text_EmptyList_IsUnanswered()
    {
        var field = new CoreCensusThemeField
        {
            FieldType = ServiceDataModelFieldType.Text,
            AllowMultiple = true
        };

        var stored = CensusAnswerValueNormalizer.Normalize(
            field,
            JsonSerializer.Serialize(new[] { "", "  " }));

        Assert.Null(stored);
        Assert.False(ServiceDataModelCompletionCalculator.IsAnswered(
            new ServiceDataModelFieldAnswerInput(
                Guid.NewGuid(), "outcomes", ServiceDataModelFieldType.Text,
                false, true, null, stored)));
    }

    [Fact]
    public void Lookup_MultipleSelect_PersistsSelectedKeysAsArray()
    {
        var keyA = Guid.NewGuid().ToString();
        var keyB = Guid.NewGuid().ToString();
        var field = new CoreCensusThemeField
        {
            FieldType = ServiceDataModelFieldType.Lookup,
            AllowMultiple = true,
            OptionsLookupKey = CensusAdminLookupOptions.Capabilities
        };

        var stored = CensusAnswerValueNormalizer.Normalize(
            field,
            JsonSerializer.Serialize(new[] { keyA, keyB, "" }));

        using var doc = JsonDocument.Parse(stored!);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(2, doc.RootElement.GetArrayLength());
        Assert.Contains(keyA, doc.RootElement.EnumerateArray().Select(x => x.GetString()));
        Assert.Contains(keyB, doc.RootElement.EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public void Capability_DisplayLabel_IsTitleReference()
    {
        Assert.Equal("Risk (GRC27372)", CapabilityDisplay.Format("Risk", "GRC27372"));
        var entity = new CapabilityLookup { Title = "Risk", Reference = "GRC27372" };
        Assert.Equal("Risk (GRC27372)", entity.DisplayLabel);
    }

    [Fact]
    public async Task Capability_Reference_MustBeUnique()
    {
        await using var db = CreateDb();
        var service = new CapabilityAdminService(db, new AllowAllAccess());

        var first = await service.CreateAsync(
            new CapabilityEditViewModel { Title = "Risk", Reference = "GRC27372" },
            "admin@example.com");
        Assert.True(first.Ok);

        var second = await service.CreateAsync(
            new CapabilityEditViewModel { Title = "Other", Reference = "GRC27372" },
            "admin@example.com");
        Assert.False(second.Ok);
        Assert.Contains("already used", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Resolve_Capabilities_UsesTitleReferenceLabel()
    {
        await using var db = CreateDb();
        var id = Guid.NewGuid();
        db.CapabilityLookups.Add(new CapabilityLookup
        {
            Id = id,
            Title = "Risk",
            Reference = "GRC27372",
            Description = "Manage risk in the department",
            SortOrder = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var options = await CensusAdminLookupOptions.ResolveAsync(db, CensusAdminLookupOptions.Capabilities);
        Assert.Single(options);
        Assert.Equal(id.ToString(), options[0].ValueKey);
        Assert.Equal("Risk (GRC27372)", options[0].Label);
    }

    [Fact]
    public async Task RepairCatalog_DoesNotOverwrite_AllowMultiple_Or_LookupType()
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
        theme.Fields.Add(new CoreCensusThemeField
        {
            StableKey = "primary-user-group-type",
            Label = "Primary user group type",
            SortOrder = 2,
            FieldType = ServiceDataModelFieldType.Lookup,
            OptionsLookupKey = "fips-user-groups",
            AllowMultiple = true
        });
        db.CoreCensusThemes.Add(theme);
        await db.SaveChangesAsync();

        await service.EnsureSeededAsync();

        var reloaded = await db.CoreCensusThemeFields.AsNoTracking()
            .SingleAsync(f => f.StableKey == "primary-user-group-type");
        Assert.Equal(ServiceDataModelFieldType.Lookup, reloaded.FieldType);
        Assert.True(reloaded.AllowMultiple);
        Assert.Equal("fips-user-groups", reloaded.OptionsLookupKey);
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
            CompassDbContext db, string email, IEnumerable<Guid> productIds) =>
            Task.FromResult<IReadOnlyList<Guid>>(productIds.ToList());
    }
}
