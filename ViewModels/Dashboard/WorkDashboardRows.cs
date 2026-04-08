using Compass.Models;

namespace Compass.ViewModels.Dashboard;

/// <summary>Monthly reporting row on the modern work dashboard (backed by <see cref="Models.Project"/>).</summary>
public class HomeMonthlyUpdateRow
{
    public int WorkItemId { get; set; }
    public string WorkTitle { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public string PortfolioName { get; set; } = "—";
    public string? RagStatusName { get; set; }
}

/// <summary>RAG overview tab on the modern work dashboard.</summary>
public class WorkDashboardRagOverviewRow
{
    public int WorkItemId { get; set; }
    public string Title { get; set; } = "";
    public string PortfolioName { get; set; } = "—";
    public string PhaseDisplayName { get; set; } = "—";
    public string PhaseCssSuffix { get; set; } = "";
    public ChromeRagSnapshot? LatestRag { get; set; }
    public string Commentary { get; set; } = "—";
}
