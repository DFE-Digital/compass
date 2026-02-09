namespace Compass.Models;

public class CommissionReportingViewModel
{
    public Commission Commission { get; set; } = new();
    public int TotalProducts { get; set; }
    public int CompletedProducts { get; set; }
    public int InProgressProducts { get; set; }
    public int NotStartedProducts { get; set; }
    public int LateProducts { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<CommissionBusinessAreaCompletion> BusinessAreaCompletions { get; set; } = new();
}

public class CommissionBusinessAreaCompletion
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int CompletedProducts { get; set; }
    public int InProgressProducts { get; set; }
    public int NotStartedProducts { get; set; }
    public int LateProducts { get; set; }
    public decimal CompletionPercentage { get; set; }
}
