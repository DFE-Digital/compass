using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers.Admin
{
    [Route("Admin/DivisionBusinessAreaUser")]
    [Authorize]
    public class DivisionBusinessAreaUserController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<DivisionBusinessAreaUserController> _logger;
        private readonly IPermissionService _permissionService;
        private readonly IUserDirectoryService _userDirectoryService;

        public DivisionBusinessAreaUserController(
            CompassDbContext context,
            ILogger<DivisionBusinessAreaUserController> logger,
            IPermissionService permissionService,
            IUserDirectoryService userDirectoryService)
        {
            _context = context;
            _logger = logger;
            _permissionService = permissionService;
            _userDirectoryService = userDirectoryService;
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

        // GET: Admin/DivisionBusinessAreaUser
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var divisions = await _context.Divisions
                .Include(d => d.DivisionUsers)
                    .ThenInclude(du => du.User)
                .Include(d => d.DivisionBusinessAreas)
                    .ThenInclude(dba => dba.BusinessAreaLookup)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Name)
                .ToListAsync();

            var businessAreas = await _context.BusinessAreaLookups
                .Include(ba => ba.BusinessAreaUsers)
                    .ThenInclude(bau => bau.User)
                .Include(ba => ba.DivisionBusinessAreas)
                    .ThenInclude(dba => dba.Division)
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .ToListAsync();

            ViewBag.Divisions = divisions;
            ViewBag.BusinessAreas = businessAreas;

            return View("~/Views/Admin/DivisionBusinessAreaUser/Index.cshtml");
        }

        // GET: Admin/DivisionBusinessAreaUser/ManageBusinessAreaUsers/{id}
        [HttpGet("ManageBusinessAreaUsers/{id}")]
        public async Task<IActionResult> ManageBusinessAreaUsers(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var businessArea = await _context.BusinessAreaLookups
                .Include(ba => ba.BusinessAreaUsers)
                    .ThenInclude(bau => bau.User)
                .FirstOrDefaultAsync(ba => ba.Id == id);

            if (businessArea == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/DivisionBusinessAreaUser/ManageBusinessAreaUsers.cshtml", businessArea);
        }

        // POST: Admin/DivisionBusinessAreaUser/AddUserToBusinessArea
        [HttpPost("AddUserToBusinessArea")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToBusinessArea(int businessAreaId, string? entraUserObjectId, string? entraUserEmail, string? entraUserName)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var businessArea = await _context.BusinessAreaLookups
                .Include(ba => ba.BusinessAreaUsers)
                .FirstOrDefaultAsync(ba => ba.Id == businessAreaId);

            if (businessArea == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(entraUserObjectId) || string.IsNullOrWhiteSpace(entraUserEmail))
            {
                TempData["ErrorMessage"] = "Please select a user from Entra ID.";
                return RedirectToAction(nameof(ManageBusinessAreaUsers), new { id = businessAreaId });
            }

            try
            {
                User? user = null;

                // Try to get or create user using Entra Object ID
                if (Guid.TryParse(entraUserObjectId, out var objectIdGuid))
                {
                    try
                    {
                        user = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                        _logger.LogInformation("Ensured user exists via UserDirectoryService: ObjectId={ObjectId}, Email={Email}, Name={Name}", 
                            entraUserObjectId, user.Email, user.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to ensure user with ObjectId {ObjectId}, trying email lookup", entraUserObjectId);
                    }
                }

                // Fallback: try to find user by email if EnsureUserAsync failed
                if (user == null && !string.IsNullOrWhiteSpace(entraUserEmail))
                {
                    var userEmailLower = entraUserEmail.ToLowerInvariant().Trim();
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmailLower);
                }

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Could not find or create user. Please try again.";
                    return RedirectToAction(nameof(ManageBusinessAreaUsers), new { id = businessAreaId });
                }

                // Check if user is already assigned to this business area
                var existingAssignment = await _context.BusinessAreaUsers
                    .FirstOrDefaultAsync(bau => bau.BusinessAreaLookupId == businessAreaId && bau.UserId == user.Id);

                if (existingAssignment != null)
                {
                    TempData["ErrorMessage"] = $"{user.Name} is already assigned to this business area.";
                    return RedirectToAction(nameof(ManageBusinessAreaUsers), new { id = businessAreaId });
                }

                // Create new assignment
                var businessAreaUser = new BusinessAreaUser
                {
                    BusinessAreaLookupId = businessAreaId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.BusinessAreaUsers.Add(businessAreaUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{user.Name} has been added to {businessArea.Name}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to business area");
                TempData["ErrorMessage"] = "An error occurred while adding the user. Please try again.";
            }

            return RedirectToAction(nameof(ManageBusinessAreaUsers), new { id = businessAreaId });
        }

        // POST: Admin/DivisionBusinessAreaUser/RemoveUserFromBusinessArea
        [HttpPost("RemoveUserFromBusinessArea")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromBusinessArea(int businessAreaId, int userId)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var businessAreaUser = await _context.BusinessAreaUsers
                .Include(bau => bau.User)
                .Include(bau => bau.BusinessAreaLookup)
                .FirstOrDefaultAsync(bau => bau.BusinessAreaLookupId == businessAreaId && bau.UserId == userId);

            if (businessAreaUser == null)
            {
                return NotFound();
            }

            try
            {
                var userName = businessAreaUser.User.Name;
                var businessAreaName = businessAreaUser.BusinessAreaLookup.Name;

                _context.BusinessAreaUsers.Remove(businessAreaUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{userName} has been removed from {businessAreaName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from business area");
                TempData["ErrorMessage"] = "An error occurred while removing the user. Please try again.";
            }

            return RedirectToAction(nameof(ManageBusinessAreaUsers), new { id = businessAreaId });
        }

        // GET: Admin/DivisionBusinessAreaUser/ManageDivisionUsers/{id}
        [HttpGet("ManageDivisionUsers/{id}")]
        public async Task<IActionResult> ManageDivisionUsers(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var division = await _context.Divisions
                .Include(d => d.DivisionUsers)
                    .ThenInclude(du => du.User)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (division == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/DivisionBusinessAreaUser/ManageDivisionUsers.cshtml", division);
        }

        // POST: Admin/DivisionBusinessAreaUser/AddUserToDivision
        [HttpPost("AddUserToDivision")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToDivision(int divisionId, string? entraUserObjectId, string? entraUserEmail, string? entraUserName)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var division = await _context.Divisions
                .Include(d => d.DivisionUsers)
                .FirstOrDefaultAsync(d => d.Id == divisionId);

            if (division == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(entraUserObjectId) || string.IsNullOrWhiteSpace(entraUserEmail))
            {
                TempData["ErrorMessage"] = "Please select a user from Entra ID.";
                return RedirectToAction(nameof(ManageDivisionUsers), new { id = divisionId });
            }

            try
            {
                User? user = null;

                // Try to get or create user using Entra Object ID
                if (Guid.TryParse(entraUserObjectId, out var objectIdGuid))
                {
                    try
                    {
                        user = await _userDirectoryService.EnsureUserAsync(objectIdGuid);
                        _logger.LogInformation("Ensured user exists via UserDirectoryService: ObjectId={ObjectId}, Email={Email}, Name={Name}", 
                            entraUserObjectId, user.Email, user.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to ensure user with ObjectId {ObjectId}, trying email lookup", entraUserObjectId);
                    }
                }

                // Fallback: try to find user by email if EnsureUserAsync failed
                if (user == null && !string.IsNullOrWhiteSpace(entraUserEmail))
                {
                    var userEmailLower = entraUserEmail.ToLowerInvariant().Trim();
                    user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmailLower);
                }

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Could not find or create user. Please try again.";
                    return RedirectToAction(nameof(ManageDivisionUsers), new { id = divisionId });
                }

                // Check if user is already assigned to this division
                var existingAssignment = await _context.DivisionUsers
                    .FirstOrDefaultAsync(du => du.DivisionId == divisionId && du.UserId == user.Id);

                if (existingAssignment != null)
                {
                    TempData["ErrorMessage"] = $"{user.Name} is already assigned to this division.";
                    return RedirectToAction(nameof(ManageDivisionUsers), new { id = divisionId });
                }

                // Create new assignment
                var divisionUser = new DivisionUser
                {
                    DivisionId = divisionId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DivisionUsers.Add(divisionUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{user.Name} has been added to {division.Name}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user to division");
                TempData["ErrorMessage"] = "An error occurred while adding the user. Please try again.";
            }

            return RedirectToAction(nameof(ManageDivisionUsers), new { id = divisionId });
        }

        // POST: Admin/DivisionBusinessAreaUser/RemoveUserFromDivision
        [HttpPost("RemoveUserFromDivision")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromDivision(int divisionId, int userId)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var divisionUser = await _context.DivisionUsers
                .Include(du => du.User)
                .Include(du => du.Division)
                .FirstOrDefaultAsync(du => du.DivisionId == divisionId && du.UserId == userId);

            if (divisionUser == null)
            {
                return NotFound();
            }

            try
            {
                var userName = divisionUser.User.Name;
                var divisionName = divisionUser.Division.Name;

                _context.DivisionUsers.Remove(divisionUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{userName} has been removed from {divisionName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing user from division");
                TempData["ErrorMessage"] = "An error occurred while removing the user. Please try again.";
            }

            return RedirectToAction(nameof(ManageDivisionUsers), new { id = divisionId });
        }

        // GET: Admin/DivisionBusinessAreaUser/CreateDivision
        [HttpGet("CreateDivision")]
        public async Task<IActionResult> CreateDivision()
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            return View("~/Views/Admin/DivisionBusinessAreaUser/CreateDivision.cshtml");
        }

        // POST: Admin/DivisionBusinessAreaUser/CreateDivision
        [HttpPost("CreateDivision")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDivision([Bind("Name,Description,SortOrder,IsActive")] Division division)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if name already exists
                    if (await _context.Divisions.AnyAsync(d => d.Name == division.Name))
                    {
                        TempData["ErrorMessage"] = "A division with this name already exists.";
                        return View("~/Views/Admin/DivisionBusinessAreaUser/CreateDivision.cshtml", division);
                    }

                    division.CreatedAt = DateTime.UtcNow;
                    division.UpdatedAt = DateTime.UtcNow;
                    _context.Add(division);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Division '{division.Name}' has been created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating division");
                    TempData["ErrorMessage"] = "An error occurred while creating the division. Please try again.";
                }
            }

            return View("~/Views/Admin/DivisionBusinessAreaUser/CreateDivision.cshtml", division);
        }

        // GET: Admin/DivisionBusinessAreaUser/EditDivision/{id}
        [HttpGet("EditDivision/{id}")]
        public async Task<IActionResult> EditDivision(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var division = await _context.Divisions.FindAsync(id);
            if (division == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/DivisionBusinessAreaUser/EditDivision.cshtml", division);
        }

        // POST: Admin/DivisionBusinessAreaUser/EditDivision
        [HttpPost("EditDivision")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDivision(int id, [Bind("Id,Name,Description,SortOrder,IsActive")] Division division)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            if (id != division.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if name already exists for a different record
                    if (await _context.Divisions.AnyAsync(d => d.Name == division.Name && d.Id != id))
                    {
                        TempData["ErrorMessage"] = "A division with this name already exists.";
                        return View("~/Views/Admin/DivisionBusinessAreaUser/EditDivision.cshtml", division);
                    }

                    var existingDivision = await _context.Divisions.FindAsync(id);
                    if (existingDivision == null)
                    {
                        return NotFound();
                    }

                    existingDivision.Name = division.Name;
                    existingDivision.Description = division.Description;
                    existingDivision.SortOrder = division.SortOrder;
                    existingDivision.IsActive = division.IsActive;
                    existingDivision.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Division '{division.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating division");
                    TempData["ErrorMessage"] = "An error occurred while updating the division. Please try again.";
                }
            }

            return View("~/Views/Admin/DivisionBusinessAreaUser/EditDivision.cshtml", division);
        }

        // GET: Admin/DivisionBusinessAreaUser/ManageDivisionBusinessAreas/{id}
        [HttpGet("ManageDivisionBusinessAreas/{id}")]
        public async Task<IActionResult> ManageDivisionBusinessAreas(int id)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var division = await _context.Divisions
                .Include(d => d.DivisionBusinessAreas)
                    .ThenInclude(dba => dba.BusinessAreaLookup)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (division == null)
            {
                return NotFound();
            }

            var allBusinessAreas = await _context.BusinessAreaLookups
                .Where(ba => ba.IsActive)
                .OrderBy(ba => ba.SortOrder)
                .ThenBy(ba => ba.Name)
                .ToListAsync();

            ViewBag.AllBusinessAreas = allBusinessAreas;

            return View("~/Views/Admin/DivisionBusinessAreaUser/ManageDivisionBusinessAreas.cshtml", division);
        }

        // POST: Admin/DivisionBusinessAreaUser/AddBusinessAreaToDivision
        [HttpPost("AddBusinessAreaToDivision")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBusinessAreaToDivision(int divisionId, int businessAreaId)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var division = await _context.Divisions
                .Include(d => d.DivisionBusinessAreas)
                .FirstOrDefaultAsync(d => d.Id == divisionId);

            if (division == null)
            {
                return NotFound();
            }

            var businessArea = await _context.BusinessAreaLookups.FindAsync(businessAreaId);
            if (businessArea == null)
            {
                return NotFound();
            }

            // Check if business area is already assigned to this division
            var existingAssignment = await _context.DivisionBusinessAreas
                .FirstOrDefaultAsync(dba => dba.DivisionId == divisionId && dba.BusinessAreaLookupId == businessAreaId);

            if (existingAssignment != null)
            {
                TempData["ErrorMessage"] = $"{businessArea.Name} is already assigned to {division.Name}.";
                return RedirectToAction(nameof(ManageDivisionBusinessAreas), new { id = divisionId });
            }

            try
            {
                var divisionBusinessArea = new DivisionBusinessArea
                {
                    DivisionId = divisionId,
                    BusinessAreaLookupId = businessAreaId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.DivisionBusinessAreas.Add(divisionBusinessArea);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{businessArea.Name} has been added to {division.Name}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding business area to division");
                TempData["ErrorMessage"] = "An error occurred while adding the business area. Please try again.";
            }

            return RedirectToAction(nameof(ManageDivisionBusinessAreas), new { id = divisionId });
        }

        // POST: Admin/DivisionBusinessAreaUser/RemoveBusinessAreaFromDivision
        [HttpPost("RemoveBusinessAreaFromDivision")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveBusinessAreaFromDivision(int divisionId, int businessAreaId)
        {
            if (!await IsAuthorizedAsync())
            {
                return Forbid();
            }

            var divisionBusinessArea = await _context.DivisionBusinessAreas
                .Include(dba => dba.Division)
                .Include(dba => dba.BusinessAreaLookup)
                .FirstOrDefaultAsync(dba => dba.DivisionId == divisionId && dba.BusinessAreaLookupId == businessAreaId);

            if (divisionBusinessArea == null)
            {
                return NotFound();
            }

            try
            {
                var businessAreaName = divisionBusinessArea.BusinessAreaLookup.Name;
                var divisionName = divisionBusinessArea.Division.Name;

                _context.DivisionBusinessAreas.Remove(divisionBusinessArea);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{businessAreaName} has been removed from {divisionName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing business area from division");
                TempData["ErrorMessage"] = "An error occurred while removing the business area. Please try again.";
            }

            return RedirectToAction(nameof(ManageDivisionBusinessAreas), new { id = divisionId });
        }
    }
}
