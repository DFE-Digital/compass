using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;

namespace FipsReporting.Controllers.Reporting
{
    public class ReportingProductsController : BaseController
    {
        private readonly IReportingService _reportingService;
        private readonly CmsApiService _cmsApiService;
        private readonly IMetricsService _metricsService;
        private readonly IMilestoneService _milestoneService;

        public ReportingProductsController(IReportingService reportingService, CmsApiService cmsApiService, IMetricsService metricsService, IMilestoneService milestoneService)
        {
            _reportingService = reportingService;
            _cmsApiService = cmsApiService;
            _metricsService = metricsService;
            _milestoneService = milestoneService;
        }

        public async Task<IActionResult> Index(ProductFilter filter)
        {
            try
            {
                // Get user's email from claims
                var userEmail = GetUserEmail();
                
                // For development, use a hardcoded email if no user is authenticated
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk";
                    Console.WriteLine($"ReportingProductsController: No authenticated user found, using development email: {userEmail}");
                }
                else
                {
                    Console.WriteLine($"ReportingProductsController: Using authenticated user email: {userEmail}");
                }
                
                // Use CMS API to get products assigned to user via product-contacts
                var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
                
                // Log the number of products returned
                Console.WriteLine($"ReportingProductsController: Found {assignedProducts.Count} products for user {userEmail}");
                
                // Map CmsProduct to ProductViewModel and calculate milestone counts
                var productsWithMilestones = new List<ProductViewModel>();
                
                foreach (var product in assignedProducts)
                {
                    var productViewModel = _cmsApiService.MapToViewModel(product, true);
                    
                    // Get milestone count for this product
                    if (!string.IsNullOrEmpty(productViewModel.FipsId))
                    {
                        var milestones = await _milestoneService.GetMilestonesByFipsIdAsync(productViewModel.FipsId);
                        productViewModel.MilestoneCount = milestones.Count;
                    }
                    else
                    {
                        productViewModel.MilestoneCount = 0;
                    }
                    
                    productsWithMilestones.Add(productViewModel);
                }
                
                ViewBag.Filter = filter;
                ViewBag.UserEmail = userEmail;
                
                return View("~/Views/Reporting/Products/Index.cshtml", productsWithMilestones);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ReportingProductsController: Error: {ex.Message}");
                return HandleException(ex, nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var product = await _cmsApiService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                var userEmail = GetUserEmail();
                var isAllocated = await _reportingService.IsProductAllocatedToUserAsync(id.ToString(), userEmail);
                var applicableMetrics = await _metricsService.GetApplicableMetricsAsync(id.ToString());
                
                var viewModel = _cmsApiService.MapToViewModel(product, isAllocated);
                
                ViewBag.ApplicableMetrics = applicableMetrics;
                ViewBag.UserEmail = userEmail;
                
                return View("~/Views/Reporting/Products/Details.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Details));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllocateProduct(string productId)
        {
            try
            {
                var userEmail = GetUserEmail();
                await _reportingService.AllocateProductToUserAsync(productId, userEmail, GetUserEmail());
                TempData["Success"] = "Product allocated successfully.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(AllocateProduct));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeallocateProduct(string productId)
        {
            try
            {
                var userEmail = GetUserEmail();
                await _reportingService.DeallocateProductFromUserAsync(productId, userEmail);
                TempData["Success"] = "Product deallocated successfully.";
                return RedirectToAction(nameof(Details), new { id = productId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(DeallocateProduct));
            }
        }

        public async Task<IActionResult> ReportData(int productId, int metricId)
        {
            try
            {
                var product = await _cmsApiService.GetProductByIdAsync(productId);
                var metric = await _metricsService.GetMetricByIdAsync(metricId);
                
                if (product == null || metric == null)
                {
                    return NotFound();
                }

                ViewBag.Product = product;
                ViewBag.Metric = metric;
                
                return View("~/Views/Reporting/Products/ReportData.cshtml");
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(ReportData));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReportData(ReportingData reportData)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // TODO: Implement reporting data submission
                    TempData["Success"] = "Report data submitted successfully.";
                    return RedirectToAction(nameof(Details), new { id = reportData.ProductId });
                }
                
                TempData["Error"] = "Please provide valid report data.";
                return RedirectToAction(nameof(ReportData), new { productId = reportData.ProductId, metricId = reportData.MetricId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(SubmitReportData));
            }
        }
    }
}
