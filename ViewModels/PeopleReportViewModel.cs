namespace Compass.ViewModels;

public class PeopleReportViewModel
{
    public List<PersonAllocationSummary> People { get; set; } = new();
    public string? FilterName { get; set; }
    public string? FilterEmail { get; set; }
    public string? FilterRole { get; set; }
    public string? FilterEmploymentType { get; set; }
    public string? FilterFundingArrangement { get; set; }
    public string? FilterProject { get; set; }
}

public class PersonAllocationSummary
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal TotalAllocationPercent { get; set; }
    public List<PersonProjectAllocation> ProjectAllocations { get; set; } = new();
}

public class PersonProjectAllocation
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleType { get; set; } = string.Empty; // "Team Member", "SRO", "Service Owner", "Primary Contact"
    public string? TimeAllocation { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string FundingArrangement { get; set; } = string.Empty;
    public decimal AllocationPercent { get; set; }
}

