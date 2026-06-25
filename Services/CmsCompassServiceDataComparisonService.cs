using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Compass.Services;

public sealed class CmsCompassServiceDataComparisonService : ICmsCompassServiceDataComparisonService
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    private readonly CompassDbContext _db;
    private readonly IProductsApiService _productsApi;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CmsCompassServiceDataComparisonService> _logger;

    public CmsCompassServiceDataComparisonService(
        CompassDbContext db,
        IProductsApiService productsApi,
        IConfiguration configuration,
        ILogger<CmsCompassServiceDataComparisonService> logger)
    {
        _db = db;
        _productsApi = productsApi;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CmsCompassServiceDataViewModel> BuildReportAsync(CancellationToken cancellationToken = default)
    {
        var vm = new CmsCompassServiceDataViewModel
        {
            CmsApiBaseUrl = _configuration["CmsApi:BaseUrl"]?.Trim() ?? "",
            GeneratedAtUtc = DateTime.UtcNow
        };

        List<ProductDto> cmsProducts;
        Dictionary<string, List<CategoryValueDto>> cmsCategoriesByType;
        try
        {
            cmsProducts = await _productsApi.GetAllProductsAsync();
            cmsCategoriesByType = await _productsApi.GetAllCategoryValuesByTypeAsync();
            vm.CmsFetchSucceeded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch CMS data for service data comparison");
            vm.CmsFetchError = ex.Message;
            return vm;
        }

        var liveCms = cmsProducts
            .Where(IsLiveCmsProduct)
            .ToList();

        var compassProducts = await _db.CMDBProducts.AsNoTracking()
            .Include(p => p.Phase)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.UserGroups).ThenInclude(u => u.FipsUserGroup)
            .Include(p => p.BusinessAreas).ThenInclude(b => b.FipsBusinessArea)
            .Where(p => p.Status == CMDBProductStatus.Active)
            .ToListAsync(cancellationToken);

        var liveCmsWithCmdb = liveCms
            .Where(p => !string.IsNullOrWhiteSpace(p.CmdbSysId))
            .GroupBy(p => p.CmdbSysId!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var liveCompassWithCmdb = compassProducts
            .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
            .GroupBy(p => p.CMDBID!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        vm.LookupComparisons = await BuildLookupComparisonsAsync(cmsCategoriesByType, cancellationToken);
        vm.ValueMappingGroups = await BuildValueMappingGroupsAsync(liveCms, cmsCategoriesByType, cancellationToken);
        vm.Products = BuildProductRows(liveCmsWithCmdb, liveCompassWithCmdb);

        vm.Summary = new CmsCompassServiceDataSummary
        {
            CmsLiveWithCmdbId = liveCmsWithCmdb.Count,
            CompassLiveWithCmdbId = liveCompassWithCmdb.Count,
            CmsLiveMissingCmdbId = liveCms.Count(p => string.IsNullOrWhiteSpace(p.CmdbSysId)),
            CompassLiveMissingCmdbId = compassProducts.Count(p => string.IsNullOrWhiteSpace(p.CMDBID)),
            Matched = vm.Products.Count(p =>
                p.MatchStatus is CmsCompassProductMatchStatus.MatchedIdentical
                    or CmsCompassProductMatchStatus.MatchedWithDifferences),
            MatchedIdentical = vm.Products.Count(p => p.MatchStatus == CmsCompassProductMatchStatus.MatchedIdentical),
            MatchedWithDifferences = vm.Products.Count(p =>
                p.MatchStatus == CmsCompassProductMatchStatus.MatchedWithDifferences),
            CmsOnly = vm.Products.Count(p => p.MatchStatus == CmsCompassProductMatchStatus.CmsOnly),
            CompassOnly = vm.Products.Count(p => p.MatchStatus == CmsCompassProductMatchStatus.CompassOnly)
        };

        return vm;
    }

    private async Task<IReadOnlyList<CmsCompassValueMappingGroup>> BuildValueMappingGroupsAsync(
        IReadOnlyList<ProductDto> liveCmsProducts,
        Dictionary<string, List<CategoryValueDto>> cmsCategoriesByType,
        CancellationToken cancellationToken)
    {
        var phaseOptions = await _db.PhaseLookups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Select(x => new CmsCompassLookupOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
        var channelOptions = await _db.FipsChannels.AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CmsCompassLookupOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
        var typeOptions = await _db.FipsTypes.AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CmsCompassLookupOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
        var businessAreaOptions = await _db.FipsBusinessAreas.AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CmsCompassLookupOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);
        var userGroupOptions = await _db.FipsUserGroups.AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CmsCompassLookupOption { Id = x.Id, Name = x.Name })
            .ToListAsync(cancellationToken);

        var groups = new List<CmsCompassValueMappingGroup>();
        foreach (var field in CmsCompassMappingHelper.SyncFields)
        {
            var options = field.FieldKey switch
            {
                CmsCompassMappingHelper.FieldPhase => phaseOptions,
                CmsCompassMappingHelper.FieldChannel => channelOptions,
                CmsCompassMappingHelper.FieldType => typeOptions,
                CmsCompassMappingHelper.FieldBusinessArea => businessAreaOptions,
                CmsCompassMappingHelper.FieldUserGroup => userGroupOptions,
                _ => []
            };

            var cmsValues = CmsCompassMappingHelper.MergeCmsValueNames(
                liveCmsProducts, cmsCategoriesByType, field.CmsTypeNames);
            var optionByName = options.ToDictionary(o => o.Name.Trim(), o => o, NameComparer);

            var rows = cmsValues.Select(cmsName =>
            {
                optionByName.TryGetValue(cmsName, out var exact);
                return new CmsCompassValueMappingRow
                {
                    FieldKey = field.FieldKey,
                    CmsValueName = cmsName,
                    SuggestedCompassId = exact?.Id,
                    SuggestedCompassName = exact?.Name,
                    HasExactNameMatch = exact != null,
                    CompassOptions = options
                };
            }).ToList();

            groups.Add(new CmsCompassValueMappingGroup
            {
                FieldKey = field.FieldKey,
                FieldLabel = field.FieldLabel,
                CompassLookupLabel = CmsCompassMappingHelper.CompassLookupLabel(field.FieldKey),
                Rows = rows
            });
        }

        return groups;
    }

    private async Task<IReadOnlyList<CmsCompassLookupComparisonRow>> BuildLookupComparisonsAsync(
        Dictionary<string, List<CategoryValueDto>> cmsCategoriesByType,
        CancellationToken cancellationToken)
    {
        var phaseCms = GetEnabledCmsNames(cmsCategoriesByType, "Phase");
        var channelCms = GetEnabledCmsNames(cmsCategoriesByType, "Channel");
        var typeCms = GetEnabledCmsNames(cmsCategoriesByType, "Type");
        var userGroupCms = GetEnabledCmsNames(cmsCategoriesByType, "User group", "User Group", "User groups", "User Groups");
        var businessAreaCms = GetEnabledCmsNames(cmsCategoriesByType, "Business area", "Business Area");

        var phaseCompass = await _db.PhaseLookups.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Name.Trim())
            .ToListAsync(cancellationToken);
        var channelCompass = await _db.FipsChannels.AsNoTracking()
            .Where(x => x.Active)
            .Select(x => x.Name.Trim())
            .ToListAsync(cancellationToken);
        var typeCompass = await _db.FipsTypes.AsNoTracking()
            .Where(x => x.Active)
            .Select(x => x.Name.Trim())
            .ToListAsync(cancellationToken);
        var userGroupCompass = await _db.FipsUserGroups.AsNoTracking()
            .Where(x => x.Active)
            .Select(x => x.Name.Trim())
            .ToListAsync(cancellationToken);
        var businessAreaCompass = await _db.FipsBusinessAreas.AsNoTracking()
            .Where(x => x.Active)
            .Select(x => x.Name.Trim())
            .ToListAsync(cancellationToken);

        return
        [
            CompareLookup("Phase", phaseCms, phaseCompass),
            CompareLookup("Channel", channelCms, channelCompass),
            CompareLookup("Type", typeCms, typeCompass),
            CompareLookup("User group", userGroupCms, userGroupCompass),
            CompareLookup("Business area", businessAreaCms, businessAreaCompass)
        ];
    }

    private static CmsCompassLookupComparisonRow CompareLookup(
        string label,
        IReadOnlyList<string> cmsNames,
        IReadOnlyList<string> compassNames)
    {
        var cmsSet = cmsNames.ToHashSet(NameComparer);
        var compassSet = compassNames.ToHashSet(NameComparer);
        return new CmsCompassLookupComparisonRow
        {
            Label = label,
            CmsCount = cmsSet.Count,
            CompassCount = compassSet.Count,
            CmsOnlyNames = cmsSet.Except(compassSet, NameComparer).OrderBy(x => x, NameComparer).ToList(),
            CompassOnlyNames = compassSet.Except(cmsSet, NameComparer).OrderBy(x => x, NameComparer).ToList()
        };
    }

    private static List<string> GetEnabledCmsNames(
        Dictionary<string, List<CategoryValueDto>> cmsCategoriesByType,
        params string[] typeNames)
    {
        foreach (var typeName in typeNames)
        {
            var values = cmsCategoriesByType
                .FirstOrDefault(kv => string.Equals(kv.Key, typeName, StringComparison.OrdinalIgnoreCase))
                .Value;
            if (values == null)
                continue;
            return values
                .Select(v => v.Name.Trim())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(NameComparer)
                .OrderBy(n => n, NameComparer)
                .ToList();
        }

        return [];
    }

    private static IReadOnlyList<CmsCompassProductComparisonRow> BuildProductRows(
        Dictionary<string, ProductDto> cmsByCmdb,
        Dictionary<string, CMDBProduct> compassByCmdb)
    {
        var allCmdbIds = cmsByCmdb.Keys
            .Union(compassByCmdb.Keys, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        var rows = new List<CmsCompassProductComparisonRow>();
        foreach (var cmdbId in allCmdbIds)
        {
            cmsByCmdb.TryGetValue(cmdbId, out var cms);
            compassByCmdb.TryGetValue(cmdbId, out var compass);
            rows.Add(BuildProductRow(cmdbId, cms, compass));
        }

        return rows;
    }

    private static CmsCompassProductComparisonRow BuildProductRow(
        string cmdbId,
        ProductDto? cms,
        CMDBProduct? compass)
    {
        var cmsPhase = GetCmsCategoryName(cms, "Phase");
        var cmsChannels = JoinNames(GetCmsCategoryNames(cms, "Channel"));
        var cmsTypes = JoinNames(GetCmsCategoryNames(cms, "Type"));
        var cmsUserGroups = JoinNames(GetCmsCategoryNames(cms, "User group", "User Group", "User groups", "User Groups"));
        var cmsBusinessAreas = JoinNames(GetCmsCategoryNames(cms, "Business area", "Business Area"));

        var compassPhase = compass?.Phase?.Name?.Trim();
        var compassChannels = JoinNames(compass?.Channels.Select(c => c.FipsChannel.Name) ?? []);
        var compassTypes = JoinNames(compass?.Types.Select(t => t.FipsType.Name) ?? []);
        var compassUserGroups = JoinNames(compass?.UserGroups.Select(u => u.FipsUserGroup.Name) ?? []);
        var compassBusinessAreas = JoinNames(compass?.BusinessAreas.Select(b => b.FipsBusinessArea.Name) ?? []);

        var row = new CmsCompassProductComparisonRow
        {
            CmdbId = cmdbId,
            CompassProductId = compass?.Id,
            CmsTitle = cms?.Title?.Trim(),
            CompassTitle = compass?.Title?.Trim(),
            CmsFipsId = cms?.FipsId?.Trim(),
            CmsState = cms?.State?.Trim(),
            CompassStatus = compass?.Status.ToString(),
            CmsProductUrl = NormalizeOptional(cms?.ProductUrl),
            CompassProductUrl = NormalizeOptional(compass?.ProductURL),
            CmsPhase = cmsPhase,
            CompassPhase = compassPhase,
            CmsChannels = cmsChannels,
            CompassChannels = compassChannels,
            CmsTypes = cmsTypes,
            CompassTypes = compassTypes,
            CmsUserGroups = cmsUserGroups,
            CompassUserGroups = compassUserGroups,
            CmsBusinessAreas = cmsBusinessAreas,
            CompassBusinessAreas = compassBusinessAreas
        };

        if (cms == null)
        {
            row.MatchStatus = CmsCompassProductMatchStatus.CompassOnly;
            return row;
        }

        if (compass == null)
        {
            row.MatchStatus = CmsCompassProductMatchStatus.CmsOnly;
            return row;
        }

        var differences = new List<string>();
        if (!StringEquals(cms?.Title, compass.Title))
            differences.Add("Title");
        if (!StringEquals(NormalizeOptional(cms?.ProductUrl), NormalizeOptional(compass.ProductURL)))
            differences.Add("Product URL");
        if (!StringEquals(cmsPhase, compassPhase))
            differences.Add("Phase");
        if (!SetEquals(cmsChannels, compassChannels))
            differences.Add("Channel(s)");
        if (!SetEquals(cmsTypes, compassTypes))
            differences.Add("Type(s)");
        if (!SetEquals(cmsUserGroups, compassUserGroups))
            differences.Add("User group(s)");
        if (!SetEquals(cmsBusinessAreas, compassBusinessAreas))
            differences.Add("Business area(s)");

        row.Differences = differences;
        row.MatchStatus = differences.Count == 0
            ? CmsCompassProductMatchStatus.MatchedIdentical
            : CmsCompassProductMatchStatus.MatchedWithDifferences;

        return row;
    }

    private static bool IsLiveCmsProduct(ProductDto product) =>
        string.Equals(product.State, "Active", StringComparison.OrdinalIgnoreCase)
        && product.PublishedAt.HasValue;

    private static string? GetCmsCategoryName(ProductDto? product, params string[] typeNames)
    {
        var names = GetCmsCategoryNames(product, typeNames);
        return names.FirstOrDefault();
    }

    private static IReadOnlyList<string> GetCmsCategoryNames(ProductDto? product, params string[] typeNames)
    {
        if (product?.CategoryValues == null)
            return [];

        var typeSet = typeNames.ToHashSet(NameComparer);
        return product.CategoryValues
            .Where(cv => cv.CategoryType?.Name != null && typeSet.Contains(cv.CategoryType.Name.Trim()))
            .Select(cv => cv.Name.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(NameComparer)
            .OrderBy(n => n, NameComparer)
            .ToList();
    }

    private static string JoinNames(IEnumerable<string> names) =>
        string.Join(", ", names
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(NameComparer)
            .OrderBy(n => n, NameComparer));

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static bool StringEquals(string? left, string? right) =>
        string.Equals(NormalizeOptional(left), NormalizeOptional(right), StringComparison.OrdinalIgnoreCase);

    private static bool SetEquals(string leftJoined, string rightJoined)
    {
        var left = SplitNames(leftJoined).OrderBy(x => x, NameComparer).ToList();
        var right = SplitNames(rightJoined).OrderBy(x => x, NameComparer).ToList();
        return left.SequenceEqual(right, NameComparer);
    }

    private static HashSet<string> SplitNames(string joined) =>
        joined.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(NameComparer);
}
