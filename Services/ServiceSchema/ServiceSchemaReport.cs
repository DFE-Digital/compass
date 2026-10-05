using Compass.Data;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaReport
{
    public static async Task<ServiceSchemaDqReportViewModel> LoadAsync(CompassDbContext db, CancellationToken ct)
    {
        var snapshot = await LoadSnapshotAsync(db, ct);
        var rows = snapshot.Products
            .Select(p => ToProduct(p, snapshot.TopicCount))
            .OrderBy(r => r.Percent)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var topics = TopicRows(snapshot);
        var overall = topics.Count == 0 ? 0 : (int)Math.Round(topics.Average(t => t.Percent), MidpointRounding.AwayFromZero);

        return new ServiceSchemaDqReportViewModel
        {
            TopicCount = snapshot.TopicCount,
            ActiveProducts = rows.Count,
            OverallPercent = overall,
            NotStarted = rows.Count(r => r.TopicsRecorded == 0),
            InProgress = rows.Count(r => r.TopicsRecorded > 0 && r.TopicsRecorded < snapshot.TopicCount),
            Complete = rows.Count(r => snapshot.TopicCount > 0 && r.TopicsRecorded >= snapshot.TopicCount),
            WithAnyRecording = rows.Count(r => r.TopicsRecorded > 0),
            Topics = topics,
            BusinessAreas = BusinessAreas(snapshot, rows),
            Phases = Phases(snapshot),
            Products = rows
        };
    }

    public static async Task<ServiceSchemaDqTopicPage?> LoadTopicAsync(CompassDbContext db, string? key, string? view, CancellationToken ct)
    {
        var snapshot = await LoadSnapshotAsync(db, ct);
        var topic = snapshot.Layout.FindTopic(key);
        if (topic == null)
            return null;

        var area = snapshot.Layout.AreaFor(topic.Key);
        var informed = Informed(snapshot, topic.Key);
        var items = Set(snapshot.Items, topic.Key);
        var declared = Set(snapshot.Declared, topic.Key);
        if (topic.Key == "responsibility")
            items.UnionWith(snapshot.Contacts);

        var showRecorded = string.Equals(view, "recorded", StringComparison.OrdinalIgnoreCase);
        var matched = snapshot.Products
            .Where(p => showRecorded ? informed.Contains(p.Id) : !informed.Contains(p.Id))
            .Select(p => ToProduct(p, snapshot.TopicCount))
            .OrderBy(p => p.Percent)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ServiceSchemaDqTopicPage
        {
            Key = topic.Key,
            Name = topic.Heading,
            AreaKey = area?.Key ?? "",
            AreaName = area?.Name ?? "",
            View = showRecorded ? "recorded" : "missing",
            TotalProducts = snapshot.Products.Count,
            ProductsWithItems = items.Count,
            ProductsDeclared = declared.Count,
            ProductsWithInformation = informed.Count,
            Empty = snapshot.Products.Count - informed.Count,
            Percent = Percent(informed.Count, snapshot.Products.Count),
            Products = matched
        };
    }

    private static async Task<Snapshot> LoadSnapshotAsync(CompassDbContext db, CancellationToken ct)
    {
        var layout = await ServiceSchemaLayout.LoadAsync(db, ct);
        var products = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active)
            .OrderBy(p => p.Title)
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                Phase = p.Phase != null ? p.Phase.Name : "",
                Areas = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList(),
                HasContact = p.Contacts.Any()
            })
            .ToListAsync(ct);

        var snapshot = new Snapshot
        {
            Layout = layout,
            TopicCount = layout.Topics.Count,
            Products = products.Select(p => new ProductSlice
            {
                Id = p.Id,
                UniqueId = p.UniqueID,
                Title = p.Title,
                Phase = string.IsNullOrWhiteSpace(p.Phase) ? "No phase" : p.Phase,
                Areas = p.Areas.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                HasContact = p.HasContact
            }).ToList()
        };

        if (snapshot.Products.Count == 0)
            return snapshot;

        var ids = snapshot.Products.Select(p => p.Id).ToList();
        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => ids.Contains(e.ProductId) && e.RemovedAt == null)
            .Select(e => new { e.ProductId, e.DomainKey })
            .ToListAsync(ct);
        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => ids.Contains(d.ProductId))
            .Select(d => new { d.ProductId, d.SectionKey, d.StatusCode })
            .ToListAsync(ct);

        foreach (var entry in entries)
            Add(snapshot.Items, layout.FindTopic(entry.DomainKey)?.Key, entry.ProductId);
        foreach (var declaration in declarations.Where(d => ServiceSchemaWorkspace.IsClosingStatus(d.StatusCode)))
            Add(snapshot.Declared, layout.FindTopic(declaration.SectionKey)?.Key, declaration.ProductId);
        if (layout.FindTopic("responsibility") != null)
        {
            foreach (var product in snapshot.Products.Where(p => p.HasContact))
                snapshot.Contacts.Add(product.Id);
        }

        var informedByTopic = layout.Topics.ToDictionary(
            topic => topic.Key,
            topic => Informed(snapshot, topic.Key),
            StringComparer.OrdinalIgnoreCase);
        foreach (var product in snapshot.Products)
        {
            foreach (var pair in informedByTopic)
            {
                if (pair.Value.Contains(product.Id))
                    product.Covered.Add(pair.Key);
            }
        }

        return snapshot;
    }

    private static List<ServiceSchemaDqTopicRow> TopicRows(Snapshot snapshot)
    {
        var rows = new List<ServiceSchemaDqTopicRow>();
        foreach (var area in snapshot.Layout.Areas.Where(a => a.Key != "overview"))
        {
            foreach (var topic in area.Topics)
            {
                var informed = Informed(snapshot, topic.Key);
                var items = Set(snapshot.Items, topic.Key);
                var declared = Set(snapshot.Declared, topic.Key);
                if (topic.Key == "responsibility")
                    items.UnionWith(snapshot.Contacts);
                rows.Add(new ServiceSchemaDqTopicRow
                {
                    Key = topic.Key,
                    Name = topic.Heading,
                    AreaKey = area.Key,
                    AreaName = area.Name,
                    Group = area.Group,
                    ProductsWithItems = items.Count,
                    ProductsDeclared = declared.Count,
                    ProductsWithInformation = informed.Count,
                    Empty = snapshot.Products.Count - informed.Count,
                    Percent = Percent(informed.Count, snapshot.Products.Count)
                });
            }
        }

        return rows;
    }

    private static List<ServiceSchemaDqAreaRow> BusinessAreas(Snapshot snapshot, List<ServiceSchemaDqProductRow> rows)
    {
        return snapshot.Products
            .SelectMany(p =>
            {
                var names = p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas;
                var row = rows.First(r => r.Id == p.Id);
                return names.Select(name => (name, row));
            })
            .GroupBy(x => x.name, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var items = g.Select(x => x.row).ToList();
                var addressed = items.Sum(i => i.TopicsRecorded);
                return new ServiceSchemaDqAreaRow
                {
                    Name = g.First().name,
                    ProductCount = items.Count,
                    AveragePercent = Percent(addressed, items.Count * snapshot.TopicCount),
                    NotStarted = items.Count(i => i.TopicsRecorded == 0),
                    InProgress = items.Count(i => i.TopicsRecorded > 0 && i.TopicsRecorded < snapshot.TopicCount),
                    Complete = items.Count(i => snapshot.TopicCount > 0 && i.TopicsRecorded >= snapshot.TopicCount)
                };
            })
            .OrderBy(a => a.AveragePercent)
            .ThenByDescending(a => a.ProductCount)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ServiceSchemaDqMixRow> Phases(Snapshot snapshot) =>
        snapshot.Products
            .GroupBy(p => p.Phase, StringComparer.OrdinalIgnoreCase)
            .Select(g => new ServiceSchemaDqMixRow { Name = g.First().Phase, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static ServiceSchemaDqProductRow ToProduct(ProductSlice product, int topicCount) =>
        new()
        {
            Id = product.Id,
            UniqueId = product.UniqueId,
            Title = product.Title,
            BusinessAreas = product.Areas.Count == 0 ? "No business area" : string.Join(", ", product.Areas),
            Phase = product.Phase,
            TopicsRecorded = product.Covered.Count,
            TopicCount = topicCount,
            Percent = Percent(product.Covered.Count, topicCount)
        };

    private static HashSet<Guid> Informed(Snapshot snapshot, string topicKey)
    {
        var informed = new HashSet<Guid>(Set(snapshot.Items, topicKey));
        informed.UnionWith(Set(snapshot.Declared, topicKey));
        if (string.Equals(topicKey, "responsibility", StringComparison.OrdinalIgnoreCase))
            informed.UnionWith(snapshot.Contacts);
        return informed;
    }

    private static HashSet<Guid> Set(Dictionary<string, HashSet<Guid>> map, string key) =>
        map.TryGetValue(key, out var set) ? set : new HashSet<Guid>();

    private static void Add(Dictionary<string, HashSet<Guid>> map, string? key, Guid id)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;
        if (!map.TryGetValue(key, out var set))
        {
            set = new HashSet<Guid>();
            map[key] = set;
        }
        set.Add(id);
    }

    private static int Percent(int addressed, int total) =>
        total <= 0 ? 0 : (int)Math.Round(addressed * 100d / total, MidpointRounding.AwayFromZero);

    private sealed class Snapshot
    {
        public ServiceSchemaLayout Layout { get; init; } = null!;
        public int TopicCount { get; init; }
        public List<ProductSlice> Products { get; init; } = new();
        public Dictionary<string, HashSet<Guid>> Items { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, HashSet<Guid>> Declared { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<Guid> Contacts { get; } = new();
    }

    private sealed class ProductSlice
    {
        public Guid Id { get; init; }
        public int UniqueId { get; init; }
        public string Title { get; init; } = "";
        public string Phase { get; init; } = "";
        public List<string> Areas { get; init; } = new();
        public bool HasContact { get; init; }
        public HashSet<string> Covered { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
