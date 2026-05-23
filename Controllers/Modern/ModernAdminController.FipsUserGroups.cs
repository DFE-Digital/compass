using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("fips/user-group/add")]
    public async Task<IActionResult> FipsUserGroupCreate(int? parentId)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        var allGroups = await _context.FipsUserGroups.AsNoTracking().ToListAsync();
        if (parentId.HasValue && allGroups.All(g => g.Id != parentId.Value))
            parentId = null;

        var vm = new AdminFipsUserGroupEditViewModel
        {
            IsCreate = true,
            ParentId = parentId,
            ParentPath = AdminFipsUserGroupTreeHelper.BuildParentPath(parentId, allGroups),
            DisplayOrder = 0,
            IsActive = true,
            ParentOptions = AdminFipsUserGroupTreeHelper.BuildParentOptionsForEdit(allGroups, excludeGroupId: null)
        };

        return View("~/Views/Modern/Admin/FipsUserGroupEdit.cshtml", vm);
    }

    [HttpGet("fips/user-group/{id:int}/edit")]
    public async Task<IActionResult> FipsUserGroupEdit(int id)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        var entity = await _context.FipsUserGroups.AsNoTracking()
            .Include(g => g.Synonyms)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (entity == null)
            return NotFound();

        var allGroups = await _context.FipsUserGroups.AsNoTracking().ToListAsync();
        var vm = new AdminFipsUserGroupEditViewModel
        {
            IsCreate = false,
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.Active,
            ParentId = entity.ParentId,
            ParentPath = AdminFipsUserGroupTreeHelper.BuildParentPath(entity.ParentId, allGroups),
            ChildCount = allGroups.Count(g => g.ParentId == entity.Id),
            ParentOptions = AdminFipsUserGroupTreeHelper.BuildParentOptionsForEdit(allGroups, entity.Id),
            Synonyms = entity.Synonyms
                .OrderBy(s => s.Synonym, StringComparer.OrdinalIgnoreCase)
                .Select(s => new AdminFipsUserGroupSynonymRow { Id = s.Id, Synonym = s.Synonym })
                .ToList()
        };

        return View("~/Views/Modern/Admin/FipsUserGroupEdit.cshtml", vm);
    }

    [HttpPost("fips/user-group/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsAddUserGroup(string name, string? description, int displayOrder, int? parentId)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        name = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(FipsUserGroupCreate), new { parentId });
        }

        var allGroups = await _context.FipsUserGroups.AsNoTracking().ToListAsync();
        if (!AdminFipsUserGroupTreeHelper.IsValidParentAssignment(parentId, groupId: null, allGroups))
        {
            TempData["AdminMessage"] = "Choose a valid parent group.";
            return RedirectToAction(nameof(FipsUserGroupCreate), new { parentId });
        }

        var entity = new FipsUserGroup
        {
            Name = name,
            Description = description?.Trim(),
            DisplayOrder = displayOrder,
            ParentId = parentId,
            Active = true
        };
        _context.FipsUserGroups.Add(entity);
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"User group \"{entity.Name}\" added.";
        return RedirectToAction(nameof(FipsUserGroupEdit), new { id = entity.Id });
    }

    [HttpPost("fips/user-group/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsUpdateUserGroup(int id, string name, string? description, int displayOrder, int? parentId)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        var entity = await _context.FipsUserGroups.FindAsync(id);
        if (entity == null)
            return NotFound();

        name = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["AdminMessage"] = "Name is required.";
            return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
        }

        var allGroups = await _context.FipsUserGroups.AsNoTracking().ToListAsync();
        if (!AdminFipsUserGroupTreeHelper.IsValidParentAssignment(parentId, id, allGroups))
        {
            TempData["AdminMessage"] = "Parent cannot be this group or one of its children.";
            return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
        }

        entity.Name = name;
        entity.Description = description?.Trim();
        entity.DisplayOrder = displayOrder;
        entity.ParentId = parentId;
        await _context.SaveChangesAsync();

        TempData["AdminMessage"] = $"User group \"{entity.Name}\" updated.";
        return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
    }

    [HttpPost("fips/user-group/{id:int}/synonym")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsAddUserGroupSynonym(int id, string synonym)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        synonym = synonym?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(synonym))
        {
            TempData["AdminMessage"] = "Synonym is required.";
            return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
        }

        var groupExists = await _context.FipsUserGroups.AnyAsync(g => g.Id == id);
        if (!groupExists)
            return NotFound();

        var duplicate = await _context.FipsUserGroupSynonyms.AnyAsync(s =>
            s.FipsUserGroupId == id && s.Synonym.ToLower() == synonym.ToLower());
        if (duplicate)
        {
            TempData["AdminMessage"] = "That synonym already exists for this group.";
            return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
        }

        _context.FipsUserGroupSynonyms.Add(new FipsUserGroupSynonym { FipsUserGroupId = id, Synonym = synonym });
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Synonym added.";
        return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
    }

    [HttpPost("fips/user-group/{id:int}/synonym/{synonymId:int}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsRemoveUserGroupSynonym(int id, int synonymId)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        var synonym = await _context.FipsUserGroupSynonyms
            .FirstOrDefaultAsync(s => s.Id == synonymId && s.FipsUserGroupId == id);
        if (synonym == null)
            return NotFound();

        _context.FipsUserGroupSynonyms.Remove(synonym);
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = "Synonym removed.";
        return RedirectToAction(nameof(FipsUserGroupEdit), new { id });
    }

    [HttpPost("fips/user-group/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FipsToggleUserGroup(int id)
    {
        var guard = await RequireFipsDatabaseAdminAsync();
        if (guard != null)
            return guard;

        var entity = await _context.FipsUserGroups.FindAsync(id);
        if (entity == null)
            return NotFound();

        entity.Active = !entity.Active;
        await _context.SaveChangesAsync();
        TempData["AdminMessage"] = $"\"{entity.Name}\" is now {(entity.Active ? "active" : "inactive")}.";

        var referer = Request.Headers.Referer.ToString();
        if (referer.Contains("/fips/user-group/", StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(FipsUserGroupEdit), new { id });

        return RedirectToAction("Index", new { panel = "fips-user-groups" });
    }
}
