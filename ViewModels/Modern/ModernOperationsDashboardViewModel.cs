namespace Compass.ViewModels.Modern;

/// <summary>Central Operations home — at-a-glance metrics and entry points to operations tools.</summary>
public sealed class ModernOperationsDashboardViewModel
{
    /// <summary>
    /// Rough “needs attention” total: new CMS requests + pending RAID tier reviews + DDR awaiting insight (DDR count is zero when feature off).
    /// </summary>
    public int AttentionQueueTotal { get; init; }

    public int PendingTierChangeCount { get; init; }
    public int PendingEscalationsCount { get; init; }
    public int PendingDeescalationsCount { get; init; }
    public int CurrentlyEscalatedCount { get; init; }
    public int ActiveRisksCount { get; init; }

    /// <summary>Active delivery work items (projects).</summary>
    public int ManageWorkActiveCount { get; init; }

    /// <summary>Demand pipeline requests (when demand feature is on).</summary>
    public int ManageDemandTotalCount { get; init; }

    /// <summary>Scheduled triage meetings with no date or a date on/after today.</summary>
    public int ManageTriageUpcomingMeetingsCount { get; init; }

    /// <summary>Active performance commission periods.</summary>
    public int ManagePerformanceActiveCommissionsCount { get; init; }

    /// <summary>Active products in the service register (when FIPS DB is on).</summary>
    public int ServiceRegisterActiveProductCount { get; init; }

    /// <summary>Set when the DDR feature is on — submitted DDRs awaiting insight classification.</summary>
    public int PendingDdrDesignOpsInsightCount { get; init; }

    /// <summary>CMS access requests (Design Decision Records review publishing) — New queue.</summary>
    public int CmsAccessRequestsNewCount { get; init; }

    public int CmsAccessRequestsCompletedCount { get; init; }

    public int CmsAccessRequestsRejectedCount { get; init; }
}
