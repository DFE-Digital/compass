using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Compass.Tests;

public class CensusThemeSchemaExportImportTests
{
    [Fact]
    public async Task Export_IncludesThemeQuestionAndCapability()
    {
        await using var db = CreateDb();
        var themeId = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var capabilityId = Guid.NewGuid();

        db.CoreCensusThemes.Add(new CoreCensusTheme
        {
            Id = themeId,
            StableKey = "purpose",
            Name = "Purpose",
            Guidance = "Why the service exists",
            SortOrder = 10,
            IsServiceOffering = false,
            IsActive = true,
            Fields =
            {
                new CoreCensusThemeField
                {
                    Id = fieldId,
                    CoreCensusThemeId = themeId,
                    StableKey = "outcomes",
                    Label = "Outcomes",
                    Guidance = "Describe outcomes",
                    FieldType = ServiceDataModelFieldType.Text,
                    IsMandatory = true,
                    CountsTowardsCompletion = true,
                    SortOrder = 1,
                    AllowMultiple = false,
                    IsDisabled = false,
                    Options =
                    {
                        new CoreCensusThemeFieldOption
                        {
                            ValueKey = "example",
                            Label = "Example",
                            SortOrder = 1
                        }
                    }
                }
            }
        });
        // Service offering must never appear in the portable schema.
        db.CoreCensusThemes.Add(new CoreCensusTheme
        {
            StableKey = CoreCensusThemeCatalog.ServiceOfferingKey,
            Name = "Service offering",
            IsServiceOffering = true,
            IsActive = true,
            SortOrder = 0
        });
        db.CapabilityLookups.Add(new CapabilityLookup
        {
            Id = capabilityId,
            Title = "Risk",
            Description = "Manage risk",
            Reference = "GRC27372",
            SortOrder = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new CoreCensusThemeService(db, new AllowAllAccess());
        var package = await service.ExportSchemaAsync("admin@example.com");

        Assert.NotNull(package);
        Assert.Equal(1, package!.SchemaVersion);
        Assert.DoesNotContain(package.Themes, t => t.StableKey == CoreCensusThemeCatalog.ServiceOfferingKey);
        Assert.Contains(package.Themes, t => t.StableKey == "purpose");
        var theme = package.Themes.Single(t => t.StableKey == "purpose");
        Assert.Equal("Purpose", theme.Name);
        Assert.Contains(theme.Questions, q => q.StableKey == "outcomes");
        var question = theme.Questions.Single(q => q.StableKey == "outcomes");
        Assert.Equal("Outcomes", question.Label);
        Assert.Equal(nameof(ServiceDataModelFieldType.Text), question.FieldType);
        Assert.Contains(question.Options, o => o.ValueKey == "example");
        Assert.Contains(package.Capabilities, c => c.Reference == "GRC27372" && c.Title == "Risk");
        // Never export system ids — identity is stable key / reference.
        var json = JsonSerializer.Serialize(package);
        Assert.DoesNotContain(capabilityId.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(themeId.ToString(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(fieldId.ToString(), json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Import_UpdatesLabelByStableKey_DoesNotDeleteExtraLocalQuestion()
    {
        await using var db = CreateDb();
        var themeId = Guid.NewGuid();
        var localOnlyFieldId = Guid.NewGuid();
        var capabilityId = Guid.NewGuid();

        db.CoreCensusThemes.Add(new CoreCensusTheme
        {
            Id = themeId,
            StableKey = "purpose",
            Name = "Purpose",
            SortOrder = 10,
            IsServiceOffering = false,
            IsActive = true,
            Fields =
            {
                new CoreCensusThemeField
                {
                    CoreCensusThemeId = themeId,
                    StableKey = "outcomes",
                    Label = "Old outcomes label",
                    FieldType = ServiceDataModelFieldType.Text,
                    SortOrder = 1,
                    CountsTowardsCompletion = true
                },
                new CoreCensusThemeField
                {
                    Id = localOnlyFieldId,
                    CoreCensusThemeId = themeId,
                    StableKey = "local-only-question",
                    Label = "Local only",
                    FieldType = ServiceDataModelFieldType.Text,
                    SortOrder = 2,
                    CountsTowardsCompletion = true,
                    IsDisabled = false
                }
            }
        });
        db.CapabilityLookups.Add(new CapabilityLookup
        {
            Id = capabilityId,
            Title = "Old title",
            Reference = "GRC27372",
            SortOrder = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var package = new CensusThemeSchemaPackage
        {
            SchemaVersion = 1,
            Themes =
            {
                new CensusThemeSchemaThemeDto
                {
                    StableKey = "purpose",
                    Name = "Purpose",
                    SortOrder = 10,
                    Disabled = false,
                    Questions =
                    {
                        new CensusThemeSchemaQuestionDto
                        {
                            StableKey = "outcomes",
                            Label = "Updated outcomes label",
                            FieldType = "Text",
                            SortOrder = 1,
                            IsMandatory = true,
                            CountsTowardsCompletion = true,
                            Disabled = false
                        }
                    }
                }
            },
            Capabilities =
            {
                new CensusThemeSchemaCapabilityDto
                {
                    Title = "Risk management",
                    Description = "Updated description",
                    Reference = "GRC27372"
                }
            }
        };

        var service = new CoreCensusThemeService(db, new AllowAllAccess());
        var result = await service.ImportSchemaAsync(package, disableNotInFile: false, "admin@example.com");

        Assert.True(result.Ok, result.Error);
        Assert.False(result.DisableNotInFile);
        Assert.Contains("left unchanged", result.SummaryMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, result.QuestionsUpdated);
        Assert.Equal(1, result.CapabilitiesUpdated);

        var outcomes = await db.CoreCensusThemeFields.AsNoTracking()
            .SingleAsync(f => f.StableKey == "outcomes");
        Assert.Equal("Updated outcomes label", outcomes.Label);

        var localOnly = await db.CoreCensusThemeFields.AsNoTracking()
            .SingleAsync(f => f.StableKey == "local-only-question");
        Assert.Equal(localOnlyFieldId, localOnly.Id);
        Assert.False(localOnly.IsDisabled);
        Assert.Equal("Local only", localOnly.Label);

        var capability = await db.CapabilityLookups.AsNoTracking()
            .SingleAsync(c => c.Reference == "GRC27372");
        Assert.Equal(capabilityId, capability.Id);
        Assert.Equal("Risk management", capability.Title);
        Assert.Equal("Updated description", capability.Description);
    }

    [Fact]
    public async Task Import_RejectsInvalidJsonShape_MissingStableKey()
    {
        await using var db = CreateDb();
        db.CoreCensusThemes.Add(new CoreCensusTheme
        {
            StableKey = "purpose",
            Name = "Purpose",
            IsServiceOffering = false,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var package = new CensusThemeSchemaPackage
        {
            SchemaVersion = 1,
            Themes =
            {
                new CensusThemeSchemaThemeDto
                {
                    StableKey = "",
                    Name = "Broken",
                    Questions = { new CensusThemeSchemaQuestionDto { StableKey = "x", Label = "X", FieldType = "Text" } }
                }
            }
        };

        var service = new CoreCensusThemeService(db, new AllowAllAccess());
        var result = await service.ImportSchemaAsync(package, false, "admin@example.com");
        Assert.False(result.Ok);
        Assert.Contains("stableKey", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RoundTrip_ExportThenImport_PreservesStableKeys()
    {
        await using var source = CreateDb();
        var theme = new CoreCensusTheme
        {
            StableKey = "capability",
            Name = "Capability",
            SortOrder = 50,
            IsServiceOffering = false,
            IsActive = true
        };
        theme.Fields.Add(new CoreCensusThemeField
        {
            StableKey = "capabilities",
            Label = "Capabilities",
            FieldType = ServiceDataModelFieldType.Lookup,
            OptionsLookupKey = CensusAdminLookupOptions.Capabilities,
            AllowMultiple = true,
            SortOrder = 1,
            CountsTowardsCompletion = true
        });
        source.CoreCensusThemes.Add(theme);
        source.CapabilityLookups.Add(new CapabilityLookup
        {
            Title = "Risk",
            Reference = "GRC27372",
            SortOrder = 1,
            IsActive = true
        });
        await source.SaveChangesAsync();

        var exporter = new CoreCensusThemeService(source, new AllowAllAccess());
        var package = await exporter.ExportSchemaAsync("admin@example.com");
        Assert.NotNull(package);

        await using var target = CreateDb();
        // Seed a different local capability id for the same reference.
        var targetCapId = Guid.NewGuid();
        target.CapabilityLookups.Add(new CapabilityLookup
        {
            Id = targetCapId,
            Title = "Risk (old)",
            Reference = "GRC27372",
            SortOrder = 1,
            IsActive = true
        });
        await target.SaveChangesAsync();

        var importer = new CoreCensusThemeService(target, new AllowAllAccess());
        var result = await importer.ImportSchemaAsync(package!, disableNotInFile: false, "admin@example.com");
        Assert.True(result.Ok, result.Error);

        // Target starts empty of themes → EnsureSeeded may populate the catalogue first;
        // import must still upsert our exported theme/question by stable key.
        var importedTheme = await target.CoreCensusThemes
            .Include(t => t.Fields)
            .SingleAsync(t => t.StableKey == "capability");
        Assert.Equal("Capability", importedTheme.Name);
        var field = importedTheme.Fields.Single(f => f.StableKey == "capabilities");
        Assert.Equal(ServiceDataModelFieldType.Lookup, field.FieldType);
        Assert.True(field.AllowMultiple);
        Assert.Equal(CensusAdminLookupOptions.Capabilities, field.OptionsLookupKey);

        var cap = await target.CapabilityLookups.SingleAsync(c => c.Reference == "GRC27372");
        Assert.Equal(targetCapId, cap.Id);
        Assert.Equal("Risk", cap.Title);
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
