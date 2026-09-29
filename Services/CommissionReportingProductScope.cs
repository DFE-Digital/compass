using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Product filtering for commission / product performance reporting (matches ProductReportingController rules).</summary>
public static class CommissionReportingProductScope
{
    public static bool PassesDataTypeExclusion(ProductDto product)
    {
        var types = product.CategoryValues?
            .Where(cv => cv.CategoryType?.Name?.Trim().Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
            .Select(cv => cv.Name?.Trim() ?? string.Empty)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (!types.Any())
            return true;
        if (types.Count == 1 && types[0].Trim().Equals("Data", StringComparison.OrdinalIgnoreCase))
            return false;
        if (types.All(t => t.Trim().Equals("Data", StringComparison.OrdinalIgnoreCase)))
            return false;
        return true;
    }

    public static bool PassesPhaseExclusion(ProductDto product) =>
        string.IsNullOrEmpty(product.Phase) ||
        (!product.Phase.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) &&
         !product.Phase.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Service-register contact roles that can complete a performance return.
    /// Names match <c>FipsContactRoles</c> (case-insensitive).
    /// </summary>
    public static bool IsPerformanceReportingContactRole(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
            return false;

        return roleName.Trim() switch
        {
            var name when name.Equals("Service Owner", StringComparison.OrdinalIgnoreCase) => true,
            var name when name.Equals("Product manager", StringComparison.OrdinalIgnoreCase) => true,
            var name when name.Equals("Delivery Manager", StringComparison.OrdinalIgnoreCase) => true,
            var name when name.Equals("Reporting contact", StringComparison.OrdinalIgnoreCase) => true,
            _ => false
        };
    }

    public static async Task<List<ProductDto>> GetUserProductsForReportingAsync(
        string? userEmail,
        IProductsApiService productsApi,
        CompassDbContext? serviceRegister = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userEmail))
            return new List<ProductDto>();

        var t1 = productsApi.GetProductsByServiceOwnerAsync(userEmail);
        var t2 = productsApi.GetProductsByProductManagerAsync(userEmail);
        var t3 = productsApi.GetProductsByDeliveryManagerAsync(userEmail);
        var t4 = productsApi.GetProductsByReportingUserAsync(userEmail);
        await Task.WhenAll(t1, t2, t3, t4);

        var products = (await t1)
            .Concat(await t2)
            .Concat(await t3)
            .Concat(await t4)
            .ToList();

        if (serviceRegister != null)
            products.AddRange(await LoadServiceRegisterContactProductsAsync(
                userEmail, productsApi, serviceRegister, cancellationToken));

        return CombineUserReportingProducts(products)
            .Where(PassesPhaseExclusion)
            .Where(PassesDataTypeExclusion)
            .ToList();
    }

    /// <summary>
    /// Catalogue products where the user is a reporting contact on the service register,
    /// matched by ServiceNow sys id. CMS role relations often omit that delivery manager.
    /// </summary>
    private static async Task<List<ProductDto>> LoadServiceRegisterContactProductsAsync(
        string userEmail,
        IProductsApiService productsApi,
        CompassDbContext serviceRegister,
        CancellationToken cancellationToken)
    {
        var email = userEmail.Trim().ToLowerInvariant();
        var reportingRoles = new[] { "service owner", "product manager", "delivery manager", "reporting contact" };

        var sysIds = await serviceRegister.CMDBProducts.AsNoTracking()
            .Where(p => p.CMDBID != null && p.CMDBID != "")
            .Where(p => p.Contacts.Any(c =>
                c.UserEmail != null &&
                c.UserEmail.ToLower() == email &&
                c.FipsContactRole != null &&
                reportingRoles.Contains(c.FipsContactRole.Name.ToLower())))
            .Select(p => p.CMDBID!)
            .ToListAsync(cancellationToken);

        var sysIdSet = sysIds
            .Select(id => id.Trim())
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sysIdSet.Count == 0)
            return new List<ProductDto>();

        var catalogue = await productsApi.GetAllProductsAsync(null);
        return catalogue
            .Where(p =>
                p.State != null &&
                p.State.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(p.CmdbSysId) &&
                sysIdSet.Contains(p.CmdbSysId.Trim()))
            .ToList();
    }

    public static List<ProductDto> CombineUserReportingProducts(IEnumerable<ProductDto> products) =>
        products
            .GroupBy(ProductIdentityKey, StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => g.First())
            .ToList();

    private static string ProductIdentityKey(ProductDto product)
    {
        if (!string.IsNullOrWhiteSpace(product.FipsId))
            return "f:" + product.FipsId.Trim();
        if (!string.IsNullOrWhiteSpace(product.DocumentId))
            return "d:" + product.DocumentId.Trim();
        if (!string.IsNullOrWhiteSpace(product.CmdbSysId))
            return "c:" + product.CmdbSysId.Trim();
        return "";
    }

    public static List<ProductDto> GetAllActivePublishedEligible(IEnumerable<ProductDto> allProducts) =>
        allProducts
            .Where(p => p.State != null &&
                        p.State.Equals("Active", StringComparison.OrdinalIgnoreCase) &&
                        p.PublishedAt.HasValue)
            .Where(PassesPhaseExclusion)
            .Where(PassesDataTypeExclusion)
            .ToList();

    public static string? GetCategoryName(ProductDto product, string categoryTypeName)
    {
        if (product.CategoryValues == null)
            return null;
        var cv = product.CategoryValues.FirstOrDefault(c =>
            c.CategoryType != null &&
            c.CategoryType.Name.Equals(categoryTypeName, StringComparison.OrdinalIgnoreCase));
        return cv?.Name;
    }

    /// <summary>FIPS "Business area" category.</summary>
    public static string? GetBusinessArea(ProductDto product) => GetCategoryName(product, "Business area");

    /// <summary>Directorate from product categories (exact "Directorate" or type name containing "Directorate").</summary>
    public static string? GetDirectorate(ProductDto product)
    {
        if (product.CategoryValues == null)
            return null;
        foreach (var cv in product.CategoryValues)
        {
            var tn = cv.CategoryType?.Name?.Trim();
            if (string.IsNullOrEmpty(tn))
                continue;
            if (tn.Equals("Directorate", StringComparison.OrdinalIgnoreCase))
                return cv.Name;
            if (tn.Contains("Directorate", StringComparison.OrdinalIgnoreCase))
                return cv.Name;
        }

        return null;
    }

    /// <summary>Distinct phase and Type category values from a product set (e.g. active catalogue).</summary>
    public static (List<string> Phases, List<string> Types) CollectDistinctPhasesAndTypesFromProducts(
        IEnumerable<ProductDto> products)
    {
        var phases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in products)
        {
            if (!string.IsNullOrWhiteSpace(p.Phase))
                phases.Add(p.Phase.Trim());
            if (p.CategoryValues == null)
                continue;
            foreach (var cv in p.CategoryValues.Where(c =>
                         c.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true))
            {
                if (!string.IsNullOrWhiteSpace(cv.Name))
                    types.Add(cv.Name.Trim());
            }
        }

        return (
            phases.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList(),
            types.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList());
    }

    public static HashSet<string> ParseCommaSeparatedValues(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the product is in scope for this commission's phase/type rules (and global reporting exclusions).
    /// Empty commission phase/type rules mean &quot;any&quot; (still subject to decommissioned / data-only exclusions).
    /// </summary>
    public static bool ProductMatchesCommissionInScopeRules(Commission commission, ProductDto product)
    {
        if (!PassesPhaseExclusion(product) || !PassesDataTypeExclusion(product))
            return false;

        var phaseRules = ParseCommaSeparatedValues(commission.InScopePhases);
        if (phaseRules.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(product.Phase))
                return false;
            if (!phaseRules.Contains(product.Phase.Trim()))
                return false;
        }

        var typeRules = ParseCommaSeparatedValues(commission.InScopeTypes);
        if (typeRules.Count > 0)
        {
            var productTypes = product.CategoryValues?
                .Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
                .Select(cv => cv.Name?.Trim() ?? "")
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList() ?? new List<string>();
            if (!productTypes.Any(pt => typeRules.Contains(pt)))
                return false;
        }

        return true;
    }
}
