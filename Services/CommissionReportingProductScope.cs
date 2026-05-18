using Compass.Models;

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

    public static async Task<List<ProductDto>> GetUserProductsForReportingAsync(
        string? userEmail,
        IProductsApiService productsApi)
    {
        if (string.IsNullOrEmpty(userEmail))
            return new List<ProductDto>();

        var t1 = productsApi.GetProductsByServiceOwnerAsync(userEmail);
        var t2 = productsApi.GetProductsByProductManagerAsync(userEmail);
        var t3 = productsApi.GetProductsByDeliveryManagerAsync(userEmail);
        var t4 = productsApi.GetProductsByReportingUserAsync(userEmail);
        await Task.WhenAll(t1, t2, t3, t4);

        var productsByServiceOwner = await t1;
        var productsByProductManager = await t2;
        var productsByDeliveryManager = await t3;
        var productsByReportingUser = await t4;

        return productsByServiceOwner
            .Concat(productsByProductManager)
            .Concat(productsByDeliveryManager)
            .Concat(productsByReportingUser)
            .GroupBy(p => p.FipsId)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .Select(g => g.First())
            .Where(PassesPhaseExclusion)
            .Where(PassesDataTypeExclusion)
            .ToList();
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
