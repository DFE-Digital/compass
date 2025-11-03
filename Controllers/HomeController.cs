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

    public async Task<IActionResult> Index()
    {
        try
        {
            var userEmail = User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Index: No user email found");
                TempData["ErrorMessage"] = "Unable to identify the current user.";
                return View();
            }

            // Get or create the current user
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            if (currentUser == null)
            {
                _logger.LogWarning("Index: User not found in database for email: {Email}", userEmail);
                TempData["ErrorMessage"] = "User account not found. Please contact an administrator.";
                return View();
            }

            // Get projects where user is a project contact
            var myProjects = await _context.Projects
                .Where(p => !p.IsDeleted && p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()))
                .Include(p => p.ProjectContacts)
                .Include(p => p.Milestones)
                .Include(p => p.Issues)
                .Include(p => p.Risks)
                .Include(p => p.ProjectProducts)
                .Include(p => p.Successes)
                .OrderBy(p => p.Title)
                .ToListAsync();

            // Get products where user is a product contact
            var allProducts = await _productsApiService.GetProductsAsync();
            var myProducts = allProducts
                .Where(p => p.ProductContacts?.Any(pc => 
                    pc.UsersPermissionsUser?.Email?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true) == true)
                .OrderBy(p => p.Title)
                .ToList();

            // Calculate summary statistics
            var allActiveMilestones = myProjects.SelectMany(p => p.Milestones.Where(m => !m.IsDeleted)).ToList();
            var milestonesDueThisWeek = allActiveMilestones.Where(m => m.DueDate >= DateTime.Today && m.DueDate <= DateTime.Today.AddDays(7)).ToList();
            var overdueMilestones = allActiveMilestones.Where(m => m.DueDate < DateTime.Today && m.Status != "complete").ToList();

            var allActiveIssues = myProjects.SelectMany(p => p.Issues.Where(i => !i.IsDeleted)).ToList();
            var highPriorityIssues = allActiveIssues.Where(i => i.Severity == "high" || i.Severity == "critical").ToList();
            var openIssues = allActiveIssues.Where(i => i.Status != "resolved" && i.Status != "closed").ToList();

            // Get RAG status breakdown
            var redProjects = myProjects.Where(p => p.RagStatus == "Red").ToList();
            var amberRedProjects = myProjects.Where(p => p.RagStatus == "Amber-Red").ToList();
            var amberProjects = myProjects.Where(p => p.RagStatus == "Amber" || p.RagStatus == "Amber-Green").ToList();
            var greenProjects = myProjects.Where(p => p.RagStatus == "Green").ToList();

            // Get recent successes (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var recentSuccesses = myProjects.SelectMany(p => p.Successes.Where(s => s.RecordedAt >= thirtyDaysAgo))
                .OrderByDescending(s => s.RecordedAt)
                .Take(10)
                .ToList();

            // Calculate Your Tasks
            // First, combine all at-risk projects (Red and Amber-Red)
            var atRiskProjects = redProjects.Concat(amberRedProjects).ToList();
            
            // Task 1: Projects needing Path to Green documented (at risk projects without path to green)
            var projectsNeedingPathToGreen = atRiskProjects.Where(p => string.IsNullOrWhiteSpace(p.PathToGreen)).ToList();
            
            // Task 2: Expired open milestones (milestones due in the past that are not complete or cancelled)
            var expiredOpenMilestones = allActiveMilestones
                .Where(m => m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled")
                .ToList();
            
            // Task 3: Products with operational returns that are Due or Late
            var now = DateTime.UtcNow;
            var currentYear = now.Month == 1 ? now.Year - 1 : now.Year;
            var currentMonth = now.Month == 1 ? 12 : now.Month - 1;
            
            var productsNeedingReturns = new List<(Compass.Models.ProductDto Product, ReturnStatus Status, DateTime DueDate)>();
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
            
            // Task 4: High priority issues (already calculated above)
            
            // Calculate dashboard metrics
            var tasksDue = projectsNeedingPathToGreen.Count + expiredOpenMilestones.Count + productsNeedingReturns.Count + highPriorityIssues.Count;
            var serviceHealthIssues = productsNeedingReturns.Count;
            var projectHealthIssues = atRiskProjects.Count;
            
            // Pass all data to ViewBag
            ViewBag.CurrentUser = currentUser;
            ViewBag.MyProjects = myProjects;
            ViewBag.MyProducts = myProducts;
            ViewBag.AllActiveMilestones = allActiveMilestones;
            ViewBag.MilestonesDueThisWeek = milestonesDueThisWeek;
            ViewBag.OverdueMilestones = overdueMilestones;
            ViewBag.AllActiveIssues = allActiveIssues;
            ViewBag.HighPriorityIssues = highPriorityIssues;
            ViewBag.OpenIssues = openIssues;
            ViewBag.RedProjects = redProjects;
            ViewBag.AmberRedProjects = amberRedProjects;
            ViewBag.AmberProjects = amberProjects;
            ViewBag.GreenProjects = greenProjects;
            ViewBag.RecentSuccesses = recentSuccesses;
            
            // Task data
            ViewBag.AtRiskProjects = atRiskProjects;
            ViewBag.ProjectsNeedingPathToGreen = projectsNeedingPathToGreen;
            ViewBag.ExpiredOpenMilestones = expiredOpenMilestones;
            ViewBag.ProductsNeedingReturns = productsNeedingReturns;
            
            // Dashboard metrics
            ViewBag.TasksDue = tasksDue;
            ViewBag.ServiceHealthIssues = serviceHealthIssues;
            ViewBag.ProjectHealthIssues = projectHealthIssues;

            _logger.LogInformation(
                "Index loaded for {Email}: {Projects} projects, {Products} products, {Milestones} milestones, {Issues} issues",
                userEmail, myProjects.Count, myProducts.Count, allActiveMilestones.Count, allActiveIssues.Count);

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Index");
            TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
            return View();
        }
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

