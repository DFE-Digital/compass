using Compass.ViewModels.Modern.Ddr;

namespace Compass.ViewModels.Modern;

/// <summary>Central Operations — DDR overview at <c>/modern/operations/ddr</c>.</summary>
public sealed class ModernOperationsDdrDashboardViewModel
{
    public int TotalDdrs { get; init; }

    /// <summary>Submitted DDRs with no DesignOps insight classification yet.</summary>
    public int PendingDesignOpsInsightCount { get; init; }

    public int SubmittedTotalCount { get; init; }
    public int RetrospectiveCount { get; init; }

    public IReadOnlyList<DdrOpsQueueRow> PendingInsightPreview { get; init; } = Array.Empty<DdrOpsQueueRow>();

    public IReadOnlyList<DdrDashboardBreakdownRow> ByCategory { get; init; } = Array.Empty<DdrDashboardBreakdownRow>();
    public IReadOnlyList<DdrDashboardBreakdownRow> ByDeviationType { get; init; } = Array.Empty<DdrDashboardBreakdownRow>();
    public IReadOnlyList<DdrDashboardBreakdownRow> ByProduct { get; init; } = Array.Empty<DdrDashboardBreakdownRow>();

    public string OversightPendingInsightUrl { get; init; } = "#";

    /// <summary>DesignOps oversight hub (all filters).</summary>
    public string OversightDashboardUrl { get; init; } = "#";

    public string RegisterUrl { get; init; } = "#";
    public string RegisterRetrospectiveUrl { get; init; } = "#";
}

public sealed class DdrOpsQueueRow
{
    public string Reference { get; init; } = "";
    public string ShortTitle { get; init; } = "";
    public DateTime? SubmittedAt { get; init; }
    public string DetailUrl { get; init; } = "#";
}
