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
    
    // Board-ready structures
    public ExecutiveSummary ExecutiveSummary { get; set; } = new();
    public List<FocusAreaPerformance> FocusAreaPerformances { get; set; } = new();
    public List<KeyMeasure> KeyMeasures { get; set; } = new();
    public List<BoardObjective> Objectives { get; set; } = new();
    public List<PerformanceCriteria> PerformanceCriteria { get; set; } = new();
    public List<BoardRisk> Risks { get; set; } = new();
    public List<AISummary> AISummaries { get; set; } = new();
}

public class ExecutiveSummary
{
    public string OverallStatus { get; set; } = "On Track"; // On Track, At Risk, Behind
    public int ObjectivesOnTrack { get; set; }
    public int ObjectivesAtRisk { get; set; }
    public int ObjectivesAchieved { get; set; }
    public int TotalObjectives { get; set; }
    public double ObjectivesOnTrackPercentage { get; set; }
    public double ObjectivesAchievedPercentage { get; set; }
    public int TotalPlans { get; set; }
}

public class FocusAreaPerformance
{
    public string FocusArea { get; set; } = string.Empty;
    public int ObjectivesOnTrack { get; set; }
    public int TotalObjectives { get; set; }
    public double OnTrackPercentage { get; set; }
    public double AverageCompletion { get; set; }
    public double AverageRiskScore { get; set; }
}

public class KeyMeasure
{
    public string Title { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string TargetValue { get; set; } = string.Empty;
    public string TargetDate { get; set; } = string.Empty;
    public string Status { get; set; } = "On Track"; // On Track, At Risk, Behind
    public string Trend { get; set; } = "neutral"; // up, down, neutral
}

public class BoardObjective
{
    public string Title { get; set; } = string.Empty;
    public string FocusArea { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string OwnerInitials { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = "On Track"; // On Track, At Risk, Behind, Achieved
    public string Description { get; set; } = string.Empty;
}

public class PerformanceCriteria
{
    public string Category { get; set; } = string.Empty;
    public string Deliverable { get; set; } = string.Empty;
    public string Status { get; set; } = "On Track";
    public int OnTrackCount { get; set; }
    public int BehindCount { get; set; }
    public int AchievedCount { get; set; }
    public int TotalCount { get; set; }
    public string Owner { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class BoardRisk
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Likelihood { get; set; } // 0-10
    public double Impact { get; set; } // 0-10
    public double RiskScore { get; set; } // Likelihood * Impact
    public string RiskLevel { get; set; } = "Moderate"; // Low, Moderate, High, Critical
    public string Mitigation { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime? ProximityDate { get; set; }
}

public class AISummary
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string KeyChanges { get; set; } = string.Empty;
    public string SuggestedActions { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
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

