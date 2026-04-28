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

    /// <summary>Active + paused count for the current user (delivery team), with the same filters but ignoring org vs “your work” scope.</summary>
    public int RegisterActivePausedCountMine { get; set; }

    /// <summary>Active + paused count for the organisation register (no “mine” filter), with the same filters.</summary>
    public int RegisterActivePausedCountOrg { get; set; }

    /// <summary>Column heading for the current reporting month, e.g. &quot;Apr Update&quot; (aligned with work dashboard).</summary>
    public string RegisterMonthlyColumnHeader { get; set; } = "Monthly update";

    public List<WorkRegisterRow> ActivePaused { get; set; } = new();
    public List<WorkRegisterRow> Completed { get; set; } = new();
    public List<WorkRegisterRow> Cancelled { get; set; } = new();

    public List<Portfolio> Portfolios { get; set; } = new();
    public List<BusinessAreaLookup> BusinessAreas { get; set; } = new();
    public List<Directorate> Directorates { get; set; } = new();
    public List<WorkLookupOption> DeliveryPhaseOptions { get; set; } = new();
    public List<RagStatusLookupOption> RagOptions { get; set; } = new();
    public List<WorkLookupOption> PriorityOptions { get; set; } = new();

    public string? FilterSearch { get; set; }
    public int? FilterPortfolioId { get; set; }
    public int? FilterBusinessAreaId { get; set; }
    public int? FilterDirectorateId { get; set; }
    public int? FilterPhaseId { get; set; }
    public int? FilterRagId { get; set; }
    public int? FilterPriorityId { get; set; }
    public string? FilterMonthlyUpdate { get; set; }

    public int? FilterPrimaryContactUserId { get; set; }

    /// <summary>Selected tag ids (multi-select filter); work item must match at least one.</summary>
    public List<int> FilterTagIds { get; set; } = new();

    /// <summary>Single-tag filter (All work UI); mirrors one entry in <see cref="FilterTagIds"/> when used.</summary>
    public int? FilterTagId { get; set; }

    /// <summary>Register column sort key: title, businessarea, phase, priority, rag, primarycontact, status, monthly.</summary>
    public string RegisterSortField { get; set; } = "title";

    /// <summary>When true, register list is sorted descending for <see cref="RegisterSortField"/>.</summary>
    public bool RegisterSortDescending { get; set; }

    /// <summary>Distinct primary contacts among non-deleted work items (register filter).</summary>
    public List<WorkPrimaryContactOption> PrimaryContactFilterOptions { get; set; } = new();

    /// <summary>Active tag definitions for multi-select filter.</summary>
    public List<WorkLookupOption> TagFilterOptions { get; set; } = new();

    /// <summary>When true, <see cref="RegisterPageRows"/> holds the current page; <see cref="ActivePaused"/> / Completed / Cancelled are empty.</summary>
    public bool RegisterIsPaginated { get; set; }

    /// <summary>Tab key for pagination: active, completed, cancelled, all.</summary>
    public string RegisterTab { get; set; } = "active";

    public int RegisterPage { get; set; } = 1;
    public int RegisterPageSize { get; set; } = 20;
    public int RegisterTotalCount { get; set; }
    public int RegisterPageCount { get; set; } = 1;

    /// <summary>1-based index of first row on this page (0 if none).</summary>
    public int RegisterDisplayRowStart { get; set; }

    /// <summary>1-based index of last row on this page (0 if none).</summary>
    public int RegisterDisplayRowEnd { get; set; }

    public List<WorkRegisterRow> RegisterPageRows { get; set; } = new();

    /// <summary>Removable filter summary for the register search bar (chips with clear links).</summary>
    public IReadOnlyList<SearchAndFilterActiveChip> ActiveFilterChips { get; set; } = Array.Empty<SearchAndFilterActiveChip>();
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

    /// <summary>Business area name from <see cref="Project.BusinessAreaLookup"/> (register &quot;Business area&quot; column).</summary>
    public string? BusinessAreaName { get; set; }
    public string? PhaseName { get; set; }
    public string? PriorityName { get; set; }
    public string? RagName { get; set; }

    /// <summary>Suffix for <c>dfe-c-tag--*</c> from <see cref="Compass.Models.RagStatusLookup.CssClass"/> (admin).</summary>
    public string? RagCssClass { get; set; }

    public int? RagStatusId { get; set; }
    public string? SroDisplayName { get; set; }
    public string? RagBackgroundColourKey { get; set; }
    public string? RagTextColourKey { get; set; }
    public int MilestoneCount { get; set; }
    public string? MonthlyUpdateStatus { get; set; }
    public string? MonthlyUpdateStatusLink { get; set; }
    public string? MonthlyUpdateFilterKey { get; set; }
    /// <summary>e.g. "Mar Update" for the current reporting period.</summary>
    public string? LatestMonthlyDueLabel { get; set; }
    /// <summary>view | complete | not-due | na (completed/cancelled — no monthly cycle).</summary>
    public string LatestMonthlyDueAction { get; set; } = "na";
    public string? LatestMonthlyDueUrl { get; set; }
    /// <summary>Full month/year for accessibility, e.g. "April 2026".</summary>
    public string? LatestMonthlyPeriodLabel { get; set; }
    /// <summary>Badge text aligned with work dashboard: Submitted, Draft, Not due, Not started.</summary>
    public string? LatestMonthlyStatusLabel { get; set; }
    /// <summary>Short action for aria-label: View, Complete, or empty.</summary>
    public string? LatestMonthlyActionLabel { get; set; }
    public int? FirstRiskOrIssueId { get; set; }
    public string? FirstRiskReference { get; set; }
    public string? CompletedAt { get; set; }
    public string? CancelledReason { get; set; }

    /// <summary>Comma-separated tag names for display / search.</summary>
    public string? TagNamesSummary { get; set; }

    /// <summary>Sort proxy aligned with monthly activity (maps from project updated time).</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>Primary contact row for work register filters.</summary>
public class WorkPrimaryContactOption
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
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
