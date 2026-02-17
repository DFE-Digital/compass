using Compass.Models;

namespace Compass.Models;

public class ServiceOwnerSroReportViewModel
{
    public User? SelectedUser { get; set; }
    public User? CurrentUser { get; set; }
    public bool IsAdmin { get; set; }
    
    // Products
    public List<ProductSummary> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public int ProductsAsServiceOwner { get; set; }
    public int ProductsAsSro { get; set; }
    
    // Projects
    public List<ProjectSummary> Projects { get; set; } = new();
    public int TotalProjects { get; set; }
    public int ProjectsAsServiceOwner { get; set; }
    public int ProjectsAsSro { get; set; }
    
    // Performance Reporting Summary
    public PerformanceReportingSummary PerformanceReporting { get; set; } = new();
    
    // Project Reporting Summary
    public ProjectReportingSummary ProjectReporting { get; set; } = new();
    
    // Service Assessment Summary
    public ServiceAssessmentSummary ServiceAssessment { get; set; } = new();
    
    // All users for switcher
    public List<User> AllUsers { get; set; } = new();
}

public class ProductSummary
{
    public string DocumentId { get; set; } = string.Empty;
    public string FipsId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Phase { get; set; }
    public string State { get; set; } = string.Empty;
    public string? ProductUrl { get; set; }
    public List<string> Roles { get; set; } = new(); // "Service Owner", "SRO", etc.
    public bool HasPerformanceReporting { get; set; }
    public DateTime? LastPerformanceSubmission { get; set; }
    public bool IsPerformanceReportingOverdue { get; set; }
}

public class ProjectSummary
{
    public int Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? RagStatus { get; set; }
    public string? Phase { get; set; }
    public string? Status { get; set; }
    public List<string> Roles { get; set; } = new(); // "Service Owner", "SRO", etc.
    public int MilestoneCount { get; set; }
    public int OverdueMilestoneCount { get; set; }
    public int IssueCount { get; set; }
    public int HighPriorityIssueCount { get; set; }
    public int RiskCount { get; set; }
    public int HighRiskCount { get; set; }
    public int ActionCount { get; set; }
    public int OpenActionCount { get; set; }
}

public class PerformanceReportingSummary
{
    public int TotalProductsRequiringReporting { get; set; }
    public int ProductsWithSubmissions { get; set; }
    public int ProductsOverdue { get; set; }
    public int ProductsUpToDate { get; set; }
    public double CompletionPercentage { get; set; }
    public DateTime? LastSubmissionDate { get; set; }
}

public class ProjectReportingSummary
{
    public int TotalProjects { get; set; }
    public int TotalMilestones { get; set; }
    public int OverdueMilestones { get; set; }
    public int TotalIssues { get; set; }
    public int OpenIssues { get; set; }
    public int HighPriorityIssues { get; set; }
    public int TotalRisks { get; set; }
    public int HighRisks { get; set; }
    public int TotalActions { get; set; }
    public int OpenActions { get; set; }
    public Dictionary<string, int> ProjectsByRagStatus { get; set; } = new();
}

public class ServiceAssessmentSummary
{
    public int TotalAssessments { get; set; }
    public int AssessmentsPassed { get; set; }
    public int AssessmentsFailed { get; set; }
    public int AssessmentsInProgress { get; set; }
    public int TotalActions { get; set; }
    public int OpenActions { get; set; }
    public int OverdueActions { get; set; }
    public Dictionary<string, int> AssessmentsByType { get; set; } = new();
    public Dictionary<string, int> AssessmentsByPhase { get; set; } = new();
}
