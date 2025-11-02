using Compass.Models;

namespace Compass.Models;

public class DashboardViewModel : BaseViewModel
{
    public User? CurrentUser { get; set; }
    public List<ProductDto> MyProducts { get; set; } = new List<ProductDto>();
    public List<Project> MyProjects { get; set; } = new List<Project>();
    public List<Issue> MyIssues { get; set; } = new List<Issue>();
    public List<Milestone> MyMilestones { get; set; } = new List<Milestone>();
    
    // Summary counts
    public int TotalProducts => MyProducts.Count;
    public int TotalProjects => MyProjects.Count;
    public int TotalIssues => MyIssues.Count;
    public int TotalMilestones => MyMilestones.Count;
    
    // Project RAG counts
    public int GreenProjects => MyProjects.Count(p => p.RagStatus == "Green");
    public int AmberGreenProjects => MyProjects.Count(p => p.RagStatus == "Amber-Green");
    public int AmberProjects => MyProjects.Count(p => p.RagStatus == "Amber");
    public int AmberRedProjects => MyProjects.Count(p => p.RagStatus == "Amber-Red");
    public int RedProjects => MyProjects.Count(p => p.RagStatus == "Red");
    public int ProjectsWithoutRAG => MyProjects.Count(p => string.IsNullOrEmpty(p.RagStatus));
    
    public int UrgentMilestones => MyMilestones.Count(m => 
        m.Status != "complete" && 
        m.Status != "cancelled" && 
        m.DueDate <= DateTime.UtcNow.AddDays(7));
    
    public int OpenIssues => MyIssues.Count(i => 
        i.Status != "resolved" && 
        i.Status != "closed");
    
    // Upcoming milestones (next 30 days)
    public List<Milestone> UpcomingMilestones => MyMilestones
        .Where(m => m.Status != "complete" && 
                    m.Status != "cancelled" && 
                    m.DueDate >= DateTime.UtcNow && 
                    m.DueDate <= DateTime.UtcNow.AddDays(30))
        .OrderBy(m => m.DueDate)
        .Take(10)
        .ToList();
    
    // Highest severity issues (critical and high)
    public List<Issue> CriticalIssues => MyIssues
        .Where(i => (i.Severity == "critical" || i.Severity == "high") && 
                    i.Status != "resolved" && 
                    i.Status != "closed")
        .OrderByDescending(i => i.Severity == "critical" ? 1 : 0)
        .ThenBy(i => i.TargetResolutionDate ?? DateTime.MaxValue)
        .Take(10)
        .ToList();
    
    // Reports (placeholder for now - will need reporting integration)
    public List<ReportStatus> Reports { get; set; } = new List<ReportStatus>();
}

// Placeholder for report status
public class ReportStatus
{
    public string Type { get; set; } = string.Empty; // "Delivery" or "Operational"
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // "Due", "Late", "Submitted"
    public DateTime? DueDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
}

