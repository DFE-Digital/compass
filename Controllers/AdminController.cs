using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Authorization;
using Compass.Services;

namespace Compass.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IApiTokenService _apiTokenService;

    public AdminController(CompassDbContext context, ILogger<AdminController> logger, IApiTokenService apiTokenService)
    {
        _context = context;
        _logger = logger;
        _apiTokenService = apiTokenService;
    }

    // GET: Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/User/Users.cshtml", users);
    }

    // GET: Admin/CreateUser
    public IActionResult CreateUser()
    {
        return View("~/Views/Admin/User/CreateUser.cshtml");
    }

    // POST: Admin/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(User user)
    {
        if (ModelState.IsValid)
        {
            try
            {
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"User '{user.Name}' has been created successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
            }
        }
        
        return View("~/Views/Admin/User/CreateUser.cshtml", user);
    }

    // GET: Admin/EditUser/5
    public async Task<IActionResult> EditUser(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/User/EditUser.cshtml", user);
    }

    // POST: Admin/EditUser/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"User '{user.Name}' has been updated successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ModelState.AddModelError("", "An error occurred while updating the user. Please try again.");
            }
        }
        
        return View("~/Views/Admin/User/EditUser.cshtml", user);
    }

    // GET: Admin/DeleteUser/5
    public async Task<IActionResult> DeleteUser(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/User/DeleteUser.cshtml", user);
    }

    // POST: Admin/DeleteUser/5
    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"User '{user.Name}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            TempData["ErrorMessage"] = "An error occurred while deleting the user. Please try again.";
        }

        return RedirectToAction(nameof(Users));
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

    // ==================== STRATEGIC OBJECTIVES ====================

    // GET: Admin/Objectives
    public async Task<IActionResult> Objectives()
    {
        var objectives = await _context.Objectives
            .Include(o => o.OwnerUser)
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        
        return View("~/Views/Admin/Objective/Index.cshtml", objectives);
    }

    // GET: Admin/ObjectiveDetails/5
    public async Task<IActionResult> ObjectiveDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var objective = await _context.Objectives
            .Include(o => o.OwnerUser)
            .Include(o => o.Risks.Where(r => !r.IsDeleted))
            .Include(o => o.Issues.Where(i => !i.IsDeleted))
            .Include(o => o.Milestones.Where(m => !m.IsDeleted))
            .Include(o => o.Actions.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (objective == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Objective/Details.cshtml", objective);
    }

    // GET: Admin/CreateObjective
    public async Task<IActionResult> CreateObjective()
    {
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        return View("~/Views/Admin/Objective/Create.cshtml");
    }

    // POST: Admin/CreateObjective
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateObjective([Bind("Title,Theme,Description,OwnerUserId,Status")] Objective objective)
    {
        if (ModelState.IsValid)
        {
            try
            {
                objective.CreatedAt = DateTime.UtcNow;
                objective.UpdatedAt = DateTime.UtcNow;
                objective.IsDeleted = false;
                
                _context.Add(objective);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Strategic objective '{objective.Title}' has been created successfully.";
                return RedirectToAction(nameof(Objectives));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating objective");
                ModelState.AddModelError("", "An error occurred while creating the objective. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", objective.OwnerUserId);
        return View("~/Views/Admin/Objective/Create.cshtml", objective);
    }

    // GET: Admin/EditObjective/5
    public async Task<IActionResult> EditObjective(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var objective = await _context.Objectives.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (objective == null)
        {
            return NotFound();
        }

        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", objective.OwnerUserId);
        return View("~/Views/Admin/Objective/Edit.cshtml", objective);
    }

    // POST: Admin/EditObjective/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditObjective(int id, [Bind("Id,Title,Theme,Description,OwnerUserId,Status")] Objective objective)
    {
        if (id != objective.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingObjective = await _context.Objectives.FindAsync(id);
                if (existingObjective == null || existingObjective.IsDeleted)
                {
                    return NotFound();
                }

                existingObjective.Title = objective.Title;
                existingObjective.Theme = objective.Theme;
                existingObjective.Description = objective.Description;
                existingObjective.OwnerUserId = objective.OwnerUserId;
                existingObjective.Status = objective.Status;
                existingObjective.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Strategic objective '{objective.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Objectives));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ObjectiveExists(objective.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating objective");
                ModelState.AddModelError("", "An error occurred while updating the objective. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name", objective.OwnerUserId);
        return View("~/Views/Admin/Objective/Edit.cshtml", objective);
    }

    // GET: Admin/DeleteObjective/5
    public async Task<IActionResult> DeleteObjective(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var objective = await _context.Objectives
            .Include(o => o.OwnerUser)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (objective == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Objective/Delete.cshtml", objective);
    }

    // POST: Admin/DeleteObjective/5
    [HttpPost, ActionName("DeleteObjective")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteObjectiveConfirmed(int id)
    {
        try
        {
            var objective = await _context.Objectives
                .Include(o => o.Risks)
                .Include(o => o.Issues)
                .Include(o => o.Milestones)
                .Include(o => o.Actions)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
                
            if (objective == null)
            {
                TempData["ErrorMessage"] = "Strategic objective not found.";
                return RedirectToAction(nameof(Objectives));
            }

            // Check for related items
            var relatedItemsCount = objective.Risks.Count + objective.Issues.Count + 
                                   objective.Milestones.Count + objective.Actions.Count;
                                   
            if (relatedItemsCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{objective.Title}' because it has {relatedItemsCount} related item(s). Please remove or reassign all related items before deleting.";
                return RedirectToAction(nameof(ObjectiveDetails), new { id = id });
            }

            objective.IsDeleted = true;
            objective.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Strategic objective '{objective.Title}' has been deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting objective");
            TempData["ErrorMessage"] = "An error occurred while deleting the objective. Please try again.";
        }
        
        return RedirectToAction(nameof(Objectives));
    }

    private bool ObjectiveExists(int id)
    {
        return _context.Objectives.Any(e => e.Id == id && !e.IsDeleted);
    }

    // ========================================
    // SETTINGS
    // ========================================

    // GET: Admin/Settings
    public async Task<IActionResult> Settings()
    {
        // Load all lookup data for tabbed interface
        ViewBag.RiskTypes = await _context.RiskTypes.OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.RiskTiers = await _context.RiskTiers.OrderBy(rt => rt.SortOrder).ThenBy(rt => rt.Name).ToListAsync();
        ViewBag.ActionSources = await _context.ActionSources.OrderBy(a_s => a_s.SortOrder).ThenBy(a_s => a_s.Name).ToListAsync();
        
        return View("~/Views/Admin/Settings/Index.cshtml");
    }

    // ========================================
    // SETTINGS - Risk Types
    // ========================================

    // GET: Admin/RiskTypes
    public async Task<IActionResult> RiskTypes()
    {
        var riskTypes = await _context.RiskTypes
            .OrderBy(rt => rt.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/RiskTypes.cshtml", riskTypes);
    }

    // GET: Admin/CreateRiskType
    public IActionResult CreateRiskType()
    {
        return View("~/Views/Admin/Settings/CreateRiskType.cshtml");
    }

    // POST: Admin/CreateRiskType
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRiskType([Bind("Code,Name,Description,Summary,IsActive")] RiskType riskType)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists
                if (await _context.RiskTypes.AnyAsync(rt => rt.Code == riskType.Code))
                {
                    ModelState.AddModelError("Code", "A risk type with this code already exists.");
                }
                else
                {
                    riskType.CreatedAt = DateTime.UtcNow;
                    riskType.UpdatedAt = DateTime.UtcNow;
                    _context.Add(riskType);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk type '{riskType.Name}' has been created successfully.";
                    return RedirectToAction(nameof(RiskTypes));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating risk type");
                ModelState.AddModelError("", "An error occurred while creating the risk type. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateRiskType.cshtml", riskType);
    }

    // GET: Admin/EditRiskType/5
    public async Task<IActionResult> EditRiskType(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskType = await _context.RiskTypes.FindAsync(id);
        if (riskType == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditRiskType.cshtml", riskType);
    }

    // POST: Admin/EditRiskType/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRiskType(int id, [Bind("Id,Code,Name,Description,Summary,IsActive")] RiskType riskType)
    {
        if (id != riskType.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists for a different record
                if (await _context.RiskTypes.AnyAsync(rt => rt.Code == riskType.Code && rt.Id != id))
                {
                    ModelState.AddModelError("Code", "A risk type with this code already exists.");
                }
                else
                {
                    var existingRiskType = await _context.RiskTypes.FindAsync(id);
                    if (existingRiskType == null)
                    {
                        return NotFound();
                    }

                    existingRiskType.Code = riskType.Code;
                    existingRiskType.Name = riskType.Name;
                    existingRiskType.Description = riskType.Description;
                    existingRiskType.Summary = riskType.Summary;
                    existingRiskType.IsActive = riskType.IsActive;
                    existingRiskType.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk type '{riskType.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(RiskTypes));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RiskTypeExists(riskType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating risk type");
                ModelState.AddModelError("", "An error occurred while updating the risk type. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/EditRiskType.cshtml", riskType);
    }

    // GET: Admin/DeleteRiskType/5
    public async Task<IActionResult> DeleteRiskType(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskType = await _context.RiskTypes.FindAsync(id);
        if (riskType == null)
        {
            return NotFound();
        }

        // Check if any risks are using this type
        var riskCount = await _context.RiskRiskTypes.CountAsync(rrt => rrt.RiskTypeId == id);
        ViewBag.RiskCount = riskCount;

        return View("~/Views/Admin/Settings/DeleteRiskType.cshtml", riskType);
    }

    // POST: Admin/DeleteRiskType/5
    [HttpPost, ActionName("DeleteRiskType")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRiskTypeConfirmed(int id)
    {
        try
        {
            var riskType = await _context.RiskTypes.FindAsync(id);
            if (riskType != null)
            {
                // Check if any risks are using this type
                var riskCount = await _context.RiskRiskTypes.CountAsync(rrt => rrt.RiskTypeId == id);
                if (riskCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete risk type '{riskType.Name}' as it is being used by {riskCount} risk(s). Please reassign those risks first.";
                }
                else
                {
                    _context.RiskTypes.Remove(riskType);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk type '{riskType.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting risk type");
            TempData["ErrorMessage"] = "An error occurred while deleting the risk type. Please try again.";
        }
        
        return RedirectToAction(nameof(RiskTypes));
    }

    private bool RiskTypeExists(int id)
    {
        return _context.RiskTypes.Any(e => e.Id == id);
    }

    // ========================================
    // SETTINGS - Risk Tiers
    // ========================================

    // GET: Admin/RiskTiers
    public async Task<IActionResult> RiskTiers()
    {
        var riskTiers = await _context.RiskTiers
            .OrderBy(rt => rt.SortOrder)
            .ThenBy(rt => rt.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/RiskTiers.cshtml", riskTiers);
    }

    // GET: Admin/CreateRiskTier
    public IActionResult CreateRiskTier()
    {
        return View("~/Views/Admin/Settings/CreateRiskTier.cshtml");
    }

    // POST: Admin/CreateRiskTier
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRiskTier([Bind("Code,Name,Description,Summary,SortOrder,IsActive")] RiskTier riskTier)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists
                if (await _context.RiskTiers.AnyAsync(rt => rt.Code == riskTier.Code))
                {
                    ModelState.AddModelError("Code", "A risk tier with this code already exists.");
                }
                else
                {
                    riskTier.CreatedAt = DateTime.UtcNow;
                    riskTier.UpdatedAt = DateTime.UtcNow;
                    _context.Add(riskTier);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk tier '{riskTier.Name}' has been created successfully.";
                    return RedirectToAction(nameof(RiskTiers));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating risk tier");
                ModelState.AddModelError("", "An error occurred while creating the risk tier. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateRiskTier.cshtml", riskTier);
    }

    // GET: Admin/EditRiskTier/5
    public async Task<IActionResult> EditRiskTier(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskTier = await _context.RiskTiers.FindAsync(id);
        if (riskTier == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditRiskTier.cshtml", riskTier);
    }

    // POST: Admin/EditRiskTier/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRiskTier(int id, [Bind("Id,Code,Name,Description,Summary,SortOrder,IsActive")] RiskTier riskTier)
    {
        if (id != riskTier.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists for a different record
                if (await _context.RiskTiers.AnyAsync(rt => rt.Code == riskTier.Code && rt.Id != id))
                {
                    ModelState.AddModelError("Code", "A risk tier with this code already exists.");
                }
                else
                {
                    var existingRiskTier = await _context.RiskTiers.FindAsync(id);
                    if (existingRiskTier == null)
                    {
                        return NotFound();
                    }

                    existingRiskTier.Code = riskTier.Code;
                    existingRiskTier.Name = riskTier.Name;
                    existingRiskTier.Description = riskTier.Description;
                    existingRiskTier.Summary = riskTier.Summary;
                    existingRiskTier.SortOrder = riskTier.SortOrder;
                    existingRiskTier.IsActive = riskTier.IsActive;
                    existingRiskTier.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk tier '{riskTier.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(RiskTiers));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RiskTierExists(riskTier.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating risk tier");
                ModelState.AddModelError("", "An error occurred while updating the risk tier. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/EditRiskTier.cshtml", riskTier);
    }

    // GET: Admin/DeleteRiskTier/5
    public async Task<IActionResult> DeleteRiskTier(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskTier = await _context.RiskTiers.FindAsync(id);
        if (riskTier == null)
        {
            return NotFound();
        }

        // Check if any risks are using this tier
        var riskCount = await _context.Risks.CountAsync(r => r.RiskTierId == id && !r.IsDeleted);
        ViewBag.RiskCount = riskCount;

        return View("~/Views/Admin/Settings/DeleteRiskTier.cshtml", riskTier);
    }

    // POST: Admin/DeleteRiskTier/5
    [HttpPost, ActionName("DeleteRiskTier")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRiskTierConfirmed(int id)
    {
        try
        {
            var riskTier = await _context.RiskTiers.FindAsync(id);
            if (riskTier != null)
            {
                // Check if any risks are using this tier
                var riskCount = await _context.Risks.CountAsync(r => r.RiskTierId == id && !r.IsDeleted);
                if (riskCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete risk tier '{riskTier.Name}' as it is being used by {riskCount} risk(s). Please reassign those risks first.";
                }
                else
                {
                    _context.RiskTiers.Remove(riskTier);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk tier '{riskTier.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting risk tier");
            TempData["ErrorMessage"] = "An error occurred while deleting the risk tier. Please try again.";
        }
        
        return RedirectToAction(nameof(RiskTiers));
    }

    private bool RiskTierExists(int id)
    {
        return _context.RiskTiers.Any(e => e.Id == id);
    }

    // ========================================
    // SETTINGS - Action Sources
    // ========================================

    // GET: Admin/ActionSources
    public async Task<IActionResult> ActionSources()
    {
        var actionSources = await _context.ActionSources
            .OrderBy(a_s => a_s.SortOrder)
            .ThenBy(a_s => a_s.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/ActionSources.cshtml", actionSources);
    }

    // GET: Admin/CreateActionSource
    public IActionResult CreateActionSource()
    {
        return View("~/Views/Admin/Settings/CreateActionSource.cshtml");
    }

    // POST: Admin/CreateActionSource
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateActionSource([Bind("Code,Name,Description,Summary,SortOrder,IsActive")] ActionSource actionSource)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.ActionSources.AnyAsync(a_s => a_s.Code == actionSource.Code))
                {
                    ModelState.AddModelError("Code", "An action source with this code already exists.");
                }
                else
                {
                    actionSource.CreatedAt = DateTime.UtcNow;
                    actionSource.UpdatedAt = DateTime.UtcNow;
                    _context.Add(actionSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Action source '{actionSource.Name}' has been created successfully.";
                    return RedirectToAction(nameof(ActionSources));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action source");
                ModelState.AddModelError("", "An error occurred while creating the action source. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateActionSource.cshtml", actionSource);
    }

    // GET: Admin/EditActionSource/5
    public async Task<IActionResult> EditActionSource(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actionSource = await _context.ActionSources.FindAsync(id);
        if (actionSource == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditActionSource.cshtml", actionSource);
    }

    // POST: Admin/EditActionSource/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditActionSource(int id, [Bind("Id,Code,Name,Description,Summary,SortOrder,IsActive")] ActionSource actionSource)
    {
        if (id != actionSource.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.ActionSources.AnyAsync(a_s => a_s.Code == actionSource.Code && a_s.Id != id))
                {
                    ModelState.AddModelError("Code", "An action source with this code already exists.");
                }
                else
                {
                    var existingActionSource = await _context.ActionSources.FindAsync(id);
                    if (existingActionSource == null)
                    {
                        return NotFound();
                    }

                    existingActionSource.Code = actionSource.Code;
                    existingActionSource.Name = actionSource.Name;
                    existingActionSource.Description = actionSource.Description;
                    existingActionSource.Summary = actionSource.Summary;
                    existingActionSource.SortOrder = actionSource.SortOrder;
                    existingActionSource.IsActive = actionSource.IsActive;
                    existingActionSource.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Action source '{actionSource.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(ActionSources));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActionSourceExists(actionSource.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action source");
                ModelState.AddModelError("", "An error occurred while updating the action source. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/EditActionSource.cshtml", actionSource);
    }

    // GET: Admin/DeleteActionSource/5
    public async Task<IActionResult> DeleteActionSource(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actionSource = await _context.ActionSources.FindAsync(id);
        if (actionSource == null)
        {
            return NotFound();
        }

        var actionCount = await _context.Actions.CountAsync(a => a.ActionSourceId == id && !a.IsDeleted);
        ViewBag.ActionCount = actionCount;

        return View("~/Views/Admin/Settings/DeleteActionSource.cshtml", actionSource);
    }

    // POST: Admin/DeleteActionSource/5
    [HttpPost, ActionName("DeleteActionSource")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteActionSourceConfirmed(int id)
    {
        try
        {
            var actionSource = await _context.ActionSources.FindAsync(id);
            if (actionSource != null)
            {
                var actionCount = await _context.Actions.CountAsync(a => a.ActionSourceId == id && !a.IsDeleted);
                if (actionCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete action source '{actionSource.Name}' as it is being used by {actionCount} action(s). Please reassign those actions first.";
                }
                else
                {
                    _context.ActionSources.Remove(actionSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Action source '{actionSource.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action source");
            TempData["ErrorMessage"] = "An error occurred while deleting the action source. Please try again.";
        }
        
        return RedirectToAction(nameof(ActionSources));
    }

    private bool ActionSourceExists(int id)
    {
        return _context.ActionSources.Any(e => e.Id == id);
    }

    // API Token Management

    public async Task<IActionResult> ApiTokens()
    {
        var tokens = await _apiTokenService.GetAllTokensAsync();
        return View("~/Views/Admin/ApiTokens/Index.cshtml", tokens);
    }

    public IActionResult CreateApiToken()
    {
        return View("~/Views/Admin/ApiTokens/Create.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApiToken(string name, string? description, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Token name is required.";
            return RedirectToAction(nameof(CreateApiToken));
        }

        try
        {
            var userEmail = User.Identity?.Name ?? "unknown";
            var token = await _apiTokenService.CreateTokenAsync(name, description ?? string.Empty, userEmail, expiresAt);
            
            TempData["SuccessMessage"] = "API token created successfully. Make sure to copy the token now - you won't be able to see it again!";
            TempData["NewToken"] = token.Token;
            
            return RedirectToAction(nameof(ConfigurePermissions), new { id = token.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API token");
            TempData["ErrorMessage"] = "An error occurred while creating the API token.";
            return RedirectToAction(nameof(CreateApiToken));
        }
    }

    public async Task<IActionResult> ConfigurePermissions(int id)
    {
        var token = await _apiTokenService.GetByIdAsync(id);
        if (token == null)
        {
            TempData["ErrorMessage"] = "API token not found.";
            return RedirectToAction(nameof(ApiTokens));
        }

        var permissions = await _apiTokenService.GetPermissionsAsync(id);

        var resources = new[] { "Risks", "Issues", "Actions", "Milestones", "PerformanceMetrics", "EnterpriseMetrics", "FunctionalStandards" };
        
        ViewBag.Token = token;
        ViewBag.Permissions = permissions;
        ViewBag.Resources = resources;

        return View("~/Views/Admin/ApiTokens/ConfigurePermissions.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(int id, Dictionary<string, string> permissions)
    {
        try
        {
            var permissionsDict = new Dictionary<string, (bool read, bool create, bool update, bool delete)>();

            foreach (var resource in new[] { "Risks", "Issues", "Actions", "Milestones", "PerformanceMetrics", "EnterpriseMetrics", "FunctionalStandards" })
            {
                var read = permissions.ContainsKey($"{resource}_read") && permissions[$"{resource}_read"] == "on";
                var create = permissions.ContainsKey($"{resource}_create") && permissions[$"{resource}_create"] == "on";
                var update = permissions.ContainsKey($"{resource}_update") && permissions[$"{resource}_update"] == "on";
                var delete = permissions.ContainsKey($"{resource}_delete") && permissions[$"{resource}_delete"] == "on";

                if (read || create || update || delete)
                {
                    permissionsDict[resource] = (read, create, update, delete);
                }
            }

            await _apiTokenService.SetPermissionsAsync(id, permissionsDict);

            TempData["SuccessMessage"] = "Permissions updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API token permissions");
            TempData["ErrorMessage"] = "An error occurred while saving permissions.";
        }

        return RedirectToAction(nameof(ConfigurePermissions), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecycleApiToken(int id)
    {
        try
        {
            var token = await _apiTokenService.GetByIdAsync(id);
            if (token == null)
            {
                TempData["ErrorMessage"] = "API token not found.";
                return RedirectToAction(nameof(ApiTokens));
            }

            // Generate new token value
            var newToken = await _apiTokenService.RecycleTokenAsync(id);
            
            TempData["SuccessMessage"] = "API token recycled successfully. Make sure to copy the new token now - you won't be able to see it again!";
            TempData["NewToken"] = newToken;
            
            return RedirectToAction(nameof(ConfigurePermissions), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recycling API token");
            TempData["ErrorMessage"] = "An error occurred while recycling the token.";
            return RedirectToAction(nameof(ConfigurePermissions), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApiToken(int id)
    {
        try
        {
            var token = await _apiTokenService.GetByIdAsync(id);
            if (token != null)
            {
                var newStatus = !token.IsActive;
                await _apiTokenService.UpdateTokenStatusAsync(id, newStatus);
                TempData["SuccessMessage"] = $"API token {(newStatus ? "activated" : "suspended")} successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling API token status");
            TempData["ErrorMessage"] = "An error occurred while updating the token status.";
        }

        // Check if we came from ConfigurePermissions
        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("ConfigurePermissions"))
        {
            return RedirectToAction(nameof(ConfigurePermissions), new { id });
        }

        return RedirectToAction(nameof(ApiTokens));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApiToken(int id)
    {
        try
        {
            await _apiTokenService.DeleteTokenAsync(id);
            TempData["SuccessMessage"] = "API token deleted successfully.";
            return RedirectToAction(nameof(ApiTokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API token");
            TempData["ErrorMessage"] = "An error occurred while deleting the token.";
            
            // Check if we came from ConfigurePermissions
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("ConfigurePermissions"))
            {
                return RedirectToAction(nameof(ConfigurePermissions), new { id });
            }
            
            return RedirectToAction(nameof(ApiTokens));
        }
    }

    public async Task<IActionResult> ApiLogs(int? tokenId = null)
    {
        var query = _context.ApiRequestLogs
            .Include(l => l.ApiToken)
            .OrderByDescending(l => l.RequestTimestamp)
            .AsQueryable();

        if (tokenId.HasValue)
        {
            query = query.Where(l => l.ApiTokenId == tokenId.Value);
        }

        var logs = await query.Take(1000).ToListAsync();

        ViewBag.Tokens = await _apiTokenService.GetAllTokensAsync();
        ViewBag.SelectedTokenId = tokenId;

        return View("~/Views/Admin/ApiTokens/Logs.cshtml", logs);
    }
}

