using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class ServiceDataModelCanonicalPrefillTests
{
    private static ServiceDataModelField Field(
        string key,
        ServiceDataModelFieldType type,
        string? canonical = null,
        string? visibility = null,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            StableKey = key,
            Label = key,
            FieldType = type,
            CanonicalAttributeKey = canonical,
            VisibilityRuleJson = visibility,
            IsMandatory = true,
            CountsTowardsCompletion = true
        };

    private static ServiceDataModelFieldAnswerInput Input(ServiceDataModelField field, string? valueJson = null) =>
        new(
            field.Id,
            field.StableKey,
            field.FieldType,
            field.IsMandatory,
            field.CountsTowardsCompletion,
            field.VisibilityRuleJson,
            valueJson,
            IsValid: true);

    [Fact]
    public void Prefills_Title_Url_And_Description_From_Product_When_Unanswered()
    {
        var title = Field("confirm-title", ServiceDataModelFieldType.Text, "title");
        var summary = Field("service-summary", ServiceDataModelFieldType.MultilineText, "description");
        var url = Field("public-url", ServiceDataModelFieldType.Url, "url",
            """{"fieldKey":"has-public-url","equals":"Yes"}""");
        var hasUrl = Field("has-public-url", ServiceDataModelFieldType.YesNo);

        var product = new CMDBProduct
        {
            Title = "Find a school",
            UserDescription = "Helps parents find schools.",
            ProductURL = "https://www.gov.uk/find-school"
        };

        var result = ServiceDataModelCanonicalPrefill.Apply(
            new[] { Input(title), Input(summary), Input(hasUrl), Input(url) },
            new[] { title, summary, hasUrl, url },
            product,
            out var suppressed);

        Assert.Contains("has-public-url", suppressed);
        Assert.Equal("\"Find a school\"", result.First(r => r.StableKey == "confirm-title").ValueJson);
        Assert.Equal("\"Helps parents find schools.\"", result.First(r => r.StableKey == "service-summary").ValueJson);
        Assert.Equal("\"https://www.gov.uk/find-school\"", result.First(r => r.StableKey == "public-url").ValueJson);
        Assert.Equal("true", result.First(r => r.StableKey == "has-public-url").ValueJson);
    }

    [Fact]
    public void When_Url_Missing_Does_Not_Suppress_HasPublicUrl_And_Does_Not_Prefill_Url()
    {
        var url = Field("public-url", ServiceDataModelFieldType.Url, "url",
            """{"fieldKey":"has-public-url","equals":"Yes"}""");
        var hasUrl = Field("has-public-url", ServiceDataModelFieldType.YesNo);
        var product = new CMDBProduct { Title = "Service", ProductURL = null };

        var result = ServiceDataModelCanonicalPrefill.Apply(
            new[] { Input(hasUrl), Input(url) },
            new[] { hasUrl, url },
            product,
            out var suppressed);

        Assert.Empty(suppressed);
        Assert.Null(result.First(r => r.StableKey == "has-public-url").ValueJson);
        Assert.Null(result.First(r => r.StableKey == "public-url").ValueJson);
    }

    [Fact]
    public void Does_Not_Overwrite_Existing_Description_Or_Url_Answers()
    {
        var summary = Field("service-summary", ServiceDataModelFieldType.MultilineText, "description");
        var url = Field("public-url", ServiceDataModelFieldType.Url, "url");
        var product = new CMDBProduct
        {
            Title = "Register title",
            UserDescription = "Register description",
            ProductURL = "https://register.example"
        };

        var result = ServiceDataModelCanonicalPrefill.Apply(
            new[]
            {
                Input(summary, "\"Edited description\""),
                Input(url, "\"https://edited.example\"")
            },
            new[] { summary, url },
            product,
            out _);

        Assert.Equal("\"Edited description\"", result.First(r => r.StableKey == "service-summary").ValueJson);
        Assert.Equal("\"https://edited.example\"", result.First(r => r.StableKey == "public-url").ValueJson);
    }

    [Fact]
    public void Title_Always_Uses_Register_Value_Even_When_Answer_Differs()
    {
        var title = Field("confirm-title", ServiceDataModelFieldType.Text, "title");
        var product = new CMDBProduct { Title = "Register title" };

        var result = ServiceDataModelCanonicalPrefill.Apply(
            new[] { Input(title, "\"Proposed title\"") },
            new[] { title },
            product,
            out _);

        Assert.Equal("\"Register title\"", result.Single().ValueJson);
    }

    [Fact]
    public void Description_Falls_Back_To_CmdbDescription()
    {
        var summary = Field("service-summary", ServiceDataModelFieldType.MultilineText, "description");
        var product = new CMDBProduct
        {
            Title = "X",
            CMDBDescription = "From CMDB",
            UserDescription = null
        };

        var result = ServiceDataModelCanonicalPrefill.Apply(
            new[] { Input(summary) },
            new[] { summary },
            product,
            out _);

        Assert.Equal("\"From CMDB\"", result.Single().ValueJson);
    }

    [Fact]
    public void IsSameAsRegister_Ignores_Case_And_Whitespace()
    {
        Assert.True(ServiceDataModelCanonicalPrefill.IsSameAsRegister("  Abc ", "abc"));
        Assert.False(ServiceDataModelCanonicalPrefill.IsSameAsRegister("Abc", "Def"));
        Assert.True(ServiceDataModelCanonicalPrefill.IsSameAsRegister(null, "  "));
    }

    [Fact]
    public void Canonical_Key_Helpers_Classify_Title_Url_And_Description()
    {
        Assert.True(ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey("title"));
        Assert.True(ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey("Name"));
        Assert.False(ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey("url"));

        Assert.True(ServiceDataModelCanonicalPrefill.IsUrlCanonicalKey("product-url"));
        Assert.True(ServiceDataModelCanonicalPrefill.IsDescriptionCanonicalKey("user-description"));
        Assert.True(ServiceDataModelCanonicalPrefill.IsDirectProductUpdateCanonicalKey("url"));
        Assert.True(ServiceDataModelCanonicalPrefill.IsDirectProductUpdateCanonicalKey("description"));
        Assert.False(ServiceDataModelCanonicalPrefill.IsDirectProductUpdateCanonicalKey("title"));
        Assert.False(ServiceDataModelCanonicalPrefill.IsDirectProductUpdateCanonicalKey("phase"));
    }

    [Fact]
    public void Prefill_Makes_Url_Visible_In_Completion_When_Register_Has_Url()
    {
        var hasUrl = Field("has-public-url", ServiceDataModelFieldType.YesNo);
        var url = Field("public-url", ServiceDataModelFieldType.Url, "url",
            """{"fieldKey":"has-public-url","equals":"Yes"}""");
        var product = new CMDBProduct { Title = "S", ProductURL = "https://example.com" };

        var inputs = ServiceDataModelCanonicalPrefill.Apply(
            new[] { Input(hasUrl), Input(url) },
            new[] { hasUrl, url },
            product,
            out var suppressed);

        Assert.Contains("has-public-url", suppressed);
        var completion = ServiceDataModelCompletionCalculator.Calculate(inputs);
        Assert.Equal(2, completion.ApplicableCountingFields);
        Assert.Equal(2, completion.AnsweredCountingFields);
        Assert.Equal(100m, completion.FieldCompletionPercent);
    }

    [Fact]
    public void Apply_Does_Not_Throw_On_Duplicate_Field_StableKeys()
    {
        var enabled = Field("section-status", ServiceDataModelFieldType.SingleChoice);
        var duplicate = Field("section-status", ServiceDataModelFieldType.SingleChoice);
        duplicate.IsDisabled = true;
        var product = new CMDBProduct { Title = "Service" };

        var exception = Record.Exception(() =>
            ServiceDataModelCanonicalPrefill.Apply(
                new[] { Input(enabled), Input(duplicate) },
                new[] { enabled, duplicate },
                product,
                out _));

        Assert.Null(exception);
    }

    [Fact]
    public void IndexFieldsByStableKey_Prefers_First_Enabled_When_Duplicates_Exist()
    {
        var disabled = Field("section-status", ServiceDataModelFieldType.SingleChoice);
        disabled.IsDisabled = true;
        var enabled = Field("section-status", ServiceDataModelFieldType.SingleChoice);

        var indexed = ServiceDataModelCanonicalPrefill.IndexFieldsByStableKey(new[] { disabled, enabled });

        Assert.Single(indexed);
        Assert.Same(enabled, indexed["section-status"]);
    }
}
