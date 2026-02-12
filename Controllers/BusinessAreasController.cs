using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.AspNetCore.Authorization;

namespace Compass.Controllers;

[Authorize]
public class BusinessAreasController : Controller
{
    private readonly CompassDbContext _context;
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<BusinessAreasController> _logger;

    public BusinessAreasController(
        CompassDbContext context,
        IProductsApiService productsApiService,
        ILogger<BusinessAreasController> logger)
    {
        _context = context;
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: BusinessAreas/Index
    public async Task<IActionResult> Index(int? commissionId = null)
    {
        // Get active commissions
        var activeCommissions = await _context.Commissions
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        if (!activeCommissions.Any())
        {
            ViewBag.Message = "No active commissions are currently available.";
            return View(new BusinessAreasViewModel
            {
                BusinessAreaCompletions = new List<BusinessAreaCompletion>()
            });
        }

        // Use selected commission or default to most recent
        var selectedCommission = commissionId.HasValue
            ? activeCommissions.FirstOrDefault(c => c.Id == commissionId.Value)
            : activeCommissions.FirstOrDefault();

        if (selectedCommission == null)
        {
            TempData["ErrorMessage"] = "Selected commission not found.";
            return RedirectToAction("Index");
        }

        // Get all products (excluding Decommissioned/Decommissioning, only Published)
        var allProducts = await _productsApiService.GetAllProductsAsync();
        var eligibleProducts = allProducts
            .Where(p => p.State != null && 
                       !p.State.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) &&
                       !p.State.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase) &&
                       p.PublishedAt.HasValue)
            .ToList();

        // Get all commission submissions for this commission
        var allSubmissions = await _context.CommissionSubmissions
            .Include(cs => cs.MetricValues)
            .Where(cs => cs.CommissionId == selectedCommission.Id)
            .ToDictionaryAsync(cs => cs.ProductDocumentId, cs => cs);

        // Group products by business area
        var businessAreaGroups = new Dictionary<string, List<ProductDto>>();
        
        foreach (var product in eligibleProducts)
        {
            var businessArea = product.CategoryValues?
                .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                ?.Name ?? "Unassigned";

            if (!businessAreaGroups.ContainsKey(businessArea))
            {
                businessAreaGroups[businessArea] = new List<ProductDto>();
            }
            businessAreaGroups[businessArea].Add(product);
        }

        // Calculate statistics for each product
        var productStatuses = new Dictionary<string, CommissionSubmissionStatus>();
        foreach (var product in eligibleProducts)
        {
            var documentId = product.DocumentId ?? "";
            var submission = allSubmissions.GetValueOrDefault(documentId);
            var status = submission?.Status ?? CommissionSubmissionStatus.NotStarted;
            productStatuses[documentId] = status;
        }

        // Calculate business area completions
        var businessAreaCompletions = new List<BusinessAreaCompletion>();
        foreach (var group in businessAreaGroups.OrderBy(g => g.Key))
        {
            var baProducts = group.Value;
            var baTotal = baProducts.Count;
            var baCompleted = 0;
            var baInProgress = 0;
            var baNotStarted = 0;
            var baLate = 0;

            foreach (var product in baProducts)
            {
                var documentId = product.DocumentId ?? "";
                var status = productStatuses.GetValueOrDefault(documentId, CommissionSubmissionStatus.NotStarted);
                
                switch (status)
                {
                    case CommissionSubmissionStatus.Submitted:
                        baCompleted++;
                        break;
                    case CommissionSubmissionStatus.InProgress:
                        baInProgress++;
                        break;
                    case CommissionSubmissionStatus.Late:
                        baLate++;
                        break;
                    case CommissionSubmissionStatus.NotStarted:
                        baNotStarted++;
                        break;
                }
            }

            var baCompletionPercentage = baTotal > 0 ? (decimal)baCompleted / baTotal * 100 : 0;

            businessAreaCompletions.Add(new BusinessAreaCompletion
            {
                BusinessAreaName = group.Key,
                TotalProducts = baTotal,
                CompletedProducts = baCompleted,
                InProgressProducts = baInProgress,
                NotStartedProducts = baNotStarted,
                LateProducts = baLate,
                CompletionPercentage = Math.Round(baCompletionPercentage, 1)
            });
        }

        // Calculate overall statistics
        var totalProducts = eligibleProducts.Count;
        var completedProducts = businessAreaCompletions.Sum(ba => ba.CompletedProducts);
        var overallCompletionPercentage = totalProducts > 0 ? (decimal)completedProducts / totalProducts * 100 : 0;

        var viewModel = new BusinessAreasViewModel
        {
            Commission = selectedCommission,
            TotalProducts = totalProducts,
            CompletedProducts = completedProducts,
            InProgressProducts = businessAreaCompletions.Sum(ba => ba.InProgressProducts),
            NotStartedProducts = businessAreaCompletions.Sum(ba => ba.NotStartedProducts),
            LateProducts = businessAreaCompletions.Sum(ba => ba.LateProducts),
            CompletionPercentage = Math.Round(overallCompletionPercentage, 1),
            BusinessAreaCompletions = businessAreaCompletions
        };

        ViewBag.ActiveCommissions = activeCommissions;
        ViewBag.SelectedCommission = selectedCommission;

        return View(viewModel);
    }

    // GET: BusinessAreas/Details/{businessAreaName}
    public async Task<IActionResult> Details(string businessAreaName, int? commissionId = null)
    {
        if (string.IsNullOrEmpty(businessAreaName))
        {
            TempData["ErrorMessage"] = "Business area name is required.";
            return RedirectToAction("Index");
        }

        // Get active commissions
        var activeCommissions = await _context.Commissions
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync();

        if (!activeCommissions.Any())
        {
            TempData["ErrorMessage"] = "No active commissions are currently available.";
            return RedirectToAction("Index");
        }

        // Use selected commission or default to most recent
        var selectedCommission = commissionId.HasValue
            ? activeCommissions.FirstOrDefault(c => c.Id == commissionId.Value)
            : activeCommissions.FirstOrDefault();

        if (selectedCommission == null)
        {
            TempData["ErrorMessage"] = "Selected commission not found.";
            return RedirectToAction("Index");
        }

        // Get all products (excluding Decommissioned/Decommissioning, only Published)
        var allProducts = await _productsApiService.GetAllProductsAsync();
        var eligibleProducts = allProducts
            .Where(p => p.State != null && 
                       !p.State.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) &&
                       !p.State.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase) &&
                       p.PublishedAt.HasValue)
            .ToList();

        // Filter products by business area
        var businessAreaProducts = eligibleProducts
            .Where(p =>
            {
                var ba = p.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
                    ?.Name ?? "Unassigned";
                return ba.Equals(businessAreaName, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        // Get all commission submissions for this commission
        var allSubmissions = await _context.CommissionSubmissions
            .Include(cs => cs.MetricValues)
            .Where(cs => cs.CommissionId == selectedCommission.Id)
            .ToDictionaryAsync(cs => cs.ProductDocumentId, cs => cs);

        // Create product status view models
        var productStatuses = new List<BusinessAreaProductStatus>();
        foreach (var product in businessAreaProducts.OrderBy(p => p.Title))
        {
            var documentId = product.DocumentId ?? "";
            var submission = allSubmissions.GetValueOrDefault(documentId);
            var status = submission?.Status ?? CommissionSubmissionStatus.NotStarted;
            var completedMetrics = submission?.MetricValues?.Count(mv => mv.IsComplete) ?? 0;
            var totalMetrics = submission?.MetricValues?.Count ?? 0;

            productStatuses.Add(new BusinessAreaProductStatus
            {
                Product = product,
                Status = status,
                CompletedMetrics = completedMetrics,
                TotalMetrics = totalMetrics,
                Submission = submission
            });
        }

        // Calculate statistics
        var totalProducts = productStatuses.Count;
        var completedProducts = productStatuses.Count(p => p.Status == CommissionSubmissionStatus.Submitted);
        var inProgressProducts = productStatuses.Count(p => p.Status == CommissionSubmissionStatus.InProgress);
        var notStartedProducts = productStatuses.Count(p => p.Status == CommissionSubmissionStatus.NotStarted);
        var lateProducts = productStatuses.Count(p => p.Status == CommissionSubmissionStatus.Late);
        var completionPercentage = totalProducts > 0 ? (decimal)completedProducts / totalProducts * 100 : 0;

        var viewModel = new BusinessAreaDetailsViewModel
        {
            BusinessAreaName = businessAreaName,
            Commission = selectedCommission,
            TotalProducts = totalProducts,
            CompletedProducts = completedProducts,
            InProgressProducts = inProgressProducts,
            NotStartedProducts = notStartedProducts,
            LateProducts = lateProducts,
            CompletionPercentage = Math.Round(completionPercentage, 1),
            ProductStatuses = productStatuses
        };

        ViewBag.ActiveCommissions = activeCommissions;
        ViewBag.SelectedCommission = selectedCommission;

        return View(viewModel);
    }
}
