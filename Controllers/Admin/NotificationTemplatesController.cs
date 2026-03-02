using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class NotificationTemplatesController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<NotificationTemplatesController> _logger;
    private readonly IPermissionService _permissionService;

    public NotificationTemplatesController(
        CompassDbContext context,
        ILogger<NotificationTemplatesController> logger,
        IPermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
    }

    private string GetUserEmail()
    {
        return User.Identity?.Name 
            ?? User.FindFirst(ClaimTypes.Email)?.Value 
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value
            ?? string.Empty;
    }

    private async Task<bool> IsAuthorizedAsync()
    {
        var userEmail = GetUserEmail();
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await _permissionService.IsSuperAdminAsync(userEmail) ||
               await _permissionService.IsInGroupAsync(userEmail, "Central Operations Admin");
    }

    // GET: Admin/NotificationTemplates
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var templates = await _context.NotificationTemplates
            .Include(t => t.NotificationRules)
            .OrderBy(t => t.TriggerCode)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return View("~/Views/Admin/NotificationTemplates/Index.cshtml", templates);
    }

    // GET: Admin/NotificationTemplates/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var template = await _context.NotificationTemplates
            .Include(t => t.NotificationRules)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/NotificationTemplates/Details.cshtml", template);
    }

    // GET: Admin/NotificationTemplates/Create
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        ViewBag.TriggerCodes = GetTriggerCodes();
        return View("~/Views/Admin/NotificationTemplates/Create.cshtml");
    }

    // POST: Admin/NotificationTemplates/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description,TriggerCode,Subject,Body,IsActive")] NotificationTemplate template)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            var userEmail = GetUserEmail();
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            template.CreatedBy = userEmail;
            template.UpdatedBy = userEmail;

            _context.Add(template);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification template created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.TriggerCodes = GetTriggerCodes();
        return View("~/Views/Admin/NotificationTemplates/Create.cshtml", template);
    }

    // GET: Admin/NotificationTemplates/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template == null)
        {
            return NotFound();
        }

        ViewBag.TriggerCodes = GetTriggerCodes();
        return View("~/Views/Admin/NotificationTemplates/Edit.cshtml", template);
    }

    // POST: Admin/NotificationTemplates/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,TriggerCode,Subject,Body,IsActive,CreatedAt")] NotificationTemplate template)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        if (id != template.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var userEmail = GetUserEmail();
                template.UpdatedAt = DateTime.UtcNow;
                template.UpdatedBy = userEmail;

                _context.Update(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notification template updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await NotificationTemplateExistsAsync(template.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        ViewBag.TriggerCodes = GetTriggerCodes();
        return View("~/Views/Admin/NotificationTemplates/Edit.cshtml", template);
    }

    // GET: Admin/NotificationTemplates/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var template = await _context.NotificationTemplates
            .Include(t => t.NotificationRules)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/NotificationTemplates/Delete.cshtml", template);
    }

    // POST: Admin/NotificationTemplates/Delete/5
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var template = await _context.NotificationTemplates.FindAsync(id);
        if (template != null)
        {
            // Check if template is used by any rules
            var hasRules = await _context.NotificationRules
                .AnyAsync(r => r.NotificationTemplateId == id);

            if (hasRules)
            {
                TempData["ErrorMessage"] = "Cannot delete template that is used by notification rules. Please delete the rules first.";
                return RedirectToAction(nameof(Index));
            }

            _context.NotificationTemplates.Remove(template);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification template deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> NotificationTemplateExistsAsync(int id)
    {
        return await _context.NotificationTemplates.AnyAsync(e => e.Id == id);
    }

    private List<string> GetTriggerCodes()
    {
        return new List<string>
        {
            "team_member_added",
            "sro_assigned",
            "primary_contact_assigned",
            "rag_status_changed",
            "report_reminder",
            "project_created",
            "project_status_changed"
        };
    }
}
