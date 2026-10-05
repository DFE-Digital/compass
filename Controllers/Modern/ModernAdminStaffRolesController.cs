using Compass.Attributes;
using Compass.Data;
using Compass.Models.ServiceSchema;
using Compass.Services.ServiceSchema;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

[Authorize]
[RequireAdmin]
[Route("modern/admin/staff-roles")]
public class ModernAdminStaffRolesController : Controller
{
    private readonly CompassDbContext _db;

    public ModernAdminStaffRolesController(CompassDbContext db) => _db = db;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.MainNavSection = "admin";
        ViewBag.SubNavItem = "admin-hub";
        var roles = await _db.StaffRoles.AsNoTracking()
            .OrderBy(r => r.Family).ThenBy(r => r.SortOrder).ThenBy(r => r.Name)
            .ToListAsync(ct);
        return View("~/Views/Modern/Admin/StaffRoles/Index.cshtml", roles);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string? family, string? name, string? description, CancellationToken ct)
    {
        var familyName = (family ?? "").Trim();
        var roleName = (name ?? "").Trim();
        if (familyName.Length is < 1 or > 80 || roleName.Length is < 1 or > 100)
        {
            TempData["AdminError"] = "Enter a family and a role name.";
            return RedirectToAction(nameof(Index));
        }

        var code = DdatStaffRoleCatalog.CodeFor(familyName, roleName);
        if (await _db.StaffRoles.AnyAsync(r => r.Code == code, ct))
        {
            TempData["AdminError"] = "That role already exists.";
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.UtcNow;
        var sort = await _db.StaffRoles.Select(r => (int?)r.SortOrder).MaxAsync(ct) ?? 0;
        _db.StaffRoles.Add(new StaffRole
        {
            Family = familyName,
            Name = roleName,
            Code = code,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            SourceUrl = ServiceSchemaCatalog.FrameworkHomeUrl,
            SortOrder = sort + 1,
            IsActive = true,
            IsFrameworkRole = false,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Staff role added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string? name, string? description, bool isActive, CancellationToken ct)
    {
        var role = await _db.StaffRoles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (role == null)
            return NotFound();

        var roleName = (name ?? "").Trim();
        if (roleName.Length is < 1 or > 100)
        {
            TempData["AdminError"] = "Enter a role name up to 100 characters.";
            return RedirectToAction(nameof(Index));
        }

        role.Name = roleName;
        role.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        role.IsActive = isActive;
        role.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = isActive
            ? "Staff role updated."
            : "Staff role deactivated. Existing contacts keep the role, and it cannot be chosen for new contacts.";
        return RedirectToAction(nameof(Index));
    }
}
