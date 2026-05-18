namespace Compass.ViewModels;

/// <summary>Automated narrative and highlights for the monthly report Intelligence panel.</summary>
public class MonthlyReportIntelligence
{
    public string ScopeLabel { get; set; } = "All business areas";
    public string MonthDisplay { get; set; } = "";
    public string PrevMonthDisplay { get; set; } = "";

    /// <summary>Opening summary paragraphs (plain language, not prescriptive).</summary>
    public List<string> SummaryParagraphs { get; set; } = new();

    public List<MonthlyReportIntelligenceSection> Sections { get; set; } = new();

    /// <summary>Count of notable signals surfaced (for panel badge).</summary>
    public int SignalCount { get; set; }

    public bool HasContent => SummaryParagraphs.Count > 0 || Sections.Any(s => s.HasListContent);
}

public class MonthlyReportIntelligenceSection
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Intro { get; set; }
    public List<MonthlyReportIntelligenceItem> Items { get; set; } = new();
    public List<MonthlyReportIntelligenceGroup> Groups { get; set; } = new();
    public int OverflowCount { get; set; }

    public bool HasListContent => Items.Count > 0 || Groups.Any(g => g.Items.Count > 0);
}

public class MonthlyReportIntelligenceGroup
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Hint { get; set; }
    /// <summary>positive, warning, neutral</summary>
    public string Tone { get; set; } = "neutral";
    public List<MonthlyReportIntelligenceItem> Items { get; set; } = new();
    public int OverflowCount { get; set; }
}

public class MonthlyReportIntelligenceItem
{
    public string Text { get; set; } = "";
    public string? Subtext { get; set; }
    /// <summary>positive, warning, neutral</summary>
    public string Tone { get; set; } = "neutral";
    public int? ProjectId { get; set; }
    public int? RiskId { get; set; }
    public int? IssueId { get; set; }
}
