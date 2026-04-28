using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("directorate-leadership")]
    public IActionResult DirectorateLeadership()
        => RedirectToAction(nameof(Index), new { panel = "directorate-leadership" });

    [HttpGet("directorate-leadership/create")]
    public async Task<IActionResult> DirectorateLeadershipCreate(CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-roles");
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;

        var options = await _context.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new RaidLookupOptionVm(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        var vm = new AdminDirectorateLeadershipCreateViewModel { DirectorateOptions = options };
        return View("~/Views/Modern/Admin/DirectorateLeadershipCreate.cshtml", vm);
    }

    [HttpPost("directorate-leadership/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DirectorateLeadershipCreate(
        int divisionId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");

        if (userId <= 0)
        {
            TempData["AdminError"] = "Search for a user and select them from the directory before adding.";
            return RedirectToAction(nameof(DirectorateLeadershipCreate));
        }

        if (divisionId <= 0)
        {
            TempData["AdminError"] = "Select a directorate.";
            return RedirectToAction(nameof(DirectorateLeadershipCreate));
        }

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            TempData["AdminError"] =
                "That user could not be found. They must sign in at least once before they can be added.";
            return RedirectToAction(nameof(DirectorateLeadershipCreate));
        }

        var dirOk = await _context.Divisions.AsNoTracking()
            .AnyAsync(d => d.Id == divisionId && d.IsActive, cancellationToken);
        if (!dirOk)
        {
            TempData["AdminError"] = "That directorate is invalid or inactive.";
            return RedirectToAction(nameof(DirectorateLeadershipCreate));
        }

        if (await _context.DivisionUsers.AnyAsync(
                m => m.UserId == user.Id && m.DivisionId == divisionId,
                cancellationToken))
        {
            TempData["AdminMessage"] = "That user is already listed as directorate leadership for this directorate.";
            return RedirectToAction(nameof(Index), new { panel = "directorate-leadership" });
        }

        var now = DateTime.UtcNow;
        _context.DivisionUsers.Add(new DivisionUser
        {
            UserId = user.Id,
            DivisionId = divisionId,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);

        TempData["AdminMessage"] =
            $"{user.Name ?? user.Email} added as directorate leadership for this directorate.";
        return RedirectToAction(nameof(Index), new { panel = "directorate-leadership" });
    }

    [HttpPost("directorate-leadership/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DirectorateLeadershipRemove(
        int membershipId,
        CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");
        var row = await _context.DivisionUsers
            .FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken);
        if (row == null)
        {
            TempData["AdminError"] = "That membership could not be found.";
            return RedirectToAction(nameof(Index), new { panel = "directorate-leadership" });
        }

        _context.DivisionUsers.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminMessage"] = "Directorate leadership assignment removed.";
        return RedirectToAction(nameof(Index), new { panel = "directorate-leadership" });
    }
}
