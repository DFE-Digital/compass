using Compass.Services.Modern;

namespace Compass.ViewModels.Modern;

public sealed class GdsReturnReportViewModel
{
    public List<GdsReturnCommissionOption> Commissions { get; set; } = new();

    public List<int> SelectedCommissionIds { get; set; } = new();
}
