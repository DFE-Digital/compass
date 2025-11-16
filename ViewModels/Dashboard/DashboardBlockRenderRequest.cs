using System.Collections.Generic;

namespace Compass.ViewModels.Dashboard;

public class DashboardBlockRenderRequest
{
    public string BlockType { get; set; } = string.Empty;
    public string? BlockId { get; set; }
    public Dictionary<string, string>? Settings { get; set; }
}

