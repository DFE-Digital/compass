using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Models;
using Compass.Services;
using Compass.Data;

namespace Compass.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICmsApiService _cmsApiService;
    private readonly IProductsApiService _productsApiService;
    private readonly CompassDbContext _context;

    public HomeController(
        ILogger<HomeController> logger, 
        ICmsApiService cmsApiService,
        IProductsApiService productsApiService,
        CompassDbContext context)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
        _productsApiService = productsApiService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? view = "overview",
        string? statusFilter = null,
        string? priorityFilter = null,
        string? dateFilter = "all")
    {
        var userEmail = User.Identity?.Name;
        
        if (string.IsNullOrEmpty(userEmail))
        {
            return RedirectToAction("Error");
        }

        // Get or create the current user
        var currentUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

        var viewModel = new DashboardViewModel
        {
            PageTitle = "Dashboard",
            PageDescription = "Your personalised overview of products, risks, issues, actions and milestones",
            IsHomepage = true,
            CurrentUser = currentUser
        };

        try
        {
            // Fetch products where user is a product contact
            var allProducts = await _productsApiService.GetProductsAsync();
            viewModel.MyProducts = allProducts
                .Where(p => p.ProductContacts?.Any(pc => 
                    pc.UsersPermissionsUser?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
                .OrderBy(p => p.Title)
                .ToList();

            if (currentUser != null)
            {
                // Fetch issues owned by the user
                var issuesQuery = _context.Issues
                    .Include(i => i.Objective)
                    .Where(i => !i.IsDeleted && i.OwnerUserId == currentUser.Id);

                // Apply filters for issues view
                if (view == "issues")
                {
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        issuesQuery = issuesQuery.Where(i => i.Status == statusFilter);
                    }
                    if (!string.IsNullOrEmpty(priorityFilter))
                    {
                        issuesQuery = issuesQuery.Where(i => i.Severity == priorityFilter);
                    }
                    issuesQuery = ApplyDateFilter(issuesQuery, dateFilter, i => i.TargetResolutionDate);
                }

                viewModel.MyIssues = await issuesQuery
                    .OrderByDescending(i => i.CreatedAt)
                    .Take(100)
                    .ToListAsync();

                // Fetch actions assigned to the user
                var actionsQuery = _context.Actions
                    .Include(a => a.Objective)
                    .Where(a => !a.IsDeleted && a.AssignedToEmail != null && a.AssignedToEmail.ToLower() == currentUser.Email.ToLower());

                // Apply filters for actions view
                if (view == "actions")
                {
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        actionsQuery = actionsQuery.Where(a => a.Status == statusFilter);
                    }
                    if (!string.IsNullOrEmpty(priorityFilter))
                    {
                        actionsQuery = actionsQuery.Where(a => a.Priority == priorityFilter);
                    }
                    actionsQuery = ApplyDateFilter(actionsQuery, dateFilter, a => a.DueDate);
                }

                viewModel.MyActions = await actionsQuery
                    .OrderBy(a => a.DueDate)
                    .Take(100)
                    .ToListAsync();

                // Fetch milestones owned by the user
                var milestonesQuery = _context.Milestones
                    .Include(m => m.Objective)
                    .Where(m => !m.IsDeleted && m.OwnerUserId == currentUser.Id);

                // Apply filters for milestones view
                if (view == "milestones")
                {
                    if (!string.IsNullOrEmpty(statusFilter))
                    {
                        milestonesQuery = milestonesQuery.Where(m => m.Status == statusFilter);
                    }
                    milestonesQuery = ApplyMilestoneDateFilter(milestonesQuery, dateFilter);
                }

                viewModel.MyMilestones = await milestonesQuery
                    .OrderBy(m => m.DueDate)
                    .Take(100)
                    .ToListAsync();
            }

            // Fetch risks where user is the owner (by email)
            var risksQuery = _context.Risks
                .Include(r => r.Objective)
                .Where(r => !r.IsDeleted && 
                    r.OwnerEmail != null && 
                    r.OwnerEmail.ToLower() == userEmail.ToLower());

            // Apply filters for risks view
            if (view == "risks")
            {
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    risksQuery = risksQuery.Where(r => r.Status == statusFilter);
                }
                if (!string.IsNullOrEmpty(priorityFilter))
                {
                    risksQuery = risksQuery.Where(r => 
                        (priorityFilter == "critical" && r.RiskScore >= 15) ||
                        (priorityFilter == "high" && r.RiskScore >= 10 && r.RiskScore < 15) ||
                        (priorityFilter == "medium" && r.RiskScore >= 5 && r.RiskScore < 10) ||
                        (priorityFilter == "low" && r.RiskScore < 5));
                }
                risksQuery = ApplyDateFilter(risksQuery, dateFilter, r => r.TargetDate);
            }

            viewModel.MyRisks = await risksQuery
                .OrderByDescending(r => r.RiskScore)
                .Take(100)
                .ToListAsync();

            _logger.LogInformation(
                "Dashboard loaded for {Email}: {Products} products, {Issues} issues, {Risks} risks, {Actions} actions, {Milestones} milestones",
                userEmail, 
                viewModel.TotalProducts, 
                viewModel.TotalIssues, 
                viewModel.TotalRisks, 
                viewModel.TotalActions, 
                viewModel.TotalMilestones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {Email}", userEmail);
        }

        ViewData["ActiveNav"] = "home";
        ViewBag.CurrentView = view;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.PriorityFilter = priorityFilter;
        ViewBag.DateFilter = dateFilter;
        
        return View(viewModel);
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
}

