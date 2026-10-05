namespace Compass.ViewModels.Modern;

public sealed class ServiceLineReportViewModel
{
    public bool ShowSchema { get; set; }
    public int ServiceLineCount { get; set; }
    public int ActiveProducts { get; set; }
    public int InALine { get; set; }
    public int NotInALine { get; set; }
    public int InMultipleLines { get; set; }
    public int LinesSpanningBusinessAreas { get; set; }
    public int EmptyLines { get; set; }
    public int? SchemaInALine { get; set; }
    public int? SchemaNotInALine { get; set; }
    public List<ServiceLineReportLineRow> Lines { get; set; } = new();
    public List<ServiceLineReportProductRow> InLineProducts { get; set; } = new();
    public List<ServiceLineReportProductRow> UnassignedProducts { get; set; } = new();
    public List<ServiceLineReportProductRow> SharedProducts { get; set; } = new();
    public List<ServiceLineReportProductRow> MultiAreaProducts { get; set; } = new();
}

public sealed class ServiceLineReportLineRow
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int ProductCount { get; set; }
    public int BusinessAreaCount { get; set; }
    public string ProductAreas { get; set; } = "";
    public int ProductsInMultipleAreas { get; set; }
    public int SharedProductCount { get; set; }
    public int DirectorateCount { get; set; }
    public int WorkItemCount { get; set; }
    public string LinkedAreas { get; set; } = "";
    public bool AreaMismatch { get; set; }
    public bool ProductsSpanMultipleAreas { get; set; }
    public int? SchemaPercent { get; set; }
}

public sealed class ServiceLineReportProductRow
{
    public Guid Id { get; set; }
    public int UniqueId { get; set; }
    public string Title { get; set; } = "";
    public string BusinessAreas { get; set; } = "";
    public int BusinessAreaCount { get; set; }
    public int LineCount { get; set; }
    public string Lines { get; set; } = "";
    public int? SchemaPercent { get; set; }
}
