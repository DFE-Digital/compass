namespace Compass.ViewModels;

public class RaidLinkSummaryViewModel
{
    public int RiskCount { get; set; }
    public int IssueCount { get; set; }
    public int ActionCount { get; set; }
    public int DecisionCount { get; set; }

    public bool HasAny => RiskCount > 0 || IssueCount > 0 || ActionCount > 0 || DecisionCount > 0;
}

