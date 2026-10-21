namespace Compass.ViewModels.Modern;

/// <summary>Service census reporting dashboard (monthly-report style).</summary>
public sealed class ServiceSchemaDqReportViewModel
{
    public int TopicCount { get; set; }
    public int ActiveProducts { get; set; }
    public int TotalEntries { get; set; }
    public int OverallPercent { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Complete { get; set; }
    public string? BusinessAreaFilter { get; set; }
    public List<string> BusinessAreaOptions { get; set; } = new();
    public List<ServiceSchemaDqAreaRow> BusinessAreas { get; set; } = new();
    public List<ServiceSchemaDqSectionRow> Sections { get; set; } = new();
    public List<ServiceSchemaDqInsightPanel> Insights { get; set; } = new();
    public List<ServiceSchemaDqTopicRow> WeakestQuestions { get; set; } = new();
    public List<ServiceSchemaDqProductRow> ProductsBehind { get; set; } = new();
    /// <summary>Per-product census coverage (used by manage directory and service-line report).</summary>
    public List<ServiceSchemaDqProductRow> Products { get; set; } = new();
}

public sealed class ServiceSchemaDqSectionRow
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public int TopicCount { get; set; }
    public int AveragePercent { get; set; }
    public int EmptyQuestions { get; set; }
    public int EntryCount { get; set; }
    public List<ServiceSchemaDqTopicRow> Topics { get; set; } = new();
}

public sealed class ServiceSchemaDqTopicRow
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string AreaKey { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string Mode { get; set; } = "text";
    public bool HasSelectableValues { get; set; }
    public int ProductsWithItems { get; set; }
    public int ProductsDeclared { get; set; }
    public int ProductsWithInformation { get; set; }
    public int Empty { get; set; }
    public int Percent { get; set; }
    public int DistinctValues { get; set; }
    public int EntryCount { get; set; }
}

public sealed class ServiceSchemaDqInsightPanel
{
    public string TopicKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string ModeLabel { get; set; } = "";
    public string Blurb { get; set; } = "";
    public int ProductsWithAnswers { get; set; }
    public int DistinctValues { get; set; }
    public int EntryCount { get; set; }
    public List<ServiceSchemaDqValueRow> Values { get; set; } = new();
}

public sealed class ServiceSchemaDqValueRow
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int ProductCount { get; set; }
    public int EntryCount { get; set; }
}

public sealed class ServiceSchemaDqAreaRow
{
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public int AveragePercent { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Complete { get; set; }
}

public sealed class ServiceSchemaDqProductRow
{
    public Guid Id { get; set; }
    public int UniqueId { get; set; }
    public string Title { get; set; } = "";
    public string BusinessAreas { get; set; } = "";
    public string Phase { get; set; } = "";
    public int TopicsRecorded { get; set; }
    public int TopicCount { get; set; }
    public int Percent { get; set; }
    public string Answer { get; set; } = "";
    public string? AnswerDetail { get; set; }
    public bool DeclaredNothing { get; set; }
}

public sealed class ServiceSchemaDqTopicPage
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string AreaKey { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string Mode { get; set; } = "text";
    public string ModeLabel { get; set; } = "";
    public string Help { get; set; } = "";
    public bool HasSelectableValues { get; set; }
    public string View { get; set; } = "missing";
    public string? SelectedValue { get; set; }
    public string? SelectedValueLabel { get; set; }
    public string? BusinessAreaFilter { get; set; }
    public List<string> BusinessAreaOptions { get; set; } = new();
    public int TotalProducts { get; set; }
    public int ProductsWithItems { get; set; }
    public int ProductsDeclared { get; set; }
    public int ProductsWithInformation { get; set; }
    public int Empty { get; set; }
    public int Percent { get; set; }
    public int EntryCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int ProductMatchCount { get; set; }
    public List<ServiceSchemaDqValueRow> Values { get; set; } = new();
    public List<ServiceSchemaDqProductRow> Products { get; set; } = new();
}
