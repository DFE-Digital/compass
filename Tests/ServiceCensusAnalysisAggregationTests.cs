using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class ServiceCensusAnalysisAggregationTests
{
    private static ServiceCensusThemeDefinition Theme(
        string key,
        string name,
        params ServiceCensusFieldDefinition[] fields) =>
        new()
        {
            StableKey = key,
            Name = name,
            SortOrder = 1,
            Fields = fields
        };

    private static ServiceCensusFieldDefinition Field(
        string key,
        string label,
        ServiceDataModelFieldType type,
        bool allowMultiple = false,
        params string[] optionKeys) =>
        new()
        {
            StableKey = key,
            Label = label,
            FieldType = type,
            AllowMultiple = allowMultiple,
            SortOrder = 1,
            OptionKeys = optionKeys
        };

    private static ServiceCensusAnswerFact Fact(
        Guid productId,
        string title,
        string themeKey,
        string fieldKey,
        ServiceDataModelFieldType type,
        bool hasAnswer,
        IReadOnlyList<string>? choiceKeys = null,
        IReadOnlyList<string>? textValues = null,
        IReadOnlyList<string>? linkedKeys = null,
        decimal? number = null,
        bool allowMultiple = false) =>
        new()
        {
            AssignmentId = Guid.NewGuid(),
            ProductId = productId,
            ProductTitle = title,
            ThemeStableKey = themeKey,
            ThemeName = "Theme",
            FieldStableKey = fieldKey,
            FieldLabel = fieldKey,
            FieldType = type,
            AllowMultiple = allowMultiple,
            HasAnswer = hasAnswer,
            ChoiceKeys = choiceKeys ?? Array.Empty<string>(),
            TextValues = textValues ?? Array.Empty<string>(),
            LinkedItemKeys = linkedKeys ?? Array.Empty<string>(),
            NumberValue = number
        };

    [Fact]
    public void ParseAnswer_ChoiceCounts_YesNoAndMultiple()
    {
        var yes = ServiceCensusAnalysisAggregation.ParseAnswer(
            ServiceDataModelFieldType.YesNo, false, "\"Yes\"");
        Assert.True(yes.HasAnswer);
        Assert.Equal(new[] { "Yes" }, yes.ChoiceKeys);

        var multi = ServiceCensusAnalysisAggregation.ParseAnswer(
            ServiceDataModelFieldType.MultipleChoice, false, "[\"a\",\"b\"]");
        Assert.True(multi.HasAnswer);
        Assert.Equal(2, multi.ChoiceKeys.Count);
    }

    [Fact]
    public void BuildMetricDetail_Choice_CountsAndPercentages()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var p3 = Guid.NewGuid();
        var theme = Theme("users", "Users",
            Field("channel", "Channel", ServiceDataModelFieldType.SingleChoice, false, "web", "phone"));
        var field = theme.Fields[0];
        var facts = new[]
        {
            Fact(p1, "A", "users", "channel", ServiceDataModelFieldType.SingleChoice, true, choiceKeys: new[] { "web" }),
            Fact(p2, "B", "users", "channel", ServiceDataModelFieldType.SingleChoice, true, choiceKeys: new[] { "web" }),
            Fact(p3, "C", "users", "channel", ServiceDataModelFieldType.SingleChoice, true, choiceKeys: new[] { "phone" }),
        };
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["web"] = "Web",
            ["phone"] = "Phone"
        };

        var detail = ServiceCensusAnalysisAggregation.BuildMetricDetail(theme, field, facts, labels, 1, 25);

        Assert.Equal(3, detail.AnsweredServiceCount);
        Assert.Equal("choice", detail.AggregationKind);
        var web = Assert.Single(detail.Options, o => o.OptionKey == "web");
        Assert.Equal(2, web.Count);
        Assert.Equal(66.7m, web.Percent);
        var phone = Assert.Single(detail.Options, o => o.OptionKey == "phone");
        Assert.Equal(1, phone.Count);
        Assert.Equal(33.3m, phone.Percent);
    }

    [Fact]
    public void BuildMetricDetail_Number_AverageMinMax()
    {
        var theme = Theme("cost", "Cost",
            Field("fte", "FTE", ServiceDataModelFieldType.Number));
        var field = theme.Fields[0];
        var facts = new[]
        {
            Fact(Guid.NewGuid(), "A", "cost", "fte", ServiceDataModelFieldType.Number, true, number: 2m),
            Fact(Guid.NewGuid(), "B", "cost", "fte", ServiceDataModelFieldType.Number, true, number: 4m),
            Fact(Guid.NewGuid(), "C", "cost", "fte", ServiceDataModelFieldType.Number, true, number: 6m),
        };

        var detail = ServiceCensusAnalysisAggregation.BuildMetricDetail(
            theme, field, facts, new Dictionary<string, string>(), 1, 25);

        Assert.Equal("number", detail.AggregationKind);
        Assert.Equal(3, detail.NumberCount);
        Assert.Equal(4m, detail.NumberAverage);
        Assert.Equal(2m, detail.NumberMin);
        Assert.Equal(6m, detail.NumberMax);
    }

    [Fact]
    public void BuildThemeSummaries_ScopedProductExclusion_DoesNotLeakAnswers()
    {
        var inScope = Guid.NewGuid();
        var outOfScope = Guid.NewGuid();
        var theme = Theme("purpose", "Purpose",
            Field("outcomes", "Outcomes", ServiceDataModelFieldType.Text));

        // Only in-scope facts are passed — mirrors access filtering before aggregation.
        var scopedFacts = new[]
        {
            Fact(inScope, "Visible", "purpose", "outcomes", ServiceDataModelFieldType.Text, true,
                textValues: new[] { "Improve outcomes" }),
        };

        var summaries = ServiceCensusAnalysisAggregation.BuildThemeSummaries(new[] { theme }, scopedFacts);
        var row = Assert.Single(summaries);
        Assert.Equal(1, row.AnsweredServiceCount);

        var metric = ServiceCensusAnalysisAggregation.BuildMetricDetail(
            theme, theme.Fields[0], scopedFacts, new Dictionary<string, string>(), 1, 25);
        Assert.Equal(1, metric.AnsweredServiceCount);
        Assert.DoesNotContain(metric.TextValues, v => v.DisplayValue.Contains(outOfScope.ToString(), StringComparison.Ordinal));

        // Explicitly prove out-of-scope facts are required to inflate counts — and we did not include them.
        var leaked = scopedFacts.Concat(new[]
        {
            Fact(outOfScope, "Secret", "purpose", "outcomes", ServiceDataModelFieldType.Text, true,
                textValues: new[] { "Classified answer" })
        }).ToList();
        var withLeak = ServiceCensusAnalysisAggregation.BuildMetricDetail(
            theme, theme.Fields[0], leaked, new Dictionary<string, string>(), 1, 25);
        Assert.Equal(2, withLeak.AnsweredServiceCount);
        Assert.Equal(1, metric.AnsweredServiceCount);
    }

    [Fact]
    public void BuildOptionDrill_ReturnsOnlyMatchingProducts()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var theme = Theme("users", "Users",
            Field("channel", "Channel", ServiceDataModelFieldType.SingleChoice, false, "web", "phone"));
        var facts = new[]
        {
            Fact(p1, "Web service", "users", "channel", ServiceDataModelFieldType.SingleChoice, true, choiceKeys: new[] { "web" }),
            Fact(p2, "Phone service", "users", "channel", ServiceDataModelFieldType.SingleChoice, true, choiceKeys: new[] { "phone" }),
        };

        var drill = ServiceCensusAnalysisAggregation.BuildOptionDrill(
            theme, theme.Fields[0], "web", "Web", facts, 1, 25);

        Assert.Equal(1, drill.ProductTotalCount);
        Assert.Equal(p1, Assert.Single(drill.Products).ProductId);
    }

    [Fact]
    public void HasMeaningfulAnswer_RejectsEmptyArrays()
    {
        Assert.False(ServiceCensusAnalysisAggregation.HasMeaningfulAnswer(null));
        Assert.False(ServiceCensusAnalysisAggregation.HasMeaningfulAnswer("[]"));
        Assert.False(ServiceCensusAnalysisAggregation.HasMeaningfulAnswer("\"\""));
        Assert.True(ServiceCensusAnalysisAggregation.HasMeaningfulAnswer("\"Yes\""));
        Assert.True(ServiceCensusAnalysisAggregation.HasMeaningfulAnswer("[\"a\"]"));
    }
}
