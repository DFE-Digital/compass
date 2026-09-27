using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

/// <summary>
/// Guards the census work-list: Service Register populations enriched with standing census
/// completion (not started = 0/N, never "not assigned").
/// </summary>
public class CensusWorkListBuilderTests
{
    private static FipsProductRow Product(Guid id, string title, string? owner = null) => new()
    {
        Id = id,
        Title = title,
        Status = CMDBProductStatus.Active,
        ServiceOwner = owner
    };

    private static CensusWorkListBuilder.PublishedCensusBaseline Baseline(int applicable = 5) =>
        new(Guid.NewGuid(), applicable);

    [Fact]
    public void NormalizeTab_DefaultsLegacyAndUnknown_ToYour()
    {
        Assert.Equal(CensusWorkListBuilder.TabYour, CensusWorkListBuilder.NormalizeTab(null));
        Assert.Equal(CensusWorkListBuilder.TabYour, CensusWorkListBuilder.NormalizeTab("my-work"));
        Assert.Equal(CensusWorkListBuilder.TabYour, CensusWorkListBuilder.NormalizeTab("managed"));
        Assert.Equal(CensusWorkListBuilder.TabAll, CensusWorkListBuilder.NormalizeTab("all"));
        Assert.Equal(CensusWorkListBuilder.TabBusinessArea, CensusWorkListBuilder.NormalizeTab("business-area"));
    }

    [Fact]
    public void YourProducts_WithoutStandingRecord_ShowsNotStartedZeroOfTotal()
    {
        var namedId = Guid.NewGuid();
        var named = new[] { Product(namedId, "Named service") };
        var progress = new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>();
        var baseline = Baseline(5);

        var rows = CensusWorkListBuilder.BuildRows(named, progress, baseline, statusFilter: null);

        Assert.Single(rows);
        Assert.Equal(namedId, rows[0].Id);
        Assert.False(rows[0].HasCensusAssignment);
        Assert.True(rows[0].CensusPublishedAvailable);
        Assert.Equal(ServiceDataModelAssignmentStatus.NotStarted, rows[0].CensusStatus);
        Assert.Equal("Not started", rows[0].CensusStatusLabel);
        Assert.Equal(0, rows[0].CensusAnsweredCountingFields);
        Assert.Equal(5, rows[0].CensusApplicableCountingFields);
        Assert.Equal(0m, rows[0].CensusFieldCompletionPercent);
    }

    [Fact]
    public void WithoutPublishedCensus_ShowsEmptyCompletion_NotFakeZero()
    {
        var namedId = Guid.NewGuid();
        var named = new[] { Product(namedId, "Named service") };

        var rows = CensusWorkListBuilder.BuildRows(
            named,
            new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>(),
            publishedBaseline: null,
            statusFilter: null);

        Assert.Single(rows);
        Assert.False(rows[0].CensusPublishedAvailable);
        Assert.Null(rows[0].CensusAnsweredCountingFields);
        Assert.Null(rows[0].CensusApplicableCountingFields);
        Assert.Null(rows[0].CensusFieldCompletionPercent);
        Assert.Equal("Not started", rows[0].CensusStatusLabel);
    }

    [Fact]
    public void WithStandingRecord_UsesProgressCounts()
    {
        var namedId = Guid.NewGuid();
        var standingId = Guid.NewGuid();
        var named = new[] { Product(namedId, "Named service") };
        var progress = new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>
        {
            [namedId] = new(
                standingId,
                ServiceDataModelAssignmentStatus.InProgress,
                40m,
                AnsweredCountingFields: 2,
                ApplicableCountingFields: 5,
                ServiceCensusDefaults.StableKey,
                DateTime.UtcNow)
        };

        var rows = CensusWorkListBuilder.BuildRows(named, progress, Baseline(5), statusFilter: null);

        Assert.Single(rows);
        Assert.True(rows[0].HasCensusAssignment);
        Assert.Equal(standingId, rows[0].CensusAssignmentId);
        Assert.Equal(ServiceDataModelAssignmentStatus.InProgress, rows[0].CensusStatus);
        Assert.Equal("In progress", rows[0].CensusStatusLabel);
        Assert.Equal(2, rows[0].CensusAnsweredCountingFields);
        Assert.Equal(5, rows[0].CensusApplicableCountingFields);
        Assert.Equal(40m, rows[0].CensusFieldCompletionPercent);
    }

    [Fact]
    public void StatusFilter_NotStarted_IncludesProductsWithoutStandingRecord()
    {
        var unstarted = Guid.NewGuid();
        var inProgress = Guid.NewGuid();
        var named = new[]
        {
            Product(unstarted, "Not started yet"),
            Product(inProgress, "In progress")
        };
        var progress = new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>
        {
            [inProgress] = new(
                Guid.NewGuid(),
                ServiceDataModelAssignmentStatus.InProgress,
                20m,
                1,
                5,
                ServiceCensusDefaults.StableKey,
                DateTime.UtcNow)
        };

        var rows = CensusWorkListBuilder.BuildRows(
            named, progress, Baseline(5),
            nameof(ServiceDataModelAssignmentStatus.NotStarted));

        Assert.Single(rows);
        Assert.Equal(unstarted, rows[0].Id);
    }

    [Fact]
    public void SelectPrimaryProgress_PrefersServiceCensusStableKey()
    {
        var censusId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var primary = CensusWorkListBuilder.SelectPrimaryProgress(
        [
            new(otherId, ServiceDataModelAssignmentStatus.Reviewed, 100m, 5, 5, "other-model", DateTime.UtcNow.AddHours(1)),
            new(censusId, ServiceDataModelAssignmentStatus.NotStarted, 0m, 0, 5, ServiceCensusDefaults.StableKey, DateTime.UtcNow)
        ]);

        Assert.NotNull(primary);
        Assert.Equal(censusId, primary!.StandingRecordId);
    }

    [Fact]
    public void GroupByBusinessArea_PutsUnassignedUnderNoBusinessArea()
    {
        var withArea = Product(Guid.NewGuid(), "With area");
        var without = Product(Guid.NewGuid(), "Without area");
        var areas = new Dictionary<Guid, IReadOnlyList<string>>
        {
            [withArea.Id] = new[] { "Schools" }
        };

        var groups = CensusWorkListBuilder.GroupByBusinessArea([withArea, without], areas);

        Assert.Equal(2, groups.Count);
        Assert.Equal("Schools", groups[0].Name);
        Assert.Equal(CensusWorkListBuilder.NoBusinessAreaLabel, groups[1].Name);
        Assert.Single(groups[0].Products);
        Assert.Equal(without.Id, groups[1].Products[0].Id);
    }

    [Fact]
    public void GroupByBusinessArea_DuplicatesProductAcrossMultipleAreas()
    {
        var multi = Product(Guid.NewGuid(), "Multi");
        var areas = new Dictionary<Guid, IReadOnlyList<string>>
        {
            [multi.Id] = new[] { "Alpha", "Beta" }
        };

        var groups = CensusWorkListBuilder.GroupByBusinessArea([multi], areas);

        Assert.Equal(2, groups.Count);
        Assert.All(groups, g => Assert.Single(g.Products));
    }

    [Fact]
    public void FormatStatusLabel_NeverSaysAssigned()
    {
        Assert.Equal("Not started", CensusWorkListBuilder.FormatStatusLabel(null));
        Assert.Equal("Not started", CensusWorkListBuilder.FormatStatusLabel(ServiceDataModelAssignmentStatus.NotStarted));
        Assert.DoesNotContain("assign", CensusWorkListBuilder.FormatStatusLabel(null), StringComparison.OrdinalIgnoreCase);
    }
}
