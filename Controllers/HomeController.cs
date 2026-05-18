using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Models;
using Compass.Services;
using Compass.Data;
using Compass.ViewModels.Dashboard;
using Compass.Services.Dashboard;
using Compass.Helpers;
using Compass.Controllers.Modern;
using Compass.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Compass.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICmsApiService _cmsApiService;
    private readonly IProductsApiService _productsApiService;
    private readonly IReturnStatusService _returnStatusService;
    private readonly IMonthlyUpdateService _monthlyUpdateService;
    private readonly CompassDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IHomeDashboardViewModelBuilder _dashboardBuilder;

    public HomeController(
        ILogger<HomeController> logger, 
        ICmsApiService cmsApiService,
        IProductsApiService productsApiService,
        IReturnStatusService returnStatusService,
        IMonthlyUpdateService monthlyUpdateService,
        CompassDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IHomeDashboardViewModelBuilder dashboardBuilder)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
        _monthlyUpdateService = monthlyUpdateService;
        _context = context;
        _environment = environment;
        _configuration = configuration;
        _dashboardBuilder = dashboardBuilder;
    }

    /// <summary>
    /// Legacy URL <c>/</c> and <c>/Home/Index</c> — the product UI lives under <c>/modern/</c>; redirect to the modern dashboard.
    /// </summary>
    public IActionResult Index(string? testRole = null, int? testUserId = null) =>
        RedirectToAction(nameof(ModernDashboardController.Index), "ModernDashboard", new { testRole, testUserId });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDashboardPreferences(DashboardPreferenceInputModel input)
    {
        try
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            if (currentUser == null)
            {
                TempData["ErrorMessage"] = "User account not found.";
                return RedirectToAction(nameof(Index));
            }

            var preference = await _dashboardBuilder.GetOrCreateDashboardPreferenceAsync(currentUser);

            preference.ShowTasksPanel = input.ShowTasksPanel;
            preference.ShowProductPanel = input.ShowProductPanel;
            preference.ShowRiskPanel = input.ShowRiskPanel;
            preference.ShowMilestonePanel = input.ShowMilestonePanel;
            preference.ShowRemindersPanel = input.ShowRemindersPanel;
            preference.ShowSuccessPanel = input.ShowSuccessPanel;
            preference.PreferredTaskGrouping = string.IsNullOrWhiteSpace(input.PreferredTaskGrouping)
                ? "priority"
                : input.PreferredTaskGrouping.Trim().ToLowerInvariant();
            preference.DashboardFocus = string.IsNullOrWhiteSpace(input.DashboardFocus)
                ? null
                : input.DashboardFocus.Trim();

            var selectedQuickLinks = (input.SelectedQuickLinks ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            preference.QuickLaunchShortcuts = selectedQuickLinks.Any()
                ? string.Join(',', selectedQuickLinks)
                : null;

            preference.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Dashboard preferences saved.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update dashboard preferences");
            TempData["ErrorMessage"] = "We could not save your dashboard preferences.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDashboardLayout([FromBody] DashboardLayoutUpdateModel input)
    {
        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized();
        }

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

        if (currentUser == null)
        {
            return Unauthorized();
        }

        var preference = await _dashboardBuilder.GetOrCreateDashboardPreferenceAsync(currentUser);
        var definitions = DashboardLayoutHelper.GetBlockCatalog();
        var validTypes = definitions.Select(d => d.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sanitizedBlocks = (input?.Blocks ?? new List<DashboardBlockInstance>())
            .Where(b => !string.IsNullOrWhiteSpace(b.Type) && validTypes.Contains(b.Type))
            .Select(b => new DashboardBlockInstance
            {
                Id = string.IsNullOrWhiteSpace(b.Id) ? Guid.NewGuid().ToString() : b.Id,
                Type = b.Type,
                X = Math.Max(0, Math.Min(11, b.X)),
                Y = Math.Max(0, b.Y),
                Width = Math.Clamp(b.Width, 1, 12),
                Height = Math.Max(1, b.Height),
                Settings = b.Settings ?? new Dictionary<string, string>()
            })
            .ToList();

        preference.DashboardLayout = DashboardLayoutHelper.SerializeLayout(sanitizedBlocks);
        preference.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RenderDashboardBlock([FromBody] DashboardBlockRenderRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.BlockType))
        {
            return BadRequest();
        }

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized();
        }

        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

        if (currentUser == null)
        {
            return Unauthorized();
        }

        var preference = await _dashboardBuilder.GetOrCreateDashboardPreferenceAsync(currentUser);
        var blockDefinitions = DashboardLayoutHelper.GetBlockCatalog();
        var definition = blockDefinitions
            .FirstOrDefault(d => d.Type.Equals(request.BlockType, StringComparison.OrdinalIgnoreCase));

        if (definition == null)
        {
            return NotFound();
        }

        var dashboardViewModel = await _dashboardBuilder.BuildDashboardViewModelAsync(currentUser, userEmail, preference, Url, HttpContext);
        var blockInstance = new DashboardBlockInstance
        {
            Id = string.IsNullOrWhiteSpace(request.BlockId) ? Guid.NewGuid().ToString() : request.BlockId,
            Type = definition.Type,
            Width = definition.DefaultWidth,
            Height = definition.DefaultHeight,
            Settings = request.Settings ?? new Dictionary<string, string>()
        };

        return PartialView("_DashboardBlockContent", (blockInstance, dashboardViewModel));
    }

    private static DateTime NextWeekday(DateTime start, DayOfWeek dayOfWeek)
    {
        var daysToAdd = ((int)dayOfWeek - (int)start.DayOfWeek + 7) % 7;
        if (daysToAdd == 0)
        {
            daysToAdd = 7;
        }

        return start.AddDays(daysToAdd);
    }

    private IQueryable<T> ApplyDateFilter<T>(IQueryable<T> query, string? dateFilter, System.Linq.Expressions.Expression<Func<T, DateTime?>> dateSelector)
        where T : class
    {
        if (string.IsNullOrEmpty(dateFilter) || dateFilter == "all")
        {
            return query;
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        return dateFilter switch
        {
            "overdue" => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.NotEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?))),
                    System.Linq.Expressions.Expression.LessThan(
                        System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                        System.Linq.Expressions.Expression.Constant(now))),
                dateSelector.Parameters)),
            "today" => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.NotEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?))),
                    System.Linq.Expressions.Expression.AndAlso(
                        System.Linq.Expressions.Expression.GreaterThanOrEqual(
                            System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                            System.Linq.Expressions.Expression.Constant(today)),
                        System.Linq.Expressions.Expression.LessThan(
                            System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                            System.Linq.Expressions.Expression.Constant(today.AddDays(1))))),
                dateSelector.Parameters)),
            "week" => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.NotEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?))),
                    System.Linq.Expressions.Expression.LessThanOrEqual(
                        System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                        System.Linq.Expressions.Expression.Constant(now.AddDays(7)))),
                dateSelector.Parameters)),
            "month" => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.NotEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?))),
                    System.Linq.Expressions.Expression.LessThanOrEqual(
                        System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                        System.Linq.Expressions.Expression.Constant(now.AddMonths(1)))),
                dateSelector.Parameters)),
            "next_month" => query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.NotEqual(dateSelector.Body, System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?))),
                    System.Linq.Expressions.Expression.AndAlso(
                        System.Linq.Expressions.Expression.GreaterThan(
                            System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                            System.Linq.Expressions.Expression.Constant(now.AddMonths(1))),
                        System.Linq.Expressions.Expression.LessThanOrEqual(
                            System.Linq.Expressions.Expression.Property(dateSelector.Body, "Value"),
                            System.Linq.Expressions.Expression.Constant(now.AddMonths(2))))),
                dateSelector.Parameters)),
            _ => query
        };
    }

    private IQueryable<Milestone> ApplyMilestoneDateFilter(IQueryable<Milestone> query, string? dateFilter)
    {
        if (string.IsNullOrEmpty(dateFilter) || dateFilter == "all")
        {
            return query;
        }

        var now = DateTime.UtcNow;
        var today = now.Date;

        return dateFilter switch
        {
            "overdue" => query.Where(m => m.DueDate < now),
            "today" => query.Where(m => m.DueDate >= today && m.DueDate < today.AddDays(1)),
            "week" => query.Where(m => m.DueDate <= now.AddDays(7)),
            "month" => query.Where(m => m.DueDate <= now.AddMonths(1)),
            "next_month" => query.Where(m => m.DueDate > now.AddMonths(1) && m.DueDate <= now.AddMonths(2)),
            _ => query
        };
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;
        if (exception != null)
            _logger.LogError(exception, "Unhandled exception on {Path}", HttpContext.Request.Path);

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public new IActionResult NotFound()
    {
        var originalStatusCode = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalStatusCode;
        var statusCode = originalStatusCode is >= 400 ? originalStatusCode.Value : StatusCodes.Status404NotFound;
        Response.StatusCode = statusCode;
        ViewData["StatusCode"] = statusCode;
        return View();
    }

    public IActionResult Support()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetTestUsers()
    {
        // Only allow in development
        if (!_environment.IsDevelopment())
        {
            return Forbid();
        }

        try
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name, u.Email })
                .Take(100) // Limit to first 100 users for performance
                .ToListAsync();

            return Json(users);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load test users - database may be unavailable");
            // Return empty array instead of failing
            return Json(Array.Empty<object>());
        }
    }
}

