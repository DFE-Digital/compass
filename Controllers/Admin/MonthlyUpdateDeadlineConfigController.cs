using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers.Admin
{
    [Route("Admin/[controller]")]
    [Authorize]
    public class MonthlyUpdateDeadlineConfigController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<MonthlyUpdateDeadlineConfigController> _logger;
        private readonly IPermissionService _permissionService;

        public MonthlyUpdateDeadlineConfigController(
            CompassDbContext context,
            ILogger<MonthlyUpdateDeadlineConfigController> logger,
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

        /// <summary>
        /// Keeps legacy SQL columns populated so inserts/updates succeed on databases that still have WorkingDayDeadline and DueDayRule.
        /// </summary>
        private static void SyncLegacyDeadlineColumns(MonthlyUpdateDeadlineConfig model)
        {
            model.WorkingDayDeadline = model.DueCalendarDay;
            model.DueDayRule = 0;
        }

        // ========================================
        // INDEX - List all configurations
        // ========================================

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var configs = await _context.MonthlyUpdateDeadlineConfigs
                .OrderByDescending(c => c.EffectiveFrom)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Index.cshtml", configs);
        }

        // ========================================
        // CREATE
        // ========================================

        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Create.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MonthlyUpdateDeadlineConfig model)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Check for overlapping effective periods
                var overlapping = await _context.MonthlyUpdateDeadlineConfigs
                    .Where(c => c.IsActive &&
                               ((c.EffectiveUntil == null || c.EffectiveUntil >= model.EffectiveFrom) &&
                                (model.EffectiveUntil == null || c.EffectiveFrom <= model.EffectiveUntil)))
                    .FirstOrDefaultAsync();

                if (overlapping != null)
                {
                    var overlapStart = overlapping.EffectiveFrom.ToString("MMMM yyyy");
                    var overlapEnd = overlapping.EffectiveUntil?.ToString("MMMM yyyy") ?? "indefinite";
                    ModelState.AddModelError("", $"This configuration overlaps with an existing active configuration (Effective: {overlapStart} to {overlapEnd}). Please adjust the dates or deactivate the existing configuration.");
                    return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Create.cshtml", model);
                }

                model.CreatedBy = GetUserEmail();
                model.UpdatedBy = GetUserEmail();
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                SyncLegacyDeadlineColumns(model);

                _context.MonthlyUpdateDeadlineConfigs.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created MonthlyUpdateDeadlineConfig {Id} (DueMonthOffset: {Offset}, DueCalendarDay: {CalDay}, CommissionDays: {CommissionDays}, EffectiveFrom: {From}) by {User}", 
                    model.Id, model.DueMonthOffset, model.DueCalendarDay, model.CommissionDaysBeforeMonthEnd, model.EffectiveFrom, GetUserEmail());

                TempData["SuccessMessage"] = $"Successfully created monthly update deadline configuration '{model.Name}'.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Create.cshtml", model);
        }

        // ========================================
        // EDIT
        // ========================================

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var config = await _context.MonthlyUpdateDeadlineConfigs.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Edit.cshtml", config);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MonthlyUpdateDeadlineConfig model)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.MonthlyUpdateDeadlineConfigs.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    // Check for overlapping effective periods (excluding current record)
                    var overlapping = await _context.MonthlyUpdateDeadlineConfigs
                        .Where(c => c.Id != id &&
                                   c.IsActive &&
                                   ((c.EffectiveUntil == null || c.EffectiveUntil >= model.EffectiveFrom) &&
                                    (model.EffectiveUntil == null || c.EffectiveFrom <= model.EffectiveUntil)))
                        .FirstOrDefaultAsync();

                    if (overlapping != null)
                    {
                        var overlapStart = overlapping.EffectiveFrom.ToString("MMMM yyyy");
                        var overlapEnd = overlapping.EffectiveUntil?.ToString("MMMM yyyy") ?? "indefinite";
                        ModelState.AddModelError("", $"This configuration overlaps with an existing active configuration (Effective: {overlapStart} to {overlapEnd}). Please adjust the dates or deactivate the existing configuration.");
                        return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Edit.cshtml", model);
                    }

                    existing.Name = model.Name;
                    existing.DueMonthOffset = model.DueMonthOffset;
                    existing.DueCalendarDay = model.DueCalendarDay;
                    existing.CommissionDaysBeforeMonthEnd = model.CommissionDaysBeforeMonthEnd;
                    existing.EffectiveFrom = model.EffectiveFrom;
                    existing.EffectiveUntil = model.EffectiveUntil;
                    existing.IsActive = model.IsActive;
                    existing.IsDefault = model.IsDefault;
                    existing.Notes = model.Notes;
                    existing.UpdatedBy = GetUserEmail();
                    existing.UpdatedAt = DateTime.UtcNow;

                    SyncLegacyDeadlineColumns(existing);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated MonthlyUpdateDeadlineConfig {Id} by {User}", id, GetUserEmail());

                    TempData["SuccessMessage"] = "Successfully updated monthly update deadline configuration.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.MonthlyUpdateDeadlineConfigs.AnyAsync(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View("~/Views/Admin/MonthlyUpdateDeadlineConfig/Edit.cshtml", model);
        }

        // ========================================
        // DELETE
        // ========================================

        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var config = await _context.MonthlyUpdateDeadlineConfigs.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            _context.MonthlyUpdateDeadlineConfigs.Remove(config);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted MonthlyUpdateDeadlineConfig {Id} by {User}", id, GetUserEmail());

            TempData["SuccessMessage"] = "Successfully deleted monthly update deadline configuration.";
            return RedirectToAction(nameof(Index));
        }
    }
}
