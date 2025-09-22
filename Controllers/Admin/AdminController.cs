using Microsoft.AspNetCore.Mvc;
using FipsReporting.Services;
using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;

namespace FipsReporting.Controllers.Admin
{
    public class AdminController : Controller
    {
        private readonly ReportingDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ReportingDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Admin";
            ViewData["ActiveNav"] = "admin";
            ViewData["ActiveNavItem"] = "dashboard";

            try
            {
                var viewModel = new AdminViewModel
                {
                    TotalUsers = await _context.UserPermissions.CountAsync(),
                    ActiveUsers = await _context.UserPermissions.CountAsync(u => u.IsActive),
                    TotalMetrics = await _context.PerformanceMetrics.CountAsync(),
                    ActiveMetrics = await _context.PerformanceMetrics.CountAsync(m => m.Enabled),
                    TotalMilestones = await _context.Milestones.CountAsync(),
                    OverdueMilestones = await _context.Milestones.CountAsync(m => m.DueDate < DateTime.UtcNow && m.Status != "Completed"),
                    TotalProducts = await _context.PerformanceMetricData.Select(p => p.ProductId).Distinct().CountAsync(),
                    MonthlySubmissions = await _context.PerformanceMetricData.CountAsync(p => p.CreatedAt.Month == DateTime.UtcNow.Month && p.CreatedAt.Year == DateTime.UtcNow.Year)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                
                // Return a basic view model if there's an error
                var viewModel = new AdminViewModel
                {
                    TotalUsers = 0,
                    ActiveUsers = 0,
                    TotalMetrics = 0,
                    ActiveMetrics = 0,
                    TotalMilestones = 0,
                    OverdueMilestones = 0,
                    TotalProducts = 0,
                    MonthlySubmissions = 0
                };

                return View(viewModel);
            }
        }
    }

    public class AdminViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalMetrics { get; set; }
        public int ActiveMetrics { get; set; }
        public int TotalMilestones { get; set; }
        public int OverdueMilestones { get; set; }
        public int TotalProducts { get; set; }
        public int MonthlySubmissions { get; set; }
    }
}
