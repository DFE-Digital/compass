using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public sealed class FipsCompletionBulkImportService : IFipsCompletionBulkImportService
{
    private readonly CompassDbContext _db;
    private readonly IProductsApiService _productsApi;
    private readonly IFipsProductWriteService _fipsProductWrite;

    public FipsCompletionBulkImportService(
        CompassDbContext db,
        IProductsApiService productsApi,
        IFipsProductWriteService fipsProductWrite)
    {
        _db = db;
        _productsApi = productsApi;
        _fipsProductWrite = fipsProductWrite;
    }

    public async Task<FipsCompletionImportResult> ImportAsync(
        IReadOnlyList<FipsCompletionSpreadsheet.ImportRow> rows,
        string actorEmail,
        string? auditDisplayName,
        CancellationToken cancellationToken = default)
    {
        var phaseCategoryByName = (await _productsApi.GetPhaseCategoryValuesAsync())
            .GroupBy(p => p.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var cmdbPhaseByName = await _db.PhaseLookups.AsNoTracking()
            .Where(p => p.IsActive)
            .ToDictionaryAsync(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var cmdbByTitle = await _db.CMDBProducts
            .Include(p => p.BusinessAreas)
            .Include(p => p.Channels)
            .Include(p => p.UserGroups)
            .Include(p => p.Types)
            .Include(p => p.CategorisationItems)
            .ToDictionaryAsync(p => p.Title.Trim(), p => p, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var strapiByFipsId = new Dictionary<string, ProductDto>(StringComparer.OrdinalIgnoreCase);
        var strapiByTitle = new Dictionary<string, ProductDto>(StringComparer.OrdinalIgnoreCase);

        var results = new List<FipsCompletionImportRowResult>();
        var updated = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var row in rows)
        {
            var line = new FipsCompletionImportRowResult
            {
                RowNumber = row.RowNumber,
                ProductTitle = row.ProductTitle,
                FipsId = row.FipsId
            };

            if (!row.HasAnyUpdate)
            {
                line.Errors.Add("No phase or product URL value to apply.");
                results.Add(line);
                skipped++;
                continue;
            }

            try
            {
                var strapiProduct = await ResolveStrapiProductAsync(row, strapiByFipsId, strapiByTitle, cancellationToken);
                cmdbByTitle.TryGetValue(row.ProductTitle.Trim(), out var cmdbProduct);
                if (cmdbProduct == null && strapiProduct != null)
                    cmdbByTitle.TryGetValue(strapiProduct.Title.Trim(), out cmdbProduct);

                if (strapiProduct == null && cmdbProduct == null)
                {
                    line.Errors.Add("Product not found (check FIPS ID or product title).");
                    results.Add(line);
                    failed++;
                    continue;
                }

                var rowOk = true;
                var anyChange = false;

                if (row.HasPhaseUpdate)
                {
                    if (strapiProduct != null && !string.IsNullOrWhiteSpace(strapiProduct.FipsId))
                    {
                        if (!phaseCategoryByName.TryGetValue(row.Phase!.Trim(), out var phaseCatId))
                        {
                            line.Errors.Add($"Unknown phase '{row.Phase}' for FIPS (CMS).");
                            rowOk = false;
                        }
                        else
                        {
                            var ok = await _productsApi.UpdateProductPhaseAsync(strapiProduct.FipsId, phaseCatId);
                            if (ok)
                            {
                                line.Messages.Add($"FIPS phase → {row.Phase}");
                                anyChange = true;
                            }
                            else
                            {
                                line.Errors.Add("Failed to update phase in FIPS (CMS).");
                                rowOk = false;
                            }
                        }
                    }

                    if (cmdbProduct != null)
                    {
                        if (!cmdbPhaseByName.TryGetValue(row.Phase!.Trim(), out _))
                        {
                            line.Errors.Add($"Unknown phase '{row.Phase}' for service register.");
                            rowOk = false;
                        }
                    }
                }

                if (row.HasProductUrlUpdate && strapiProduct != null && !string.IsNullOrWhiteSpace(strapiProduct.FipsId))
                {
                    var ok = await _productsApi.UpdateProductUrlAsync(strapiProduct.FipsId, row.ProductUrl!);
                    if (ok)
                    {
                        line.Messages.Add("FIPS product URL updated.");
                        anyChange = true;
                    }
                    else
                    {
                        line.Errors.Add("Failed to update product URL in FIPS (CMS).");
                        rowOk = false;
                    }
                }

                if (cmdbProduct != null)
                {
                    int? targetPhaseId = cmdbProduct.PhaseId;
                    if (row.HasPhaseUpdate && cmdbPhaseByName.TryGetValue(row.Phase!.Trim(), out var cmdbPhaseId))
                        targetPhaseId = cmdbPhaseId;

                    var targetUrl = row.HasProductUrlUpdate ? row.ProductUrl : cmdbProduct.ProductURL;
                    var cmdbNeedsUpdate = cmdbProduct.PhaseId != targetPhaseId || cmdbProduct.ProductURL != targetUrl;

                    if (cmdbNeedsUpdate)
                    {
                        var outcome = await _fipsProductWrite.TryUpdateAsync(
                            cmdbProduct.Id,
                            actorEmail,
                            auditDisplayName,
                            requireServiceOwnerManager: false,
                            cmdbProduct.UserDescription,
                            targetPhaseId,
                            targetUrl,
                            cmdbProduct.BusinessAreas.Select(b => b.FipsBusinessAreaId).ToArray(),
                            cmdbProduct.Channels.Select(c => c.FipsChannelId).ToArray(),
                            cmdbProduct.UserGroups.Select(u => u.FipsUserGroupId).ToArray(),
                            cmdbProduct.Types.Select(t => t.FipsTypeId).ToArray(),
                            directorateIds: null,
                            categorisationItemIds: cmdbProduct.CategorisationItems.Select(c => c.FipsCategorisationItemId).ToArray(),
                            reportingContactUserId: null,
                            isEnterpriseService: null,
                            cancellationToken: cancellationToken);

                        if (outcome.NotFound || outcome.Forbidden)
                        {
                            line.Errors.Add("Failed to update service register product.");
                            rowOk = false;
                        }
                        else if (outcome.Changes.Count > 0)
                        {
                            anyChange = true;
                            if (outcome.Changes.Contains("Phase"))
                                line.Messages.Add($"Service register phase → {row.Phase}");
                            if (outcome.Changes.Contains("Product URL"))
                                line.Messages.Add("Service register product URL updated.");
                        }
                    }
                }

                if (rowOk && anyChange)
                {
                    line.Success = true;
                    updated++;
                }
                else if (rowOk && !anyChange)
                {
                    line.Messages.Add("No changes (values already match).");
                    skipped++;
                }
                else
                    failed++;
            }
            catch (Exception ex)
            {
                line.Errors.Add(ex.Message);
                failed++;
            }

            results.Add(line);
        }

        return new FipsCompletionImportResult
        {
            TotalRows = rows.Count,
            UpdatedCount = updated,
            SkippedCount = skipped,
            FailedCount = failed,
            Rows = results
        };
    }

    private async Task<ProductDto?> ResolveStrapiProductAsync(
        FipsCompletionSpreadsheet.ImportRow row,
        Dictionary<string, ProductDto> byFipsId,
        Dictionary<string, ProductDto> byTitle,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(row.FipsId))
        {
            if (byFipsId.TryGetValue(row.FipsId, out var cached))
                return cached;

            var byFips = await _productsApi.GetProductByFipsIdAsync(row.FipsId);
            if (byFips != null)
                byFipsId[row.FipsId] = byFips;
            return byFips;
        }

        if (string.IsNullOrWhiteSpace(row.ProductTitle))
            return null;

        if (byTitle.TryGetValue(row.ProductTitle, out var titleCached))
            return titleCached;

        var matches = await _productsApi.SearchProductsByTitleAsync(row.ProductTitle);
        var exact = matches.FirstOrDefault(p =>
            string.Equals(p.Title.Trim(), row.ProductTitle, StringComparison.OrdinalIgnoreCase));
        var product = exact ?? matches.FirstOrDefault();
        if (product != null)
            byTitle[row.ProductTitle] = product;
        return product;
    }
}
