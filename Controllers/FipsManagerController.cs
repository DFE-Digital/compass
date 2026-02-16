using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Compass.Services;
using Compass.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Compass.Controllers;

[Authorize]
public class FipsManagerController : Controller
{
    private readonly ILogger<FipsManagerController> _logger;
    private readonly IProductsApiService _productsApiService;
    private readonly IConfiguration _configuration;

    public FipsManagerController(
        ILogger<FipsManagerController> logger,
        IProductsApiService productsApiService,
        IConfiguration configuration)
    {
        _logger = logger;
        _productsApiService = productsApiService;
        _configuration = configuration;
    }

    // GET: FipsManager/Index
    public async Task<IActionResult> Index()
    {
        // Check feature flag
        if (!_configuration.GetValue<bool>("FeatureFlags:EnableFIPSManager", false))
        {
            return NotFound();
        }

        try
        {
            ViewData["Title"] = "FIPS Manager - Active Products";

            // Get all products
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            
            // Filter to only Active products (State = "Active")
            var activeProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.State) && p.State.Equals("Active", StringComparison.OrdinalIgnoreCase))
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

        try
        {
            ViewData["Title"] = "FIPS Manager - New Products";

            // Get all products
            var allProducts = await _productsApiService.GetAllProductsAsync(null);
            
            // Filter to only New products (State = "New")
            var newProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.State) && p.State.Equals("New", StringComparison.OrdinalIgnoreCase))
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
                _logger.LogError("Failed to update product {DocumentId}. Status: {StatusCode}, Error: {Error}",
                    documentId, response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {DocumentId}", documentId);
            return false;
        }
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
    public ProductDto Product { get; set; } = null!;
    public Dictionary<string, List<CategoryValueDto>> CategoryTypes { get; set; } = new();
    public List<int> CurrentCategoryValueIds { get; set; } = new();
    public ProductCompletionItem CompletionItem { get; set; } = null!;
}
