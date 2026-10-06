using Compass.Services.Modern;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    [HttpGet("gds-return")]
    public async Task<IActionResult> GdsReturn(CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        try
        {
            var commissions = await _gdsReturnExport.ListCommissionsAsync(cancellationToken);
            var model = new GdsReturnReportViewModel
            {
                Commissions = commissions.ToList(),
                SelectedCommissionIds = commissions.Where(c => c.IsActive).Select(c => c.Id).Take(2).ToList()
            };
            return View("~/Views/Modern/Reporting/GdsReturn.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GDS return report");
            TempData["ErrorMessage"] = "An error occurred while loading commissions. Please try again.";
            return View("~/Views/Modern/Reporting/GdsReturn.cshtml", new GdsReturnReportViewModel());
        }
    }

    [HttpPost("gds-return/export")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GdsReturnExport(
        int[]? commissionIds,
        CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var ids = (commissionIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            TempData["ErrorMessage"] = "Select at least one commission to export.";
            var commissions = await _gdsReturnExport.ListCommissionsAsync(cancellationToken);
            return View("~/Views/Modern/Reporting/GdsReturn.cshtml", new GdsReturnReportViewModel
            {
                Commissions = commissions.ToList()
            });
        }

        try
        {
            var bytes = await _gdsReturnExport.BuildExcelAsync(ids, cancellationToken);
            var fileName = $"gds-return-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting GDS return for commissions {CommissionIds}", string.Join(",", ids));
            TempData["ErrorMessage"] = "An error occurred while exporting the GDS return. Please try again.";
            var commissions = await _gdsReturnExport.ListCommissionsAsync(cancellationToken);
            return View("~/Views/Modern/Reporting/GdsReturn.cshtml", new GdsReturnReportViewModel
            {
                Commissions = commissions.ToList(),
                SelectedCommissionIds = ids
            });
        }
    }
}
