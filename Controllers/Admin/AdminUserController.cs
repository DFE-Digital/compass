using FipsReporting.Data;
using FipsReporting.Models;
using FipsReporting.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FipsReporting.Controllers.Admin
{
    public class AdminUserController : Controller
    {
        private readonly IUserPermissionService _userPermissionService;
        private readonly FipsReporting.Services.IAuthenticationService _authenticationService;
        private readonly ILogger<AdminUserController> _logger;

        public AdminUserController(IUserPermissionService userPermissionService, 
            FipsReporting.Services.IAuthenticationService authenticationService, ILogger<AdminUserController> logger)
        {
            _userPermissionService = userPermissionService;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        protected string GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("preferred_username")?.Value ?? string.Empty;
        }

        protected IActionResult HandleException(Exception ex, string action)
        {
            _logger.LogError(ex, "Error in {Action}", action);
            TempData["Error"] = "An error occurred while processing your request. Please try again.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userPermissions = await _userPermissionService.GetAllUserPermissionsAsync();
                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-users";
                return View("~/Views/Admin/AdminUser/Index.cshtml", userPermissions);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Index");
            }
        }

        public IActionResult Create()
        {
            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "manage-users";
            return View("~/Views/Admin/AdminUser/Create.cshtml", new UserPermissionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserPermissionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["ActiveNav"] = "admin-users";
                    return View(model);
                }

                var userPermission = new UserPermission
                {
                    Email = model.Email,
                    Name = model.Name,
                    IsActive = true,
                    CanAddProduct = model.CanAddProduct,
                    CanEditProduct = model.CanEditProduct,
                    CanDeleteProduct = model.CanDeleteProduct,
                    CanAddMetric = model.CanAddMetric,
                    CanEditMetric = model.CanEditMetric,
                    CanDeleteMetric = model.CanDeleteMetric,
                    CanAddMilestone = model.CanAddMilestone,
                    CanEditMilestone = model.CanEditMilestone,
                    CanDeleteMilestone = model.CanDeleteMilestone,
                    CanAddUser = model.CanAddUser,
                    CanEditUser = model.CanEditUser,
                    CanViewReports = model.CanViewReports,
                    CanSubmitReports = model.CanSubmitReports
                };

                var currentUser = GetUserEmail();
                await _userPermissionService.CreateUserPermissionAsync(userPermission, currentUser);

                TempData["Success"] = $"User permissions created successfully for {model.Email}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Create");
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var userPermission = await _userPermissionService.GetAllUserPermissionsAsync();
                var user = userPermission.FirstOrDefault(u => u.Id == id);
                
                if (user == null)
                {
                    TempData["Error"] = "User not found";
                    return RedirectToAction(nameof(Index));
                }

                var model = new UserPermissionViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    IsActive = user.IsActive,
                    CanAddProduct = user.CanAddProduct,
                    CanEditProduct = user.CanEditProduct,
                    CanDeleteProduct = user.CanDeleteProduct,
                    CanAddMetric = user.CanAddMetric,
                    CanEditMetric = user.CanEditMetric,
                    CanDeleteMetric = user.CanDeleteMetric,
                    CanAddMilestone = user.CanAddMilestone,
                    CanEditMilestone = user.CanEditMilestone,
                    CanDeleteMilestone = user.CanDeleteMilestone,
                    CanAddUser = user.CanAddUser,
                    CanEditUser = user.CanEditUser,
                    CanViewReports = user.CanViewReports,
                    CanSubmitReports = user.CanSubmitReports
                };

                ViewData["ActiveNav"] = "admin";
                ViewData["ActiveNavItem"] = "manage-users";
                return View("~/Views/Admin/AdminUser/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Edit");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserPermissionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["ActiveNav"] = "admin";
                    ViewData["ActiveNavItem"] = "manage-users";
                    return View("~/Views/Admin/AdminUser/Edit.cshtml", model);
                }

                var userPermission = new UserPermission
                {
                    Id = model.Id,
                    Email = model.Email,
                    Name = model.Name,
                    IsActive = model.IsActive,
                    CanAddProduct = model.CanAddProduct,
                    CanEditProduct = model.CanEditProduct,
                    CanDeleteProduct = model.CanDeleteProduct,
                    CanAddMetric = model.CanAddMetric,
                    CanEditMetric = model.CanEditMetric,
                    CanDeleteMetric = model.CanDeleteMetric,
                    CanAddMilestone = model.CanAddMilestone,
                    CanEditMilestone = model.CanEditMilestone,
                    CanDeleteMilestone = model.CanDeleteMilestone,
                    CanAddUser = model.CanAddUser,
                    CanEditUser = model.CanEditUser,
                    CanViewReports = model.CanViewReports,
                    CanSubmitReports = model.CanSubmitReports
                };

                var currentUser = GetUserEmail();
                await _userPermissionService.UpdateUserPermissionAsync(userPermission, currentUser);

                TempData["Success"] = $"User permissions updated successfully for {model.Email}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Edit");
            }
        }

    }
}
