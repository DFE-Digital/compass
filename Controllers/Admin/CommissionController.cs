using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers.Admin
{
    [Route("Admin/Commission")]
    [Authorize]
    public class CommissionController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<CommissionController> _logger;
        private readonly IPermissionService _permissionService;

        public CommissionController(
            CompassDbContext context,
            ILogger<CommissionController> logger,
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

        // GET: Admin/Commission
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var commissions = await _context.Commissions
                .OrderByDescending(c => c.StartDate)
                .ToListAsync();

            return View("~/Views/Admin/Commission/Index.cshtml", commissions);
        }

        // GET: Admin/Commission/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var commission = new Commission
            {
                IsActive = true,
                OpenDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(30),
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(3)
            };

            return View("~/Views/Admin/Commission/Create.cshtml", commission);
        }

        // POST: Admin/Commission/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Commission model, string? quarter)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date.");
                    return View("~/Views/Admin/Commission/Create.cshtml", model);
                }

                if (model.OpenDate > model.DueDate)
                {
                    ModelState.AddModelError("DueDate", "Due date must be after or equal to open date.");
                    return View("~/Views/Admin/Commission/Create.cshtml", model);
                }

                // Set quarter if provided
                if (!string.IsNullOrWhiteSpace(quarter))
                {
                    model.Quarter = quarter;
                }

                var userEmail = GetUserEmail();
                model.CreatedBy = userEmail;
                model.UpdatedBy = userEmail;
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                _context.Commissions.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created commission {Id} ({Name}) by {User}", 
                    model.Id, model.Name, userEmail);

                TempData["SuccessMessage"] = $"Successfully created commission '{model.Name}'.";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/Commission/Create.cshtml", model);
        }

        // GET: Admin/Commission/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var commission = await _context.Commissions.FindAsync(id);
            if (commission == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Commission/Edit.cshtml", commission);
        }

        // POST: Admin/Commission/Edit/{id}
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Commission model, string? quarter)
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
                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after start date.");
                    return View("~/Views/Admin/Commission/Edit.cshtml", model);
                }

                if (model.OpenDate > model.DueDate)
                {
                    ModelState.AddModelError("DueDate", "Due date must be after or equal to open date.");
                    return View("~/Views/Admin/Commission/Edit.cshtml", model);
                }

                try
                {
                    var existing = await _context.Commissions.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    existing.Name = model.Name;
                    existing.Description = model.Description;
                    existing.StartDate = model.StartDate;
                    existing.EndDate = model.EndDate;
                    existing.Quarter = !string.IsNullOrWhiteSpace(quarter) ? quarter : null;
                    existing.OpenDate = model.OpenDate;
                    existing.DueDate = model.DueDate;
                    existing.IsActive = model.IsActive;
                    existing.UpdatedBy = GetUserEmail();
                    existing.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Updated commission {Id} by {User}", id, GetUserEmail());

                    TempData["SuccessMessage"] = "Successfully updated commission.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating commission {Id}", id);
                    ModelState.AddModelError("", "An error occurred while updating the commission.");
                }
            }

            return View("~/Views/Admin/Commission/Edit.cshtml", model);
        }

        // POST: Admin/Commission/Delete/{id}
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            try
            {
                var commission = await _context.Commissions
                    .Include(c => c.Submissions)
                    .FirstOrDefaultAsync(c => c.Id == id);
                
                if (commission == null)
                {
                    return NotFound();
                }

                // Check if there are any submissions
                if (commission.Submissions.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete commission that has submissions. Deactivate it instead.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Commissions.Remove(commission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted commission {Id} by {User}", id, GetUserEmail());

                TempData["SuccessMessage"] = "Successfully deleted commission.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting commission {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the commission.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
