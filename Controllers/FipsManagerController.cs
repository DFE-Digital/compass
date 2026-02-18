using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Compass.Services;
using Compass.Models;
using Compass.Services.Fips;
using Compass.Models.Fips;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;
using Compass.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace Compass.Controllers;

[Authorize]
public class FipsManagerController : Controller
{
    private readonly ILogger<FipsManagerController> _logger;
    private readonly IProductsApiService _productsApiService;
    private readonly IConfiguration _configuration;
    private readonly ICmdbService _cmdbService;
    private readonly IMemoryCache _cache;
    private readonly IPermissionService _permissionService;
    private readonly CompassDbContext _context;
    private readonly INotificationService _notificationService;

    public FipsManagerController(
        ILogger<FipsManagerController> logger,
        IProductsApiService productsApiService,
        IConfiguration configuration,
        ICmdbService cmdbService,
        IMemoryCache cache,
        IPermissionService permissionService,
        CompassDbContext context,
        INotificationService notificationService)
    {
        _logger = logger;
        _productsApiService = productsApiService;
        _configuration = configuration;
        _cmdbService = cmdbService;
        _cache = cache;
        _permissionService = permissionService;
        _context = context;
        _notificationService = notificationService;
    }

    private async Task<bool> HasFipsManagerAccessAsync()
    {
        var userEmail = User?.Identity?.Name ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userEmail))
        {
            return false;
        }

        try
        {
            // Check if user is in allowed groups: "Central Operations Admin", "HOP", or "Design Operations"
            var allowedGroups = new[] { "Central Operations Admin", "HOP", "Design Operations" };
            foreach (var groupName in allowedGroups)
            {
                if (await _permissionService.IsInGroupAsync(userEmail, groupName))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FIPS Manager access for {Email}", userEmail);
            // Non-blocking: default to false
        }

        return false;
    }

    // GET: FipsManager/Index
    public async Task<IActionResult> Index()
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        try
        {
            ViewData["Title"] = "FIPS Manager - Active Products";

            // Clear cache to ensure real-time data
            _cache.Remove("products_list_all_states");
            _cache.Remove("products_list_all_states_");
            
            // Get all products (will fetch fresh data from CMS)
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            
            // Filter to only Active products (State = "Active"), excluding those with Type or Phase category "Decommissioned" or "Decommissioning"
            var excludedValues = new[] { "Decommissioned", "Decommissioning" };
            var activeProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.State) 
                    && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase)
                    && !HasCategoryValue(p, "Type", excludedValues)
                    && !HasCategoryValue(p, "Phase", excludedValues))
                .OrderBy(p => p.Title)
                .ToList();

            // Calculate completion data for each product
            var completionItems = activeProducts
                .Select(CreateProductCompletionItem)
                .ToList();

            var averageCompletion = completionItems.Any()
                ? completionItems.Average(p => p.CompletionPercentage)
                : 0;

            var viewModel = new FipsManagerIndexViewModel
            {
                Products = completionItems,
                AverageCompletionPercentage = averageCompletion,
                TotalProducts = completionItems.Count
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS Manager index");
            TempData["ErrorMessage"] = "An error occurred while loading products. Please try again.";
            return View(new FipsManagerIndexViewModel());
        }
    }

    // GET: FipsManager/NewProducts
    public async Task<IActionResult> NewProducts()
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        try
        {
            ViewData["Title"] = "FIPS Manager - New Products";

            // Clear cache to ensure real-time data
            _cache.Remove("products_list_all_states");
            _cache.Remove("products_list_all_states_");
            
            // Get all products (will fetch fresh data from CMS)
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            
            // Filter to only New products (State = "New"), excluding those with Type or Phase category "Decommissioned" or "Decommissioning"
            var excludedValues = new[] { "Decommissioned", "Decommissioning" };
            var newProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.State) 
                    && p.State.Equals("New", StringComparison.OrdinalIgnoreCase)
                    && !HasCategoryValue(p, "Type", excludedValues)
                    && !HasCategoryValue(p, "Phase", excludedValues))
                .OrderBy(p => p.Title)
                .ToList();

            // Calculate completion data for each product
            var completionItems = newProducts
                .Select(CreateProductCompletionItem)
                .ToList();

            var averageCompletion = completionItems.Any()
                ? completionItems.Average(p => p.CompletionPercentage)
                : 0;

            var viewModel = new FipsManagerIndexViewModel
            {
                Products = completionItems,
                AverageCompletionPercentage = averageCompletion,
                TotalProducts = completionItems.Count
            };

            return View("Index", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS Manager new products");
            TempData["ErrorMessage"] = "An error occurred while loading products. Please try again.";
            return View("Index", new FipsManagerIndexViewModel());
        }
    }

    // GET: FipsManager/ProductDetails/{documentId}
    public async Task<IActionResult> ProductDetails(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get all category values grouped by type
            var categoryTypes = await _productsApiService.GetAllCategoryValuesByTypeAsync();

            // Get current category values for the product
            var currentCategoryValueIds = product.CategoryValues?
                .Select(cv => cv.Id)
                .ToList() ?? new List<int>();

            var viewModel = new FipsManagerProductDetailsViewModel
            {
                Product = product,
                CategoryTypes = categoryTypes,
                CurrentCategoryValueIds = currentCategoryValueIds,
                CompletionItem = CreateProductCompletionItem(product)
            };

            ViewData["Title"] = $"FIPS Manager - {product.Title}";

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product details for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while loading the product. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: FipsManager/UpdateProduct
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProduct(
        string documentId,
        string? shortDescription,
        string? longDescription,
        string? productUrl,
        List<int>? categoryValueIds)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Get product first to get fipsId for the update payload
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null || string.IsNullOrEmpty(product.FipsId))
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            var fipsId = product.FipsId;
            var success = true;
            var messages = new List<string>();

            // Build update payload with fipsId to prevent regeneration
            var updateData = new Dictionary<string, object>
            {
                ["fips_id"] = fipsId
            };

            // Add fields to update
            if (shortDescription != null)
            {
                updateData["short_description"] = shortDescription.Trim();
            }

            if (longDescription != null)
            {
                updateData["long_description"] = longDescription.Trim();
            }

            if (productUrl != null)
            {
                updateData["product_url"] = productUrl.Trim();
            }

            // CRITICAL: Preserve existing category values if not being updated
            // When doing a PUT request, Strapi will clear category_values if they're not included
            // Check if categories are being explicitly updated (not null and has values)
            bool isUpdatingCategories = categoryValueIds != null && categoryValueIds.Any();
            bool isUpdatingOtherFields = shortDescription != null || longDescription != null || productUrl != null;
            
            if (isUpdatingCategories)
            {
                // User explicitly provided category values - use them
                updateData["category_values"] = categoryValueIds;
                _logger.LogInformation("Updating category values for product {DocumentId} with {Count} values", 
                    documentId, categoryValueIds.Count);
            }
            else if (isUpdatingOtherFields)
            {
                // User is updating other fields (description, URL) but not categories
                // Preserve existing category values to prevent them from being cleared
                var currentCategoryValues = new List<int>();
                if (product.CategoryValues != null && product.CategoryValues.Any())
                {
                    foreach (var cv in product.CategoryValues)
                    {
                        currentCategoryValues.Add(cv.Id);
                    }
                    _logger.LogInformation("Preserving {Count} existing category values for product {DocumentId} while updating other fields: {Ids}", 
                        currentCategoryValues.Count, documentId, string.Join(", ", currentCategoryValues));
                    updateData["category_values"] = currentCategoryValues;
                }
                else
                {
                    _logger.LogInformation("Product {DocumentId} has no category values to preserve when updating description.", documentId);
                    // Still include empty array to be explicit
                    updateData["category_values"] = currentCategoryValues;
                }
            }
            // If neither categories nor other fields are being updated, don't include category_values
            // (This shouldn't happen in practice, but handle it gracefully)

            // Perform update using documentId
            var updateSuccess = await UpdateProductByDocumentIdAsync(documentId, updateData);
            if (updateSuccess)
            {
                var updateMessages = new List<string>();
                if (shortDescription != null || longDescription != null) updateMessages.Add("Product description updated.");
                if (productUrl != null) updateMessages.Add("Product URL updated.");
                if (categoryValueIds != null) updateMessages.Add("Product categories updated.");
                messages.AddRange(updateMessages);
            }
            else
            {
                success = false;
                messages.Add("Failed to update product.");
            }

            if (success)
            {
                TempData["SuccessMessage"] = string.Join(" ", messages);
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(" ", messages);
            }

            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while updating the product. Please try again.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
    }

    // POST: FipsManager/UpdateProductRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProductRole(
        string documentId,
        string roleFieldName,
        string? entraUserObjectId,
        string? entraUserEmail,
        string? entraUserName,
        bool isMultipleAllowed = false)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(roleFieldName))
        {
            TempData["ErrorMessage"] = "Product ID and role are required.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }

        // Get product to get fipsId
        var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
        if (product == null || string.IsNullOrEmpty(product.FipsId))
        {
            TempData["ErrorMessage"] = "Product not found.";
            return RedirectToAction(nameof(Index));
        }

        var fipsId = product.FipsId;

        // Validate role field name
        var validRoles = new[] 
        { 
            "service_owner", 
            "product_manager", 
            "delivery_manager", 
            "Information_asset_owner", 
            "reporting_user", 
            "senior_responsible_officer", 
            "service_designs", 
            "user_researchers" 
        };
        
        if (!validRoles.Contains(roleFieldName))
        {
            TempData["ErrorMessage"] = "Invalid role specified.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }

        // For single-user roles, require user selection
        var singleUserRoles = new[] { "service_owner", "Information_asset_owner", "senior_responsible_officer" };
        var isSingleUserRole = singleUserRoles.Contains(roleFieldName);

        if (isSingleUserRole && (string.IsNullOrWhiteSpace(entraUserObjectId) || string.IsNullOrWhiteSpace(entraUserEmail)))
        {
            TempData["ErrorMessage"] = "Please select a user from the search results.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }

        try
        {
            // Get or create Entra user
            if (!string.IsNullOrWhiteSpace(entraUserObjectId) && !string.IsNullOrWhiteSpace(entraUserEmail))
            {
                var entraUser = await _productsApiService.GetOrCreateEntraUserAsync(
                    entraUserEmail.Trim(),
                    entraUserObjectId.Trim(),
                    string.IsNullOrWhiteSpace(entraUserName) ? entraUserEmail.Trim() : entraUserName.Trim());

                if (entraUser == null)
                {
                    TempData["ErrorMessage"] = "Failed to get or create Entra User. Please try again.";
                    return RedirectToAction(nameof(ProductDetails), new { documentId });
                }

                // Update role using documentId, ensuring fipsId is in the payload
                var updateData = new Dictionary<string, object>
                {
                    ["fips_id"] = fipsId,
                    [roleFieldName] = new[] { entraUser.Id }
                };

                var success = await UpdateProductByDocumentIdAsync(documentId, updateData);
                if (success)
                {
                    TempData["SuccessMessage"] = $"{GetRoleDisplayName(roleFieldName)} updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Failed to update {GetRoleDisplayName(roleFieldName)}.";
                }
            }
            else if (isMultipleAllowed)
            {
                // For multiple-user roles, we might want to clear or handle differently
                // For now, we'll just show a message
                TempData["InfoMessage"] = "Multiple users are allowed for this role. Use the add user functionality.";
            }

            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product role {RoleFieldName} for {DocumentId}", roleFieldName, documentId);
            TempData["ErrorMessage"] = "An error occurred while updating the role. Please try again.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
    }

    // POST: FipsManager/SyncRolesFromCmdb
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncRolesFromCmdb(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Get product to get cmdb_sys_id
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null || string.IsNullOrEmpty(product.CmdbSysId))
            {
                TempData["ErrorMessage"] = "Product not found or does not have a CMDB sys_id. Cannot sync roles from CMDB.";
                return RedirectToAction(nameof(ProductDetails), new { documentId });
            }

            var cmdbSysId = product.CmdbSysId;
            _logger.LogInformation("Syncing roles from CMDB for product {DocumentId} with CMDB sys_id {CmdbSysId}", 
                documentId, cmdbSysId);

            // Get service offering from CMDB
            var cmdbEntry = await _cmdbService.GetServiceOfferingBySysIdAsync(cmdbSysId);
            if (cmdbEntry == null)
            {
                TempData["ErrorMessage"] = $"Could not find service offering in CMDB with sys_id: {cmdbSysId}";
                return RedirectToAction(nameof(ProductDetails), new { documentId });
            }

            // Get users from CMDB
            var cmdbUsers = await _cmdbService.GetServiceOfferingUsersAsync(cmdbEntry);
            
            // Map CMDB roles to Strapi product relation field names
            var roleMapping = new Dictionary<string, (CmdbUser? user, string strapiFieldName)>
            {
                { "service_owner", (cmdbUsers.ServiceOwner, "service_owner") },
                { "product_manager", (cmdbUsers.ProductManager, "product_manager") },
                { "delivery_manager", (cmdbUsers.DeliveryManager, "delivery_manager") },
                { "information_asset_owner", (cmdbUsers.InformationAssetOwner, "Information_asset_owner") },
                { "senior_responsible_owner", (cmdbUsers.SeniorResponsibleOwner, "senior_responsible_officer") }
            };

            var entraUserIds = new Dictionary<string, object>(); // Can be string (documentId) or int (numeric ID)
            var usersProcessed = 0;
            var rolesUpdated = 0;

            // Create/update entra-users for each role that has a user
            foreach (var (cmdbRole, (user, strapiFieldName)) in roleMapping)
            {
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    // Create or update entra-user
                    var entraUser = await _productsApiService.GetOrCreateEntraUserAsync(
                        user.Email,
                        user.FederatedId,
                        user.Name,
                        user.FirstName,
                        user.LastName);

                    if (entraUser != null)
                    {
                        // Use documentId if available, otherwise fall back to numeric Id
                        // Strapi v5 prefers documentId for relations, but accepts numeric ID as fallback
                        if (!string.IsNullOrEmpty(entraUser.DocumentId))
                        {
                            entraUserIds[strapiFieldName] = entraUser.DocumentId;
                            usersProcessed++;
                            _logger.LogInformation("Processed entra-user for {Role}: {Email} (DocumentId: {DocumentId})", 
                                cmdbRole, user.Email, entraUser.DocumentId);
                        }
                        else if (entraUser.Id > 0)
                        {
                            // Fallback to numeric ID if documentId is not available
                            entraUserIds[strapiFieldName] = entraUser.Id;
                            usersProcessed++;
                            _logger.LogInformation("Processed entra-user for {Role}: {Email} (ID: {Id}, DocumentId not available)", 
                                cmdbRole, user.Email, entraUser.Id);
                        }
                        else
                        {
                            _logger.LogWarning("Entra-user for {Role}: {Email} has no valid ID or DocumentId", 
                                cmdbRole, user.Email);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create/update entra-user for {Role}: {Email}", 
                            cmdbRole, user.Email);
                    }
                }
            }

            // Always update product with entra-user relations (even if some are empty to clear them)
            var updateData = new Dictionary<string, object>
            {
                ["fips_id"] = product.FipsId ?? string.Empty
            };

            // Set each role to array with user ID if user exists, or empty array if not
            // Strapi v5 accepts either documentId (string) or numeric id for relations
            foreach (var (cmdbRole, (user, strapiFieldName)) in roleMapping)
            {
                if (entraUserIds.ContainsKey(strapiFieldName))
                {
                    var userId = entraUserIds[strapiFieldName];
                    // Create array with single user ID (can be string documentId or int numeric ID)
                    updateData[strapiFieldName] = new[] { userId };
                    rolesUpdated++;
                }
                else
                {
                    // Clear the relation if no user exists for this role in CMDB
                    updateData[strapiFieldName] = Array.Empty<object>();
                }
            }

            var success = await UpdateProductByDocumentIdAsync(documentId, updateData);
            if (success)
            {
                // Clear cache to ensure fresh data is loaded on next page load
                _cache.Remove($"product_docid_{documentId}");
                if (!string.IsNullOrEmpty(product.FipsId))
                {
                    _cache.Remove($"product_{product.FipsId}");
                }
                _cache.Remove("products_list_all");
                _cache.Remove("products_list_all_states");
                
                if (rolesUpdated > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully synced {rolesUpdated} role(s) from CMDB. " +
                        $"Processed {usersProcessed} user(s).";
                }
                else
                {
                    TempData["InfoMessage"] = "No users found in CMDB for this service offering. Product roles have been cleared.";
                }
                _logger.LogInformation("Successfully synced roles from CMDB for product {DocumentId}", documentId);
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update product with roles from CMDB.";
                _logger.LogError("Failed to update product {DocumentId} with roles from CMDB", documentId);
            }

            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing roles from CMDB for product {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while syncing roles from CMDB. Please try again.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
    }

    // POST: FipsManager/BulkSyncRolesFromCmdb
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkSyncRolesFromCmdb()
    {
        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            _logger.LogWarning("BulkSyncRolesFromCmdb: Access denied for user {UserEmail}", User?.Identity?.Name);
            return StatusCode(403, Json(new { error = "Access denied." }));
        }

        try
        {
            // Get all active products with CMDB sys_id
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            var excludedValues = new[] { "Decommissioned", "Decommissioning" };
            var activeProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.State) 
                    && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase)
                    && !HasCategoryValue(p, "Type", excludedValues)
                    && !HasCategoryValue(p, "Phase", excludedValues)
                    && !string.IsNullOrEmpty(p.CmdbSysId))
                .ToList();

            // Store sync progress in cache
            var syncId = Guid.NewGuid().ToString();
            var syncProgress = new BulkSyncProgress
            {
                Total = activeProducts.Count,
                Current = 0,
                SuccessCount = 0,
                ErrorCount = 0,
                SkippedCount = 0,
                LogEntries = new List<BulkSyncLogEntry>(),
                IsComplete = false
            };
            _cache.Set($"bulk_sync_{syncId}", syncProgress, TimeSpan.FromMinutes(30));

            // Start background sync (fire and forget)
            _ = Task.Run(async () => await ProcessBulkSync(syncId, activeProducts));

            return Json(new { syncId = syncId, total = activeProducts.Count, current = 0, progress = 0, status = "Starting bulk sync...", logEntries = new List<object>(), isComplete = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting bulk CMDB sync");
            return Json(new { error = "An error occurred while starting the sync process." });
        }
    }

    // GET: FipsManager/GetBulkSyncProgress
    [HttpGet]
    public async Task<IActionResult> GetBulkSyncProgress([FromQuery] string syncId)
    {
        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Json(new { error = "Access denied." });
        }

        if (string.IsNullOrEmpty(syncId))
        {
            return Json(new { error = "Sync ID is required." });
        }

        var cacheKey = $"bulk_sync_{syncId}";
        if (!_cache.TryGetValue(cacheKey, out BulkSyncProgress? progress) || progress == null)
        {
            return Json(new { error = "Sync progress not found." });
        }

        var progressPercent = progress.Total > 0 ? (progress.Current / (double)progress.Total) * 100 : 0;
        var status = progress.IsComplete 
            ? $"Sync completed! {progress.SuccessCount} succeeded, {progress.ErrorCount} errors, {progress.SkippedCount} skipped."
            : $"Processing product {progress.Current} of {progress.Total}...";

        return Json(new
        {
            progress = progressPercent,
            total = progress.Total,
            current = progress.Current,
            status = status,
            logEntries = progress.LogEntries.Select(e => new { type = e.Type, message = e.Message }).ToList(),
            isComplete = progress.IsComplete,
            successCount = progress.SuccessCount,
            errorCount = progress.ErrorCount,
            skippedCount = progress.SkippedCount
        });
    }

    private async Task ProcessBulkSync(string syncId, List<ProductDto> products)
    {
        var cacheKey = $"bulk_sync_{syncId}";
        var progress = new BulkSyncProgress
        {
            Total = products.Count,
            Current = 0,
            SuccessCount = 0,
            ErrorCount = 0,
            SkippedCount = 0,
            LogEntries = new List<BulkSyncLogEntry>(),
            IsComplete = false
        };

        foreach (var product in products)
        {
            try
            {
                progress.Current++;
                progress.LogEntries.Add(new BulkSyncLogEntry 
                { 
                    Type = "info", 
                    Message = $"Processing: {product.Title} (CMDB: {product.CmdbSysId})..." 
                });

                if (string.IsNullOrEmpty(product.CmdbSysId) || string.IsNullOrEmpty(product.DocumentId))
                {
                    progress.SkippedCount++;
                    progress.LogEntries.Add(new BulkSyncLogEntry 
                    { 
                        Type = "info", 
                        Message = $"Skipped: {product.Title} - No CMDB sys_id or document ID" 
                    });
                    _cache.Set(cacheKey, progress, TimeSpan.FromMinutes(30));
                    continue;
                }

                // Get service offering from CMDB
                var cmdbEntry = await _cmdbService.GetServiceOfferingBySysIdAsync(product.CmdbSysId);
                if (cmdbEntry == null)
                {
                    progress.ErrorCount++;
                    progress.LogEntries.Add(new BulkSyncLogEntry 
                    { 
                        Type = "error", 
                        Message = $"Error: {product.Title} - CMDB entry not found" 
                    });
                    _cache.Set(cacheKey, progress, TimeSpan.FromMinutes(30));
                    continue;
                }

                // Get users from CMDB
                var cmdbUsers = await _cmdbService.GetServiceOfferingUsersAsync(cmdbEntry);
                
                // Map CMDB roles to Strapi product relation field names
                var roleMapping = new Dictionary<string, (CmdbUser? user, string strapiFieldName)>
                {
                    { "service_owner", (cmdbUsers.ServiceOwner, "service_owner") },
                    { "product_manager", (cmdbUsers.ProductManager, "product_manager") },
                    { "delivery_manager", (cmdbUsers.DeliveryManager, "delivery_manager") },
                    { "information_asset_owner", (cmdbUsers.InformationAssetOwner, "Information_asset_owner") },
                    { "senior_responsible_owner", (cmdbUsers.SeniorResponsibleOwner, "senior_responsible_officer") }
                };

                var entraUserIds = new Dictionary<string, object>();
                var usersProcessed = 0;
                var rolesUpdated = 0;

                // Create/update entra-users for each role that has a user
                foreach (var (cmdbRole, (user, strapiFieldName)) in roleMapping)
                {
                    if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                    {
                        var entraUser = await _productsApiService.GetOrCreateEntraUserAsync(
                            user.Email,
                            user.FederatedId,
                            user.Name,
                            user.FirstName,
                            user.LastName);

                        if (entraUser != null)
                        {
                            if (!string.IsNullOrEmpty(entraUser.DocumentId))
                            {
                                entraUserIds[strapiFieldName] = entraUser.DocumentId;
                                usersProcessed++;
                            }
                            else if (entraUser.Id > 0)
                            {
                                entraUserIds[strapiFieldName] = entraUser.Id;
                                usersProcessed++;
                            }
                        }
                    }
                }

                // Update product with entra-user relations
                var updateData = new Dictionary<string, object>
                {
                    ["fips_id"] = product.FipsId ?? string.Empty
                };

                foreach (var (cmdbRole, (user, strapiFieldName)) in roleMapping)
                {
                    if (entraUserIds.ContainsKey(strapiFieldName))
                    {
                        updateData[strapiFieldName] = new[] { entraUserIds[strapiFieldName] };
                        rolesUpdated++;
                    }
                    else
                    {
                        updateData[strapiFieldName] = Array.Empty<object>();
                    }
                }

                var success = await UpdateProductByDocumentIdAsync(product.DocumentId!, updateData);
                if (success)
                {
                    progress.SuccessCount++;
                    progress.LogEntries.Add(new BulkSyncLogEntry 
                    { 
                        Type = "success", 
                        Message = $"Success: {product.Title} - {rolesUpdated} role(s) updated, {usersProcessed} user(s) processed" 
                    });

                    // Clear cache
                    _cache.Remove($"product_docid_{product.DocumentId}");
                    if (!string.IsNullOrEmpty(product.FipsId))
                    {
                        _cache.Remove($"product_{product.FipsId}");
                    }
                }
                else
                {
                    progress.ErrorCount++;
                    progress.LogEntries.Add(new BulkSyncLogEntry 
                    { 
                        Type = "error", 
                        Message = $"Error: {product.Title} - Failed to update product" 
                    });
                }
            }
            catch (Exception ex)
            {
                progress.ErrorCount++;
                progress.LogEntries.Add(new BulkSyncLogEntry 
                { 
                    Type = "error", 
                    Message = $"Error: {product.Title} - {ex.Message}" 
                });
                _logger.LogError(ex, "Error syncing product {DocumentId} in bulk sync", product.DocumentId);
            }

            _cache.Set(cacheKey, progress, TimeSpan.FromMinutes(30));
        }

        // Mark as complete
        progress.IsComplete = true;
        progress.LogEntries.Add(new BulkSyncLogEntry 
        { 
            Type = "success", 
            Message = $"Bulk sync completed! {progress.SuccessCount} succeeded, {progress.ErrorCount} errors, {progress.SkippedCount} skipped." 
        });
        _cache.Set(cacheKey, progress, TimeSpan.FromMinutes(30));

        // Clear main product list cache
        _cache.Remove("products_list_all");
        _cache.Remove("products_list_all_states");
    }

    // POST: FipsManager/RemoveProductRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProductRole(string documentId, string roleFieldName)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId) || string.IsNullOrWhiteSpace(roleFieldName))
        {
            TempData["ErrorMessage"] = "Product ID and role are required.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }

        try
        {
            // To remove a role, we set it to an empty array
            // This requires a method in ProductsApiService to clear a role
            // For now, we'll use UpdateProductRoleAsync with a special handling
            // Actually, we need to update the product with an empty array for that role
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null || string.IsNullOrEmpty(product.DocumentId))
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDetails), new { documentId });
            }

            // Update with empty array - this will be handled by a new method
            // For now, return a message
            TempData["InfoMessage"] = "Role removal functionality will be implemented.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product role {RoleFieldName} for {DocumentId}", roleFieldName, documentId);
            TempData["ErrorMessage"] = "An error occurred while removing the role. Please try again.";
            return RedirectToAction(nameof(ProductDetails), new { documentId });
        }
    }

    private ProductCompletionItem CreateProductCompletionItem(ProductDto product)
    {
        // Log category values for debugging
        if (product.CategoryValues != null)
        {
            _logger.LogInformation("Product {DocumentId} has {Count} category values", 
                product.DocumentId ?? "unknown", product.CategoryValues.Count);
            foreach (var cv in product.CategoryValues)
            {
                _logger.LogInformation("  - CV: Id={Id}, Name={Name}, CategoryType={CategoryType}", 
                    cv.Id, cv.Name, cv.CategoryType?.Name ?? "NULL");
            }
        }

        // Extract business area - use Trim() to handle whitespace
        var businessArea = product.CategoryValues?
            .FirstOrDefault(cv => cv.CategoryType != null && 
                                 cv.CategoryType.Name?.Trim().Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)?.Name
            ?? "Not assigned";

        // Extract phase - check both product.Phase property and CategoryValues
        var phase = product.CategoryValues?
            .FirstOrDefault(cv => cv.CategoryType != null && 
                                 cv.CategoryType.Name?.Trim().Equals("Phase", StringComparison.OrdinalIgnoreCase) == true);
        var phaseName = !string.IsNullOrEmpty(product.Phase) 
            ? product.Phase 
            : phase?.Name;

        // Extract user groups
        var userGroupNames = new List<string>();
        var userGroupIds = new List<int>();
        var userGroupVariations = new[] { "User group", "User groups", "User Group", "User Groups" };
        
        // Extract channel
        var channelNames = new List<string>();
        var channelVariations = new[] { "Channel", "Channels" };
        
        // Extract type
        var typeNames = new List<string>();
        var typeVariations = new[] { "Type", "Types" };
        
        if (product.CategoryValues != null)
        {
            foreach (var cv in product.CategoryValues)
            {
                if (cv.CategoryType != null && !string.IsNullOrWhiteSpace(cv.CategoryType.Name))
                {
                    var categoryTypeName = cv.CategoryType.Name.Trim();
                    
                    // Check for user groups
                    if (userGroupVariations.Any(v => 
                        categoryTypeName.Equals(v, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!string.IsNullOrWhiteSpace(cv.Name))
                        {
                            userGroupNames.Add(cv.Name);
                            userGroupIds.Add(cv.Id);
                        }
                    }
                    
                    // Check for channel
                    if (channelVariations.Any(v => 
                        categoryTypeName.Equals(v, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!string.IsNullOrWhiteSpace(cv.Name))
                        {
                            channelNames.Add(cv.Name);
                        }
                    }
                    
                    // Check for type
                    if (typeVariations.Any(v => 
                        categoryTypeName.Equals(v, StringComparison.OrdinalIgnoreCase)))
                    {
                        if (!string.IsNullOrWhiteSpace(cv.Name))
                        {
                            typeNames.Add(cv.Name);
                        }
                    }
                }
            }
        }

        // Calculate user groups count - also check directly like other controllers
        var userGroupsCount = product.CategoryValues?
            .Count(cv => cv.CategoryType != null && 
                        !string.IsNullOrWhiteSpace(cv.CategoryType.Name) &&
                        userGroupVariations.Any(v => 
                            cv.CategoryType.Name.Trim().Equals(v, StringComparison.OrdinalIgnoreCase))) ?? 0;

        // Extract contacts
        var contactDetails = new List<string>();
        var sroContacts = new List<string>();
        var iaoContacts = new List<string>();
        var deliveryManagerContacts = new List<string>();
        var serviceOwnerContacts = new List<string>();
        var productManagerContacts = new List<string>();

        // Extract from product roles
        if (product.ServiceOwners != null && product.ServiceOwners.Any())
        {
            foreach (var so in product.ServiceOwners)
            {
                if (so != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(so.DisplayName)
                        ? so.DisplayName
                        : (!string.IsNullOrWhiteSpace(so.FirstName) || !string.IsNullOrWhiteSpace(so.LastName))
                            ? $"{so.FirstName} {so.LastName}".Trim()
                            : so.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        serviceOwnerContacts.Add(displayName);
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.SeniorResponsibleOfficers != null && product.SeniorResponsibleOfficers.Any())
        {
            foreach (var sro in product.SeniorResponsibleOfficers)
            {
                if (sro != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(sro.DisplayName)
                        ? sro.DisplayName
                        : (!string.IsNullOrWhiteSpace(sro.FirstName) || !string.IsNullOrWhiteSpace(sro.LastName))
                            ? $"{sro.FirstName} {sro.LastName}".Trim()
                            : sro.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        sroContacts.Add(displayName);
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.InformationAssetOwners != null && product.InformationAssetOwners.Any())
        {
            foreach (var iao in product.InformationAssetOwners)
            {
                if (iao != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(iao.DisplayName)
                        ? iao.DisplayName
                        : (!string.IsNullOrWhiteSpace(iao.FirstName) || !string.IsNullOrWhiteSpace(iao.LastName))
                            ? $"{iao.FirstName} {iao.LastName}".Trim()
                            : iao.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        iaoContacts.Add(displayName);
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.DeliveryManagers != null && product.DeliveryManagers.Any())
        {
            foreach (var dm in product.DeliveryManagers)
            {
                if (dm != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(dm.DisplayName)
                        ? dm.DisplayName
                        : (!string.IsNullOrWhiteSpace(dm.FirstName) || !string.IsNullOrWhiteSpace(dm.LastName))
                            ? $"{dm.FirstName} {dm.LastName}".Trim()
                            : dm.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        deliveryManagerContacts.Add(displayName);
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.ProductManagers != null && product.ProductManagers.Any())
        {
            foreach (var pm in product.ProductManagers)
            {
                if (pm != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(pm.DisplayName)
                        ? pm.DisplayName
                        : (!string.IsNullOrWhiteSpace(pm.FirstName) || !string.IsNullOrWhiteSpace(pm.LastName))
                            ? $"{pm.FirstName} {pm.LastName}".Trim()
                            : pm.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        productManagerContacts.Add(displayName);
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.ReportingUsers != null && product.ReportingUsers.Any())
        {
            foreach (var ru in product.ReportingUsers)
            {
                if (ru != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(ru.DisplayName)
                        ? ru.DisplayName
                        : (!string.IsNullOrWhiteSpace(ru.FirstName) || !string.IsNullOrWhiteSpace(ru.LastName))
                            ? $"{ru.FirstName} {ru.LastName}".Trim()
                            : ru.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.ServiceDesigns != null && product.ServiceDesigns.Any())
        {
            foreach (var sd in product.ServiceDesigns)
            {
                if (sd != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(sd.DisplayName)
                        ? sd.DisplayName
                        : (!string.IsNullOrWhiteSpace(sd.FirstName) || !string.IsNullOrWhiteSpace(sd.LastName))
                            ? $"{sd.FirstName} {sd.LastName}".Trim()
                            : sd.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        if (product.UserResearchers != null && product.UserResearchers.Any())
        {
            foreach (var ur in product.UserResearchers)
            {
                if (ur != null)
                {
                    var displayName = !string.IsNullOrWhiteSpace(ur.DisplayName)
                        ? ur.DisplayName
                        : (!string.IsNullOrWhiteSpace(ur.FirstName) || !string.IsNullOrWhiteSpace(ur.LastName))
                            ? $"{ur.FirstName} {ur.LastName}".Trim()
                            : ur.EmailAddress;
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        contactDetails.Add(displayName);
                    }
                }
            }
        }

        // Calculate completion - use direct checks like other controllers with Trim() for safety
        var hasPhase = !string.IsNullOrEmpty(product.Phase) ||
                      (product.CategoryValues?.Any(cv => 
                          cv.CategoryType != null &&
                          !string.IsNullOrWhiteSpace(cv.CategoryType.Name) &&
                          cv.CategoryType.Name.Trim().Equals("Phase", StringComparison.OrdinalIgnoreCase) == true) == true);
        var hasBusinessArea = businessArea != "Not assigned" && !string.IsNullOrWhiteSpace(businessArea);
        var contactsCount = contactDetails.Count;
        var hasProductUrl = !string.IsNullOrEmpty(product.ProductUrl);
        
        // Log completion calculation for debugging
        _logger.LogInformation("Product {DocumentId} completion: hasPhase={HasPhase} (Phase={Phase}), hasBusinessArea={HasBusinessArea} (BA={BusinessArea}), contactsCount={ContactsCount}, hasProductUrl={HasProductUrl}, userGroupsCount={UserGroupsCount}", 
            product.DocumentId ?? "unknown", hasPhase, phaseName ?? "null", hasBusinessArea, businessArea, contactsCount, hasProductUrl, userGroupsCount);

        var completedCriteria = 0;
        if (hasPhase) completedCriteria++;
        if (hasBusinessArea) completedCriteria++;
        if (contactsCount > 0) completedCriteria++;
        if (hasProductUrl) completedCriteria++;
        if (userGroupsCount > 0) completedCriteria++;

        var completionPercentage = (completedCriteria / 5.0) * 100;

        return new ProductCompletionItem
        {
            FipsId = product.FipsId ?? string.Empty,
            DocumentId = product.DocumentId ?? string.Empty,
            CmdbSysId = product.CmdbSysId,
            ProductTitle = product.Title,
            BusinessArea = businessArea,
            PhaseName = phaseName,
            State = product.State ?? "New",
            SeniorResponsibleOfficer = sroContacts.Count > 0 ? string.Join(", ", sroContacts) : null,
            InformationAssetOwner = iaoContacts.Count > 0 ? string.Join(", ", iaoContacts) : null,
            DeliveryManager = deliveryManagerContacts.Count > 0 ? string.Join(", ", deliveryManagerContacts) : null,
            ServiceOwner = serviceOwnerContacts.Count > 0 ? string.Join(", ", serviceOwnerContacts) : null,
            SeniorResponsibleOfficerContacts = new List<string>(sroContacts),
            InformationAssetOwnerContacts = new List<string>(iaoContacts),
            DeliveryManagerContacts = new List<string>(deliveryManagerContacts),
            ServiceOwnerContacts = new List<string>(serviceOwnerContacts),
            ProductManagerContacts = new List<string>(productManagerContacts),
            ContactDetails = contactDetails,
            UserGroupNames = userGroupNames,
            UserGroupCategoryValueIds = userGroupIds,
            ChannelNames = channelNames,
            TypeNames = typeNames,
            ProductUrl = product.ProductUrl,
            HasPhase = hasPhase,
            HasBusinessArea = hasBusinessArea,
            ContactsCount = contactsCount,
            HasProductUrl = hasProductUrl,
            UserGroupsCount = userGroupsCount,
            CompletionPercentage = completionPercentage
        };
    }

    private static string GetRoleDisplayName(string roleFieldName)
    {
        return roleFieldName switch
        {
            "service_owner" => "Service Owner",
            "product_manager" => "Product Manager",
            "delivery_manager" => "Delivery Manager",
            "Information_asset_owner" => "Information Asset Owner",
            "reporting_user" => "Reporting User",
            "senior_responsible_officer" => "Senior Responsible Officer",
            "service_designs" => "Service Designs",
            "user_researchers" => "User Researchers",
            _ => roleFieldName
        };
    }

    /// <summary>
    /// Checks if a product has a category value matching any of the excluded values for the specified category type
    /// </summary>
    private static bool HasCategoryValue(ProductDto product, string categoryTypeName, string[] excludedValues)
    {
        if (product.CategoryValues == null || !product.CategoryValues.Any())
        {
            return false;
        }

        return product.CategoryValues
            .Any(cv => cv.CategoryType != null 
                && cv.CategoryType.Name != null
                && cv.CategoryType.Name.Equals(categoryTypeName, StringComparison.OrdinalIgnoreCase)
                && excludedValues.Any(excluded => 
                    cv.Name != null && cv.Name.Equals(excluded, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task<bool> UpdateProductByDocumentIdAsync(string documentId, Dictionary<string, object> updateData)
    {
        try
        {
            var dataObject = new { data = updateData };
            var json = JsonSerializer.Serialize(dataObject);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var writeApiKey = _configuration["CmsApi:WriteApiKey"];
            var baseUrl = _configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";

            using var httpClient = new HttpClient();
            var baseUri = baseUrl.TrimEnd('/');
            if (!baseUri.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                baseUri += "/api";
            }
            httpClient.BaseAddress = new Uri(baseUri + "/");

            if (!string.IsNullOrEmpty(writeApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", writeApiKey);
            }

            var response = await httpClient.PutAsync($"products/{documentId}", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated product {DocumentId}", documentId);
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update product {DocumentId}. Status: {StatusCode}, Error: {Error}, Request JSON: {RequestJson}",
                    documentId, response.StatusCode, errorContent, json);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {DocumentId}", documentId);
            return false;
        }
    }

    // GET: FipsManager/ProductDqManager/MyServices
    [HttpGet]
    [Route("FipsManager/ProductDqManager/MyServices")]
    public async Task<IActionResult> ProductDqManagerMyServices()
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        try
        {
            var userEmail = User?.Identity?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                TempData["ErrorMessage"] = "User email not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Title"] = "Product DQ Manager - My Services";

            // Get products where user is in one of the roles
            var productsByServiceOwner = await _productsApiService.GetProductsByServiceOwnerAsync(userEmail);
            var productsByProductManager = await _productsApiService.GetProductsByProductManagerAsync(userEmail);
            var productsByDeliveryManager = await _productsApiService.GetProductsByDeliveryManagerAsync(userEmail);

            // Get all products and filter by Information Asset Owner and Senior Responsible Officer client-side
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            var productsByInformationAssetOwner = allProducts
                .Where(p => p.InformationAssetOwners != null && 
                           p.InformationAssetOwners.Any(iao => 
                               iao.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();
            var productsBySeniorResponsibleOfficer = allProducts
                .Where(p => p.SeniorResponsibleOfficers != null && 
                           p.SeniorResponsibleOfficers.Any(sro => 
                               sro.EmailAddress?.Equals(userEmail, StringComparison.OrdinalIgnoreCase) == true))
                .ToList();

            // Combine and deduplicate by DocumentId
            var allUserProducts = productsByServiceOwner
                .Concat(productsByProductManager)
                .Concat(productsByDeliveryManager)
                .Concat(productsByInformationAssetOwner)
                .Concat(productsBySeniorResponsibleOfficer)
                .Where(p => !string.IsNullOrEmpty(p.DocumentId))
                .GroupBy(p => p.DocumentId)
                .Select(g => g.First())
                .OrderBy(p => p.Title)
                .ToList();

            // Get DQ reviews for these products
            var documentIds = allUserProducts.Select(p => p.DocumentId!).ToList();
            var dqReviews = await _context.ProductDqReviews
                .Where(r => documentIds.Contains(r.ProductDocumentId))
                .ToListAsync();

            // Create view model with DQ data
            var productsWithDq = allUserProducts.Select(p =>
            {
                var dqScore = CalculateDqScore(p);
                var review = dqReviews.FirstOrDefault(r => r.ProductDocumentId == p.DocumentId);
                
                DateTime nextDue;
                if (review != null && review.NextDueDate != default(DateTime))
                {
                    nextDue = review.NextDueDate;
                }
                else if (review?.LastReviewedDate != null)
                {
                    nextDue = review.LastReviewedDate.AddMonths(6);
                }
                else
                {
                    // Default to 31 Mar 2026 if no review exists
                    nextDue = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
                }

                return new ProductDqListItem
                {
                    Product = p,
                    DqScore = dqScore,
                    LastReviewedDate = review?.LastReviewedDate,
                    NextDueDate = nextDue,
                    HasReviewInProgress = false // TODO: Track in-progress reviews
                };
            }).ToList();

            var viewModel = new ProductDqManagerIndexViewModel
            {
                Products = productsWithDq,
                ViewType = "MyServices"
            };

            return View("ProductDqManager/Index", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Product DQ Manager My Services");
            TempData["ErrorMessage"] = "An error occurred while loading products. Please try again.";
            return View("ProductDqManager/Index", new ProductDqManagerIndexViewModel { ViewType = "MyServices" });
        }
    }

    // GET: FipsManager/ProductDqManager/AllProducts
    [HttpGet]
    [Route("FipsManager/ProductDqManager/AllProducts")]
    public async Task<IActionResult> ProductDqManagerAllProducts()
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        try
        {
            ViewData["Title"] = "Product DQ Manager - All Products";

            // Get all active products
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            var activeProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.State) 
                    && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(p.DocumentId))
                .OrderBy(p => p.Title)
                .ToList();

            // Get DQ reviews for these products
            var documentIds = activeProducts.Select(p => p.DocumentId!).ToList();
            var dqReviews = await _context.ProductDqReviews
                .Where(r => documentIds.Contains(r.ProductDocumentId))
                .ToListAsync();

            // Create view model with DQ data
            var productsWithDq = activeProducts.Select(p =>
            {
                var dqScore = CalculateDqScore(p);
                var review = dqReviews.FirstOrDefault(r => r.ProductDocumentId == p.DocumentId);
                
                DateTime nextDue;
                if (review != null && review.NextDueDate != default(DateTime))
                {
                    nextDue = review.NextDueDate;
                }
                else if (review?.LastReviewedDate != null)
                {
                    nextDue = review.LastReviewedDate.AddMonths(6);
                }
                else
                {
                    // Default to 31 Mar 2026 if no review exists
                    nextDue = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
                }

                return new ProductDqListItem
                {
                    Product = p,
                    DqScore = dqScore,
                    LastReviewedDate = review?.LastReviewedDate,
                    NextDueDate = nextDue,
                    HasReviewInProgress = false // TODO: Track in-progress reviews
                };
            }).ToList();

            var viewModel = new ProductDqManagerIndexViewModel
            {
                Products = productsWithDq,
                ViewType = "AllProducts"
            };

            return View("ProductDqManager/Index", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Product DQ Manager All Products");
            TempData["ErrorMessage"] = "An error occurred while loading products. Please try again.";
            return View("ProductDqManager/Index", new ProductDqManagerIndexViewModel { ViewType = "AllProducts" });
        }
    }

    // GET: FipsManager/ProductDqManager/StartReview/{documentId}
    [HttpGet]
    [Route("FipsManager/ProductDqManager/StartReview/{documentId}")]
    public async Task<IActionResult> StartDqReview(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }

        try
        {
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            // Store review in session to track in-progress reviews
            HttpContext.Session.SetString($"DqReview_{documentId}", DateTime.UtcNow.ToString("O"));

            return RedirectToAction(nameof(DqReviewInterstitial), new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting DQ review for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while starting the review. Please try again.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
    }

    // GET: FipsManager/ProductDqManager/ReviewInterstitial/{documentId}
    [HttpGet]
    [Route("FipsManager/ProductDqManager/ReviewInterstitial/{documentId}")]
    public async Task<IActionResult> DqReviewInterstitial(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }

        try
        {
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            var viewModel = new DqReviewInterstitialViewModel
            {
                Product = product,
                DocumentId = documentId,
                CurrentStep = 1,
                TotalSteps = 3
            };

            ViewData["Title"] = $"Product DQ Review - {product.Title}";
            return View("ProductDqManager/ReviewInterstitial", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DQ review interstitial for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while loading the review. Please try again.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
    }

    // GET: FipsManager/ProductDqManager/ReviewCheck/{documentId}
    [HttpGet]
    [Route("FipsManager/ProductDqManager/ReviewCheck/{documentId}")]
    public async Task<IActionResult> DqReviewCheck(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }

        try
        {
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            // Get all category values grouped by type
            var categoryTypes = await _productsApiService.GetAllCategoryValuesByTypeAsync();

            // Get current category values for the product
            var currentCategoryValueIds = product.CategoryValues?
                .Select(cv => cv.Id)
                .ToList() ?? new List<int>();

            var viewModel = new DqReviewCheckViewModel
            {
                Product = product,
                DocumentId = documentId,
                CategoryTypes = categoryTypes,
                CurrentCategoryValueIds = currentCategoryValueIds,
                LongDescription = product.LongDescription,
                ProductUrl = product.ProductUrl,
                CurrentStep = 2,
                TotalSteps = 3
            };

            ViewData["Title"] = $"Product DQ Review - {product?.Title ?? "Unknown Product"}";
            return View("ProductDqManager/ReviewCheck", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DQ review check for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while loading the review. Please try again.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
    }

    // POST: FipsManager/ProductDqManager/ReviewCheck/{documentId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("FipsManager/ProductDqManager/ReviewCheck/{documentId}")]
    public async Task<IActionResult> DqReviewCheck(string documentId, DqReviewCheckViewModel model)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }

        try
        {
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            // Process contact changes from form
            // Contact changes come as JSON strings in hidden inputs
            var serviceOwnerChanges = new List<ContactChange>();
            var formServiceOwnerChanges = Request.Form["ServiceOwnerChanges"];
            if (!string.IsNullOrEmpty(formServiceOwnerChanges))
            {
                foreach (var changeJson in formServiceOwnerChanges)
                {
                    if (!string.IsNullOrWhiteSpace(changeJson))
                    {
                        try
                        {
                            var change = JsonSerializer.Deserialize<ContactChange>(changeJson);
                            if (change != null)
                            {
                                serviceOwnerChanges.Add(change);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse ServiceOwner change: {ChangeJson}", changeJson);
                        }
                    }
                }
            }

            // Validate: At least 1 active Service Owner (not marked for removal)
            var activeServiceOwners = serviceOwnerChanges.Where(c => c.Action != "remove").ToList();
            if (!activeServiceOwners.Any())
            {
                // Check if product has existing service owners that aren't being removed
                if (product.ServiceOwners == null || !product.ServiceOwners.Any())
                {
                    ModelState.AddModelError("ServiceOwnerChanges", "At least one Service Owner is required.");
                }
                else
                {
                    // Check if all existing service owners are being removed
                    var existingEmails = product.ServiceOwners.Select(so => so.EmailAddress?.ToLowerInvariant()).Where(e => !string.IsNullOrEmpty(e)).ToList();
                    var removedEmails = serviceOwnerChanges.Where(c => c.Action == "remove").Select(c => c.Email?.ToLowerInvariant()).Where(e => !string.IsNullOrEmpty(e)).ToList();
                    if (existingEmails.All(e => removedEmails.Contains(e)))
                    {
                        ModelState.AddModelError("ServiceOwnerChanges", "At least one Service Owner is required.");
                    }
                }
            }

            // Process other contact changes
            var sroChanges = ProcessContactChanges(Request.Form["SeniorResponsibleOfficerChanges"]);
            var iaoChanges = ProcessContactChanges(Request.Form["InformationAssetOwnerChanges"]);
            var dmChanges = ProcessContactChanges(Request.Form["DeliveryManagerChanges"]);
            var pmChanges = ProcessContactChanges(Request.Form["ProductManagerChanges"]);

            model.ServiceOwnerChanges = serviceOwnerChanges;
            model.SeniorResponsibleOfficerChanges = sroChanges;
            model.InformationAssetOwnerChanges = iaoChanges;
            model.DeliveryManagerChanges = dmChanges;
            model.ProductManagerChanges = pmChanges;

            // Store changes in session for confirmation page
            // Only track changes for fields that can be updated in CMS:
            // - long_description
            // - product_url
            // - category_values
            var changes = new Dictionary<string, object>();
            if (model.LongDescription != product.LongDescription)
            {
                changes["Long Description"] = new { Old = product.LongDescription ?? "-", New = model.LongDescription ?? "-" };
            }
            if (model.ProductUrl != product.ProductUrl)
            {
                changes["Product URL"] = new { Old = product.ProductUrl ?? "-", New = model.ProductUrl ?? "-" };
            }
            
            // Check category value changes
            var currentCategoryValueIds = product.CategoryValues?.Select(cv => cv.Id).ToList() ?? new List<int>();
            var newCategoryValueIds = model.CategoryValueIds ?? new List<int>();
            if (!currentCategoryValueIds.SequenceEqual(newCategoryValueIds))
            {
                var currentNames = product.CategoryValues?.Select(cv => cv.Name).ToList() ?? new List<string>();
                // Get new category value names from the model's category types
                var newNames = new List<string>();
                if (model.CategoryTypes != null && newCategoryValueIds.Any())
                {
                    foreach (var typeGroup in model.CategoryTypes.Values)
                    {
                        foreach (var cv in typeGroup.Where(cv => newCategoryValueIds.Contains(cv.Id)))
                        {
                            newNames.Add(cv.Name);
                        }
                    }
                }
                changes["Category Values"] = new { Old = string.Join(", ", currentNames), New = string.Join(", ", newNames) };
            }

            // Track contact changes (for display in confirmation, but not updated in CMS)
            var contactChangesList = new List<string>();
            if (model.ServiceOwnerChanges != null && model.ServiceOwnerChanges.Any(c => c.Action != "keep"))
            {
                var adds = model.ServiceOwnerChanges.Where(c => c.Action == "add").Select(c => $"Add: {c.DisplayName ?? c.Email}").ToList();
                var removes = model.ServiceOwnerChanges.Where(c => c.Action == "remove").Select(c => $"Remove: {c.DisplayName ?? c.Email}").ToList();
                if (adds.Any() || removes.Any())
                {
                    contactChangesList.Add($"Service Owner: {string.Join(", ", adds.Concat(removes))}");
                }
            }
            if (model.SeniorResponsibleOfficerChanges != null && model.SeniorResponsibleOfficerChanges.Any(c => c.Action != "keep"))
            {
                var adds = model.SeniorResponsibleOfficerChanges.Where(c => c.Action == "add").Select(c => $"Add: {c.DisplayName ?? c.Email}").ToList();
                var removes = model.SeniorResponsibleOfficerChanges.Where(c => c.Action == "remove").Select(c => $"Remove: {c.DisplayName ?? c.Email}").ToList();
                if (adds.Any() || removes.Any())
                {
                    contactChangesList.Add($"SRO: {string.Join(", ", adds.Concat(removes))}");
                }
            }
            if (model.InformationAssetOwnerChanges != null && model.InformationAssetOwnerChanges.Any(c => c.Action != "keep"))
            {
                var adds = model.InformationAssetOwnerChanges.Where(c => c.Action == "add").Select(c => $"Add: {c.DisplayName ?? c.Email}").ToList();
                var removes = model.InformationAssetOwnerChanges.Where(c => c.Action == "remove").Select(c => $"Remove: {c.DisplayName ?? c.Email}").ToList();
                if (adds.Any() || removes.Any())
                {
                    contactChangesList.Add($"IAO: {string.Join(", ", adds.Concat(removes))}");
                }
            }
            if (model.DeliveryManagerChanges != null && model.DeliveryManagerChanges.Any(c => c.Action != "keep"))
            {
                var adds = model.DeliveryManagerChanges.Where(c => c.Action == "add").Select(c => $"Add: {c.DisplayName ?? c.Email}").ToList();
                var removes = model.DeliveryManagerChanges.Where(c => c.Action == "remove").Select(c => $"Remove: {c.DisplayName ?? c.Email}").ToList();
                if (adds.Any() || removes.Any())
                {
                    contactChangesList.Add($"DM: {string.Join(", ", adds.Concat(removes))}");
                }
            }
            if (model.ProductManagerChanges != null && model.ProductManagerChanges.Any(c => c.Action != "keep"))
            {
                var adds = model.ProductManagerChanges.Where(c => c.Action == "add").Select(c => $"Add: {c.DisplayName ?? c.Email}").ToList();
                var removes = model.ProductManagerChanges.Where(c => c.Action == "remove").Select(c => $"Remove: {c.DisplayName ?? c.Email}").ToList();
                if (adds.Any() || removes.Any())
                {
                    contactChangesList.Add($"PM: {string.Join(", ", adds.Concat(removes))}");
                }
            }

            if (contactChangesList.Any())
            {
                changes["Contacts (offline)"] = new { Old = "Current contacts", New = string.Join("; ", contactChangesList) };
            }

            // Store in session
            var changesJson = JsonSerializer.Serialize(changes);
            HttpContext.Session.SetString($"DqReviewChanges_{documentId}", changesJson);
            
            // Serialize model with proper options to handle ContactChange objects
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            HttpContext.Session.SetString($"DqReviewData_{documentId}", JsonSerializer.Serialize(model, options));
            
            // Debug logging
            _logger.LogInformation("DqReviewCheck POST - Stored changes: {ChangesJson}", changesJson);
            _logger.LogInformation("DqReviewCheck POST - Contact changes list count: {Count}", contactChangesList.Count);

            // Reload product and category types for the view (needed for validation errors)
            model.Product = product;
            var categoryTypes = await _productsApiService.GetAllCategoryValuesByTypeAsync();
            model.CategoryTypes = categoryTypes;
            model.CurrentCategoryValueIds = model.CategoryValueIds ?? new List<int>();
            model.CurrentStep = 2;
            model.TotalSteps = 3;

            if (!ModelState.IsValid)
            {
                return View("ProductDqManager/ReviewCheck", model);
            }

            return RedirectToAction(nameof(DqReviewConfirmation), new { documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing DQ review check for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while processing the review. Please try again.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
    }

    // GET: FipsManager/ProductDqManager/ReviewConfirmation/{documentId}
    [HttpGet]
    [Route("FipsManager/ProductDqManager/ReviewConfirmation/{documentId}")]
    public async Task<IActionResult> DqReviewConfirmation(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }

        try
        {
            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            // Get stored review data from session to show what will change
            var reviewDataJson = HttpContext.Session.GetString($"DqReviewData_{documentId}");
            var reviewData = string.IsNullOrEmpty(reviewDataJson) 
                ? null 
                : JsonSerializer.Deserialize<DqReviewCheckViewModel>(reviewDataJson);
            
            // Also try to get pre-built changes from session (from POST action)
            var storedChangesJson = HttpContext.Session.GetString($"DqReviewChanges_{documentId}");
            Dictionary<string, object>? storedChanges = null;
            if (!string.IsNullOrEmpty(storedChangesJson))
            {
                try
                {
                    // Deserialize as JsonElement first, then convert to Dictionary<string, object>
                    var storedChangesElement = JsonSerializer.Deserialize<JsonElement>(storedChangesJson);
                    storedChanges = new Dictionary<string, object>();
                    
                    foreach (var prop in storedChangesElement.EnumerateObject())
                    {
                        storedChanges[prop.Name] = prop.Value;
                    }
                    
                    _logger.LogInformation("ReviewConfirmation - Loaded stored changes with {Count} items: {Keys}", 
                        storedChanges.Count, string.Join(", ", storedChanges.Keys));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize stored changes from session: {Error}", ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("ReviewConfirmation - No stored changes found in session");
            }
            
            // Build changes dictionary for display
            // Only track changes for fields that can be updated in CMS:
            // - long_description
            // - product_url
            // - category_values
            var changes = new Dictionary<string, object>();
            if (reviewData != null)
            {
                // Only add Long Description if it actually changed
                var oldLongDesc = product.LongDescription?.Trim() ?? "";
                var newLongDesc = reviewData.LongDescription?.Trim() ?? "";
                if (oldLongDesc != newLongDesc)
                {
                    changes["Long Description"] = new { 
                        Old = string.IsNullOrWhiteSpace(oldLongDesc) ? "(empty)" : oldLongDesc, 
                        New = string.IsNullOrWhiteSpace(newLongDesc) ? "(empty)" : newLongDesc 
                    };
                }
                
                // Only add Product URL if it actually changed
                var oldProductUrl = product.ProductUrl?.Trim() ?? "";
                var newProductUrl = reviewData.ProductUrl?.Trim() ?? "";
                if (oldProductUrl != newProductUrl)
                {
                    changes["Product URL"] = new { 
                        Old = string.IsNullOrWhiteSpace(oldProductUrl) ? "(empty)" : oldProductUrl, 
                        New = string.IsNullOrWhiteSpace(newProductUrl) ? "(empty)" : newProductUrl 
                    };
                }
                
                // Check category value changes
                var currentCategoryValueIds = product.CategoryValues?.Select(cv => cv.Id).ToList() ?? new List<int>();
                var newCategoryValueIds = reviewData.CategoryValueIds ?? new List<int>();
                if (!currentCategoryValueIds.SequenceEqual(newCategoryValueIds))
                {
                    var currentNames = product.CategoryValues?.Select(cv => cv.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
                    
                    // Get new category value names
                    var allCategoryValuesByType = await _productsApiService.GetAllCategoryValuesByTypeAsync();
                    var newNames = new List<string>();
                    foreach (var categoryTypeGroup in allCategoryValuesByType.Values)
                    {
                        foreach (var cv in categoryTypeGroup)
                        {
                            if (newCategoryValueIds.Contains(cv.Id) && !string.IsNullOrWhiteSpace(cv.Name))
                            {
                                newNames.Add(cv.Name);
                            }
                        }
                    }
                    
                    changes["Category Values"] = new { 
                        Old = currentNames.Any() ? string.Join(", ", currentNames) : "(none)", 
                        New = newNames.Any() ? string.Join(", ", newNames) : "(none)" 
                    };
                }

                // Add contact changes to display (but note they're not updated in CMS)
                var contactChangesList = new List<string>();
                
                // Debug logging
                _logger.LogInformation("ReviewConfirmation - reviewData is null: {IsNull}", reviewData == null);
                if (reviewData != null)
                {
                    _logger.LogInformation("ReviewConfirmation - SRO Changes: {IsNull}, Count: {Count}", 
                        reviewData.SeniorResponsibleOfficerChanges == null,
                        reviewData.SeniorResponsibleOfficerChanges?.Count ?? 0);
                    if (reviewData.SeniorResponsibleOfficerChanges != null)
                    {
                        foreach (var change in reviewData.SeniorResponsibleOfficerChanges)
                        {
                            _logger.LogInformation("ReviewConfirmation - SRO Change: Action={Action}, Email={Email}, DisplayName={DisplayName}", 
                                change.Action, change.Email, change.DisplayName);
                        }
                    }
                }
                
                if (reviewData?.ServiceOwnerChanges != null && reviewData.ServiceOwnerChanges.Any(c => c.Action != "keep"))
                {
                    var soChanges = reviewData.ServiceOwnerChanges.Where(c => c.Action != "keep")
                        .Select(c => $"{c.Action.ToUpperInvariant()}: {c.DisplayName ?? c.Email}").ToList();
                    if (soChanges.Any())
                    {
                        contactChangesList.Add($"Service Owner: {string.Join(", ", soChanges)}");
                    }
                }
                if (reviewData?.SeniorResponsibleOfficerChanges != null && reviewData.SeniorResponsibleOfficerChanges.Any(c => c.Action != "keep"))
                {
                    var sroChanges = reviewData.SeniorResponsibleOfficerChanges.Where(c => c.Action != "keep")
                        .Select(c => $"{c.Action.ToUpperInvariant()}: {c.DisplayName ?? c.Email}").ToList();
                    if (sroChanges.Any())
                    {
                        contactChangesList.Add($"SRO: {string.Join(", ", sroChanges)}");
                    }
                }
                if (reviewData?.InformationAssetOwnerChanges != null && reviewData.InformationAssetOwnerChanges.Any(c => c.Action != "keep"))
                {
                    var iaoChanges = reviewData.InformationAssetOwnerChanges.Where(c => c.Action != "keep")
                        .Select(c => $"{c.Action.ToUpperInvariant()}: {c.DisplayName ?? c.Email}").ToList();
                    if (iaoChanges.Any())
                    {
                        contactChangesList.Add($"IAO: {string.Join(", ", iaoChanges)}");
                    }
                }
                if (reviewData?.DeliveryManagerChanges != null && reviewData.DeliveryManagerChanges.Any(c => c.Action != "keep"))
                {
                    var dmChanges = reviewData.DeliveryManagerChanges.Where(c => c.Action != "keep")
                        .Select(c => $"{c.Action.ToUpperInvariant()}: {c.DisplayName ?? c.Email}").ToList();
                    if (dmChanges.Any())
                    {
                        contactChangesList.Add($"DM: {string.Join(", ", dmChanges)}");
                    }
                }
                if (reviewData?.ProductManagerChanges != null && reviewData.ProductManagerChanges.Any(c => c.Action != "keep"))
                {
                    var pmChanges = reviewData.ProductManagerChanges.Where(c => c.Action != "keep")
                        .Select(c => $"{c.Action.ToUpperInvariant()}: {c.DisplayName ?? c.Email}").ToList();
                    if (pmChanges.Any())
                    {
                        contactChangesList.Add($"PM: {string.Join(", ", pmChanges)}");
                    }
                }

                if (contactChangesList.Any())
                {
                    changes["Contacts (offline)"] = new { Old = "Current contacts", New = string.Join("; ", contactChangesList) };
                    _logger.LogInformation("ReviewConfirmation - Added contact changes to changes dict: {Changes}", string.Join("; ", contactChangesList));
                }
                else
                {
                    _logger.LogInformation("ReviewConfirmation - No contact changes to add (contactChangesList is empty)");
                }
            }

            // Use stored changes if available, otherwise use newly built changes
            var finalChanges = storedChanges ?? changes;
            
            var viewModel = new DqReviewConfirmationViewModel
            {
                Product = product,
                DocumentId = documentId,
                Changes = finalChanges,
                CurrentStep = 3,
                TotalSteps = 3
            };
            
            // Debug logging
            _logger.LogInformation("ReviewConfirmation - Final changes count: {Count}, Keys: {Keys}", 
                finalChanges.Count,
                string.Join(", ", finalChanges.Keys));

            ViewData["Title"] = $"Product DQ Review - {product.Title}";
            return View("ProductDqManager/ReviewConfirmation", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading DQ review confirmation for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while loading the review. Please try again.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
    }

    // POST: FipsManager/ProductDqManager/SubmitReview/{documentId}
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("FipsManager/ProductDqManager/SubmitReview/{documentId}")]
    public async Task<IActionResult> SubmitDqReview(string documentId)
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        // Check user has access to FIPS Manager
        if (!await HasFipsManagerAccessAsync())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            TempData["ErrorMessage"] = "Product ID is required.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }

        try
        {
            var userEmail = User?.Identity?.Name ?? string.Empty;
            var userName = User?.Identity?.Name ?? "Unknown";

            var product = await _productsApiService.GetProductByDocumentIdAsync(documentId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            // Get stored review data from session
            var reviewDataJson = HttpContext.Session.GetString($"DqReviewData_{documentId}");
            if (string.IsNullOrEmpty(reviewDataJson))
            {
                TempData["ErrorMessage"] = "Review data not found. Please start the review again.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            var reviewData = JsonSerializer.Deserialize<DqReviewCheckViewModel>(reviewDataJson);
            if (reviewData == null)
            {
                TempData["ErrorMessage"] = "Invalid review data. Please start the review again.";
                return RedirectToAction(nameof(ProductDqManagerAllProducts));
            }

            var changesList = new List<string>();

            // CRITICAL: Preserve ALL existing fields when updating
            // Strapi PUT requests replace the entire record, so we must include all fields
            var updateData = new Dictionary<string, object>
            {
                ["fips_id"] = product.FipsId ?? string.Empty,
                ["title"] = product.Title ?? string.Empty,
                ["short_description"] = product.ShortDescription ?? string.Empty,
                ["state"] = product.State ?? "Active",
                ["cmdb_sys_id"] = product.CmdbSysId ?? string.Empty
            };

            // Update long_description if changed, otherwise preserve existing
            if (reviewData.LongDescription != product.LongDescription)
            {
                updateData["long_description"] = reviewData.LongDescription?.Trim() ?? string.Empty;
                changesList.Add("Long description updated");
            }
            else
            {
                updateData["long_description"] = product.LongDescription ?? string.Empty;
            }

            // Update product_url if changed, otherwise preserve existing
            if (reviewData.ProductUrl != product.ProductUrl)
            {
                updateData["product_url"] = reviewData.ProductUrl?.Trim() ?? string.Empty;
                changesList.Add("Product URL updated");
            }
            else
            {
                updateData["product_url"] = product.ProductUrl ?? string.Empty;
            }

            // Update category values if changed, otherwise preserve existing
            var currentCategoryValueIds = product.CategoryValues?.Select(cv => cv.Id).ToList() ?? new List<int>();
            var newCategoryValueIds = reviewData.CategoryValueIds ?? new List<int>();
            if (!currentCategoryValueIds.SequenceEqual(newCategoryValueIds))
            {
                updateData["category_values"] = newCategoryValueIds;
                changesList.Add("Category values updated");
            }
            else
            {
                // Preserve existing category values
                updateData["category_values"] = currentCategoryValueIds;
            }

            // Update product in CMS (only if there are actual changes)
            if (changesList.Any())
            {
                var success = await UpdateProductByDocumentIdAsync(documentId, updateData);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to update product. Please try again.";
                    return RedirectToAction(nameof(ProductDqManagerAllProducts));
                }
            }

            // Note: Contacts are NOT updated in the CMS - they are only displayed for review/validation

            // Process and store contact changes (for offline management)
            var contactChangesSummary = new List<string>();
            if (reviewData.ServiceOwnerChanges != null && reviewData.ServiceOwnerChanges.Any(c => c.Action != "keep"))
            {
                var soChanges = reviewData.ServiceOwnerChanges.Where(c => c.Action != "keep")
                    .Select(c => $"{c.Action}: {c.DisplayName ?? c.Email}").ToList();
                contactChangesSummary.Add($"Service Owner: {string.Join(", ", soChanges)}");
            }
            if (reviewData.SeniorResponsibleOfficerChanges != null && reviewData.SeniorResponsibleOfficerChanges.Any(c => c.Action != "keep"))
            {
                var sroChanges = reviewData.SeniorResponsibleOfficerChanges.Where(c => c.Action != "keep")
                    .Select(c => $"{c.Action}: {c.DisplayName ?? c.Email}").ToList();
                contactChangesSummary.Add($"SRO: {string.Join(", ", sroChanges)}");
            }
            if (reviewData.InformationAssetOwnerChanges != null && reviewData.InformationAssetOwnerChanges.Any(c => c.Action != "keep"))
            {
                var iaoChanges = reviewData.InformationAssetOwnerChanges.Where(c => c.Action != "keep")
                    .Select(c => $"{c.Action}: {c.DisplayName ?? c.Email}").ToList();
                contactChangesSummary.Add($"IAO: {string.Join(", ", iaoChanges)}");
            }
            if (reviewData.DeliveryManagerChanges != null && reviewData.DeliveryManagerChanges.Any(c => c.Action != "keep"))
            {
                var dmChanges = reviewData.DeliveryManagerChanges.Where(c => c.Action != "keep")
                    .Select(c => $"{c.Action}: {c.DisplayName ?? c.Email}").ToList();
                contactChangesSummary.Add($"DM: {string.Join(", ", dmChanges)}");
            }
            if (reviewData.ProductManagerChanges != null && reviewData.ProductManagerChanges.Any(c => c.Action != "keep"))
            {
                var pmChanges = reviewData.ProductManagerChanges.Where(c => c.Action != "keep")
                    .Select(c => $"{c.Action}: {c.DisplayName ?? c.Email}").ToList();
                contactChangesSummary.Add($"PM: {string.Join(", ", pmChanges)}");
            }

            // Store contact changes as JSON for offline management
            var allContactChanges = new
            {
                ServiceOwner = reviewData.ServiceOwnerChanges ?? new List<ContactChange>(),
                SeniorResponsibleOfficer = reviewData.SeniorResponsibleOfficerChanges ?? new List<ContactChange>(),
                InformationAssetOwner = reviewData.InformationAssetOwnerChanges ?? new List<ContactChange>(),
                DeliveryManager = reviewData.DeliveryManagerChanges ?? new List<ContactChange>(),
                ProductManager = reviewData.ProductManagerChanges ?? new List<ContactChange>()
            };
            var contactChangesJson = JsonSerializer.Serialize(allContactChanges);

            // Save DQ review record
            var now = DateTime.UtcNow;
            var nextDue = now.AddMonths(6);

            // Build changes summary
            var allChanges = new List<string>(changesList);
            if (contactChangesSummary.Any())
            {
                allChanges.Add($"Contact changes (offline): {string.Join("; ", contactChangesSummary)}");
            }

            var dqReview = new ProductDqReview
            {
                ProductDocumentId = documentId,
                ProductFipsId = product.FipsId,
                LastReviewedDate = now,
                NextDueDate = nextDue,
                ReviewedByEmail = userEmail,
                ReviewedByName = userName,
                ChangesMade = allChanges.Any() ? string.Join("; ", allChanges) : "No changes made",
                ContactChangesJson = contactChangesJson
            };

            // Send email notification if there are contact changes
            if (contactChangesSummary.Any())
            {
                try
                {
                    var templateId = _configuration["GovUkNotify:ContactChangeTemplateId"];
                    var recipientEmail = _configuration["GovUkNotify:ContactChangeRecipientEmail"];

                    if (!string.IsNullOrWhiteSpace(templateId) && !string.IsNullOrWhiteSpace(recipientEmail))
                    {
                        // Format contact changes for email
                        var formattedChanges = new List<string>();
                        if (reviewData.ServiceOwnerChanges != null)
                        {
                            foreach (var change in reviewData.ServiceOwnerChanges.Where(c => c.Action != "keep"))
                            {
                                var action = change.Action.ToUpperInvariant();
                                var name = change.DisplayName ?? change.Email;
                                formattedChanges.Add($"{name} (Service Owner) - {action}");
                            }
                        }
                        if (reviewData.SeniorResponsibleOfficerChanges != null)
                        {
                            foreach (var change in reviewData.SeniorResponsibleOfficerChanges.Where(c => c.Action != "keep"))
                            {
                                var action = change.Action.ToUpperInvariant();
                                var name = change.DisplayName ?? change.Email;
                                formattedChanges.Add($"{name} (SRO) - {action}");
                            }
                        }
                        if (reviewData.InformationAssetOwnerChanges != null)
                        {
                            foreach (var change in reviewData.InformationAssetOwnerChanges.Where(c => c.Action != "keep"))
                            {
                                var action = change.Action.ToUpperInvariant();
                                var name = change.DisplayName ?? change.Email;
                                formattedChanges.Add($"{name} (IAO) - {action}");
                            }
                        }
                        if (reviewData.DeliveryManagerChanges != null)
                        {
                            foreach (var change in reviewData.DeliveryManagerChanges.Where(c => c.Action != "keep"))
                            {
                                var action = change.Action.ToUpperInvariant();
                                var name = change.DisplayName ?? change.Email;
                                formattedChanges.Add($"{name} (Delivery Manager) - {action}");
                            }
                        }
                        if (reviewData.ProductManagerChanges != null)
                        {
                            foreach (var change in reviewData.ProductManagerChanges.Where(c => c.Action != "keep"))
                            {
                                var action = change.Action.ToUpperInvariant();
                                var name = change.DisplayName ?? change.Email;
                                formattedChanges.Add($"{name} (Product Manager) - {action}");
                            }
                        }

                        var personalisation = new Dictionary<string, dynamic>
                        {
                            { "product", product.Title ?? product.FipsId ?? "Unknown Product" },
                            { "changes", string.Join("\n", formattedChanges) }
                        };

                        var notificationResult = await _notificationService.SendEmailWithTemplateAsync(
                            recipientEmail,
                            templateId,
                            personalisation,
                            triggerCode: "dq_review_contact_change",
                            contextData: new Dictionary<string, object>
                            {
                                { "productDocumentId", documentId },
                                { "productFipsId", product.FipsId ?? "" },
                                { "reviewedBy", userEmail }
                            });

                        if (notificationResult.Success)
                        {
                            _logger.LogInformation(
                                "Contact change notification sent successfully for product {ProductTitle} (DocumentId: {DocumentId})",
                                product.Title,
                                documentId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to send contact change notification for product {ProductTitle} (DocumentId: {DocumentId}): {Error}",
                                product.Title,
                                documentId,
                                notificationResult.ErrorMessage);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Contact change notification not sent - template ID or recipient email not configured. TemplateId: {TemplateId}, RecipientEmail: {RecipientEmail}",
                            templateId ?? "not set",
                            recipientEmail ?? "not set");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the review submission
                    _logger.LogError(
                        ex,
                        "Error sending contact change notification for product {ProductTitle} (DocumentId: {DocumentId})",
                        product.Title,
                        documentId);
                }
            }

            _context.ProductDqReviews.Add(dqReview);
            await _context.SaveChangesAsync();

            // Send confirmation email to the reviewer with all changes
            try
            {
                var confirmationTemplateId = _configuration["GovUkNotify:ReviewConfirmationTemplateId"];

                if (!string.IsNullOrWhiteSpace(confirmationTemplateId) && !string.IsNullOrWhiteSpace(userEmail))
                {
                    // Format all changes for the reviewer email
                    var allChangesFormatted = new List<string>();

                    // CMS changes
                    if (reviewData.LongDescription != product.LongDescription)
                    {
                        allChangesFormatted.Add("Long Description - UPDATED");
                    }
                    if (reviewData.ProductUrl != product.ProductUrl)
                    {
                        allChangesFormatted.Add("Product URL - UPDATED");
                    }
                    if (!currentCategoryValueIds.SequenceEqual(newCategoryValueIds))
                    {
                        var oldNames = product.CategoryValues?.Select(cv => cv.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList() ?? new List<string>();
                        var newNames = new List<string>();
                        if (reviewData.CategoryValueIds != null && reviewData.CategoryValueIds.Any())
                        {
                            // Get all category values to find names for new IDs
                            var allCategoryValuesByType = await _productsApiService.GetAllCategoryValuesByTypeAsync();
                            foreach (var categoryTypeGroup in allCategoryValuesByType.Values)
                            {
                                foreach (var cv in categoryTypeGroup)
                                {
                                    if (reviewData.CategoryValueIds.Contains(cv.Id) && !string.IsNullOrWhiteSpace(cv.Name))
                                    {
                                        newNames.Add(cv.Name);
                                    }
                                }
                            }
                        }
                        var removed = oldNames.Where(n => !newNames.Contains(n)).ToList();
                        var added = newNames.Where(n => !oldNames.Contains(n)).ToList();
                        if (removed.Any())
                        {
                            allChangesFormatted.Add($"Category Values - REMOVED: {string.Join(", ", removed)}");
                        }
                        if (added.Any())
                        {
                            allChangesFormatted.Add($"Category Values - ADDED: {string.Join(", ", added)}");
                        }
                        if (!removed.Any() && !added.Any() && oldNames.Any())
                        {
                            // Values were reordered or changed but same items
                            allChangesFormatted.Add("Category Values - UPDATED");
                        }
                    }

                    // Contact changes
                    if (reviewData.ServiceOwnerChanges != null)
                    {
                        foreach (var change in reviewData.ServiceOwnerChanges.Where(c => c.Action != "keep"))
                        {
                            var action = change.Action.ToUpperInvariant();
                            var name = change.DisplayName ?? change.Email;
                            allChangesFormatted.Add($"{name} (Service Owner) - {action}");
                        }
                    }
                    if (reviewData.SeniorResponsibleOfficerChanges != null)
                    {
                        foreach (var change in reviewData.SeniorResponsibleOfficerChanges.Where(c => c.Action != "keep"))
                        {
                            var action = change.Action.ToUpperInvariant();
                            var name = change.DisplayName ?? change.Email;
                            allChangesFormatted.Add($"{name} (SRO) - {action}");
                        }
                    }
                    if (reviewData.InformationAssetOwnerChanges != null)
                    {
                        foreach (var change in reviewData.InformationAssetOwnerChanges.Where(c => c.Action != "keep"))
                        {
                            var action = change.Action.ToUpperInvariant();
                            var name = change.DisplayName ?? change.Email;
                            allChangesFormatted.Add($"{name} (IAO) - {action}");
                        }
                    }
                    if (reviewData.DeliveryManagerChanges != null)
                    {
                        foreach (var change in reviewData.DeliveryManagerChanges.Where(c => c.Action != "keep"))
                        {
                            var action = change.Action.ToUpperInvariant();
                            var name = change.DisplayName ?? change.Email;
                            allChangesFormatted.Add($"{name} (Delivery Manager) - {action}");
                        }
                    }
                    if (reviewData.ProductManagerChanges != null)
                    {
                        foreach (var change in reviewData.ProductManagerChanges.Where(c => c.Action != "keep"))
                        {
                            var action = change.Action.ToUpperInvariant();
                            var name = change.DisplayName ?? change.Email;
                            allChangesFormatted.Add($"{name} (Product Manager) - {action}");
                        }
                    }

                    // If there are any changes, send the email
                    if (allChangesFormatted.Any())
                    {
                        var personalisation = new Dictionary<string, dynamic>
                        {
                            { "product", product.Title ?? product.FipsId ?? "Unknown Product" },
                            { "changes", string.Join("\n", allChangesFormatted) }
                        };

                        var notificationResult = await _notificationService.SendEmailWithTemplateAsync(
                            userEmail,
                            confirmationTemplateId,
                            personalisation,
                            triggerCode: "dq_review_confirmation",
                            contextData: new Dictionary<string, object>
                            {
                                { "productDocumentId", documentId },
                                { "productFipsId", product.FipsId ?? "" },
                                { "reviewedBy", userEmail }
                            });

                        if (notificationResult.Success)
                        {
                            _logger.LogInformation(
                                "Review confirmation email sent successfully to {UserEmail} for product {ProductTitle} (DocumentId: {DocumentId})",
                                userEmail,
                                product.Title,
                                documentId);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to send review confirmation email to {UserEmail} for product {ProductTitle} (DocumentId: {DocumentId}): {Error}",
                                userEmail,
                                product.Title,
                                documentId,
                                notificationResult.ErrorMessage);
                        }
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No changes detected for product {ProductTitle} (DocumentId: {DocumentId}), skipping confirmation email",
                            product.Title,
                            documentId);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Review confirmation email not sent - template ID not configured or user email not available. TemplateId: {TemplateId}, UserEmail: {UserEmail}",
                        confirmationTemplateId ?? "not set",
                        userEmail ?? "not set");
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the review submission
                _logger.LogError(
                    ex,
                    "Error sending review confirmation email to {UserEmail} for product {ProductTitle} (DocumentId: {DocumentId})",
                    userEmail,
                    product.Title,
                    documentId);
            }

            // Clear session data
            HttpContext.Session.Remove($"DqReview_{documentId}");
            HttpContext.Session.Remove($"DqReviewChanges_{documentId}");
            HttpContext.Session.Remove($"DqReviewData_{documentId}");

            TempData["SuccessMessage"] = "Product DQ review completed successfully.";

            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting DQ review for {DocumentId}", documentId);
            TempData["ErrorMessage"] = "An error occurred while submitting the review. Please try again.";
            return RedirectToAction(nameof(ProductDqManagerAllProducts));
        }
    }

    private List<ContactChange> ProcessContactChanges(Microsoft.Extensions.Primitives.StringValues formValues)
    {
        var changes = new List<ContactChange>();
        if (!string.IsNullOrEmpty(formValues))
        {
            foreach (var changeJson in formValues)
            {
                if (!string.IsNullOrWhiteSpace(changeJson))
                {
                    try
                    {
                        var change = JsonSerializer.Deserialize<ContactChange>(changeJson);
                        if (change != null)
                        {
                            changes.Add(change);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse contact change: {ChangeJson}", changeJson);
                    }
                }
            }
        }
        return changes;
    }

    private double CalculateDqScore(ProductDto product)
    {
        var criteria = new List<bool>();

        // Key contacts: SRO and SO as minimum
        var hasSro = product.SeniorResponsibleOfficers != null && product.SeniorResponsibleOfficers.Any();
        var hasSo = product.ServiceOwners != null && product.ServiceOwners.Any();
        criteria.Add(hasSro && hasSo);

        // Business area
        var hasBusinessArea = product.CategoryValues != null &&
            product.CategoryValues.Any(cv => cv.CategoryType != null && 
                      cv.CategoryType.Name?.Trim().Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                      !string.IsNullOrWhiteSpace(cv.Name) &&
                      cv.Name != "Not assigned");
        criteria.Add(hasBusinessArea);

        // Phase
        var hasPhase = !string.IsNullOrEmpty(product.Phase) ||
                      (product.CategoryValues?.Any(cv => 
                          cv.CategoryType != null &&
                          !string.IsNullOrWhiteSpace(cv.CategoryType.Name) &&
                          cv.CategoryType.Name.Trim().Equals("Phase", StringComparison.OrdinalIgnoreCase) == true) == true);
        criteria.Add(hasPhase);

        // At least 1 channel
        var hasChannel = product.CategoryValues != null &&
            product.CategoryValues.Any(cv => cv.CategoryType != null &&
                      (cv.CategoryType.Name?.Trim().Equals("Channel", StringComparison.OrdinalIgnoreCase) == true ||
                       cv.CategoryType.Name?.Trim().Equals("Channels", StringComparison.OrdinalIgnoreCase) == true) &&
                      !string.IsNullOrWhiteSpace(cv.Name));
        criteria.Add(hasChannel);

        // At least 1 user group
        var userGroupVariations = new[] { "User group", "User groups", "User Group", "User Groups" };
        var hasUserGroup = product.CategoryValues != null &&
            product.CategoryValues.Any(cv => cv.CategoryType != null &&
                      userGroupVariations.Any(v => 
                          cv.CategoryType.Name?.Trim().Equals(v, StringComparison.OrdinalIgnoreCase) == true) &&
                      !string.IsNullOrWhiteSpace(cv.Name));
        criteria.Add(hasUserGroup);

        // At least 1 type
        var typeVariations = new[] { "Type", "Types" };
        var hasType = product.CategoryValues != null &&
            product.CategoryValues.Any(cv => cv.CategoryType != null &&
                      typeVariations.Any(v => 
                          cv.CategoryType.Name?.Trim().Equals(v, StringComparison.OrdinalIgnoreCase) == true) &&
                      !string.IsNullOrWhiteSpace(cv.Name));
        criteria.Add(hasType);

        // Calculate percentage (6 criteria total)
        var completedCount = criteria.Count(c => c);
        return (completedCount / 6.0) * 100.0;
    }
}

public class FipsManagerIndexViewModel
{
    public List<ProductCompletionItem> Products { get; set; } = new();
    public double AverageCompletionPercentage { get; set; }
    public int TotalProducts { get; set; }
}

public class FipsManagerProductDetailsViewModel
{
    public ProductDto? Product { get; set; }
    public Dictionary<string, List<CategoryValueDto>> CategoryTypes { get; set; } = new();
    public List<int> CurrentCategoryValueIds { get; set; } = new();
    public ProductCompletionItem CompletionItem { get; set; } = null!;
}

public class BulkSyncProgress
{
    public int Total { get; set; }
    public int Current { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public int SkippedCount { get; set; }
    public List<BulkSyncLogEntry> LogEntries { get; set; } = new();
    public bool IsComplete { get; set; }
}

public class BulkSyncLogEntry
{
    public string Type { get; set; } = "info"; // info, success, error
    public string Message { get; set; } = string.Empty;
}

public class ProductDqManagerIndexViewModel
{
    public List<ProductDqListItem> Products { get; set; } = new();
    public string ViewType { get; set; } = "MyServices"; // MyServices or AllProducts
}

public class ProductDqListItem
{
    public ProductDto? Product { get; set; }
    public double DqScore { get; set; }
    public DateTime? LastReviewedDate { get; set; }
    public DateTime NextDueDate { get; set; }
    public bool HasReviewInProgress { get; set; }
}

public class DqReviewInterstitialViewModel
{
    public ProductDto? Product { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
}

public class DqReviewCheckViewModel
{
    public ProductDto? Product { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public Dictionary<string, List<CategoryValueDto>> CategoryTypes { get; set; } = new();
    public List<int> CurrentCategoryValueIds { get; set; } = new();
    
    // Form fields
    public string? LongDescription { get; set; }
    public string? ProductUrl { get; set; }
    public List<int>? CategoryValueIds { get; set; }
    
    // Contact changes (stored but not updated in CMS)
    public List<ContactChange>? ServiceOwnerChanges { get; set; }
    public List<ContactChange>? ProductManagerChanges { get; set; }
    public List<ContactChange>? DeliveryManagerChanges { get; set; }
    public List<ContactChange>? InformationAssetOwnerChanges { get; set; }
    public List<ContactChange>? SeniorResponsibleOfficerChanges { get; set; }
    
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
}

public class ContactChange
{
    public string Action { get; set; } = string.Empty; // "add" or "remove"
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ObjectId { get; set; }
}

public class DqReviewConfirmationViewModel
{
    public ProductDto? Product { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public Dictionary<string, object> Changes { get; set; } = new();
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
}
