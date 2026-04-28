using Compass.Models;

namespace Compass.ViewModels.Dashboard;

/// <summary>Monthly reporting row on the modern work dashboard (backed by <see cref="Models.Project"/>).</summary>
public class HomeMonthlyUpdateRow
{
    public int WorkItemId { get; set; }
    public string WorkTitle { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    /// <summary>Short label for the table, e.g. "Mar Update".</summary>
    public string PeriodTitle { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    /// <summary>Submitted, Draft, Not due (before reporting window), or Not started.</summary>
    public string StatusLabel { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    /// <summary>view (submitted), complete (return open/overdue), not-due (before window).</summary>
    public string RowKind { get; set; } = "not-due";
    public string PortfolioName { get; set; } = "—";
    public string? RagStatusName { get; set; }
    public string BusinessArea { get; set; } = "—";
    public string? PriorityName { get; set; }
    public string? PhaseName { get; set; }
    public string PrimaryContact { get; set; } = "—";
    /// <summary>Comma-separated active tag names (same source as work register).</summary>
    public string? TagNamesSummary { get; set; }
    public string WorkItemStatus { get; set; } = string.Empty;
    public ChromeRagSnapshot? LatestRag { get; set; }
}
