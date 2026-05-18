using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("business-area-leadership")]
    public IActionResult BusinessAreaLeadership()
        => RedirectToAction(nameof(Index), new { panel = "business-area-leadership" });

    [HttpGet("business-area-leadership/create")]
    public async Task<IActionResult> BusinessAreaLeadershipCreate(CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-roles");
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;

        var options = await _context.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new RaidLookupOptionVm(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        var vm = new AdminBusinessAreaLeadershipCreateViewModel { BusinessAreaOptions = options };
        return View("~/Views/Modern/Admin/BusinessAreaLeadershipCreate.cshtml", vm);
    }

    [HttpPost("business-area-leadership/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BusinessAreaLeadershipCreate(
        int businessAreaLookupId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");

        if (userId <= 0)
        {
            TempData["AdminError"] = "Search for a user and select them from the directory before adding.";
            return RedirectToAction(nameof(BusinessAreaLeadershipCreate));
        }

        if (businessAreaLookupId <= 0)
        {
            TempData["AdminError"] = "Select a business area.";
            return RedirectToAction(nameof(BusinessAreaLeadershipCreate));
        }

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            TempData["AdminError"] =
                "That user could not be found. They must sign in at least once before they can be added.";
            return RedirectToAction(nameof(BusinessAreaLeadershipCreate));
        }

        var baOk = await _context.BusinessAreaLookups.AsNoTracking()
            .AnyAsync(b => b.Id == businessAreaLookupId && b.IsActive, cancellationToken);
        if (!baOk)
        {
            TempData["AdminError"] = "That business area is invalid or inactive.";
            return RedirectToAction(nameof(BusinessAreaLeadershipCreate));
        }

        if (await _context.BusinessAreaLeadershipMembers.AnyAsync(
                m => m.UserId == user.Id && m.BusinessAreaLookupId == businessAreaLookupId,
                cancellationToken))
        {
            TempData["AdminMessage"] = "That user is already listed as leadership for this business area.";
            return RedirectToAction(nameof(Index), new { panel = "business-area-leadership" });
        }

        var now = DateTime.UtcNow;
        _context.BusinessAreaLeadershipMembers.Add(new BusinessAreaLeadershipMember
        {
            UserId = user.Id,
            BusinessAreaLookupId = businessAreaLookupId,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);

        TempData["AdminMessage"] =
            $"{user.Name ?? user.Email} added as Deputy Director (DD) / leadership for this business area.";
        return RedirectToAction(nameof(Index), new { panel = "business-area-leadership" });
    }

    [HttpPost("business-area-leadership/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BusinessAreaLeadershipRemove(
        int membershipId,
        CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");
        var row = await _context.BusinessAreaLeadershipMembers
            .FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken);
        if (row == null)
        {
            TempData["AdminError"] = "That membership could not be found.";
            return RedirectToAction(nameof(Index), new { panel = "business-area-leadership" });
        }

        _context.BusinessAreaLeadershipMembers.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminMessage"] = "Business area leadership assignment removed.";
        return RedirectToAction(nameof(Index), new { panel = "business-area-leadership" });
    }
}
