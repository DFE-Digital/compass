using Compass.Data;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Compass.Controllers;

[Authorize]
public class UserLeadershipController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<UserLeadershipController> _logger;
    private readonly IProductsApiService _productsApiService;

    public UserLeadershipController(
        CompassDbContext context,
        ILogger<UserLeadershipController> logger,
        IProductsApiService productsApiService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? userId)
    {
        var viewModel = await BuildPageViewModelAsync(userId);
        return View("~/Views/Admin/UserLeadership/Index.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SelectUser(int? selectedUserId)
    {
        if (!selectedUserId.HasValue)
        {
            TempData["ErrorMessage"] = "Select a user to continue.";
            return RedirectToAction(nameof(Index));
        }

        var userIdValue = selectedUserId.Value;
        return RedirectToAction(nameof(Index), new { userId = userIdValue });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(UserLeadershipAssignmentInputModel input)
    {
        if (!input.UserId.HasValue)
        {
            ModelState.AddModelError("Input.UserId", "Select a user before assigning leadership scope.");
        }

        input.BusinessAreas ??= Array.Empty<string>();

        var submittedKeys = input.BusinessAreas
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var businessAreaOptions = await GetBusinessAreaOptionsAsync();
        var optionLookup = businessAreaOptions
            .ToDictionary(o => o.Value, o => o.Name, StringComparer.OrdinalIgnoreCase);

        var validKeys = submittedKeys
            .Where(optionLookup.ContainsKey)
            .ToList();

        if (!validKeys.Any())
        {
            ModelState.AddModelError("Input.BusinessAreas", "Select at least one valid business area.");
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildPageViewModelAsync(input.UserId);
            invalidModel.Input = input;
            return View("~/Views/Admin/UserLeadership/Index.cshtml", invalidModel);
        }

        var selectedUser = await _context.Users.FindAsync(input.UserId.Value);
        if (selectedUser == null)
        {
            TempData["ErrorMessage"] = "The selected user could not be found.";
            return RedirectToAction(nameof(Index));
        }

        var newAssignments = new List<UserBusinessAreaRoleAssignment>();

        foreach (var role in input.Roles.Distinct())
        {
            foreach (var businessAreaKey in validKeys)
            {
                var businessAreaName = optionLookup[businessAreaKey];

                var exists = await _context.UserBusinessAreaRoleAssignments
                    .AnyAsync(a =>
                        a.UserId == selectedUser.Id &&
                        a.BusinessAreaKey == businessAreaKey &&
                        a.Role == role);

                if (exists)
                {
                    continue;
                }

                newAssignments.Add(new UserBusinessAreaRoleAssignment
                {
                    UserId = selectedUser.Id,
                    BusinessAreaKey = businessAreaKey,
                    BusinessAreaName = businessAreaName,
                    Role = role,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        if (newAssignments.Any())
        {
            _context.UserBusinessAreaRoleAssignments.AddRange(newAssignments);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Recorded {newAssignments.Count} leadership assignment(s) for {selectedUser.Name}.";
        }
        else
        {
            TempData["InfoMessage"] = "Those leadership assignments already exist.";
        }

        return RedirectToAction(nameof(Index), new { userId = selectedUser.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int userId)
    {
        var assignment = await _context.UserBusinessAreaRoleAssignments.FindAsync(id);
        if (assignment == null)
        {
            TempData["ErrorMessage"] = "Unable to find that assignment.";
            return RedirectToAction(nameof(Index), new { userId });
        }

        _context.UserBusinessAreaRoleAssignments.Remove(assignment);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Removed leadership assignment.";
        return RedirectToAction(nameof(Index), new { userId });
    }

    private async Task<UserLeadershipAssignmentPageViewModel> BuildPageViewModelAsync(int? userId)
    {
        var businessAreas = await GetBusinessAreaOptionsAsync();

        User? selectedUser = null;
        IReadOnlyCollection<UserBusinessAreaRoleAssignment> assignments = Array.Empty<UserBusinessAreaRoleAssignment>();

        if (userId.HasValue)
        {
            selectedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
            if (selectedUser != null)
            {
                assignments = await _context.UserBusinessAreaRoleAssignments
                    .Where(a => a.UserId == selectedUser.Id)
                    .OrderByDescending(a => a.Role)
                    .ThenBy(a => a.BusinessAreaName)
                    .ToListAsync();
            }
        }

        var allAssignments = await _context.UserBusinessAreaRoleAssignments
            .Include(a => a.User)
            .ToListAsync();

        var assignees = allAssignments
            .GroupBy(a => new
            {
                a.UserId,
                a.User.Name,
                a.User.Email
            })
            .Select(group => new UserLeadershipAssigneeSummary
            {
                UserId = group.Key.UserId,
                Name = string.IsNullOrWhiteSpace(group.Key.Name) ? "Unknown user" : group.Key.Name!,
                Email = group.Key.Email ?? string.Empty,
                AssignmentCount = group.Count(),
                BusinessAreas = group
                    .Select(a => a.BusinessAreaName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name)
                    .ToList(),
                Roles = group
                    .Select(a => a.Role)
                    .Distinct()
                    .OrderByDescending(role => role)
                    .ToList()
            })
            .OrderBy(summary => summary.Name)
            .ToList();

        return new UserLeadershipAssignmentPageViewModel
        {
            SelectedUser = selectedUser,
            Assignments = assignments,
            BusinessAreas = businessAreas,
            RoleOptions = BuildRoleOptions(),
            Assignees = assignees,
            Input = new UserLeadershipAssignmentInputModel
            {
                UserId = userId
            }
        };
    }

    private static IReadOnlyCollection<LeadershipRoleOption> BuildRoleOptions() => new[]
    {
        new LeadershipRoleOption
        {
            Value = LeadershipRoleTier.PermanentSecretary,
            Label = "Permanent Secretary",
            Description = "Whole-department remit, ultimate accounting officer."
        },
        new LeadershipRoleOption
        {
            Value = LeadershipRoleTier.DirectorGeneral,
            Label = "Director General",
            Description = "Leads multiple directorates or missions."
        },
        new LeadershipRoleOption
        {
            Value = LeadershipRoleTier.CLevel,
            Label = "C-Level",
            Description = "Chief digital, data or technology leadership."
        },
        new LeadershipRoleOption
        {
            Value = LeadershipRoleTier.DeputyDirectorOrSro,
            Label = "Deputy Director / SRO",
            Description = "Owns delivery outcomes for a programme or product line."
        },
        new LeadershipRoleOption
        {
            Value = LeadershipRoleTier.PortfolioLead,
            Label = "Portfolio lead / G6",
            Description = "Heads up a delivery portfolio or product cluster."
        }
    };

    private async Task<IReadOnlyCollection<LeadershipBusinessAreaOption>> GetBusinessAreaOptionsAsync()
    {
        var names = await _productsApiService.GetBusinessAreasAsync();
        var options = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => new LeadershipBusinessAreaOption
            {
                Name = name.Trim(),
                Value = BuildBusinessAreaKey(name)
            })
            .Where(option => !string.IsNullOrEmpty(option.Value))
            .OrderBy(option => option.Name)
            .ToList();

        return options;
    }

    private static string BuildBusinessAreaKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-");
        return slug.Trim('-');
    }
}

