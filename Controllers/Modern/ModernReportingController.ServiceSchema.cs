using Compass.Services.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    [HttpGet("service-schema")]
    public async Task<IActionResult> ServiceSchema(string? area, CancellationToken cancellationToken = default)
    {
        if (!SchemaReportEnabled())
            return NotFound();

        try
        {
            var model = await ServiceSchemaReport.LoadAsync(_context, area, cancellationToken);
            SetNav("reporting-service-schema");
            return View("~/Views/Modern/Reporting/ServiceSchema.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service census report");
            TempData["ErrorMessage"] = "An error occurred while loading the service census report. Please try again.";
            SetNav("reporting-service-schema");
            return View("~/Views/Modern/Reporting/ServiceSchema.cshtml", new ServiceSchemaDqReportViewModel());
        }
    }

    [HttpGet("service-schema/topics/{key}")]
    public async Task<IActionResult> ServiceSchemaTopic(
        string key,
        string? view,
        string? value,
        string? area,
        int? page,
        CancellationToken cancellationToken = default)
    {
        if (!SchemaReportEnabled())
            return NotFound();

        try
        {
            var model = await ServiceSchemaReport.LoadTopicAsync(_context, key, view, value, area, page, cancellationToken);
            if (model == null)
                return NotFound();
            SetNav("reporting-service-schema");
            return View("~/Views/Modern/Reporting/ServiceSchemaTopic.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service census question report");
            TempData["ErrorMessage"] = "An error occurred while loading this census question. Please try again.";
            return RedirectToAction(nameof(ServiceSchema));
        }
    }

    private bool SchemaReportEnabled() =>
        ViewBag.ShowFipsDatabaseServiceRegister as bool? == true
        && ViewBag.ShowServiceRegisterSchema as bool? == true;
}
