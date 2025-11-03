using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using System.Security.Claims;

namespace Compass.Controllers;

[Authorize]
public class StaffRoleReturnController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<StaffRoleReturnController> _logger;

    public StaffRoleReturnController(CompassDbContext context, ILogger<StaffRoleReturnController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: StaffRoleReturn/Index
    public async Task<IActionResult> Index()
    {
        var currentUserEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUserEmail))
        {
            TempData["ErrorMessage"] = "Unable to identify current user.";
            return RedirectToAction("Index", "Home");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found in system.";
            return RedirectToAction("Index", "Home");
        }

        // Get current year for reporting period (1 April to 31 March)
        // Year stored in database represents the YEAR of the due date (31 March)
        // First return period Apr 2024-Mar 2025 -> due 31 Mar 2026, stored as year 2026
        // Second return period Apr 2025-Mar 2026 -> due 31 Mar 2027, stored as year 2027
        var today = DateTime.Today;
        
        // We need to determine the next due date
        // If we're in Nov 2024: next due is Mar 2026
        // If we're in May 2025: next due is Mar 2026 (for period Apr 2024-Mar 2025)
        // If we're in May 2026: next due is Mar 2027 (for period Apr 2025-Mar 2026)
        var currentYear = 2026; // First return is always due Mar 31, 2026
        
        // If we're past Mar 31, 2026, move to the next return period
        var nextDue2026 = new DateTime(2026, 3, 31);
        if (today > nextDue2026)
        {
            // We're past the first due date, so next one is Mar 2027
            currentYear = 2027;
        }
        
        var dueDate = new DateTime(currentYear, 3, 31);
        
        // Check if return exists for this year
        var existingReturn = await _context.StaffRoleReturns
            .Include(srr => srr.GddRole)
            .Include(srr => srr.SecondarySkills)
                .ThenInclude(srs => srs.Skill)
            .FirstOrDefaultAsync(srr => srr.UserId == user.Id && srr.Year == currentYear);

        // Get all available GDD Roles and Skills for dropdowns
        ViewBag.GddRoles = await _context.GddRoles
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoleFamily)
            .ThenBy(r => r.DisplayName)
            .ToListAsync();
        
        ViewBag.Skills = await _context.Skills
            .Where(s => s.IsActive)
            .OrderBy(s => s.SkillName)
            .ToListAsync();

        ViewBag.CurrentUser = user;
        ViewBag.CurrentYear = currentYear;
        ViewBag.DueDate = dueDate;
        ViewBag.ExistingReturn = existingReturn;
        ViewBag.IsOverdue = dueDate < DateTime.Today;

        return View();
    }

    // POST: StaffRoleReturn/CreateOrUpdate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrUpdate(StaffRoleReturn staffRoleReturn, List<int> secondarySkillIds)
    {
        var currentUserEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUserEmail))
        {
            TempData["ErrorMessage"] = "Unable to identify current user.";
            return RedirectToAction("Index", "StaffRoleReturn");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found in system.";
            return RedirectToAction("Index", "StaffRoleReturn");
        }

        staffRoleReturn.UserId = user.Id;
        
        // Limit secondary skills to 5
        if (secondarySkillIds != null && secondarySkillIds.Count > 5)
        {
            ModelState.AddModelError("SecondarySkills", "You can select up to 5 secondary skills.");
            secondarySkillIds = secondarySkillIds.Take(5).ToList();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingReturn = await _context.StaffRoleReturns
                    .Include(srr => srr.SecondarySkills)
                    .FirstOrDefaultAsync(srr => srr.UserId == user.Id && srr.Year == staffRoleReturn.Year);

                if (existingReturn != null)
                {
                    // Update existing return - always set to submitted
                    existingReturn.GddRoleId = staffRoleReturn.GddRoleId;
                    existingReturn.Grade = staffRoleReturn.Grade;
                    existingReturn.Status = ReturnStatus.Submitted;
                    existingReturn.LastModifiedDate = DateTime.UtcNow;
                    existingReturn.UpdatedAt = DateTime.UtcNow;
                    
                    if (!existingReturn.SubmittedDate.HasValue)
                    {
                        existingReturn.SubmittedDate = DateTime.UtcNow;
                    }

                    // Remove existing secondary skills
                    _context.StaffRoleReturnSkills.RemoveRange(existingReturn.SecondarySkills);

                    // Add new secondary skills
                    if (secondarySkillIds != null && secondarySkillIds.Any())
                    {
                        for (int i = 0; i < secondarySkillIds.Count; i++)
                        {
                            existingReturn.SecondarySkills.Add(new StaffRoleReturnSkill
                            {
                                SkillId = secondarySkillIds[i],
                                DisplayOrder = i + 1
                            });
                        }
                    }

                    _context.Update(existingReturn);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Your staff role return has been updated successfully.";
                    _logger.LogInformation("Staff role return updated for user {UserId}, year {Year}", user.Id, staffRoleReturn.Year);
                }
                else
                {
                    // Create new return - always set to submitted
                    var newReturn = new StaffRoleReturn
                    {
                        UserId = user.Id,
                        Year = staffRoleReturn.Year,
                        GddRoleId = staffRoleReturn.GddRoleId,
                        Grade = staffRoleReturn.Grade,
                        Status = ReturnStatus.Submitted,
                        LastModifiedDate = DateTime.UtcNow,
                        SubmittedDate = DateTime.UtcNow,
                        DueDate = new DateTime(staffRoleReturn.Year, 3, 31)
                    };

                    _context.StaffRoleReturns.Add(newReturn);
                    await _context.SaveChangesAsync();

                    // Add secondary skills
                    if (secondarySkillIds != null && secondarySkillIds.Any())
                    {
                        for (int i = 0; i < secondarySkillIds.Count; i++)
                        {
                            _context.StaffRoleReturnSkills.Add(new StaffRoleReturnSkill
                            {
                                StaffRoleReturnId = newReturn.Id,
                                SkillId = secondarySkillIds[i],
                                DisplayOrder = i + 1
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Your staff role return has been submitted successfully.";
                    _logger.LogInformation("Staff role return created for user {UserId}, year {Year}", user.Id, staffRoleReturn.Year);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating staff role return");
                TempData["ErrorMessage"] = "An error occurred while saving your staff role return. Please try again.";
            }
        }

        // If we get here, reload data for the view
        ViewBag.GddRoles = await _context.GddRoles
            .Where(r => r.IsActive)
            .OrderBy(r => r.RoleFamily)
            .ThenBy(r => r.DisplayName)
            .ToListAsync();
        
        ViewBag.Skills = await _context.Skills
            .Where(s => s.IsActive)
            .OrderBy(s => s.SkillName)
            .ToListAsync();
        
        ViewBag.CurrentUser = user;
        ViewBag.CurrentYear = staffRoleReturn.Year;
        ViewBag.DueDate = new DateTime(staffRoleReturn.Year, 3, 31);

        return View("Index", staffRoleReturn);
    }

    // GET: StaffRoleReturn/Admin/Overdue
    public async Task<IActionResult> Overdue()
    {
        // Check if user is admin
        if (!await IsAdminAsync())
        {
            TempData["ErrorMessage"] = "You do not have permission to access this page.";
            return RedirectToAction("Index", "Home");
        }

        // Get current year for reporting period (1 April to 31 March)
        var today = DateTime.Today;
        var currentYear = 2026; // First return is always due Mar 31, 2026
        
        // If we're past Mar 31, 2026, move to the next return period
        var nextDue2026 = new DateTime(2026, 3, 31);
        if (today > nextDue2026)
        {
            currentYear = 2027;
        }
        
        var dueDate = new DateTime(currentYear, 3, 31);

        // Get all users who haven't submitted or have draft returns
        var allUsers = await _context.Users.ToListAsync();
        var usersWithReturns = await _context.StaffRoleReturns
            .Where(srr => srr.Year == currentYear && srr.Status == ReturnStatus.Submitted)
            .Select(srr => srr.UserId)
            .ToListAsync();

        var overdueUsers = allUsers.Where(u => !usersWithReturns.Contains(u.Id)).ToList();

        ViewBag.CurrentYear = currentYear;
        ViewBag.DueDate = dueDate;
        ViewBag.IsOverdue = dueDate < DateTime.Today;
        ViewBag.OverdueUsers = overdueUsers;

        return View();
    }

    private async Task<bool> IsAdminAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            return false;
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        return user != null && (user.Role == UserRole.Admin || user.Role == UserRole.SuperAdmin);
    }
}

