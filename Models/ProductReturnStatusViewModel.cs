namespace Compass.Models;

public class ProductReturnStatusViewModel
{
    public ProductDto Product { get; set; } = new();
    public int CurrentPeriodYear { get; set; }
    public int CurrentPeriodMonth { get; set; }
    public ReturnStatus? Status { get; set; }
    public int? CompletedMetrics { get; set; }
    public int? TotalMetrics { get; set; }
    public string? UserRole { get; set; }
}

