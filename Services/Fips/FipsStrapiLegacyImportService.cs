using System.Text.Json;
using System.Text.Json.Serialization;
using Compass.Data;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public sealed class FipsStrapiLegacyImportService : IFipsStrapiLegacyImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CompassDbContext _db;
    private readonly IFipsProductWriteService _fipsProductWrite;

    public FipsStrapiLegacyImportService(CompassDbContext db, IFipsProductWriteService fipsProductWrite)
    {
        _db = db;
        _fipsProductWrite = fipsProductWrite;
    }

    public async Task<FipsCompletionImportResult> ImportAsync(
        Stream jsonStream,
        string actorEmail,
        string? auditDisplayName,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        var export = await JsonSerializer.DeserializeAsync<StrapiLegacyExportRoot>(jsonStream, JsonOptions, cancellationToken);
        var records = export?.Data ?? [];
        if (records.Count == 0)
        {
            return new FipsCompletionImportResult
            {
                TotalRows = 0,
                Rows =
                [
                    new FipsCompletionImportRowResult
                    {
                        RowNumber = 0,
                        Errors = ["No products found in JSON (expected a root object with a data array)."]
                    }
                ],
                FailedCount = 1
            };
        }

        var phaseByName = await _db.PhaseLookups.AsNoTracking()
            .Where(p => p.IsActive)
            .ToDictionaryAsync(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var channelByName = await _db.FipsChannels.AsNoTracking()
            .Where(c => c.Active)
            .ToDictionaryAsync(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var typeByName = await _db.FipsTypes.AsNoTracking()
            .Where(t => t.Active)
            .ToDictionaryAsync(t => t.Name.Trim(), t => t.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var products = await _db.CMDBProducts
            .Include(p => p.BusinessAreas)
            .Include(p => p.Channels)
            .Include(p => p.UserGroups)
            .Include(p => p.Types)
            .Include(p => p.CategorisationItems)
            .ToListAsync(cancellationToken);

        var byCmdbId = products
            .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
            .GroupBy(p => p.CMDBID!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var results = new List<FipsCompletionImportRowResult>();
        var updated = 0;
        var skipped = 0;
        var failed = 0;
        var rowNumber = 0;

        foreach (var record in records)
        {
            rowNumber++;
            var line = new FipsCompletionImportRowResult
            {
                RowNumber = rowNumber,
                ProductTitle = record.Title?.Trim() ?? "",
                FipsId = record.CmdbSysId?.Trim()
            };

            var sysId = record.CmdbSysId?.Trim();
            if (string.IsNullOrWhiteSpace(sysId))
            {
                line.Errors.Add("Missing cmdb_sys_id.");
                results.Add(line);
                failed++;
                continue;
            }

            if (!byCmdbId.TryGetValue(sysId, out var product))
            {
                line.Errors.Add($"No service register product with CMDB ID '{sysId}'.");
                results.Add(line);
                failed++;
                continue;
            }

            var rowOk = true;
            int? targetPhaseId = product.PhaseId;
            var targetChannels = product.Channels.Select(c => c.FipsChannelId).ToList();
            var targetTypes = product.Types.Select(t => t.FipsTypeId).ToList();
            var targetUserDescription = NormalizeOptionalText(record.LongDescription);
            var targetProductUrl = NormalizeOptionalText(record.ProductUrl);

            foreach (var cv in record.CategoryValues ?? [])
            {
                var typeName = cv.CategoryType?.Name?.Trim();
                var valueName = cv.Name?.Trim();
                if (string.IsNullOrWhiteSpace(typeName) || string.IsNullOrWhiteSpace(valueName))
                    continue;

                switch (typeName)
                {
                    case "Phase":
                        if (!phaseByName.TryGetValue(valueName, out var phaseId))
                        {
                            line.Errors.Add($"Unknown phase '{valueName}' in service register.");
                            rowOk = false;
                        }
                        else
                            targetPhaseId = phaseId;
                        break;
                    case "Channel":
                        if (!channelByName.TryGetValue(valueName, out var channelId))
                        {
                            line.Errors.Add($"Unknown channel '{valueName}' in service register.");
                            rowOk = false;
                        }
                        else if (!targetChannels.Contains(channelId))
                            targetChannels.Add(channelId);
                        break;
                    case "Type":
                        if (!typeByName.TryGetValue(valueName, out var typeId))
                        {
                            line.Errors.Add($"Unknown type '{valueName}' in service register.");
                            rowOk = false;
                        }
                        else if (!targetTypes.Contains(typeId))
                            targetTypes.Add(typeId);
                        break;
                    case "Business area":
                        // Intentionally skipped — Compass business areas differ from legacy Strapi values.
                        break;
                }
            }

            if (!rowOk)
            {
                results.Add(line);
                failed++;
                continue;
            }

            var preserveBusinessAreas = product.BusinessAreas.Select(b => b.FipsBusinessAreaId).ToArray();
            var preserveUserGroups = product.UserGroups.Select(u => u.FipsUserGroupId).ToArray();
            var preserveCategories = product.CategorisationItems.Select(c => c.FipsCategorisationItemId).ToArray();

            var wouldChange =
                product.UserDescription != targetUserDescription
                || product.ProductURL != targetProductUrl
                || product.PhaseId != targetPhaseId
                || !SetEquals(product.Channels.Select(c => c.FipsChannelId), targetChannels)
                || !SetEquals(product.Types.Select(t => t.FipsTypeId), targetTypes);

            if (!wouldChange)
            {
                line.Messages.Add("No changes (values already match).");
                results.Add(line);
                skipped++;
                continue;
            }

            if (dryRun)
            {
                line.Success = true;
                line.Messages.Add("Dry run — would update.");
                if (product.UserDescription != targetUserDescription)
                    line.Messages.Add("User description");
                if (product.ProductURL != targetProductUrl)
                    line.Messages.Add("Product URL");
                if (product.PhaseId != targetPhaseId)
                    line.Messages.Add("Phase");
                if (!SetEquals(product.Channels.Select(c => c.FipsChannelId), targetChannels))
                    line.Messages.Add("Channel(s)");
                if (!SetEquals(product.Types.Select(t => t.FipsTypeId), targetTypes))
                    line.Messages.Add("Type(s)");
                results.Add(line);
                updated++;
                continue;
            }

            var outcome = await _fipsProductWrite.TryUpdateAsync(
                product.Id,
                actorEmail,
                auditDisplayName,
                requireServiceOwnerManager: false,
                targetUserDescription,
                targetPhaseId,
                targetProductUrl,
                preserveBusinessAreas,
                targetChannels.ToArray(),
                preserveUserGroups,
                targetTypes.ToArray(),
                preserveCategories,
                reportingContactUserId: null,
                isEnterpriseService: null,
                cancellationToken);

            if (outcome.NotFound || outcome.Forbidden)
            {
                line.Errors.Add("Failed to update service register product.");
                results.Add(line);
                failed++;
                continue;
            }

            if (outcome.Changes.Count == 0)
            {
                line.Messages.Add("No changes (values already match).");
                skipped++;
            }
            else
            {
                line.Success = true;
                line.Messages.AddRange(outcome.Changes.Select(c => $"Updated {c}"));
                updated++;
            }

            results.Add(line);
        }

        return new FipsCompletionImportResult
        {
            TotalRows = records.Count,
            UpdatedCount = updated,
            SkippedCount = skipped,
            FailedCount = failed,
            Rows = results
        };
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool SetEquals(IEnumerable<int> left, IReadOnlyList<int> right) =>
        left.OrderBy(x => x).SequenceEqual(right.OrderBy(x => x));

    private sealed class StrapiLegacyExportRoot
    {
        [JsonPropertyName("data")]
        public List<StrapiLegacyProduct> Data { get; set; } = [];
    }

    private sealed class StrapiLegacyProduct
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("cmdb_sys_id")]
        public string? CmdbSysId { get; set; }

        [JsonPropertyName("long_description")]
        public string? LongDescription { get; set; }

        [JsonPropertyName("product_url")]
        public string? ProductUrl { get; set; }

        [JsonPropertyName("category_values")]
        public List<StrapiLegacyCategoryValue>? CategoryValues { get; set; }
    }

    private sealed class StrapiLegacyCategoryValue
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("category_type")]
        public StrapiLegacyCategoryType? CategoryType { get; set; }
    }

    private sealed class StrapiLegacyCategoryType
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
