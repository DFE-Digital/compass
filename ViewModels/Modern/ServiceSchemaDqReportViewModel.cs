namespace Compass.ViewModels.Modern;

public sealed class ServiceSchemaDqReportViewModel
{
    public int TopicCount { get; set; }
    public int ActiveProducts { get; set; }
    public int OverallPercent { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Complete { get; set; }
    public int WithAnyRecording { get; set; }
    public string Stage { get; set; } = "";
    public List<ServiceSchemaDqTopicRow> Topics { get; set; } = new();
    public List<ServiceSchemaDqAreaRow> BusinessAreas { get; set; } = new();
    public List<ServiceSchemaDqMixRow> Phases { get; set; } = new();
    public List<ServiceSchemaDqProductRow> Products { get; set; } = new();
}

public sealed class ServiceSchemaDqTopicRow
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string AreaKey { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string Group { get; set; } = "";
    public int ProductsWithItems { get; set; }
    public int ProductsDeclared { get; set; }
    public int ProductsWithInformation { get; set; }
    public int Empty { get; set; }
    public int Percent { get; set; }
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

public sealed class ServiceSchemaDqMixRow
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
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
}

public sealed class ServiceSchemaDqTopicPage
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string AreaKey { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string View { get; set; } = "missing";
    public int TotalProducts { get; set; }
    public int ProductsWithItems { get; set; }
    public int ProductsDeclared { get; set; }
    public int ProductsWithInformation { get; set; }
    public int Empty { get; set; }
    public int Percent { get; set; }
    public List<ServiceSchemaDqProductRow> Products { get; set; } = new();
}
