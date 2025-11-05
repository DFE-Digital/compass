namespace Compass.Models;

public class FipsCompletionViewModel
{
    public List<ProductCompletionItem> Products { get; set; } = new();
    public double AverageCompletionPercentage { get; set; }
    public List<BusinessAreaCompletion> BusinessAreaCompletions { get; set; } = new();
    public int ZeroCompletionCount { get; set; }
    public int FullCompletionCount { get; set; }
    public int CompletedPhaseCount { get; set; }
    public int CompletedBusinessAreaCount { get; set; }
    public int CompletedUrlCount { get; set; }
}

public class ProductCompletionItem
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string BusinessArea { get; set; } = string.Empty;
    public string State { get; set; } = "New";
    
    // Completion criteria
    public bool HasPhase { get; set; }
    public bool HasBusinessArea { get; set; }
    public int ContactsCount { get; set; }
    public bool HasProductUrl { get; set; }
    public int UserGroupsCount { get; set; }
    
    // Calculated completion percentage (0-100)
    public double CompletionPercentage { get; set; }
}

public class BusinessAreaCompletion
{
    public string BusinessArea { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public double AverageCompletionPercentage { get; set; }
}

