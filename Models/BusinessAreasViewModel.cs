namespace Compass.Models;

public class BusinessAreasViewModel
{
    public Commission Commission { get; set; } = new();
    public int TotalProducts { get; set; }
    public int CompletedProducts { get; set; }
    public int InProgressProducts { get; set; }
    public int NotStartedProducts { get; set; }
    public int LateProducts { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<BusinessAreaCompletion> BusinessAreaCompletions { get; set; } = new();
}

public class BusinessAreaCompletion
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int CompletedProducts { get; set; }
    public int InProgressProducts { get; set; }
    public int NotStartedProducts { get; set; }
    public int LateProducts { get; set; }
    public decimal CompletionPercentage { get; set; }
}

public class BusinessAreaDetailsViewModel
{
    public string BusinessAreaName { get; set; } = string.Empty;
    public Commission Commission { get; set; } = new();
    public int TotalProducts { get; set; }
    public int CompletedProducts { get; set; }
    public int InProgressProducts { get; set; }
    public int NotStartedProducts { get; set; }
    public int LateProducts { get; set; }
    public decimal CompletionPercentage { get; set; }
    public List<BusinessAreaProductStatus> ProductStatuses { get; set; } = new();
}

public class BusinessAreaProductStatus
{
    public ProductDto Product { get; set; } = new();
    public CommissionSubmissionStatus Status { get; set; }
    public int CompletedMetrics { get; set; }
    public int TotalMetrics { get; set; }
    public CommissionSubmission? Submission { get; set; }
}
