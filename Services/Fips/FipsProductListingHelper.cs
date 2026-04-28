using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>Shared product list + search/filter for Service Register (manage + operations).</summary>
public static class FipsProductListingHelper
{
    private static readonly string[] ReportingContactRoleNames = ["Reporting contact", "Reporting user"];

    public static async Task<FipsProductsViewModel> BuildProductsViewModelAsync(
        CompassDbContext context,
        IFipsBusinessAreaLookupSyncService fipsBusinessAreaLookupSync,
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
        await fipsBusinessAreaLookupSync.SyncFromBusinessAreaLookupsAsync(cancellationToken);

        var email = currentUserEmail;
        var query = context.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole)
            .AsNoTracking()
            .AsQueryable();

        IQueryable<CMDBProduct> tabQuery = activeTab switch
        {
            "my" => query.Where(p => p.Status != CMDBProductStatus.Inactive &&
                                     p.Status != CMDBProductStatus.Rejected &&
                                     p.Contacts.Any(c => c.UserEmail == email)),
            "all" => query.Where(p => p.Status == CMDBProductStatus.Active),
            "new" => query.Where(p => p.Status == CMDBProductStatus.New),
            "inactive" => query.Where(p => p.Status == CMDBProductStatus.Rejected),
            "retired" => query.Where(p => p.Status == CMDBProductStatus.Inactive),
            _ => query.Where(p => p.Status == CMDBProductStatus.Active)
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            tabQuery = tabQuery.Where(p =>
                p.Title.Contains(s) ||
                (p.CMDBID != null && p.CMDBID.Contains(s)) ||
                (p.CMDBDescription != null && p.CMDBDescription.Contains(s)));
        }

        if (businessAreaId.HasValue)
            tabQuery = tabQuery.Where(p => p.BusinessAreas.Any(ba => ba.FipsBusinessAreaId == businessAreaId.Value));
        if (channelId.HasValue)
            tabQuery = tabQuery.Where(p => p.Channels.Any(c => c.FipsChannelId == channelId.Value));
        if (userGroupId.HasValue)
            tabQuery = tabQuery.Where(p => p.UserGroups.Any(ug => ug.FipsUserGroupId == userGroupId.Value));
        if (typeId.HasValue)
            tabQuery = tabQuery.Where(p => p.Types.Any(t => t.FipsTypeId == typeId.Value));
        if (phaseId.HasValue)
            tabQuery = tabQuery.Where(p => p.PhaseId == phaseId.Value);

        var products = await tabQuery.OrderBy(p => p.Title).ToListAsync(cancellationToken);

        var businessAreaLookups = await context.BusinessAreaLookups
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var allCounts = await context.CMDBProducts
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var myCount = await context.CMDBProducts
            .Where(p => p.Status != CMDBProductStatus.Inactive &&
                        p.Status != CMDBProductStatus.Rejected &&
                        p.Contacts.Any(c => c.UserEmail == email))
            .CountAsync(cancellationToken);

        return new FipsProductsViewModel
        {
            ActiveTab = activeTab,
            Search = search,
            BusinessAreaId = businessAreaId,
            ChannelId = channelId,
            UserGroupId = userGroupId,
            TypeId = typeId,
            PhaseId = phaseId,
            MyProductsCount = myCount,
            AllProductsCount = allCounts.Where(c => c.Status == CMDBProductStatus.Active).Sum(c => c.Count),
            NewProductsCount = allCounts.Where(c => c.Status == CMDBProductStatus.New).Sum(c => c.Count),
            InactiveProductsCount = allCounts.Where(c => c.Status == CMDBProductStatus.Rejected).Sum(c => c.Count),
            RetiredCount = allCounts.Where(c => c.Status == CMDBProductStatus.Inactive).Sum(c => c.Count),
            Products = products.Select(p => new FipsProductRow
            {
                Id = p.Id,
                UniqueID = p.UniqueID,
                Title = p.Title,
                CMDBDescription = p.CMDBDescription,
                UserDescription = p.UserDescription,
                PhaseName = p.Phase?.Name,
                Status = p.Status,
                BusinessAreaDisplay = string.Join(", ", p.BusinessAreas.Select(ba => ba.FipsBusinessArea.Name)),
                TypesDisplay = p.Types.Count == 0
                    ? null
                    : string.Join(
                        ", ",
                        p.Types
                            .OrderBy(t => t.FipsType.DisplayOrder)
                            .ThenBy(t => t.FipsType.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(t => t.FipsType.Name)),
                ChannelsDisplay = p.Channels.Count == 0
                    ? null
                    : string.Join(
                        ", ",
                        p.Channels
                            .OrderBy(c => c.FipsChannel.DisplayOrder)
                            .ThenBy(c => c.FipsChannel.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(c => c.FipsChannel.Name)),
                UserGroupCount = p.UserGroups.Count,
                ContactCount = p.Contacts.Count,
                ServiceOwner = p.Contacts
                    .Where(c => string.Equals(c.FipsContactRole?.Name, "Service Owner", StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.UserName ?? c.UserEmail ?? "")
                    .FirstOrDefault(),
                ReportingContact = string.Join(", ", p.Contacts
                    .Where(c => ReportingContactRoleNames.Contains(c.FipsContactRole?.Name, StringComparer.OrdinalIgnoreCase))
                    .Select(c => c.UserName ?? c.UserEmail ?? "")
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()),
                QualityScore = CalculateQualityScore(p),
                QualityScoreMax = 7
            }).ToList(),
            BusinessAreaOptions = await context.FipsBusinessAreas.Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            ChannelOptions = await context.FipsChannels.Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            UserGroupOptions = await context.FipsUserGroups.Where(x => x.Active && x.ParentId == null).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            TypeOptions = await context.FipsTypes.Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken),
            PhaseOptions = await context.PhaseLookups.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken),
            BusinessAreaLookups = businessAreaLookups,
        };
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
