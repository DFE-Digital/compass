using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class ServiceDataModelCompletionReportGroupingTests
{
    private static IReadOnlyList<string> CleanNames(string?[]? names) =>
        (names ?? Array.Empty<string?>())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .ToList();

    private static ServiceDataModelCompletionAssignmentFact Fact(
        string title,
        ServiceDataModelAssignmentStatus status,
        string?[]? businessAreas = null,
        string?[]? directorates = null,
        string?[]? types = null,
        string?[]? owners = null,
        string? phase = null,
        bool overdue = false,
        decimal? fieldPct = null) =>
        new()
        {
            AssignmentId = Guid.NewGuid(),
            ProductId = Guid.NewGuid(),
            ProductTitle = title,
            ModelName = "Service Census",
            Status = status,
            IsOverdue = overdue,
            FieldCompletionPercent = fieldPct,
            BusinessAreaNames = CleanNames(businessAreas),
            DirectorateNames = CleanNames(directorates),
            ServiceTypeNames = CleanNames(types),
            OwnerEmails = CleanNames(owners),
            PhaseName = phase
        };

    [Fact]
    public void ParseGroupBy_DefaultsAndAcceptsKnownValues()
    {
        Assert.Equal(ServiceDataModelCompletionReportGroupBy.BusinessArea,
            ServiceDataModelCompletionReportGrouping.ParseGroupBy(null));
        Assert.Equal(ServiceDataModelCompletionReportGroupBy.Directorate,
            ServiceDataModelCompletionReportGrouping.ParseGroupBy("directorate"));
        Assert.Equal(ServiceDataModelCompletionReportGroupBy.ServiceType,
            ServiceDataModelCompletionReportGrouping.ParseGroupBy("ServiceType"));
        Assert.Equal(ServiceDataModelCompletionReportGroupBy.ServiceType,
            ServiceDataModelCompletionReportGrouping.ParseGroupBy("Type"));
        Assert.Equal(ServiceDataModelCompletionReportGroupBy.BusinessArea,
            ServiceDataModelCompletionReportGrouping.ParseGroupBy("not-a-cut"));
    }

    [Fact]
    public void Summarise_KeepsNotApplicableDistinctFromIncomplete()
    {
        var facts = new[]
        {
            Fact("A", ServiceDataModelAssignmentStatus.NotStarted),
            Fact("B", ServiceDataModelAssignmentStatus.InProgress, fieldPct: 40m),
            Fact("C", ServiceDataModelAssignmentStatus.NotApplicable),
            Fact("D", ServiceDataModelAssignmentStatus.Withdrawn),
            Fact("E", ServiceDataModelAssignmentStatus.Reviewed, fieldPct: 100m),
        };

        var summary = ServiceDataModelCompletionReportGrouping.Summarise(facts, DateTime.UtcNow);

        Assert.Equal(5, summary.AssignedCount);
        Assert.Equal(3, summary.EligibleCount);
        Assert.Equal(1, summary.NotStartedCount);
        Assert.Equal(1, summary.InProgressCount);
        Assert.Equal(1, summary.ReviewedCount);
        Assert.Equal(1, summary.NotApplicableCount);
        Assert.Equal(1, summary.WithdrawnCount);
        Assert.Equal(33.3m, summary.PortfolioCompletionPercent);
    }

    [Fact]
    public void BuildBreakdown_ByBusinessArea_ExpandsMultiValuedProducts()
    {
        var facts = new[]
        {
            Fact("Shared", ServiceDataModelAssignmentStatus.Reviewed,
                businessAreas: new[] { "Area A", "Area B" }, fieldPct: 100m),
            Fact("Solo", ServiceDataModelAssignmentStatus.NotStarted,
                businessAreas: new[] { "Area A" }),
            Fact("Orphan", ServiceDataModelAssignmentStatus.NotApplicable),
        };

        var rows = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts,
            ServiceDataModelCompletionReportGroupBy.BusinessArea,
            DateTime.UtcNow);

        Assert.Equal(3, rows.Count);
        var areaA = Assert.Single(rows, r => r.GroupLabel == "Area A");
        Assert.Equal(2, areaA.AssignedCount);
        Assert.Equal(2, areaA.EligibleCount);
        Assert.Equal(1, areaA.ReviewedCount);
        Assert.Equal(1, areaA.NotStartedCount);

        var areaB = Assert.Single(rows, r => r.GroupLabel == "Area B");
        Assert.Equal(1, areaB.AssignedCount);
        Assert.Equal(1, areaB.ReviewedCount);

        var none = Assert.Single(rows, r => r.GroupLabel == "No business area");
        Assert.Equal(1, none.NotApplicableCount);
        Assert.Equal(0, none.EligibleCount);
    }

    [Fact]
    public void BuildBreakdown_ByPhase_UsesFallbackWhenMissing()
    {
        var facts = new[]
        {
            Fact("Live", ServiceDataModelAssignmentStatus.Submitted, phase: "Live"),
            Fact("Unset", ServiceDataModelAssignmentStatus.NotStarted),
        };

        var rows = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts,
            ServiceDataModelCompletionReportGroupBy.Phase,
            DateTime.UtcNow);

        Assert.Contains(rows, r => r.GroupLabel == "Live" && r.SubmittedCount == 1);
        Assert.Contains(rows, r => r.GroupLabel == "Phase not set" && r.NotStartedCount == 1);
    }

    [Fact]
    public void BuildBreakdown_ByDirectorate_HandlesMissingMultiAndNullNames()
    {
        var facts = new[]
        {
            Fact("Multi", ServiceDataModelAssignmentStatus.Reviewed,
                directorates: new[] { "Dir A", "Dir B", " ", null! }, fieldPct: 100m),
            Fact("Solo", ServiceDataModelAssignmentStatus.NotStarted,
                directorates: new[] { "Dir A" }),
            Fact("Orphan", ServiceDataModelAssignmentStatus.InProgress,
                directorates: Array.Empty<string>()),
            Fact("WhitespaceOnly", ServiceDataModelAssignmentStatus.NotApplicable,
                directorates: new[] { "  ", "\t" }),
        };

        var rows = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts,
            ServiceDataModelCompletionReportGroupBy.Directorate,
            DateTime.UtcNow);

        Assert.Equal(3, rows.Count);
        var dirA = Assert.Single(rows, r => r.GroupLabel == "Dir A");
        Assert.Equal(2, dirA.AssignedCount);
        Assert.Equal(1, dirA.ReviewedCount);

        Assert.Contains(rows, r => r.GroupLabel == "Dir B" && r.AssignedCount == 1);

        var none = Assert.Single(rows, r => r.GroupLabel == "No directorate");
        Assert.Equal(2, none.AssignedCount);
        Assert.Equal(1, none.InProgressCount);
        Assert.Equal(1, none.NotApplicableCount);
    }

    [Fact]
    public void BuildBreakdown_ByOwnerPhaseAndType_UsesFallbacks()
    {
        var facts = new[]
        {
            Fact("A", ServiceDataModelAssignmentStatus.Submitted,
                owners: new[] { "a@example.com" }, phase: "Live", types: new[] { "Public" }),
            Fact("B", ServiceDataModelAssignmentStatus.NotStarted),
        };

        var owners = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts, ServiceDataModelCompletionReportGroupBy.Owner, DateTime.UtcNow);
        Assert.Contains(owners, r => r.GroupLabel == "a@example.com" && r.SubmittedCount == 1);
        Assert.Contains(owners, r => r.GroupLabel == "No owner" && r.NotStartedCount == 1);

        var phases = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts, ServiceDataModelCompletionReportGroupBy.Phase, DateTime.UtcNow);
        Assert.Contains(phases, r => r.GroupLabel == "Live");
        Assert.Contains(phases, r => r.GroupLabel == "Phase not set");

        var types = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts, ServiceDataModelCompletionReportGroupBy.ServiceType, DateTime.UtcNow);
        Assert.Contains(types, r => r.GroupLabel == "Public");
        Assert.Contains(types, r => r.GroupLabel == "No type");
    }

    [Fact]
    public void DimensionKeys_Product_FallsBackWhenTitleMissing()
    {
        var blank = Fact("", ServiceDataModelAssignmentStatus.NotStarted);
        blank = new ServiceDataModelCompletionAssignmentFact
        {
            AssignmentId = blank.AssignmentId,
            ProductId = blank.ProductId,
            ProductTitle = "   ",
            ModelName = blank.ModelName,
            Status = blank.Status
        };

        var keys = ServiceDataModelCompletionReportGrouping.DimensionKeys(
            blank, ServiceDataModelCompletionReportGroupBy.Product);

        Assert.Equal(new[] { "Untitled product" }, keys);
    }

    [Fact]
    public void BuildBreakdown_ProductCut_ReturnsEmptyAggregate()
    {
        var facts = new[]
        {
            Fact("Only", ServiceDataModelAssignmentStatus.InProgress)
        };

        var rows = ServiceDataModelCompletionReportGrouping.BuildBreakdown(
            facts,
            ServiceDataModelCompletionReportGroupBy.Product,
            DateTime.UtcNow);

        Assert.Empty(rows);
    }

    [Fact]
    public void Paginate_ReturnsRequestedPage()
    {
        var facts = Enumerable.Range(1, 30)
            .Select(i => Fact($"P{i:00}", ServiceDataModelAssignmentStatus.NotStarted))
            .ToList();

        var (items, total, pageNum, pageSize, totalPages) =
            ServiceDataModelCompletionReportGrouping.Paginate(facts, page: 2, pageSize: 25);

        Assert.Equal(30, total);
        Assert.Equal(2, pageNum);
        Assert.Equal(25, pageSize);
        Assert.Equal(2, totalPages);
        Assert.Equal(5, items.Count);
        Assert.Equal("P26", items[0].ProductTitle);
    }
}
