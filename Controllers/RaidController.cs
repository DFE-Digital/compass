using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class RaidController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<RaidController> _logger;

    public RaidController(CompassDbContext context, IProductsApiService productsApiService, ILogger<RaidController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Raid
    public async Task<IActionResult> Index(
        string? viewType = "all", // all, risks, issues, actions
        string? groupBy = "none", // none, business_area, owner, project
        string? businessArea = null,
        string? ownerEmail = null,
        string? fipsId = null,
        string? status = null,
        int page = 1,
        int pageSize = 50,
        string? sortBy = null,
        string? sortOrder = "asc")
    {
        var userEmail = User.Identity?.Name;
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail!.ToLower());

        // Get user's leadership business areas
        var userLeadershipBusinessAreas = new List<string>();
        if (currentUser != null)
        {
            var leadershipAssignments = await _context.UserBusinessAreaRoleAssignments
                .Where(a => a.UserId == currentUser.Id)
                .Select(a => a.BusinessAreaName)
                .Distinct()
                .ToListAsync();
            userLeadershipBusinessAreas = leadershipAssignments;
        }

        // Build queries for each type
        var risksQuery = _context.Risks
            .Include(r => r.Objective)
            .Include(r => r.RiskTier)
            .Where(r => !r.IsDeleted);
        
        // Get all users for owner name lookups
        var allUsers = await _context.Users.ToListAsync();
        var userByEmail = allUsers.ToDictionary(u => u.Email?.ToLower() ?? "", u => u);

        var issuesQuery = _context.Issues
            .Include(i => i.OwnerUser)
            .Include(i => i.Objective)
            .Where(i => !i.IsDeleted);

        var actionsQuery = _context.Actions
            .Include(a => a.Objective)
            .Include(a => a.AssignedToUser)
            .Where(a => !a.IsDeleted);

        // Apply UserLeadership scope - filter by business areas where user has leadership
        if (userLeadershipBusinessAreas.Any())
        {
            risksQuery = risksQuery.Where(r => r.BusinessArea != null && userLeadershipBusinessAreas.Contains(r.BusinessArea));
            issuesQuery = issuesQuery.Where(i => i.BusinessArea != null && userLeadershipBusinessAreas.Contains(i.BusinessArea));
            actionsQuery = actionsQuery.Where(a => a.BusinessArea != null && userLeadershipBusinessAreas.Contains(a.BusinessArea));
        }

        // Apply filters
        if (!string.IsNullOrEmpty(businessArea))
        {
            risksQuery = risksQuery.Where(r => r.BusinessArea == businessArea);
            issuesQuery = issuesQuery.Where(i => i.BusinessArea == businessArea);
            actionsQuery = actionsQuery.Where(a => a.BusinessArea == businessArea);
        }

        if (!string.IsNullOrEmpty(ownerEmail))
        {
            risksQuery = risksQuery.Where(r => r.OwnerEmail != null && r.OwnerEmail.ToLower() == ownerEmail.ToLower());
            issuesQuery = issuesQuery.Where(i => i.OwnerUser != null && i.OwnerUser.Email != null && i.OwnerUser.Email.ToLower() == ownerEmail.ToLower());
            actionsQuery = actionsQuery.Where(a => a.AssignedToUser != null && a.AssignedToUser.Email != null && a.AssignedToUser.Email.ToLower() == ownerEmail.ToLower());
        }

        if (!string.IsNullOrEmpty(fipsId))
        {
            risksQuery = risksQuery.Where(r => r.FipsId == fipsId);
            issuesQuery = issuesQuery.Where(i => i.FipsId == fipsId);
            actionsQuery = actionsQuery.Where(a => a.FipsId == fipsId);
        }

        if (!string.IsNullOrEmpty(status))
        {
            risksQuery = risksQuery.Where(r => r.Status == status);
            issuesQuery = issuesQuery.Where(i => i.Status == status);
            actionsQuery = actionsQuery.Where(a => a.Status == status);
        }

        // Get counts for pagination (before sorting and pagination)
        var totalRisks = await risksQuery.CountAsync();
        var totalIssues = await issuesQuery.CountAsync();
        var totalActions = await actionsQuery.CountAsync();

        // Apply sorting - handle each type separately
        risksQuery = ApplyRiskSorting(risksQuery, sortBy, sortOrder);
        issuesQuery = ApplyIssueSorting(issuesQuery, sortBy, sortOrder);
        actionsQuery = ApplyActionSorting(actionsQuery, sortBy, sortOrder);

        // Get data based on view type with pagination
        var risks = new List<Risk>();
        var issues = new List<Issue>();
        var actions = new List<Models.Action>();

        // Apply pagination if grouping is none
        if (groupBy == "none")
        {
            var skip = (page - 1) * pageSize;

            if (viewType == "all" || viewType == "risks")
            {
                risks = await risksQuery.Skip(skip).Take(pageSize).ToListAsync();
            }

            if (viewType == "all" || viewType == "issues")
            {
                issues = await issuesQuery.Skip(skip).Take(pageSize).ToListAsync();
            }

            if (viewType == "all" || viewType == "actions")
            {
                actions = await actionsQuery.Skip(skip).Take(pageSize).ToListAsync();
            }
        }
        else
        {
            // For grouped views, get all data
            if (viewType == "all" || viewType == "risks")
            {
                risks = await risksQuery.ToListAsync();
            }

            if (viewType == "all" || viewType == "issues")
            {
                issues = await issuesQuery.ToListAsync();
            }

            if (viewType == "all" || viewType == "actions")
            {
                actions = await actionsQuery.ToListAsync();
            }
        }

        // Get filter options
        var businessAreas = await _productsApiService.GetBusinessAreasAsync();
        var allProducts = await _productsApiService.GetProductsAsync();
        
        // Get distinct owners
        var riskOwners = await _context.Risks
            .Where(r => !r.IsDeleted && !string.IsNullOrEmpty(r.OwnerEmail))
            .Select(r => r.OwnerEmail!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        var issueOwners = await _context.Issues
            .Where(i => !i.IsDeleted && i.OwnerUser != null && i.OwnerUser.Email != null)
            .Select(i => i.OwnerUser!.Email!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        var actionOwners = await _context.Actions
            .Where(a => !a.IsDeleted && a.AssignedToUser != null && a.AssignedToUser.Email != null)
            .Select(a => a.AssignedToUser!.Email!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        var allOwners = riskOwners.Union(issueOwners).Union(actionOwners).Distinct().OrderBy(e => e).ToList();

        // Get distinct FipsIds
        var fipsIds = await _context.Risks
            .Where(r => !r.IsDeleted && !string.IsNullOrEmpty(r.FipsId))
            .Select(r => r.FipsId!)
            .Union(_context.Issues.Where(i => !i.IsDeleted && !string.IsNullOrEmpty(i.FipsId)).Select(i => i.FipsId!))
            .Union(_context.Actions.Where(a => !a.IsDeleted && !string.IsNullOrEmpty(a.FipsId)).Select(a => a.FipsId!))
            .Distinct()
            .OrderBy(f => f)
            .ToListAsync();

        var productsInUse = allProducts
            .Where(p => fipsIds.Contains(p.FipsId ?? ""))
            .OrderBy(p => p.Title)
            .ToList();

        ViewBag.ViewType = viewType;
        ViewBag.GroupBy = groupBy;
        ViewBag.CurrentBusinessArea = businessArea;
        ViewBag.CurrentOwnerEmail = ownerEmail;
        ViewBag.CurrentFipsId = fipsId;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.SortBy = sortBy;
        ViewBag.SortOrder = sortOrder;
        ViewBag.TotalRisks = totalRisks;
        ViewBag.TotalIssues = totalIssues;
        ViewBag.TotalActions = totalActions;
        ViewBag.BusinessAreas = businessAreas;
        ViewBag.Products = productsInUse;
        ViewBag.Owners = allOwners;
        ViewBag.UserLeadershipBusinessAreas = userLeadershipBusinessAreas;
        ViewBag.UserByEmail = userByEmail;

        // Check if this is the homepage (no filters, default view)
        var isHomepage = string.IsNullOrEmpty(businessArea) && 
                        string.IsNullOrEmpty(ownerEmail) && 
                        string.IsNullOrEmpty(fipsId) && 
                        string.IsNullOrEmpty(status) &&
                        (viewType == "all" || string.IsNullOrEmpty(viewType)) &&
                        (groupBy == "none" || string.IsNullOrEmpty(groupBy));

        // Build dashboard data for homepage
        RaidDashboardViewModel? dashboardData = null;
        if (isHomepage)
        {
            dashboardData = await BuildDashboardDataAsync(userLeadershipBusinessAreas);
        }

        // Build grouped data if needed
        RaidGroupedViewModel? groupedData = null;
        if (groupBy != "none" && !string.IsNullOrEmpty(groupBy))
        {
            groupedData = await BuildGroupedDataAsync(risks, issues, actions, groupBy, userLeadershipBusinessAreas);
        }

        var viewModel = new RaidIndexViewModel
        {
            Risks = risks,
            Issues = issues,
            Actions = actions,
            ViewType = viewType ?? "all",
            GroupBy = groupBy ?? "none",
            GroupedData = groupedData,
            DashboardData = dashboardData
        };

        return View(viewModel);
    }

    private async Task<RaidDashboardViewModel> BuildDashboardDataAsync(List<string> userLeadershipBusinessAreas)
    {
        var dashboard = new RaidDashboardViewModel();

        // Get all open items (not closed/resolved/done)
        var openRisksQuery = _context.Risks
            .Where(r => !r.IsDeleted && r.Status != "closed");
        
        var openIssuesQuery = _context.Issues
            .Where(i => !i.IsDeleted && i.Status != "closed" && i.Status != "resolved");
        
        var openActionsQuery = _context.Actions
            .Where(a => !a.IsDeleted && a.Status != "done" && a.Status != "cancelled");

        // Apply leadership filter if needed
        if (userLeadershipBusinessAreas.Any())
        {
            openRisksQuery = openRisksQuery.Where(r => r.BusinessArea != null && userLeadershipBusinessAreas.Contains(r.BusinessArea));
            openIssuesQuery = openIssuesQuery.Where(i => i.BusinessArea != null && userLeadershipBusinessAreas.Contains(i.BusinessArea));
            openActionsQuery = openActionsQuery.Where(a => a.BusinessArea != null && userLeadershipBusinessAreas.Contains(a.BusinessArea));
        }

        dashboard.OpenRisksCount = await openRisksQuery.CountAsync();
        dashboard.OpenIssuesCount = await openIssuesQuery.CountAsync();
        dashboard.OpenActionsCount = await openActionsQuery.CountAsync();

        // Get top 10 critical risks (RiskScore >= 20)
        var criticalRisks = await openRisksQuery
            .Include(r => r.OwnerUser)
            .Where(r => r.RiskScore >= 20)
            .OrderByDescending(r => r.RiskScore)
            .ThenByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();
        dashboard.TopCriticalRisks = criticalRisks;

        // Get top 10 critical issues (Severity = "critical")
        var criticalIssues = await openIssuesQuery
            .Include(i => i.OwnerUser)
            .Where(i => i.Severity == "critical")
            .OrderByDescending(i => i.CreatedAt)
            .Take(10)
            .ToListAsync();
        dashboard.TopCriticalIssues = criticalIssues;

        // Get all business areas with leadership info
        var allBusinessAreas = await _productsApiService.GetBusinessAreasAsync();
        
        // Get all leadership assignments
        var leadershipAssignments = await _context.UserBusinessAreaRoleAssignments
            .Include(a => a.User)
            .Where(a => a.Role == LeadershipRoleTier.DeputyDirectorOrSro || a.Role == LeadershipRoleTier.DirectorGeneral)
            .ToListAsync();

        foreach (var businessArea in allBusinessAreas)
        {
            // Apply leadership filter if needed
            if (userLeadershipBusinessAreas.Any() && !userLeadershipBusinessAreas.Contains(businessArea))
            {
                continue;
            }

            var areaAssignments = leadershipAssignments
                .Where(a => a.BusinessAreaName == businessArea)
                .ToList();

            var deputyDirectorSro = areaAssignments
                .FirstOrDefault(a => a.Role == LeadershipRoleTier.DeputyDirectorOrSro)?.User;

            var directorGeneral = areaAssignments
                .FirstOrDefault(a => a.Role == LeadershipRoleTier.DirectorGeneral)?.User;

            // Get counts for this business area
            var areaRisksQuery = _context.Risks
                .Where(r => !r.IsDeleted && r.BusinessArea == businessArea && r.Status != "closed");
            
            var areaIssuesQuery = _context.Issues
                .Where(i => !i.IsDeleted && i.BusinessArea == businessArea && i.Status != "closed" && i.Status != "resolved");
            
            var areaActionsQuery = _context.Actions
                .Where(a => !a.IsDeleted && a.BusinessArea == businessArea && a.Status != "done" && a.Status != "cancelled");

            // Apply leadership filter if needed
            if (userLeadershipBusinessAreas.Any())
            {
                areaRisksQuery = areaRisksQuery.Where(r => userLeadershipBusinessAreas.Contains(businessArea));
                areaIssuesQuery = areaIssuesQuery.Where(i => userLeadershipBusinessAreas.Contains(businessArea));
                areaActionsQuery = areaActionsQuery.Where(a => userLeadershipBusinessAreas.Contains(businessArea));
            }

            var areaRisksCount = await areaRisksQuery.CountAsync();
            var areaIssuesCount = await areaIssuesQuery.CountAsync();
            var areaActionsCount = await areaActionsQuery.CountAsync();

            // Only include business areas that have RAID items or leadership
            if (areaRisksCount > 0 || areaIssuesCount > 0 || areaActionsCount > 0 || deputyDirectorSro != null || directorGeneral != null)
            {
                dashboard.BusinessAreaSummaries.Add(new BusinessAreaSummary
                {
                    BusinessArea = businessArea,
                    DeputyDirectorSro = deputyDirectorSro,
                    DirectorGeneral = directorGeneral,
                    OpenRisksCount = areaRisksCount,
                    OpenIssuesCount = areaIssuesCount,
                    OpenActionsCount = areaActionsCount
                });
            }
        }

        // Sort by business area name
        dashboard.BusinessAreaSummaries = dashboard.BusinessAreaSummaries
            .OrderBy(b => b.BusinessArea)
            .ToList();

        return dashboard;
    }

    // GET: Raid/Summary
    public async Task<IActionResult> Summary(
        string groupBy, // business_area, owner, project
        string groupValue, // the value of the group (e.g., business area name, owner email, fipsId)
        string? itemType = "all", // all, risks, issues, actions, decisions
        string? status = null,
        int page = 1,
        int pageSize = 50)
    {
        var userEmail = User.Identity?.Name;
        var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail!.ToLower());

        // Get user's leadership business areas
        var userLeadershipBusinessAreas = new List<string>();
        if (currentUser != null)
        {
            var leadershipAssignments = await _context.UserBusinessAreaRoleAssignments
                .Where(a => a.UserId == currentUser.Id)
                .Select(a => a.BusinessAreaName)
                .Distinct()
                .ToListAsync();
            userLeadershipBusinessAreas = leadershipAssignments;
        }

        // Get all users for owner name lookups
        var allUsers = await _context.Users.ToListAsync();
        var userByEmail = allUsers.ToDictionary(u => u.Email?.ToLower() ?? "", u => u);

        // Build queries based on group type
        var risksQuery = _context.Risks
            .Include(r => r.Objective)
            .Include(r => r.RiskTier)
            .Where(r => !r.IsDeleted);

        var issuesQuery = _context.Issues
            .Include(i => i.OwnerUser)
            .Include(i => i.Objective)
            .Where(i => !i.IsDeleted);

        var actionsQuery = _context.Actions
            .Include(a => a.Objective)
            .Include(a => a.AssignedToUser)
            .Where(a => !a.IsDeleted);

        var decisionsQuery = _context.Decisions
            .Include(d => d.OwnerUser)
            .Where(d => !d.IsDeleted);

        // Apply UserLeadership scope
        if (userLeadershipBusinessAreas.Any())
        {
            risksQuery = risksQuery.Where(r => r.BusinessArea != null && userLeadershipBusinessAreas.Contains(r.BusinessArea));
            issuesQuery = issuesQuery.Where(i => i.BusinessArea != null && userLeadershipBusinessAreas.Contains(i.BusinessArea));
            actionsQuery = actionsQuery.Where(a => a.BusinessArea != null && userLeadershipBusinessAreas.Contains(a.BusinessArea));
            decisionsQuery = decisionsQuery.Where(d => d.BusinessArea != null && userLeadershipBusinessAreas.Contains(d.BusinessArea));
        }

        // Apply group filter
        switch (groupBy)
        {
            case "business_area":
                risksQuery = risksQuery.Where(r => r.BusinessArea == groupValue);
                issuesQuery = issuesQuery.Where(i => i.BusinessArea == groupValue);
                actionsQuery = actionsQuery.Where(a => a.BusinessArea == groupValue);
                decisionsQuery = decisionsQuery.Where(d => d.BusinessArea == groupValue);
                break;
            case "owner":
                risksQuery = risksQuery.Where(r => r.OwnerEmail != null && r.OwnerEmail.ToLower() == groupValue.ToLower());
                issuesQuery = issuesQuery.Where(i => i.OwnerUser != null && i.OwnerUser.Email != null && i.OwnerUser.Email.ToLower() == groupValue.ToLower());
                actionsQuery = actionsQuery.Where(a => a.AssignedToUser != null && a.AssignedToUser.Email != null && a.AssignedToUser.Email.ToLower() == groupValue.ToLower());
                decisionsQuery = decisionsQuery.Where(d => d.OwnerUser != null && d.OwnerUser.Email != null && d.OwnerUser.Email.ToLower() == groupValue.ToLower());
                break;
            case "project":
                risksQuery = risksQuery.Where(r => r.FipsId == groupValue);
                issuesQuery = issuesQuery.Where(i => i.FipsId == groupValue);
                actionsQuery = actionsQuery.Where(a => a.FipsId == groupValue);
                decisionsQuery = decisionsQuery.Where(d => d.FipsId == groupValue);
                break;
        }

        // Apply status filter
        if (!string.IsNullOrEmpty(status))
        {
            risksQuery = risksQuery.Where(r => r.Status == status);
            issuesQuery = issuesQuery.Where(i => i.Status == status);
            actionsQuery = actionsQuery.Where(a => a.Status == status);
            decisionsQuery = decisionsQuery.Where(d => d.Status == status);
        }

        // Get counts
        var totalRisks = await risksQuery.CountAsync();
        var totalIssues = await issuesQuery.CountAsync();
        var totalActions = await actionsQuery.CountAsync();
        var totalDecisions = await decisionsQuery.CountAsync();

        // Get data with pagination
        var skip = (page - 1) * pageSize;
        var risks = new List<Risk>();
        var issues = new List<Issue>();
        var actions = new List<Models.Action>();
        var decisions = new List<Decision>();

        if (itemType == "all" || itemType == "risks")
        {
            risks = await risksQuery.OrderByDescending(r => r.RiskScore).Skip(skip).Take(pageSize).ToListAsync();
        }

        if (itemType == "all" || itemType == "issues")
        {
            issues = await issuesQuery.OrderByDescending(i => i.Severity).Skip(skip).Take(pageSize).ToListAsync();
        }

        if (itemType == "all" || itemType == "actions")
        {
            actions = await actionsQuery.OrderBy(a => a.DueDate ?? DateTime.MaxValue).Skip(skip).Take(pageSize).ToListAsync();
        }

        if (itemType == "all" || itemType == "decisions")
        {
            decisions = await decisionsQuery.OrderByDescending(d => d.DecisionDate ?? d.CreatedAt).Skip(skip).Take(pageSize).ToListAsync();
        }

        // Get group display name
        string groupDisplayName = groupValue;
        if (groupBy == "owner" && userByEmail.ContainsKey(groupValue.ToLower()))
        {
            groupDisplayName = userByEmail[groupValue.ToLower()].Name ?? groupValue;
        }
        else if (groupBy == "project")
        {
            var products = await _productsApiService.GetProductsAsync();
            var product = products.FirstOrDefault(p => p.FipsId == groupValue);
            if (product != null)
            {
                groupDisplayName = product.Title;
            }
        }

        ViewBag.GroupBy = groupBy;
        ViewBag.GroupValue = groupValue;
        ViewBag.GroupDisplayName = groupDisplayName;
        ViewBag.ItemType = itemType;
        ViewBag.Status = status;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRisks = totalRisks;
        ViewBag.TotalIssues = totalIssues;
        ViewBag.TotalActions = totalActions;
        ViewBag.TotalDecisions = totalDecisions;
        ViewBag.UserByEmail = userByEmail;

        var viewModel = new RaidSummaryViewModel
        {
            Risks = risks,
            Issues = issues,
            Actions = actions,
            Decisions = decisions,
            GroupBy = groupBy,
            GroupValue = groupValue,
            GroupDisplayName = groupDisplayName,
            ItemType = itemType ?? "all"
        };

        return View(viewModel);
    }

    private async Task<RaidGroupedViewModel> BuildGroupedDataAsync(
        List<Risk> risks,
        List<Issue> issues,
        List<Models.Action> actions,
        string groupBy,
        List<string> userLeadershipBusinessAreas)
    {
        var groupedData = new RaidGroupedViewModel { GroupBy = groupBy };

        if (groupBy == "business_area")
        {
            // Get all business areas from risks, issues, actions, and decisions
            var businessAreas = risks.Where(r => !string.IsNullOrEmpty(r.BusinessArea))
                .Select(r => r.BusinessArea!)
                .Union(issues.Where(i => !string.IsNullOrEmpty(i.BusinessArea)).Select(i => i.BusinessArea!))
                .Union(actions.Where(a => !string.IsNullOrEmpty(a.BusinessArea)).Select(a => a.BusinessArea!))
                .Distinct()
                .OrderBy(ba => ba)
                .ToList();

            // Apply leadership filter if needed
            if (userLeadershipBusinessAreas.Any())
            {
                businessAreas = businessAreas.Where(ba => userLeadershipBusinessAreas.Contains(ba)).ToList();
            }

            foreach (var ba in businessAreas)
            {
                var riskCount = risks.Count(r => r.BusinessArea == ba);
                var issueCount = issues.Count(i => i.BusinessArea == ba);
                var actionCount = actions.Count(a => a.BusinessArea == ba);
                
                // Get decision count
                var decisionQuery = _context.Decisions
                    .Where(d => !d.IsDeleted && d.BusinessArea == ba);
                
                // Apply leadership filter if needed
                if (userLeadershipBusinessAreas.Any())
                {
                    decisionQuery = decisionQuery.Where(d => d.BusinessArea != null && userLeadershipBusinessAreas.Contains(d.BusinessArea));
                }
                
                var decisionCount = await decisionQuery.CountAsync();

                if (userLeadershipBusinessAreas.Any() && !userLeadershipBusinessAreas.Contains(ba))
                {
                    continue;
                }

                groupedData.BusinessAreaGroups.Add(new BusinessAreaGroup
                {
                    BusinessArea = ba,
                    RiskCount = riskCount,
                    IssueCount = issueCount,
                    ActionCount = actionCount,
                    DecisionCount = decisionCount
                });
            }
        }
        else if (groupBy == "owner")
        {
            // Get all owners
            var ownerEmails = risks.Where(r => !string.IsNullOrEmpty(r.OwnerEmail))
                .Select(r => r.OwnerEmail!.ToLower())
                .Union(issues.Where(i => i.OwnerUser != null && i.OwnerUser.Email != null).Select(i => i.OwnerUser!.Email!.ToLower()))
                .Union(actions.Where(a => a.AssignedToUser != null && a.AssignedToUser.Email != null).Select(a => a.AssignedToUser!.Email!.ToLower()))
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            // Get decisions owners
            var decisionOwnersQuery = _context.Decisions
                .Where(d => !d.IsDeleted && d.OwnerUser != null && d.OwnerUser.Email != null);
            
            // Apply leadership filter if needed
            if (userLeadershipBusinessAreas.Any())
            {
                decisionOwnersQuery = decisionOwnersQuery.Where(d => d.BusinessArea != null && userLeadershipBusinessAreas.Contains(d.BusinessArea));
            }
            
            var decisionOwners = await decisionOwnersQuery
                .Select(d => d.OwnerUser!.Email!.ToLower())
                .Distinct()
                .ToListAsync();

            ownerEmails = ownerEmails.Union(decisionOwners).Distinct().OrderBy(e => e).ToList();

            var allUsers = await _context.Users.ToListAsync();
            var userByEmail = allUsers.ToDictionary(u => u.Email?.ToLower() ?? "", u => u);

            foreach (var email in ownerEmails)
            {
                var riskCount = risks.Count(r => r.OwnerEmail != null && r.OwnerEmail.ToLower() == email);
                var issueCount = issues.Count(i => i.OwnerUser != null && i.OwnerUser.Email != null && i.OwnerUser.Email.ToLower() == email);
                var actionCount = actions.Count(a => a.AssignedToUser != null && a.AssignedToUser.Email != null && a.AssignedToUser.Email.ToLower() == email);
                
                // Get decision count
                var decisionCountQuery = _context.Decisions
                    .Where(d => !d.IsDeleted && d.OwnerUser != null && d.OwnerUser.Email != null && d.OwnerUser.Email.ToLower() == email);
                
                // Apply leadership filter if needed
                if (userLeadershipBusinessAreas.Any())
                {
                    decisionCountQuery = decisionCountQuery.Where(d => d.BusinessArea != null && userLeadershipBusinessAreas.Contains(d.BusinessArea));
                }
                
                var decisionCount = await decisionCountQuery.CountAsync();

                var userName = userByEmail.ContainsKey(email) ? userByEmail[email].Name : email;

                groupedData.OwnerGroups.Add(new OwnerGroup
                {
                    OwnerEmail = email,
                    OwnerName = userName,
                    RiskCount = riskCount,
                    IssueCount = issueCount,
                    ActionCount = actionCount,
                    DecisionCount = decisionCount
                });
            }
        }
        else if (groupBy == "project")
        {
            // Get all FipsIds
            var fipsIds = risks.Where(r => !string.IsNullOrEmpty(r.FipsId))
                .Select(r => r.FipsId!)
                .Union(issues.Where(i => !string.IsNullOrEmpty(i.FipsId)).Select(i => i.FipsId!))
                .Union(actions.Where(a => !string.IsNullOrEmpty(a.FipsId)).Select(a => a.FipsId!))
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            // Get decisions FipsIds
            var decisionFipsIdsQuery = _context.Decisions
                .Where(d => !d.IsDeleted && !string.IsNullOrEmpty(d.FipsId));
            
            // Apply leadership filter if needed
            if (userLeadershipBusinessAreas.Any())
            {
                decisionFipsIdsQuery = decisionFipsIdsQuery.Where(d => d.BusinessArea != null && userLeadershipBusinessAreas.Contains(d.BusinessArea));
            }
            
            var decisionFipsIds = await decisionFipsIdsQuery
                .Select(d => d.FipsId!)
                .Distinct()
                .ToListAsync();

            fipsIds = fipsIds.Union(decisionFipsIds).Distinct().OrderBy(f => f).ToList();

            var products = await _productsApiService.GetProductsAsync();

            foreach (var fipsId in fipsIds)
            {
                var riskCount = risks.Count(r => r.FipsId == fipsId);
                var issueCount = issues.Count(i => i.FipsId == fipsId);
                var actionCount = actions.Count(a => a.FipsId == fipsId);
                
                // Get decision count
                var decisionCountQuery = _context.Decisions
                    .Where(d => !d.IsDeleted && d.FipsId == fipsId);
                
                // Apply leadership filter if needed
                if (userLeadershipBusinessAreas.Any())
                {
                    decisionCountQuery = decisionCountQuery.Where(d => d.BusinessArea != null && userLeadershipBusinessAreas.Contains(d.BusinessArea));
                }
                
                var decisionCount = await decisionCountQuery.CountAsync();

                var product = products.FirstOrDefault(p => p.FipsId == fipsId);
                var projectTitle = product?.Title ?? fipsId;

                groupedData.ProjectGroups.Add(new ProjectGroup
                {
                    FipsId = fipsId,
                    ProjectTitle = projectTitle,
                    RiskCount = riskCount,
                    IssueCount = issueCount,
                    ActionCount = actionCount,
                    DecisionCount = decisionCount
                });
            }
        }

        return groupedData;
    }

    private IQueryable<Risk> ApplyRiskSorting(IQueryable<Risk> query, string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return query.OrderByDescending(r => r.RiskScore);
        }

        return sortBy switch
        {
            "title" => sortOrder == "desc" ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
            "riskScore" => sortOrder == "desc" ? query.OrderByDescending(r => r.RiskScore) : query.OrderBy(r => r.RiskScore),
            "status" => sortOrder == "desc" ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "businessArea" => sortOrder == "desc" ? query.OrderByDescending(r => r.BusinessArea) : query.OrderBy(r => r.BusinessArea),
            _ => query.OrderByDescending(r => r.RiskScore)
        };
    }

    private IQueryable<Issue> ApplyIssueSorting(IQueryable<Issue> query, string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return query.OrderByDescending(i => i.Severity);
        }

        return sortBy switch
        {
            "title" => sortOrder == "desc" ? query.OrderByDescending(i => i.Title) : query.OrderBy(i => i.Title),
            "severity" => sortOrder == "desc" ? query.OrderByDescending(i => i.Severity) : query.OrderBy(i => i.Severity),
            "status" => sortOrder == "desc" ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            "businessArea" => sortOrder == "desc" ? query.OrderByDescending(i => i.BusinessArea) : query.OrderBy(i => i.BusinessArea),
            _ => query.OrderByDescending(i => i.Severity)
        };
    }

    private IQueryable<Models.Action> ApplyActionSorting(IQueryable<Models.Action> query, string? sortBy, string? sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return query.OrderBy(a => a.DueDate ?? DateTime.MaxValue);
        }

        return sortBy switch
        {
            "title" => sortOrder == "desc" ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
            "priority" => sortOrder == "desc" ? query.OrderByDescending(a => a.Priority) : query.OrderBy(a => a.Priority),
            "status" => sortOrder == "desc" ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "businessArea" => sortOrder == "desc" ? query.OrderByDescending(a => a.BusinessArea) : query.OrderBy(a => a.BusinessArea),
            "dueDate" => sortOrder == "desc" ? query.OrderByDescending(a => a.DueDate) : query.OrderBy(a => a.DueDate ?? DateTime.MaxValue),
            _ => query.OrderBy(a => a.DueDate ?? DateTime.MaxValue)
        };
    }
}

public class RaidIndexViewModel
{
    public List<Risk> Risks { get; set; } = new();
    public List<Issue> Issues { get; set; } = new();
    public List<Models.Action> Actions { get; set; } = new();
    public string ViewType { get; set; } = "all";
    public string GroupBy { get; set; } = "none";
    public RaidGroupedViewModel? GroupedData { get; set; }
    public RaidDashboardViewModel? DashboardData { get; set; }
}

public class RaidDashboardViewModel
{
    public int OpenRisksCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public int OpenActionsCount { get; set; }
    public List<Risk> TopCriticalRisks { get; set; } = new();
    public List<Issue> TopCriticalIssues { get; set; } = new();
    public List<BusinessAreaSummary> BusinessAreaSummaries { get; set; } = new();
}

public class BusinessAreaSummary
{
    public string BusinessArea { get; set; } = string.Empty;
    public User? DeputyDirectorSro { get; set; }
    public User? DirectorGeneral { get; set; }
    public int OpenRisksCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public int OpenActionsCount { get; set; }
}

public class RaidGroupedViewModel
{
    public string GroupBy { get; set; } = string.Empty;
    public List<BusinessAreaGroup> BusinessAreaGroups { get; set; } = new();
    public List<OwnerGroup> OwnerGroups { get; set; } = new();
    public List<ProjectGroup> ProjectGroups { get; set; } = new();
}

public class BusinessAreaGroup
{
    public string BusinessArea { get; set; } = string.Empty;
    public int RiskCount { get; set; }
    public int IssueCount { get; set; }
    public int ActionCount { get; set; }
    public int DecisionCount { get; set; }
}

public class OwnerGroup
{
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int RiskCount { get; set; }
    public int IssueCount { get; set; }
    public int ActionCount { get; set; }
    public int DecisionCount { get; set; }
}

public class ProjectGroup
{
    public string FipsId { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public int RiskCount { get; set; }
    public int IssueCount { get; set; }
    public int ActionCount { get; set; }
    public int DecisionCount { get; set; }
}

public class RaidSummaryViewModel
{
    public List<Risk> Risks { get; set; } = new();
    public List<Issue> Issues { get; set; } = new();
    public List<Models.Action> Actions { get; set; } = new();
    public List<Decision> Decisions { get; set; } = new();
    public string GroupBy { get; set; } = string.Empty;
    public string GroupValue { get; set; } = string.Empty;
    public string GroupDisplayName { get; set; } = string.Empty;
    public string ItemType { get; set; } = "all";
}

