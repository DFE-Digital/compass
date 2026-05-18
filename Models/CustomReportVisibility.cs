namespace Compass.Models;

/// <summary>Who may open and run a custom report in the report library.</summary>
public enum CustomReportVisibility
{
    /// <summary>Only the owner; not listed to others.</summary>
    Private = 0,
    /// <summary>All signed-in users may run the report.</summary>
    Public = 1,
    /// <summary>Owner plus users explicitly added via <see cref="CustomReportShare"/>.</summary>
    Shared = 2
}
