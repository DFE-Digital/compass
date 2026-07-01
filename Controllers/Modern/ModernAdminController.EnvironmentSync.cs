using Compass.Attributes;
using Compass.Services.EnvironmentSync;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
  [HttpPost("environment-sync/preview")]
  [ValidateAntiForgeryToken]
  [RequireSuperAdmin]
  public async Task<IActionResult> EnvironmentSyncPreview(
    int direction,
    [FromServices] IEnvironmentSyncService syncService,
    CancellationToken cancellationToken)
  {
    if (!Enum.IsDefined(typeof(EnvironmentSyncDirection), direction))
    {
      TempData["AdminError"] = "Unknown sync direction.";
      return RedirectToAction(nameof(Index), new { panel = "environment-sync" });
    }

    try
    {
      var preview = await syncService.PreviewAsync((EnvironmentSyncDirection)direction, cancellationToken);
      TempData["EnvironmentSyncPreviewJson"] = System.Text.Json.JsonSerializer.Serialize(preview);
    }
    catch (Exception ex)
    {
      TempData["AdminError"] = ex.Message;
    }

    return RedirectToAction(nameof(Index), new { panel = "environment-sync" });
  }

  [HttpPost("environment-sync/execute")]
  [ValidateAntiForgeryToken]
  [RequireSuperAdmin]
  public async Task<IActionResult> EnvironmentSyncExecute(
    int direction,
    string confirmationPhrase,
    bool dryRun,
    [FromServices] IEnvironmentSyncService syncService,
    CancellationToken cancellationToken)
  {
    if (!Enum.IsDefined(typeof(EnvironmentSyncDirection), direction))
    {
      TempData["AdminError"] = "Unknown sync direction.";
      return RedirectToAction(nameof(Index), new { panel = "environment-sync" });
    }

    var email = User.Identity?.Name ?? "unknown";
    var result = await syncService.ExecuteAsync(
      (EnvironmentSyncDirection)direction,
      confirmationPhrase,
      email,
      dryRun,
      cancellationToken);

    TempData["EnvironmentSyncResultJson"] = System.Text.Json.JsonSerializer.Serialize(result);
    if (!result.Success)
      TempData["AdminError"] = string.Join(" ", result.Errors);
    else if (result.DryRun)
      TempData["AdminMessage"] = "Dry run completed. Review the summary below, then run again without dry run to apply changes.";
    else
      TempData["AdminMessage"] = "Environment sync completed.";

    return RedirectToAction(nameof(Index), new { panel = "environment-sync" });
  }

  private async Task<EnvironmentSyncPanelViewModel?> BuildEnvironmentSyncPanelAsync(CancellationToken cancellationToken)
  {
    var syncService = HttpContext.RequestServices.GetRequiredService<IEnvironmentSyncService>();
    EnvironmentSyncConnectionInfo connection;
    try
    {
      connection = await syncService.GetConnectionInfoAsync(cancellationToken);
    }
    catch (Exception ex)
    {
      return new EnvironmentSyncPanelViewModel
      {
        Connection = new EnvironmentSyncConnectionInfo
        {
          CurrentCatalog = "unknown",
          PeerCatalog = "unknown",
          CurrentIsProduction = false,
          PeerIsProduction = false,
          IsEnabled = false,
          DisabledReason = ex.Message
        }
      };
    }

    EnvironmentSyncPreview? serviceRegisterPreview = null;
    EnvironmentSyncPreview? workRaidPreview = null;
    if (TempData["EnvironmentSyncPreviewJson"] is string previewJson)
    {
      try
      {
        var preview = System.Text.Json.JsonSerializer.Deserialize<EnvironmentSyncPreview>(previewJson);
        if (preview?.Direction == EnvironmentSyncDirection.DevToProdServiceRegister)
          serviceRegisterPreview = preview;
        else if (preview?.Direction == EnvironmentSyncDirection.ProdToDevWorkRaid)
          workRaidPreview = preview;
      }
      catch { /* ignore stale temp data */ }
    }

    EnvironmentSyncResult? lastResult = null;
    if (TempData["EnvironmentSyncResultJson"] is string resultJson)
    {
      try
      {
        lastResult = System.Text.Json.JsonSerializer.Deserialize<EnvironmentSyncResult>(resultJson);
      }
      catch { /* ignore stale temp data */ }
    }

    return new EnvironmentSyncPanelViewModel
    {
      Connection = connection,
      ServiceRegisterPreview = serviceRegisterPreview,
      WorkRaidPreview = workRaidPreview,
      LastResult = lastResult
    };
  }
}
