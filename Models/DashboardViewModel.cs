using Compass.Models;

namespace Compass.Models;

public class DashboardViewModel : BaseViewModel
{
    public User? CurrentUser { get; set; }
    public List<ProductDto> MyProducts { get; set; } = new List<ProductDto>();
    public List<Issue> MyIssues { get; set; } = new List<Issue>();
    public List<Risk> MyRisks { get; set; } = new List<Risk>();
    public List<Action> MyActions { get; set; } = new List<Action>();
    public List<Milestone> MyMilestones { get; set; } = new List<Milestone>();
    
    // Summary counts
    public int TotalProducts => MyProducts.Count;
    public int TotalIssues => MyIssues.Count;
    public int TotalRisks => MyRisks.Count;
    public int TotalActions => MyActions.Count;
    public int TotalMilestones => MyMilestones.Count;
    
    // Urgent items (due in next 7 days or overdue)
    public int UrgentActions => MyActions.Count(a => 
        a.Status != "done" && 
        a.Status != "cancelled" && 
        a.DueDate.HasValue && 
        a.DueDate.Value <= DateTime.UtcNow.AddDays(7));
    
    public int UrgentMilestones => MyMilestones.Count(m => 
        m.Status != "complete" && 
        m.Status != "cancelled" && 
        m.DueDate <= DateTime.UtcNow.AddDays(7));
    
    public int OpenIssues => MyIssues.Count(i => 
        i.Status != "resolved" && 
        i.Status != "closed");
    
    public int ActiveRisks => MyRisks.Count(r => 
        r.Status != "closed");
}

