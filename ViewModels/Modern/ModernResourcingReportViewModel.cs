using Compass.Models;

namespace Compass.ViewModels.Modern;

/// <summary>Monthly resourcing report totals (FTE + MSC) with configurable bands.</summary>
public sealed class ModernResourcingReportViewModel
{
    public int ReportYear { get; set; }
    public int ReportMonth { get; set; }
    public string MonthName { get; set; } = "";

    public int MinReportYear { get; set; } = 2026;
    public int MaxReportYear { get; set; } = 2026;

    public int? FilterBusinessAreaId { get; set; }
    public int? FilterDirectorateId { get; set; }

    /// <summary>all | mission | outcomes | priority</summary>
    public string Dimension { get; set; } = "all";
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }

    public List<PrioritiesReportGroupOption> GroupOptions { get; set; } = new();

    public List<BusinessAreaLookup> BusinessAreas { get; set; } = new();
    public List<Division> Directorates { get; set; } = new();

    public bool HasPreviousMonthNav { get; set; }
    public bool HasNextMonthNav { get; set; }
    public int? PreviousNavYear { get; set; }
    public int? PreviousNavMonth { get; set; }
    public int? NextNavYear { get; set; }
    public int? NextNavMonth { get; set; }

    public decimal TotalPermFte { get; set; }
    public decimal TotalMspFte { get; set; }
    public decimal TotalResourcingFte { get; set; }
    public int SubmittedWorkItemCount { get; set; }

    public List<ResourcingBandViewModel> Bands { get; set; } = new();
    public List<ResourcingAggregateRow> DirectorateRows { get; set; } = new();
    public List<ResourcingAggregateRow> BusinessAreaRows { get; set; } = new();
    public List<ResourcingWorkItemRow> WorkItemRows { get; set; } = new();
    public List<ResourcingTrendMonthPoint> TrendPoints { get; set; } = new();
    public List<ResourcingGroupTrendSeries> DirectorateTrendSeries { get; set; } = new();
    public List<ResourcingGroupTrendSeries> BusinessAreaTrendSeries { get; set; } = new();
}

public sealed class ResourcingBandViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal MinFte { get; set; }
    public decimal? MaxFte { get; set; }
    public string? CssClass { get; set; }
    public int SortOrder { get; set; }
}

public sealed class ResourcingAggregateRow
{
    public string Name { get; set; } = "";
    public int? GroupId { get; set; }
    public int WorkItemCount { get; set; }
    public decimal PermFteTotal { get; set; }
    public decimal MspFteTotal { get; set; }
    public decimal ResourcingFteTotal { get; set; }
    public string BandName { get; set; } = "—";
    public string? BandCssClass { get; set; }
    public List<int> ProjectIds { get; set; } = new();
}

public sealed class ResourcingWorkItemRow
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = "";
    public string BusinessArea { get; set; } = "Not set";
    public string Directorates { get; set; } = "Not set";
    public string Rag { get; set; } = "Not Set";
    public string? Priority { get; set; }
    public decimal PermFte { get; set; }
    public decimal MspFte { get; set; }
    public decimal ResourcingFte { get; set; }
    public string BandName { get; set; } = "—";
    public string? BandCssClass { get; set; }
}

public sealed class ResourcingTrendMonthPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = "";
    public decimal PermFteTotal { get; set; }
    public decimal MspFteTotal { get; set; }
    public decimal ResourcingFteTotal { get; set; }
    public int SubmittedWorkItemCount { get; set; }
    public List<int> WorkItemIds { get; set; } = new();
}

public sealed class ResourcingGroupTrendSeries
{
    public string GroupName { get; set; } = "";
    public List<ResourcingTrendMonthPoint> Points { get; set; } = new();
}
