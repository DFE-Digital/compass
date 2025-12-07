namespace Compass.ViewModels.DemandManagement;

public class CsvImportViewModel
{
    public List<string> CsvColumns { get; set; } = new();
    public Dictionary<string, string> FieldMappings { get; set; } = new(); // CSV column -> DB field
    public List<Dictionary<string, string>> SampleRows { get; set; } = new(); // First few rows for preview
    public string? TempFilePath { get; set; }
}

public class CsvImportResultViewModel
{
    public int TotalRows { get; set; }
    public int SuccessfulImports { get; set; }
    public int FailedImports { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public List<int> CreatedRequestIds { get; set; } = new();
}

public class ImportError
{
    public int RowNumber { get; set; }
    public string? DemandId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

