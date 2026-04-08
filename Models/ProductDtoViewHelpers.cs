namespace Compass.Models;

/// <summary>Display helpers for CMS <see cref="ProductDto"/> — used by Modern product chrome partials.</summary>
public static class ProductDtoViewHelpers
{
    public static string? BusinessArea(ProductDto? p) =>
        p?.CategoryValues?
            .FirstOrDefault(cv => cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)
            ?.Name;

    public static IReadOnlyList<string> TypeNames(ProductDto? p)
    {
        if (p?.CategoryValues is null)
            return Array.Empty<string>();
        return p.CategoryValues
            .Where(cv => cv.CategoryType?.Name?.Equals("Type", StringComparison.OrdinalIgnoreCase) == true)
            .Select(cv => cv.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
    }
}
