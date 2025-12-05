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
    public IActionResult Create()
    {
        return View("~/Views/Admin/TrainingCourse/Create.cshtml");
    }

    /// <summary>
    /// Create new course - POST
    /// </summary>
    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TrainingCourse course)
    {
        if (ModelState.IsValid)
        {
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

        return View("~/Views/Admin/TrainingCourse/Edit.cshtml", course);
    }

    /// <summary>
    /// Edit course - POST
    /// </summary>
    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TrainingCourse course)
    {
        if (id != course.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
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

