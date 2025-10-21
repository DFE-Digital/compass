namespace Compass.Models;

public class ProductRaidReport
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public int TotalRisks { get; set; }
    public int OpenRisks { get; set; }
    public int HighRisks { get; set; }
    public int MediumRisks { get; set; }
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int CriticalIssues { get; set; }
    public int BlockedIssues { get; set; }
    public int TotalActions { get; set; }
    public int OverdueActions { get; set; }
    public int TotalMilestones { get; set; }
    public int OverdueMilestones { get; set; }
    public int HealthScore { get; set; }
}

