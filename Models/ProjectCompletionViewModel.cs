namespace Compass.Models;

public class ProjectCompletionViewModel
{
    public List<ProjectCompletionItem> Projects { get; set; } = new();
    public double AverageCompletionPercentage { get; set; }
    public List<BusinessAreaCompletion> BusinessAreaCompletions { get; set; } = new();
    public int ZeroCompletionCount { get; set; }
    public int FullCompletionCount { get; set; }
    
    // Field completion counts
    public int CompletedSroCount { get; set; }
    public int CompletedPmoContactCount { get; set; }
    public int CompletedDirectorateCount { get; set; }
    public int CompletedBudgetOwnerCount { get; set; }
    public int CompletedRagStatusCount { get; set; }
    public int CompletedPriorityCount { get; set; }
    public int CompletedBusinessAreaCount { get; set; }
    public int CompletedPrimaryContactCount { get; set; }
    public int CompletedActivityTypeCount { get; set; }
    public int CompletedSpendControlCount { get; set; }
}

public class ProjectCompletionItem
{
    public int ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string? Phase { get; set; }
    public string? BusinessArea { get; set; }
    public string? RagStatus { get; set; }
    public string? PriorityName { get; set; }
    public int? PriorityId { get; set; }
    public string? PrimaryContactName { get; set; }
    public int? PrimaryContactUserId { get; set; }
    public string? ActivityTypeName { get; set; }
    public int? ActivityTypeLookupId { get; set; }
    public bool? IsSubjectToSpendControl { get; set; }
    
    // Collections
    public List<string> SeniorResponsibleOfficerNames { get; set; } = new();
    public List<int> SeniorResponsibleOfficerUserIds { get; set; } = new();
    public List<string> PmoContactNames { get; set; } = new();
    public List<int> PmoContactUserIds { get; set; } = new();
    public List<string> DirectorateNames { get; set; } = new();
    public List<int> DirectorateLookupIds { get; set; } = new();
    public List<string> BudgetOwnerNames { get; set; } = new();
    public List<int> BudgetOwnerBusinessAreaLookupIds { get; set; } = new();
    
    // Completion criteria
    public bool HasSro { get; set; }
    public bool HasPmoContact { get; set; }
    public bool HasDirectorate { get; set; }
    public bool HasBudgetOwner { get; set; }
    public bool HasRagStatus { get; set; }
    public bool HasPriority { get; set; }
    public bool HasBusinessArea { get; set; }
    public bool HasPrimaryContact { get; set; }
    public bool HasActivityType { get; set; }
    public bool HasSpendControl { get; set; }
    
    // Calculated completion percentage (0-100)
    public double CompletionPercentage { get; set; }
}

