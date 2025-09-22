using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Models;
using FipsReporting.Data;

namespace FipsReporting.Controllers.Admin
{
    public class AdminMilestonesController : BaseController
    {
        private readonly IMilestoneService _milestoneService;

        public AdminMilestonesController(IMilestoneService milestoneService)
        {
            _milestoneService = milestoneService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-milestones";
                var overdueMilestones = await _milestoneService.GetOverdueMilestonesAsync();
                ViewBag.OverdueCount = overdueMilestones.Count;
                return View(overdueMilestones);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(Index));
            }
        }

        public async Task<IActionResult> AllMilestones()
        {
            try
            {
                var milestones = await _milestoneService.GetMilestonesByStatusAsync("Not Started");
                return View(milestones);
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(AllMilestones));
            }
        }

        public async Task<IActionResult> MilestoneDetails(int id)
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
                return HandleException(ex, nameof(MilestoneDetails));
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
                return RedirectToAction(nameof(MilestoneDetails), new { id = milestoneId });
            }
            catch (Exception ex)
            {
                return HandleException(ex, nameof(AddUpdate));
            }
        }
    }
}
