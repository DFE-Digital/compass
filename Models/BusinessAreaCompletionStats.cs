namespace Compass.Models;

public class BusinessAreaCompletionStats
{
    public string BusinessArea { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int ProductsRequiringReporting { get; set; }
    public int ProductsSubmitted { get; set; }
    public int ProductsDue { get; set; }
    public int ProductsLate { get; set; }
    public int ProductsNotStarted { get; set; }
    public double CompletionPercentage { get; set; }
}
