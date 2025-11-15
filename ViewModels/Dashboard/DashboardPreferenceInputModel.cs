using System.Collections.Generic;

namespace Compass.ViewModels.Dashboard;

public class DashboardPreferenceInputModel
{
    public bool ShowTasksPanel { get; set; }
    public bool ShowProductPanel { get; set; }
    public bool ShowRiskPanel { get; set; }
    public bool ShowMilestonePanel { get; set; }
    public bool ShowRemindersPanel { get; set; }
    public bool ShowSuccessPanel { get; set; }

    public string PreferredTaskGrouping { get; set; } = "priority";
    public string? DashboardFocus { get; set; }

    public List<string> SelectedQuickLinks { get; set; } = new();
}

