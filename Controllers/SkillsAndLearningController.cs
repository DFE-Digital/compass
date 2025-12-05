using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Compass.Controllers;

/// <summary>
/// Unified controller for Skills and Learning features
/// Handles all user roles: Individuals, Learning and Skills approvers, HOP, Central Ops Admin, and Reporting
/// </summary>
[Authorize]
public class SkillsAndLearningController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<SkillsAndLearningController> _logger;
    private readonly IPermissionService _permissionService;
    private readonly INudgingService _nudgingService;
    private readonly INotificationService? _notificationService;
    private readonly IGraphService? _graphService;

    public SkillsAndLearningController(
        CompassDbContext context,
        ILogger<SkillsAndLearningController> logger,
        IPermissionService permissionService,
        INudgingService nudgingService,
        INotificationService? notificationService = null,
        IGraphService? graphService = null)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _nudgingService = nudgingService;
        _notificationService = notificationService;
        _graphService = graphService;
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    private async Task<int?> GetCurrentUserIdAsync()
    {
        var userEmail = User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            return null;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

        return user?.Id;
    }

    /// <summary>
    /// Main dashboard for Skills and Learning
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        // Get upcoming approved training (not completed, with planned date in the future or null)
        var upcomingTraining = await _context.TrainingRequests
            .Where(tr => tr.UserId == userId.Value 
                && tr.Status == "Approved"
                && (tr.TrainingCompleted != true)
                && (tr.PlannedDate == null || tr.PlannedDate >= DateTime.UtcNow.Date))
            .OrderBy(tr => tr.PlannedDate ?? DateTime.MaxValue)
            .Include(tr => tr.Course)
            .ToListAsync();

        // Generate and get nudges
        var nudges = await _nudgingService.GenerateNudgesForUserAsync(userId.Value);

        // Load user's professional profile
        var profile = await _context.UserProfessionalProfiles
            .Include(p => p.DdatProfession)
            .Include(p => p.DdatFrameworkRole)
            .Include(p => p.UserSkills)
                .ThenInclude(us => us.Skill)
            .Include(p => p.AdditionalDdatFrameworkSkills)
                .ThenInclude(adfs => adfs.DdatFrameworkSkill)
            .Include(p => p.CapabilityGaps)
                .ThenInclude(cg => cg.Action)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (profile == null)
        {
            profile = new UserProfessionalProfile
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserProfessionalProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        // Load Head of Profession based on the user's Role
        // First try to get profession from the profile, then try to match from role
        Models.User? headOfProfession = null;
        int? professionIdToUse = null;

        // If user has DdatProfessionId set, use that
        if (profile.DdatProfessionId.HasValue)
        {
            professionIdToUse = profile.DdatProfessionId.Value;
        }
        // Otherwise, if user has a Role, try to find profession by matching RoleFamily to RoleGroup or Role to Name
        else if (profile.DdatFrameworkRoleId.HasValue && profile.DdatFrameworkRole != null)
        {
            // Load active professions and filter in memory for case-insensitive comparison
            var allActiveProfessions = await _context.DdatProfessions
                .Where(p => p.IsActive)
                .ToListAsync();
            
            var roleFamily = profile.DdatFrameworkRole.RoleFamily;
            var role = profile.DdatFrameworkRole.Role;
            
            // Extract base role name (everything before first parenthesis, if any)
            var baseRoleName = role;
            if (!string.IsNullOrEmpty(role) && role.Contains('('))
            {
                baseRoleName = role.Substring(0, role.IndexOf('(')).Trim();
            }
            
            // Try matching RoleFamily to RoleGroup or Name
            var profession = allActiveProfessions
                .FirstOrDefault(p => 
                    (!string.IsNullOrEmpty(roleFamily) &&
                     ((!string.IsNullOrEmpty(p.RoleGroup) && 
                       string.Equals(p.RoleGroup, roleFamily, StringComparison.OrdinalIgnoreCase)) ||
                      (!string.IsNullOrEmpty(p.Name) && 
                       string.Equals(p.Name, roleFamily, StringComparison.OrdinalIgnoreCase)))) ||
                    // Also try matching Role name (base name before parentheses) to profession Name
                    (!string.IsNullOrEmpty(baseRoleName) &&
                     !string.IsNullOrEmpty(p.Name) &&
                     string.Equals(p.Name, baseRoleName, StringComparison.OrdinalIgnoreCase)));
            
            if (profession != null)
            {
                professionIdToUse = profession.Id;
            }
        }

        if (professionIdToUse.HasValue)
        {
            var hop = await _context.HOPS
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.DdatProfessionId == professionIdToUse.Value);
            
            if (hop?.User != null)
            {
                headOfProfession = hop.User;
            }
        }

        ViewBag.HeadOfProfession = headOfProfession;

        // Load professions for dropdown
        ViewBag.Professions = await _context.DdatProfessions
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        // Load skills for skills table
        ViewBag.AvailableSkills = await _context.Skills
            .Where(s => s.IsActive)
            .OrderBy(s => s.SkillName)
            .ToListAsync();

        // Load active framework version for DDAT roles
        var activeFrameworkVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);
        
        ViewBag.ActiveFrameworkVersion = activeFrameworkVersion;
        
        if (activeFrameworkVersion != null)
        {
            ViewBag.RoleFamilies = await _context.DdatFrameworkRoles
                .Where(r => r.FrameworkVersionId == activeFrameworkVersion.Id && !r.IsArchived)
                .Select(r => r.RoleFamily)
                .Distinct()
                .OrderBy(rf => rf)
                .ToListAsync();
        }
        else
        {
            ViewBag.RoleFamilies = new List<string>();
        }

        // Load grades
        ViewBag.Grades = await _context.Grades
            .Where(g => g.IsActive)
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Code)
            .Select(g => g.Code)
            .ToListAsync();

        ViewBag.UpcomingTraining = upcomingTraining;
        ViewBag.Nudges = nudges;
        ViewBag.Profile = profile;

        return View(profile);
    }

    /// <summary>
    /// Browse approved/recommended training courses
    /// </summary>
    public async Task<IActionResult> BrowseCourses(
        string? professionFilter,
        string? capabilityFilter,
        string? providerFilter,
        string? formatFilter,
        string? modeFilter,
        string? search)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var query = _context.TrainingCourses
            .Where(tc => tc.Active);

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tc => 
                tc.Title.Contains(search) ||
                (tc.Description != null && tc.Description.Contains(search)) ||
                (tc.Provider != null && tc.Provider.Contains(search)));
        }

        if (!string.IsNullOrEmpty(professionFilter))
        {
            query = query.Where(tc => 
                (tc.ProfessionTags != null && tc.ProfessionTags.Contains(professionFilter)) ||
                (tc.PrimaryProfessionTags != null && tc.PrimaryProfessionTags.Contains(professionFilter)) ||
                (tc.SecondaryProfessionTags != null && tc.SecondaryProfessionTags.Contains(professionFilter)));
        }

        if (!string.IsNullOrEmpty(capabilityFilter))
        {
            query = query.Where(tc => 
                tc.CapabilityTags != null && 
                tc.CapabilityTags.Contains(capabilityFilter));
        }

        if (!string.IsNullOrEmpty(providerFilter))
        {
            query = query.Where(tc => tc.Provider == providerFilter);
        }

        if (!string.IsNullOrEmpty(formatFilter))
        {
            query = query.Where(tc => tc.Format == formatFilter);
        }

        if (!string.IsNullOrEmpty(modeFilter))
        {
            query = query.Where(tc => tc.Mode == modeFilter);
        }

        var courses = await query
            .OrderBy(tc => tc.Title)
            .ToListAsync();

        // Get distinct providers from active courses for dropdown
        var providers = await _context.TrainingCourses
            .Where(tc => tc.Active && !string.IsNullOrEmpty(tc.Provider))
            .Select(tc => tc.Provider)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        // Get distinct professions from PrimaryProfessionTags
        var allProfessions = new HashSet<string>();
        var coursesWithProfessions = await _context.TrainingCourses
            .Where(tc => tc.Active && !string.IsNullOrEmpty(tc.PrimaryProfessionTags))
            .Select(tc => tc.PrimaryProfessionTags)
            .ToListAsync();
        
        foreach (var professionTags in coursesWithProfessions)
        {
            if (!string.IsNullOrEmpty(professionTags))
            {
                var professions = professionTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var profession in professions)
                {
                    if (!string.IsNullOrWhiteSpace(profession))
                    {
                        allProfessions.Add(profession.Trim());
                    }
                }
            }
        }

        var professionsList = allProfessions.OrderBy(p => p).ToList();

        // Get user profile for recommendations
        var userProfile = await _context.UserProfessionalProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        ViewBag.ProfessionFilter = professionFilter;
        ViewBag.CapabilityFilter = capabilityFilter;
        ViewBag.ProviderFilter = providerFilter;
        ViewBag.FormatFilter = formatFilter;
        ViewBag.ModeFilter = modeFilter;
        ViewBag.Search = search;
        ViewBag.Providers = providers;
        ViewBag.Professions = professionsList;
        ViewBag.UserProfile = userProfile;

        return View(courses);
    }

    /// <summary>
    /// View course details
    /// </summary>
    public async Task<IActionResult> CourseDetails(int id)
    {
        var course = await _context.TrainingCourses
            .FirstOrDefaultAsync(tc => tc.Id == id && tc.Active);

        if (course == null)
        {
            return NotFound();
        }

        return View(course);
    }

    /// <summary>
    /// Get course details as JSON (for AJAX requests)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCourseDetails(int id)
    {
        var course = await _context.TrainingCourses
            .FirstOrDefaultAsync(tc => tc.Id == id && tc.Active);

        if (course == null)
        {
            return Json(new { error = "Course not found" });
        }

        return Json(new
        {
            id = course.Id,
            title = course.Title,
            provider = course.Provider ?? "N/A",
            cost = course.Cost.HasValue ? course.Cost.Value : (decimal?)null,
            description = course.Description ?? "No description available",
            primaryProfessions = course.PrimaryProfessionTags ?? "None",
            secondaryProfessions = course.SecondaryProfessionTags ?? "None",
            format = course.Format ?? "N/A",
            duration = course.Duration ?? "N/A",
            url = course.Url
        });
    }

    /// <summary>
    /// Request training - GET
    /// </summary>
    public async Task<IActionResult> RequestTraining(int? courseId, int? nudgeId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var courses = await _context.TrainingCourses
            .Where(tc => tc.Active)
            .OrderBy(tc => tc.Title)
            .ToListAsync();

        ViewBag.Courses = new SelectList(courses, "Id", "Title", courseId);
        ViewBag.NudgeId = nudgeId;

        if (courseId.HasValue)
        {
            var course = await _context.TrainingCourses
                .FirstOrDefaultAsync(tc => tc.Id == courseId.Value);
            ViewBag.SelectedCourse = course;
        }

        return View();
    }

    /// <summary>
    /// Request training - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestTraining(
        int? courseId,
        string? customCourseTitle,
        string? customCourseProvider,
        decimal? customCourseCost,
        string? customCourseUrl,
        string justification,
        string? professionAlignment,
        int? nudgeId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(justification))
        {
            ModelState.AddModelError("Justification", "Justification is required");
        }

        if (!courseId.HasValue && string.IsNullOrEmpty(customCourseTitle))
        {
            ModelState.AddModelError("CustomCourseTitle", "Either select a course or provide a custom course title");
        }

        if (!ModelState.IsValid)
        {
            var courses = await _context.TrainingCourses
                .Where(tc => tc.Active)
                .OrderBy(tc => tc.Title)
                .ToListAsync();

            ViewBag.Courses = new SelectList(courses, "Id", "Title", courseId);
            ViewBag.NudgeId = nudgeId;
            return View();
        }

        var request = new TrainingRequest
        {
            UserId = userId.Value,
            CourseId = courseId,
            CustomCourseTitle = customCourseTitle,
            CustomCourseProvider = customCourseProvider,
            CustomCourseCost = customCourseCost,
            CustomCourseUrl = customCourseUrl,
            Justification = justification,
            ProfessionAlignment = professionAlignment,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TrainingRequests.Add(request);
        await _context.SaveChangesAsync();

        // If accepting a nudge, mark it as accepted
        if (nudgeId.HasValue)
        {
            await _nudgingService.AcceptNudgeAsync(nudgeId.Value, userId.Value);
        }

        TempData["SuccessMessage"] = "Training request saved as draft. You can submit it when ready.";
        return RedirectToAction(nameof(MyRequests));
    }

    /// <summary>
    /// Submit a draft training request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRequest(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var request = await _context.TrainingRequests
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Draft")
        {
            TempData["ErrorMessage"] = "Only draft requests can be submitted";
            return RedirectToAction(nameof(MyRequests));
        }

        request.Status = "Submitted";
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Training request submitted successfully";
        return RedirectToAction(nameof(MyRequests));
    }

    /// <summary>
    /// Edit a draft training request
    /// </summary>
    public async Task<IActionResult> EditRequest(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Draft")
        {
            TempData["ErrorMessage"] = "Only draft requests can be edited";
            return RedirectToAction(nameof(MyRequests));
        }

        var courses = await _context.TrainingCourses
            .Where(tc => tc.Active)
            .OrderBy(tc => tc.Title)
            .ToListAsync();

        ViewBag.Courses = new SelectList(courses, "Id", "Title", request.CourseId);
        return View(request);
    }

    /// <summary>
    /// Update a draft training request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequest(
        int id,
        int? courseId,
        string? customCourseTitle,
        string? justification,
        string? professionAlignment)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var request = await _context.TrainingRequests
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Draft")
        {
            TempData["ErrorMessage"] = "Only draft requests can be edited";
            return RedirectToAction(nameof(MyRequests));
        }

        if (string.IsNullOrEmpty(justification))
        {
            ModelState.AddModelError("Justification", "Justification is required");
        }

        if (!courseId.HasValue && string.IsNullOrEmpty(customCourseTitle))
        {
            ModelState.AddModelError("CustomCourseTitle", "Either select a course or provide a custom course title");
        }

        if (!ModelState.IsValid)
        {
            var courses = await _context.TrainingCourses
                .Where(tc => tc.Active)
                .OrderBy(tc => tc.Title)
                .ToListAsync();

            ViewBag.Courses = new SelectList(courses, "Id", "Title", courseId);
            return View("EditRequest", request);
        }

        request.CourseId = courseId;
        request.CustomCourseTitle = customCourseTitle;
        request.Justification = justification;
        request.ProfessionAlignment = professionAlignment;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Training request updated successfully";
        return RedirectToAction(nameof(MyRequests));
    }

    /// <summary>
    /// Withdraw a draft or submitted training request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WithdrawRequest(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var request = await _context.TrainingRequests
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Draft" && request.Status != "Submitted")
        {
            TempData["ErrorMessage"] = "Only draft or submitted requests can be withdrawn";
            return RedirectToAction(nameof(MyRequests));
        }

        _context.TrainingRequests.Remove(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Training request withdrawn successfully";
        return RedirectToAction(nameof(MyRequests));
    }

    /// <summary>
    /// Manage withdrawal request for an approved training request
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ManageWithdrawal(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Approved")
        {
            TempData["ErrorMessage"] = "Only approved requests can request withdrawal";
            return RedirectToAction(nameof(MyRequests));
        }

        if (request.TrainingCompleted == true)
        {
            TempData["ErrorMessage"] = "Cannot withdraw completed training";
            return RedirectToAction(nameof(MyRequests));
        }

        return View(request);
    }

    /// <summary>
    /// Submit withdrawal request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitWithdrawalRequest(int id, string reason)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var request = await _context.TrainingRequests
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Approved")
        {
            TempData["ErrorMessage"] = "Only approved requests can request withdrawal";
            return RedirectToAction(nameof(MyRequests));
        }

        if (request.TrainingCompleted == true)
        {
            TempData["ErrorMessage"] = "Cannot withdraw completed training";
            return RedirectToAction(nameof(MyRequests));
        }

        request.WithdrawalRequested = true;
        request.WithdrawalReason = reason;
        request.WithdrawalRequestedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Withdrawal request submitted successfully. An admin will review it shortly.";
        return RedirectToAction(nameof(MyRequests));
    }

    /// <summary>
    /// View withdrawal request details (Admin)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ViewWithdrawalRequest(int id)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .Include(tr => tr.TransferToUser)
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null || !request.WithdrawalRequested)
        {
            return NotFound();
        }

        return View(request);
    }

    /// <summary>
    /// Cancel withdrawal request if free (Admin)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelWithdrawalRequest(int id)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null || !request.WithdrawalRequested)
        {
            return NotFound();
        }

        // Check if it's free (no cost or actual cost is 0)
        var cost = request.ActualCost ?? request.Course?.Cost ?? request.CustomCourseCost ?? 0;
        if (cost > 0)
        {
            TempData["ErrorMessage"] = "Cannot cancel withdrawal - this training has a cost. Please transfer it instead.";
            return RedirectToAction(nameof(ViewWithdrawalRequest), new { id });
        }

        // Cancel the request
        _context.TrainingRequests.Remove(request);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Withdrawal request cancelled - training request removed successfully";
        return RedirectToAction(nameof(AdminRequests));
    }

    /// <summary>
    /// Transfer training request to another user (Admin)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TransferRequest(int id, int transferToUserId)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.User)
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        var transferToUser = await _context.Users.FindAsync(transferToUserId);
        if (transferToUser == null)
        {
            TempData["ErrorMessage"] = "User not found";
            return RedirectToAction(nameof(ViewWithdrawalRequest), new { id });
        }

        // Transfer the request
        var originalUserId = request.UserId;
        var originalUserName = request.User?.Name ?? "Unknown";
        request.UserId = transferToUserId;
        request.TransferToUserId = transferToUserId;
        request.WithdrawalRequested = false; // Clear withdrawal request if it was a withdrawal
        request.WithdrawalReason = null;
        request.WithdrawalRequestedAt = null;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Training request transferred from {originalUserName} to {transferToUser.Name} successfully";
        return RedirectToAction(nameof(AdminRequests));
    }

    /// <summary>
    /// Dismiss a training nudge
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissNudge(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var success = await _nudgingService.DismissNudgeAsync(id, userId.Value);
        if (success)
        {
            TempData["SuccessMessage"] = "Recommendation dismissed";
        }
        else
        {
            TempData["ErrorMessage"] = "Unable to dismiss recommendation";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Accept a training nudge (redirects to request training with course pre-selected)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptNudge(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var nudge = await _context.TrainingNudges
            .Include(tn => tn.Course)
            .FirstOrDefaultAsync(tn => tn.Id == id && tn.UserId == userId.Value);

        if (nudge == null || nudge.Course == null)
        {
            TempData["ErrorMessage"] = "Recommendation not found";
            return RedirectToAction(nameof(Index));
        }

        // Mark nudge as accepted
        await _nudgingService.AcceptNudgeAsync(id, userId.Value);

        // Redirect to request training with course pre-selected
        return RedirectToAction(nameof(RequestTraining), new { courseId = nudge.CourseId, nudgeId = id });
    }

    /// <summary>
    /// View my training requests (includes all requests and completed training)
    /// </summary>
    public async Task<IActionResult> MyRequests(string? statusFilter)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var query = _context.TrainingRequests
            .Where(tr => tr.UserId == userId.Value)
            .Include(tr => tr.Course)
            .AsQueryable();

        // Apply status filter based on navigation selection
        if (!string.IsNullOrEmpty(statusFilter))
        {
            switch (statusFilter)
            {
                case "Submitted":
                    query = query.Where(tr => tr.Status == "Submitted");
                    break;
                case "Approved":
                    query = query.Where(tr => tr.Status == "Approved" && (tr.TrainingCompleted != true));
                    break;
                case "Rejected":
                    query = query.Where(tr => tr.Status == "Rejected");
                    break;
                case "Cancelled":
                    // Get cancelled training records
                    var cancelledRecordUserIds = await _context.TrainingRecords
                        .Where(tr => tr.UserId == userId.Value && tr.Status == "Cancelled")
                        .Select(tr => tr.UserId)
                        .Distinct()
                        .ToListAsync();
                    var cancelledRecordCourseIds = await _context.TrainingRecords
                        .Where(tr => tr.UserId == userId.Value && tr.Status == "Cancelled")
                        .Select(tr => tr.CourseId)
                        .Distinct()
                        .ToListAsync();
                    query = query.Where(tr => 
                        (cancelledRecordUserIds.Contains(tr.UserId) && cancelledRecordCourseIds.Contains(tr.CourseId)) ||
                        tr.WithdrawalRequested);
                    break;
                case "Completed":
                    query = query.Where(tr => tr.Status == "Approved" && tr.TrainingCompleted == true);
                    break;
            }
        }
        else
        {
            // Default to "Submitted" if no filter specified
            query = query.Where(tr => tr.Status == "Submitted");
            statusFilter = "Submitted";
        }

        var requests = await query
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        // Load related TrainingRecords for feedback/ratings
        var requestIds = requests.Select(r => r.Id).ToList();
        var trainingRecords = await _context.TrainingRecords
            .Where(tr => tr.UserId == userId.Value)
            .Include(tr => tr.Course)
            .ToListAsync();

        // Match TrainingRecords to TrainingRequests by CourseId and UserId
        var recordsByCourse = trainingRecords
            .GroupBy(tr => tr.CourseId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAt).First());

        // Get counts for each status
        var baseQuery = _context.TrainingRequests.Where(tr => tr.UserId == userId.Value);
        var submittedCount = await baseQuery.CountAsync(tr => tr.Status == "Submitted");
        var approvedCount = await baseQuery.CountAsync(tr => tr.Status == "Approved" && (tr.TrainingCompleted != true));
        var rejectedCount = await baseQuery.CountAsync(tr => tr.Status == "Rejected");
        
        var cancelledCountUserIds = await _context.TrainingRecords
            .Where(tr => tr.UserId == userId.Value && tr.Status == "Cancelled")
            .Select(tr => tr.UserId)
            .Distinct()
            .ToListAsync();
        var cancelledCountCourseIds = await _context.TrainingRecords
            .Where(tr => tr.UserId == userId.Value && tr.Status == "Cancelled")
            .Select(tr => tr.CourseId)
            .Distinct()
            .ToListAsync();
        var cancelledCount = await baseQuery.CountAsync(tr => 
            (cancelledCountUserIds.Contains(tr.UserId) && cancelledCountCourseIds.Contains(tr.CourseId)) ||
            tr.WithdrawalRequested);
        
        var completedCount = await baseQuery.CountAsync(tr => tr.Status == "Approved" && tr.TrainingCompleted == true);

        ViewBag.TrainingRecords = recordsByCourse;
        ViewBag.StatusFilter = statusFilter ?? "Submitted";
        ViewBag.SubmittedCount = submittedCount;
        ViewBag.ApprovedCount = approvedCount;
        ViewBag.RejectedCount = rejectedCount;
        ViewBag.CancelledCount = cancelledCount;
        ViewBag.CompletedCount = completedCount;
        return View(requests);
    }


    /// <summary>
    /// Provide feedback for completed training
    /// </summary>
    public async Task<IActionResult> ProvideFeedback(int id)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var record = await _context.TrainingRecords
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (record == null)
        {
            return NotFound();
        }

        return View(record);
    }

    /// <summary>
    /// Submit feedback for completed training
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitFeedback(
        int id,
        int outcomeRating,
        string? feedback,
        string? evidenceFileUrl,
        IFormFile? evidenceFile)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var record = await _context.TrainingRecords
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id && tr.UserId == userId.Value);

        if (record == null)
        {
            return NotFound();
        }

        if (outcomeRating < 1 || outcomeRating > 5)
        {
            ModelState.AddModelError("OutcomeRating", "Rating must be between 1 and 5");
            return View("ProvideFeedback", record);
        }

        // Handle file upload
        string? finalEvidenceUrl = evidenceFileUrl;
        if (evidenceFile != null && evidenceFile.Length > 0)
        {
            // Validate file size (10 MB limit)
            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (evidenceFile.Length > maxFileSize)
            {
                ModelState.AddModelError("EvidenceFile", "File size must be less than 10 MB");
                return View("ProvideFeedback", record);
            }

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
            var fileExtension = Path.GetExtension(evidenceFile.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("EvidenceFile", "File type not allowed. Allowed types: PDF, JPG, PNG, DOC, DOCX");
                return View("ProvideFeedback", record);
            }

            try
            {
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "training-evidence");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{record.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Path.GetFileName(evidenceFile.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await evidenceFile.CopyToAsync(stream);
                }

                // Store relative URL
                finalEvidenceUrl = $"/uploads/training-evidence/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading evidence file");
                ModelState.AddModelError("EvidenceFile", "Error uploading file. Please try again.");
                return View("ProvideFeedback", record);
            }
        }

        record.OutcomeRating = outcomeRating;
        record.Feedback = feedback;
        record.EvidenceFileUrl = finalEvidenceUrl;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Feedback submitted successfully";
        return RedirectToAction(nameof(MyRequests));
    }

    /// <summary>
    /// Update professional profile
    /// </summary>
    public async Task<IActionResult> MyProfile()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _context.UserProfessionalProfiles
            .Include(p => p.DdatProfession)
            .Include(p => p.DdatFrameworkRole)
            .Include(p => p.UserSkills)
                .ThenInclude(us => us.Skill)
            .Include(p => p.AdditionalDdatFrameworkSkills)
                .ThenInclude(adfs => adfs.DdatFrameworkSkill)
            .Include(p => p.CapabilityGaps)
                .ThenInclude(cg => cg.Action)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (profile == null)
        {
            profile = new UserProfessionalProfile
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserProfessionalProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        // Load professions for dropdown
        ViewBag.Professions = await _context.DdatProfessions
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();

        // Load skills for skills table
        ViewBag.AvailableSkills = await _context.Skills
            .Where(s => s.IsActive)
            .OrderBy(s => s.SkillName)
            .ToListAsync();

        // Load active framework version for DDAT roles
        ViewBag.ActiveFrameworkVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);

        // Load role families if framework exists
        var activeFrameworkVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);
        
        ViewBag.ActiveFrameworkVersion = activeFrameworkVersion;
        
        if (activeFrameworkVersion != null)
        {
            ViewBag.RoleFamilies = await _context.DdatFrameworkRoles
                .Where(r => r.FrameworkVersionId == activeFrameworkVersion.Id && !r.IsArchived)
                .Select(r => r.RoleFamily)
                .Distinct()
                .OrderBy(rf => rf)
                .ToListAsync();
        }
        else
        {
            ViewBag.RoleFamilies = new List<string>();
        }

        // Load grades
        ViewBag.Grades = await _context.Grades
            .Where(g => g.IsActive)
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Code)
            .Select(g => g.Code)
            .ToListAsync();

        return View(profile);
    }

    /// <summary>
    /// Update professional profile - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        int? ddatProfessionId,
        int? ddatFrameworkRoleId,
        string? substantiveGrade,
        int[]? selectedSkillIds,
        int[]? selectedDdatFrameworkSkillIds,
        string[]? capabilityGapDescriptions,
        int[]? capabilityGapIds)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _context.UserProfessionalProfiles
            .Include(p => p.UserSkills)
            .Include(p => p.AdditionalDdatFrameworkSkills)
            .Include(p => p.CapabilityGaps)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (profile == null)
        {
            profile = new UserProfessionalProfile
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserProfessionalProfiles.Add(profile);
        }

        // Update profession
        profile.DdatProfessionId = ddatProfessionId;
        
        // Update DDAT Framework role
        profile.DdatFrameworkRoleId = ddatFrameworkRoleId;
        
        // Update substantive grade
        profile.SubstantiveGrade = substantiveGrade;
        
        profile.UpdatedAt = DateTime.UtcNow;

        // Update profession skills (legacy)
        if (selectedSkillIds != null)
        {
            // Remove skills that are no longer selected
            var skillsToRemove = profile.UserSkills
                .Where(us => !selectedSkillIds.Contains(us.SkillId))
                .ToList();
            foreach (var skillToRemove in skillsToRemove)
            {
                _context.UserProfessionalProfileSkills.Remove(skillToRemove);
            }

            // Add new skills
            var existingSkillIds = profile.UserSkills.Select(us => us.SkillId).ToList();
            var newSkillIds = selectedSkillIds.Where(id => !existingSkillIds.Contains(id)).ToList();
            foreach (var skillId in newSkillIds)
            {
                profile.UserSkills.Add(new UserProfessionalProfileSkill
                {
                    UserProfessionalProfileId = profile.Id,
                    SkillId = skillId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Remove all profession skills if none selected
            _context.UserProfessionalProfileSkills.RemoveRange(profile.UserSkills);
        }

        // Update additional DDAT Framework Skills
        if (selectedDdatFrameworkSkillIds != null)
        {
            // Remove skills that are no longer selected
            var ddatSkillsToRemove = profile.AdditionalDdatFrameworkSkills
                .Where(adfs => !selectedDdatFrameworkSkillIds.Contains(adfs.DdatFrameworkSkillId))
                .ToList();
            foreach (var skillToRemove in ddatSkillsToRemove)
            {
                _context.UserDdatFrameworkSkills.Remove(skillToRemove);
            }

            // Add new DDAT Framework Skills
            var existingDdatSkillIds = profile.AdditionalDdatFrameworkSkills.Select(adfs => adfs.DdatFrameworkSkillId).ToList();
            var newDdatSkillIds = selectedDdatFrameworkSkillIds.Where(id => !existingDdatSkillIds.Contains(id)).ToList();
            foreach (var skillId in newDdatSkillIds)
            {
                profile.AdditionalDdatFrameworkSkills.Add(new UserDdatFrameworkSkill
                {
                    UserProfessionalProfileId = profile.Id,
                    DdatFrameworkSkillId = skillId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Remove all additional DDAT Framework Skills if none selected
            _context.UserDdatFrameworkSkills.RemoveRange(profile.AdditionalDdatFrameworkSkills);
        }

        // Update capability gaps
        if (capabilityGapDescriptions != null && capabilityGapIds != null)
        {
            // Update existing gaps
            for (int i = 0; i < capabilityGapIds.Length && i < capabilityGapDescriptions.Length; i++)
            {
                var gapId = capabilityGapIds[i];
                var description = capabilityGapDescriptions[i]?.Trim();
                
                if (gapId > 0 && !string.IsNullOrEmpty(description))
                {
                    var gap = profile.CapabilityGaps.FirstOrDefault(cg => cg.Id == gapId);
                    if (gap != null)
                    {
                        gap.Description = description;
                        gap.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            // Add new gaps (where gapId is 0 or negative)
            for (int i = 0; i < capabilityGapDescriptions.Length; i++)
            {
                var description = capabilityGapDescriptions[i]?.Trim();
                if (i >= capabilityGapIds.Length || capabilityGapIds[i] <= 0)
                {
                    if (!string.IsNullOrEmpty(description))
                    {
                        profile.CapabilityGaps.Add(new CapabilityGap
                        {
                            UserProfessionalProfileId = profile.Id,
                            Description = description,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // Remove gaps that are no longer in the list
            var submittedGapIds = capabilityGapIds.Where(id => id > 0).ToList();
            var gapsToRemove = profile.CapabilityGaps
                .Where(cg => !submittedGapIds.Contains(cg.Id))
                .ToList();
            foreach (var gapToRemove in gapsToRemove)
            {
                _context.CapabilityGaps.Remove(gapToRemove);
            }
        }
        else
        {
            // Remove all gaps if none submitted
            _context.CapabilityGaps.RemoveRange(profile.CapabilityGaps);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Profile updated successfully";
        return RedirectToAction(nameof(Index));
    }


    /// <summary>
    /// Update profession and skills - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfessionAndSkills(int? professionId, int[] skillIds)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _context.UserProfessionalProfiles
            .Include(p => p.UserSkills)
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        if (profile == null)
        {
            profile = new UserProfessionalProfile
            {
                UserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserProfessionalProfiles.Add(profile);
        }

        // Update profession
        profile.DdatProfessionId = professionId;
        
        // If profession is set, also update the legacy Profession field for backward compatibility
        if (professionId.HasValue)
        {
            var profession = await _context.DdatProfessions.FindAsync(professionId.Value);
            if (profession != null)
            {
                profile.Profession = profession.Name;
            }
        }
        else
        {
            profile.Profession = null;
        }

        // Get Head of Profession for the selected profession
        if (professionId.HasValue)
        {
            var hop = await _context.HOPS
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.DdatProfessionId == professionId.Value);
            
            if (hop?.User != null)
            {
                profile.HeadOfProfessionId = hop.User.Email;
            }
            else
            {
                profile.HeadOfProfessionId = null;
            }
        }
        else
        {
            profile.HeadOfProfessionId = null;
        }

        // Update skills
        // Remove existing skill assignments
        var existingSkills = profile.UserSkills.ToList();
        foreach (var existingSkill in existingSkills)
        {
            _context.UserProfessionalProfileSkills.Remove(existingSkill);
        }

        // Add new skill assignments
        if (skillIds != null && skillIds.Length > 0)
        {
            foreach (var skillId in skillIds)
            {
                var skill = await _context.Skills.FindAsync(skillId);
                if (skill != null)
                {
                    profile.UserSkills.Add(new UserProfessionalProfileSkill
                    {
                        UserProfessionalProfileId = profile.Id,
                        SkillId = skillId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your profession and skills have been updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Get skills for a profession (AJAX endpoint)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSkillsForProfession(int professionId)
    {
        var skills = await _context.ProfessionSkills
            .Where(ps => ps.DdatProfessionId == professionId && ps.Skill.IsActive)
            .Include(ps => ps.Skill)
            .Select(ps => new
            {
                id = ps.Skill.Id,
                skillName = ps.Skill.SkillName,
                description = ps.Skill.Description,
                category = ps.Skill.Category
            })
            .OrderBy(s => s.skillName)
            .ToListAsync();

        // Get Head of Profession
        var hop = await _context.HOPS
            .Include(h => h.User)
            .Where(h => h.DdatProfessionId == professionId)
            .Select(h => new
            {
                name = h.User != null ? h.User.Name : "Unknown",
                email = h.User != null ? h.User.Email : ""
            })
            .FirstOrDefaultAsync();

        return Json(new
        {
            skills = skills,
            headOfProfession = hop
        });
    }

    /// <summary>
    /// Get role families from active DDAT Framework version
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDdatRoleFamilies()
    {
        var activeVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);

        if (activeVersion == null)
        {
            return Json(new List<object>());
        }

        var roleFamilies = await _context.DdatFrameworkRoles
            .Where(r => r.FrameworkVersionId == activeVersion.Id && !r.IsArchived)
            .Select(r => r.RoleFamily)
            .Distinct()
            .OrderBy(rf => rf)
            .ToListAsync();

        return Json(roleFamilies);
    }

    /// <summary>
    /// Get roles for a specific role family
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDdatRolesByFamily(string roleFamily)
    {
        var activeVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);

        if (activeVersion == null)
        {
            return Json(new List<object>());
        }

        var roles = await _context.DdatFrameworkRoles
            .Where(r => r.FrameworkVersionId == activeVersion.Id && 
                       r.RoleFamily == roleFamily && 
                       !r.IsArchived)
            .Select(r => r.Role)
            .Distinct()
            .OrderBy(r => r)
            .ToListAsync();

        return Json(roles);
    }

    /// <summary>
    /// Get role levels for a specific role
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDdatRoleLevelsByRole(string roleFamily, string role)
    {
        var activeVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);

        if (activeVersion == null)
        {
            return Json(new List<object>());
        }

        var roleLevels = await _context.DdatFrameworkRoles
            .Where(r => r.FrameworkVersionId == activeVersion.Id && 
                       r.RoleFamily == roleFamily && 
                       r.Role == role && 
                       !r.IsArchived)
            .Select(r => new { r.Id, r.RoleLevel, r.RoleLevelDescription })
            .OrderBy(r => r.RoleLevel)
            .ToListAsync();

        return Json(roleLevels);
    }

    /// <summary>
    /// Get skills required for a specific role level with capability levels
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDdatSkillsForRole(int roleId, string? grade = null)
    {
        var role = await _context.DdatFrameworkRoles
            .Include(r => r.RoleSkills)
            .FirstOrDefaultAsync(r => r.Id == roleId);

        if (role == null)
        {
            return Json(new List<object>());
        }

        var skills = new List<object>();
        foreach (var roleSkill in role.RoleSkills.OrderBy(rs => rs.SkillName))
        {
            object skillInfo;

            // If grade is provided, get the expected capability level from grade mappings
            if (!string.IsNullOrWhiteSpace(grade))
            {
                var frameworkSkill = await _context.DdatFrameworkSkills
                    .Include(s => s.GradeMappings)
                    .FirstOrDefaultAsync(s => s.SkillName == roleSkill.SkillName && 
                                            s.FrameworkVersionId == role.FrameworkVersionId);

                if (frameworkSkill != null)
                {
                    var gradeMapping = frameworkSkill.GradeMappings
                        .FirstOrDefault(gm => gm.Grade == grade && 
                                             gm.CapabilityLevel.Equals(roleSkill.SkillLevel, StringComparison.OrdinalIgnoreCase));

                    if (gradeMapping != null)
                    {
                        skillInfo = new
                        {
                            skillName = roleSkill.SkillName,
                            skillDescription = roleSkill.SkillDescription,
                            requiredLevel = roleSkill.SkillLevel,
                            levelDescription = roleSkill.SkillLevelDescription,
                            expectedCapabilityLevel = gradeMapping.CapabilityLevel
                        };
                    }
                    else
                    {
                        skillInfo = new
                        {
                            skillName = roleSkill.SkillName,
                            skillDescription = roleSkill.SkillDescription,
                            requiredLevel = roleSkill.SkillLevel,
                            levelDescription = roleSkill.SkillLevelDescription
                        };
                    }
                }
                else
                {
                    skillInfo = new
                    {
                        skillName = roleSkill.SkillName,
                        skillDescription = roleSkill.SkillDescription,
                        requiredLevel = roleSkill.SkillLevel,
                        levelDescription = roleSkill.SkillLevelDescription
                    };
                }
            }
            else
            {
                skillInfo = new
                {
                    skillName = roleSkill.SkillName,
                    skillDescription = roleSkill.SkillDescription,
                    requiredLevel = roleSkill.SkillLevel,
                    levelDescription = roleSkill.SkillLevelDescription
                };
            }

            skills.Add(skillInfo);
        }

        return Json(skills);
    }

    /// <summary>
    /// Get all DDAT Framework Skills for additional skills selection
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDdatFrameworkSkills()
    {
        var activeVersion = await _context.DdatFrameworkVersions
            .FirstOrDefaultAsync(v => v.IsActive);

        if (activeVersion == null)
        {
            return Json(new List<object>());
        }

        var skills = await _context.DdatFrameworkSkills
            .Where(s => s.FrameworkVersionId == activeVersion.Id && !s.IsArchived)
            .Select(s => new { s.Id, s.SkillName, s.SkillDescription })
            .OrderBy(s => s.SkillName)
            .ToListAsync();

        return Json(skills);
    }

    #region Role Helper Methods

    /// <summary>
    /// Get current user email from claims
    /// </summary>
    private string? GetCurrentUserEmail()
    {
        return User.Identity?.Name
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Check if user is in Learning and Skills role
    /// </summary>
    private async Task<bool> IsLearningAndSkillsRoleAsync()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email)) return false;
        return await _permissionService.IsInGroupAsync(email, "Learning and skills");
    }

    /// <summary>
    /// Check if user is HOP or Central Ops Admin
    /// </summary>
    private async Task<bool> IsHOPOrCentralOpsAsync()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email)) return false;
        return await _permissionService.IsInGroupAsync(email, "HOP") ||
               await _permissionService.IsInGroupAsync(email, "Central Operations Admin");
    }

    /// <summary>
    /// Check if user is Central Ops Admin
    /// </summary>
    private async Task<bool> IsCentralOpsAdminAsync()
    {
        var email = GetCurrentUserEmail();
        if (string.IsNullOrEmpty(email)) return false;
        return await _permissionService.IsInGroupAsync(email, "Central Operations Admin");
    }

    /// <summary>
    /// Get user's profession scope (for HOP users)
    /// </summary>
    private async Task<List<string>> GetUserProfessionScopeAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue) return new List<string>();

        var professions = await _context.HOPS
            .Include(h => h.DdatProfession)
            .Where(h => h.UserId == userId.Value)
            .Select(h => h.DdatProfession != null ? h.DdatProfession.Name : "")
            .Where(p => !string.IsNullOrEmpty(p))
            .ToListAsync();

        return professions;
    }

    private (DateTime yearStart, DateTime yearEnd) GetFinancialYearDates(int year)
    {
        // UK financial year starts 1st April
        var yearStart = new DateTime(year, 4, 1);
        var yearEnd = yearStart.AddYears(1).AddDays(-1);
        return (yearStart, yearEnd);
    }

    #endregion

    #region Learning and Skills Role Actions (Approvers)


    /// <summary>
    /// View all training requests with filters (Learning & Skills role)
    /// </summary>
    public async Task<IActionResult> AdminRequests(
        string? statusFilter,
        string? professionFilter,
        string? search)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var query = _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(search)) ||
                (tr.User != null && tr.User.Email.Contains(search)) ||
                (tr.Course != null && tr.Course.Title.Contains(search)) ||
                (tr.CustomCourseTitle != null && tr.CustomCourseTitle.Contains(search)));
        }

        if (!string.IsNullOrEmpty(professionFilter))
        {
            var userIds = await _context.UserProfessionalProfiles
                .Where(upp => upp.Profession == professionFilter)
                .Select(upp => upp.UserId)
                .ToListAsync();

            query = query.Where(tr => userIds.Contains(tr.UserId));
        }

        var requests = await query
            .Where(tr => !tr.WithdrawalRequested) // Exclude withdrawal requests from main list
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        // Get withdrawal requests separately
        var withdrawalRequests = await _context.TrainingRequests
            .Where(tr => tr.WithdrawalRequested)
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .OrderByDescending(tr => tr.WithdrawalRequestedAt)
            .ToListAsync();

        // Calculate status counts (excluding withdrawal requests)
        var baseQuery = _context.TrainingRequests.Where(tr => !tr.WithdrawalRequested);
        if (!string.IsNullOrEmpty(search))
        {
            baseQuery = baseQuery.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(search)) ||
                (tr.User != null && tr.User.Email.Contains(search)) ||
                (tr.Course != null && tr.Course.Title.Contains(search)) ||
                (tr.CustomCourseTitle != null && tr.CustomCourseTitle.Contains(search)));
        }
        if (!string.IsNullOrEmpty(professionFilter))
        {
            var userIds = await _context.UserProfessionalProfiles
                .Where(upp => upp.Profession == professionFilter)
                .Select(upp => upp.UserId)
                .ToListAsync();
            baseQuery = baseQuery.Where(tr => userIds.Contains(tr.UserId));
        }

        ViewBag.StatusFilter = statusFilter;
        ViewBag.ProfessionFilter = professionFilter;
        ViewBag.Search = search;
        ViewBag.WithdrawalRequests = withdrawalRequests;
        ViewBag.SubmittedCount = await baseQuery.CountAsync(tr => tr.Status == "Submitted");
        ViewBag.ApprovedCount = await baseQuery.CountAsync(tr => tr.Status == "Approved");
        ViewBag.RejectedCount = await baseQuery.CountAsync(tr => tr.Status == "Rejected");
        ViewBag.CancelledCount = await baseQuery.CountAsync(tr => tr.Status == "Cancelled");
        ViewBag.CompletedCount = await baseQuery.CountAsync(tr => tr.Status == "Completed");
        ViewBag.OnHoldCount = await baseQuery.CountAsync(tr => tr.Status == "On-hold");

        return View("~/Views/LearningAndSkills/ProfessionRequests.cshtml", requests);
    }

    /// <summary>
    /// Alias for AdminRequests - View all training requests with filters (Learning & Skills role)
    /// </summary>
    public async Task<IActionResult> ProfessionRequests(
        string? statusFilter,
        string? professionFilter,
        string? search)
    {
        return await AdminRequests(statusFilter, professionFilter, search);
    }

    /// <summary>
    /// Approve a training request (Learning & Skills role)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id, string? comments)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Submitted")
        {
            TempData["ErrorMessage"] = "Only submitted requests can be approved";
            return RedirectToAction(nameof(AdminRequests));
        }

        var userEmail = GetCurrentUserEmail() ?? "System";

        request.Status = "Approved";
        request.ApprovedBy = userEmail;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApproverComments = comments;
        request.UpdatedAt = DateTime.UtcNow;

        var record = new TrainingRecord
        {
            UserId = request.UserId,
            CourseId = request.CourseId,
            Status = "Booked",
            DateRequested = request.CreatedAt,
            DateApproved = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TrainingRecords.Add(record);
        await _context.SaveChangesAsync();

        // Send notification
        if (_notificationService != null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    var subject = "Training request approved";
                    var body = $@"Your training request has been approved.

Course: {request.Course?.Title ?? request.CustomCourseTitle ?? "Custom course"}
Requested: {request.CreatedAt:dd MMM yyyy}
Approved by: {userEmail}
{(string.IsNullOrEmpty(comments) ? "" : $"\nComments: {comments}")}

You can view your training requests at: {Url.Action("MyRequests", "SkillsAndLearning", null, Request.Scheme)}";

                    await _notificationService.SendEmailAsync(
                        user.Email,
                        subject,
                        body,
                        triggerCode: "training_request_approved",
                        contextData: new Dictionary<string, object> { { "requestId", request.Id } });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send approval notification to {Email}", user.Email);
                }
            }
        }

        TempData["SuccessMessage"] = "Training request approved successfully";
        return RedirectToAction(nameof(AdminRequests));
    }

    /// <summary>
    /// Reject a training request (Learning & Skills role)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, string? comments)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        if (request.Status != "Submitted")
        {
            TempData["ErrorMessage"] = "Only submitted requests can be rejected";
            return RedirectToAction(nameof(AdminRequests));
        }

        var userEmail = GetCurrentUserEmail() ?? "System";

        request.Status = "Rejected";
        request.ApprovedBy = userEmail;
        request.ApprovedAt = DateTime.UtcNow;
        request.ApproverComments = comments;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send notification
        if (_notificationService != null)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                try
                {
                    var subject = "Training request rejected";
                    var body = $@"Your training request has been rejected.

Course: {request.Course?.Title ?? request.CustomCourseTitle ?? "Custom course"}
Requested: {request.CreatedAt:dd MMM yyyy}
Rejected by: {userEmail}
Reason: {comments ?? "No reason provided"}

You can view your training requests at: {Url.Action("MyRequests", "SkillsAndLearning", null, Request.Scheme)}";

                    await _notificationService.SendEmailAsync(
                        user.Email,
                        subject,
                        body,
                        triggerCode: "training_request_rejected",
                        contextData: new Dictionary<string, object> { { "requestId", request.Id } });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send rejection notification to {Email}", user.Email);
                }
            }
        }

        TempData["SuccessMessage"] = "Training request rejected";
        return RedirectToAction(nameof(AdminRequests));
    }

    /// <summary>
    /// View form to update request details (actual cost, planned date, completion) - Learning & Skills role
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UpdateRequestDetails(int id)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        return View("~/Views/LearningAndSkills/UpdateRequestDetails.cshtml", request);
    }

    /// <summary>
    /// Update request details (actual cost, planned date, completion) - Learning & Skills role
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRequestDetails(
        int id,
        decimal? actualCost,
        DateTime? plannedDate,
        string? trainingCompleted,
        DateTime? completedDate)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var request = await _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .FirstOrDefaultAsync(tr => tr.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        // Update actual cost
        request.ActualCost = actualCost;

        // Update planned date
        request.PlannedDate = plannedDate;

        // Calculate and set FinancialYear based on PlannedDate (or CreatedAt if no PlannedDate)
        if (plannedDate.HasValue)
        {
            var date = plannedDate.Value;
            request.FinancialYear = date.Month >= 4 ? date.Year : date.Year - 1;
        }
        else
        {
            var createdDate = request.CreatedAt;
            request.FinancialYear = createdDate.Month >= 4 ? createdDate.Year : createdDate.Year - 1;
        }

        // Update training completion status
        if (string.IsNullOrEmpty(trainingCompleted))
        {
            request.TrainingCompleted = null;
        }
        else
        {
            request.TrainingCompleted = bool.Parse(trainingCompleted);
        }

        // Update completed date (only if training was completed)
        if (request.TrainingCompleted == true)
        {
            request.CompletedDate = completedDate ?? plannedDate; // Use planned date if completed date not provided
        }
        else
        {
            request.CompletedDate = null;
        }

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Request details updated successfully";
        return RedirectToAction(nameof(AdminRequests));
    }

    /// <summary>
    /// View all learning records (Learning & Skills role)
    /// </summary>
    public async Task<IActionResult> AdminLearning(
        string? statusFilter,
        string? professionFilter,
        string? search)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var query = _context.TrainingRecords
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(search)) ||
                (tr.User != null && tr.User.Email.Contains(search)) ||
                (tr.Course != null && tr.Course.Title.Contains(search)));
        }

        if (!string.IsNullOrEmpty(professionFilter))
        {
            var userIds = await _context.UserProfessionalProfiles
                .Where(upp => upp.Profession == professionFilter)
                .Select(upp => upp.UserId)
                .ToListAsync();

            query = query.Where(tr => userIds.Contains(tr.UserId));
        }

        var records = await query
            .OrderByDescending(tr => tr.DateAttended ?? tr.DateRequested)
            .ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.ProfessionFilter = professionFilter;
        ViewBag.Search = search;

        return View("~/Views/LearningAndSkills/ViewLearning.cshtml", records);
    }

    /// <summary>
    /// Export training requests to CSV (Learning & Skills role)
    /// </summary>
    public async Task<IActionResult> ExportRequests(
        string? statusFilter,
        string? professionFilter,
        string? search)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var query = _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        if (!string.IsNullOrEmpty(professionFilter))
        {
            query = query.Where(tr => tr.User != null && 
                _context.UserProfessionalProfiles.Any(upp => upp.UserId == tr.UserId && upp.Profession == professionFilter));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && (tr.User.Name.Contains(search) || tr.User.Email.Contains(search))) ||
                (tr.Course != null && tr.Course.Title.Contains(search)) ||
                (tr.CustomCourseTitle != null && tr.CustomCourseTitle.Contains(search)));
        }

        var requests = await query
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Training Requests Export");
        csv.AppendLine($"Export Date,{DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        csv.AppendLine();
        csv.AppendLine("Request ID,User Name,User Email,Course,Status,Justification,Profession Alignment,Requested Date,Approved Date,Approved By,Approver Comments");

        foreach (var request in requests)
        {
            var escapeCsv = new Func<string?, string>(s =>
            {
                if (string.IsNullOrEmpty(s)) return "";
                if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                {
                    return "\"" + s.Replace("\"", "\"\"") + "\"";
                }
                return s;
            });

            csv.AppendLine($"{request.Id}," +
                $"{escapeCsv(request.User?.Name)}," +
                $"{escapeCsv(request.User?.Email)}," +
                $"{escapeCsv(request.Course?.Title ?? request.CustomCourseTitle)}," +
                $"{request.Status}," +
                $"{escapeCsv(request.Justification)}," +
                $"{escapeCsv(request.ProfessionAlignment)}," +
                $"{request.CreatedAt:yyyy-MM-dd HH:mm}," +
                $"{(request.ApprovedAt.HasValue ? request.ApprovedAt.Value.ToString("yyyy-MM-dd HH:mm") : "")}," +
                $"{escapeCsv(request.ApprovedBy)}," +
                $"{escapeCsv(request.ApproverComments)}");
        }

        var fileName = $"Training_Requests_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var fileBytes = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, fileBytes, bom.Length, bytes.Length);

        return File(fileBytes, "text/csv", fileName);
    }

    /// <summary>
    /// Export learning records to CSV (Learning & Skills role)
    /// </summary>
    public async Task<IActionResult> ExportLearning(
        string? statusFilter,
        string? professionFilter,
        string? search)
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var query = _context.TrainingRecords
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        if (!string.IsNullOrEmpty(professionFilter))
        {
            query = query.Where(tr => tr.User != null && 
                _context.UserProfessionalProfiles.Any(upp => upp.UserId == tr.UserId && upp.Profession == professionFilter));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && (tr.User.Name.Contains(search) || tr.User.Email.Contains(search))) ||
                (tr.Course != null && tr.Course.Title.Contains(search)));
        }

        var records = await query
            .OrderByDescending(tr => tr.DateAttended ?? tr.DateRequested)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Learning Records Export");
        csv.AppendLine($"Export Date,{DateTime.UtcNow:yyyy-MM-dd HH:mm}");
        csv.AppendLine();
        csv.AppendLine("Record ID,User Name,User Email,Course,Status,Date Requested,Date Approved,Date Attended,Rating,Feedback,Actual Cost");

        foreach (var record in records)
        {
            var escapeCsv = new Func<string?, string>(s =>
            {
                if (string.IsNullOrEmpty(s)) return "";
                if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                {
                    return "\"" + s.Replace("\"", "\"\"") + "\"";
                }
                return s;
            });

            csv.AppendLine($"{record.Id}," +
                $"{escapeCsv(record.User?.Name)}," +
                $"{escapeCsv(record.User?.Email)}," +
                $"{escapeCsv(record.Course?.Title)}," +
                $"{record.Status}," +
                $"{(record.DateRequested.HasValue ? record.DateRequested.Value.ToString("yyyy-MM-dd") : "")}," +
                $"{(record.DateApproved.HasValue ? record.DateApproved.Value.ToString("yyyy-MM-dd") : "")}," +
                $"{(record.DateAttended.HasValue ? record.DateAttended.Value.ToString("yyyy-MM-dd") : "")}," +
                $"{(record.OutcomeRating.HasValue ? record.OutcomeRating.Value.ToString() : "")}," +
                $"{escapeCsv(record.Feedback)}," +
                $"{(record.CostActual.HasValue ? record.CostActual.Value.ToString("F2") : "")}");
        }

        var fileName = $"Learning_Records_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var fileBytes = new byte[bom.Length + bytes.Length];
        Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
        Buffer.BlockCopy(bytes, 0, fileBytes, bom.Length, bytes.Length);

        return File(fileBytes, "text/csv", fileName);
    }

    #endregion

    #region HOP / Central Ops Admin Actions

    /// <summary>
    /// Dashboard for HOP/Central Ops Admin
    /// </summary>
    public async Task<IActionResult> HOPDashboard(string? statusFilter)
    {
        if (!await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var professionScope = await GetUserProfessionScopeAsync();
        var isCentralOps = await IsCentralOpsAdminAsync();

        var userIds = new List<int>();
        if (professionScope.Any() && !isCentralOps)
        {
            userIds = await _context.UserProfessionalProfiles
                .Include(upp => upp.DdatProfession)
                .Where(upp => professionScope.Contains(upp.Profession ?? "") || 
                             (upp.DdatProfession != null && professionScope.Contains(upp.DdatProfession.Name)))
                .Select(upp => upp.UserId)
                .ToListAsync();
        }

        var baseQuery = _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();
        
        if (userIds.Any() && !isCentralOps)
        {
            baseQuery = baseQuery.Where(tr => userIds.Contains(tr.UserId));
        }

        // Apply status filter
        var query = baseQuery.AsQueryable();
        if (!string.IsNullOrEmpty(statusFilter))
        {
            switch (statusFilter)
            {
                case "New":
                    query = query.Where(tr => tr.Status == "Submitted");
                    break;
                case "Approved":
                    query = query.Where(tr => tr.Status == "Approved" && (tr.TrainingCompleted != true));
                    break;
                case "Rejected":
                    query = query.Where(tr => tr.Status == "Rejected");
                    break;
                case "Completed":
                    query = query.Where(tr => tr.Status == "Approved" && tr.TrainingCompleted == true);
                    break;
                case "Cancelled":
                    // Get cancelled training records and their related requests
                    var cancelledUserIds = await _context.TrainingRecords
                        .Where(tr => tr.Status == "Cancelled" && (userIds.Any() && !isCentralOps ? userIds.Contains(tr.UserId) : true))
                        .Select(tr => tr.UserId)
                        .Distinct()
                        .ToListAsync();
                    var cancelledCourseIds = await _context.TrainingRecords
                        .Where(tr => tr.Status == "Cancelled" && (userIds.Any() && !isCentralOps ? userIds.Contains(tr.UserId) : true))
                        .Select(tr => tr.CourseId)
                        .Distinct()
                        .ToListAsync();
                    query = query.Where(tr => 
                        (cancelledUserIds.Contains(tr.UserId) && cancelledCourseIds.Contains(tr.CourseId)) ||
                        tr.WithdrawalRequested);
                    break;
            }
        }
        else
        {
            // Default to "New" if no filter specified
            query = query.Where(tr => tr.Status == "Submitted");
            statusFilter = "New";
        }

        var requests = await query
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        // Get counts for each status
        var totalRequests = await baseQuery.CountAsync();
        var newCount = await baseQuery.CountAsync(tr => tr.Status == "Submitted");
        var approvedCount = await baseQuery.CountAsync(tr => tr.Status == "Approved" && (tr.TrainingCompleted != true));
        var rejectedCount = await baseQuery.CountAsync(tr => tr.Status == "Rejected");
        var completedCount = await baseQuery.CountAsync(tr => tr.Status == "Approved" && tr.TrainingCompleted == true);
        
        var cancelledCountUserIds = await _context.TrainingRecords
            .Where(tr => tr.Status == "Cancelled" && (userIds.Any() && !isCentralOps ? userIds.Contains(tr.UserId) : true))
            .Select(tr => tr.UserId)
            .Distinct()
            .ToListAsync();
        var cancelledCountCourseIds = await _context.TrainingRecords
            .Where(tr => tr.Status == "Cancelled" && (userIds.Any() && !isCentralOps ? userIds.Contains(tr.UserId) : true))
            .Select(tr => tr.CourseId)
            .Distinct()
            .ToListAsync();
        var cancelledCount = await baseQuery.CountAsync(tr => 
            (cancelledCountUserIds.Contains(tr.UserId) && cancelledCountCourseIds.Contains(tr.CourseId)) ||
            tr.WithdrawalRequested);

        var recordsQuery = _context.TrainingRecords.AsQueryable();
        if (userIds.Any() && !isCentralOps)
        {
            recordsQuery = recordsQuery.Where(tr => userIds.Contains(tr.UserId));
        }

        var totalSpent = await recordsQuery
            .Where(tr => tr.CostActual.HasValue)
            .SumAsync(tr => tr.CostActual ?? 0);

        var estimatedCostRequests = await baseQuery
            .Where(tr => tr.Status == "Approved" || tr.Status == "Submitted")
            .ToListAsync();
        
        var estimatedCost = estimatedCostRequests.Sum(tr => 
            tr.Course?.Cost ?? tr.CustomCourseCost ?? 0);

        ViewBag.TotalRequests = totalRequests;
        ViewBag.PendingRequests = newCount;
        ViewBag.ApprovedRequests = approvedCount;
        ViewBag.TotalSpent = totalSpent;
        ViewBag.EstimatedCost = estimatedCost;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.NewCount = newCount;
        ViewBag.ApprovedCount = approvedCount;
        ViewBag.RejectedCount = rejectedCount;
        ViewBag.CompletedCount = completedCount;
        ViewBag.CancelledCount = cancelledCount;
        ViewBag.ProfessionScope = professionScope;
        ViewBag.IsCentralOps = isCentralOps;

        return View("~/Views/HOP/Index.cshtml", requests);
    }

    /// <summary>
    /// View profession requests for users in my profession scope (HOP)
    /// </summary>
    public async Task<IActionResult> HOPRequests(
        string? statusFilter,
        string? search,
        string? nameFilter,
        string? courseFilter)
    {
        if (!await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var professionScope = await GetUserProfessionScopeAsync();
        var isCentralOps = await IsCentralOpsAdminAsync();

        var userIds = new List<int>();
        if (professionScope.Any() && !isCentralOps)
        {
            userIds = await _context.UserProfessionalProfiles
                .Include(upp => upp.DdatProfession)
                .Where(upp => professionScope.Contains(upp.Profession ?? "") || 
                             (upp.DdatProfession != null && professionScope.Contains(upp.DdatProfession.Name)))
                .Select(upp => upp.UserId)
                .ToListAsync();
        }

        var query = _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (userIds.Any() && !isCentralOps)
        {
            query = query.Where(tr => userIds.Contains(tr.UserId));
        }

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        // Legacy search filter (for backward compatibility)
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(search)) ||
                (tr.User != null && tr.User.Email.Contains(search)) ||
                (tr.Course != null && tr.Course.Title.Contains(search)) ||
                (tr.CustomCourseTitle != null && tr.CustomCourseTitle.Contains(search)));
        }

        // Name filter
        if (!string.IsNullOrEmpty(nameFilter))
        {
            query = query.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(nameFilter)) ||
                (tr.User != null && tr.User.Email.Contains(nameFilter)));
        }

        // Course filter
        if (!string.IsNullOrEmpty(courseFilter))
        {
            query = query.Where(tr => 
                (tr.Course != null && tr.Course.Title.Contains(courseFilter)) ||
                (tr.CustomCourseTitle != null && tr.CustomCourseTitle.Contains(courseFilter)));
        }

        var requests = await query
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.Search = search;
        ViewBag.NameFilter = nameFilter;
        ViewBag.CourseFilter = courseFilter;
        ViewBag.ProfessionScope = professionScope;

        return View("~/Views/HOP/ProfessionRequests.cshtml", requests);
    }

    /// <summary>
    /// Alias for HOPRequests - View profession requests (HOP/Central Ops Admin)
    /// </summary>
    public async Task<IActionResult> ProfessionRequests(
        string? statusFilter,
        string? search,
        string? nameFilter,
        string? courseFilter)
    {
        return await HOPRequests(statusFilter, search, nameFilter, courseFilter);
    }

    /// <summary>
    /// View user training history scoped to profession (HOP/Central Ops Admin)
    /// </summary>
    public async Task<IActionResult> UserHistory(string? search)
    {
        if (!await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var professionScope = await GetUserProfessionScopeAsync();
        var isCentralOps = await IsCentralOpsAdminAsync();

        var userIds = new List<int>();
        if (professionScope.Any() && !isCentralOps)
        {
            userIds = await _context.UserProfessionalProfiles
                .Include(upp => upp.DdatProfession)
                .Where(upp => professionScope.Contains(upp.Profession ?? "") || 
                             (upp.DdatProfession != null && professionScope.Contains(upp.DdatProfession.Name)))
                .Select(upp => upp.UserId)
                .ToListAsync();
        }

        var query = _context.TrainingRecords
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (userIds.Any() && !isCentralOps)
        {
            query = query.Where(tr => userIds.Contains(tr.UserId));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(search)) ||
                (tr.User != null && tr.User.Email.Contains(search)) ||
                (tr.Course != null && tr.Course.Title.Contains(search)));
        }

        var records = await query
            .OrderByDescending(tr => tr.DateAttended ?? tr.DateRequested)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.ProfessionScope = professionScope;

        return View("~/Views/HOP/UserHistory.cshtml", records);
    }

    /// <summary>
    /// Budget and spending dashboard (HOP/Central Ops Admin)
    /// </summary>
    public async Task<IActionResult> BudgetAndSpending(int? financialYear = null)
    {
        if (!await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var professionScope = await GetUserProfessionScopeAsync();
        var isCentralOps = await IsCentralOpsAdminAsync();

        // Calculate financial year if not provided (UK FY starts 1st April)
        if (!financialYear.HasValue)
        {
            var now = DateTime.UtcNow;
            financialYear = now.Month >= 4 ? now.Year : now.Year - 1;
        }

        var selectedFinancialYear = financialYear.Value;
        var (yearStart, yearEnd) = GetFinancialYearDates(selectedFinancialYear);

        var userIds = new List<int>();
        if (professionScope.Any() && !isCentralOps)
        {
            userIds = await _context.UserProfessionalProfiles
                .Where(upp => professionScope.Contains(upp.Profession ?? ""))
                .Select(upp => upp.UserId)
                .ToListAsync();
        }

        // Helper function to get financial year from TrainingRequest
        Func<TrainingRequest, int> getFinancialYear = tr =>
        {
            if (tr.FinancialYear.HasValue)
                return tr.FinancialYear.Value;
            if (tr.PlannedDate.HasValue)
            {
                var date = tr.PlannedDate.Value;
                return date.Month >= 4 ? date.Year : date.Year - 1;
            }
            var createdDate = tr.CreatedAt;
            return createdDate.Month >= 4 ? createdDate.Year : createdDate.Year - 1;
        };

        // Get spending by provider from TrainingRequests with ActualCost (for completed training) - filtered by FY
        var allRequestsWithCost = await _context.TrainingRequests
            .Where(tr => tr.ActualCost.HasValue && 
                       tr.ActualCost.Value > 0 &&
                       (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .Include(tr => tr.Course)
            .ToListAsync();

        var spendingByProvider = allRequestsWithCost
            .Where(tr => getFinancialYear(tr) == selectedFinancialYear)
            .GroupBy(tr => tr.Course != null && !string.IsNullOrEmpty(tr.Course.Provider) 
                ? tr.Course.Provider 
                : (!string.IsNullOrEmpty(tr.CustomCourseProvider) ? tr.CustomCourseProvider : "Unknown"))
            .Select(g => new
            {
                Provider = g.Key ?? "Unknown",
                TotalSpent = g.Sum(tr => tr.ActualCost ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

        // Get spending by profession from TrainingRequests with ActualCost - filtered by FY
        var allRequestsForProfession = await _context.TrainingRequests
            .Where(tr => tr.ActualCost.HasValue && 
                       tr.ActualCost.Value > 0 &&
                       (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .Include(tr => tr.User)
            .ToListAsync();

        var userProfiles = await _context.UserProfessionalProfiles
            .Include(upp => upp.DdatProfession)
            .Where(upp => allRequestsForProfession.Select(tr => tr.UserId).Contains(upp.UserId))
            .ToListAsync();

        var spendingByProfession = allRequestsForProfession
            .Where(tr => getFinancialYear(tr) == selectedFinancialYear)
            .Join(userProfiles,
                tr => tr.UserId,
                upp => upp.UserId,
                (tr, upp) => new { tr, upp.Profession, upp.DdatProfession })
            .GroupBy(x => x.DdatProfession != null ? x.DdatProfession.Name : (x.Profession ?? "Unknown"))
            .Select(g => new
            {
                Profession = g.Key ?? "Unknown",
                TotalSpent = g.Sum(x => x.tr.ActualCost ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

        // Calculate forecasted costs - filtered by FY
        var allForecastRequests = await _context.TrainingRequests
            .Where(tr => (tr.Status == "Approved" || tr.Status == "Submitted") && 
                        (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .Include(tr => tr.Course)
            .ToListAsync();

        var forecastedCosts = allForecastRequests
            .Where(tr => getFinancialYear(tr) == selectedFinancialYear)
            .Sum(tr => tr.Course?.Cost ?? tr.CustomCourseCost ?? 0);

        // Calculate actual spent - filtered by FY
        var actualSpent = allRequestsWithCost
            .Where(tr => getFinancialYear(tr) == selectedFinancialYear)
            .Sum(tr => tr.ActualCost ?? 0);

        // Load budget for the selected financial year
        var currentBudget = await _context.LearningBudgets
            .FirstOrDefaultAsync(lb => lb.FinancialYear == selectedFinancialYear && lb.IsActive);

        // Calculate current and next FY for navigation
        var currentDate = DateTime.UtcNow;
        var currentFY = currentDate.Month >= 4 ? currentDate.Year : currentDate.Year - 1;
        var nextFY = currentFY + 1;

        ViewBag.SpendingByProvider = spendingByProvider;
        ViewBag.SpendingByProfession = spendingByProfession;
        ViewBag.ForecastedCosts = forecastedCosts;
        ViewBag.ActualSpent = actualSpent;
        ViewBag.ProfessionScope = professionScope;
        ViewBag.IsCentralOps = isCentralOps;
        ViewBag.CurrentBudget = currentBudget;
        ViewBag.SelectedFinancialYear = selectedFinancialYear;
        ViewBag.CurrentFY = currentFY;
        ViewBag.NextFY = nextFY;

        return View("~/Views/HOP/BudgetAndSpending.cshtml");
    }

    /// <summary>
    /// Manage budget (Central Ops Admin only) - List all budgets and allow editing
    /// </summary>
    public async Task<IActionResult> ManageBudget(int? financialYear = null)
    {
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

        // Get all budgets
        var allBudgets = await _context.LearningBudgets
            .OrderByDescending(lb => lb.FinancialYear)
            .ToListAsync();

        // Calculate current FY
        var now = DateTime.UtcNow;
        var currentFY = now.Month >= 4 ? now.Year : now.Year - 1;

        // If no financialYear specified, use current FY
        if (!financialYear.HasValue)
        {
            financialYear = currentFY;
        }

        var selectedFY = financialYear.Value;

        // Get or create budget for selected FY
        var budget = allBudgets.FirstOrDefault(lb => lb.FinancialYear == selectedFY && lb.IsActive);
        
        if (budget == null)
        {
            budget = new LearningBudget
            {
                FinancialYear = selectedFY,
                TotalBudget = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LearningBudgets.Add(budget);
            await _context.SaveChangesAsync();
            allBudgets.Add(budget);
        }

        // Calculate spending for selected FY
        Func<TrainingRequest, int> getFinancialYear = tr =>
        {
            if (tr.FinancialYear.HasValue)
                return tr.FinancialYear.Value;
            if (tr.PlannedDate.HasValue)
            {
                var date = tr.PlannedDate.Value;
                return date.Month >= 4 ? date.Year : date.Year - 1;
            }
            var createdDate = tr.CreatedAt;
            return createdDate.Month >= 4 ? createdDate.Year : createdDate.Year - 1;
        };

        var allRequestsWithCost = await _context.TrainingRequests
            .Where(tr => tr.ActualCost.HasValue && tr.ActualCost.Value > 0)
            .ToListAsync();

        var actualSpent = allRequestsWithCost
            .Where(tr => getFinancialYear(tr) == selectedFY)
            .Sum(tr => tr.ActualCost ?? 0);

        var allForecastRequests = await _context.TrainingRequests
            .Where(tr => tr.Status == "Approved" || tr.Status == "Submitted")
            .Include(tr => tr.Course)
            .ToListAsync();

        var forecastedCosts = allForecastRequests
            .Where(tr => getFinancialYear(tr) == selectedFY)
            .Sum(tr => tr.Course?.Cost ?? tr.CustomCourseCost ?? 0);

        budget.Spent = actualSpent;
        budget.Forecasted = forecastedCosts;
        await _context.SaveChangesAsync();

        ViewBag.AllBudgets = allBudgets;
        ViewBag.SelectedFinancialYear = selectedFY;
        ViewBag.CurrentFY = currentFY;
        ViewBag.NextFY = currentFY + 1;
        ViewBag.Remaining = budget.TotalBudget - (actualSpent + forecastedCosts);

        return View("~/Views/HOP/ManageBudget.cshtml", budget);
    }

    /// <summary>
    /// Create new budget (Central Ops Admin only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBudget(int financialYear, decimal totalBudget)
    {
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

        if (totalBudget < 0)
        {
            ModelState.AddModelError("TotalBudget", "Budget amount must be positive");
            return RedirectToAction(nameof(ManageBudget), new { financialYear });
        }

        // Check if budget already exists for this FY
        var existingBudget = await _context.LearningBudgets
            .FirstOrDefaultAsync(lb => lb.FinancialYear == financialYear && lb.IsActive);

        if (existingBudget != null)
        {
            TempData["ErrorMessage"] = $"A budget already exists for Financial Year {financialYear}/{financialYear + 1}";
            return RedirectToAction(nameof(ManageBudget), new { financialYear });
        }

        var userEmail = GetCurrentUserEmail() ?? "System";

        var budget = new LearningBudget
        {
            FinancialYear = financialYear,
            TotalBudget = totalBudget,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userEmail,
            UpdatedBy = userEmail
        };

        _context.LearningBudgets.Add(budget);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget for FY {financialYear}/{financialYear + 1} created: £{totalBudget:F2}";
        return RedirectToAction(nameof(ManageBudget), new { financialYear });
    }

    /// <summary>
    /// Update budget (Central Ops Admin only)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateBudget(int id, decimal totalBudget)
    {
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

        var budget = await _context.LearningBudgets
            .FirstOrDefaultAsync(lb => lb.Id == id && lb.IsActive);

        if (budget == null)
        {
            return NotFound();
        }

        if (totalBudget < 0)
        {
            ModelState.AddModelError("TotalBudget", "Budget amount must be positive");
            return RedirectToAction(nameof(ManageBudget), new { financialYear = budget.FinancialYear });
        }

        var userEmail = GetCurrentUserEmail() ?? "System";

        budget.TotalBudget = totalBudget;
        budget.UpdatedAt = DateTime.UtcNow;
        budget.UpdatedBy = userEmail;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget for FY {budget.FinancialYear}/{budget.FinancialYear + 1} updated to £{totalBudget:F2}";
        return RedirectToAction(nameof(ManageBudget), new { financialYear = budget.FinancialYear });
    }

    /// <summary>
    /// Identify department-wide capability trends (HOP)
    /// </summary>
    public async Task<IActionResult> CapabilityTrends()
    {
        if (!await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var professionScope = await GetUserProfessionScopeAsync();
        var isCentralOps = await IsCentralOpsAdminAsync();

        var gapsByProfession = await _context.UserProfessionalProfiles
            .Include(upp => upp.CapabilityGaps)
            .Where(upp => ((upp.CapabilityGaps != null && upp.CapabilityGaps.Any()) || !string.IsNullOrEmpty(upp.CapabilityGapsLegacy)) &&
                         (professionScope.Count == 0 || isCentralOps || professionScope.Contains(upp.Profession ?? "")))
            .GroupBy(upp => upp.Profession ?? "Unknown")
            .Select(g => new
            {
                Profession = g.Key ?? "Unknown",
                UserCount = g.Count(),
                UsersWithGaps = g.Count()
            })
            .ToListAsync();

        var trainingTrends = await _context.TrainingRecords
            .Where(tr => (professionScope.Count == 0 || isCentralOps || 
                         _context.UserProfessionalProfiles
                             .Where(upp => professionScope.Contains(upp.Profession ?? ""))
                             .Select(upp => upp.UserId)
                             .Contains(tr.UserId)))
            .Join(_context.UserProfessionalProfiles,
                tr => tr.UserId,
                upp => upp.UserId,
                (tr, upp) => new { tr, upp.Profession })
            .GroupBy(x => new { x.Profession, Year = x.tr.DateAttended.HasValue ? x.tr.DateAttended.Value.Year : DateTime.UtcNow.Year })
            .Select(g => new
            {
                Profession = g.Key.Profession ?? "Unknown",
                Year = g.Key.Year,
                Count = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Profession)
            .ToListAsync();

        ViewBag.GapsByProfession = gapsByProfession;
        ViewBag.TrainingTrends = trainingTrends;
        ViewBag.ProfessionScope = professionScope;

        return View("~/Views/HOP/CapabilityTrends.cshtml");
    }

    #endregion

    #region Reporting Actions

    /// <summary>
    /// Spend analysis report
    /// </summary>
    public async Task<IActionResult> SpendAnalysis(int? year)
    {
        if (!await IsLearningAndSkillsRoleAsync() && !await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var reportYear = year ?? DateTime.UtcNow.Year;
        var (yearStart, yearEnd) = GetFinancialYearDates(reportYear);

        var monthlySpend = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .GroupBy(tr => new { Year = tr.DateAttended.Value.Year, Month = tr.DateAttended.Value.Month })
            .Select(g => new
            {
                Month = g.Key.Month,
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                Spend = g.Sum(tr => tr.CostActual ?? 0),
            })
            .OrderBy(x => x.Month)
            .ToListAsync();

        var monthlySpendLabels = Enumerable.Range(1, 12).Select(m => new DateTime(reportYear, m, 1).ToString("MMM")).ToList();
        var monthlySpendData = new decimal[12];
        foreach (var item in monthlySpend)
        {
            monthlySpendData[item.Month - 1] = item.Spend;
        }
        ViewBag.MonthlySpendLabels = JsonSerializer.Serialize(monthlySpendLabels);
        ViewBag.MonthlySpendData = JsonSerializer.Serialize(monthlySpendData);

        var spendingByProfession = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .Join(_context.UserProfessionalProfiles,
                tr => tr.UserId,
                upp => upp.UserId,
                (tr, upp) => new { tr, upp.Profession })
            .GroupBy(x => x.Profession ?? "Unknown")
            .Select(g => new
            {
                Profession = g.Key,
                TotalSpent = g.Sum(x => x.tr.CostActual ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();
        ViewBag.SpendingByProfessionLabels = JsonSerializer.Serialize(spendingByProfession.Select(x => x.Profession));
        ViewBag.SpendingByProfessionData = JsonSerializer.Serialize(spendingByProfession.Select(x => x.TotalSpent));

        var spendingByProvider = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .Include(tr => tr.Course)
            .GroupBy(tr => tr.Course != null ? tr.Course.Provider : "Unknown")
            .Select(g => new
            {
                Provider = g.Key,
                TotalSpent = g.Sum(tr => tr.CostActual ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();
        ViewBag.SpendingByProviderLabels = JsonSerializer.Serialize(spendingByProvider.Select(x => x.Provider));
        ViewBag.SpendingByProviderData = JsonSerializer.Serialize(spendingByProvider.Select(x => x.TotalSpent));

        var topCoursesBySpend = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .Include(tr => tr.Course)
            .GroupBy(tr => tr.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                CourseTitle = g.First().Course != null ? g.First().Course.Title : "Custom Course",
                TotalSpend = g.Sum(tr => tr.CostActual ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpend)
            .Take(10)
            .ToListAsync();
        ViewBag.TopCoursesBySpend = topCoursesBySpend;

        ViewBag.Year = reportYear;
        ViewBag.AvailableYears = Enumerable.Range(DateTime.UtcNow.Year - 2, 5).OrderByDescending(y => y).ToList();

        return View("~/Views/LearningAndDevelopmentReporting/SpendAnalysis.cshtml");
    }

    /// <summary>
    /// Outcomes and Satisfaction Report
    /// </summary>
    public async Task<IActionResult> Outcomes(int? year)
    {
        if (!await IsLearningAndSkillsRoleAsync() && !await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var reportYear = year ?? DateTime.UtcNow.Year;
        var (yearStart, yearEnd) = GetFinancialYearDates(reportYear);

        var totalApproved = await _context.TrainingRequests
            .CountAsync(tr => tr.Status == "Approved" && tr.ApprovedAt >= yearStart && tr.ApprovedAt <= yearEnd);

        var completed = await _context.TrainingRecords
            .CountAsync(tr => tr.Status == "Completed" && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd);

        var completionRate = totalApproved > 0 ? (double)completed / totalApproved * 100 : 0;

        var ratings = await _context.TrainingRecords
            .Where(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .GroupBy(tr => tr.OutcomeRating.Value)
            .Select(g => new
            {
                Rating = g.Key,
                Count = g.Count(),
                Percentage = (double)g.Count() / _context.TrainingRecords
                    .Count(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd) * 100
            })
            .OrderBy(x => x.Rating)
            .ToListAsync();

        ViewBag.RatingsDistributionLabels = JsonSerializer.Serialize(ratings.Select(x => x.Rating));
        ViewBag.RatingsDistributionData = JsonSerializer.Serialize(ratings.Select(x => x.Count));
        ViewBag.RatingsDistributionPercentages = JsonSerializer.Serialize(ratings.Select(x => x.Percentage));

        var professionRatings = await _context.TrainingRecords
            .Where(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .Join(_context.UserProfessionalProfiles,
                tr => tr.UserId,
                upp => upp.UserId,
                (tr, upp) => new { tr, upp.Profession })
            .GroupBy(x => x.Profession ?? "Unknown")
            .Select(g => new
            {
                Profession = g.Key,
                AverageRating = g.Average(x => (double?)x.tr.OutcomeRating.Value) ?? 0,
                Count = g.Count()
            })
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.AverageRating)
            .ToListAsync();
        ViewBag.ProfessionRatingsLabels = JsonSerializer.Serialize(professionRatings.Select(x => x.Profession));
        ViewBag.ProfessionRatingsData = JsonSerializer.Serialize(professionRatings.Select(x => x.AverageRating));

        var feedbackCounts = await _context.TrainingRecords
            .Where(tr => !string.IsNullOrEmpty(tr.Feedback) && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .Include(tr => tr.Course)
            .GroupBy(tr => tr.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                CourseTitle = g.First().Course != null ? g.First().Course.Title : "Unknown",
                FeedbackCount = g.Count(),
                AverageRating = g.Average(tr => (double?)tr.OutcomeRating.Value) ?? 0
            })
            .OrderByDescending(x => x.FeedbackCount)
            .Take(10)
            .ToListAsync();

        ViewBag.Year = reportYear;
        ViewBag.TotalApproved = totalApproved;
        ViewBag.Completed = completed;
        ViewBag.CompletionRate = completionRate;
        ViewBag.FeedbackCounts = feedbackCounts;
        ViewBag.AvailableYears = Enumerable.Range(DateTime.UtcNow.Year - 2, 5).OrderByDescending(y => y).ToList();

        return View("~/Views/LearningAndDevelopmentReporting/Outcomes.cshtml");
    }

    /// <summary>
    /// Profession Analytics Report
    /// </summary>
    public async Task<IActionResult> ProfessionAnalytics(int? year, string? profession)
    {
        if (!await IsLearningAndSkillsRoleAsync() && !await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var reportYear = year ?? DateTime.UtcNow.Year;
        var (yearStart, yearEnd) = GetFinancialYearDates(reportYear);

        // Get profession data - check both legacy Profession field and DdatProfession.Name
        var professionData = await _context.UserProfessionalProfiles
            .Include(upp => upp.DdatProfession)
            .Where(upp => !string.IsNullOrEmpty(upp.Profession) || upp.DdatProfession != null)
            .GroupBy(upp => upp.Profession ?? upp.DdatProfession!.Name)
            .Select(g => new
            {
                Profession = g.Key,
                UserCount = g.Count(),
                UserIds = g.Select(upp => upp.UserId).ToList()
            })
            .ToListAsync();

        // Now calculate statistics for each profession (using in-memory processing since we need to use Contains with lists)
        var professionStats = new List<dynamic>();
        foreach (var prof in professionData)
        {
            if (!string.IsNullOrEmpty(profession) && prof.Profession != profession)
            {
                continue;
            }

            var userIds = prof.UserIds;

            var totalRequests = await _context.TrainingRequests
                .CountAsync(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd &&
                           userIds.Contains(tr.UserId));

            var approvedRequests = await _context.TrainingRequests
                .CountAsync(tr => tr.Status == "Approved" && 
                           ((tr.ApprovedAt.HasValue && tr.ApprovedAt >= yearStart && tr.ApprovedAt <= yearEnd) ||
                            (!tr.ApprovedAt.HasValue && tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd)) &&
                           userIds.Contains(tr.UserId));

            var rejectedRequests = await _context.TrainingRequests
                .CountAsync(tr => tr.Status == "Rejected" && 
                           ((tr.ApprovedAt.HasValue && tr.ApprovedAt >= yearStart && tr.ApprovedAt <= yearEnd) ||
                            (!tr.ApprovedAt.HasValue && tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd)) &&
                           userIds.Contains(tr.UserId));

            var pendingRequests = await _context.TrainingRequests
                .CountAsync(tr => tr.Status == "Submitted" && 
                           tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd &&
                           userIds.Contains(tr.UserId));

            var totalSpent = await _context.TrainingRecords
                .Where(tr => tr.CostActual.HasValue &&
                           tr.DateAttended.HasValue &&
                           tr.DateAttended >= yearStart &&
                           tr.DateAttended <= yearEnd &&
                           userIds.Contains(tr.UserId))
                .SumAsync(tr => tr.CostActual ?? 0);

            var avgRating = await _context.TrainingRecords
                .Where(tr => tr.OutcomeRating.HasValue &&
                           tr.DateAttended.HasValue &&
                           tr.DateAttended >= yearStart &&
                           tr.DateAttended <= yearEnd &&
                           userIds.Contains(tr.UserId))
                .AverageAsync(tr => (double?)tr.OutcomeRating.Value);

            professionStats.Add(new
            {
                Profession = prof.Profession,
                UserCount = prof.UserCount,
                TotalRequests = totalRequests,
                ApprovedRequests = approvedRequests,
                RejectedRequests = rejectedRequests,
                PendingRequests = pendingRequests,
                TotalSpent = totalSpent,
                AverageRating = avgRating ?? 0
            });
        }

        ViewBag.Year = reportYear;
        ViewBag.SelectedProfession = profession;
        ViewBag.ProfessionData = professionStats.OrderByDescending(x => x.TotalRequests).ToList();

        var professions = professionData
            .Select(p => p.Profession)
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        ViewBag.Professions = professions;

        return View("~/Views/LearningAndDevelopmentReporting/ProfessionAnalytics.cshtml");
    }

    /// <summary>
    /// Exports L&D report data to CSV.
    /// </summary>
    public async Task<IActionResult> ExportReport(int? year, string reportType)
    {
        if (!await IsLearningAndSkillsRoleAsync() && !await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var reportYear = year ?? DateTime.UtcNow.Year;
        var (yearStart, yearEnd) = GetFinancialYearDates(reportYear);

        var csv = new StringBuilder();
        var fileName = $"L&D_Report_{reportType}_{reportYear}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        Response.Headers.Add("Content-Encoding", "UTF-8");
        Response.Headers.Add("Content-Type", "text/csv; charset=utf-8");
        Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        await Response.Body.WriteAsync(Encoding.UTF8.GetPreamble());

        switch (reportType)
        {
            case "SpendAnalysis":
                csv.AppendLine("Financial Year,Report Type,Generated At");
                csv.AppendLine($"{reportYear}/{reportYear + 1},Spend Analysis,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine();
                csv.AppendLine("Profession,Total Spent,Number of Records");
                var spendingByProfession = await _context.TrainingRecords
                    .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
                    .Join(_context.UserProfessionalProfiles,
                        tr => tr.UserId,
                        upp => upp.UserId,
                        (tr, upp) => new { tr, upp.Profession })
                    .GroupBy(x => x.Profession ?? "Unknown")
                    .Select(g => new
                    {
                        Profession = g.Key,
                        TotalSpent = g.Sum(x => x.tr.CostActual ?? 0),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .ToListAsync();
                foreach (var item in spendingByProfession)
                {
                    csv.AppendLine($"\"{item.Profession}\",{item.TotalSpent:F2},{item.Count}");
                }
                csv.AppendLine();
                csv.AppendLine("Provider,Total Spent,Number of Records");
                var spendingByProvider = await _context.TrainingRecords
                    .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
                    .Include(tr => tr.Course)
                    .GroupBy(tr => tr.Course != null ? tr.Course.Provider : "Unknown")
                    .Select(g => new
                    {
                        Provider = g.Key,
                        TotalSpent = g.Sum(tr => tr.CostActual ?? 0),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .ToListAsync();
                foreach (var item in spendingByProvider)
                {
                    csv.AppendLine($"\"{item.Provider}\",{item.TotalSpent:F2},{item.Count}");
                }
                break;

            case "RequestAnalysis":
                csv.AppendLine("Financial Year,Report Type,Generated At");
                csv.AppendLine($"{reportYear}/{reportYear + 1},Request Analysis,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine();
                csv.AppendLine("Status,Count");
                var statusBreakdown = await _context.TrainingRequests
                    .Where(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd)
                    .GroupBy(tr => tr.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();
                foreach (var item in statusBreakdown)
                {
                    csv.AppendLine($"\"{item.Status}\",{item.Count}");
                }
                csv.AppendLine();
                csv.AppendLine("Profession,Total Requests,Approved Requests,Rejected Requests");
                var professionRequestData = await _context.UserProfessionalProfiles
                    .Where(upp => !string.IsNullOrEmpty(upp.Profession))
                    .GroupBy(upp => upp.Profession)
                    .Select(g => new
                    {
                        Profession = g.Key,
                        TotalRequests = _context.TrainingRequests
                            .Count(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd &&
                                       _context.UserProfessionalProfiles
                                           .Where(uppInner => uppInner.Profession == g.Key)
                                           .Select(uppInner => uppInner.UserId)
                                           .Contains(tr.UserId)),
                        ApprovedRequests = _context.TrainingRequests
                            .Count(tr => tr.Status == "Approved" && tr.ApprovedAt >= yearStart && tr.ApprovedAt <= yearEnd &&
                                       _context.UserProfessionalProfiles
                                           .Where(uppInner => uppInner.Profession == g.Key)
                                           .Select(uppInner => uppInner.UserId)
                                           .Contains(tr.UserId)),
                        RejectedRequests = _context.TrainingRequests
                            .Count(tr => tr.Status == "Rejected" && tr.ApprovedAt >= yearStart && tr.ApprovedAt <= yearEnd &&
                                       _context.UserProfessionalProfiles
                                           .Where(uppInner => uppInner.Profession == g.Key)
                                           .Select(uppInner => uppInner.UserId)
                                           .Contains(tr.UserId))
                    })
                    .OrderByDescending(x => x.TotalRequests)
                    .ToListAsync();
                foreach (var item in professionRequestData)
                {
                    csv.AppendLine($"\"{item.Profession}\",{item.TotalRequests},{item.ApprovedRequests},{item.RejectedRequests}");
                }
                break;

            case "OutcomesAnalysis":
                csv.AppendLine("Financial Year,Report Type,Generated At");
                csv.AppendLine($"{reportYear}/{reportYear + 1},Outcomes Analysis,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine();
                csv.AppendLine("Rating,Count,Percentage");
                var ratings = await _context.TrainingRecords
                    .Where(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
                    .GroupBy(tr => tr.OutcomeRating.Value)
                    .Select(g => new
                    {
                        Rating = g.Key,
                        Count = g.Count(),
                        Percentage = (double)g.Count() / _context.TrainingRecords
                            .Count(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd) * 100
                    })
                    .OrderBy(x => x.Rating)
                    .ToListAsync();
                foreach (var item in ratings)
                {
                    csv.AppendLine($"{item.Rating},{item.Count},{item.Percentage:F2}%");
                }
                csv.AppendLine();
                csv.AppendLine("Profession,Average Rating,Number of Records");
                var professionRatings = await _context.TrainingRecords
                    .Where(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
                    .Join(_context.UserProfessionalProfiles,
                        tr => tr.UserId,
                        upp => upp.UserId,
                        (tr, upp) => new { tr, upp.Profession })
                    .GroupBy(x => x.Profession ?? "Unknown")
                    .Select(g => new
                    {
                        Profession = g.Key,
                        AverageRating = g.Average(x => (double?)x.tr.OutcomeRating.Value) ?? 0,
                        Count = g.Count()
                    })
                    .Where(x => x.Count > 0)
                    .OrderByDescending(x => x.AverageRating)
                    .ToListAsync();
                foreach (var item in professionRatings)
                {
                    csv.AppendLine($"\"{item.Profession}\",{item.AverageRating:F2},{item.Count}");
                }
                break;

            default:
                csv.AppendLine("Invalid report type.");
                break;
        }

        return Content(csv.ToString(), "text/csv", Encoding.UTF8);
    }

    #endregion
}

