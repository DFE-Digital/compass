namespace Compass.Models;

/// <summary>
/// JSON template stored in <see cref="CustomReport.DefinitionJson"/> for templated delivery reports.
/// Defines scope (which work items and service register entries are included),
/// the reporting period, and the ordered list of report sections to render.
/// </summary>
public sealed class CustomReportTemplateDefinition
{
    public string ReportingPeriod { get; set; } = "calendar-month";

    public CustomReportScope Scope { get; set; } = new();

    public List<CustomReportTemplateSection> Sections { get; set; } = new();
}

public sealed class CustomReportScope
{
    public List<int> WorkItemIds { get; set; } = new();
    public List<int> ServiceRegisterIds { get; set; } = new();
}

public sealed class CustomReportTemplateSection
{
    public string SectionType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
}

/// <summary>Available section types for templated custom reports.</summary>
public static class CustomReportSectionTypes
{
    public const string CountWorkItems = "count-work-items";
    public const string CountServiceRegister = "count-service-register";
    public const string CountRisks = "count-risks";
    public const string CountIssues = "count-issues";
    public const string CountAccessibilityIssues = "count-accessibility-issues";

    public const string TableRagPriority = "table-rag-priority";
    public const string TableMilestones = "table-milestones";
    public const string TablePathToGreen = "table-path-to-green";
    public const string TableMonthlyUpdates = "table-monthly-updates";
    public const string TableRag = "table-rag";
    public const string TableResourcing = "table-resourcing";

    public const string Intelligence = "intelligence";

    public static readonly (string Key, string Label, string Group)[] All =
    [
        (CountWorkItems, "Work items count", "Counts"),
        (CountServiceRegister, "Service register count", "Counts"),
        (CountRisks, "Risks count", "Counts"),
        (CountIssues, "Issues count", "Counts"),
        (CountAccessibilityIssues, "Accessibility issues count", "Counts"),

        (TableRagPriority, "RAG and Priority", "Tables and trend charts"),
        (TableMilestones, "Milestones", "Tables and trend charts"),
        (TablePathToGreen, "Path to green", "Tables and trend charts"),
        (TableMonthlyUpdates, "Monthly updates", "Tables and trend charts"),
        (TableRag, "RAG summary", "Tables and trend charts"),
        (TableResourcing, "Resourcing", "Tables and trend charts"),

        (Intelligence, "Generated intelligence", "Intelligence"),
    ];
}
