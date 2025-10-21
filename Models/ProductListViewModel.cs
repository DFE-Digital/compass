namespace Compass.Models;

/// <summary>
/// View model for displaying a product in the Products list
/// </summary>
public class ProductListViewModel
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string? BusinessArea { get; set; }
    public string? Phase { get; set; }
    
    // RAID counts
    public int TotalRisks { get; set; }
    public int OpenRisks { get; set; }
    public int HighRisks { get; set; }
    
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int BlockedIssues { get; set; }
    
    public int TotalActions { get; set; }
    public int OverdueActions { get; set; }
    
    public int TotalMilestones { get; set; }
    public int OverdueMilestones { get; set; }
    
    // Health indicator
    public int HealthScore { get; set; }
    
    // User assignment
    public bool IsUserAssigned { get; set; }
}

/// <summary>
/// View model for the Products Index page
/// </summary>
public class ProductsIndexViewModel
{
    public List<ProductListViewModel> MyProducts { get; set; } = new();
    public List<ProductListViewModel> AllProducts { get; set; } = new();
    public int TotalProducts { get; set; }
    public int MyProductsCount { get; set; }
    
    // Summary statistics
    public int TotalRisks { get; set; }
    public int TotalHighRisks { get; set; }
    public int TotalIssues { get; set; }
    public int TotalCriticalIssues { get; set; }
}

