using Compass.Data;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaReport
{
    private const int ValuesOnDashboard = 15;
    private const int TopicPageSize = 50;

    private static readonly (string Key, string Blurb)[] InsightTopics =
    [
        ("strategic-objective", "Which strategic objectives products say they support."),
        (ServiceSchemaAreas.EnablingProductKey, "Products that provide enabling functionality into DDT, by kind."),
        ("user-group", "User groups products serve."),
        ("technology", "Technologies and components recorded across the estate."),
        (ServiceSchemaAreas.ApiProvideKey, "APIs products provide for others to use."),
        (ServiceSchemaAreas.ApiUseKey, "APIs products use that they do not provide themselves."),
        ("pattern", "Patterns and components products provide for reuse."),
        ("stack", "Stacks and platforms products run on or contribute to."),
        ("integration", "Integrations recorded across products."),
        ("dependency", "Other product dependencies, internal and external.")
    ];

    public static Task<ServiceSchemaDqReportViewModel> LoadAsync(CompassDbContext db, CancellationToken ct) =>
        LoadAsync(db, null, ct);

    public static async Task<ServiceSchemaDqReportViewModel> LoadAsync(
        CompassDbContext db,
        string? businessArea,
        CancellationToken ct)
    {
        var snapshot = await LoadSnapshotAsync(db, ct);
        var areaFilter = string.IsNullOrWhiteSpace(businessArea) ? null : businessArea.Trim();
        var products = FilterProducts(snapshot.Products, areaFilter);
        var rows = products
            .Select(p => ToProduct(p, snapshot.TopicCount))
            .OrderBy(r => r.Percent)
            .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var topics = TopicRows(snapshot, products);
        var overall = topics.Count == 0 ? 0 : (int)Math.Round(topics.Average(t => t.Percent), MidpointRounding.AwayFromZero);
        var productIds = products.Select(p => p.Id).ToHashSet();

        return new ServiceSchemaDqReportViewModel
        {
            TopicCount = snapshot.TopicCount,
            ActiveProducts = rows.Count,
            TotalEntries = snapshot.EntriesByTopic.Values
                .SelectMany(list => list)
                .Count(e => productIds.Contains(e.ProductId)),
            OverallPercent = overall,
            NotStarted = rows.Count(r => r.TopicsRecorded == 0),
            InProgress = rows.Count(r => r.TopicsRecorded > 0 && r.TopicsRecorded < snapshot.TopicCount),
            Complete = rows.Count(r => snapshot.TopicCount > 0 && r.TopicsRecorded >= snapshot.TopicCount),
            BusinessAreaFilter = areaFilter,
            BusinessAreaOptions = snapshot.Products
                .SelectMany(p => p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            BusinessAreas = BusinessAreas(products, rows, snapshot.TopicCount),
            Sections = SectionRows(topics),
            Insights = InsightPanels(snapshot, productIds),
            WeakestQuestions = topics.OrderBy(t => t.Percent).ThenByDescending(t => t.Empty).Take(8).ToList(),
            ProductsBehind = rows.Where(r => r.Percent < 100).Take(12).ToList(),
            Products = rows
        };
    }

    public static async Task<ServiceSchemaDqTopicPage?> LoadTopicAsync(
        CompassDbContext db,
        string? key,
        string? view,
        string? value,
        string? businessArea,
        int? page,
        CancellationToken ct)
    {
        var snapshot = await LoadSnapshotAsync(db, ct);
        var topic = snapshot.Layout.FindTopic(key);
        if (topic == null)
            return null;

        var areaFilter = string.IsNullOrWhiteSpace(businessArea) ? null : businessArea.Trim();
        var products = FilterProducts(snapshot.Products, areaFilter);
        var area = snapshot.Layout.AreaFor(topic.Key);
        var informed = Informed(snapshot, topic.Key);
        informed.IntersectWith(products.Select(p => p.Id));
        var items = Set(snapshot.Items, topic.Key);
        items.IntersectWith(products.Select(p => p.Id));
        var declared = Set(snapshot.Declared, topic.Key);
        declared.IntersectWith(products.Select(p => p.Id));
        if (ServiceSchemaAreas.IsServiceResponsibility(topic.Key))
            items.UnionWith(snapshot.Contacts.Where(id => products.Any(p => p.Id == id)));

        var selectable = HasSelectableValues(topic.Mode);
        var normalisedView = NormaliseView(view, selectable);
        var selectedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        var values = ValueRows(snapshot, topic, products.Select(p => p.Id).ToHashSet());
        var selectedLabel = selectedValue == null
            ? null
            : values.FirstOrDefault(v => string.Equals(v.Code, selectedValue, StringComparison.OrdinalIgnoreCase))?.Label
              ?? selectedValue;

        var matched = MatchProducts(snapshot, topic, informed, normalisedView, selectedValue)
            .Where(p => products.Any(x => x.Id == p.Id))
            .Select(p => ToProduct(p, snapshot.TopicCount, AnswerFor(snapshot, topic, p.Id)))
            .OrderBy(p => p.Percent)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pageNumber = page is > 0 ? page.Value : 1;
        var paged = matched.Skip((pageNumber - 1) * TopicPageSize).Take(TopicPageSize).ToList();
        var productIds = products.Select(p => p.Id).ToHashSet();
        var entryCount = snapshot.EntriesByTopic.TryGetValue(topic.Key, out var entries)
            ? entries.Count(e => productIds.Contains(e.ProductId))
            : 0;

        return new ServiceSchemaDqTopicPage
        {
            Key = topic.Key,
            Name = topic.Heading,
            AreaKey = area?.Key ?? "",
            AreaName = area?.Name ?? "",
            Mode = topic.Mode,
            ModeLabel = ModeLabel(topic),
            Help = topic.Help,
            HasSelectableValues = selectable,
            View = normalisedView,
            SelectedValue = selectedValue,
            SelectedValueLabel = selectedLabel,
            BusinessAreaFilter = areaFilter,
            BusinessAreaOptions = snapshot.Products
                .SelectMany(p => p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            TotalProducts = products.Count,
            ProductsWithItems = items.Count,
            ProductsDeclared = declared.Count,
            ProductsWithInformation = informed.Count,
            Empty = products.Count - informed.Count,
            Percent = Percent(informed.Count, products.Count),
            EntryCount = entryCount,
            Page = pageNumber,
            PageSize = TopicPageSize,
            ProductMatchCount = matched.Count,
            Values = values,
            Products = paged
        };
    }

    private static List<ProductSlice> FilterProducts(List<ProductSlice> products, string? areaFilter)
    {
        if (string.IsNullOrWhiteSpace(areaFilter))
            return products;
        if (string.Equals(areaFilter, "No business area", StringComparison.OrdinalIgnoreCase))
            return products.Where(p => p.Areas.Count == 0).ToList();
        return products.Where(p => p.Areas.Any(a => string.Equals(a, areaFilter, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private static string NormaliseView(string? view, bool selectable) =>
        view?.Trim().ToLowerInvariant() switch
        {
            "recorded" => "recorded",
            "answers" => "answers",
            "missing" => "missing",
            _ => selectable ? "answers" : "missing"
        };

    private static IEnumerable<ProductSlice> MatchProducts(
        Snapshot snapshot,
        ServiceSchemaTopic topic,
        HashSet<Guid> informed,
        string view,
        string? selectedValue)
    {
        if (!string.IsNullOrWhiteSpace(selectedValue))
        {
            var matching = ProductsWithValue(snapshot, topic, selectedValue);
            return snapshot.Products.Where(p => matching.Contains(p.Id));
        }

        return view switch
        {
            "recorded" or "answers" => snapshot.Products.Where(p => informed.Contains(p.Id)),
            _ => snapshot.Products.Where(p => !informed.Contains(p.Id))
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
        var productTitles = snapshot.Products.ToDictionary(p => p.Id, p => p.Title);

        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => ids.Contains(e.ProductId) && e.RemovedAt == null)
            .Select(e => new EntrySlice
            {
                ProductId = e.ProductId,
                DomainKey = e.DomainKey,
                Title = e.Title,
                Narrative = e.Narrative,
                LookupCode = e.LookupCode,
                SecondaryLookupCode = e.SecondaryLookupCode,
                CatalogueItemId = e.CatalogueItemId
            })
            .ToListAsync(ct);

        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => ids.Contains(d.ProductId))
            .Select(d => new { d.ProductId, d.SectionKey, d.StatusCode })
            .ToListAsync(ct);

        var lookupKeys = layout.Topics
            .Select(t => t.LookupSource)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)!;
        snapshot.Lookups = await ServiceSchemaAdminLookups.LoadAsync(db, lookupKeys, ct);

        var catalogueIds = entries
            .Where(e => e.CatalogueItemId != null)
            .Select(e => e.CatalogueItemId!.Value)
            .Distinct()
            .ToList();
        if (catalogueIds.Count > 0)
        {
            snapshot.CatalogueNames = await db.CensusCatalogueItems.AsNoTracking()
                .Where(c => catalogueIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        }

        var linkedProductIds = entries
            .Select(e => Guid.TryParse(e.LookupCode, out var id) ? id : (Guid?)null)
            .Where(id => id != null)
            .Select(id => id!.Value)
            .Distinct()
            .Where(id => !productTitles.ContainsKey(id))
            .ToList();
        if (linkedProductIds.Count > 0)
        {
            var linkedTitles = await db.CMDBProducts.AsNoTracking()
                .Where(p => linkedProductIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Title })
                .ToListAsync(ct);
            foreach (var linked in linkedTitles)
                productTitles[linked.Id] = linked.Title;
        }

        snapshot.ProductTitles = productTitles;

        foreach (var entry in entries)
        {
            var topicKey = layout.FindTopic(entry.DomainKey)?.Key;
            if (string.IsNullOrWhiteSpace(topicKey))
                continue;
            Add(snapshot.Items, topicKey, entry.ProductId);
            if (!snapshot.EntriesByTopic.TryGetValue(topicKey, out var list))
            {
                list = new List<EntrySlice>();
                snapshot.EntriesByTopic[topicKey] = list;
            }
            list.Add(entry);
        }

        foreach (var declaration in declarations.Where(d => ServiceSchemaWorkspace.IsClosingStatus(d.StatusCode)))
            Add(snapshot.Declared, layout.FindTopic(declaration.SectionKey)?.Key, declaration.ProductId);

        if (layout.FindTopic(ServiceSchemaAreas.ServiceResponsibilityKey) != null)
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

    private static List<ServiceSchemaDqTopicRow> TopicRows(Snapshot snapshot, List<ProductSlice> products)
    {
        var productIds = products.Select(p => p.Id).ToHashSet();
        var rows = new List<ServiceSchemaDqTopicRow>();
        foreach (var area in snapshot.Layout.Areas.Where(a => a.Key != "overview"))
        {
            foreach (var topic in area.Topics)
            {
                var informed = Informed(snapshot, topic.Key);
                informed.IntersectWith(productIds);
                var items = Set(snapshot.Items, topic.Key);
                items.IntersectWith(productIds);
                var declared = Set(snapshot.Declared, topic.Key);
                declared.IntersectWith(productIds);
                if (ServiceSchemaAreas.IsServiceResponsibility(topic.Key))
                    items.UnionWith(snapshot.Contacts.Where(productIds.Contains));
                var values = ValueRows(snapshot, topic, productIds);
                var entryCount = snapshot.EntriesByTopic.TryGetValue(topic.Key, out var topicEntries)
                    ? topicEntries.Count(e => productIds.Contains(e.ProductId))
                    : 0;
                rows.Add(new ServiceSchemaDqTopicRow
                {
                    Key = topic.Key,
                    Name = topic.Heading,
                    AreaKey = area.Key,
                    AreaName = area.Name,
                    Mode = topic.Mode,
                    HasSelectableValues = HasSelectableValues(topic.Mode),
                    ProductsWithItems = items.Count,
                    ProductsDeclared = declared.Count,
                    ProductsWithInformation = informed.Count,
                    Empty = products.Count - informed.Count,
                    Percent = Percent(informed.Count, products.Count),
                    DistinctValues = values.Count,
                    EntryCount = entryCount
                });
            }
        }

        return rows;
    }

    private static List<ServiceSchemaDqSectionRow> SectionRows(List<ServiceSchemaDqTopicRow> topics) =>
        topics
            .GroupBy(t => t.AreaKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var list = g.OrderBy(t => t.Percent).ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
                return new ServiceSchemaDqSectionRow
                {
                    Key = g.Key,
                    Name = list[0].AreaName,
                    TopicCount = list.Count,
                    AveragePercent = list.Count == 0 ? 0 : (int)Math.Round(list.Average(t => t.Percent), MidpointRounding.AwayFromZero),
                    EmptyQuestions = list.Count(t => t.Empty > 0),
                    EntryCount = list.Sum(t => t.EntryCount),
                    Topics = list
                };
            })
            .OrderBy(s => s.AveragePercent)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<ServiceSchemaDqInsightPanel> InsightPanels(Snapshot snapshot, HashSet<Guid> productIds)
    {
        var panels = new List<ServiceSchemaDqInsightPanel>();
        foreach (var (key, blurb) in InsightTopics)
        {
            var topic = snapshot.Layout.FindTopic(key);
            if (topic == null || !HasSelectableValues(topic.Mode))
                continue;
            var area = snapshot.Layout.AreaFor(topic.Key);
            var values = ValueRows(snapshot, topic, productIds);
            var entryCount = snapshot.EntriesByTopic.TryGetValue(topic.Key, out var entries)
                ? entries.Count(e => productIds.Contains(e.ProductId))
                : 0;
            panels.Add(new ServiceSchemaDqInsightPanel
            {
                TopicKey = topic.Key,
                Title = topic.Heading,
                AreaName = area?.Name ?? "",
                ModeLabel = ModeLabel(topic),
                Blurb = blurb,
                ProductsWithAnswers = Set(snapshot.Items, topic.Key).Count(productIds.Contains),
                DistinctValues = values.Count,
                EntryCount = entryCount,
                Values = values.Take(ValuesOnDashboard).ToList()
            });
        }

        return panels
            .OrderByDescending(p => p.EntryCount)
            .ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ServiceSchemaDqValueRow> ValueRows(Snapshot snapshot, ServiceSchemaTopic topic, HashSet<Guid>? productIds = null)
    {
        if (!snapshot.EntriesByTopic.TryGetValue(topic.Key, out var entries) || entries.Count == 0)
            return new List<ServiceSchemaDqValueRow>();

        var scoped = productIds == null ? entries : entries.Where(e => productIds.Contains(e.ProductId)).ToList();
        if (scoped.Count == 0)
            return new List<ServiceSchemaDqValueRow>();

        return scoped
            .SelectMany(e => ValueKeys(snapshot, topic, e))
            .GroupBy(v => v.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                return new ServiceSchemaDqValueRow
                {
                    Code = first.Code,
                    Label = first.Label,
                    EntryCount = g.Count(),
                    ProductCount = g.Select(x => x.ProductId).Distinct().Count()
                };
            })
            .OrderByDescending(v => v.ProductCount)
            .ThenByDescending(v => v.EntryCount)
            .ThenBy(v => v.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<Guid> ProductsWithValue(Snapshot snapshot, ServiceSchemaTopic topic, string valueCode)
    {
        if (!snapshot.EntriesByTopic.TryGetValue(topic.Key, out var entries))
            return new HashSet<Guid>();

        return entries
            .Where(e => ValueKeys(snapshot, topic, e).Any(v => string.Equals(v.Code, valueCode, StringComparison.OrdinalIgnoreCase)))
            .Select(e => e.ProductId)
            .ToHashSet();
    }

    private static IEnumerable<(string Code, string Label, Guid ProductId)> ValueKeys(
        Snapshot snapshot,
        ServiceSchemaTopic topic,
        EntrySlice entry)
    {
        switch (topic.Mode)
        {
            case ServiceSchemaResponseModes.YesChoice:
                if (!string.IsNullOrWhiteSpace(entry.SecondaryLookupCode))
                {
                    yield return (
                        entry.SecondaryLookupCode,
                        ChoiceLabel(topic, entry.SecondaryLookupCode) ?? entry.SecondaryLookupCode,
                        entry.ProductId);
                }
                else if (!string.IsNullOrWhiteSpace(entry.LookupCode))
                {
                    yield return (
                        entry.LookupCode,
                        YesNoLabel(entry.LookupCode),
                        entry.ProductId);
                }
                yield break;

            case ServiceSchemaResponseModes.Lookup:
            case ServiceSchemaResponseModes.Choice:
                if (!string.IsNullOrWhiteSpace(entry.LookupCode))
                {
                    yield return (
                        entry.LookupCode,
                        ResolvePrimaryLabel(snapshot, topic, entry.LookupCode),
                        entry.ProductId);
                }
                yield break;

            case ServiceSchemaResponseModes.Catalogue:
                if (entry.CatalogueItemId is Guid catalogueId)
                {
                    yield return (
                        catalogueId.ToString(),
                        CatalogueName(snapshot, catalogueId),
                        entry.ProductId);
                }
                yield break;

            case ServiceSchemaResponseModes.Products:
                if (!string.IsNullOrWhiteSpace(entry.LookupCode))
                {
                    yield return (
                        entry.LookupCode,
                        LinkedProductName(snapshot, entry.LookupCode) ?? entry.Title ?? entry.LookupCode,
                        entry.ProductId);
                }
                yield break;

            default:
                var title = string.IsNullOrWhiteSpace(entry.Title) ? null : entry.Title.Trim();
                if (title != null)
                    yield return (title, title, entry.ProductId);
                yield break;
        }
    }

    private static (string Answer, string? Detail, bool Declared) AnswerFor(
        Snapshot snapshot,
        ServiceSchemaTopic topic,
        Guid productId)
    {
        var hasEntries = snapshot.EntriesByTopic.TryGetValue(topic.Key, out var entries)
            && entries.Any(e => e.ProductId == productId);

        if (!hasEntries && Set(snapshot.Declared, topic.Key).Contains(productId))
            return ("Nothing to record", null, true);

        if (!hasEntries)
        {
            if (ServiceSchemaAreas.IsServiceResponsibility(topic.Key) && snapshot.Contacts.Contains(productId))
                return ("Named contact on product", null, false);
            return ("", null, false);
        }

        var labels = entries!
            .Where(e => e.ProductId == productId)
            .Select(e => FormatEntry(snapshot, topic, e))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .ToList();
        if (labels.Count == 0)
            return ("Recorded response", null, false);

        var answer = string.Join("; ", labels.Select(l => l.Label).Distinct(StringComparer.OrdinalIgnoreCase));
        var detailParts = labels
            .Select(l => l.Detail)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return (answer, detailParts.Count == 0 ? null : string.Join(" · ", detailParts!), false);
    }

    private static (string Label, string? Detail) FormatEntry(Snapshot snapshot, ServiceSchemaTopic topic, EntrySlice entry)
    {
        switch (topic.Mode)
        {
            case ServiceSchemaResponseModes.YesChoice:
            {
                var answer = YesNoLabel(entry.LookupCode ?? "");
                var category = ChoiceLabel(topic, entry.SecondaryLookupCode);
                if (!string.IsNullOrWhiteSpace(category))
                    answer += " — " + category;
                return (answer, TrimText(entry.Narrative));
            }
            case ServiceSchemaResponseModes.Lookup:
            {
                var primary = ResolvePrimaryLabel(snapshot, topic, entry.LookupCode);
                var secondary = ServiceSchemaAdminLookups.LabelFor(snapshot.Lookups, entry.SecondaryLookupCode);
                var detail = string.Join(" · ", new[] { secondary, TrimText(entry.Title), TrimText(entry.Narrative) }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
                return (primary, string.IsNullOrWhiteSpace(detail) ? null : detail);
            }
            case ServiceSchemaResponseModes.Choice:
                return (
                    ResolvePrimaryLabel(snapshot, topic, entry.LookupCode),
                    TrimText(entry.Narrative) ?? TrimText(entry.Title));
            case ServiceSchemaResponseModes.Catalogue:
            {
                var name = entry.CatalogueItemId is Guid id
                    ? CatalogueName(snapshot, id)
                    : (entry.Title ?? "Catalogue item");
                return (name, TrimText(entry.Narrative));
            }
            case ServiceSchemaResponseModes.Products:
            {
                var name = LinkedProductName(snapshot, entry.LookupCode) ?? entry.Title ?? "Linked product";
                return (name, TrimText(entry.Narrative));
            }
            default:
                return (
                    string.IsNullOrWhiteSpace(entry.Title) ? "Recorded response" : entry.Title!,
                    TrimText(entry.Narrative));
        }
    }

    private static string ResolvePrimaryLabel(Snapshot snapshot, ServiceSchemaTopic topic, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Unspecified";
        var choice = ChoiceLabel(topic, code);
        if (!string.IsNullOrWhiteSpace(choice))
            return choice;
        var lookup = ServiceSchemaAdminLookups.LabelFor(snapshot.Lookups, code);
        return string.IsNullOrWhiteSpace(lookup) ? code : lookup;
    }

    private static string CatalogueName(Snapshot snapshot, Guid id) =>
        snapshot.CatalogueNames.TryGetValue(id, out var name) ? name : id.ToString();

    private static string? LinkedProductName(Snapshot snapshot, string? code) =>
        Guid.TryParse(code, out var id) && snapshot.ProductTitles.TryGetValue(id, out var title) ? title : null;

    private static string? ChoiceLabel(ServiceSchemaTopic topic, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;
        return topic.ChoiceOptions
            .FirstOrDefault(c => string.Equals(c.Value, code, StringComparison.OrdinalIgnoreCase))
            ?.Label;
    }

    private static string YesNoLabel(string code) =>
        string.Equals(code, "yes", StringComparison.OrdinalIgnoreCase) ? "Yes"
        : string.Equals(code, "no", StringComparison.OrdinalIgnoreCase) ? "No"
        : string.IsNullOrWhiteSpace(code) ? "Unspecified" : code;

    private static string? TrimText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HasSelectableValues(string? mode) =>
        mode is ServiceSchemaResponseModes.Lookup
            or ServiceSchemaResponseModes.Choice
            or ServiceSchemaResponseModes.YesChoice
            or ServiceSchemaResponseModes.Catalogue
            or ServiceSchemaResponseModes.Products;

    private static string ModeLabel(ServiceSchemaTopic topic) => topic.Mode switch
    {
        ServiceSchemaResponseModes.Lookup => "Lookup",
        ServiceSchemaResponseModes.Choice => "Choice",
        ServiceSchemaResponseModes.YesChoice => "Yes / no + choice",
        ServiceSchemaResponseModes.Catalogue => "Catalogue",
        ServiceSchemaResponseModes.Products => "Products",
        _ => "Text"
    };

    private static List<ServiceSchemaDqAreaRow> BusinessAreas(
        List<ProductSlice> products,
        List<ServiceSchemaDqProductRow> rows,
        int topicCount)
    {
        return products
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
                    AveragePercent = Percent(addressed, items.Count * topicCount),
                    NotStarted = items.Count(i => i.TopicsRecorded == 0),
                    InProgress = items.Count(i => i.TopicsRecorded > 0 && i.TopicsRecorded < topicCount),
                    Complete = items.Count(i => topicCount > 0 && i.TopicsRecorded >= topicCount)
                };
            })
            .OrderBy(a => a.AveragePercent)
            .ThenByDescending(a => a.ProductCount)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ServiceSchemaDqProductRow ToProduct(ProductSlice product, int topicCount) =>
        ToProduct(product, topicCount, ("", null, false));

    private static ServiceSchemaDqProductRow ToProduct(
        ProductSlice product,
        int topicCount,
        (string Answer, string? Detail, bool Declared) answer) =>
        new()
        {
            Id = product.Id,
            UniqueId = product.UniqueId,
            Title = product.Title,
            BusinessAreas = product.Areas.Count == 0 ? "No business area" : string.Join(", ", product.Areas),
            Phase = product.Phase,
            TopicsRecorded = product.Covered.Count,
            TopicCount = topicCount,
            Percent = Percent(product.Covered.Count, topicCount),
            Answer = answer.Answer,
            AnswerDetail = answer.Detail,
            DeclaredNothing = answer.Declared
        };

    private static HashSet<Guid> Informed(Snapshot snapshot, string topicKey)
    {
        var informed = new HashSet<Guid>(Set(snapshot.Items, topicKey));
        informed.UnionWith(Set(snapshot.Declared, topicKey));
        if (ServiceSchemaAreas.IsServiceResponsibility(topicKey))
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
        public Dictionary<string, List<EntrySlice>> EntriesByTopic { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<ServiceSchemaChoice>> Lookups { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<Guid, string> CatalogueNames { get; set; } = new();
        public Dictionary<Guid, string> ProductTitles { get; set; } = new();
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

    private sealed class EntrySlice
    {
        public Guid ProductId { get; init; }
        public string DomainKey { get; init; } = "";
        public string? Title { get; init; }
        public string? Narrative { get; init; }
        public string? LookupCode { get; init; }
        public string? SecondaryLookupCode { get; init; }
        public Guid? CatalogueItemId { get; init; }
    }
}
