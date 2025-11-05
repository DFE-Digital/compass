namespace Compass.Models;

public class DesignAndRunBoardViewModel
{
    public List<DesignAndRunBoardItem> Products { get; set; } = new();
    public double AverageCompletionPercentage { get; set; }
    public List<BusinessAreaSummary> BusinessAreaSummaries { get; set; } = new();
    public int TotalProducts { get; set; }
    public int EnrolledInAccessibilityCount { get; set; }
    public int TotalOpenAccessibilityIssues { get; set; }
    public List<DesignAndRunBoardItem> TopAtRiskProducts { get; set; } = new();
}

public class DesignAndRunBoardItem
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string BusinessArea { get; set; } = string.Empty;
    
    // FIPS Completion data
    public bool HasPhase { get; set; }
    public bool HasBusinessArea { get; set; }
    public int ContactsCount { get; set; }
    public bool HasProductUrl { get; set; }
    public int UserGroupsCount { get; set; }
    public double CompletionPercentage { get; set; }
    
    // Accessibility data
    public bool IsEnrolledInAccessibility { get; set; }
    public DateTime? AccessibilityEnrolledAt { get; set; }
    public int OpenAccessibilityIssuesCount { get; set; }
    public int ResolvedAccessibilityIssuesCount { get; set; }
    public int TotalAccessibilityIssuesCount { get; set; }
    public string AccessibilityComplianceStatus { get; set; } = "Not enrolled";
    
    // Performance metrics (perf-ux-1 and perf-acc-3)
    public string? PerfUx1Value { get; set; } // User experience metric
    public string? PerfAcc3Value { get; set; } // Accessibility metric
    public DateTime? LastMetricSubmission { get; set; }
    public string? LastSubmittedBy { get; set; }
    
    // Risk scoring
    public double RiskScore { get; set; }
    public List<string> RiskFactors { get; set; } = new();
}

public class BusinessAreaSummary
{
    public string BusinessArea { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public double AverageCompletionPercentage { get; set; }
    public int EnrolledInAccessibilityCount { get; set; }
    public int TotalAccessibilityIssues { get; set; }
    public double AverageRiskScore { get; set; }
}

