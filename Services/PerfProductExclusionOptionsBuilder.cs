using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Active Service Register products for performance reporting exclusion pickers.</summary>
public sealed class PerfProductExclusionProductOption
{
    public string ProductDocumentId { get; set; } = "";
    public string? FipsId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Label { get; set; } = "";
}

public static class PerfProductExclusionOptionsBuilder
{
    public static async Task<List<PerfProductExclusionProductOption>> BuildActiveServiceRegisterOptionsAsync(
        CompassDbContext context,
        IProductsApiService productsApi,
        IEnumerable<string>? ensureDocumentIds = null,
        CancellationToken cancellationToken = default)
    {
        var cmdbActive = await context.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active)
            .OrderBy(p => p.Title)
            .Select(p => new { p.Id, p.Title, p.CMDBID })
            .ToListAsync(cancellationToken);

        var cmsByFips = await LoadCmsProductsByFipsIdAsync(productsApi, cancellationToken);

        var options = new List<PerfProductExclusionProductOption>(cmdbActive.Count);
        var seenDocIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in cmdbActive)
        {
            var option = MapCmdbRow(p.Id, p.Title, p.CMDBID, cmsByFips);
            if (seenDocIds.Add(option.ProductDocumentId))
                options.Add(option);
        }

        if (ensureDocumentIds == null)
            return options;

        foreach (var docId in ensureDocumentIds.Where(d => !string.IsNullOrWhiteSpace(d)))
        {
            var trimmed = docId.Trim();
            if (!seenDocIds.Add(trimmed))
                continue;

            var existing = await context.PerformanceReportingProductExclusions.AsNoTracking()
                .Where(e => e.ProductDocumentId == trimmed)
                .Select(e => new { e.ProductDocumentId, e.FipsId, e.ProductName })
                .FirstOrDefaultAsync(cancellationToken);

            options.Add(new PerfProductExclusionProductOption
            {
                ProductDocumentId = trimmed,
                FipsId = existing?.FipsId,
                DisplayName = existing?.ProductName ?? trimmed,
                Label = FormatLabel(existing?.ProductName ?? trimmed, existing?.FipsId)
            });
        }

        return options.OrderBy(o => o.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static async Task<PerfProductExclusionProductOption?> ResolveByDocumentIdAsync(
        CompassDbContext context,
        IProductsApiService productsApi,
        string productDocumentId,
        CancellationToken cancellationToken = default)
    {
        var docId = (productDocumentId ?? "").Trim();
        if (string.IsNullOrEmpty(docId))
            return null;

        var options = await BuildActiveServiceRegisterOptionsAsync(
            context, productsApi, new[] { docId }, cancellationToken);
        return options.FirstOrDefault(o =>
            string.Equals(o.ProductDocumentId, docId, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<Dictionary<string, ProductDto>> LoadCmsProductsByFipsIdAsync(
        IProductsApiService productsApi,
        CancellationToken cancellationToken)
    {
        try
        {
            var cmsProducts = await productsApi.GetAllProductsAsync();
            return cmsProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.FipsId))
                .GroupBy(p => p.FipsId!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, ProductDto>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static PerfProductExclusionProductOption MapCmdbRow(
        Guid cmdbId,
        string title,
        string? cmdbIdCode,
        IReadOnlyDictionary<string, ProductDto> cmsByFips)
    {
        var fipsId = string.IsNullOrWhiteSpace(cmdbIdCode) ? null : cmdbIdCode.Trim();
        var displayName = title;
        string? documentId = null;

        if (fipsId != null && cmsByFips.TryGetValue(fipsId, out var cms))
        {
            documentId = cms.DocumentId;
            if (!string.IsNullOrWhiteSpace(cms.Title))
                displayName = cms.Title;
        }

        documentId ??= cmdbId.ToString();

        return new PerfProductExclusionProductOption
        {
            ProductDocumentId = documentId,
            FipsId = fipsId,
            DisplayName = displayName,
            Label = FormatLabel(displayName, fipsId)
        };
    }

    private static string FormatLabel(string displayName, string? fipsId) =>
        string.IsNullOrWhiteSpace(fipsId) ? displayName : $"{displayName} ({fipsId})";
}
