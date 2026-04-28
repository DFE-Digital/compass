using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("business-area-admins")]
    public IActionResult BusinessAreaAdmins()
        => RedirectToAction(nameof(Index), new { panel = "business-area-admins" });

    [HttpGet("business-area-admins/create")]
    public async Task<IActionResult> BusinessAreaAdminCreate(CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-roles");
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;

        var options = await _context.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new RaidLookupOptionVm(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        var vm = new AdminBusinessAreaAdminCreateViewModel { BusinessAreaOptions = options };
        return View("~/Views/Modern/Admin/BusinessAreaAdminCreate.cshtml", vm);
    }

    [HttpPost("business-area-admins/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BusinessAreaAdminCreate(
        int businessAreaLookupId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");

        if (userId <= 0)
        {
            TempData["AdminError"] = "Search for a user and select them from the directory before adding.";
            return RedirectToAction(nameof(BusinessAreaAdminCreate));
        }

        if (businessAreaLookupId <= 0)
        {
            TempData["AdminError"] = "Select a business area.";
            return RedirectToAction(nameof(BusinessAreaAdminCreate));
        }

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            TempData["AdminError"] =
                "That user could not be found. They must sign in at least once before they can be added.";
            return RedirectToAction(nameof(BusinessAreaAdminCreate));
        }

        var baOk = await _context.BusinessAreaLookups.AsNoTracking()
            .AnyAsync(b => b.Id == businessAreaLookupId && b.IsActive, cancellationToken);
        if (!baOk)
        {
            TempData["AdminError"] = "That business area is invalid or inactive.";
            return RedirectToAction(nameof(BusinessAreaAdminCreate));
        }

        if (await _context.BusinessAreaAdminMembers.AnyAsync(
                m => m.UserId == user.Id && m.BusinessAreaLookupId == businessAreaLookupId,
                cancellationToken))
        {
            TempData["AdminMessage"] = "That user is already a delegated admin for this business area.";
            return RedirectToAction(nameof(Index), new { panel = "business-area-admins" });
        }

        var now = DateTime.UtcNow;
        _context.BusinessAreaAdminMembers.Add(new BusinessAreaAdminMember
        {
            UserId = user.Id,
            BusinessAreaLookupId = businessAreaLookupId,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);

        TempData["AdminMessage"] =
            $"{user.Name ?? user.Email} added as delegated admin for this business area.";
        return RedirectToAction(nameof(Index), new { panel = "business-area-admins" });
    }

    [HttpPost("business-area-admins/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BusinessAreaAdminRemove(int membershipId, CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");
        var row = await _context.BusinessAreaAdminMembers
            .FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken);
        if (row == null)
        {
            TempData["AdminError"] = "That membership could not be found.";
            return RedirectToAction(nameof(Index), new { panel = "business-area-admins" });
        }

        _context.BusinessAreaAdminMembers.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminMessage"] = "Business area permission removed.";
        return RedirectToAction(nameof(Index), new { panel = "business-area-admins" });
    }
}
