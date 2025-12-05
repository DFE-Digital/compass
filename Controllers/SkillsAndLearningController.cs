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

    public SkillsAndLearningController(
        CompassDbContext context,
        ILogger<SkillsAndLearningController> logger,
        IPermissionService permissionService,
        INudgingService nudgingService,
        INotificationService? notificationService = null)
    {
        _context = context;
        _logger = logger;
        _permissionService = permissionService;
        _nudgingService = nudgingService;
        _notificationService = notificationService;
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

        var userProfile = await _context.UserProfessionalProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        var myRequests = await _context.TrainingRequests
            .Where(tr => tr.UserId == userId.Value)
            .OrderByDescending(tr => tr.CreatedAt)
            .Take(5)
            .Include(tr => tr.Course)
            .ToListAsync();

        var myRecords = await _context.TrainingRecords
            .Where(tr => tr.UserId == userId.Value)
            .OrderByDescending(tr => tr.DateAttended ?? tr.DateRequested)
            .Take(5)
            .Include(tr => tr.Course)
            .ToListAsync();

        // Generate and get nudges
        var nudges = await _nudgingService.GenerateNudgesForUserAsync(userId.Value);

        ViewBag.UserProfile = userProfile;
        ViewBag.MyRequests = myRequests;
        ViewBag.MyRecords = myRecords;
        ViewBag.Nudges = nudges;

        return View();
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
                tc.ProfessionTags != null && 
                tc.ProfessionTags.Contains(professionFilter));
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

        // Get user profile for recommendations
        var userProfile = await _context.UserProfessionalProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value);

        ViewBag.ProfessionFilter = professionFilter;
        ViewBag.CapabilityFilter = capabilityFilter;
        ViewBag.ProviderFilter = providerFilter;
        ViewBag.FormatFilter = formatFilter;
        ViewBag.ModeFilter = modeFilter;
        ViewBag.Search = search;
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
    /// View my training requests
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

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        var requests = await query
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        return View(requests);
    }

    /// <summary>
    /// View my learning history
    /// </summary>
    public async Task<IActionResult> LearningHistory(string? statusFilter)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var query = _context.TrainingRecords
            .Where(tr => tr.UserId == userId.Value)
            .Include(tr => tr.Course)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(tr => tr.Status == statusFilter);
        }

        var records = await query
            .OrderByDescending(tr => tr.DateAttended ?? tr.DateRequested)
            .ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        return View(records);
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
        return RedirectToAction(nameof(LearningHistory));
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

        return View(profile);
    }

    /// <summary>
    /// Update professional profile - POST
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        string? profession,
        string? skills,
        string? capabilityGaps)
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var profile = await _context.UserProfessionalProfiles
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

        profile.Profession = profession;
        profile.Skills = skills;
        profile.CapabilityGaps = capabilityGaps;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Profile updated successfully";
        return RedirectToAction(nameof(MyProfile));
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
            .Where(h => h.UserId == userId.Value)
            .Select(h => h.Profession)
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
    /// Dashboard for Learning & Skills role - View all training activity
    /// </summary>
    public async Task<IActionResult> AdminDashboard()
    {
        if (!await IsLearningAndSkillsRoleAsync())
        {
            return Forbid();
        }

        var totalRequests = await _context.TrainingRequests.CountAsync();
        var pendingRequests = await _context.TrainingRequests.CountAsync(tr => tr.Status == "Submitted");
        var totalRecords = await _context.TrainingRecords.CountAsync();
        var completedRecords = await _context.TrainingRecords.CountAsync(tr => tr.Status == "Completed");

        var recentRequests = await _context.TrainingRequests
            .Include(tr => tr.User)
            .Include(tr => tr.Course)
            .OrderByDescending(tr => tr.CreatedAt)
            .Take(10)
            .ToListAsync();

        ViewBag.TotalRequests = totalRequests;
        ViewBag.PendingRequests = pendingRequests;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.CompletedRecords = completedRecords;
        ViewBag.RecentRequests = recentRequests;

        return View("~/Views/LearningAndSkills/Index.cshtml");
    }

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
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.ProfessionFilter = professionFilter;
        ViewBag.Search = search;

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
    public async Task<IActionResult> HOPDashboard()
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
                .Where(upp => professionScope.Contains(upp.Profession ?? ""))
                .Select(upp => upp.UserId)
                .ToListAsync();
        }

        var query = _context.TrainingRequests.AsQueryable();
        if (userIds.Any() && !isCentralOps)
        {
            query = query.Where(tr => userIds.Contains(tr.UserId));
        }

        var totalRequests = await query.CountAsync();
        var pendingRequests = await query.CountAsync(tr => tr.Status == "Submitted");
        var approvedRequests = await query.CountAsync(tr => tr.Status == "Approved");

        var recordsQuery = _context.TrainingRecords.AsQueryable();
        if (userIds.Any() && !isCentralOps)
        {
            recordsQuery = recordsQuery.Where(tr => userIds.Contains(tr.UserId));
        }

        var totalSpent = await recordsQuery
            .Where(tr => tr.CostActual.HasValue)
            .SumAsync(tr => tr.CostActual ?? 0);

        var estimatedCost = await query
            .Where(tr => tr.Status == "Approved" || tr.Status == "Submitted")
            .Join(_context.TrainingCourses,
                tr => tr.CourseId,
                tc => tc.Id,
                (tr, tc) => tc.Cost ?? 0)
            .SumAsync();

        ViewBag.TotalRequests = totalRequests;
        ViewBag.PendingRequests = pendingRequests;
        ViewBag.ApprovedRequests = approvedRequests;
        ViewBag.TotalSpent = totalSpent;
        ViewBag.EstimatedCost = estimatedCost;
        ViewBag.ProfessionScope = professionScope;
        ViewBag.IsCentralOps = isCentralOps;

        return View("~/Views/HOP/Index.cshtml");
    }

    /// <summary>
    /// View profession requests for users in my profession scope (HOP)
    /// </summary>
    public async Task<IActionResult> HOPRequests(
        string? statusFilter,
        string? search)
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
                .Where(upp => professionScope.Contains(upp.Profession ?? ""))
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

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(tr => 
                (tr.User != null && tr.User.Name.Contains(search)) ||
                (tr.User != null && tr.User.Email.Contains(search)) ||
                (tr.Course != null && tr.Course.Title.Contains(search)) ||
                (tr.CustomCourseTitle != null && tr.CustomCourseTitle.Contains(search)));
        }

        var requests = await query
            .OrderByDescending(tr => tr.CreatedAt)
            .ToListAsync();

        ViewBag.StatusFilter = statusFilter;
        ViewBag.Search = search;
        ViewBag.ProfessionScope = professionScope;

        return View("~/Views/HOP/ProfessionRequests.cshtml", requests);
    }

    /// <summary>
    /// Budget and spending dashboard (HOP/Central Ops Admin)
    /// </summary>
    public async Task<IActionResult> BudgetAndSpending()
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
                .Where(upp => professionScope.Contains(upp.Profession ?? ""))
                .Select(upp => upp.UserId)
                .ToListAsync();
        }

        var spendingByProvider = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .Include(tr => tr.Course)
            .GroupBy(tr => tr.Course != null ? tr.Course.Provider : "Unknown")
            .Select(g => new
            {
                Provider = g.Key ?? "Unknown",
                TotalSpent = g.Sum(tr => tr.CostActual ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();

        var spendingByProfession = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .Join(_context.UserProfessionalProfiles,
                tr => tr.UserId,
                upp => upp.UserId,
                (tr, upp) => new { tr, upp.Profession })
            .GroupBy(x => x.Profession ?? "Unknown")
            .Select(g => new
            {
                Profession = g.Key ?? "Unknown",
                TotalSpent = g.Sum(x => x.tr.CostActual ?? 0),
                Count = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();

        var forecastedCosts = await _context.TrainingRequests
            .Where(tr => (tr.Status == "Approved" || tr.Status == "Submitted") && 
                        (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .Join(_context.TrainingCourses,
                tr => tr.CourseId,
                tc => tc.Id,
                (tr, tc) => tc.Cost ?? 0)
            .SumAsync();

        var actualSpent = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && (userIds.Count == 0 || isCentralOps || userIds.Contains(tr.UserId)))
            .SumAsync(tr => tr.CostActual ?? 0);

        LearningBudget? currentBudget = null;
        if (isCentralOps)
        {
            var currentYear = DateTime.UtcNow.Year;
            currentBudget = await _context.LearningBudgets
                .FirstOrDefaultAsync(lb => lb.FinancialYear == currentYear && lb.IsActive);
        }

        ViewBag.SpendingByProvider = spendingByProvider;
        ViewBag.SpendingByProfession = spendingByProfession;
        ViewBag.ForecastedCosts = forecastedCosts;
        ViewBag.ActualSpent = actualSpent;
        ViewBag.ProfessionScope = professionScope;
        ViewBag.IsCentralOps = isCentralOps;
        ViewBag.CurrentBudget = currentBudget;

        return View("~/Views/HOP/BudgetAndSpending.cshtml");
    }

    /// <summary>
    /// Manage budget (Central Ops Admin only)
    /// </summary>
    public async Task<IActionResult> ManageBudget()
    {
        if (!await IsCentralOpsAdminAsync())
        {
            return Forbid();
        }

        var currentYear = DateTime.UtcNow.Year;
        var budget = await _context.LearningBudgets
            .FirstOrDefaultAsync(lb => lb.FinancialYear == currentYear && lb.IsActive);

        if (budget == null)
        {
            budget = new LearningBudget
            {
                FinancialYear = currentYear,
                TotalBudget = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.LearningBudgets.Add(budget);
            await _context.SaveChangesAsync();
        }

        var actualSpent = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue)
            .SumAsync(tr => tr.CostActual ?? 0);

        var forecastedCosts = await _context.TrainingRequests
            .Where(tr => tr.Status == "Approved" || tr.Status == "Submitted")
            .Join(_context.TrainingCourses,
                tr => tr.CourseId,
                tc => tc.Id,
                (tr, tc) => tc.Cost ?? 0)
            .SumAsync();

        budget.Spent = actualSpent;
        budget.Forecasted = forecastedCosts;

        ViewBag.Budget = budget;
        ViewBag.Remaining = budget.TotalBudget - (actualSpent + forecastedCosts);

        return View("~/Views/HOP/ManageBudget.cshtml", budget);
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
            return View("~/Views/HOP/ManageBudget.cshtml", budget);
        }

        var userEmail = GetCurrentUserEmail() ?? "System";

        budget.TotalBudget = totalBudget;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Budget for FY {budget.FinancialYear} updated to £{totalBudget:F2}";
        return RedirectToAction(nameof(BudgetAndSpending));
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
            .Where(upp => !string.IsNullOrEmpty(upp.CapabilityGaps) &&
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
    /// Main L&D Reporting Dashboard
    /// </summary>
    public async Task<IActionResult> Reporting(int? year)
    {
        if (!await IsLearningAndSkillsRoleAsync() && !await IsHOPOrCentralOpsAsync())
        {
            return Forbid();
        }

        var reportYear = year ?? DateTime.UtcNow.Year;
        var (yearStart, yearEnd) = GetFinancialYearDates(reportYear);

        ViewBag.TotalRequests = await _context.TrainingRequests.CountAsync(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd);
        ViewBag.ApprovedRequests = await _context.TrainingRequests.CountAsync(tr => tr.Status == "Approved" && tr.ApprovedAt >= yearStart && tr.ApprovedAt <= yearEnd);
        ViewBag.CompletedRecords = await _context.TrainingRecords.CountAsync(tr => tr.Status == "Completed" && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd);
        ViewBag.TotalSpent = await _context.TrainingRecords.Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd).SumAsync(tr => tr.CostActual ?? 0);

        var monthlySpend = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .GroupBy(tr => new { Month = tr.DateAttended.Value.Month, Year = tr.DateAttended.Value.Year })
            .Select(g => new { Month = g.Key.Month, Spend = g.Sum(tr => tr.CostActual ?? 0) })
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

        var monthlyRequests = await _context.TrainingRequests
            .Where(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd)
            .GroupBy(tr => new { Month = tr.CreatedAt.Month, Year = tr.CreatedAt.Year })
            .Select(g => new { Month = g.Key.Month, Count = g.Count() })
            .OrderBy(x => x.Month)
            .ToListAsync();

        var monthlyRequestData = new int[12];
        foreach (var item in monthlyRequests)
        {
            monthlyRequestData[item.Month - 1] = item.Count;
        }
        ViewBag.MonthlyRequestData = JsonSerializer.Serialize(monthlyRequestData);

        var requestsByProfession = await _context.TrainingRequests
            .Where(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd)
            .Join(_context.UserProfessionalProfiles,
                tr => tr.UserId,
                upp => upp.UserId,
                (tr, upp) => upp.Profession ?? "Unknown")
            .GroupBy(p => p)
            .Select(g => new { Profession = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();
        ViewBag.RequestsByProfessionLabels = JsonSerializer.Serialize(requestsByProfession.Select(x => x.Profession));
        ViewBag.RequestsByProfessionData = JsonSerializer.Serialize(requestsByProfession.Select(x => x.Count));

        var ratingsDistribution = await _context.TrainingRecords
            .Where(tr => tr.OutcomeRating.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .GroupBy(tr => tr.OutcomeRating.Value)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .OrderBy(x => x.Rating)
            .ToListAsync();
        ViewBag.RatingsDistributionLabels = JsonSerializer.Serialize(ratingsDistribution.Select(x => x.Rating));
        ViewBag.RatingsDistributionData = JsonSerializer.Serialize(ratingsDistribution.Select(x => x.Count));

        var statusBreakdown = await _context.TrainingRequests
            .Where(tr => tr.CreatedAt >= yearStart && tr.CreatedAt <= yearEnd)
            .GroupBy(tr => tr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
        ViewBag.StatusBreakdownLabels = JsonSerializer.Serialize(statusBreakdown.Select(x => x.Status));
        ViewBag.StatusBreakdownData = JsonSerializer.Serialize(statusBreakdown.Select(x => x.Count));

        var topProviders = await _context.TrainingRecords
            .Where(tr => tr.CostActual.HasValue && tr.DateAttended >= yearStart && tr.DateAttended <= yearEnd)
            .Include(tr => tr.Course)
            .GroupBy(tr => tr.Course != null ? tr.Course.Provider : "Unknown")
            .Select(g => new { Provider = g.Key ?? "Unknown", TotalSpend = g.Sum(tr => tr.CostActual ?? 0), Count = g.Count() })
            .OrderByDescending(x => x.TotalSpend)
            .Take(5)
            .ToListAsync();
        ViewBag.TopProviders = topProviders;
        ViewBag.ProviderStats = topProviders; // Alias for view compatibility

        ViewBag.Year = reportYear;
        ViewBag.AvailableYears = Enumerable.Range(DateTime.UtcNow.Year - 2, 5).OrderByDescending(y => y).ToList();

        return View("~/Views/LearningAndDevelopmentReporting/Index.cshtml");
    }

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

        var professionData = await _context.UserProfessionalProfiles
            .Where(upp => !string.IsNullOrEmpty(upp.Profession))
            .GroupBy(upp => upp.Profession)
            .Select(g => new
            {
                Profession = g.Key,
                UserCount = g.Count(),
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
                                   .Contains(tr.UserId)),
                TotalSpent = _context.TrainingRecords
                    .Where(tr => tr.CostActual.HasValue &&
                               tr.DateAttended >= yearStart &&
                               tr.DateAttended <= yearEnd &&
                               _context.UserProfessionalProfiles
                                   .Where(uppInner => uppInner.Profession == g.Key)
                                   .Select(uppInner => uppInner.UserId)
                                   .Contains(tr.UserId))
                    .Sum(tr => tr.CostActual ?? 0),
                AverageRating = _context.TrainingRecords
                    .Where(tr => tr.OutcomeRating.HasValue &&
                               tr.DateAttended >= yearStart &&
                               tr.DateAttended <= yearEnd &&
                               _context.UserProfessionalProfiles
                                   .Where(uppInner => uppInner.Profession == g.Key)
                                   .Select(uppInner => uppInner.UserId)
                                   .Contains(tr.UserId))
                    .Average(tr => (double?)tr.OutcomeRating.Value)
            })
            .Where(x => string.IsNullOrEmpty(profession) || x.Profession == profession)
            .OrderByDescending(x => x.TotalRequests)
            .ToListAsync();

        ViewBag.Year = reportYear;
        ViewBag.SelectedProfession = profession;
        ViewBag.ProfessionData = professionData;

        var professions = await _context.UserProfessionalProfiles
            .Where(upp => !string.IsNullOrEmpty(upp.Profession))
            .Select(upp => upp.Profession)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

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

