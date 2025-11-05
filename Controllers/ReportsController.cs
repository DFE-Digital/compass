using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        CompassDbContext context,
        IProductsApiService productsApiService,
        ILogger<ReportsController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: Reports
    public IActionResult Index()
    {
        return View();
    }

    // GET: Reports/QueryData
    public async Task<IActionResult> QueryData()
    {
        // Get lookup data for filters
        var products = await _productsApiService.GetProductsAsync(null);
        ViewBag.Products = products;
        
        ViewBag.BusinessAreas = await _context.Risks
            .Where(r => !string.IsNullOrEmpty(r.BusinessArea))
            .Select(r => r.BusinessArea)
            .Distinct()
            .OrderBy(ba => ba)
            .ToListAsync();
        
        ViewBag.Objectives = await _context.Objectives
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.Title)
            .ToListAsync();
        
        ViewBag.RiskTypes = await _context.RiskTypes
            .OrderBy(rt => rt.Name)
            .ToListAsync();
        
        ViewBag.RiskTiers = await _context.RiskTiers
            .OrderBy(rt => rt.Name)
            .ToListAsync();
        
        ViewBag.ActionSources = await _context.ActionSources
            .OrderBy(a => a.Name)
            .ToListAsync();
        
        return View();
    }

    // POST: Reports/QueryData
    [HttpPost]
    public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
    {
        try
        {
            var results = new List<Dictionary<string, object>>();
            
            switch (request.EntityType)
            {
                case "risks":
                    results = await ExecuteRiskQuery(request);
                    break;
                case "issues":
                    results = await ExecuteIssueQuery(request);
                    break;
                case "actions":
                    results = await ExecuteActionQuery(request);
                    break;
                case "milestones":
                    results = await ExecuteMilestoneQuery(request);
                    break;
            }
            
            return Json(new { success = true, data = results, count = results.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query");
            return Json(new { success = false, error = ex.Message });
        }
    }

    private async Task<List<Dictionary<string, object>>> ExecuteRiskQuery(QueryRequest request)
    {
        var query = _context.Risks
            .Include(r => r.RiskRiskTypes).ThenInclude(rrt => rrt.RiskType)
            .Include(r => r.RiskTier)
            .Include(r => r.Objective)
            .Where(r => !r.IsDeleted)
            .AsQueryable();
        
        // Apply filters
        if (request.Filters != null)
        {
            foreach (var filter in request.Filters)
            {
                query = filter.Field switch
                {
                    "status" => query.Where(r => r.Status == filter.Value),
                    "businessArea" => query.Where(r => r.BusinessArea == filter.Value),
                    "fipsId" => query.Where(r => r.FipsId == filter.Value),
                    "riskScoreMin" => query.Where(r => r.RiskScore >= int.Parse(filter.Value)),
                    "riskScoreMax" => query.Where(r => r.RiskScore <= int.Parse(filter.Value)),
                    "riskTierId" => query.Where(r => r.RiskTierId == int.Parse(filter.Value)),
                    "objectiveId" => query.Where(r => r.ObjectiveId == int.Parse(filter.Value)),
                    _ => query
                };
            }
        }
        
        // Apply sorting
        query = request.SortBy switch
        {
            "title" => request.SortDirection == "desc" ? query.OrderByDescending(r => r.Title) : query.OrderBy(r => r.Title),
            "riskScore" => request.SortDirection == "desc" ? query.OrderByDescending(r => r.RiskScore) : query.OrderBy(r => r.RiskScore),
            "status" => request.SortDirection == "desc" ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "createdAt" => request.SortDirection == "desc" ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.RiskScore)
        };
        
        var risks = await query.ToListAsync();
        
        return risks.Select(r => BuildRiskResult(r, request.Fields)).ToList();
    }

    private async Task<List<Dictionary<string, object>>> ExecuteIssueQuery(QueryRequest request)
    {
        var query = _context.Issues
            .Include(i => i.Objective)
            .Include(i => i.OwnerUser)
            .Where(i => !i.IsDeleted)
            .AsQueryable();
        
        // Apply filters
        if (request.Filters != null)
        {
            foreach (var filter in request.Filters)
            {
                query = filter.Field switch
                {
                    "status" => query.Where(i => i.Status == filter.Value),
                    "severity" => query.Where(i => i.Severity == filter.Value),
                    "priority" => query.Where(i => i.Priority == filter.Value),
                    "businessArea" => query.Where(i => i.BusinessArea == filter.Value),
                    "fipsId" => query.Where(i => i.FipsId == filter.Value),
                    "blocked" => query.Where(i => i.BlockedFlag == bool.Parse(filter.Value)),
                    "objectiveId" => query.Where(i => i.ObjectiveId == int.Parse(filter.Value)),
                    _ => query
                };
            }
        }
        
        // Apply sorting
        query = request.SortBy switch
        {
            "title" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.Title) : query.OrderBy(i => i.Title),
            "severity" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.Severity) : query.OrderBy(i => i.Severity),
            "status" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.Status) : query.OrderBy(i => i.Status),
            "detectedDate" => request.SortDirection == "desc" ? query.OrderByDescending(i => i.DetectedDate) : query.OrderBy(i => i.DetectedDate),
            _ => query.OrderByDescending(i => i.DetectedDate)
        };
        
        var issues = await query.ToListAsync();
        
        return issues.Select(i => BuildIssueResult(i, request.Fields)).ToList();
    }

    private async Task<List<Dictionary<string, object>>> ExecuteActionQuery(QueryRequest request)
    {
        var query = _context.Actions
            .Include(a => a.ActionSource)
            .Include(a => a.Objective)
            .Where(a => !a.IsDeleted)
            .AsQueryable();
        
        // Apply filters
        if (request.Filters != null)
        {
            foreach (var filter in request.Filters)
            {
                query = filter.Field switch
                {
                    "status" => query.Where(a => a.Status == filter.Value),
                    "businessArea" => query.Where(a => a.BusinessArea == filter.Value),
                    "fipsId" => query.Where(a => a.FipsId == filter.Value),
                    "actionSourceId" => query.Where(a => a.ActionSourceId == int.Parse(filter.Value)),
                    "assignedToEmail" => query.Where(a => a.AssignedToEmail == filter.Value),
                    "objectiveId" => query.Where(a => a.ObjectiveId == int.Parse(filter.Value)),
                    "overdue" => bool.Parse(filter.Value) ? query.Where(a => a.DueDate.HasValue && a.DueDate < DateTime.Today && a.Status != "done" && a.Status != "cancelled") : query,
                    _ => query
                };
            }
        }
        
        // Apply sorting
        query = request.SortBy switch
        {
            "title" => request.SortDirection == "desc" ? query.OrderByDescending(a => a.Title) : query.OrderBy(a => a.Title),
            "status" => request.SortDirection == "desc" ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "dueDate" => request.SortDirection == "desc" ? query.OrderByDescending(a => a.DueDate) : query.OrderBy(a => a.DueDate),
            "createdAt" => request.SortDirection == "desc" ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            _ => query.OrderBy(a => a.DueDate)
        };
        
        var actions = await query.ToListAsync();
        
        return actions.Select(a => BuildActionResult(a, request.Fields)).ToList();
    }

    private async Task<List<Dictionary<string, object>>> ExecuteMilestoneQuery(QueryRequest request)
    {
        var query = _context.Milestones
            .Include(m => m.OwnerUser)
            .Include(m => m.Objective)
            .Where(m => !m.IsDeleted)
            .AsQueryable();
        
        // Apply filters
        if (request.Filters != null)
        {
            foreach (var filter in request.Filters)
            {
                query = filter.Field switch
                {
                    "status" => query.Where(m => m.Status == filter.Value),
                    "businessArea" => query.Where(m => m.BusinessArea == filter.Value),
                    "fipsId" => query.Where(m => m.FipsId == filter.Value),
                    "objectiveId" => query.Where(m => m.ObjectiveId == int.Parse(filter.Value)),
                    "overdue" => bool.Parse(filter.Value) ? query.Where(m => m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled") : query,
                    _ => query
                };
            }
        }
        
        // Apply sorting
        query = request.SortBy switch
        {
            "name" => request.SortDirection == "desc" ? query.OrderByDescending(m => m.Name) : query.OrderBy(m => m.Name),
            "status" => request.SortDirection == "desc" ? query.OrderByDescending(m => m.Status) : query.OrderBy(m => m.Status),
            "dueDate" => request.SortDirection == "desc" ? query.OrderByDescending(m => m.DueDate) : query.OrderBy(m => m.DueDate),
            "progressPercent" => request.SortDirection == "desc" ? query.OrderByDescending(m => m.ProgressPercent) : query.OrderBy(m => m.ProgressPercent),
            _ => query.OrderBy(m => m.DueDate)
        };
        
        var milestones = await query.ToListAsync();
        
        return milestones.Select(m => BuildMilestoneResult(m, request.Fields)).ToList();
    }

    private Dictionary<string, object> BuildRiskResult(Risk risk, List<string> fields)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = risk.Id,
            ["_detailsUrl"] = $"/Risk/Details/{risk.Id}"
        };
        
        foreach (var field in fields)
        {
            result[field] = field switch
            {
                "title" => risk.Title,
                "riskScore" => risk.RiskScore,
                "impactRating" => risk.ImpactRating,
                "likelihoodRating" => risk.LikelihoodRating,
                "status" => risk.Status,
                "businessArea" => risk.BusinessArea ?? "-",
                "fipsId" => risk.FipsId ?? "-",
                "riskTier" => risk.RiskTier?.Name ?? "-",
                "riskTypes" => risk.RiskRiskTypes != null && risk.RiskRiskTypes.Any() 
                    ? string.Join(", ", risk.RiskRiskTypes.Select(rrt => rrt.RiskType?.Name)) 
                    : "-",
                "objective" => risk.Objective?.Title ?? "-",
                "proximityDate" => risk.ProximityDate?.ToString("dd/MM/yyyy") ?? "-",
                "createdAt" => risk.CreatedAt.ToString("dd/MM/yyyy"),
                _ => (object?)null!
            };
        }
        
        return result;
    }

    private Dictionary<string, object> BuildIssueResult(Issue issue, List<string> fields)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = issue.Id,
            ["_detailsUrl"] = $"/Issue/Details/{issue.Id}"
        };
        
        foreach (var field in fields)
        {
            result[field] = field switch
            {
                "title" => issue.Title,
                "severity" => issue.Severity ?? "-",
                "priority" => issue.Priority ?? "-",
                "status" => issue.Status,
                "businessArea" => issue.BusinessArea ?? "-",
                "fipsId" => issue.FipsId ?? "-",
                "blocked" => issue.BlockedFlag ? "Yes" : "No",
                "objective" => issue.Objective?.Title ?? "-",
                "owner" => issue.OwnerUser?.Email ?? "-",
                "detectedDate" => issue.DetectedDate.ToString("dd/MM/yyyy"),
                "targetResolutionDate" => issue.TargetResolutionDate?.ToString("dd/MM/yyyy") ?? "-",
                "createdAt" => issue.CreatedAt.ToString("dd/MM/yyyy"),
                _ => (object?)null!
            };
        }
        
        return result;
    }

    private Dictionary<string, object> BuildActionResult(Models.Action action, List<string> fields)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = action.Id,
            ["_detailsUrl"] = $"/Action/Details/{action.Id}"
        };
        
        foreach (var field in fields)
        {
            result[field] = field switch
            {
                "title" => action.Title,
                "status" => action.Status,
                "businessArea" => action.BusinessArea ?? "-",
                "fipsId" => action.FipsId ?? "-",
                "actionSource" => action.ActionSource?.Name ?? "-",
                "assignedTo" => action.AssignedToEmail ?? "-",
                "objective" => action.Objective?.Title ?? "-",
                "priority" => action.Priority ?? "-",
                "dueDate" => action.DueDate?.ToString("dd/MM/yyyy") ?? "-",
                "completedDate" => action.CompletedDate?.ToString("dd/MM/yyyy") ?? "-",
                "createdAt" => action.CreatedAt.ToString("dd/MM/yyyy"),
                _ => (object?)null!
            };
        }
        
        return result;
    }

    private Dictionary<string, object> BuildMilestoneResult(Milestone milestone, List<string> fields)
    {
        var result = new Dictionary<string, object>
        {
            ["id"] = milestone.Id,
            ["_detailsUrl"] = $"/Milestone/Details/{milestone.Id}"
        };
        
        foreach (var field in fields)
        {
            result[field] = field switch
            {
                "name" => milestone.Name,
                "status" => milestone.Status,
                "businessArea" => milestone.BusinessArea ?? "-",
                "fipsId" => milestone.FipsId ?? "-",
                "owner" => milestone.OwnerUser?.Email ?? milestone.OwnerEmail ?? "-",
                "objective" => milestone.Objective?.Title ?? "-",
                "dueDate" => milestone.DueDate.ToString("dd/MM/yyyy"),
                "progressPercent" => milestone.ProgressPercent?.ToString() ?? "0",
                "createdAt" => milestone.CreatedAt.ToString("dd/MM/yyyy"),
                _ => (object?)null!
            };
        }
        
        return result;
    }

    public class QueryRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public List<string> Fields { get; set; } = new();
        public List<QueryFilter> Filters { get; set; } = new();
        public string SortBy { get; set; } = string.Empty;
        public string SortDirection { get; set; } = "asc";
    }

    public class QueryFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    // DISABLED: Reports/RisksAndIssues view removed
    /*
    public async Task<IActionResult> RisksAndIssues()
    {
        try
        {
            // Fetch all products
            var products = await _productsApiService.GetProductsAsync(null);
            
            // Get RAID counts per product
            var productReports = new List<ProductRaidReport>();
            
            foreach (var product in products.OrderBy(p => p.Title))
            {
                if (string.IsNullOrEmpty(product.FipsId)) continue;
                
                var risks = await _context.Risks
                    .Where(r => r.FipsId == product.FipsId && !r.IsDeleted)
                    .ToListAsync();
                
                var issues = await _context.Issues
                    .Where(i => i.FipsId == product.FipsId && !i.IsDeleted)
                    .ToListAsync();
                
                var actions = await _context.Actions
                    .Where(a => a.FipsId == product.FipsId && !a.IsDeleted)
                    .ToListAsync();
                
                var milestones = await _context.Milestones
                    .Where(m => m.FipsId == product.FipsId && !m.IsDeleted)
                    .ToListAsync();
                
                // Calculate risk statistics
                var openRisks = risks.Count(r => r.Status == "open" || r.Status == "treating");
                var highRisks = risks.Count(r => r.RiskScore >= 15);
                var mediumRisks = risks.Count(r => r.RiskScore >= 10 && r.RiskScore < 15);
                
                // Calculate issue statistics
                var openIssues = issues.Count(i => i.Status == "open" || i.Status == "in_progress");
                var criticalIssues = issues.Count(i => i.Severity == "critical");
                var blockedIssues = issues.Count(i => i.BlockedFlag);
                
                // Calculate action statistics
                var overdueActions = actions.Count(a => 
                    a.DueDate.HasValue && 
                    a.DueDate < DateTime.UtcNow && 
                    a.Status != "done" && 
                    a.Status != "cancelled");
                
                // Calculate milestone statistics
                var overdueMilestones = milestones.Count(m => 
                    m.DueDate < DateTime.UtcNow && 
                    m.Status != "complete" && 
                    m.Status != "cancelled");
                
                productReports.Add(new ProductRaidReport
                {
                    FipsId = product.FipsId,
                    ProductTitle = product.Title,
                    TotalRisks = risks.Count,
                    OpenRisks = openRisks,
                    HighRisks = highRisks,
                    MediumRisks = mediumRisks,
                    TotalIssues = issues.Count,
                    OpenIssues = openIssues,
                    CriticalIssues = criticalIssues,
                    BlockedIssues = blockedIssues,
                    TotalActions = actions.Count,
                    OverdueActions = overdueActions,
                    TotalMilestones = milestones.Count,
                    OverdueMilestones = overdueMilestones,
                    HealthScore = CalculateHealthScore(openRisks, highRisks, openIssues, criticalIssues, blockedIssues, overdueActions, overdueMilestones)
                });
            }
            
            // Sort by health score (worst first)
            productReports = productReports.OrderBy(p => p.HealthScore).ToList();
            
            return View(productReports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating risks and issues report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new List<ProductRaidReport>());
        }
    }
    */

    // DISABLED: Reports/Analysis view removed
    /*
    public async Task<IActionResult> Analysis()
    {
        try
        {
            // Fetch all products
            var products = await _productsApiService.GetProductsAsync(null);
            
            var analysisReports = new List<ProductAnalysisReport>();
            
            foreach (var product in products)
            {
                if (string.IsNullOrEmpty(product.FipsId)) continue;
                
                // Get RAID data
                var risks = await _context.Risks
                    .Where(r => r.FipsId == product.FipsId && !r.IsDeleted)
                    .ToListAsync();
                
                var issues = await _context.Issues
                    .Where(i => i.FipsId == product.FipsId && !i.IsDeleted)
                    .ToListAsync();
                
                var actions = await _context.Actions
                    .Where(a => a.FipsId == product.FipsId && !a.IsDeleted)
                    .ToListAsync();
                
                var milestones = await _context.Milestones
                    .Where(m => m.FipsId == product.FipsId && !m.IsDeleted)
                    .ToListAsync();
                
                // Get latest product return
                var latestReturn = await _context.ProductReturns
                    .Where(pr => pr.FipsId == product.FipsId)
                    .OrderByDescending(pr => pr.Year)
                    .ThenByDescending(pr => pr.Month)
                    .FirstOrDefaultAsync();
                
                // Get user satisfaction metric if available
                decimal? userSatisfaction = null;
                if (latestReturn != null)
                {
                    var satisfactionMetric = await _context.PerformanceMetrics
                        .Where(pm => pm.Identifier.ToLower().Contains("satisfaction") || 
                                   pm.Identifier.ToLower().Contains("sat"))
                        .FirstOrDefaultAsync();
                    
                    if (satisfactionMetric != null)
                    {
                        var metricValue = await _context.ProductMetricValues
                            .Where(mv => mv.ProductReturnId == latestReturn.Id && 
                                       mv.PerformanceMetricId == satisfactionMetric.Id)
                            .FirstOrDefaultAsync();
                        
                        if (metricValue?.Value != null && decimal.TryParse(metricValue.Value, out var parsedValue))
                        {
                            userSatisfaction = parsedValue;
                        }
                    }
                }
                
                // Calculate problem indicators
                var problemScore = CalculateProblemScore(
                    risks, issues, actions, milestones, userSatisfaction);
                
                var report = new ProductAnalysisReport
                {
                    FipsId = product.FipsId,
                    ProductTitle = product.Title,
                    UserSatisfaction = userSatisfaction,
                    TotalRisks = risks.Count,
                    HighRisks = risks.Count(r => r.RiskScore >= 15),
                    TotalIssues = issues.Count,
                    CriticalIssues = issues.Count(i => i.Severity == "critical"),
                    BlockedIssues = issues.Count(i => i.BlockedFlag),
                    OverdueActions = actions.Count(a => 
                        a.DueDate.HasValue && 
                        a.DueDate < DateTime.UtcNow && 
                        a.Status != "done" && 
                        a.Status != "cancelled"),
                    DelayedMilestones = milestones.Count(m => 
                        m.Status == "delayed" || 
                        (m.DueDate < DateTime.UtcNow && m.Status != "complete" && m.Status != "cancelled")),
                    AverageRiskScore = risks.Any() ? risks.Average(r => r.RiskScore) : 0,
                    OldestOpenIssue = issues
                        .Where(i => i.Status == "open" || i.Status == "in_progress")
                        .OrderBy(i => i.DetectedDate)
                        .FirstOrDefault()?.DetectedDate,
                    ProblemScore = problemScore,
                    NeedsAttention = problemScore > 50,
                    HasMetricsData = latestReturn != null,
                    LastReportDate = latestReturn?.SubmittedDate
                };
                
                analysisReports.Add(report);
            }
            
            // Sort by problem score (worst first)
            analysisReports = analysisReports.OrderByDescending(p => p.ProblemScore).ToList();
            
            // Calculate statistics
            ViewBag.TotalProducts = analysisReports.Count;
            ViewBag.ProductsNeedingAttention = analysisReports.Count(p => p.NeedsAttention);
            ViewBag.TotalHighRisks = analysisReports.Sum(p => p.HighRisks);
            ViewBag.TotalCriticalIssues = analysisReports.Sum(p => p.CriticalIssues);
            ViewBag.TotalBlockedIssues = analysisReports.Sum(p => p.BlockedIssues);
            
            return View(analysisReports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analysis report");
            TempData["ErrorMessage"] = "An error occurred while generating the analysis. Please try again.";
            return View(new List<ProductAnalysisReport>());
        }
    }
    */

    private int CalculateHealthScore(int openRisks, int highRisks, int openIssues, 
        int criticalIssues, int blockedIssues, int overdueActions, int overdueMilestones)
    {
        // Higher score = better health (0-100)
        int score = 100;
        
        // Deduct points for problems
        score -= (openRisks * 2);
        score -= (highRisks * 5);
        score -= (openIssues * 3);
        score -= (criticalIssues * 10);
        score -= (blockedIssues * 8);
        score -= (overdueActions * 2);
        score -= (overdueMilestones * 5);
        
        return Math.Max(0, score);
    }

    // GET: Reports/Risks
    public async Task<IActionResult> Risks(string groupBy = "impact-likelihood")
    {
        try
        {
            var products = await _productsApiService.GetProductsAsync(null);
            ViewBag.Products = products;
            
            // Get all business areas (distinct from all entities)
            var businessAreas = await _context.Risks
                .Where(r => !r.IsDeleted && !string.IsNullOrEmpty(r.BusinessArea))
                .Select(r => r.BusinessArea)
                .Distinct()
                .OrderBy(ba => ba)
                .ToListAsync();
            ViewBag.BusinessAreas = businessAreas;
            
            // Get all risk types
            var riskTypes = await _context.RiskTypes
                .OrderBy(rt => rt.Name)
                .ToListAsync();
            ViewBag.RiskTypes = riskTypes;
            
            // Get all risk tiers
            var riskTiers = await _context.RiskTiers
                .OrderBy(rt => rt.Name)
                .ToListAsync();
            ViewBag.RiskTiers = riskTiers;
            
            var risks = await _context.Risks
                .Include(r => r.RiskRiskTypes)
                    .ThenInclude(rrt => rrt.RiskType)
                .Include(r => r.RiskTier)
                .Include(r => r.Objective)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.RiskScore)
                .ToListAsync();
            
            ViewBag.GroupBy = groupBy;
            ViewBag.TotalRisks = risks.Count;
            ViewBag.HighRisks = risks.Count(r => r.RiskScore >= 15);
            ViewBag.MediumRisks = risks.Count(r => r.RiskScore >= 10 && r.RiskScore < 15);
            ViewBag.LowRisks = risks.Count(r => r.RiskScore < 10);
            ViewBag.OpenRisks = risks.Count(r => r.Status == "open" || r.Status == "treating");
            ViewBag.ClosedRisks = risks.Count(r => r.Status == "closed");
            
            return View(risks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating risks report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new List<Risk>());
        }
    }

    // GET: Reports/Issues
    public async Task<IActionResult> Issues(string groupBy = "severity")
    {
        try
        {
            var products = await _productsApiService.GetProductsAsync(null);
            ViewBag.Products = products;
            
            var issues = await _context.Issues
                .Include(i => i.Objective)
                .Include(i => i.OwnerUser)
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.Severity == "critical" ? 4 : i.Severity == "high" ? 3 : i.Severity == "medium" ? 2 : 1)
                .ThenBy(i => i.Status)
                .ToListAsync();
            
            ViewBag.GroupBy = groupBy;
            ViewBag.TotalIssues = issues.Count;
            ViewBag.CriticalIssues = issues.Count(i => i.Severity == "critical");
            ViewBag.HighIssues = issues.Count(i => i.Severity == "high");
            ViewBag.MediumIssues = issues.Count(i => i.Severity == "medium");
            ViewBag.LowIssues = issues.Count(i => i.Severity == "low");
            ViewBag.OpenIssues = issues.Count(i => i.Status == "open" || i.Status == "in_progress");
            ViewBag.ResolvedIssues = issues.Count(i => i.Status == "resolved" || i.Status == "closed");
            ViewBag.BlockedIssues = issues.Count(i => i.BlockedFlag);
            
            return View(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating issues report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new List<Issue>());
        }
    }

    // GET: Reports/Actions
    public async Task<IActionResult> Actions(string groupBy = "status")
    {
        try
        {
            var products = await _productsApiService.GetProductsAsync(null);
            ViewBag.Products = products;
            
            var actions = await _context.Actions
                .Include(a => a.ActionSource)
                .Include(a => a.Objective)
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Status == "blocked" ? 0 : a.Status == "in_progress" ? 1 : a.Status == "not_started" ? 2 : 3)
                .ThenBy(a => a.DueDate)
                .ToListAsync();
            
            ViewBag.GroupBy = groupBy;
            ViewBag.TotalActions = actions.Count;
            ViewBag.DoneActions = actions.Count(a => a.Status == "done");
            ViewBag.InProgressActions = actions.Count(a => a.Status == "in_progress");
            ViewBag.NotStartedActions = actions.Count(a => a.Status == "not_started");
            ViewBag.BlockedActions = actions.Count(a => a.Status == "blocked");
            ViewBag.OverdueActions = actions.Count(a => 
                a.DueDate.HasValue && 
                a.DueDate < DateTime.Today && 
                a.Status != "done" && 
                a.Status != "cancelled");
            
            return View(actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating actions report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new List<Models.Action>());
        }
    }

    // GET: Reports/Milestones
    public async Task<IActionResult> Milestones(string groupBy = "status")
    {
        try
        {
            var products = await _productsApiService.GetProductsAsync(null);
            ViewBag.Products = products;
            
            var milestones = await _context.Milestones
                .Include(m => m.OwnerUser)
                .Include(m => m.Objective)
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Status == "delayed" ? 0 : m.Status == "at_risk" ? 1 : m.Status == "on_track" ? 2 : 3)
                .ThenBy(m => m.DueDate)
                .ToListAsync();
            
            ViewBag.GroupBy = groupBy;
            ViewBag.TotalMilestones = milestones.Count;
            ViewBag.CompleteMillestones = milestones.Count(m => m.Status == "complete");
            ViewBag.OnTrackMilestones = milestones.Count(m => m.Status == "on_track");
            ViewBag.AtRiskMilestones = milestones.Count(m => m.Status == "at_risk");
            ViewBag.DelayedMilestones = milestones.Count(m => m.Status == "delayed");
            ViewBag.OverdueMilestones = milestones.Count(m => 
                m.DueDate < DateTime.Today && 
                m.Status != "complete" && 
                m.Status != "cancelled");
            
            return View(milestones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating milestones report");
            TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
            return View(new List<Milestone>());
        }
    }

    // GET: Reports/ProductDashboard/FIPS-001
    // Redirect to new Products controller for backwards compatibility
    public IActionResult ProductDashboard(string id)
    {
        return RedirectToAction("Dashboard", "Products", new { id = id });
    }

    private int CalculateProblemScore(List<Risk> risks, List<Issue> issues, 
        List<Models.Action> actions, List<Milestone> milestones, decimal? userSatisfaction)
    {
        // Higher score = more problems (0-100)
        int score = 0;
        
        // Risk factors
        score += risks.Count(r => r.RiskScore >= 15) * 10; // High risks
        score += risks.Count(r => r.RiskScore >= 10 && r.RiskScore < 15) * 5; // Medium risks
        score += risks.Count(r => r.Status == "open") * 3; // Open risks
        
        // Issue factors
        score += issues.Count(i => i.Severity == "critical") * 15; // Critical issues
        score += issues.Count(i => i.Severity == "high") * 8; // High severity
        score += issues.Count(i => i.BlockedFlag) * 10; // Blocked issues
        score += issues.Count(i => i.Status == "open" || i.Status == "in_progress") * 2;
        
        // Action factors
        var overdueActions = actions.Count(a => 
            a.DueDate.HasValue && 
            a.DueDate < DateTime.UtcNow && 
            a.Status != "done" && 
            a.Status != "cancelled");
        score += overdueActions * 3;
        
        // Milestone factors
        var delayedMilestones = milestones.Count(m => m.Status == "delayed");
        score += delayedMilestones * 8;
        
        // User satisfaction factor (if low)
        if (userSatisfaction.HasValue && userSatisfaction < 70)
        {
            score += (int)((70 - userSatisfaction.Value) / 2);
        }
        
        return Math.Min(100, score);
    }
}

