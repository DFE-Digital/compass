using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;

namespace FipsReporting.Controllers.Reporting
{
    public class ReportingMilestonesController : BaseController
    {
        private readonly IMilestoneService _milestoneService;
        private readonly CmsApiService _cmsApiService;

        public ReportingMilestonesController(IMilestoneService milestoneService, CmsApiService cmsApiService)
        {
            _milestoneService = milestoneService;
            _cmsApiService = cmsApiService;
        }

        public async Task<IActionResult> Index(string productId)
        {
            try
            {
                // Get user's email from claims
                var userEmail = GetUserEmail();
                
                // For development, use a hardcoded email if no user is authenticated
                if (string.IsNullOrEmpty(userEmail))
                {
                    userEmail = "andy.jones@education.gov.uk";
                }
                
                // Get products assigned to current user to get the product name
                var assignedProducts = await _cmsApiService.GetProductsByUserEmailAsync(userEmail);
                var product = assignedProducts.FirstOrDefault(p => p.FipsId == productId);
                
                var milestones = await _milestoneService.GetMilestonesForProductAsync(productId);
                ViewBag.ProductId = productId;
                ViewBag.FipsId = productId;
                ViewBag.ProductName = product?.Title ?? "Unknown Product";
                
                // Use the ProductMilestones view for individual product milestones
                return View("~/Views/Reporting/milestones/ProductMilestones.cshtml", milestones);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Index));
            }
        }

        public IActionResult Create(string productId)
        {
            ViewBag.ProductId = productId;
            return View("~/Views/Reporting/milestones/Create.cshtml", new Milestone { ProductId = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Milestone milestone)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _milestoneService.CreateMilestoneAsync(milestone, GetUserEmail());
                    TempData["Success"] = "Milestone created successfully.";
                    return RedirectToAction(nameof(Index), new { productId = milestone.ProductId });
                }
                ViewBag.ProductId = milestone.ProductId;
                return View("~/Views/Reporting/milestones/Create.cshtml", milestone);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Create));
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var milestone = await _milestoneService.GetMilestoneByIdAsync(id);
                if (milestone == null)
                {
                    return NotFound();
                }
                return View(milestone);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Details));
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var milestone = await _milestoneService.GetMilestoneByIdAsync(id);
                if (milestone == null)
                {
                    return NotFound();
                }
                return View(milestone);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Edit));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Milestone milestone)
        {
            try
            {
                if (id != milestone.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    await _milestoneService.UpdateMilestoneAsync(milestone, GetUserEmail());
                    TempData["Success"] = "Milestone updated successfully.";
                    return RedirectToAction(nameof(Details), new { id = milestone.Id });
                }
                return View(milestone);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Edit));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUpdate(int milestoneId, MilestoneUpdate update)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _milestoneService.AddMilestoneUpdateAsync(update);
                    TempData["Success"] = "Milestone update added successfully.";
                }
                else
                {
                    TempData["Error"] = "Please provide valid update information.";
                }
                return RedirectToAction(nameof(Details), new { id = milestoneId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(AddUpdate));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var milestone = await _milestoneService.GetMilestoneByIdAsync(id);
                if (milestone == null)
                {
                    return NotFound();
                }

                await _milestoneService.DeleteMilestoneAsync(id);
                TempData["Success"] = "Milestone deleted successfully.";
                return RedirectToAction(nameof(Index), new { productId = milestone.ProductId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Delete));
            }
        }
    }
}
