using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

public partial class ModernAdminController
{
    [HttpGet("service-data")]
    public async Task<IActionResult> ServiceData(
        string? filter,
        [FromServices] ICmsCompassServiceDataComparisonService comparisonService,
        CancellationToken cancellationToken)
    {
        SetAdminChrome("admin-service-data");

        var report = await comparisonService.BuildReportAsync(cancellationToken);
        var normalizedFilter = NormalizeServiceDataFilter(filter);
        report.SyncableProducts = report.Products
            .Where(p => p.CanSyncFromCms)
            .ToList();
        report.Products = FilterProductRows(report.Products, normalizedFilter);

        ViewBag.ServiceDataFilter = normalizedFilter;
        return View("~/Views/Modern/Admin/ServiceData.cshtml", report);
    }

    [HttpPost("service-data/sync")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceDataSync(
        string? filter,
        bool syncPhase,
        bool syncChannel,
        bool syncType,
        bool syncBusinessArea,
        bool syncUserGroup,
        bool dryRun,
        [FromForm] List<string>? selectedCmdbIds,
        [FromForm] List<string>? mapFieldKey,
        [FromForm] List<string>? mapCmsValue,
        [FromForm] List<string>? mapCompassId,
        [FromServices] ICmsCompassServiceDataComparisonService comparisonService,
        [FromServices] ICmsCompassServiceDataSyncService syncService,
        CancellationToken cancellationToken)
    {
        SetAdminChrome("admin-service-data");

        var mappings = ParseValueMappings(mapFieldKey, mapCmsValue, mapCompassId);
        var request = new CmsCompassSyncRequest
        {
            SyncPhase = syncPhase,
            SyncChannel = syncChannel,
            SyncType = syncType,
            SyncBusinessArea = syncBusinessArea,
            SyncUserGroup = syncUserGroup,
            DryRun = dryRun,
            CmdbIds = selectedCmdbIds ?? [],
            Mappings = mappings
        };

        var actorEmail = User.Identity?.Name ?? "unknown";
        var displayName = User.FindFirst("name")?.Value ?? actorEmail;
        var syncResult = await syncService.ApplyCmsToCompassAsync(request, actorEmail, displayName, cancellationToken);

        var report = await comparisonService.BuildReportAsync(cancellationToken);
        var normalizedFilter = NormalizeServiceDataFilter(filter);
        report.SyncableProducts = report.Products
            .Where(p => p.CanSyncFromCms)
            .ToList();
        report.Products = FilterProductRows(report.Products, normalizedFilter);
        report.LastSyncResult = syncResult;

        ViewBag.ServiceDataFilter = normalizedFilter;
        ApplySubmittedMappings(report, mapFieldKey, mapCmsValue, mapCompassId);

        if (syncResult.FailedCount == 0 && syncResult.UpdatedCount > 0)
        {
            TempData["AdminMessage"] = dryRun
                ? $"Dry run complete — {syncResult.UpdatedCount} product(s) would be updated."
                : $"Updated {syncResult.UpdatedCount} COMPASS product(s) from CMS data.";
        }
        else if (syncResult.FailedCount > 0)
        {
            TempData["AdminError"] = dryRun
                ? $"Dry run finished with {syncResult.FailedCount} product(s) that could not be processed."
                : $"Sync finished with {syncResult.FailedCount} product(s) that could not be updated.";
        }
        else if (syncResult.SkippedCount > 0 && syncResult.UpdatedCount == 0)
        {
            TempData["AdminMessage"] = "No changes were needed for the selected products and fields.";
        }

        return View("~/Views/Modern/Admin/ServiceData.cshtml", report);
    }

    private static Dictionary<string, IReadOnlyDictionary<string, int?>> ParseValueMappings(
        List<string>? fieldKeys,
        List<string>? cmsValues,
        List<string>? compassIds)
    {
        var result = new Dictionary<string, Dictionary<string, int?>>(StringComparer.OrdinalIgnoreCase);
        if (fieldKeys == null || cmsValues == null || compassIds == null)
            return result.ToDictionary(k => k.Key, k => (IReadOnlyDictionary<string, int?>)k.Value, StringComparer.OrdinalIgnoreCase);

        var count = Math.Min(fieldKeys.Count, Math.Min(cmsValues.Count, compassIds.Count));
        for (var i = 0; i < count; i++)
        {
            var fieldKey = fieldKeys[i]?.Trim();
            var cmsValue = cmsValues[i]?.Trim();
            if (string.IsNullOrWhiteSpace(fieldKey) || string.IsNullOrWhiteSpace(cmsValue))
                continue;

            if (!result.TryGetValue(fieldKey, out var fieldMap))
            {
                fieldMap = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
                result[fieldKey] = fieldMap;
            }

            var rawId = compassIds[i]?.Trim();
            fieldMap[cmsValue] = int.TryParse(rawId, out var parsedId) ? parsedId : null;
        }

        return result.ToDictionary(k => k.Key, k => (IReadOnlyDictionary<string, int?>)k.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplySubmittedMappings(
        CmsCompassServiceDataViewModel report,
        List<string>? fieldKeys,
        List<string>? cmsValues,
        List<string>? compassIds)
    {
        if (fieldKeys == null || cmsValues == null || compassIds == null)
            return;

        var submitted = ParseValueMappings(fieldKeys, cmsValues, compassIds);
        foreach (var group in report.ValueMappingGroups)
        {
            if (!submitted.TryGetValue(group.FieldKey, out var fieldMap))
                continue;

            foreach (var row in group.Rows)
            {
                if (!fieldMap.TryGetValue(row.CmsValueName, out var compassId))
                    continue;

                row.SuggestedCompassId = compassId;
                row.SuggestedCompassName = row.CompassOptions
                    .FirstOrDefault(o => o.Id == compassId)?.Name;
                row.HasExactNameMatch = compassId.HasValue
                    && row.CompassOptions.Any(o => o.Id == compassId
                        && string.Equals(o.Name, row.CmsValueName, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    private static string NormalizeServiceDataFilter(string? filter)
    {
        var normalized = (filter ?? "differences").Trim().ToLowerInvariant();
        return normalized switch
        {
            "all" => "all",
            "matched" => "matched",
            "identical" => "identical",
            "differences" => "differences",
            "cms-only" => "cms-only",
            "compass-only" => "compass-only",
            _ => "differences"
        };
    }

    private static IReadOnlyList<CmsCompassProductComparisonRow> FilterProductRows(
        IReadOnlyList<CmsCompassProductComparisonRow> rows,
        string filter) =>
        filter switch
        {
            "all" => rows,
            "matched" => rows.Where(r =>
                r.MatchStatus is CmsCompassProductMatchStatus.MatchedIdentical
                    or CmsCompassProductMatchStatus.MatchedWithDifferences).ToList(),
            "identical" => rows.Where(r => r.MatchStatus == CmsCompassProductMatchStatus.MatchedIdentical).ToList(),
            "differences" => rows.Where(r => r.MatchStatus == CmsCompassProductMatchStatus.MatchedWithDifferences).ToList(),
            "cms-only" => rows.Where(r => r.MatchStatus == CmsCompassProductMatchStatus.CmsOnly).ToList(),
            "compass-only" => rows.Where(r => r.MatchStatus == CmsCompassProductMatchStatus.CompassOnly).ToList(),
            _ => rows.Where(r => r.MatchStatus == CmsCompassProductMatchStatus.MatchedWithDifferences).ToList()
        };
}
