using Compass.Models;

namespace Compass.Models.Modern.Work;

/// <summary>Filters and tables for the work register (Index / All work).</summary>
public class WorkRegisterViewModel
{
    public string PageTitle { get; set; } = "Work register";
    public string PageDescription { get; set; } = "Active, paused and recently completed work items";
    public bool IsMyWork { get; set; }

    public int ActiveCount { get; set; }
    public int PausedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int RagRedCount { get; set; }
    public int AwaitingMonthlyCount { get; set; }
    public bool AwaitingMonthlyOverdue { get; set; }
    public int ActivePausedCountBeforeMonthlyFilter { get; set; }

    public List<WorkRegisterRow> ActivePaused { get; set; } = new();
    public List<WorkRegisterRow> Completed { get; set; } = new();
    public List<WorkRegisterRow> Cancelled { get; set; } = new();

    public List<Portfolio> Portfolios { get; set; } = new();
    public List<Directorate> Directorates { get; set; } = new();
    public List<WorkLookupOption> DeliveryPhaseOptions { get; set; } = new();
    public List<RagStatusLookupOption> RagOptions { get; set; } = new();
    public List<WorkLookupOption> PriorityOptions { get; set; } = new();

    public string? FilterSearch { get; set; }
    public int? FilterPortfolioId { get; set; }
    public int? FilterDirectorateId { get; set; }
    public int? FilterPhaseId { get; set; }
    public int? FilterRagId { get; set; }
    public int? FilterPriorityId { get; set; }
    public string? FilterMonthlyUpdate { get; set; }
}

public class WorkRegisterRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PrimaryContactName { get; set; }
    public int? PortfolioId { get; set; }
    public string? PortfolioName { get; set; }
    public string? DirectorateSummary { get; set; }
    public string? PhaseName { get; set; }
    public string? PriorityName { get; set; }
    public string? RagName { get; set; }
    public int? RagStatusId { get; set; }
    public string? SroDisplayName { get; set; }
    public string? RagBackgroundColourKey { get; set; }
    public string? RagTextColourKey { get; set; }
    public int MilestoneCount { get; set; }
    public string? MonthlyUpdateStatus { get; set; }
    public string? MonthlyUpdateStatusLink { get; set; }
    public string? MonthlyUpdateFilterKey { get; set; }
    public int? FirstRiskOrIssueId { get; set; }
    public string? FirstRiskReference { get; set; }
    public string? CompletedAt { get; set; }
    public string? CancelledReason { get; set; }
}

/// <summary>Phase / priority option for register filters (replaces compass2 LookupOption for these groups).</summary>
public class WorkLookupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? BackgroundColourKey { get; set; }
    public string? TextColourKey { get; set; }
}

/// <summary>RAG option for register filters (maps from <see cref="RagStatusLookup"/>).</summary>
public class RagStatusLookupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Alias for compass-2 <c>LookupOption</c> in ported views (phase/priority filters).</summary>
public class LookupOption : WorkLookupOption { }
