using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;

namespace Compass.Controllers.Admin;

/// <summary>
/// Admin controller for managing the training course library
/// </summary>
[Route("Admin/[controller]")]
[Authorize]
public class TrainingCourseController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<TrainingCourseController> _logger;
    private readonly IPermissionService _permissionService;

    public TrainingCourseController(
        CompassDbContext context,
        ILogger<TrainingCourseController> logger,
        IPermissionService permissionService)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
    }

    /// <summary>
    /// List all training courses
    /// </summary>
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? search, bool? activeFilter)
    {
        var query = _context.TrainingCourses.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tc => 
                tc.Title.Contains(search) ||
                (tc.Description != null && tc.Description.Contains(search)) ||
                (tc.Provider != null && tc.Provider.Contains(search)));
        }

        if (activeFilter.HasValue)
        {
            query = query.Where(tc => tc.Active == activeFilter.Value);
        }
        else
        {
            // Default to active courses only
            query = query.Where(tc => tc.Active);
        }

        var courses = await query
            .OrderBy(tc => tc.Title)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.ActiveFilter = activeFilter ?? true;

        return View("~/Views/Admin/TrainingCourse/Index.cshtml", courses);
    }

    /// <summary>
    /// View course details
    /// </summary>
    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var course = await _context.TrainingCourses
            .FirstOrDefaultAsync(tc => tc.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/TrainingCourse/Details.cshtml", course);
    }

    /// <summary>
    /// Create new course - GET
    /// </summary>
    [HttpGet("Create")]
    public async Task<IActionResult> Create()
    {
        // Load active DDaT professions for the dropdown
        var professions = await _context.DdatProfessions
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        ViewBag.Professions = professions;
        return View("~/Views/Admin/TrainingCourse/Create.cshtml");
    }

    /// <summary>
    /// Create new course - POST
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingCourse course, int[] selectedPrimaryProfessionIds, int[] selectedSecondaryProfessionIds)
    {
        if (ModelState.IsValid)
        {
            // Convert selected primary profession IDs to comma-separated names
            if (selectedPrimaryProfessionIds != null && selectedPrimaryProfessionIds.Length > 0)
            {
                var professionNames = await _context.DdatProfessions
                    .Where(p => selectedPrimaryProfessionIds.Contains(p.Id))
                    .Select(p => p.Name)
                    .ToListAsync();
                
                course.PrimaryProfessionTags = string.Join(", ", professionNames);
            }
            else
            {
                course.PrimaryProfessionTags = null;
            }

            // Convert selected secondary profession IDs to comma-separated names
            if (selectedSecondaryProfessionIds != null && selectedSecondaryProfessionIds.Length > 0)
            {
                var professionNames = await _context.DdatProfessions
                    .Where(p => selectedSecondaryProfessionIds.Contains(p.Id))
                    .Select(p => p.Name)
                    .ToListAsync();
                
                course.SecondaryProfessionTags = string.Join(", ", professionNames);
            }
            else
            {
                course.SecondaryProfessionTags = null;
            }

            course.CreatedAt = DateTime.UtcNow;
            course.UpdatedAt = DateTime.UtcNow;
            course.Active = true;

            var userEmail = User.Identity?.Name ?? "System";
            course.CreatedBy = userEmail;
            course.UpdatedBy = userEmail;

            _context.TrainingCourses.Add(course);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Course created successfully";
            return RedirectToAction(nameof(Index));
        }

        // Reload professions for the view
        var professions = await _context.DdatProfessions
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        ViewBag.Professions = professions;
        ViewBag.SelectedPrimaryProfessionIds = selectedPrimaryProfessionIds ?? Array.Empty<int>();
        ViewBag.SelectedSecondaryProfessionIds = selectedSecondaryProfessionIds ?? Array.Empty<int>();

        return View("~/Views/Admin/TrainingCourse/Create.cshtml", course);
    }

    /// <summary>
    /// Edit course - GET
    /// </summary>
    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _context.TrainingCourses
            .FirstOrDefaultAsync(tc => tc.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        // Load active DDaT professions for the dropdown
        var professions = await _context.DdatProfessions
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        ViewBag.Professions = professions;
        
        // Parse existing primary profession tags to pre-select professions
        var selectedPrimaryProfessionIds = new List<int>();
        if (!string.IsNullOrEmpty(course.PrimaryProfessionTags))
        {
            var professionNames = course.PrimaryProfessionTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
            
            selectedPrimaryProfessionIds = await _context.DdatProfessions
                .Where(p => professionNames.Contains(p.Name))
                .Select(p => p.Id)
                .ToListAsync();
        }
        // Fallback to legacy ProfessionTags if PrimaryProfessionTags is empty
        else if (!string.IsNullOrEmpty(course.ProfessionTags))
        {
            var professionNames = course.ProfessionTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
            
            selectedPrimaryProfessionIds = await _context.DdatProfessions
                .Where(p => professionNames.Contains(p.Name))
                .Select(p => p.Id)
                .ToListAsync();
        }
        
        // Parse existing secondary profession tags
        var selectedSecondaryProfessionIds = new List<int>();
        if (!string.IsNullOrEmpty(course.SecondaryProfessionTags))
        {
            var professionNames = course.SecondaryProfessionTags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
            
            selectedSecondaryProfessionIds = await _context.DdatProfessions
                .Where(p => professionNames.Contains(p.Name))
                .Select(p => p.Id)
                .ToListAsync();
        }
        
        ViewBag.SelectedPrimaryProfessionIds = selectedPrimaryProfessionIds;
        ViewBag.SelectedSecondaryProfessionIds = selectedSecondaryProfessionIds;

        return View("~/Views/Admin/TrainingCourse/Edit.cshtml", course);
    }

    /// <summary>
    /// Edit course - POST
    /// </summary>
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainingCourse course, int[] selectedPrimaryProfessionIds, int[] selectedSecondaryProfessionIds)
    {
        if (id != course.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Convert selected primary profession IDs to comma-separated names
                if (selectedPrimaryProfessionIds != null && selectedPrimaryProfessionIds.Length > 0)
                {
                    var professionNames = await _context.DdatProfessions
                        .Where(p => selectedPrimaryProfessionIds.Contains(p.Id))
                        .Select(p => p.Name)
                        .ToListAsync();
                    
                    course.PrimaryProfessionTags = string.Join(", ", professionNames);
                }
                else
                {
                    course.PrimaryProfessionTags = null;
                }

                // Convert selected secondary profession IDs to comma-separated names
                if (selectedSecondaryProfessionIds != null && selectedSecondaryProfessionIds.Length > 0)
                {
                    var professionNames = await _context.DdatProfessions
                        .Where(p => selectedSecondaryProfessionIds.Contains(p.Id))
                        .Select(p => p.Name)
                        .ToListAsync();
                    
                    course.SecondaryProfessionTags = string.Join(", ", professionNames);
                }
                else
                {
                    course.SecondaryProfessionTags = null;
                }

                course.UpdatedAt = DateTime.UtcNow;
                var userEmail = User.Identity?.Name ?? "System";
                course.UpdatedBy = userEmail;

                _context.Update(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Course updated successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TrainingCourseExistsAsync(course.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        // Reload professions for the view
        var professions = await _context.DdatProfessions
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        ViewBag.Professions = professions;
        ViewBag.SelectedPrimaryProfessionIds = selectedPrimaryProfessionIds ?? Array.Empty<int>();
        ViewBag.SelectedSecondaryProfessionIds = selectedSecondaryProfessionIds ?? Array.Empty<int>();

        return View("~/Views/Admin/TrainingCourse/Edit.cshtml", course);
    }

    /// <summary>
    /// Archive course (soft delete)
    /// </summary>
    [HttpPost("Archive/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var course = await _context.TrainingCourses
            .FirstOrDefaultAsync(tc => tc.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        course.Active = false;
        course.UpdatedAt = DateTime.UtcNow;
        var userEmail = User.Identity?.Name ?? "System";
        course.UpdatedBy = userEmail;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Course archived successfully";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Restore archived course
    /// </summary>
    [HttpPost("Restore/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var course = await _context.TrainingCourses
            .FirstOrDefaultAsync(tc => tc.Id == id);

        if (course == null)
        {
            return NotFound();
        }

        course.Active = true;
        course.UpdatedAt = DateTime.UtcNow;
        var userEmail = User.Identity?.Name ?? "System";
        course.UpdatedBy = userEmail;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Course restored successfully";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> TrainingCourseExistsAsync(int id)
    {
        return await _context.TrainingCourses.AnyAsync(tc => tc.Id == id);
    }
}

