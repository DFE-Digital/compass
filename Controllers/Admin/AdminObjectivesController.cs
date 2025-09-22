using Microsoft.AspNetCore.Mvc;
using FipsReporting.Data;
using FipsReporting.Services;
using FipsReporting.Models;

namespace FipsReporting.Controllers.Admin
{
    public class AdminObjectivesController : BaseController
    {
        private readonly IObjectiveService _objectiveService;
        private readonly ILogger<AdminObjectivesController> _logger;

        public AdminObjectivesController(IObjectiveService objectiveService, ILogger<AdminObjectivesController> logger)
        {
            _objectiveService = objectiveService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-objectives";
                var objectives = await _objectiveService.GetAllObjectivesAsync();
                return View("~/Views/Admin/AdminObjectives/Index.cshtml", objectives);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading objectives");
                TempData["Error"] = "Error loading objectives. Please try again.";
                return View(new List<Objective>());
            }
        }

        public IActionResult Create()
        {
            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-objectives";
            return View("~/Views/Admin/AdminObjectives/Create.cshtml", new Objective());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Objective objective)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    objective.CreatedBy = GetUserEmail();
                    objective.UpdatedBy = GetUserEmail();
                    await _objectiveService.CreateObjectiveAsync(objective);
                    TempData["Success"] = "Objective created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Please provide valid objective information.";
                    return View("~/Views/Admin/AdminObjectives/Create.cshtml", objective);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating objective");
                TempData["Error"] = "Error creating objective. Please try again.";
                return View("~/Views/Admin/AdminObjectives/Create.cshtml", objective);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-objectives";
                var objective = await _objectiveService.GetObjectiveByIdAsync(id);
                if (objective == null)
                {
                    TempData["Error"] = "Objective not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(objective);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading objective details");
                TempData["Error"] = "Error loading objective details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-objectives";
                var objective = await _objectiveService.GetObjectiveByIdAsync(id);
                if (objective == null)
                {
                    TempData["Error"] = "Objective not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(objective);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading objective for editing");
                TempData["Error"] = "Error loading objective. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Objective objective)
        {
            try
            {
                if (id != objective.Id)
                {
                    TempData["Error"] = "Objective ID mismatch.";
                    return RedirectToAction(nameof(Index));
                }

                if (ModelState.IsValid)
                {
                    objective.UpdatedBy = GetUserEmail();
                    await _objectiveService.UpdateObjectiveAsync(objective);
                    TempData["Success"] = "Objective updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["Error"] = "Please provide valid objective information.";
                    return View("~/Views/Admin/AdminObjectives/Edit.cshtml", objective);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating objective");
                TempData["Error"] = "Error updating objective. Please try again.";
                return View("~/Views/Admin/AdminObjectives/Edit.cshtml", objective);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-objectives";
                var objective = await _objectiveService.GetObjectiveByIdAsync(id);
                if (objective == null)
                {
                    TempData["Error"] = "Objective not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(objective);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading objective for deletion");
                TempData["Error"] = "Error loading objective. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _objectiveService.DeleteObjectiveAsync(id);
                TempData["Success"] = "Objective deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting objective");
                TempData["Error"] = "Error deleting objective. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}
