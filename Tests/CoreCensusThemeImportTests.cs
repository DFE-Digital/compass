using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class CoreCensusThemeImportTests
{
    [Fact]
    public void Catalog_IncludesServiceOffering_ButImportableExcludesIt()
    {
        var offering = CoreCensusThemeCatalog.All
            .Single(t => t.StableKey == CoreCensusThemeCatalog.ServiceOfferingKey);

        Assert.True(offering.IsServiceOffering);
        Assert.Equal("Service offering", offering.Name);
        Assert.DoesNotContain(
            CoreCensusThemeCatalog.Importable,
            t => t.IsServiceOffering || t.StableKey == CoreCensusThemeCatalog.ServiceOfferingKey);
        Assert.True(CoreCensusThemeCatalog.Importable.Count >= 10);
    }

    [Fact]
    public void Catalog_SectionStatus_StableKeys_Are_Unique_Across_Themes()
    {
        var fieldKeys = CoreCensusThemeCatalog.Importable
            .SelectMany(t => t.Fields.Select(f => f.StableKey))
            .ToList();

        Assert.DoesNotContain(fieldKeys, k => string.Equals(k, "section-status", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(fieldKeys.Count, fieldKeys.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (var theme in CoreCensusThemeCatalog.Importable)
        {
            var expected = CoreCensusThemeCatalog.SectionStatusStableKey(theme.StableKey);
            Assert.Contains(theme.Fields, f => f.StableKey == expected);
        }
    }

    [Fact]
    public void Catalog_Estate_UsesServicesAndServiceLinesListTypes()
    {
        var estate = CoreCensusThemeCatalog.Get("estate")!;
        var linkedServices = estate.Fields.Single(f => f.StableKey == "linked-services");
        var linkedLines = estate.Fields.Single(f => f.StableKey == "linked-service-lines");

        Assert.Equal(ServiceDataModelFieldType.Services, linkedServices.FieldType);
        Assert.Equal(ServiceDataModelFieldType.ServiceLines, linkedLines.FieldType);
    }

    [Fact]
    public void Catalog_SectionStatus_Guidance_ExplainsWhenToComplete()
    {
        Assert.Contains(
            "Only complete this section if none of the questions can be answered, or there is no evidence to provide.",
            CoreCensusThemeCatalog.SectionInformationStatusGuidance,
            StringComparison.Ordinal);

        foreach (var theme in CoreCensusThemeCatalog.Importable)
        {
            var status = theme.Fields.Single(f =>
                f.StableKey == CoreCensusThemeCatalog.SectionStatusStableKey(theme.StableKey));
            Assert.Equal("Section information status", status.Label);
            Assert.Equal(CoreCensusThemeCatalog.SectionInformationStatusGuidance, status.Guidance);
        }
    }

    [Fact]
    public void Catalog_PrimaryUserGroupType_BindsToFipsUserGroupsLookup()
    {
        var users = CoreCensusThemeCatalog.Get("users")!;
        var field = users.Fields.Single(f => f.StableKey == "primary-user-group-type");
        Assert.Equal(ServiceDataModelFieldType.Lookup, field.FieldType);
        Assert.Equal(CensusAdminLookupOptions.FipsUserGroups, field.OptionsLookupKey);
        Assert.Equal("fips-user-groups", field.OptionsLookupKey);
        Assert.False(field.AllowMultiple);
    }

    [Fact]
    public void Catalog_Capabilities_IsLookupBoundToCapabilitiesCatalogue()
    {
        var theme = CoreCensusThemeCatalog.Get("capability")!;
        var field = theme.Fields.Single(f => f.StableKey == "capabilities");
        Assert.Equal(ServiceDataModelFieldType.Lookup, field.FieldType);
        Assert.Equal(CensusAdminLookupOptions.Capabilities, field.OptionsLookupKey);
        Assert.True(field.AllowMultiple);
    }

    [Fact]
    public void AdminLookupOptions_ListsFipsUserGroupsAndCapabilities()
    {
        Assert.Contains(
            CensusAdminLookupOptions.BindableLookups,
            l => l.Key == CensusAdminLookupOptions.FipsUserGroups);
        Assert.Contains(
            CensusAdminLookupOptions.BindableLookups,
            l => l.Key == CensusAdminLookupOptions.Capabilities);
        Assert.True(CensusAdminLookupOptions.IsKnown("fips-user-groups"));
        Assert.True(CensusAdminLookupOptions.IsKnown("capabilities"));
    }

    [Fact]
    public void AdminFipsUserGroupTree_IncludesNestedAndInactiveGroups()
    {
        // Census options must match the admin panel: full hierarchy, not roots/active-only.
        var root = new Compass.Models.Fips.FipsUserGroup
        {
            Id = 1, Name = "Workforce", ParentId = null, DisplayOrder = 1, Active = true
        };
        var inactiveParent = new Compass.Models.Fips.FipsUserGroup
        {
            Id = 2, Name = "Legacy cohort", ParentId = 1, DisplayOrder = 1, Active = false
        };
        var nestedChild = new Compass.Models.Fips.FipsUserGroup
        {
            Id = 3, Name = "Social worker", ParentId = 2, DisplayOrder = 1, Active = true
        };
        var sibling = new Compass.Models.Fips.FipsUserGroup
        {
            Id = 4, Name = "Teachers", ParentId = 1, DisplayOrder = 2, Active = true
        };

        var rows = Compass.ViewModels.Modern.AdminFipsUserGroupTreeHelper.BuildFlatTree(
            [root, inactiveParent, nestedChild, sibling]);

        Assert.Equal(4, rows.Count);
        Assert.Contains(rows, r => r.Id == 1 && r.Depth == 0);
        Assert.Contains(rows, r => r.Id == 2 && r.Depth == 1 && !r.Active);
        Assert.Contains(rows, r => r.Id == 3 && r.Depth == 2);
        Assert.Contains(rows, r => r.Id == 4 && r.Depth == 1);

        var labels = rows
            .Select(r => Compass.ViewModels.Modern.AdminFipsUserGroupTreeHelper.FormatIndentedLabel(r.Name, r.Depth))
            .ToList();
        Assert.Contains("— Legacy cohort", labels);
        Assert.Contains("—— Social worker", labels);
    }

    [Fact]
    public void Catalog_ModelsSchemaDomains_WithQuestions()
    {
        var expectedKeys = new[]
        {
            "estate", "purpose", "users", "ownership", "capability", "functionality",
            "technology", "data", "performance", "governance", "risk", "findings", "evidence"
        };

        foreach (var key in expectedKeys)
        {
            var theme = CoreCensusThemeCatalog.Importable.Single(t => t.StableKey == key);
            Assert.False(string.IsNullOrWhiteSpace(theme.Name));
            Assert.NotEmpty(theme.Fields);
        }

        var users = CoreCensusThemeCatalog.Get("users")!;
        Assert.Contains(users.Fields, f => f.StableKey == "user-groups");
        Assert.Contains(users.Fields, f => f.StableKey == "user-needs");
        Assert.Contains(users.Fields, f => f.FieldType == ServiceDataModelFieldType.Lookup);
        Assert.Contains(users.Fields, f => f.FieldType == ServiceDataModelFieldType.SingleChoice);
    }

    [Fact]
    public void Planner_ExcludesServiceOffering_EvenWhenRequested()
    {
        var candidates = CoreCensusThemeCatalog.All
            .Where(t => t.StableKey is "source" or "purpose" or "users")
            .ToList();

        var plan = CoreThemeImportPlanner.Plan(
            candidates,
            existingCoreKeys: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            existingStableKeys: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Equal(new[] { "purpose", "users" }, plan.ToImport);
        Assert.Equal(new[] { "source" }, plan.SkippedServiceOffering);
        Assert.Empty(plan.SkippedAlreadyPresent);
    }

    [Fact]
    public void SharedStructure_ExcludesServiceOfferingAndDisabled()
    {
        var themes = new List<CoreCensusTheme>
        {
            new()
            {
                StableKey = "source",
                Name = "Service offering",
                IsServiceOffering = true,
                IsActive = true,
                Fields = { new CoreCensusThemeField { StableKey = "product-name", Label = "Name" } }
            },
            new()
            {
                StableKey = "purpose",
                Name = "Purpose",
                IsServiceOffering = false,
                IsActive = true,
                Fields =
                {
                    new CoreCensusThemeField { StableKey = "outcomes", Label = "Outcomes (shared)", IsDisabled = false },
                    new CoreCensusThemeField { StableKey = "hidden", Label = "Hidden", IsDisabled = true }
                }
            },
            new()
            {
                StableKey = "users",
                Name = "Users",
                IsServiceOffering = false,
                IsActive = false,
                Fields = { new CoreCensusThemeField { StableKey = "user-groups", Label = "Groups" } }
            }
        };

        var active = themes.Where(t => t.IsActive && !t.IsServiceOffering).ToList();
        Assert.Single(active);
        Assert.Equal("purpose", active[0].StableKey);

        var fields = SharedCensusAnswerKeys.FlattenActiveFields(themes);
        Assert.Single(fields);
        Assert.Equal("outcomes", fields[0].StableKey);
        Assert.Equal("Outcomes (shared)", fields[0].Label);
    }

    [Fact]
    public void AnswerLookup_UsesFieldStableKey_NotCopiedFieldId()
    {
        var answers = new List<ServiceDataModelAnswer>
        {
            new()
            {
                ServiceDataModelFieldId = Guid.NewGuid(),
                FieldStableKey = "outcomes",
                ValueJson = "\"from shared base\""
            },
            new()
            {
                ServiceDataModelFieldId = Guid.NewGuid(),
                Field = new ServiceDataModelField { StableKey = "benefits" },
                ValueJson = "\"legacy by field nav\""
            }
        };

        var outcomes = SharedCensusAnswerKeys.FindByStableKey(answers, "outcomes");
        Assert.NotNull(outcomes);
        Assert.Equal("\"from shared base\"", outcomes!.ValueJson);

        var benefits = SharedCensusAnswerKeys.FindByStableKey(answers, "benefits");
        Assert.NotNull(benefits);
        Assert.Equal("\"legacy by field nav\"", benefits!.ValueJson);

        Assert.Null(SharedCensusAnswerKeys.FindByStableKey(answers, "missing"));
    }

    [Fact]
    public void Completion_DropsWhenNewRequiredSharedQuestionAdded()
    {
        var answered = new ServiceDataModelFieldAnswerInput(
            Guid.NewGuid(), "outcomes", ServiceDataModelFieldType.Text,
            IsMandatory: true, CountsTowardsCompletion: true, VisibilityRuleJson: null,
            ValueJson: "\"done\"");

        var before = ServiceDataModelCompletionCalculator.Calculate(new[] { answered });
        Assert.Equal(100m, before.FieldCompletionPercent);

        var newRequired = new ServiceDataModelFieldAnswerInput(
            Guid.NewGuid(), "benefits", ServiceDataModelFieldType.Text,
            IsMandatory: true, CountsTowardsCompletion: true, VisibilityRuleJson: null,
            ValueJson: null);

        var after = ServiceDataModelCompletionCalculator.Calculate(new[] { answered, newRequired });
        Assert.Equal(50m, after.FieldCompletionPercent);
        Assert.Equal(2, after.ApplicableCountingFields);
        Assert.Equal(1, after.AnsweredCountingFields);
    }

    [Fact]
    public void AdminDefaultCensusThemesController_RequiresAuthorizeAndRequireAdmin()
    {
        var type = typeof(Compass.Controllers.Modern.ModernAdminDefaultCensusThemesController);
        Assert.Contains(type.GetCustomAttributes(inherit: true), a => a.GetType().Name == "AuthorizeAttribute");
        Assert.Contains(type.GetCustomAttributes(inherit: true), a => a.GetType().Name == "RequireAdminAttribute");
    }
}
