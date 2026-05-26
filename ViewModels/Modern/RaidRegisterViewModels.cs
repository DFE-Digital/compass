using Compass.Models;

namespace Compass.ViewModels.Modern;

public class RaidRegisterOnboardingViewModel
{
    public int? RegisterId { get; set; }
    public int Step { get; set; } = 1;
    public int TotalSteps { get; } = 6;

    // Step 1: Name & description
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Step 2: Directorate & portfolio
    public int? DirectorateLookupId { get; set; }
    public int? BusinessAreaLookupId { get; set; }
    public List<SelectOption> DirectorateOptions { get; set; } = new();
    public List<SelectOption> BusinessAreaOptions { get; set; } = new();

    // Step 3: Work items
    public List<int> SelectedWorkItemIds { get; set; } = new();
    public List<SelectOption> WorkItemOptions { get; set; } = new();

    // Step 4: Service register entries
    public List<int> SelectedServiceIds { get; set; } = new();
    public List<SelectOption> ServiceOptions { get; set; } = new();

    // Step 5: Users
    public List<RaidRegisterUserRow> RegisterUsers { get; set; } = new();

    // Step 6: Review (read-only summary)
    public string? DirectorateName { get; set; }
    public string? BusinessAreaName { get; set; }
    public List<string> SelectedWorkItemNames { get; set; } = new();
    public List<string> SelectedServiceNames { get; set; } = new();
}

public record SelectOption(int Id, string Name);

public class RaidRegisterUserRow
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public RaidRegisterRole Role { get; set; } = RaidRegisterRole.Manager;
}

/// <summary>Dashboard card for one register on the top-level register list.</summary>
public class RaidRegisterCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DirectorateName { get; set; }
    public string? BusinessAreaName { get; set; }
    public string? OwnerName { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int OpenRiskCount { get; set; }
    public int OpenIssueCount { get; set; }
    public int OpenAssumptionCount { get; set; }
    public int OpenDependencyCount { get; set; }
    public int OpenNearMissCount { get; set; }
    public int TotalItemCount { get; set; }

    public List<string> WorkItemNames { get; set; } = new();
    public List<string> ServiceNames { get; set; } = new();
}

/// <summary>Top-level "My RAID Registers" dashboard.</summary>
public class RaidRegisterDashboardViewModel
{
    public List<RaidRegisterCardViewModel> Registers { get; set; } = new();
}

/// <summary>A single register's detail/sub-dashboard.</summary>
public class RaidRegisterDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DirectorateName { get; set; }
    public string? BusinessAreaName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public RaidRegisterRole CurrentUserRole { get; set; }

    public int OpenRiskCount { get; set; }
    public int OpenIssueCount { get; set; }
    public int OpenAssumptionCount { get; set; }
    public int OpenDependencyCount { get; set; }
    public int OpenNearMissCount { get; set; }

    public List<RaidRegisterRiskRow> Risks { get; set; } = new();
    public List<RaidRegisterIssueRow> Issues { get; set; } = new();
    public List<RaidRegisterAssumptionRow> Assumptions { get; set; } = new();
    public List<RaidRegisterDependencyRow> Dependencies { get; set; } = new();
    public List<RaidRegisterNearMissRow> NearMisses { get; set; } = new();

    public List<string> WorkItemNames { get; set; } = new();
    public List<string> ServiceNames { get; set; } = new();
    public List<RaidRegisterUserRow> Users { get; set; } = new();
}

public class RaidRegisterRiskRow
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Owner { get; set; }
    public decimal? InherentScore { get; set; }
    public string? Tier { get; set; }
}

public class RaidRegisterIssueRow
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public string? Owner { get; set; }
}

public class RaidRegisterAssumptionRow
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Criticality { get; set; }
    public string? Owner { get; set; }
}

public class RaidRegisterDependencyRow
{
    public int Id { get; set; }
    public string? Description { get; set; }
    public string? LinkType { get; set; }
    public string? Status { get; set; }
    public string? Owner { get; set; }
}

public class RaidRegisterNearMissRow
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Impact { get; set; }
    public string? Status { get; set; }
    public string? Seriousness { get; set; }
}
