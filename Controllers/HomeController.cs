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
            PageDescription = "Your personalised overview of projects, products, issues and milestones",
            IsHomepage = true,
            CurrentUser = currentUser
        };

        try
        {
            // Fetch projects where user is a project contact
            viewModel.MyProjects = await _context.Projects
                .Where(p => !p.IsDeleted && p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()))
                .Include(p => p.Milestones)
                .Include(p => p.Issues)
                .OrderBy(p => p.Title)
                .ToListAsync();

            // Fetch products where user is a product contact
            var allProducts = await _productsApiService.GetProductsAsync();
            viewModel.MyProducts = allProducts
                .Where(p => p.ProductContacts?.Any(pc => 
                    pc.UsersPermissionsUser?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
                .OrderBy(p => p.Title)
                .ToList();

            if (currentUser != null)
            {
                // Fetch all issues across projects and products where user is owner
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

                // Fetch milestones across projects where user is owner
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

            _logger.LogInformation(
                "Dashboard loaded for {Email}: {Projects} projects, {Products} products, {Issues} issues, {Milestones} milestones",
                userEmail, 
                viewModel.TotalProjects,
                viewModel.TotalProducts, 
                viewModel.TotalIssues, 
                viewModel.TotalMilestones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for user {Email}", userEmail);
        }

        // Calculate dashboard metrics for cards
        try
        {
            // Tasks Due: count all overdue or due-this-week items
            var overdueItemsCount = (viewModel.MyMilestones?.Count(m => 
                m.Status != "complete" && 
                m.Status != "cancelled" && 
                m.DueDate < DateTime.UtcNow) ?? 0) +
                (viewModel.MyIssues?.Count(i => 
                    i.Status != "resolved" && 
                    i.Status != "closed" && 
                    i.TargetResolutionDate.HasValue &&
                    i.TargetResolutionDate.Value < DateTime.UtcNow) ?? 0);
            
            var dueThisWeekCount = (viewModel.MyMilestones?.Count(m => 
                m.Status != "complete" && 
                m.Status != "cancelled" && 
                m.DueDate >= DateTime.UtcNow && 
                m.DueDate <= DateTime.UtcNow.AddDays(7)) ?? 0);
            
            var tasksDue = overdueItemsCount + dueThisWeekCount;

            // Service Health: count products with overdue/late operational returns
            var now = DateTime.UtcNow;
            var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
            var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
            
            var serviceHealthIssues = 0;
            foreach (var product in viewModel.MyProducts.Where(p => !string.IsNullOrEmpty(p.FipsId)))
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
                    serviceHealthIssues++;
                }
            }

            // Project Health: count at-risk projects (Red or Amber-Red RAG status)
            var projectHealthIssues = (viewModel.MyProjects?.Count(p => 
                p.RagStatus == "Red" || p.RagStatus == "Amber-Red") ?? 0);

            ViewBag.TasksDue = tasksDue;
            ViewBag.ServiceHealthIssues = serviceHealthIssues;
            ViewBag.ProjectHealthIssues = projectHealthIssues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard metrics");
            ViewBag.TasksDue = 0;
            ViewBag.ServiceHealthIssues = 0;
            ViewBag.ProjectHealthIssues = 0;
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

