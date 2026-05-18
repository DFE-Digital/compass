using Compass.Helpers;
using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernStandardsController
{
    private static string NormalizeManagementSection(string? section)
    {
        var key = (section ?? "ddt").Trim().ToLowerInvariant();
        return key switch
        {
            "functional" => "functional",
            "categories" => "categories",
            "products" => "products",
            _ => "ddt"
        };
    }

    private async Task<IActionResult?> RequireStandardsManagementAccessAsync()
    {
        if (!await StandardsPermissionHelper.CanAccessModernStandardsManagementAsync(_permissions, User))
            return Forbid();
        return null;
    }

    private IActionResult RedirectToManagement(
        string section,
        string? productStatus = null,
        string? successMessage = null,
        string? errorMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(successMessage))
            TempData["SuccessMessage"] = successMessage;
        if (!string.IsNullOrWhiteSpace(errorMessage))
            TempData["ErrorMessage"] = errorMessage;
        return RedirectToAction(nameof(Management), new { section, productStatus });
    }

    private async Task<List<StandardsManagementCategoryRow>> LoadManagementCategoriesAsync()
    {
        var categories = await _context.StandardCategories.AsNoTracking()
            .Include(c => c.SubCategories)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => new StandardsManagementCategoryRow
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            SubCategories = c.SubCategories
                .OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Name)
                .Select(sc => new StandardsManagementSubCategoryRow
                {
                    Id = sc.Id,
                    CategoryId = sc.CategoryId,
                    Name = sc.Name,
                    Description = sc.Description,
                    SortOrder = sc.SortOrder,
                    IsActive = sc.IsActive
                })
                .ToList()
        }).ToList();
    }

    private async Task<List<StandardsManagementProductRow>> LoadManagementProductsAsync(string? filterStatus)
    {
        var query = _context.StandardProducts.AsNoTracking()
            .Include(p => p.CreatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filterStatus))
            query = query.Where(p => p.ApprovalStatus == filterStatus);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var linkedCounts = await _context.DdtStandardProducts.AsNoTracking()
            .GroupBy(x => x.StandardProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.ProductId, g => g.Count);

        return products.Select(p => new StandardsManagementProductRow
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Provider = p.Provider,
            Version = p.Version,
            ApprovalStatus = p.ApprovalStatus,
            DfeProductName = p.DfeProductName,
            LinkedStandardsCount = linkedCounts.GetValueOrDefault(p.Id, 0),
            CreatedByDisplay = p.CreatedByUser?.Name,
            CreatedAt = p.CreatedAt
        }).ToList();
    }

    [HttpGet("management/products/new")]
    public async Task<IActionResult> ManagementProductNew()
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        SetChrome("standards-management");
        return View("~/Views/Modern/Standards/ManagementProductForm.cshtml", new StandardsManagementProductFormViewModel());
    }

    [HttpGet("management/products/{id:int}/edit")]
    public async Task<IActionResult> ManagementProductEdit(int id)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var product = await _context.StandardProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        SetChrome("standards-management");
        return View("~/Views/Modern/Standards/ManagementProductForm.cshtml", new StandardsManagementProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Provider = product.Provider,
            Version = product.Version,
            DfeFipsProductId = product.DfeFipsProductId,
            DfeProductName = product.DfeProductName,
            ApprovalStatus = product.ApprovalStatus
        });
    }

    [HttpPost("management/products/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementProductCreate(
        string name,
        string? description,
        string? provider,
        string? version,
        string? dfeFipsProductId,
        string? dfeProductName)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToManagement("products", errorMessage: "Product name is required.");

        var product = new StandardProduct
        {
            Name = name,
            Description = description?.Trim(),
            Provider = provider?.Trim(),
            Version = version?.Trim(),
            DfeFipsProductId = dfeFipsProductId?.Trim(),
            DfeProductName = dfeProductName?.Trim(),
            ApprovalStatus = "Pending",
            CreatedByUserId = GetCurrentUserId(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StandardProducts.Add(product);
        await _context.SaveChangesAsync();

        return RedirectToManagement("products", successMessage: $"Product \"{product.Name}\" added and is pending approval.");
    }

    [HttpPost("management/products/{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementProductUpdate(
        int id,
        string name,
        string? description,
        string? provider,
        string? version,
        string? dfeFipsProductId,
        string? dfeProductName)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null) return NotFound();

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Product name is required.";
            return RedirectToAction(nameof(ManagementProductEdit), new { id });
        }

        product.Name = name;
        product.Description = description?.Trim();
        product.Provider = provider?.Trim();
        product.Version = version?.Trim();
        product.DfeFipsProductId = dfeFipsProductId?.Trim();
        product.DfeProductName = dfeProductName?.Trim();
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToManagement("products", successMessage: $"Product \"{product.Name}\" updated.");
    }

    [HttpPost("management/products/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementProductApprove(int id, string? reviewNotes = null)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null) return NotFound();

        product.ApprovalStatus = "Approved";
        product.ReviewedByUserId = GetCurrentUserId();
        product.ReviewedAt = DateTime.UtcNow;
        product.ReviewNotes = reviewNotes?.Trim();
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToManagement("products", productStatus: "Approved", successMessage: $"Product \"{product.Name}\" approved.");
    }

    [HttpPost("management/products/{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementProductReject(int id, string? reviewNotes = null)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var product = await _context.StandardProducts.FindAsync(id);
        if (product == null) return NotFound();

        product.ApprovalStatus = "Rejected";
        product.ReviewedByUserId = GetCurrentUserId();
        product.ReviewedAt = DateTime.UtcNow;
        product.ReviewNotes = reviewNotes?.Trim();
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToManagement("products", productStatus: "Rejected", successMessage: $"Product \"{product.Name}\" rejected.");
    }

    [HttpPost("management/products/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementProductDelete(int id)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var product = await _context.StandardProducts
            .Include(p => p.StandardProducts)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        if (product.StandardProducts.Any())
        {
            return RedirectToManagement("products",
                errorMessage: $"Cannot delete \"{product.Name}\" because it is linked to one or more standards.");
        }

        _context.StandardProducts.Remove(product);
        await _context.SaveChangesAsync();

        return RedirectToManagement("products", successMessage: $"Product \"{product.Name}\" deleted.");
    }

    [HttpGet("management/categories/new")]
    public async Task<IActionResult> ManagementCategoryNew()
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        SetChrome("standards-management");
        return View("~/Views/Modern/Standards/ManagementCategoryForm.cshtml", new StandardsManagementCategoryFormViewModel());
    }

    [HttpPost("management/categories/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementCategoryCreate(string name, string? description, int sortOrder = 0)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToManagement("categories", errorMessage: "Category name is required.");

        _context.StandardCategories.Add(new StandardCategory
        {
            Name = name,
            Description = description?.Trim(),
            SortOrder = sortOrder
        });
        await _context.SaveChangesAsync();

        return RedirectToManagement("categories", successMessage: $"Category \"{name}\" added.");
    }

    [HttpPost("management/categories/{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementCategoryUpdate(int id, string name, string? description, int sortOrder = 0)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var entity = await _context.StandardCategories.FindAsync(id);
        if (entity == null) return NotFound();

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToManagement("categories", errorMessage: "Category name is required.");

        entity.Name = name;
        entity.Description = description?.Trim();
        entity.SortOrder = sortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToManagement("categories", successMessage: $"Category \"{name}\" updated.");
    }

    [HttpPost("management/categories/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementCategoryToggle(int id)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var entity = await _context.StandardCategories.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToManagement("categories",
            successMessage: $"\"{entity.Name}\" is now {(entity.IsActive ? "active" : "inactive")}.");
    }

    [HttpPost("management/subcategories/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementSubCategoryCreate(
        int categoryId,
        string name,
        string? description,
        int sortOrder = 0)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToManagement("categories", errorMessage: "Sub-category name is required.");

        _context.StandardSubCategories.Add(new StandardSubCategory
        {
            CategoryId = categoryId,
            Name = name,
            Description = description?.Trim(),
            SortOrder = sortOrder
        });
        await _context.SaveChangesAsync();

        return RedirectToManagement("categories", successMessage: $"Sub-category \"{name}\" added.");
    }

    [HttpPost("management/subcategories/{id:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementSubCategoryUpdate(
        int id,
        string name,
        string? description,
        int sortOrder = 0)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var entity = await _context.StandardSubCategories.FindAsync(id);
        if (entity == null) return NotFound();

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
            return RedirectToManagement("categories", errorMessage: "Sub-category name is required.");

        entity.Name = name;
        entity.Description = description?.Trim();
        entity.SortOrder = sortOrder;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToManagement("categories", successMessage: $"Sub-category \"{name}\" updated.");
    }

    [HttpPost("management/subcategories/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManagementSubCategoryToggle(int id)
    {
        var denied = await RequireStandardsManagementAccessAsync();
        if (denied != null) return denied;

        var entity = await _context.StandardSubCategories.FindAsync(id);
        if (entity == null) return NotFound();

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return RedirectToManagement("categories",
            successMessage: $"\"{entity.Name}\" is now {(entity.IsActive ? "active" : "inactive")}.");
    }
}
