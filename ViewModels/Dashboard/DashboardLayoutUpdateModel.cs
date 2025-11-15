using System.Collections.Generic;

namespace Compass.ViewModels.Dashboard;

public class DashboardLayoutUpdateModel
{
    public List<DashboardBlockInstance> Blocks { get; set; } = new();
}

