using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("organisation-structure")]
    public async Task<IActionResult> OrganisationStructure(CancellationToken cancellationToken = default)
    {
        SetAdminChrome("admin-index");
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;
        if (TempData["AdminMessage"] is string msg)
            ViewBag.AdminMessage = msg;

        var directorates = await _context.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(cancellationToken);

        var links = await _context.DivisionBusinessAreas.AsNoTracking()
            .Include(d => d.BusinessAreaLookup)
            .OrderBy(d => d.DivisionId)
            .ThenBy(d => d.BusinessAreaLookup.Name)
            .ToListAsync(cancellationToken);

        var allBas = await _context.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new RaidLookupOptionVm(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        var linkByDir = links.GroupBy(l => l.DivisionId).ToDictionary(g => g.Key, g => g.ToList());
        var rows = new List<AdminDirectorateWithBusinessAreasRow>();
        foreach (var d in directorates)
        {
            var r = new AdminDirectorateWithBusinessAreasRow
            {
                DivisionId = d.Id,
                Name = d.Name
            };
            if (linkByDir.TryGetValue(d.Id, out var list))
            {
                foreach (var l in list)
                {
                    r.Links.Add(new AdminDirectorateBusinessAreaLinkRow
                    {
                        LinkId = l.Id,
                        BusinessAreaName = l.BusinessAreaLookup.Name
                    });
                }
            }
            rows.Add(r);
        }

        var vm = new AdminOrganisationStructureViewModel
        {
            Directorates = rows,
            AllBusinessAreaOptions = allBas
        };
        return View("~/Views/Modern/Admin/OrganisationStructure.cshtml", vm);
    }

    [HttpPost("organisation-structure/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrganisationStructureAdd(
        int divisionId,
        int businessAreaLookupId,
        CancellationToken cancellationToken = default)
    {
        if (divisionId <= 0 || businessAreaLookupId <= 0)
        {
            TempData["AdminError"] = "Select a directorate and a business area.";
            return RedirectToAction(nameof(OrganisationStructure));
        }

        if (!await _context.Divisions.AnyAsync(d => d.Id == divisionId && d.IsActive, cancellationToken)
            || !await _context.BusinessAreaLookups.AnyAsync(
                b => b.Id == businessAreaLookupId && b.IsActive, cancellationToken))
        {
            TempData["AdminError"] = "That directorate or business area is invalid or inactive.";
            return RedirectToAction(nameof(OrganisationStructure));
        }

        if (await _context.DivisionBusinessAreas.AnyAsync(
                d => d.DivisionId == divisionId && d.BusinessAreaLookupId == businessAreaLookupId,
                cancellationToken))
        {
            TempData["AdminError"] = "That business area is already linked to the directorate.";
            return RedirectToAction(nameof(OrganisationStructure));
        }

        var now = DateTime.UtcNow;
        _context.DivisionBusinessAreas.Add(new DivisionBusinessArea
        {
            DivisionId = divisionId,
            BusinessAreaLookupId = businessAreaLookupId,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminMessage"] = "Structure updated: business area linked to directorate.";
        return RedirectToAction(nameof(OrganisationStructure));
    }

    [HttpPost("organisation-structure/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OrganisationStructureRemove(
        int linkId,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.DivisionBusinessAreas
            .FirstOrDefaultAsync(d => d.Id == linkId, cancellationToken);
        if (row == null)
        {
            TempData["AdminError"] = "That link could not be found.";
            return RedirectToAction(nameof(OrganisationStructure));
        }

        _context.DivisionBusinessAreas.Remove(row);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["AdminMessage"] = "Link removed. The business area can still be linked to other directorates.";
        return RedirectToAction(nameof(OrganisationStructure));
    }
}
