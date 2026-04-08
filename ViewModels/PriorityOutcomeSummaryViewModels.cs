namespace Compass.ViewModels;

public class DonutChartSlice
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
    public string Color { get; set; } = "#6c757d";
}

public class PriorityOutcomeSummarySectionViewModel
{
    public int ObjectiveId { get; set; }
    public string Title { get; set; } = "";
    public string? Theme { get; set; }
    public int ProjectCount { get; set; }
    public List<DonutChartSlice> RagSlices { get; set; } = new();
    public List<DonutChartSlice> DeliveryPrioritySlices { get; set; } = new();
    public List<DonutChartSlice> MilestoneSlices { get; set; } = new();
}

public class PriorityOutcomeSummaryPageViewModel
{
    public List<PriorityOutcomeSummarySectionViewModel> Sections { get; set; } = new();
}
