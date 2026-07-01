using Compass.ViewModels.Modern;

namespace Compass.Services;

public interface ICmsCompassServiceDataComparisonService
{
    Task<CmsCompassServiceDataViewModel> BuildReportAsync(CancellationToken cancellationToken = default);
}
