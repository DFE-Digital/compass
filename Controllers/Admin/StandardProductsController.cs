using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels.Admin;
using Compass.Attributes;
using System.Linq;
using System.Threading.Tasks;

namespace Compass.Controllers.Admin;

[Authorize]
[RequireAdmin]
[Area("Admin")]
public class StandardProductsController : Controller
{
    private readonly CompassDbContext _context;

    public StandardProductsController(CompassDbContext context)
    {
        _context = context;
    }

    // GET: Admin/StandardProducts
    public async Task<IActionResult> Index(int? editId = null, string? filterStatus = null)
    {
        var query = _context.StandardProducts.AsQueryable();

        // Filter by approval status if provided
        if (!string.IsNullOrEmpty(filterStatus))
        {
            query = query.Where(p => p.ApprovalStatus == filterStatus);
        }

        var products = await query
            .Include(p => p.CreatedByUser)
            .Include(p => p.ReviewedByUser)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.FilterStatus = filterStatus;
        ViewBag.EditId = editId;

        if (editId.HasValue)
        {
            var product = await _context.StandardProducts
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == editId.Value);
            ViewBag.EditProduct = product;
        }

        return View(products);
    }

    // GET: Admin/StandardProducts/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/StandardProducts/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string name,
        string? description,
        string? provider,
        string? version,
        string? dfeFipsProductId,
        string? dfeProductName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("Name", "Name is required");
            return View();
        }

        var currentUserId = GetCurrentUserId();

        var product = new StandardProduct
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Provider = provider?.Trim(),
            Version = version?.Trim(),
            DfeFipsProductId = dfeFipsProductId?.Trim(),
            DfeProductName = dfeProductName?.Trim(),
            ApprovalStatus = "Pending",
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StandardProducts.Add(product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' created successfully. It is pending approval.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/StandardProducts/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        int id,
        string name,
        string? description,
        string? provider,
        string? version,
        string? dfeFipsProductId,
        string? dfeProductName)
    {
        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("Name", "Name is required");
            return RedirectToAction(nameof(Index), new { editId = id });
        }

        product.Name = name.Trim();
        product.Description = description?.Trim();
        product.Provider = provider?.Trim();
        product.Version = version?.Trim();
        product.DfeFipsProductId = dfeFipsProductId?.Trim();
        product.DfeProductName = dfeProductName?.Trim();
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/StandardProducts/Approve
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? reviewNotes = null)
    {
        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();

        product.ApprovalStatus = "Approved";
        product.ReviewedByUserId = currentUserId;
        product.ReviewedAt = DateTime.UtcNow;
        product.ReviewNotes = reviewNotes?.Trim();
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' has been approved.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/StandardProducts/Reject
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reviewNotes = null)
    {
        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        var currentUserId = GetCurrentUserId();

        product.ApprovalStatus = "Rejected";
        product.ReviewedByUserId = currentUserId;
        product.ReviewedAt = DateTime.UtcNow;
        product.ReviewNotes = reviewNotes?.Trim();
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' has been rejected.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Admin/StandardProducts/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.StandardProducts
            .Include(p => p.StandardProducts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        if (product.StandardProducts.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete product '{product.Name}' because it is being used by one or more standards.";
            return RedirectToAction(nameof(Index));
        }

        _context.StandardProducts.Remove(product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Product '{product.Name}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return null;
    }
}

