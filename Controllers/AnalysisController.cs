using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using ClosedXML.Excel;

namespace Compass.Controllers;

[Authorize]
public class AnalysisController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<AnalysisController> _logger;
    private readonly IProductsApiService _productsApiService;
    private readonly IServiceAssessmentApiService _serviceAssessmentApiService;

    public AnalysisController(
        CompassDbContext context,
        ILogger<AnalysisController> logger,
        IProductsApiService productsApiService,
        IServiceAssessmentApiService serviceAssessmentApiService)
    {
        _context = context;
        _logger = logger;
        _productsApiService = productsApiService;
        _serviceAssessmentApiService = serviceAssessmentApiService;
    }

    // GET: Analysis/Index
    public IActionResult Index()
    {
        return View();
    }

    // GET: Analysis/Delivery
    public async Task<IActionResult> Delivery()
    {
        try
        {
            // Get all active projects with related data
            var projects = await _context.Projects
                .Include(p => p.Milestones.Where(m => !m.IsDeleted))
                .Include(p => p.Risks.Where(r => !r.IsDeleted))
                .Include(p => p.Issues.Where(i => !i.IsDeleted))
                .Include(p => p.ProjectProducts)
                .Where(p => !p.IsDeleted && p.Status == "Active")
                .ToListAsync();

            // Identify risky delivery items
            var riskyProjects = projects
                .Where(p => 
                    p.RagStatus == "Red" || 
                    p.RagStatus == "Amber-Red" ||
                    (p.TargetDeliveryDate.HasValue && p.TargetDeliveryDate < DateTime.Today.AddDays(30) && p.ActualDeliveryDate == null) ||
                    p.Milestones.Any(m => !m.IsDeleted && m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled") ||
                    p.Risks.Any(r => !r.IsDeleted && r.RiskScore >= 12) ||
                    p.Issues.Any(i => !i.IsDeleted && (i.Severity == "high" || i.Severity == "critical")))
                .Select(p => new
                {
                    Project = p,
                    TargetDeliveryDate = p.TargetDeliveryDate,
                    RiskFactors = new List<string>
                    {
                        p.RagStatus == "Red" || p.RagStatus == "Amber-Red" ? $"RAG Status: {p.RagStatus}" : null,
                        p.TargetDeliveryDate.HasValue && p.TargetDeliveryDate < DateTime.Today.AddDays(30) && p.ActualDeliveryDate == null ? "Delivery date within 30 days" : null,
                        p.Milestones.Any(m => !m.IsDeleted && m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled") ? $"{p.Milestones.Count(m => !m.IsDeleted && m.DueDate < DateTime.Today && m.Status != "complete" && m.Status != "cancelled")} overdue milestone(s)" : null,
                        p.Risks.Any(r => !r.IsDeleted && r.RiskScore >= 12) ? $"{p.Risks.Count(r => !r.IsDeleted && r.RiskScore >= 12)} high-risk item(s)" : null,
                        p.Issues.Any(i => !i.IsDeleted && (i.Severity == "high" || i.Severity == "critical")) ? $"{p.Issues.Count(i => !i.IsDeleted && (i.Severity == "high" || i.Severity == "critical"))} high-severity issue(s)" : null
                    }.Where(f => f != null).ToList()
                })
                .OrderByDescending(x => x.RiskFactors.Count)
                .ToList();

            // Get all high-risk items
            var highRisks = await _context.Risks
                .Include(r => r.Project)
                .Include(r => r.OwnerUser)
                .Where(r => !r.IsDeleted && r.RiskScore >= 12)
                .OrderByDescending(r => r.RiskScore)
                .ThenByDescending(r => r.ProximityDate)
                .ToListAsync();

            // Get all critical/high issues
            var criticalIssues = await _context.Issues
                .Include(i => i.Project)
                .Include(i => i.OwnerUser)
                .Where(i => !i.IsDeleted && (i.Severity == "high" || i.Severity == "critical") && i.Status != "resolved" && i.Status != "closed")
                .OrderByDescending(i => i.Severity == "critical" ? 1 : 0)
                .ThenByDescending(i => i.DetectedDate)
                .ToListAsync();

            // Get overdue milestones
            var overdueMilestones = await _context.Milestones
                .Include(m => m.Project)
                .Include(m => m.OwnerUser)
                .Where(m => !m.IsDeleted && 
                    m.DueDate < DateTime.Today && 
                    m.Status != "complete" && 
                    m.Status != "cancelled")
                .OrderBy(m => m.DueDate)
                .ToListAsync();

            // Potential risks (items approaching deadlines or with increasing risk scores)
            var potentialRisks = projects
                .Where(p => 
                    (p.TargetDeliveryDate.HasValue && p.TargetDeliveryDate >= DateTime.Today && p.TargetDeliveryDate <= DateTime.Today.AddDays(60)) ||
                    (p.RagStatus == "Amber" || p.RagStatus == "Amber-Green") ||
                    p.Milestones.Any(m => !m.IsDeleted && m.DueDate >= DateTime.Today && m.DueDate <= DateTime.Today.AddDays(30) && m.Status != "complete"))
                .Select(p => 
                {
                    var approachingMilestones = p.Milestones
                        .Where(m => !m.IsDeleted && m.DueDate >= DateTime.Today && m.DueDate <= DateTime.Today.AddDays(30) && m.Status != "complete")
                        .OrderBy(m => m.DueDate)
                        .ToList();
                    
                    string riskReason;
                    if (p.TargetDeliveryDate.HasValue && p.TargetDeliveryDate >= DateTime.Today && p.TargetDeliveryDate <= DateTime.Today.AddDays(60))
                    {
                        riskReason = "Delivery date within 60 days";
                    }
                    else if (p.RagStatus == "Amber" || p.RagStatus == "Amber-Green")
                    {
                        riskReason = $"RAG Status: {p.RagStatus}";
                    }
                    else if (approachingMilestones.Any())
                    {
                        var milestoneDates = approachingMilestones
                            .Select(m => m.DueDate.ToString("dd MMM yyyy"))
                            .Distinct()
                            .ToList();
                        riskReason = $"Milestones approaching deadline ({string.Join(", ", milestoneDates)})";
                    }
                    else
                    {
                        riskReason = "Milestones approaching deadline";
                    }
                    
                    return new
                    {
                        Project = p,
                        TargetDeliveryDate = p.TargetDeliveryDate,
                        RiskReason = riskReason
                    };
                })
                .ToList();

            ViewBag.RiskyProjects = riskyProjects;
            ViewBag.HighRisks = highRisks;
            ViewBag.CriticalIssues = criticalIssues;
            ViewBag.OverdueMilestones = overdueMilestones;
            ViewBag.PotentialRisks = potentialRisks;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Delivery analysis");
            TempData["ErrorMessage"] = "An error occurred while loading the delivery analysis.";
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: Analysis/Operational
    public async Task<IActionResult> Operational()
    {
        try
        {
            // Get all products
            var allProducts = await _productsApiService.GetProductsAsync();

            // Get standards compliance data
            var standardProducts = await _context.DdtStandardProducts
                .Include(dsp => dsp.DdtStandard)
                .Include(dsp => dsp.StandardProduct)
                .ToListAsync();

            // Identify products with standards compliance issues
            var complianceIssues = new List<object>();
            
            // For each product, check if it has required standards
            // This is a simplified check - you may need to adjust based on your actual standards requirements
            foreach (var product in allProducts)
            {
                var productStandards = standardProducts
                    .Where(sp => sp.StandardProduct.DfeFipsProductId == product.Id.ToString())
                    .ToList();

                // Check for missing compliance or at-risk status
                if (productStandards.Any(sp => sp.ProductType == "Tolerated"))
                {
                    complianceIssues.Add(new
                    {
                        ProductId = product.Id,
                        ProductTitle = product.Title,
                        Issue = "Has tolerated standards (not approved)",
                        Standards = productStandards.Where(sp => sp.ProductType == "Tolerated").Select(sp => sp.DdtStandard.Title).ToList()
                    });
                }
            }

            // Get performance metrics data
            var threeMonthsAgo = DateTime.Today.AddMonths(-3);
            var recentReturns = await _context.ProductReturns
                .Include(pr => pr.MetricValues)
                    .ThenInclude(pmv => pmv.PerformanceMetric)
                .Where(pr => pr.Year > threeMonthsAgo.Year || 
                    (pr.Year == threeMonthsAgo.Year && pr.Month >= threeMonthsAgo.Month))
                .OrderByDescending(pr => pr.Year)
                .ThenByDescending(pr => pr.Month)
                .ToListAsync();

            // Identify performance stats problems
            var performanceIssues = new List<object>();

            // Check for missing or incomplete returns
            var productsWithMissingReturns = allProducts
                .Where(p => !recentReturns.Any(pr => pr.FipsId == p.Id.ToString()))
                .Select(p => new
                {
                    ProductId = p.Id,
                    ProductTitle = p.Title,
                    Issue = "No recent performance returns",
                    ReportingPeriod = (string?)null,
                    IncompleteMetrics = (int?)null,
                    LastReturn = (DateTime?)null,
                    Status = "Missing"
                })
                .ToList();

            // Check for incomplete returns
            var incompleteReturns = recentReturns
                .Where(pr => pr.MetricValues.Any(pmv => !pmv.IsComplete && !pmv.IsNotCaptured))
                .Select(pr => new
                {
                    ProductId = pr.FipsId,
                    ProductTitle = allProducts.FirstOrDefault(p => p.Id.ToString() == pr.FipsId)?.Title ?? "Unknown",
                    Issue = "Incomplete performance return",
                    ReportingPeriod = $"{pr.Year}-{pr.Month:D2}",
                    IncompleteMetrics = pr.MetricValues.Count(pmv => !pmv.IsComplete && !pmv.IsNotCaptured),
                    Status = pr.Status
                })
                .ToList();

            performanceIssues.AddRange(productsWithMissingReturns);
            performanceIssues.AddRange(incompleteReturns);

            // Get products with declining performance trends
            var performanceTrends = recentReturns
                .GroupBy(pr => pr.FipsId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductTitle = allProducts.FirstOrDefault(p => p.Id.ToString() == g.Key)?.Title ?? "Unknown",
                    Returns = g.OrderByDescending(pr => pr.Year).ThenByDescending(pr => pr.Month).ToList(),
                    Trend = "Stable" // Simplified - you could add actual trend calculation
                })
                .Where(x => x.Returns.Count >= 2)
                .ToList();

            ViewBag.ComplianceIssues = complianceIssues;
            ViewBag.PerformanceIssues = performanceIssues;
            ViewBag.PerformanceTrends = performanceTrends;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Operational analysis");
            TempData["ErrorMessage"] = "An error occurred while loading the operational analysis.";
            return RedirectToAction("Index", "Home");
        }
    }

        // GET: Analysis/ServiceStandards
        public async Task<IActionResult> ServiceStandards()
    {
        try
        {
            // Get service assessment data
            var assessmentData = await _serviceAssessmentApiService.GetActionsByStandardAsync();

            if (assessmentData?.Assessments == null || !assessmentData.Assessments.Any())
            {
                ViewBag.ErrorMessage = "Unable to load service assessment data. Please check the API configuration.";
                ViewBag.StandardAnalysis = new List<object>();
                ViewBag.MostIssueProne = new List<object>();
                ViewBag.StandardsWithRepeatedIssues = new List<object>();
                ViewBag.IncreasingTrends = new List<object>();
                return View();
            }

            // Aggregate all actions by standard number across all assessments
            var allActionsByStandard = new Dictionary<int, List<ActionItem>>();
            
            foreach (var assessment in assessmentData.Assessments)
            {
                if (assessment.ActionsByStandard == null) continue;
                
                foreach (var actionsByStandard in assessment.ActionsByStandard)
                {
                    if (actionsByStandard.Actions == null) continue;
                    
                    if (!allActionsByStandard.ContainsKey(actionsByStandard.Standard))
                    {
                        allActionsByStandard[actionsByStandard.Standard] = new List<ActionItem>();
                    }
                    
                    // Add assessment name to each action for context
                    foreach (var action in actionsByStandard.Actions)
                    {
                        allActionsByStandard[actionsByStandard.Standard].Add(action);
                    }
                }
            }

            // Analyze trends by standard (1-14)
            var standardAnalysis = new List<object>();
            
            for (int standardNum = 1; standardNum <= 14; standardNum++)
            {
                if (allActionsByStandard.TryGetValue(standardNum, out var actions))
                {
                    // Get standard name from first action (they all have the same standard title)
                    var standardName = actions.FirstOrDefault()?.StandardTitle ?? $"Standard {standardNum}";
                    
                    // Group by status
                    var actionsByStatus = actions.GroupBy(a => a.Status ?? "unknown")
                        .ToDictionary(g => g.Key, g => g.Count());

                    // Identify repeated issues (same comments across multiple assessments)
                    var repeatedIssues = actions
                        .GroupBy(a => a.Comments?.ToLowerInvariant() ?? "")
                        .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                        .Select(g => new
                        {
                            Title = g.First().Comments?.Length > 100 
                                ? g.First().Comments.Substring(0, 100) + "..." 
                                : g.First().Comments ?? "Unknown",
                            FullComment = g.First().Comments,
                            Count = g.Count(),
                            Assessments = g.Select(a => 
                                assessmentData.Assessments.FirstOrDefault(ass => ass.AssessmentID == a.AssessmentID)?.AssessmentName ?? 
                                $"Assessment {a.AssessmentID}").Distinct().ToList()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList();

                    // Calculate trend (comparing recent vs older actions)
                    var recentActions = actions.Where(a => a.Created.HasValue && a.Created >= DateTime.Today.AddMonths(-3)).Count();
                    var olderActions = actions.Where(a => a.Created.HasValue && a.Created < DateTime.Today.AddMonths(-3)).Count();
                    var trend = recentActions > olderActions ? "Increasing" : 
                               recentActions < olderActions ? "Decreasing" : "Stable";

                    standardAnalysis.Add(new
                    {
                        StandardNumber = standardNum,
                        StandardName = standardName,
                        TotalActions = actions.Count,
                        ByStatus = actionsByStatus,
                        RepeatedIssues = repeatedIssues,
                        Trend = trend,
                        RecentActions = recentActions,
                        OlderActions = olderActions
                    });
                }
                else
                {
                    standardAnalysis.Add(new
                    {
                        StandardNumber = standardNum,
                        StandardName = $"Standard {standardNum}",
                        TotalActions = 0,
                        ByStatus = new Dictionary<string, int>(),
                        RepeatedIssues = new List<object>(),
                        Trend = "No data",
                        RecentActions = 0,
                        OlderActions = 0
                    });
                }
            }

            // Identify most issue-prone standards
            var mostIssueProne = standardAnalysis
                .OrderByDescending(s => ((dynamic)s).TotalActions)
                .Take(5)
                .ToList();


            // NEW: Analysis by Assessment Outcome
            var byOutcome = assessmentData.Assessments
                .GroupBy(a => a.AssessmentOutcome ?? "Unknown")
                .Select(g => new
                {
                    Outcome = g.Key,
                    Count = g.Count(),
                    Assessments = g.Select(a => a.AssessmentName).ToList(),
                    TotalActions = g.SelectMany(a => a.ActionsByStandard ?? new List<ActionsByStandard>())
                        .SelectMany(abs => abs.Actions ?? new List<ActionItem>())
                        .Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // NEW: Analysis by Assessment Phase
            var byPhase = assessmentData.Assessments
                .GroupBy(a => a.AssessmentPhase ?? "Unknown")
                .Select(g => new
                {
                    Phase = g.Key,
                    Count = g.Count(),
                    Assessments = g.Select(a => a.AssessmentName).ToList(),
                    TotalActions = g.SelectMany(a => a.ActionsByStandard ?? new List<ActionsByStandard>())
                        .SelectMany(abs => abs.Actions ?? new List<ActionItem>())
                        .Count(),
                    AvgActionsPerAssessment = g.SelectMany(a => a.ActionsByStandard ?? new List<ActionsByStandard>())
                        .SelectMany(abs => abs.Actions ?? new List<ActionItem>())
                        .Count() / (double)Math.Max(g.Count(), 1)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            // NEW: Overdue Actions Analysis
            var allActions = assessmentData.Assessments
                .SelectMany(a => a.ActionsByStandard ?? new List<ActionsByStandard>())
                .SelectMany(abs => abs.Actions ?? new List<ActionItem>())
                .ToList();

            // Group overdue actions by assessment, then by standard
            var overdueActionsByAssessment = allActions
                .Where(a => a.Status?.ToLower() == "open" && 
                           a.EstimatedResolutionDate.HasValue && 
                           a.EstimatedResolutionDate.Value < DateTime.Today)
                .GroupBy(a => a.AssessmentID)
                .Select(assessmentGroup =>
                {
                    var assessment = assessmentData.Assessments.FirstOrDefault(ass => ass.AssessmentID == assessmentGroup.Key);
                    var actions = assessmentGroup.ToList();
                    
                    // Group actions by standard within this assessment
                    var actionsByStandard = actions
                        .GroupBy(a => a.Standard)
                        .Select(standardGroup =>
                        {
                            var standardActions = standardGroup.Select(a => new
                            {
                                ActionID = a.ActionID,
                                Comments = a.Comments,
                                DaysOverdue = (DateTime.Today - a.EstimatedResolutionDate!.Value).Days,
                                EstimatedResolutionDate = a.EstimatedResolutionDate
                            })
                            .OrderByDescending(a => a.DaysOverdue)
                            .ToList();

                            return new
                            {
                                Standard = standardGroup.Key,
                                StandardTitle = standardGroup.First().StandardTitle,
                                ActionCount = standardActions.Count,
                                Actions = standardActions
                            };
                        })
                        .OrderBy(s => s.Standard)
                        .ToList();

                    return new
                    {
                        AssessmentID = assessmentGroup.Key,
                        AssessmentName = assessment?.AssessmentName ?? "Unknown Assessment",
                        AssessmentPhase = assessment?.AssessmentPhase ?? "Unknown",
                        TotalOverdueActions = actions.Count,
                        Standards = actionsByStandard
                    };
                })
                .OrderByDescending(a => a.TotalOverdueActions)
                .ToList();

            // Group assessments by name, then by phase
            var assessmentsByNameAndPhase = assessmentData.Assessments
                .GroupBy(a => a.AssessmentName ?? "Unknown")
                .Select(nameGroup => new
                {
                    AssessmentName = nameGroup.Key,
                    Phases = nameGroup
                        .GroupBy(a => a.AssessmentPhase ?? "Unknown")
                        .Select(phaseGroup => new
                        {
                            Phase = phaseGroup.Key,
                            Assessments = phaseGroup.Select(a => new
                            {
                                AssessmentID = a.AssessmentID,
                                AssessmentOutcome = a.AssessmentOutcome,
                                TotalActions = (a.ActionsByStandard ?? new List<ActionsByStandard>())
                                    .SelectMany(abs => abs.Actions ?? new List<ActionItem>())
                                    .Count(),
                                OpenActions = (a.ActionsByStandard ?? new List<ActionsByStandard>())
                                    .SelectMany(abs => abs.Actions ?? new List<ActionItem>())
                                    .Count(act => act.Status?.ToLower() == "open"),
                                Standards = Enumerable.Range(1, 14).Select(standardNum => new
                                {
                                    StandardNumber = standardNum,
                                    Outcome = a.ActionsByStandard?
                                        .FirstOrDefault(abs => abs.Standard == standardNum)?
                                        .StandardOutcome ?? null,
                                    HasActions = (a.ActionsByStandard?
                                        .FirstOrDefault(abs => abs.Standard == standardNum)?
                                        .Actions?.Count ?? 0) > 0
                                }).ToList()
                            }).ToList()
                        })
                        .OrderBy(p => p.Phase)
                        .ToList()
                })
                .OrderBy(g => g.AssessmentName)
                .ToList();

            ViewBag.StandardAnalysis = standardAnalysis;
            ViewBag.MostIssueProne = mostIssueProne;
            ViewBag.ByOutcome = byOutcome;
            ViewBag.ByPhase = byPhase;
            ViewBag.OverdueActionsByAssessment = overdueActionsByAssessment;
            ViewBag.OverdueActionsCount = overdueActionsByAssessment.Sum(a => a.TotalOverdueActions);
            ViewBag.AssessmentsByNameAndPhase = assessmentsByNameAndPhase;
            ViewBag.TotalAssessments = assessmentData.Assessments.Count;
            ViewBag.TotalActions = allActions.Count;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Service Standards analysis");
            TempData["ErrorMessage"] = "An error occurred while loading the service standards analysis.";
            return RedirectToAction("Index", "Home");
        }
    }

    // GET: Analysis/ExportServiceStandards
    public async Task<IActionResult> ExportServiceStandards()
    {
        try
        {
            // Get service assessment data
            var assessmentData = await _serviceAssessmentApiService.GetActionsByStandardAsync();

            if (assessmentData?.Assessments == null || !assessmentData.Assessments.Any())
            {
                TempData["ErrorMessage"] = "Unable to export service assessment data. No data available.";
                return RedirectToAction("ServiceStandards");
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Service Assessments & Actions");

            // Headers
            var headers = new[]
            {
                "Assessment ID",
                "Assessment Name",
                "Assessment Phase",
                "Assessment Outcome",
                "Assessment Status",
                "Assessment Type",
                "Standard Number",
                "Standard Title",
                "Standard Outcome",
                "Action ID",
                "Action Comments",
                "Action Status",
                "Created Date",
                "Estimated Resolution Date",
                "Days Overdue",
                "Assigned To",
                "Created By",
                "Unique ID"
            };

            // Add header row
            for (var column = 0; column < headers.Length; column++)
            {
                var cell = worksheet.Cell(1, column + 1);
                cell.Value = headers[column];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            var currentRow = 2;

            // Export all assessments and their actions
            foreach (var assessment in assessmentData.Assessments.OrderBy(a => a.AssessmentName))
            {
                if (assessment.ActionsByStandard == null || !assessment.ActionsByStandard.Any())
                {
                    // Add row for assessment with no actions
                    worksheet.Cell(currentRow, 1).Value = assessment.AssessmentID;
                    worksheet.Cell(currentRow, 2).Value = assessment.AssessmentName ?? string.Empty;
                    worksheet.Cell(currentRow, 3).Value = assessment.AssessmentPhase ?? string.Empty;
                    worksheet.Cell(currentRow, 4).Value = assessment.AssessmentOutcome ?? string.Empty;
                    worksheet.Cell(currentRow, 5).Value = assessment.AssessmentStatus ?? string.Empty;
                    worksheet.Cell(currentRow, 6).Value = assessment.AssessmentType ?? string.Empty;
                    // Leave action columns empty
                    currentRow++;
                }
                else
                {
                    // Add rows for each action
                    foreach (var actionsByStandard in assessment.ActionsByStandard.OrderBy(abs => abs.Standard))
                    {
                        if (actionsByStandard.Actions == null || !actionsByStandard.Actions.Any())
                        {
                            // Add row for standard with no actions
                            worksheet.Cell(currentRow, 1).Value = assessment.AssessmentID;
                            worksheet.Cell(currentRow, 2).Value = assessment.AssessmentName ?? string.Empty;
                            worksheet.Cell(currentRow, 3).Value = assessment.AssessmentPhase ?? string.Empty;
                            worksheet.Cell(currentRow, 4).Value = assessment.AssessmentOutcome ?? string.Empty;
                            worksheet.Cell(currentRow, 5).Value = assessment.AssessmentStatus ?? string.Empty;
                            worksheet.Cell(currentRow, 6).Value = assessment.AssessmentType ?? string.Empty;
                            worksheet.Cell(currentRow, 7).Value = actionsByStandard.Standard;
                            worksheet.Cell(currentRow, 8).Value = actionsByStandard.StandardTitle ?? string.Empty;
                            worksheet.Cell(currentRow, 9).Value = actionsByStandard.StandardOutcome ?? string.Empty;
                            // Leave action columns empty
                            currentRow++;
                        }
                        else
                        {
                            foreach (var action in actionsByStandard.Actions.OrderBy(a => a.Created))
                            {
                                worksheet.Cell(currentRow, 1).Value = assessment.AssessmentID;
                                worksheet.Cell(currentRow, 2).Value = assessment.AssessmentName ?? string.Empty;
                                worksheet.Cell(currentRow, 3).Value = assessment.AssessmentPhase ?? string.Empty;
                                worksheet.Cell(currentRow, 4).Value = assessment.AssessmentOutcome ?? string.Empty;
                                worksheet.Cell(currentRow, 5).Value = assessment.AssessmentStatus ?? string.Empty;
                                worksheet.Cell(currentRow, 6).Value = assessment.AssessmentType ?? string.Empty;
                                worksheet.Cell(currentRow, 7).Value = actionsByStandard.Standard;
                                worksheet.Cell(currentRow, 8).Value = actionsByStandard.StandardTitle ?? string.Empty;
                                worksheet.Cell(currentRow, 9).Value = actionsByStandard.StandardOutcome ?? string.Empty;
                                worksheet.Cell(currentRow, 10).Value = action.ActionID ?? string.Empty;
                                worksheet.Cell(currentRow, 11).Value = action.Comments ?? string.Empty;
                                worksheet.Cell(currentRow, 12).Value = action.Status ?? string.Empty;
                                
                                if (action.Created.HasValue)
                                {
                                    worksheet.Cell(currentRow, 13).Value = action.Created.Value;
                                    worksheet.Cell(currentRow, 13).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                                }
                                
                                if (action.EstimatedResolutionDate.HasValue)
                                {
                                    worksheet.Cell(currentRow, 14).Value = action.EstimatedResolutionDate.Value;
                                    worksheet.Cell(currentRow, 14).Style.DateFormat.Format = "yyyy-mm-dd";
                                    
                                    // Calculate days overdue if status is open and date is past
                                    if (action.Status?.ToLower() == "open" && action.EstimatedResolutionDate.Value < DateTime.Today)
                                    {
                                        var daysOverdue = (DateTime.Today - action.EstimatedResolutionDate.Value).Days;
                                        worksheet.Cell(currentRow, 15).Value = daysOverdue;
                                        if (daysOverdue > 0)
                                        {
                                            worksheet.Cell(currentRow, 15).Style.Font.FontColor = XLColor.Red;
                                        }
                                    }
                                }
                                
                                worksheet.Cell(currentRow, 16).Value = action.AssignedTo?.ToString() ?? string.Empty;
                                worksheet.Cell(currentRow, 17).Value = action.CreatedBy?.ToString() ?? string.Empty;
                                worksheet.Cell(currentRow, 18).Value = action.UniqueID ?? string.Empty;
                                
                                currentRow++;
                            }
                        }
                    }
                }
            }

            // Format columns
            worksheet.Columns().AdjustToContents();
            worksheet.Column(11).Style.Alignment.WrapText = true; // Action Comments
            worksheet.SheetView.FreezeRows(1);

            // Save to memory stream
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"service-assessments-actions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting service standards data");
            TempData["ErrorMessage"] = "An error occurred while exporting the data. Please try again.";
            return RedirectToAction("ServiceStandards");
        }
    }
}
