using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;

namespace Compass.Controllers
{
    [Route("Admin/[controller]")]
    public class OrganizationalController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<OrganizationalController> _logger;

        public OrganizationalController(CompassDbContext context, ILogger<OrganizationalController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Organizational
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var groups = await _context.OrganizationalGroups
                .Include(g => g.ParentGroup)
                .Include(g => g.ChildGroups)
                .Include(g => g.Roles)
                .Where(g => g.IsActive)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            return View("~/Views/Admin/Organizational/Index.cshtml", groups);
        }

        // GET: Admin/Organizational/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.OrganizationalGroups
                .Include(g => g.ParentGroup)
                .Include(g => g.ChildGroups.Where(c => c.IsActive))
                .Include(g => g.Roles.Where(r => r.IsActive))
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Organizational/Details.cshtml", group);
        }

        // GET: Admin/Organizational/Create
        [HttpGet("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.ParentGroups = await _context.OrganizationalGroups
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
            
            return View("~/Views/Admin/Organizational/Create.cshtml");
        }

        // POST: Admin/Organizational/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrganizationalGroup group)
        {
            if (ModelState.IsValid)
            {
                group.CreatedAt = DateTime.UtcNow;
                group.UpdatedAt = DateTime.UtcNow;
                
                _context.Add(group);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Organizational group created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ParentGroups = await _context.OrganizationalGroups
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
            
            return View("~/Views/Admin/Organizational/Create.cshtml", group);
        }

        // GET: Admin/Organizational/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.OrganizationalGroups.FindAsync(id);
            if (group == null || !group.IsActive)
            {
                return NotFound();
            }

            ViewBag.ParentGroups = await _context.OrganizationalGroups
                .Where(g => g.IsActive && g.Id != id) // Exclude self and descendants
                .OrderBy(g => g.Name)
                .ToListAsync();
            
            return View("~/Views/Admin/Organizational/Edit.cshtml", group);
        }

        // POST: Admin/Organizational/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OrganizationalGroup group)
        {
            if (id != group.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    group.UpdatedAt = DateTime.UtcNow;
                    _context.Update(group);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Organizational group updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizationalGroupExists(group.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ParentGroups = await _context.OrganizationalGroups
                .Where(g => g.IsActive && g.Id != id)
                .OrderBy(g => g.Name)
                .ToListAsync();
            
            return View("~/Views/Admin/Organizational/Edit.cshtml", group);
        }

        // GET: Admin/Organizational/Delete/5
        [HttpGet("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var group = await _context.OrganizationalGroups
                .Include(g => g.ParentGroup)
                .Include(g => g.ChildGroups.Where(c => c.IsActive))
                .Include(g => g.Roles.Where(r => r.IsActive))
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (group == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/Organizational/Delete.cshtml", group);
        }

        // POST: Admin/Organizational/Delete/5
        [HttpPost("Delete/{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.OrganizationalGroups.FindAsync(id);
            if (group != null)
            {
                // Soft delete
                group.IsActive = false;
                group.UpdatedAt = DateTime.UtcNow;
                
                // Also soft delete child groups and roles
                var childGroups = await _context.OrganizationalGroups
                    .Where(g => g.ParentGroupId == id)
                    .ToListAsync();
                
                foreach (var child in childGroups)
                {
                    child.IsActive = false;
                    child.UpdatedAt = DateTime.UtcNow;
                }
                
                var roles = await _context.OrganizationalRoles
                    .Where(r => r.OrganizationalGroupId == id)
                    .ToListAsync();
                
                foreach (var role in roles)
                {
                    role.IsActive = false;
                    role.UpdatedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Organizational group deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Organizational/CreateRole/5
        [HttpGet("CreateRole/{groupId}")]
        public async Task<IActionResult> CreateRole(int? groupId)
        {
            if (groupId == null)
            {
                return NotFound();
            }

            var group = await _context.OrganizationalGroups.FindAsync(groupId);
            if (group == null || !group.IsActive)
            {
                return NotFound();
            }

            ViewBag.Group = group;
            ViewBag.RoleTypes = new[] { "Director General", "Director", "Deputy Director" };
            
            return View("~/Views/Admin/Organizational/CreateRole.cshtml");
        }

        // POST: Admin/Organizational/CreateRole
        [HttpPost("CreateRole")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(OrganizationalRole role)
        {
            if (ModelState.IsValid)
            {
                role.CreatedAt = DateTime.UtcNow;
                role.UpdatedAt = DateTime.UtcNow;
                
                _context.Add(role);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Organizational role created successfully.";
                return RedirectToAction(nameof(Details), new { id = role.OrganizationalGroupId });
            }

            var group = await _context.OrganizationalGroups.FindAsync(role.OrganizationalGroupId);
            ViewBag.Group = group;
            ViewBag.RoleTypes = new[] { "Director General", "Director", "Deputy Director" };
            
            return View("~/Views/Admin/Organizational/CreateRole.cshtml", role);
        }

        // GET: Admin/Organizational/EditRole/5
        [HttpGet("EditRole/{id}")]
        public async Task<IActionResult> EditRole(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var role = await _context.OrganizationalRoles
                .Include(r => r.OrganizationalGroup)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (role == null)
            {
                return NotFound();
            }

            ViewBag.RoleTypes = new[] { "Director General", "Director", "Deputy Director" };
            
            return View("~/Views/Admin/Organizational/EditRole.cshtml", role);
        }

        // POST: Admin/Organizational/EditRole/5
        [HttpPost("EditRole/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(int id, OrganizationalRole role)
        {
            if (id != role.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    role.UpdatedAt = DateTime.UtcNow;
                    _context.Update(role);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Organizational role updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrganizationalRoleExists(role.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = role.OrganizationalGroupId });
            }

            ViewBag.RoleTypes = new[] { "Director General", "Director", "Deputy Director" };
            
            return View("~/Views/Admin/Organizational/EditRole.cshtml", role);
        }

        // POST: Admin/Organizational/DeleteRole/5
        [HttpPost("DeleteRole/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.OrganizationalRoles.FindAsync(id);
            if (role != null)
            {
                var groupId = role.OrganizationalGroupId;
                
                // Soft delete
                role.IsActive = false;
                role.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Organizational role deleted successfully.";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrganizationalGroupExists(int id)
        {
            return _context.OrganizationalGroups.Any(e => e.Id == id);
        }

        private bool OrganizationalRoleExists(int id)
        {
            return _context.OrganizationalRoles.Any(e => e.Id == id);
        }
    }
}
