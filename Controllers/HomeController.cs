using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Models;
using Compass.Services;
using Compass.Data;
using Compass.ViewModels.Dashboard;
using Compass.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Compass.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICmsApiService _cmsApiService;
    private readonly IProductsApiService _productsApiService;
    private readonly IReturnStatusService _returnStatusService;
    private readonly CompassDbContext _context;

    public HomeController(
        ILogger<HomeController> logger, 
        ICmsApiService cmsApiService,
        IProductsApiService productsApiService,
        IReturnStatusService returnStatusService,
        CompassDbContext context)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _productsApiService = productsApiService;
        _returnStatusService = returnStatusService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Index: No user email found");
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return View(new HomeDashboardViewModel());
            }

            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            if (currentUser == null)
            {
                _logger.LogWarning("Index: User not found in database for email: {Email}", userEmail);
                TempData["ErrorMessage"] = "User account not found. Please contact an administrator.";
                return View(new HomeDashboardViewModel());
            }

            var preference = await GetOrCreateDashboardPreference(currentUser);

            var viewModel = await BuildDashboardViewModel(currentUser, userEmail, preference);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Index");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
            return View(new HomeDashboardViewModel());
        }
    }

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

            var preference = await GetOrCreateDashboardPreference(currentUser);

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

        var preference = await GetOrCreateDashboardPreference(currentUser);
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

        var preference = await GetOrCreateDashboardPreference(currentUser);
        var blockDefinitions = DashboardLayoutHelper.GetBlockCatalog();
        var definition = blockDefinitions
            .FirstOrDefault(d => d.Type.Equals(request.BlockType, StringComparison.OrdinalIgnoreCase));

        if (definition == null)
        {
            return NotFound();
        }

        var dashboardViewModel = await BuildDashboardViewModel(currentUser, userEmail, preference);
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

    private async Task<UserPreference> GetOrCreateDashboardPreference(User user)
    {
        var preference = await _context.UserPreferences
            .FirstOrDefaultAsync(up => up.UserId == user.Id);

        if (preference != null)
        {
            return preference;
        }

        preference = new UserPreference
        {
            UserId = user.Id,
            PreferredTaskGrouping = "priority",
            ShowTasksPanel = true,
            ShowProductPanel = true,
            ShowRiskPanel = true,
            ShowMilestonePanel = true,
            ShowRemindersPanel = true,
            ShowSuccessPanel = true
        };

        _context.UserPreferences.Add(preference);
        await _context.SaveChangesAsync();

        return preference;
    }

    private async Task<HomeDashboardViewModel> BuildDashboardViewModel(User currentUser, string userEmail, UserPreference preference)
    {
        var myProjects = await _context.Projects
            .Where(p => !p.IsDeleted && (
                p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()) ||
                (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == userEmail.ToLower())
            ))
            .Include(p => p.ProjectContacts)
            .Include(p => p.PrimaryContactUser)
            .Include(p => p.DeliveryPriority)
            .Include(p => p.Milestones)
            .Include(p => p.Issues)
            .Include(p => p.Risks)
            .Include(p => p.Actions)
            .Include(p => p.Decisions)
            .Include(p => p.ProjectProducts)
            .Include(p => p.Successes)
            .OrderBy(p => p.Title)
            .ToListAsync();

        var allProducts = await _productsApiService.GetProductsAsync();
        var myProducts = allProducts
            .Where(p => p.ProductContacts?.Any(pc =>
                pc.UsersPermissionsUser?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
            .OrderBy(p => p.Title)
            .ToList();

        var allActiveMilestones = myProjects.SelectMany(p => p.Milestones.Where(m => !m.IsDeleted)).ToList();
        var milestonesDueThisWeek = allActiveMilestones
            .Where(m => m.DueDate >= DateTime.Today && m.DueDate <= DateTime.Today.AddDays(7)).ToList();
        var overdueMilestones = allActiveMilestones
            .Where(m => m.DueDate < DateTime.Today && m.Status != "complete").ToList();

        var allActiveIssues = myProjects.SelectMany(p => p.Issues.Where(i => !i.IsDeleted)).ToList();
        var highPriorityIssues = allActiveIssues.Where(i => i.Severity == "high" || i.Severity == "critical").ToList();
        var openIssues = allActiveIssues.Where(i => i.Status != "resolved" && i.Status != "closed").ToList();

        var redProjects = myProjects.Where(p => p.RagStatus == "Red").ToList();
        var amberRedProjects = myProjects.Where(p => p.RagStatus == "Amber-Red").ToList();
        var atRiskProjects = redProjects.Concat(amberRedProjects).ToList();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentSuccesses = myProjects.SelectMany(p => p.Successes.Where(s => s.RecordedAt >= thirtyDaysAgo))
            .OrderByDescending(s => s.RecordedAt)
            .Take(10)
            .ToList();

        var projectsNeedingPathToGreen = atRiskProjects.Where(p => string.IsNullOrWhiteSpace(p.PathToGreen)).ToList();

        var expiredOpenMilestones = allActiveMilestones
            .Where(m => m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled")
            .OrderBy(m => m.DueDate)
            .ToList();

        var now = DateTime.UtcNow;
        var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
        var currentMonth = now.Month == 1 ? 12 : now.Month - 1;

        var productsNeedingReturns = new List<(ProductDto Product, ReturnStatus Status, DateTime DueDate)>();
        foreach (var product in myProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)))
        {
            var productReturn = await _context.ProductReturns
                .Where(pr => pr.FipsId == product.FipsId && pr.Year == currentYear && pr.Month == currentMonth)
                .FirstOrDefaultAsync();

            var status = _returnStatusService.CalculateReturnStatus(
                currentYear,
                currentMonth,
                productReturn?.SubmittedDate);

            if (status == ReturnStatus.Due || status == ReturnStatus.Late)
            {
                var dueDate = _returnStatusService.GetReturnDueDate(currentYear, currentMonth);
                productsNeedingReturns.Add((product, status, dueDate));
            }
        }

        var assignedActions = await _context.Actions
            .Include(a => a.Project)
            .Where(a => !a.IsDeleted && (
                (!string.IsNullOrEmpty(a.AssignedToEmail) && a.AssignedToEmail.ToLower() == userEmail.ToLower()) ||
                (a.AssignedToUserId == currentUser.Id)))
            .OrderBy(a => a.DueDate ?? DateTime.MaxValue)
            .ThenBy(a => a.Status)
            .Take(10)
            .ToListAsync();

        var unmonitoredRisks = myProjects
            .SelectMany(p => p.Risks.Where(r => !r.IsDeleted && r.Status != "closed"))
            .Where(r => !r.NextReviewDate.HasValue || r.NextReviewDate.Value < DateTime.Today.AddDays(-30))
            .OrderByDescending(r => r.RiskScore)
            .ThenBy(r => r.Title)
            .Take(10)
            .ToList();

        var priorityTasks = BuildPriorityTasks(
            projectsNeedingPathToGreen,
            expiredOpenMilestones,
            productsNeedingReturns,
            highPriorityIssues,
            assignedActions,
            myProjects);

        var reminders = BuildReminderCards(myProjects, myProducts, productsNeedingReturns, unmonitoredRisks);
        var quickLinkOptions = BuildQuickLinkOptions(preference, myProjects.Any(), myProducts.Any());
        var quickLinks = quickLinkOptions
            .Where(o => o.Selected && !o.Disabled)
            .Take(4)
            .Select(o => new DashboardQuickLink
            {
                Code = o.Code,
                Title = o.Title,
                Description = o.Description,
                Icon = o.Icon,
                Url = o.Url
            })
            .ToList();

        var metrics = new DashboardMetrics
        {
            TasksDue = priorityTasks.Count,
            ServiceHealthIssues = productsNeedingReturns.Count,
            ProjectHealthIssues = atRiskProjects.Count,
            ProductCount = myProducts.Count,
            UpcomingMilestones = milestonesDueThisWeek.Count,
            OpenIssues = openIssues.Count,
            UnreviewedRisks = unmonitoredRisks.Count
        };

        var leadershipAssignments = await _context.UserBusinessAreaRoleAssignments
            .Where(a => a.UserId == currentUser.Id)
            .OrderByDescending(a => a.Role)
            .ToListAsync();

        var leadershipBusinessAreas = leadershipAssignments
            .Select(a => a.BusinessAreaName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var highestRole = leadershipAssignments.Any()
            ? leadershipAssignments.Max(a => a.Role)
            : (LeadershipRoleTier?)null;

        var leadershipProjects = new List<Project>();
        var leadershipMetrics = new DashboardMetrics();
        var enterpriseMetrics = new EnterpriseLeadershipMetrics();
        var activeMissions = new List<Mission>();
        var priorityOutcomes = new List<Objective>();
        var enterpriseAtRiskProjects = new List<Project>();

        // Gather data based on leadership role
        if (highestRole.HasValue)
        {
            if (highestRole == LeadershipRoleTier.PermanentSecretary || 
                highestRole == LeadershipRoleTier.DirectorGeneral || 
                highestRole == LeadershipRoleTier.CLevel)
            {
                // Enterprise-wide view for PS, DG, C-Level
                var allProjects = await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.Issues.Where(i => !i.IsDeleted))
                    .Include(p => p.Risks.Where(r => !r.IsDeleted))
                    .Include(p => p.Actions.Where(a => !a.IsDeleted))
                    .ToListAsync();

                bool IsOpen(string? status) =>
                    !string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "resolved", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase);

                var allOpenIssues = allProjects
                    .SelectMany(p => p.Issues.Where(i => IsOpen(i.Status)))
                    .ToList();

                var allOpenRisks = allProjects
                    .SelectMany(p => p.Risks.Where(r => !string.Equals(r.Status, "closed", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var allOpenActions = allProjects
                    .SelectMany(p => p.Actions.Where(a => IsOpen(a.Status)))
                    .ToList();

                enterpriseAtRiskProjects = allProjects
                    .Where(p => string.Equals(p.RagStatus, "Red", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(p.RagStatus, "Amber-Red", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                enterpriseMetrics = new EnterpriseLeadershipMetrics
                {
                    TotalOpenIssues = allOpenIssues.Count,
                    TotalOpenRisks = allOpenRisks.Count,
                    TotalOpenActions = allOpenActions.Count,
                    TotalAtRiskProjects = enterpriseAtRiskProjects.Count
                };

                // Get active missions
                activeMissions = await _context.Missions
                    .Where(m => !m.IsDeleted && 
                                (m.Status == null || m.Status == "Active" || string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(m => m.Title)
                    .ToListAsync();

                enterpriseMetrics.ActiveMissionsCount = activeMissions.Count;

                // Get priority outcomes (active objectives linked to missions)
                priorityOutcomes = await _context.Objectives
                    .Where(o => !o.IsDeleted && 
                                (o.Status == "active" || string.Equals(o.Status, "active", StringComparison.OrdinalIgnoreCase)) &&
                                o.MissionId.HasValue)
                    .Include(o => o.Mission)
                    .OrderBy(o => o.Title)
                    .ToListAsync();

                enterpriseMetrics.ActivePriorityOutcomesCount = priorityOutcomes.Count;
            }
            else if (highestRole == LeadershipRoleTier.DeputyDirectorOrSro || 
                     highestRole == LeadershipRoleTier.PortfolioLead)
            {
                // Business area specific view for Deputy Director and G6
                if (leadershipBusinessAreas.Any())
                {
                    var normalizedAreas = leadershipBusinessAreas
                        .Select(name => name.ToLower())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    leadershipProjects = await _context.Projects
                        .Where(p => !p.IsDeleted
                                    && !string.IsNullOrWhiteSpace(p.BusinessArea)
                                    && normalizedAreas.Contains(p.BusinessArea!.ToLower()))
                        .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                        .Include(p => p.Issues.Where(i => !i.IsDeleted))
                        .Include(p => p.Risks.Where(r => !r.IsDeleted))
                        .Include(p => p.Actions.Where(a => !a.IsDeleted))
                        .OrderBy(p => p.Title)
                        .ToListAsync();

                    leadershipMetrics = BuildLeadershipMetrics(leadershipProjects);
                }
            }
        }

        var sectionConfig = new DashboardSectionConfig
        {
            ShowTasksPanel = preference.ShowTasksPanel,
            ShowProductPanel = preference.ShowProductPanel,
            ShowRiskPanel = preference.ShowRiskPanel,
            ShowMilestonePanel = preference.ShowMilestonePanel,
            ShowRemindersPanel = preference.ShowRemindersPanel,
            ShowSuccessPanel = preference.ShowSuccessPanel,
            PreferredTaskGrouping = preference.PreferredTaskGrouping,
            DashboardFocus = preference.DashboardFocus
        };

        var firstName = ExtractFirstName(currentUser.Name);

        var blockDefinitions = DashboardLayoutHelper.GetBlockCatalog();
        var blockInstances = DashboardLayoutHelper.ParseLayout(preference.DashboardLayout, blockDefinitions);
        if (!blockInstances.Any())
        {
            blockInstances = DashboardLayoutHelper.GetDefaultLayout(blockDefinitions);
        }

        if (string.IsNullOrWhiteSpace(preference.DashboardLayout))
        {
            preference.DashboardLayout = DashboardLayoutHelper.SerializeLayout(blockInstances);
            preference.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Dashboard VM built for {Email}: {Projects} projects, {Products} products, {Milestones} milestones, {Issues} issues, {Actions} assigned actions",
            userEmail, myProjects.Count, myProducts.Count, allActiveMilestones.Count, allActiveIssues.Count, assignedActions.Count);

        return new HomeDashboardViewModel
        {
            CurrentUser = currentUser,
            FirstName = firstName,
            SectionConfig = sectionConfig,
            Metrics = metrics,
            LeadershipMetrics = leadershipMetrics,
            PriorityTasks = priorityTasks,
            Reminders = reminders,
            QuickLinks = quickLinks,
            QuickLinkOptions = quickLinkOptions,
            BlockDefinitions = blockDefinitions,
            BlockInstances = blockInstances,
            MyProjects = myProjects,
            OversightProjects = leadershipProjects,
            MyProducts = myProducts,
            MilestonesDueThisWeek = milestonesDueThisWeek,
            OverdueMilestones = overdueMilestones,
            HighPriorityIssues = highPriorityIssues,
            UnmonitoredRisks = unmonitoredRisks,
            AssignedActions = assignedActions,
            AtRiskProjects = atRiskProjects,
            ProjectsNeedingPathToGreen = projectsNeedingPathToGreen,
            RecentSuccesses = recentSuccesses,
            ProductsNeedingReturns = productsNeedingReturns,
            LeadershipAssignments = leadershipAssignments,
            LeadershipBusinessAreas = leadershipBusinessAreas,
            HighestLeadershipRole = highestRole,
            EnterpriseMetrics = enterpriseMetrics,
            ActiveMissions = activeMissions,
            PriorityOutcomes = priorityOutcomes,
            EnterpriseAtRiskProjects = enterpriseAtRiskProjects
        };
    }

    private static DashboardMetrics BuildLeadershipMetrics(IReadOnlyCollection<Project> oversightProjects)
    {
        if (!oversightProjects.Any())
        {
            return new DashboardMetrics();
        }

        bool IsOpen(string? status) =>
            !string.Equals(status, "closed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "resolved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "complete", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase);

        var oversightIssues = oversightProjects
            .SelectMany(p => p.Issues.Where(i => !i.IsDeleted && IsOpen(i.Status)))
            .ToList();

        var oversightRisks = oversightProjects
            .SelectMany(p => p.Risks.Where(r => !r.IsDeleted && !string.Equals(r.Status, "closed", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var oversightActions = oversightProjects
            .SelectMany(p => p.Actions.Where(a => !a.IsDeleted && IsOpen(a.Status)))
            .ToList();

        var riskReviewOverdue = oversightRisks
            .Where(r => !r.NextReviewDate.HasValue || r.NextReviewDate.Value < DateTime.Today.AddDays(-30))
            .Count();

        var upcomingMilestones = oversightProjects
            .SelectMany(p => p.Milestones.Where(m => !m.IsDeleted && m.DueDate >= DateTime.Today && m.DueDate <= DateTime.Today.AddDays(14)))
            .Count();

        var atRiskProjects = oversightProjects
            .Count(p => string.Equals(p.RagStatus, "Red", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.RagStatus, "Amber-Red", StringComparison.OrdinalIgnoreCase));

        return new DashboardMetrics
        {
            TasksDue = oversightActions.Count,
            ProjectHealthIssues = atRiskProjects,
            UpcomingMilestones = upcomingMilestones,
            OpenIssues = oversightIssues.Count,
            UnreviewedRisks = riskReviewOverdue
        };
    }

    private static string ExtractFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "User";
        }

        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return nameParts.Length > 0 ? nameParts[0] : fullName;
    }

    private List<DashboardTaskItem> BuildPriorityTasks(
        IEnumerable<Project> projectsNeedingPathToGreen,
        IEnumerable<Milestone> expiredOpenMilestones,
        IEnumerable<(ProductDto Product, ReturnStatus Status, DateTime DueDate)> productsNeedingReturns,
        IEnumerable<Issue> highPriorityIssues,
        IEnumerable<Models.Action> assignedActions,
        IReadOnlyCollection<Project> allProjects)
    {
        var tasks = new List<DashboardTaskItem>();

        foreach (var project in projectsNeedingPathToGreen.Take(5))
        {
            tasks.Add(new DashboardTaskItem
            {
                Category = "Path to green",
                Title = project.Title,
                Description = $"Document how this project will return to green status ({project.ProjectCode}).",
                StatusLabel = "Missing narrative",
                PriorityBadgeClass = "badge badge-danger",
                LinkLabel = "Open project",
                LinkUrl = Url.Action("Details", "Project", new { id = project.Id, tab = "rag" }) ?? "#"
            });
        }

        foreach (var milestone in expiredOpenMilestones.Take(5))
        {
            var milestoneProject = allProjects.FirstOrDefault(p => p.Id == milestone.ProjectId);
            tasks.Add(new DashboardTaskItem
            {
                Category = "Milestone",
                Title = milestone.Name,
                Description = milestoneProject != null
                    ? $"{milestoneProject.Title} is { (DateTime.Today - milestone.DueDate).Days } day(s) over the agreed date."
                    : "Milestone is overdue and waiting for an update.",
                StatusLabel = "Overdue",
                PriorityBadgeClass = "badge badge-warning",
                DueDate = milestone.DueDate,
                LinkLabel = "Update milestone",
                LinkUrl = Url.Action("Details", "Project", new { id = milestone.ProjectId, tab = "milestones" }) ?? "#"
            });
        }

        foreach (var (product, status, dueDate) in productsNeedingReturns.Take(5))
        {
            tasks.Add(new DashboardTaskItem
            {
                Category = "Operational return",
                Title = product.Title,
                Description = status == ReturnStatus.Late
                    ? "Return is late – submit as soon as possible."
                    : "Return due this month – submit performance metrics.",
                StatusLabel = status == ReturnStatus.Late ? "Late" : "Due",
                PriorityBadgeClass = status == ReturnStatus.Late ? "badge badge-danger" : "badge badge-warning",
                DueDate = dueDate,
                LinkLabel = "Open performance metrics",
                LinkUrl = Url.Action("PerformanceMetrics", "ProductReporting", new { fipsId = product.FipsId }) ?? "#"
            });
        }

        foreach (var issue in highPriorityIssues.Take(5))
        {
            tasks.Add(new DashboardTaskItem
            {
                Category = "Issue",
                Title = issue.Title,
                Description = "Review severity, owners and current mitigation plan.",
                StatusLabel = issue.Severity?.Equals("critical", StringComparison.OrdinalIgnoreCase) == true ? "Critical" : "High",
                PriorityBadgeClass = issue.Severity?.Equals("critical", StringComparison.OrdinalIgnoreCase) == true ? "badge badge-danger" : "badge badge-warning",
                DueDate = issue.TargetResolutionDate,
                LinkLabel = "View issue",
                LinkUrl = Url.Action("Details", "Issue", new { id = issue.Id }) ?? "#"
            });
        }

        foreach (var action in assignedActions.Take(5))
        {
            tasks.Add(new DashboardTaskItem
            {
                Category = "Action",
                Title = action.Title,
                Description = action.Project != null
                    ? $"Assigned to you for {action.Project.Title}."
                    : "Action assigned to you.",
                StatusLabel = action.Status?.Replace("_", " "),
                PriorityBadgeClass = action.Blocked ? "badge badge-danger" : "badge badge-info",
                DueDate = action.DueDate,
                LinkLabel = "Review action",
                LinkUrl = Url.Action("Details", "Action", new { id = action.Id }) ?? "#"
            });
        }

        return tasks.Take(15).ToList();
    }

    private List<DashboardReminder> BuildReminderCards(
        IReadOnlyCollection<Project> myProjects,
        IReadOnlyCollection<ProductDto> myProducts,
        IReadOnlyCollection<(ProductDto Product, ReturnStatus Status, DateTime DueDate)> productsNeedingReturns,
        IReadOnlyCollection<Risk> unmonitoredRisks)
    {
        var reminders = new List<DashboardReminder>();

        if (myProjects.Any())
        {
            var nextFriday = NextWeekday(DateTime.Today, DayOfWeek.Friday);
            reminders.Add(new DashboardReminder
            {
                Title = "Share your weekly delivery update",
                Description = $"Capture blockers and changes across {myProjects.Count} project(s) before {nextFriday:dddd d MMM}.",
                Icon = "far fa-calendar-check",
                FrequencyBadge = "Weekly",
                Tone = "primary",
                LinkLabel = "Open projects",
                LinkUrl = Url.Action("Index", "Project") ?? "#"
            });
        }

        if (productsNeedingReturns.Any())
        {
            var earliestDueDate = productsNeedingReturns.Min(r => r.DueDate);
            reminders.Add(new DashboardReminder
            {
                Title = "Submit monthly performance metrics",
                Description = $"{productsNeedingReturns.Count} product(s) still need metrics for {earliestDueDate:MMMM}.",
                Icon = "fas fa-chart-line",
                FrequencyBadge = "Monthly",
                Tone = "warning",
                LinkLabel = "Go to performance hub",
                LinkUrl = Url.Action("Index", "Products") ?? "#"
            });
        }

        if (unmonitoredRisks.Any())
        {
            reminders.Add(new DashboardReminder
            {
                Title = "Review ageing risks",
                Description = $"{unmonitoredRisks.Count} risk(s) have not been reviewed in the past month.",
                Icon = "fas fa-exclamation-triangle",
                FrequencyBadge = "Fortnightly",
                Tone = "danger",
                LinkLabel = "Open risk register",
                LinkUrl = Url.Action("Index", "Risk", new { viewScope = "assigned_to_me" }) ?? "#"
            });
        }

        if (myProducts.Any())
        {
            reminders.Add(new DashboardReminder
            {
                Title = "Complete DDaT service health check",
                Description = "Confirm each product still meets the service standard and report gaps.",
                Icon = "fas fa-notes-medical",
                FrequencyBadge = "Quarterly",
                Tone = "info",
                LinkLabel = "Service health guidance",
                LinkUrl = Url.Action("Index", "DdtReports") ?? "#"
            });
        }

        var discoveryCount = myProducts.Count(p => p.Phase?.Equals("Discovery", StringComparison.OrdinalIgnoreCase) == true);
        if (discoveryCount > 0)
        {
            reminders.Add(new DashboardReminder
            {
                Title = "Book discovery peer review",
                Description = $"{discoveryCount} service(s) are in discovery and should confirm an external peer review slot.",
                Icon = "fas fa-compass",
                FrequencyBadge = "Lifecycle",
                Tone = "info",
                LinkLabel = "Book review",
                LinkUrl = Url.Action("Roadmap", "Home") ?? "#"
            });
        }

        var alphaCount = myProducts.Count(p => p.Phase?.Equals("Alpha", StringComparison.OrdinalIgnoreCase) == true);
        if (alphaCount > 0)
        {
            reminders.Add(new DashboardReminder
            {
                Title = "Schedule alpha service assessment",
                Description = $"{alphaCount} product(s) are preparing for alpha – secure a service assessment slot now.",
                Icon = "fas fa-flag-checkered",
                FrequencyBadge = "Lifecycle",
                Tone = "primary",
                LinkLabel = "Assessment guidance",
                LinkUrl = Url.Action("Roadmap", "Home") ?? "#"
            });
        }

        var privateBetaCount = myProducts.Count(p => p.Phase?.Equals("Private beta", StringComparison.OrdinalIgnoreCase) == true);
        if (privateBetaCount > 0)
        {
            reminders.Add(new DashboardReminder
            {
                Title = "Complete operational readiness review",
                Description = $"{privateBetaCount} private beta service(s) must evidence monitoring, alerting and on-call cover.",
                Icon = "fas fa-clipboard-list",
                FrequencyBadge = "Lifecycle",
                Tone = "warning",
                LinkLabel = "Operational readiness checklist",
                LinkUrl = Url.Action("Roadmap", "Home") ?? "#"
            });
        }

        var publicBetaCount = myProducts.Count(p => p.Phase?.Equals("Public beta", StringComparison.OrdinalIgnoreCase) == true);
        if (publicBetaCount > 0)
        {
            reminders.Add(new DashboardReminder
            {
                Title = "Arrange public beta assessment",
                Description = $"{publicBetaCount} service(s) are live to real users – line up the public beta assessment.",
                Icon = "fas fa-users",
                FrequencyBadge = "Lifecycle",
                Tone = "success",
                LinkLabel = "Book assessment",
                LinkUrl = Url.Action("Roadmap", "Home") ?? "#"
            });
        }

        return reminders;
    }

    private IReadOnlyCollection<DashboardQuickLinkOption> BuildQuickLinkOptions(UserPreference preference, bool hasProjects, bool hasProducts)
    {
        var selectedCodes = ParseQuickLinkCodes(preference.QuickLaunchShortcuts);
        var defaultCodes = new List<string> { "project-updates", "log-risk", "metrics", "book-assessment" };

        if (!selectedCodes.Any())
        {
            selectedCodes = defaultCodes;
        }

        var options = new List<DashboardQuickLinkOption>
        {
            new()
            {
                Code = "project-updates",
                Title = "Update delivery story",
                Description = "Refresh RAG, blockers and path to green.",
                Icon = "fas fa-clipboard-check",
                Url = Url.Action("Index", "Project") ?? "#",
                Disabled = !hasProjects
            },
            new()
            {
                Code = "log-risk",
                Title = "Log a risk",
                Description = "Capture new threats and mitigation plans.",
                Icon = "fas fa-exclamation-circle",
                Url = Url.Action("Create", "Risk") ?? "#"
            },
            new()
            {
                Code = "metrics",
                Title = "Performance metrics",
                Description = "Enter or review product performance data.",
                Icon = "fas fa-chart-line",
                Url = Url.Action("Index", "Products") ?? "#",
                Disabled = !hasProducts
            },
            new()
            {
                Code = "book-assessment",
                Title = "Book service assessment",
                Description = "Secure a DDaT assessment slot for your service.",
                Icon = "fas fa-clipboard",
                Url = Url.Action("Roadmap", "Home") ?? "#"
            },
            new()
            {
                Code = "add-milestone",
                Title = "Add milestone update",
                Description = "Keep upcoming delivery checkpoints up to date.",
                Icon = "fas fa-stream",
                Url = Url.Action("Index", "Milestone", new { tab = "assigned_to_me" }) ?? "#"
            },
            new()
            {
                Code = "report-issue",
                Title = "Raise issue",
                Description = "Escalate blockers that need wider attention.",
                Icon = "fas fa-bolt",
                Url = Url.Action("Create", "Issue") ?? "#"
            },
            new()
            {
                Code = "demand",
                Title = "Submit demand request",
                Description = "Kick off a new demand intake case.",
                Icon = "fas fa-inbox",
                Url = Url.Action("Requests", "DemandManagement") ?? "#"
            }
        };

        foreach (var option in options)
        {
            option.Selected = selectedCodes.Contains(option.Code) && !option.Disabled;
        }

        if (!options.Any(o => o.Selected))
        {
            foreach (var code in defaultCodes)
            {
                var fallback = options.FirstOrDefault(o => o.Code == code && !o.Disabled);
                if (fallback != null)
                {
                    fallback.Selected = true;
                }
            }
        }

        return options;
    }

    private static List<string> ParseQuickLinkCodes(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return new List<string>();
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(code => code.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
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

    public IActionResult Error()
    {
        return View();
    }

    public IActionResult Roadmap()
    {
        return View();
    }
}

