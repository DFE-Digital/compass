namespace Compass.ViewModels.Modern;

public class OperationsManagePerformanceViewModel
{
    public IReadOnlyList<CommissionPickerOption> CommissionOptions { get; init; } = Array.Empty<CommissionPickerOption>();
    public int? SelectedCommissionId { get; init; }
    public CommissionSummaryVm? Commission { get; init; }

    /// <summary>Products in catalogue scope for this commission (phase/type rules).</summary>
    public int EligibleProductCount { get; init; }

    public int SubmittedCount { get; init; }
    public int LateCount { get; init; }
    public int InProgressCount { get; init; }
    public int NotStartedCount { get; init; }

    /// <summary>Submitted + Late (final returns).</summary>
    public int ActualReturnCount => SubmittedCount + LateCount;

    public IReadOnlyList<OpsPerfOrgRow> BusinessAreaRows { get; init; } = Array.Empty<OpsPerfOrgRow>();
    public IReadOnlyList<OpsPerfOrgRow> DirectorateRows { get; init; } = Array.Empty<OpsPerfOrgRow>();

    public string StatusDoughnutJson { get; init; } = "{}";
    public string BusinessAreaBarJson { get; init; } = "{}";
    public string SubmissionTimelineJson { get; init; } = "{}";

    public bool HasCommission { get; init; }
    public bool HasEligibleProducts { get; init; }
}

public class CommissionPickerOption
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public DateTime DueDate { get; init; }
    public bool IsActive { get; init; }
}

public class CommissionSummaryVm
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string? Quarter { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime OpenDate { get; init; }
    public DateTime DueDate { get; init; }
    public bool IsActive { get; init; }
}

public class OpsPerfOrgRow
{
    public string Name { get; init; } = "";
    public int PotentialSubmissions { get; init; }
    public int ActualSubmitted { get; init; }
    public int ActualLate { get; init; }
    public int InProgress { get; init; }
    public int NotStarted { get; init; }

    public decimal CompletionPercent =>
        PotentialSubmissions <= 0
            ? 0
            : Math.Round(100m * (ActualSubmitted + ActualLate) / PotentialSubmissions, 1);
}
