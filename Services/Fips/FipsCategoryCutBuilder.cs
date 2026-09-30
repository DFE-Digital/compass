using Compass.Data;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

/// <summary>
/// Cuts Active and Enterprise services by business area, user group, type, or channel,
/// with the same seven data-quality checks used on the executive report.
/// </summary>
public static class FipsCategoryCutBuilder
{
    private const int QualityChecks = 7;

    public static string NormalizeScope(string? scope) =>
        string.Equals(scope, "enterprise", StringComparison.OrdinalIgnoreCase) ? "enterprise" : "active";

    public static string NormalizeCut(string? cut) => cut?.Trim().ToLowerInvariant() switch
    {
        "user-group" or "usergroup" or "user" => "user-group",
        "type" => "type",
        "channel" => "channel",
        _ => "business-area"
    };

    public static string SubNavItemFor(string cut) => cut switch
    {
        "user-group" => "manage-fips-cut-user-group",
        "type" => "manage-fips-cut-type",
        "channel" => "manage-fips-cut-channel",
        _ => "manage-fips-cut-business-area"
    };

    public static async Task<FipsCategoryCutViewModel> BuildAsync(
        CompassDbContext db,
        string? scope,
        string? cut,
        string? value,
        CancellationToken cancellationToken)
    {
        var scopeKey = NormalizeScope(scope);
        var cutKey = NormalizeCut(cut);
        var enterprise = scopeKey == "enterprise";

        var productsQuery = db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active);
        productsQuery = enterprise
            ? productsQuery.Where(p => p.IsEnterpriseService)
            : productsQuery.Where(p => !p.IsEnterpriseService);

        var products = await productsQuery
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                HasPhase = p.PhaseId != null,
                PhaseName = p.Phase != null ? p.Phase.Name : null
            })
            .ToListAsync(cancellationToken);

        var scopeIds = products.Select(p => p.Id).ToHashSet();
        var areas = await LoadLinksAsync(
            db.CMDBProductBusinessAreas.AsNoTracking()
                .Select(x => new { x.CMDBProductId, ValueId = x.FipsBusinessAreaId, Name = x.FipsBusinessArea.Name }),
            x => x.CMDBProductId, x => x.ValueId, x => x.Name, scopeIds, cancellationToken);
        var userGroups = await LoadLinksAsync(
            db.CMDBProductUserGroups.AsNoTracking()
                .Select(x => new { x.CMDBProductId, ValueId = x.FipsUserGroupId, Name = x.FipsUserGroup.Name }),
            x => x.CMDBProductId, x => x.ValueId, x => x.Name, scopeIds, cancellationToken);
        var types = await LoadLinksAsync(
            db.CMDBProductTypes.AsNoTracking()
                .Select(x => new { x.CMDBProductId, ValueId = x.FipsTypeId, Name = x.FipsType.Name }),
            x => x.CMDBProductId, x => x.ValueId, x => x.Name, scopeIds, cancellationToken);
        var channels = await LoadLinksAsync(
            db.CMDBProductChannels.AsNoTracking()
                .Select(x => new { x.CMDBProductId, ValueId = x.FipsChannelId, Name = x.FipsChannel.Name }),
            x => x.CMDBProductId, x => x.ValueId, x => x.Name, scopeIds, cancellationToken);
        var contacts = (await db.CMDBProductContacts.AsNoTracking()
            .Select(c => new { c.CMDBProductId, Role = c.FipsContactRole.Name })
            .ToListAsync(cancellationToken))
            .Where(c => scopeIds.Contains(c.CMDBProductId))
            .GroupBy(c => c.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Role).Where(n => !string.IsNullOrWhiteSpace(n)).Cast<string>().ToList());

        var cutLinks = cutKey switch
        {
            "user-group" => userGroups,
            "type" => types,
            "channel" => channels,
            _ => areas
        };

        bool HasRole(Guid productId, string role) =>
            contacts.TryGetValue(productId, out var names)
            && names.Any(n => string.Equals(n, role, StringComparison.OrdinalIgnoreCase));

        var qualities = products.Select(p => new ProductQuality
        {
            Id = p.Id,
            UniqueId = p.UniqueID,
            Title = string.IsNullOrWhiteSpace(p.Title) ? "Untitled" : p.Title.Trim(),
            Phase = string.IsNullOrWhiteSpace(p.PhaseName) ? "Not set" : p.PhaseName.Trim(),
            ServiceOwner = HasRole(p.Id, "Service Owner"),
            InformationAssetOwner = HasRole(p.Id, "Information Asset Owner"),
            Roles = contacts.TryGetValue(p.Id, out var names) && names.Count > 0,
            Type = types.ContainsKey(p.Id),
            Channel = channels.ContainsKey(p.Id),
            UserGroup = userGroups.ContainsKey(p.Id),
            PhaseSet = p.HasPhase,
            Values = cutLinks.TryGetValue(p.Id, out var values) ? values : new List<CutValue>()
        }).ToList();

        var fields = BuildFields(qualities);
        var weakest = qualities.Count == 0
            ? null
            : fields.OrderBy(f => f.CompletionPercent).ThenBy(f => f.Field, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        var slices = BuildSlices(qualities, qualities.Count);

        FipsCategorySliceRow? selected = null;
        var selectedProducts = new List<ProductQuality>();
        if (!string.IsNullOrWhiteSpace(value))
        {
            var key = value.Trim();
            selected = slices.FirstOrDefault(s => string.Equals(s.ValueKey, key, StringComparison.OrdinalIgnoreCase));
            if (selected != null)
            {
                selectedProducts = qualities
                    .Where(p => selected.IsUnassigned
                        ? p.Values.Count == 0
                        : p.Values.Any(v => v.Id.ToString() == selected.ValueKey))
                    .OrderBy(p => p.CompletionPercent)
                    .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(p => p.UniqueId)
                    .ToList();
            }
        }

        var (cutLabel, cutNoun) = cutKey switch
        {
            "user-group" => ("User group", "user group"),
            "type" => ("Type", "type"),
            "channel" => ("Channel", "channel"),
            _ => ("Business area", "business area")
        };

        return new FipsCategoryCutViewModel
        {
            Scope = scopeKey,
            Cut = cutKey,
            ScopeLabel = enterprise ? "Enterprise" : "Active",
            CutLabel = cutLabel,
            CutNoun = cutNoun,
            ServiceCount = qualities.Count,
            ValueCount = slices.Count(s => !s.IsUnassigned),
            UnassignedCount = slices.FirstOrDefault(s => s.IsUnassigned)?.ServiceCount ?? 0,
            DataQualityPercent = OverallPercent(qualities),
            WeakestField = weakest?.Field ?? "",
            WeakestFieldPercent = weakest?.CompletionPercent ?? 0,
            Fields = fields,
            Slices = slices,
            Selected = selected,
            SelectedFields = selected == null ? new List<FipsCategoryFieldRow>() : BuildFields(selectedProducts),
            Services = selectedProducts.Select(p => new FipsCategoryServiceRow
            {
                ProductId = p.Id,
                UniqueId = p.UniqueId,
                Title = p.Title,
                Phase = p.Phase,
                CompletionPercent = p.CompletionPercent,
                MissingFields = string.Join(", ", MissingNames(p))
            }).ToList()
        };
    }

    private static async Task<Dictionary<Guid, List<CutValue>>> LoadLinksAsync<T>(
        IQueryable<T> query,
        Func<T, Guid> productId,
        Func<T, int> valueId,
        Func<T, string?> name,
        HashSet<Guid> scopeIds,
        CancellationToken cancellationToken)
    {
        var rows = await query.ToListAsync(cancellationToken);
        return rows
            .Select(r => (ProductId: productId(r), ValueId: valueId(r), Name: name(r)))
            .Where(r => scopeIds.Contains(r.ProductId) && !string.IsNullOrWhiteSpace(r.Name))
            .GroupBy(r => r.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .GroupBy(r => r.ValueId)
                    .Select(vg => new CutValue(vg.Key, vg.Select(r => r.Name!.Trim()).First()))
                    .OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    private static List<FipsCategorySliceRow> BuildSlices(List<ProductQuality> products, int scopeCount)
    {
        var buckets = new Dictionary<string, SliceBucket>(StringComparer.Ordinal);
        var unassigned = new SliceBucket { Key = "none", Name = "Not set", IsUnassigned = true };

        foreach (var product in products)
        {
            if (product.Values.Count == 0)
            {
                unassigned.Products.Add(product);
                continue;
            }

            foreach (var value in product.Values)
            {
                var key = value.Id.ToString();
                if (!buckets.TryGetValue(key, out var bucket))
                {
                    bucket = new SliceBucket { Key = key, Name = value.Name };
                    buckets[key] = bucket;
                }

                if (bucket.Seen.Add(product.Id))
                    bucket.Products.Add(product);
            }
        }

        var rows = buckets.Values
            .Select(b => ToRow(b, scopeCount))
            .OrderByDescending(r => r.ServiceCount)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unassigned.Products.Count > 0)
            rows.Add(ToRow(unassigned, scopeCount));

        return rows;
    }

    private static FipsCategorySliceRow ToRow(SliceBucket bucket, int scopeCount)
    {
        var products = bucket.Products;
        var gaps = new (string Name, int Missing)[]
        {
            ("Service owner", products.Count(p => !p.ServiceOwner)),
            ("Information asset owner", products.Count(p => !p.InformationAssetOwner)),
            ("Roles", products.Count(p => !p.Roles)),
            ("Type", products.Count(p => !p.Type)),
            ("Channel", products.Count(p => !p.Channel)),
            ("User group", products.Count(p => !p.UserGroup)),
            ("Phase", products.Count(p => !p.PhaseSet))
        };
        var main = gaps.OrderByDescending(g => g.Missing).ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase).First();

        return new FipsCategorySliceRow
        {
            ValueKey = bucket.Key,
            Name = bucket.Name,
            IsUnassigned = bucket.IsUnassigned,
            ServiceCount = products.Count,
            SharePercent = Percent(products.Count, scopeCount),
            DataQualityPercent = OverallPercent(products),
            MissingServiceOwner = gaps[0].Missing,
            MissingInformationAssetOwner = gaps[1].Missing,
            MissingPhase = gaps[6].Missing,
            MainGap = main.Missing == 0 ? "Complete" : $"{main.Name} ({main.Missing})"
        };
    }

    private static List<FipsCategoryFieldRow> BuildFields(IReadOnlyList<ProductQuality> products)
    {
        var total = products.Count;
        var checks = new (string Field, string Hint, Func<ProductQuality, bool> Met)[]
        {
            ("Service owner", "Service owner contact", p => p.ServiceOwner),
            ("Information asset owner", "Information asset owner contact", p => p.InformationAssetOwner),
            ("Roles", "At least one contact role", p => p.Roles),
            ("Type", "At least one type", p => p.Type),
            ("Channel", "At least one channel", p => p.Channel),
            ("User group", "At least one user group", p => p.UserGroup),
            ("Phase", "Phase recorded", p => p.PhaseSet)
        };

        return checks.Select(check =>
        {
            var complete = products.Count(check.Met);
            return new FipsCategoryFieldRow
            {
                Field = check.Field,
                Hint = check.Hint,
                Complete = complete,
                Missing = Math.Max(0, total - complete),
                CompletionPercent = Percent(complete, total)
            };
        }).ToList();
    }

    private static decimal OverallPercent(IReadOnlyList<ProductQuality> products)
    {
        if (products.Count == 0)
            return 0m;
        var met = products.Sum(p => p.Met);
        return Math.Round(met * 100m / (products.Count * QualityChecks), 1, MidpointRounding.AwayFromZero);
    }

    private static decimal Percent(int part, int whole) =>
        whole == 0 ? 0m : Math.Round(part * 100m / whole, 1, MidpointRounding.AwayFromZero);

    private static IEnumerable<string> MissingNames(ProductQuality product)
    {
        if (!product.ServiceOwner) yield return "Service owner";
        if (!product.InformationAssetOwner) yield return "Information asset owner";
        if (!product.Roles) yield return "Roles";
        if (!product.Type) yield return "Type";
        if (!product.Channel) yield return "Channel";
        if (!product.UserGroup) yield return "User group";
        if (!product.PhaseSet) yield return "Phase";
    }

    private sealed class SliceBucket
    {
        public string Key { get; init; } = "";
        public string Name { get; init; } = "";
        public bool IsUnassigned { get; init; }
        public HashSet<Guid> Seen { get; } = new();
        public List<ProductQuality> Products { get; } = new();
    }

    private sealed class ProductQuality
    {
        public Guid Id { get; init; }
        public int UniqueId { get; init; }
        public string Title { get; init; } = "";
        public string Phase { get; init; } = "";
        public bool ServiceOwner { get; init; }
        public bool InformationAssetOwner { get; init; }
        public bool Roles { get; init; }
        public bool Type { get; init; }
        public bool Channel { get; init; }
        public bool UserGroup { get; init; }
        public bool PhaseSet { get; init; }
        public List<CutValue> Values { get; init; } = new();
        public int Met =>
            (ServiceOwner ? 1 : 0)
            + (InformationAssetOwner ? 1 : 0)
            + (Roles ? 1 : 0)
            + (Type ? 1 : 0)
            + (Channel ? 1 : 0)
            + (UserGroup ? 1 : 0)
            + (PhaseSet ? 1 : 0);
        public int CompletionPercent =>
            (int)Math.Round(Met * 100m / QualityChecks, 0, MidpointRounding.AwayFromZero);
    }

    private readonly record struct CutValue(int Id, string Name);
}
