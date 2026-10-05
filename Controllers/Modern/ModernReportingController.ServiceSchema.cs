using Compass.Services.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    [HttpGet("service-schema")]
    public async Task<IActionResult> ServiceSchema(string? stage, CancellationToken cancellationToken = default)
    {
        if (!SchemaReportEnabled())
            return NotFound();

        try
        {
            var model = await ServiceSchemaReport.LoadAsync(_context, cancellationToken);
            model.Stage = NormaliseStage(stage);
            SetNav("reporting-service-schema");
            return View("~/Views/Modern/Reporting/ServiceSchema.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service schema data quality report");
            TempData["ErrorMessage"] = "An error occurred while loading the service schema report. Please try again.";
            SetNav("reporting-service-schema");
            return View("~/Views/Modern/Reporting/ServiceSchema.cshtml", new ServiceSchemaDqReportViewModel());
        }
    }

    [HttpGet("service-schema/topics/{key}")]
    public async Task<IActionResult> ServiceSchemaTopic(string key, string? view, CancellationToken cancellationToken = default)
    {
        if (!SchemaReportEnabled())
            return NotFound();

        try
        {
            var model = await ServiceSchemaReport.LoadTopicAsync(_context, key, view, cancellationToken);
            if (model == null)
                return NotFound();
            SetNav("reporting-service-schema");
            return View("~/Views/Modern/Reporting/ServiceSchemaTopic.cshtml", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading service schema topic report");
            TempData["ErrorMessage"] = "An error occurred while loading this schema question. Please try again.";
            return RedirectToAction(nameof(ServiceSchema));
        }
    }

    private bool SchemaReportEnabled() =>
        ViewBag.ShowFipsDatabaseServiceRegister as bool? == true
        && ViewBag.ShowServiceRegisterSchema as bool? == true;

    private static string NormaliseStage(string? stage) => stage?.Trim().ToLowerInvariant() switch
    {
        "not-started" or "in-progress" or "complete" => stage.Trim().ToLowerInvariant(),
        _ => ""
    };
}
