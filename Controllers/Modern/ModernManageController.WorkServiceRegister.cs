using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernManageController
{
    [HttpGet("fips/{id:guid}/work-items/pick-work")]
    public async Task<IActionResult> FipsProductPickWork(Guid id, [FromQuery] string? q, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return new JsonResult(new { error = "FIPS is not available." }) { StatusCode = 403 };

        var email = CurrentUserEmail;
        if (!await _workServiceRegisterLinks.CanLinkFromServiceRegisterProductAsync(id, email, ct))
            return new JsonResult(new { error = "Access denied." }) { StatusCode = 403 };

        var term = (q ?? "").Trim();
        if (term.Length < 2)
            return Json(new { results = Array.Empty<object>() });

        var results = await _context.Projects
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                ((p.Title != null && p.Title.Contains(term)) || p.ProjectCode.Contains(term)))
            .OrderBy(p => p.Title)
            .Take(20)
            .Select(p => new
            {
                id = p.Id,
                title = p.Title ?? "",
                workCode = "WI-" + p.Id.ToString("D8", CultureInfo.InvariantCulture),
                subtitle = "WI-" + p.Id.ToString("D8", CultureInfo.InvariantCulture),
            })
            .ToListAsync(ct);

        return Json(new { results });
    }

    [HttpPost("fips/{id:guid}/work-items/link")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductLinkWork(Guid id, int projectId, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        var email = CurrentUserEmail;
        var outcome = await _workServiceRegisterLinks.LinkAsync(projectId, id, email, ct);
        if (!outcome.Success)
        {
            TempData["Error"] = outcome.Error ?? "Could not link work item.";
            return RedirectToAction(nameof(FipsProduct), new { id, tab = "work" });
        }

        TempData["Success"] = "Work item linked.";
        return RedirectToAction(nameof(FipsProduct), new { id, tab = "work" });
    }

    [HttpPost("fips/{id:guid}/work-items/{projectProductId:int}/unlink")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsProductUnlinkWork(Guid id, int projectProductId, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        var email = CurrentUserEmail;
        var outcome = await _workServiceRegisterLinks.UnlinkAsync(projectProductId, email, ct);
        if (!outcome.Success)
        {
            TempData["Error"] = outcome.Error ?? "Could not remove link.";
            return RedirectToAction(nameof(FipsProduct), new { id, tab = "work" });
        }

        TempData["Success"] = "Work item unlinked.";
        return RedirectToAction(nameof(FipsProduct), new { id, tab = "work" });
    }
}
