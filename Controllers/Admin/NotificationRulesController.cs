using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Compass.Controllers.Admin;

[Route("Admin/[controller]")]
[Authorize]
public class NotificationRulesController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<NotificationRulesController> _logger;
    private readonly IPermissionService _permissionService;

    public NotificationRulesController(
        CompassDbContext context,
        ILogger<NotificationRulesController> logger,
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

    // GET: Admin/NotificationRules
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var rules = await _context.NotificationRules
            .Include(r => r.NotificationTemplate)
            .OrderBy(r => r.TriggerCode)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return View("~/Views/Admin/NotificationRules/Index.cshtml", rules);
    }

    // GET: Admin/NotificationRules/Details/5
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var rule = await _context.NotificationRules
            .Include(r => r.NotificationTemplate)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/NotificationRules/Details.cshtml", rule);
    }

    // GET: Admin/NotificationRules/Create
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        ViewBag.Templates = await _context.NotificationTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
        ViewBag.TriggerCodes = GetTriggerCodes();
        ViewBag.RecipientTypes = GetRecipientTypes();
        ViewBag.RagStatuses = GetRagStatuses();

        return View("~/Views/Admin/NotificationRules/Create.cshtml");
    }

    // POST: Admin/NotificationRules/Create
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name,
        string? description,
        int notificationTemplateId,
        string triggerCode,
        bool isEnabled,
        List<string>? recipientTypes,
        List<string>? specificEmails,
        List<string>? ragStatuses,
        List<string>? projectStatuses)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var userEmail = GetUserEmail();

        // Build recipient configuration
        var recipientConfig = new Dictionary<string, object>();
        if (recipientTypes != null && recipientTypes.Any())
        {
            recipientConfig["recipients"] = recipientTypes;
        }
        if (specificEmails != null && specificEmails.Any(e => !string.IsNullOrWhiteSpace(e)))
        {
            recipientConfig["specific_emails"] = specificEmails.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        }

        // Build conditions
        var conditions = new Dictionary<string, object>();
        if (ragStatuses != null && ragStatuses.Any())
        {
            conditions["rag_statuses"] = ragStatuses;
        }
        if (projectStatuses != null && projectStatuses.Any())
        {
            conditions["project_statuses"] = projectStatuses;
        }

        var rule = new NotificationRule
        {
            Name = name,
            Description = description,
            NotificationTemplateId = notificationTemplateId,
            TriggerCode = triggerCode,
            IsEnabled = isEnabled,
            RecipientConfiguration = recipientConfig.Any() ? JsonSerializer.Serialize(recipientConfig) : null,
            Conditions = conditions.Any() ? JsonSerializer.Serialize(conditions) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userEmail,
            UpdatedBy = userEmail
        };

        if (ModelState.IsValid)
        {
            _context.Add(rule);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification rule created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Templates = await _context.NotificationTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
        ViewBag.TriggerCodes = GetTriggerCodes();
        ViewBag.RecipientTypes = GetRecipientTypes();
        ViewBag.RagStatuses = GetRagStatuses();

        return View("~/Views/Admin/NotificationRules/Create.cshtml", rule);
    }

    // GET: Admin/NotificationRules/Edit/5
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var rule = await _context.NotificationRules
            .Include(r => r.NotificationTemplate)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        ViewBag.Templates = await _context.NotificationTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
        ViewBag.TriggerCodes = GetTriggerCodes();
        ViewBag.RecipientTypes = GetRecipientTypes();
        ViewBag.RagStatuses = GetRagStatuses();

        // Parse recipient configuration and conditions
        ViewBag.SelectedRecipients = ParseRecipientConfiguration(rule.RecipientConfiguration);
        ViewBag.SpecificEmails = ParseSpecificEmails(rule.RecipientConfiguration);
        ViewBag.SelectedRagStatuses = ParseRagStatuses(rule.Conditions);
        ViewBag.SelectedProjectStatuses = ParseProjectStatuses(rule.Conditions);

        return View("~/Views/Admin/NotificationRules/Edit.cshtml", rule);
    }

    // POST: Admin/NotificationRules/Edit/5
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        string name,
        string? description,
        int notificationTemplateId,
        string triggerCode,
        bool isEnabled,
        List<string>? recipientTypes,
        List<string>? specificEmails,
        List<string>? ragStatuses,
        List<string>? projectStatuses)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var rule = await _context.NotificationRules.FindAsync(id);
        if (rule == null)
        {
            return NotFound();
        }

        var userEmail = GetUserEmail();

        // Build recipient configuration
        var recipientConfig = new Dictionary<string, object>();
        if (recipientTypes != null && recipientTypes.Any())
        {
            recipientConfig["recipients"] = recipientTypes;
        }
        if (specificEmails != null && specificEmails.Any(e => !string.IsNullOrWhiteSpace(e)))
        {
            recipientConfig["specific_emails"] = specificEmails.Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
        }

        // Build conditions
        var conditions = new Dictionary<string, object>();
        if (ragStatuses != null && ragStatuses.Any())
        {
            conditions["rag_statuses"] = ragStatuses;
        }
        if (projectStatuses != null && projectStatuses.Any())
        {
            conditions["project_statuses"] = projectStatuses;
        }

        rule.Name = name;
        rule.Description = description;
        rule.NotificationTemplateId = notificationTemplateId;
        rule.TriggerCode = triggerCode;
        rule.IsEnabled = isEnabled;
        rule.RecipientConfiguration = recipientConfig.Any() ? JsonSerializer.Serialize(recipientConfig) : null;
        rule.Conditions = conditions.Any() ? JsonSerializer.Serialize(conditions) : null;
        rule.UpdatedAt = DateTime.UtcNow;
        rule.UpdatedBy = userEmail;

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notification rule updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await NotificationRuleExistsAsync(rule.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        ViewBag.Templates = await _context.NotificationTemplates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
        ViewBag.TriggerCodes = GetTriggerCodes();
        ViewBag.RecipientTypes = GetRecipientTypes();
        ViewBag.RagStatuses = GetRagStatuses();

        return View("~/Views/Admin/NotificationRules/Edit.cshtml", rule);
    }

    // GET: Admin/NotificationRules/Delete/5
    [HttpGet("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var rule = await _context.NotificationRules
            .Include(r => r.NotificationTemplate)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/NotificationRules/Delete.cshtml", rule);
    }

    // POST: Admin/NotificationRules/Delete/5
    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await IsAuthorizedAsync())
        {
            return Forbid();
        }

        var rule = await _context.NotificationRules.FindAsync(id);
        if (rule != null)
        {
            _context.NotificationRules.Remove(rule);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification rule deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> NotificationRuleExistsAsync(int id)
    {
        return await _context.NotificationRules.AnyAsync(e => e.Id == id);
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

    private List<string> GetRecipientTypes()
    {
        return new List<string>
        {
            "team_member",
            "added_team_member",
            "sro",
            "senior_responsible_officer",
            "primary_contact",
            "team",
            "all"
        };
    }

    private List<string> GetRagStatuses()
    {
        return new List<string>
        {
            "Green",
            "Amber-Green",
            "Amber",
            "Amber-Red",
            "Red"
        };
    }

    private List<string> ParseRecipientConfiguration(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("recipients", out var recipients))
            {
                return recipients.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }
        catch { }

        return new List<string>();
    }

    private List<string> ParseSpecificEmails(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("specific_emails", out var emails))
            {
                return emails.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }
        catch { }

        return new List<string>();
    }

    private List<string> ParseRagStatuses(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("rag_statuses", out var statuses))
            {
                return statuses.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }
        catch { }

        return new List<string>();
    }

    private List<string> ParseProjectStatuses(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("project_statuses", out var statuses))
            {
                return statuses.EnumerateArray()
                    .Select(e => e.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }
        }
        catch { }

        return new List<string>();
    }
}
