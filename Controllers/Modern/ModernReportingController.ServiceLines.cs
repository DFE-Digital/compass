using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    [HttpGet("service-lines")]
    public async Task<IActionResult> ServiceLineReport(CancellationToken cancellationToken = default)
    {
        if (ViewBag.ShowFipsDatabaseServiceRegister as bool? != true)
            return NotFound();

        try
        {
            var includeSchema = ViewBag.ShowServiceRegisterSchema as bool? == true;
            var model = await Compass.Services.ServiceLineReport.LoadAsync(_context, includeSchema, cancellationToken);
            SetNav("reporting-service-lines");
            return View("~/Views/Modern/Reporting/ServiceLineReport.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service line report");
            TempData["ErrorMessage"] = "An error occurred while loading the service lines report. Please try again.";
            SetNav("reporting-service-lines");
            return View("~/Views/Modern/Reporting/ServiceLineReport.cshtml", new ServiceLineReportViewModel());
        }
    }
}
