namespace Compass.ViewModels.Modern;

/// <summary>Central Operations home — at-a-glance metrics and entry points to operations tools.</summary>
public sealed class ModernOperationsDashboardViewModel
{
    public int PendingTierChangeCount { get; init; }
    public int PendingEscalationsCount { get; init; }
    public int PendingDeescalationsCount { get; init; }
    public int CurrentlyEscalatedCount { get; init; }
}
