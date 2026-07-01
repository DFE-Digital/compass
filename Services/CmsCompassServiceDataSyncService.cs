using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Compass.Services;

public sealed class CmsCompassServiceDataSyncService : ICmsCompassServiceDataSyncService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    private readonly CompassDbContext _db;
    private readonly IProductsApiService _productsApi;
    private readonly IFipsProductWriteService _fipsProductWrite;
    private readonly ILogger<CmsCompassServiceDataSyncService> _logger;

    public CmsCompassServiceDataSyncService(
        CompassDbContext db,
        IProductsApiService productsApi,
        IFipsProductWriteService fipsProductWrite,
        ILogger<CmsCompassServiceDataSyncService> logger)
    {
        _db = db;
        _productsApi = productsApi;
        _fipsProductWrite = fipsProductWrite;
        _logger = logger;
    }

    public async Task<CmsCompassSyncResultViewModel> ApplyCmsToCompassAsync(
        CmsCompassSyncRequest request,
        string actorEmail,
        string? auditDisplayName,
        CancellationToken cancellationToken = default)
    {
        var productResults = new List<CmsCompassSyncProductResult>();

        if (!request.SyncPhase && !request.SyncChannel && !request.SyncType
            && !request.SyncBusinessArea && !request.SyncUserGroup)
        {
            return new CmsCompassSyncResultViewModel
            {
                DryRun = request.DryRun,
                FailedCount = 1,
                Results =
                [
                    new CmsCompassSyncProductResult
                    {
                        Success = false,
                        Errors = ["Select at least one field to copy from CMS to COMPASS."]
                    }
                ]
            };
        }

        var cmdbIds = request.CmdbIds
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(NameComparer)
            .ToList();

        if (cmdbIds.Count == 0)
        {
            return new CmsCompassSyncResultViewModel
            {
                DryRun = request.DryRun,
                FailedCount = 1,
                Results =
                [
                    new CmsCompassSyncProductResult
                    {
                        Success = false,
                        Errors = ["Select at least one matched product to update."]
                    }
                ]
            };
        }

        List<ProductDto> cmsProducts;
        try
        {
            cmsProducts = await _productsApi.GetAllProductsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch CMS products for service data sync");
            return new CmsCompassSyncResultViewModel
            {
                DryRun = request.DryRun,
                FailedCount = 1,
                Results =
                [
                    new CmsCompassSyncProductResult
                    {
                        Success = false,
                        Errors = [$"Could not load CMS data: {ex.Message}"]
                    }
                ]
            };
        }

        var cmsByCmdb = cmsProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.CmdbSysId))
            .GroupBy(p => p.CmdbSysId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var compassProducts = await _db.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas)
            .Include(p => p.Channels)
            .Include(p => p.UserGroups)
            .Include(p => p.Types)
            .Include(p => p.CategorisationItems)
            .Where(p => p.Status == CMDBProductStatus.Active && p.CMDBID != null)
            .ToListAsync(cancellationToken);

        var compassByCmdb = compassProducts
            .GroupBy(p => p.CMDBID!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var cmdbId in cmdbIds)
        {
            var line = new CmsCompassSyncProductResult { CmdbId = cmdbId };

            if (!cmsByCmdb.TryGetValue(cmdbId, out var cms))
            {
                line.Success = false;
                line.Errors = ["No live CMS product with this CMDB ID."];
                productResults.Add(line);
                continue;
            }

            if (!compassByCmdb.TryGetValue(cmdbId, out var compass))
            {
                line.Success = false;
                line.Errors = ["No active COMPASS product with this CMDB ID."];
                productResults.Add(line);
                continue;
            }

            line.ProductTitle = compass.Title;

            var resolve = BuildResolver(request.Mappings);
            var errors = new List<string>();

            int? targetPhaseId = compass.PhaseId;
            if (request.SyncPhase)
            {
                var cmsPhase = CmsCompassMappingHelper.GetCmsCategoryName(cms, "Phase");
                if (!string.IsNullOrWhiteSpace(cmsPhase))
                {
                    if (!resolve.TryResolve(CmsCompassMappingHelper.FieldPhase, cmsPhase, out var phaseId, out var phaseError))
                    {
                        errors.Add(phaseError!);
                    }
                    else
                    {
                        targetPhaseId = phaseId;
                    }
                }
            }

            int[] targetChannels = compass.Channels.Select(c => c.FipsChannelId).ToArray();
            if (request.SyncChannel)
            {
                var cmsChannelNames = CmsCompassMappingHelper.GetCmsCategoryNames(cms, "Channel");
                if (cmsChannelNames.Count > 0)
                {
                    if (!TryResolveMulti(cms, resolve, CmsCompassMappingHelper.FieldChannel, "Channel", Array.Empty<string>(), out var channelIds, errors))
                    {
                        // errors already added
                    }
                    else
                    {
                        targetChannels = channelIds;
                    }
                }
            }

            int[] targetTypes = compass.Types.Select(t => t.FipsTypeId).ToArray();
            if (request.SyncType)
            {
                var cmsTypeNames = CmsCompassMappingHelper.GetCmsCategoryNames(cms, "Type");
                if (cmsTypeNames.Count > 0)
                {
                    if (!TryResolveMulti(cms, resolve, CmsCompassMappingHelper.FieldType, "Type", Array.Empty<string>(), out var typeIds, errors))
                    {
                        // errors already added
                    }
                    else
                    {
                        targetTypes = typeIds;
                    }
                }
            }

            int[] targetBusinessAreas = compass.BusinessAreas.Select(b => b.FipsBusinessAreaId).ToArray();
            if (request.SyncBusinessArea)
            {
                var cmsBaNames = CmsCompassMappingHelper.GetCmsCategoryNames(cms, "Business area", "Business Area");
                if (cmsBaNames.Count > 0)
                {
                    if (!TryResolveMulti(cms, resolve, CmsCompassMappingHelper.FieldBusinessArea, "Business area", new[] { "Business Area" }, out var baIds, errors))
                    {
                        // errors already added
                    }
                    else
                    {
                        targetBusinessAreas = baIds;
                    }
                }
            }

            int[] targetUserGroups = compass.UserGroups.Select(u => u.FipsUserGroupId).ToArray();
            if (request.SyncUserGroup)
            {
                var cmsUgNames = CmsCompassMappingHelper.GetCmsCategoryNames(cms, "User group", "User Group", "User groups", "User Groups");
                if (cmsUgNames.Count > 0)
                {
                    if (!TryResolveMulti(cms, resolve, CmsCompassMappingHelper.FieldUserGroup, "User group", new[] { "User Group", "User groups", "User Groups" }, out var ugIds, errors))
                    {
                        // errors already added
                    }
                    else
                    {
                        targetUserGroups = ugIds;
                    }
                }
            }

            if (errors.Count > 0)
            {
                line.Success = false;
                line.Errors = errors;
                productResults.Add(line);
                continue;
            }

            if (request.DryRun)
            {
                var wouldChange = new List<string>();
                if (request.SyncPhase && compass.PhaseId != targetPhaseId)
                    wouldChange.Add("Phase");
                if (request.SyncChannel && !SetEquals(compass.Channels.Select(c => c.FipsChannelId), targetChannels))
                    wouldChange.Add("Channel(s)");
                if (request.SyncType && !SetEquals(compass.Types.Select(t => t.FipsTypeId), targetTypes))
                    wouldChange.Add("Type(s)");
                if (request.SyncBusinessArea && !SetEquals(compass.BusinessAreas.Select(b => b.FipsBusinessAreaId), targetBusinessAreas))
                    wouldChange.Add("Business area(s)");
                if (request.SyncUserGroup && !SetEquals(compass.UserGroups.Select(u => u.FipsUserGroupId), targetUserGroups))
                    wouldChange.Add("User group(s)");

                if (wouldChange.Count == 0)
                {
                    line.Skipped = true;
                    line.SkipReason = "Already matches CMS for the selected fields.";
                    line.Success = true;
                }
                else
                {
                    line.Success = true;
                    line.Changes = wouldChange;
                }

                productResults.Add(line);
                continue;
            }

            var outcome = await _fipsProductWrite.TryUpdateAsync(
                compass.Id,
                actorEmail,
                auditDisplayName,
                requireServiceOwnerManager: false,
                userDescription: compass.UserDescription,
                phaseId: targetPhaseId,
                productURL: compass.ProductURL,
                businessAreaIds: targetBusinessAreas,
                channelIds: targetChannels,
                userGroupIds: targetUserGroups,
                typeIds: targetTypes,
                directorateIds: null,
                categorisationItemIds: compass.CategorisationItems.Select(c => c.FipsCategorisationItemId).ToArray(),
                cancellationToken: cancellationToken);

            if (outcome.NotFound)
            {
                line.Success = false;
                line.Errors = ["COMPASS product not found."];
            }
            else if (outcome.Forbidden)
            {
                line.Success = false;
                line.Errors = ["Not permitted to update this product."];
            }
            else if (outcome.Changes.Count == 0)
            {
                line.Skipped = true;
                line.SkipReason = "Already matches CMS for the selected fields.";
                line.Success = true;
            }
            else
            {
                line.Success = true;
                line.Changes = outcome.Changes;
            }

            productResults.Add(line);
        }

        return new CmsCompassSyncResultViewModel
        {
            DryRun = request.DryRun,
            UpdatedCount = productResults.Count(r => r.Success && !r.Skipped && r.Changes.Count > 0),
            SkippedCount = productResults.Count(r => r.Skipped),
            FailedCount = productResults.Count(r => !r.Success),
            Results = productResults
        };
    }

    private static MappingResolver BuildResolver(
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, int?>> mappings) =>
        new(mappings);

    private static bool TryResolveMulti(
        ProductDto cms,
        MappingResolver resolver,
        string fieldKey,
        string firstTypeName,
        IReadOnlyList<string> extraTypeNames,
        out int[] ids,
        ICollection<string> errors)
    {
        ids = [];
        var typeNames = new List<string> { firstTypeName };
        typeNames.AddRange(extraTypeNames);
        var cmsNames = CmsCompassMappingHelper.GetCmsCategoryNames(cms, typeNames.ToArray());
        if (cmsNames.Count == 0)
        {
            return true;
        }

        var resolved = new List<int>();
        foreach (var cmsName in cmsNames)
        {
            if (!resolver.TryResolve(fieldKey, cmsName, out var compassId, out var error))
            {
                errors.Add(error!);
                return false;
            }

            if (compassId.HasValue)
                resolved.Add(compassId.Value);
        }

        ids = resolved.Distinct().OrderBy(x => x).ToArray();
        return true;
    }

    private static bool SetEquals(IEnumerable<int> left, IReadOnlyList<int> right)
    {
        var l = left.OrderBy(x => x).ToList();
        var r = right.OrderBy(x => x).ToList();
        return l.SequenceEqual(r);
    }

    private sealed class MappingResolver
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int?>> _mappings;

        public MappingResolver(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int?>> mappings)
        {
            _mappings = mappings;
        }

        public bool TryResolve(string fieldKey, string cmsValueName, out int? compassId, out string? error)
        {
            compassId = null;
            error = null;
            var trimmed = cmsValueName.Trim();

            if (_mappings.TryGetValue(fieldKey, out var fieldMap))
            {
                var match = fieldMap.FirstOrDefault(kv =>
                    string.Equals(kv.Key, trimmed, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key))
                {
                    if (!match.Value.HasValue)
                    {
                        error = $"CMS value '{trimmed}' is not mapped to a COMPASS {CmsCompassMappingHelper.FieldLabel(fieldKey)} value.";
                        return false;
                    }

                    compassId = match.Value;
                    return true;
                }
            }

            error = $"No mapping configured for CMS {CmsCompassMappingHelper.FieldLabel(fieldKey)} value '{trimmed}'.";
            return false;
        }
    }
}

internal static class CmsCompassMappingHelper
{
    internal const string FieldPhase = "phase";
    internal const string FieldChannel = "channel";
    internal const string FieldType = "type";
    internal const string FieldBusinessArea = "businessArea";
    internal const string FieldUserGroup = "userGroup";

    internal static string FieldLabel(string fieldKey) => fieldKey switch
    {
        FieldPhase => "phase",
        FieldChannel => "channel",
        FieldType => "type",
        FieldBusinessArea => "business area",
        FieldUserGroup => "user group",
        _ => fieldKey
    };

    internal static string CompassLookupLabel(string fieldKey) => fieldKey switch
    {
        FieldPhase => "Phase lookup (admin → Phases)",
        FieldChannel => "FIPS channel",
        FieldType => "FIPS type",
        FieldBusinessArea => "FIPS business area",
        FieldUserGroup => "FIPS user group",
        _ => fieldKey
    };

    internal static readonly (string FieldKey, string FieldLabel, string[] CmsTypeNames)[] SyncFields =
    [
        (FieldPhase, "Phase", ["Phase"]),
        (FieldChannel, "Channel", ["Channel"]),
        (FieldType, "Type", ["Type"]),
        (FieldBusinessArea, "Business area", ["Business area", "Business Area"]),
        (FieldUserGroup, "User group", ["User group", "User Group", "User groups", "User Groups"])
    ];

    internal static string? GetCmsCategoryName(ProductDto? product, params string[] typeNames)
    {
        var names = GetCmsCategoryNames(product, typeNames);
        return names.FirstOrDefault();
    }

    internal static IReadOnlyList<string> GetCmsCategoryNames(ProductDto? product, params string[] typeNames)
    {
        if (product?.CategoryValues == null)
            return [];

        var typeSet = typeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return product.CategoryValues
            .Where(cv => cv.CategoryType?.Name != null && typeSet.Contains(cv.CategoryType.Name.Trim()))
            .Select(cv => cv.Name.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static List<string> CollectCmsValuesInUse(
        IEnumerable<ProductDto> cmsProducts,
        params string[] typeNames)
    {
        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in cmsProducts)
        {
            foreach (var name in GetCmsCategoryNames(product, typeNames))
                values.Add(name);
        }

        return values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal static List<string> MergeCmsValueNames(
        IEnumerable<ProductDto> cmsProducts,
        Dictionary<string, List<CategoryValueDto>> cmsCategoriesByType,
        params string[] typeNames)
    {
        var values = CollectCmsValuesInUse(cmsProducts, typeNames).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var typeName in typeNames)
        {
            var fromLookup = cmsCategoriesByType
                .FirstOrDefault(kv => string.Equals(kv.Key, typeName, StringComparison.OrdinalIgnoreCase))
                .Value;
            if (fromLookup == null)
                continue;
            foreach (var cv in fromLookup)
            {
                if (!string.IsNullOrWhiteSpace(cv.Name))
                    values.Add(cv.Name.Trim());
            }
        }

        return values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
