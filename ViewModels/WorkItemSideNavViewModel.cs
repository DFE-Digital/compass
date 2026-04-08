namespace Compass.ViewModels;

public class WorkItemSideNavViewModel
{
    public int ProjectId { get; set; }

    /// <summary>When true, section links navigate to WorkItemDetails with ?tab= (used from WorkItemDangerZone).</summary>
    public bool IsDangerZonePage { get; set; }

    /// <summary>Active tab on WorkItemDetails (overview, basic, …).</summary>
    public string CurrentTab { get; set; } = "overview";
}
