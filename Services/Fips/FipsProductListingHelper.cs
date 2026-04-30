using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>Shared product list + search/filter for Service Register (manage + operations).</summary>
public static class FipsProductListingHelper
{
    public static async Task<FipsProductsViewModel> BuildProductsViewModelAsync(
        CompassDbContext context,
        string activeTab,
        string currentUserEmail,
        string? search,
        int? businessAreaId,
        int? channelId,
        int? userGroupId,
        int? typeId,
        int? phaseId,
        CancellationToken cancellationToken = default)
    {
        var email = currentUserEmail ?? string.Empty;
        var emailNorm = email.Trim().ToLower();
        var useSqlServer = IsSqlServerProvider(context);

        var totalProductsInDatabase = await TryCountCmdbProductsRowsAsync(context, cancellationToken);

        // Do not UseInclude on CMDBProducts for the list: materializing the full entity SELECTs every mapped
        // column (e.g. IsEnterpriseService). A missing or mismatched column then fails the whole list.
        var query = context.CMDBProducts.AsNoTracking().AsQueryable();

        // "Your products": match contacts case-insensitively. EF Core cannot reliably translate ToLower on Azure SQL — use raw SQL on SQL Server.
        List<Guid>? myProductIdsForTab = null;
        int myCount;
        var onMyTab = string.Equals(activeTab, "my", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(emailNorm))
        {
            myCount = 0;
        }
        else if (useSqlServer)
        {
            try
            {
                if (onMyTab)
                {
                    myProductIdsForTab = await context.Database
                        .SqlQuery<Guid>($"""
                            SELECT DISTINCT p.Id AS Value
                            FROM CMDBProducts AS p
                            INNER JOIN CMDBProductContacts AS c ON c.CMDBProductId = p.Id
                            WHERE p.[Status] NOT IN ({(int)CMDBProductStatus.Inactive}, {(int)CMDBProductStatus.Rejected})
                              AND c.UserEmail IS NOT NULL
                              AND LOWER(LTRIM(RTRIM(c.UserEmail))) = {emailNorm}
                            """)
                        .ToListAsync(cancellationToken);
                    myCount = myProductIdsForTab.Count;
                }
                else
                {
                    myCount = await context.Database
                        .SqlQuery<int>($"""
                            SELECT COUNT(DISTINCT p.Id) AS Value
                            FROM CMDBProducts AS p
                            INNER JOIN CMDBProductContacts AS c ON c.CMDBProductId = p.Id
                            WHERE p.[Status] NOT IN ({(int)CMDBProductStatus.Inactive}, {(int)CMDBProductStatus.Rejected})
                              AND c.UserEmail IS NOT NULL
                              AND LOWER(LTRIM(RTRIM(c.UserEmail))) = {emailNorm}
                            """)
                        .FirstAsync(cancellationToken);
                }
            }
            catch
            {
                myProductIdsForTab = onMyTab ? [] : null;
                myCount = 0;
            }
        }
        else
        {
            try
            {
                myCount = await context.CMDBProducts
                    .Where(p => p.Status != CMDBProductStatus.Inactive &&
                                p.Status != CMDBProductStatus.Rejected &&
                                context.CMDBProductContacts.Any(c =>
                                    c.CMDBProductId == p.Id &&
                                    c.UserEmail != null &&
                                    c.UserEmail.ToLower() == emailNorm))
                    .CountAsync(cancellationToken);
            }
            catch
            {
                myCount = 0;
            }
        }

        IQueryable<CMDBProduct> tabQuery = activeTab switch
        {
            "my" when string.IsNullOrWhiteSpace(emailNorm) => query.Where(_ => false),
            "my" when useSqlServer => myProductIdsForTab is { Count: > 0 }
                ? query.Where(p => myProductIdsForTab.Contains(p.Id))
                : query.Where(_ => false),
            "my" => query.Where(p => p.Status != CMDBProductStatus.Inactive &&
                                     p.Status != CMDBProductStatus.Rejected &&
                                     context.CMDBProductContacts.Any(c =>
                                         c.CMDBProductId == p.Id &&
                                         c.UserEmail != null &&
                                         c.UserEmail.ToLower() == emailNorm)),
            "all" => query,
            "new" => query.Where(p => p.Status == CMDBProductStatus.New),
            "inactive" => query.Where(p => p.Status == CMDBProductStatus.Rejected),
            "retired" => query.Where(p => p.Status == CMDBProductStatus.Inactive),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            tabQuery = tabQuery.Where(p =>
                p.Title.Contains(s) ||
                (p.CMDBID != null && p.CMDBID.Contains(s)) ||
                (p.CMDBDescription != null && p.CMDBDescription.Contains(s)));
        }

        if (businessAreaId is > 0)
            tabQuery = tabQuery.Where(p => p.BusinessAreas.Any(ba => ba.FipsBusinessAreaId == businessAreaId.Value));
        if (channelId is > 0)
            tabQuery = tabQuery.Where(p => p.Channels.Any(c => c.FipsChannelId == channelId.Value));
        if (userGroupId is > 0)
            tabQuery = tabQuery.Where(p => p.UserGroups.Any(ug => ug.FipsUserGroupId == userGroupId.Value));
        if (typeId is > 0)
            tabQuery = tabQuery.Where(p => p.Types.Any(t => t.FipsTypeId == typeId.Value));
        if (phaseId is > 0)
            tabQuery = tabQuery.Where(p => p.PhaseId == phaseId.Value);

        var products = await LoadFipsProductRowsAsync(tabQuery, context, cancellationToken);

        var businessAreaLookups = await context.BusinessAreaLookups
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var allProductsCount = 0;
        var newProductsCount = 0;
        var inactiveProductsCount = 0;
        var retiredCount = 0;
        try
        {
            var allCounts = await context.CMDBProducts
                .AsNoTracking()
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            allProductsCount = allCounts.Sum(c => c.Count);
            newProductsCount = allCounts.Where(c => c.Status == CMDBProductStatus.New).Sum(c => c.Count);
            inactiveProductsCount = allCounts.Where(c => c.Status == CMDBProductStatus.Rejected).Sum(c => c.Count);
            retiredCount = allCounts.Where(c => c.Status == CMDBProductStatus.Inactive).Sum(c => c.Count);
        }
        catch
        {
            // Count may fail if Status column is unavailable; leave zeros.
        }

        return new FipsProductsViewModel
        {
            ActiveTab = activeTab,
            Search = search,
            BusinessAreaId = businessAreaId,
            ChannelId = channelId,
            UserGroupId = userGroupId,
            TypeId = typeId,
            PhaseId = phaseId,
            TotalProductsInDatabase = totalProductsInDatabase,
            MyProductsCount = myCount,
            AllProductsCount = allProductsCount,
            NewProductsCount = newProductsCount,
            InactiveProductsCount = inactiveProductsCount,
            RetiredCount = retiredCount,
            Products = products,
            BusinessAreaOptions = await context.FipsBusinessAreas.Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            ChannelOptions = await context.FipsChannels.Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            UserGroupOptions = await context.FipsUserGroups.Where(x => x.Active && x.ParentId == null).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            TypeOptions = await context.FipsTypes.Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            PhaseOptions = await context.PhaseLookups.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken),
            BusinessAreaLookups = businessAreaLookups,
        };
    }

    /// <summary>
    /// Loads list rows with a <see cref="CMDBProduct"/> projection that only references needed columns, then
    /// enriches from junction tables. Avoids <c>SELECT *</c> style materialization of the product entity.
    /// </summary>
    private static async Task<List<FipsProductRow>> LoadFipsProductRowsAsync(
        IQueryable<CMDBProduct> tabQuery,
        CompassDbContext context,
        CancellationToken ct)
    {
        var core = await tabQuery
            .OrderBy(p => p.Title)
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                p.CMDBDescription,
                p.UserDescription,
                p.Status,
                p.PhaseId,
                PhaseName = p.Phase != null ? p.Phase.Name : null,
                QualityScore =
                    (p.PhaseId != null ? 1 : 0)
                    + (p.Types.Any() ? 1 : 0)
                    + (p.BusinessAreas.Any() ? 1 : 0)
                    + (p.UserGroups.Any() ? 1 : 0)
                    + (p.Channels.Any() ? 1 : 0)
                    + (p.Contacts.Any(c => c.FipsContactRole != null && c.FipsContactRole.Name == "Service Owner")
                        ? 1
                        : 0)
                    + (p.Contacts.Any(c =>
                            c.FipsContactRole != null && c.FipsContactRole.Name == "Senior Responsible Officer")
                        ? 1
                        : 0),
                ServiceOwner = p.Contacts
                    .Where(c => c.FipsContactRole != null && c.FipsContactRole.Name == "Service Owner")
                    .Select(c => c.UserName ?? c.UserEmail ?? "")
                    .FirstOrDefault(),
                ContactCount = p.Contacts.Count(),
                UserGroupCount = p.UserGroups.Count(),
            })
            .ToListAsync(ct);

        if (core.Count == 0)
            return [];

        var ids = core.Select(x => x.Id).Distinct().ToList();

        var baLookup = await LoadBusinessAreaDisplayAsync(context, ids, ct);
        var typesLookup = await LoadTypesDisplayAsync(context, ids, ct);
        var channelsLookup = await LoadChannelsDisplayAsync(context, ids, ct);
        var reportingLookup = await LoadReportingContactDisplayAsync(context, ids, ct);

        return core.Select(row => new FipsProductRow
        {
            Id = row.Id,
            UniqueID = row.UniqueID,
            Title = row.Title,
            CMDBDescription = row.CMDBDescription,
            UserDescription = row.UserDescription,
            Status = row.Status,
            PhaseName = row.PhaseName,
            BusinessAreaDisplay = baLookup.GetValueOrDefault(row.Id),
            TypesDisplay = typesLookup.GetValueOrDefault(row.Id),
            ChannelsDisplay = channelsLookup.GetValueOrDefault(row.Id),
            UserGroupCount = row.UserGroupCount,
            ContactCount = row.ContactCount,
            ServiceOwner = string.IsNullOrWhiteSpace(row.ServiceOwner) ? null : row.ServiceOwner,
            ReportingContact = reportingLookup.GetValueOrDefault(row.Id),
            QualityScore = row.QualityScore,
            QualityScoreMax = 7,
        }).ToList();
    }

    private static async Task<Dictionary<Guid, string?>> LoadBusinessAreaDisplayAsync(
        CompassDbContext context,
        List<Guid> ids,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, string?>();
        foreach (var chunk in ids.Distinct().Chunk(2000))
        {
            var chunkArr = chunk.ToArray();
            var rows = await context.CMDBProductBusinessAreas.AsNoTracking()
                .Where(b => chunkArr.Contains(b.CMDBProductId))
                .Select(b => new { b.CMDBProductId, Name = b.FipsBusinessArea.Name })
                .ToListAsync(ct);
            foreach (var g in rows.GroupBy(x => x.CMDBProductId))
                map[g.Key] = string.Join(", ", g.Select(x => x.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase));
        }

        return map;
    }

    private static async Task<Dictionary<Guid, string?>> LoadTypesDisplayAsync(
        CompassDbContext context,
        List<Guid> ids,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, string?>();
        foreach (var chunk in ids.Distinct().Chunk(2000))
        {
            var chunkArr = chunk.ToArray();
            var rows = await context.CMDBProductTypes.AsNoTracking()
                .Where(t => chunkArr.Contains(t.CMDBProductId))
                .Select(t => new { t.CMDBProductId, t.FipsType.DisplayOrder, Name = t.FipsType.Name })
                .ToListAsync(ct);
            foreach (var g in rows.GroupBy(x => x.CMDBProductId))
            {
                map[g.Key] = string.Join(
                    ", ",
                    g.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.Name));
            }
        }

        return map;
    }

    private static async Task<Dictionary<Guid, string?>> LoadChannelsDisplayAsync(
        CompassDbContext context,
        List<Guid> ids,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, string?>();
        foreach (var chunk in ids.Distinct().Chunk(2000))
        {
            var chunkArr = chunk.ToArray();
            var rows = await context.CMDBProductChannels.AsNoTracking()
                .Where(c => chunkArr.Contains(c.CMDBProductId))
                .Select(c => new { c.CMDBProductId, c.FipsChannel.DisplayOrder, Name = c.FipsChannel.Name })
                .ToListAsync(ct);
            foreach (var g in rows.GroupBy(x => x.CMDBProductId))
            {
                map[g.Key] = string.Join(
                    ", ",
                    g.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(x => x.Name));
            }
        }

        return map;
    }

    private static async Task<Dictionary<Guid, string?>> LoadReportingContactDisplayAsync(
        CompassDbContext context,
        List<Guid> ids,
        CancellationToken ct)
    {
        var map = new Dictionary<Guid, string?>();
        foreach (var chunk in ids.Distinct().Chunk(2000))
        {
            var chunkArr = chunk.ToArray();
            var rows = await context.CMDBProductContacts.AsNoTracking()
                .Where(c => chunkArr.Contains(c.CMDBProductId))
                .Where(c =>
                    c.FipsContactRole != null &&
                    (c.FipsContactRole.Name == "Reporting contact" || c.FipsContactRole.Name == "Reporting user"))
                .Select(c => new { c.CMDBProductId, Name = c.UserName ?? c.UserEmail })
                .ToListAsync(ct);
            foreach (var g in rows.GroupBy(x => x.CMDBProductId))
            {
                var parts = g.Select(x => x.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
                map[g.Key] = string.Join(", ", parts);
            }
        }

        return map;
    }

    private static bool IsSqlServerProvider(CompassDbContext context) =>
        context.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Row count without materializing <see cref="CMDBProduct"/> — survives missing optional columns on the entity.</summary>
    private static async Task<int> TryCountCmdbProductsRowsAsync(CompassDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (IsSqlServerProvider(context))
            {
                return await context.Database
                    .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM CMDBProducts")
                    .FirstAsync(cancellationToken);
            }

            return await context.CMDBProducts.AsNoTracking().CountAsync(cancellationToken);
        }
        catch
        {
            return 0;
        }
    }

    public static int CalculateQualityScore(CMDBProduct p)
    {
        var score = 0;
        if (p.PhaseId.HasValue) score++;
        if (p.Types.Count > 0) score++;
        if (p.BusinessAreas.Count > 0) score++;
        if (p.UserGroups.Count > 0) score++;
        if (p.Channels.Count > 0) score++;
        if (p.Contacts.Any(c => string.Equals(c.FipsContactRole?.Name, "Service Owner", StringComparison.OrdinalIgnoreCase))) score++;
        if (p.Contacts.Any(c => string.Equals(c.FipsContactRole?.Name, "Senior Responsible Officer", StringComparison.OrdinalIgnoreCase))) score++;
        return score;
    }

    public static SearchAndFilterViewModel BuildSearchAndFilter(FipsProductsViewModel vm, string tab, string formActionBaseUrl)
    {
        return new SearchAndFilterViewModel
        {
            IdPrefix = "fips",
            SearchPlaceholder = "Search products…",
            SearchValue = vm.Search,
            FormActionUrl = formActionBaseUrl,
            FormMethod = "get",
            ClearUrl = formActionBaseUrl,
            HiddenFields = new List<KeyValuePair<string, string>> { new("tab", tab) },
            Fields = new List<SearchAndFilterFieldViewModel>
            {
                new()
                {
                    Label = "Business area", Name = "businessAreaId",
                    SelectedValue = vm.BusinessAreaId?.ToString(),
                    Options = new List<SearchAndFilterOption> { new() { Value = "", Text = "All business areas" } }
                        .Concat(vm.BusinessAreaOptions.Select(x => new SearchAndFilterOption { Value = x.Id.ToString(), Text = x.Name })).ToList()
                },
                new()
                {
                    Label = "Channel", Name = "channelId",
                    SelectedValue = vm.ChannelId?.ToString(),
                    Options = new List<SearchAndFilterOption> { new() { Value = "", Text = "All channels" } }
                        .Concat(vm.ChannelOptions.Select(x => new SearchAndFilterOption { Value = x.Id.ToString(), Text = x.Name })).ToList()
                },
                new()
                {
                    Label = "User group", Name = "userGroupId",
                    SelectedValue = vm.UserGroupId?.ToString(),
                    Options = new List<SearchAndFilterOption> { new() { Value = "", Text = "All user groups" } }
                        .Concat(vm.UserGroupOptions.Select(x => new SearchAndFilterOption { Value = x.Id.ToString(), Text = x.Name })).ToList()
                },
                new()
                {
                    Label = "Type", Name = "typeId",
                    SelectedValue = vm.TypeId?.ToString(),
                    Options = new List<SearchAndFilterOption> { new() { Value = "", Text = "All types" } }
                        .Concat(vm.TypeOptions.Select(x => new SearchAndFilterOption { Value = x.Id.ToString(), Text = x.Name })).ToList()
                },
                new()
                {
                    Label = "Phase", Name = "phaseId",
                    SelectedValue = vm.PhaseId?.ToString(),
                    Options = new List<SearchAndFilterOption> { new() { Value = "", Text = "All phases" } }
                        .Concat(vm.PhaseOptions.Select(x => new SearchAndFilterOption { Value = x.Id.ToString(), Text = x.Name })).ToList()
                }
            }
        };
    }
}
