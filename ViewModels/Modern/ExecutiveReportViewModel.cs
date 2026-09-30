namespace Compass.ViewModels.Modern;

/// <summary>Leadership summary across work, the service register, performance returns, and accessibility.</summary>
public sealed class ExecutiveReportViewModel
{
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public string? WorkError { get; set; }
    public string? ServiceRegisterError { get; set; }
    public string? PerformanceError { get; set; }
    public string? AccessibilityError { get; set; }

    public ExecutiveWorkSummary Work { get; set; } = new();
    public ExecutiveServiceRegisterSummary ServiceRegister { get; set; } = new();
    public ExecutivePerformanceSummary Performance { get; set; } = new();
    public ExecutiveAccessibilitySummary Accessibility { get; set; } = new();
}

public sealed class ExecutiveCountRow
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public decimal Percent { get; set; }
}

public sealed class ExecutiveFieldCompletionRow
{
    public string Field { get; set; } = "";
    public string Hint { get; set; } = "";
    public int Complete { get; set; }
    public int Missing { get; set; }
    public decimal CompletionPercent { get; set; }
}

public sealed class ExecutiveWorkSummary
{
    public int ActiveCount { get; set; }
    public int PausedCount { get; set; }
    public int InProgressCount => ActiveCount + PausedCount;
    public int LinkedToPriorityOutcomeCount { get; set; }
    public int NotLinkedToPriorityOutcomeCount { get; set; }
    public List<ExecutiveCountRow> Priorities { get; set; } = new();
    public List<ExecutiveCountRow> PriorityOutcomes { get; set; } = new();
    public List<ExecutiveCountRow> Phases { get; set; } = new();
}

public sealed class ExecutiveServiceListRow
{
    public Guid ProductId { get; set; }
    public int UniqueId { get; set; }
    public string Title { get; set; } = "";
    public string Phase { get; set; } = "Not set";
    public string BusinessAreas { get; set; } = "Not set";
}

public sealed class ExecutiveServiceRegisterSummary
{
    public int ActiveCount { get; set; }
    public int NewCount { get; set; }
    public int RetiredCount { get; set; }
    public int RejectedCount { get; set; }
    public int EnterpriseCount { get; set; }
    public decimal OverallCompletionPercent { get; set; }
    public List<ExecutiveFieldCompletionRow> DataQuality { get; set; } = new();
    public List<ExecutiveServiceListRow> ActiveServices { get; set; } = new();
    public List<ExecutiveServiceListRow> EnterpriseServices { get; set; } = new();
}

public sealed class ExecutiveCommissionRow
{
    public int CommissionId { get; set; }
    public string Name { get; set; } = "";
    public DateTime DueDate { get; set; }
    public int ProductsInScope { get; set; }
    public decimal ReturnRatePercent { get; set; }
    public decimal MetricCompletionPercent { get; set; }
}

public sealed class ExecutiveCommissionBusinessAreaTab
{
    public int CommissionId { get; set; }
    public string Name { get; set; } = "";
    public DateTime DueDate { get; set; }
    public List<ExecutiveBusinessAreaCompletionRow> BusinessAreas { get; set; } = new();
}

public sealed class ExecutiveBusinessAreaCompletionRow
{
    public string BusinessArea { get; set; } = "";
    public int Total { get; set; }
    public int Returned { get; set; }
    public decimal ReturnRatePercent { get; set; }
    public decimal MetricCompletionPercent { get; set; }
}

public sealed class ExecutivePerformanceSummary
{
    public decimal AverageReturnRatePercent { get; set; }
    public decimal AverageMetricCompletionPercent { get; set; }
    public int CommissionCount { get; set; }
    public List<ExecutiveCommissionRow> Commissions { get; set; } = new();
    public List<ExecutiveCommissionBusinessAreaTab> BusinessAreaRounds { get; set; } = new();
}

public sealed class ExecutiveCriterionRow
{
    public string Criterion { get; set; } = "";
    public int Open { get; set; }
    public int Overdue { get; set; }
    public int Closed { get; set; }
}

public sealed class ExecutiveAccessibilityAreaRow
{
    public string BusinessArea { get; set; } = "";
    public int Open { get; set; }
    public int Overdue { get; set; }
    public int Closed { get; set; }
}

public sealed class ExecutiveAccessibilitySummary
{
    public bool Loaded { get; set; }
    public int Onboarded { get; set; }
    public int StatementInstalled { get; set; }
    public int OpenIssues { get; set; }
    public int OverdueIssues { get; set; }
    public int ClosedIssues { get; set; }
    public int ActiveProductCount { get; set; }
    public int ProductsNotOnboarded { get; set; }
    public string? CompassNote { get; set; }
    public List<ExecutiveCriterionRow> Conformance { get; set; } = new();
    public List<ExecutiveAccessibilityAreaRow> BusinessAreas { get; set; } = new();
}
