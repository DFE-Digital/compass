using Compass.Data;
using Compass.Models.Fips;
using Compass.Models.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaWorkspace
{
    public const int PageSize = 24;

    public static async Task<ServiceSchemaOverviewPage> LoadOverviewAsync(
        CompassDbContext db,
        string? area,
        int progressPage,
        int notStartedPage,
        int completePage,
        CancellationToken ct)
    {
        var sections = ServiceSchemaCatalog.Sections;
        var sectionCount = sections.Count;

        var products = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status != CMDBProductStatus.Rejected)
            .Select(p => new ProductSlice
            {
                Id = p.Id,
                Title = p.Title,
                HasPhase = p.PhaseId != null,
                TypeCount = p.Types.Count(),
                AreaNames = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList(),
                UserGroupCount = p.UserGroups.Count(),
                ChannelCount = p.Channels.Count(),
                HasOwner = p.Contacts.Any(c => c.FipsContactRole.Name == "Service Owner"),
                HasSro = p.Contacts.Any(c => c.FipsContactRole.Name == "Senior Responsible Officer")
            })
            .ToListAsync(ct);

        var entryDomains = await db.CensusEntries.AsNoTracking()
            .Where(e => e.RemovedAt == null)
            .Select(e => new { e.ProductId, e.DomainKey })
            .Distinct()
            .ToListAsync(ct);
        var domainsByProduct = entryDomains
            .GroupBy(e => e.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DomainKey).ToHashSet(StringComparer.OrdinalIgnoreCase));

        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Select(d => new { d.ProductId, d.SectionKey, d.StatusCode })
            .ToListAsync(ct);
        var declarationsByProduct = declarations
            .GroupBy(d => d.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(x => x.SectionKey, x => x.StatusCode, StringComparer.OrdinalIgnoreCase));

        var cards = new List<ServiceSchemaProductCard>(products.Count);
        foreach (var product in products)
        {
            domainsByProduct.TryGetValue(product.Id, out var domains);
            declarationsByProduct.TryGetValue(product.Id, out var declared);
            var addressed = sections.Count(section => SectionAddressed(section.Key, section.DomainKey, domains, declared));
            var areas = product.AreaNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            cards.Add(new ServiceSchemaProductCard
            {
                Id = product.Id,
                Title = product.Title,
                BusinessAreas = areas.Count == 0 ? "No business area" : string.Join(", ", areas),
                DqPercent = Percent(DqScore(product), 7),
                SchemaPercent = Percent(addressed, sectionCount),
                SectionsAddressed = addressed,
                SectionCount = sectionCount
            });
        }

        var cardsById = cards.ToDictionary(c => c.Id);
        var areaRows = new Dictionary<string, AreaBucket>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in products)
        {
            var card = cardsById[product.Id];
            var names = product.AreaNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (names.Count == 0)
                names.Add("No business area");
            foreach (var name in names)
            {
                if (!areaRows.TryGetValue(name, out var bucket))
                {
                    bucket = new AreaBucket(name);
                    areaRows[name] = bucket;
                }

                bucket.Dq.Add(card.DqPercent);
                if (card.SectionsAddressed == 0)
                    bucket.NotStarted++;
                else if (card.SectionsAddressed >= sectionCount)
                    bucket.Complete++;
                else
                    bucket.InProgress++;
            }
        }

        var filter = string.IsNullOrWhiteSpace(area) ? null : area.Trim();
        bool Matches(ServiceSchemaProductCard card) =>
            filter == null || card.BusinessAreas.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(n => string.Equals(n, filter, StringComparison.OrdinalIgnoreCase))
            || (filter == "No business area" && card.BusinessAreas == "No business area");

        var inProgress = cards.Where(c => c.SectionsAddressed > 0 && c.SectionsAddressed < sectionCount).Where(Matches)
            .OrderByDescending(c => c.SchemaPercent).ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var notStarted = cards.Where(c => c.SectionsAddressed == 0).Where(Matches)
            .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var complete = cards.Where(c => sectionCount > 0 && c.SectionsAddressed >= sectionCount).Where(Matches)
            .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var progressSlice = PageOf(inProgress, progressPage);
        var notStartedSlice = PageOf(notStarted, notStartedPage);
        var completeSlice = PageOf(complete, completePage);

        return new ServiceSchemaOverviewPage
        {
            SectionCount = sectionCount,
            ProductCount = cards.Count,
            InProgressCount = cards.Count(c => c.SectionsAddressed > 0 && c.SectionsAddressed < sectionCount),
            NotStartedCount = cards.Count(c => c.SectionsAddressed == 0),
            CompleteCount = cards.Count(c => sectionCount > 0 && c.SectionsAddressed >= sectionCount),
            AreaFilter = filter,
            ProgressPage = progressSlice.PageNumber,
            NotStartedPage = notStartedSlice.PageNumber,
            CompletePage = completeSlice.PageNumber,
            PageSize = PageSize,
            InProgressFiltered = inProgress.Count,
            NotStartedFiltered = notStarted.Count,
            CompleteFiltered = complete.Count,
            Areas = areaRows.Values
                .OrderBy(b => b.Name == "No business area" ? 1 : 0)
                .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .Select(b => new ServiceSchemaAreaDqRow
                {
                    Name = b.Name,
                    ProductCount = b.Dq.Count,
                    AverageDqPercent = b.Dq.Count == 0 ? 0 : (int)Math.Round(b.Dq.Average()),
                    InProgress = b.InProgress,
                    NotStarted = b.NotStarted,
                    Complete = b.Complete
                })
                .ToList(),
            InProgress = progressSlice.Items,
            NotStarted = notStartedSlice.Items,
            Complete = completeSlice.Items
        };
    }

    public static async Task<ServiceSchemaWorkspacePage?> LoadProductAsync(
        CompassDbContext db,
        Guid productId,
        string? sectionKey,
        bool canEdit,
        CancellationToken ct)
    {
        var product = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductSlice
            {
                Id = p.Id,
                Title = p.Title,
                HasPhase = p.PhaseId != null,
                TypeCount = p.Types.Count(),
                AreaNames = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList(),
                UserGroupCount = p.UserGroups.Count(),
                ChannelCount = p.Channels.Count(),
                HasOwner = p.Contacts.Any(c => c.FipsContactRole.Name == "Service Owner"),
                HasSro = p.Contacts.Any(c => c.FipsContactRole.Name == "Senior Responsible Officer")
            })
            .FirstOrDefaultAsync(ct);
        if (product == null)
            return null;

        var sections = ServiceSchemaCatalog.Sections;
        var active = ServiceSchemaCatalog.FindSection(sectionKey) ?? sections[0];
        var binding = ServiceSchemaAdminLookups.BindingFor(active.Key);
        var lookups = await ServiceSchemaAdminLookups.LoadAsync(
            db,
            new[] { binding.PrimaryKey, binding.SecondaryKey },
            ct);

        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => d.ProductId == productId)
            .ToListAsync(ct);
        var roles = await db.StaffRoles.AsNoTracking()
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Name)
            .ToListAsync(ct);
        var roleNames = roles.ToDictionary(r => r.Id, r => r.Family + " — " + r.Name);

        var domains = entries.Select(e => e.DomainKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var declared = declarations.ToDictionary(d => d.SectionKey, d => d.StatusCode, StringComparer.OrdinalIgnoreCase);
        var nav = sections.Select(section =>
        {
            var count = entries.Count(e => string.Equals(e.DomainKey, section.DomainKey, StringComparison.OrdinalIgnoreCase));
            declared.TryGetValue(section.Key, out var status);
            var addressed = count > 0 || IsClosingStatus(status);
            return new ServiceSchemaNavItem
            {
                Key = section.Key,
                Title = section.Title,
                EntryCount = count,
                Addressed = addressed,
                StatusLabel = count > 0 ? count + " recorded" : DeclarationLabel(status)
            };
        }).ToList();

        var sectionEntries = entries
            .Where(e => string.Equals(e.DomainKey, active.DomainKey, StringComparison.OrdinalIgnoreCase))
            .Select(e => ToRow(e, lookups, roleNames))
            .ToList();
        var declaration = declarations.FirstOrDefault(d => string.Equals(d.SectionKey, active.Key, StringComparison.OrdinalIgnoreCase));
        var areas = product.AreaNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return new ServiceSchemaWorkspacePage
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            BusinessAreas = areas.Count == 0 ? "No business area" : string.Join(", ", areas),
            DqScore = DqScore(product),
            DqPercent = Percent(DqScore(product), 7),
            SectionsAddressed = nav.Count(n => n.Addressed),
            SectionCount = sections.Count,
            SchemaPercent = Percent(nav.Count(n => n.Addressed), sections.Count),
            CanEdit = canEdit,
            Nav = nav,
            StaffRoles = roles.Where(r => r.IsActive).Select(r => new ServiceSchemaChoice
            {
                Value = r.Id.ToString(),
                Label = r.Name,
                Group = r.Family
            }).ToList(),
            Section = new ServiceSchemaSectionWorkspace
            {
                Key = active.Key,
                Title = active.Title,
                Help = active.Help,
                TitleLabel = active.TitleLabel,
                NarrativeLabel = active.NarrativeLabel,
                CapturePerson = active.CapturePerson,
                CaptureStaffRole = active.CaptureStaffRole,
                CaptureUrl = active.CaptureUrl,
                LookupLabel = binding.PrimaryLabel,
                SecondaryLookupLabel = binding.SecondaryLabel,
                Lookups = Choices(lookups, binding.PrimaryKey),
                SecondaryLookups = Choices(lookups, binding.SecondaryKey),
                Entries = sectionEntries,
                DeclarationStatus = declaration?.StatusCode ?? "",
                DeclarationLabel = DeclarationLabel(declaration?.StatusCode),
                DeclarationExplanation = declaration?.Explanation
            }
        };
    }

    public static async Task<ServiceSchemaDirectoryPage> LoadDirectoryAsync(
        CompassDbContext db,
        string? query,
        string? tab,
        int page,
        CancellationToken ct)
    {
        var list = string.Equals(tab, "inactive", StringComparison.OrdinalIgnoreCase) ? "inactive" : "active";
        var q = query?.Trim();
        var layout = await ServiceSchemaLayout.LoadAsync(db, ct);
        var topicCount = layout.Topics.Count;

        var products = db.CMDBProducts.AsNoTracking().Where(p => p.Status != CMDBProductStatus.Rejected);
        if (!string.IsNullOrWhiteSpace(q))
        {
            products = int.TryParse(q, out var uniqueId)
                ? products.Where(p => p.Title.Contains(q) || p.UniqueID == uniqueId)
                : products.Where(p => p.Title.Contains(q));
        }

        var activeCount = await products.CountAsync(p => p.Status == CMDBProductStatus.Active, ct);
        var inactiveCount = await products.CountAsync(p => p.Status != CMDBProductStatus.Active, ct);
        var filtered = list == "active"
            ? products.Where(p => p.Status == CMDBProductStatus.Active)
            : products.Where(p => p.Status != CMDBProductStatus.Active);
        var total = list == "active" ? activeCount : inactiveCount;
        var pageSize = 50;
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        var safePage = page < 1 ? 1 : Math.Min(page, pages);

        var rows = await filtered
            .OrderBy(p => p.Title)
            .Skip((safePage - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                p.Status,
                p.UpdatedAt,
                Phase = p.Phase != null ? p.Phase.Name : "",
                Areas = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList()
            })
            .ToListAsync(ct);

        var ids = rows.Select(r => r.Id).ToList();
        var domains = await db.CensusEntries.AsNoTracking()
            .Where(e => ids.Contains(e.ProductId) && e.RemovedAt == null)
            .Select(e => new { e.ProductId, e.DomainKey })
            .ToListAsync(ct);
        var covered = domains
            .Select(d => new { d.ProductId, Topic = layout.FindTopic(d.DomainKey)?.Key })
            .Where(d => d.Topic != null)
            .GroupBy(d => d.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Topic).Distinct(StringComparer.OrdinalIgnoreCase).Count());

        return new ServiceSchemaDirectoryPage
        {
            Query = q,
            Tab = list,
            Page = safePage,
            PageSize = pageSize,
            Total = total,
            TotalPages = pages,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            Items = rows.Select(r =>
            {
                covered.TryGetValue(r.Id, out var addressed);
                var names = r.Areas.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                return new ServiceSchemaDirectoryRow
                {
                    Id = r.Id,
                    UniqueId = r.UniqueID,
                    Title = r.Title,
                    Status = r.Status.ToString(),
                    Phase = r.Phase,
                    BusinessAreas = names.Count == 0 ? "—" : string.Join(", ", names),
                    UpdatedAt = r.UpdatedAt,
                    SectionsAddressed = addressed,
                    SectionCount = topicCount,
                    SchemaPercent = Percent(addressed, topicCount)
                };
            }).ToList()
        };
    }

    public static async Task<ServiceSchemaWorkspacePage?> LoadRecordAsync(
        CompassDbContext db,
        Guid productId,
        string? areaKey,
        string? topicKey,
        bool canEdit,
        CancellationToken ct)
    {
        var product = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                p.Status,
                Phase = p.Phase != null ? p.Phase.Name : "",
                Areas = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList(),
                HasPhase = p.PhaseId != null,
                TypeCount = p.Types.Count(),
                UserGroupCount = p.UserGroups.Count(),
                ChannelCount = p.Channels.Count(),
                HasOwner = p.Contacts.Any(c => c.FipsContactRole.Name == "Service Owner"),
                HasSro = p.Contacts.Any(c => c.FipsContactRole.Name == "Senior Responsible Officer")
            })
            .FirstOrDefaultAsync(ct);
        if (product == null)
            return null;

        var layout = await ServiceSchemaLayout.LoadAsync(db, ct);
        var requested = layout.FindTopic(topicKey);
        var area = layout.Find(areaKey);
        if (requested != null)
            area = layout.AreaFor(requested.Key) ?? area;
        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
        var registerContacts = new List<ServiceSchemaEntryRow>();
        if (area.Topics.Any(t => t.Key == "responsibility"))
        {
            var contacts = await db.CMDBProductContacts.AsNoTracking()
                .Where(c => c.CMDBProductId == productId)
                .OrderBy(c => c.FipsContactRole.DisplayOrder)
                .ThenBy(c => c.UserName)
                .Select(c => new { Role = c.FipsContactRole.Name, c.UserName, c.UserEmail })
                .ToListAsync(ct);
            registerContacts = contacts.Select(c => new ServiceSchemaEntryRow
            {
                Title = c.Role,
                Meta = PersonMeta(c.UserName, c.UserEmail),
                FromRegister = true
            }).ToList();
        }
        var lookupSources = area.Topics.Select(t => t.LookupSource);
        var lookups = await ServiceSchemaAdminLookups.LoadAsync(db, lookupSources, ct);
        var kinds = area.Topics.Where(t => t.CatalogueKind != null).Select(t => t.CatalogueKind!).Distinct().ToList();
        var catalogue = kinds.Count == 0
            ? new List<CensusCatalogueItem>()
            : await db.CensusCatalogueItems.AsNoTracking()
                .Where(c => kinds.Contains(c.Kind) && c.RemovedAt == null && c.StatusCode == "ACTIVE")
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        var catalogueNames = catalogue.ToDictionary(c => c.Id, c => c.Name);

        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => d.ProductId == productId)
            .Select(d => new { d.SectionKey, d.StatusCode })
            .ToListAsync(ct);
        var nothingToRecord = declarations
            .Where(d => IsClosingStatus(d.StatusCode))
            .Select(d => layout.FindTopic(d.SectionKey)?.Key ?? d.SectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var coveredTopics = entries
            .Select(e => layout.FindTopic(e.DomainKey)?.Key)
            .Where(key => key != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        coveredTopics.UnionWith(nothingToRecord);
        if (registerContacts.Count > 0)
            coveredTopics.Add("responsibility");
        var nav = layout.Areas.Select(item =>
        {
            var recorded = item.Topics.Any(t => coveredTopics.Contains(t.Key));
            return new ServiceSchemaAreaNav
            {
                Key = item.Key,
                Name = item.Name,
                Group = item.Group,
                State = item.Key == "overview" ? "" : recorded ? "recorded" : "empty",
                StateLabel = item.Key == "overview" ? "" : recorded ? "Recorded" : "Not recorded"
            };
        }).ToList();
        var scored = nav.Where(n => n.Key != "overview").ToList();
        var addressed = scored.Count(n => n.State == "recorded");
        var names = product.Areas.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var dq = (product.HasPhase ? 1 : 0) + (product.TypeCount > 0 ? 1 : 0) + (names.Count > 0 ? 1 : 0)
            + (product.UserGroupCount > 0 ? 1 : 0) + (product.ChannelCount > 0 ? 1 : 0)
            + (product.HasOwner ? 1 : 0) + (product.HasSro ? 1 : 0);

        return new ServiceSchemaWorkspacePage
        {
            ProductId = product.Id,
            UniqueId = product.UniqueID,
            ProductTitle = product.Title,
            Status = product.Status.ToString(),
            Phase = product.Phase,
            BusinessAreas = names.Count == 0 ? "No business area" : string.Join(", ", names),
            DqScore = dq,
            DqPercent = Percent(dq, 7),
            SectionsAddressed = addressed,
            SectionCount = scored.Count,
            SchemaPercent = Percent(addressed, scored.Count),
            CanEdit = canEdit,
            ActiveArea = area.Key,
            ActiveTopic = requested?.Key ?? "",
            AreaName = area.Name,
            AreaGroup = area.Group,
            AreaSummary = area.Summary,
            Areas = nav,
            Topics = area.Topics.Select(topic =>
            {
                var choices = topic.Mode switch
                {
                    "lookup" => Choices(lookups, topic.LookupSource),
                    "catalogue" => catalogue.Where(c => string.Equals(c.Kind, topic.CatalogueKind, StringComparison.OrdinalIgnoreCase))
                        .Select(c => new ServiceSchemaChoice { Value = c.Id.ToString(), Label = c.Name }).ToList(),
                    _ => new List<ServiceSchemaChoice>()
                };
                var matched = entries.Where(e => ServiceSchemaAreas.DomainMatches(topic, e.DomainKey)).ToList();
                var declaredNone = nothingToRecord.Contains(topic.Key);
                var recorded = matched.Count > 0 || declaredNone || (topic.Key == "responsibility" && registerContacts.Count > 0);
                var topicEntries = matched
                    .Select(e => new ServiceSchemaEntryRow
                    {
                        Id = e.Id,
                        Title = CatalogueTitle(e, catalogueNames, lookups),
                        Narrative = e.Narrative,
                        Meta = PersonMeta(e),
                        Url = e.ExternalUrl
                    }).ToList();
                if (topic.Key == "responsibility")
                    topicEntries.InsertRange(0, registerContacts);
                return new ServiceSchemaTopicBlock
                {
                    Key = topic.Key,
                    Heading = topic.Heading,
                    Help = topic.Help,
                    TitleLabel = topic.TitleLabel,
                    NarrativeLabel = topic.NarrativeLabel,
                    Mode = topic.Mode,
                    CatalogueKind = topic.CatalogueKind,
                    LookupLabel = topic.LookupLabel,
                    LookupAdminPanel = topic.LookupSource,
                    CapturePerson = topic.CapturePerson,
                    CaptureUrl = topic.CaptureUrl,
                    Choices = choices,
                    State = recorded ? "recorded" : "empty",
                    StateLabel = recorded ? "Recorded" : "Not recorded",
                    NothingToRecord = declaredNone,
                    Entries = topicEntries
                };
            }).ToList()
        };
    }

    private static string CatalogueTitle(
        CensusEntry entry,
        IReadOnlyDictionary<Guid, string> catalogueNames,
        IReadOnlyDictionary<string, List<ServiceSchemaChoice>> lookups)
    {
        if (entry.CatalogueItemId is Guid id && catalogueNames.TryGetValue(id, out var name))
            return name;
        var lookup = ServiceSchemaAdminLookups.LabelFor(lookups, entry.LookupCode);
        if (!string.IsNullOrWhiteSpace(lookup))
            return lookup;
        return string.IsNullOrWhiteSpace(entry.Title) ? "Recorded response" : entry.Title;
    }

    private static string? PersonMeta(CensusEntry entry) => PersonMeta(entry.PersonName, entry.PersonEmail);

    private static string? PersonMeta(string? name, string? email)
    {
        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(name)) bits.Add(name.Trim());
        if (!string.IsNullOrWhiteSpace(email)) bits.Add(email.Trim());
        return bits.Count == 0 ? null : string.Join(" · ", bits);
    }

    public static bool IsClosingStatus(string? status) =>
        string.Equals(status, "CONFIRMED_NONE", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "NOT_APPLICABLE", StringComparison.OrdinalIgnoreCase);

    public static string DeclarationLabel(string? status) => status switch
    {
        _ when string.Equals(status, "CONFIRMED_NONE", StringComparison.OrdinalIgnoreCase) => "Confirmed none",
        _ when string.Equals(status, "NOT_APPLICABLE", StringComparison.OrdinalIgnoreCase) => "Not applicable",
        _ => "Not started"
    };

    private static bool SectionAddressed(
        string sectionKey,
        string domainKey,
        HashSet<string>? domains,
        Dictionary<string, string>? declared)
    {
        if (domains != null && domains.Contains(domainKey))
            return true;
        return declared != null && declared.TryGetValue(sectionKey, out var status) && IsClosingStatus(status);
    }

    private static ServiceSchemaEntryRow ToRow(
        CensusEntry entry,
        IReadOnlyDictionary<string, List<ServiceSchemaChoice>> lookups,
        IReadOnlyDictionary<int, string> roleNames)
    {
        var bits = new List<string>();
        var primary = ServiceSchemaAdminLookups.LabelFor(lookups, entry.LookupCode);
        var secondary = ServiceSchemaAdminLookups.LabelFor(lookups, entry.SecondaryLookupCode);
        if (!string.IsNullOrWhiteSpace(primary))
            bits.Add(primary);
        if (!string.IsNullOrWhiteSpace(secondary))
            bits.Add(secondary);
        if (entry.StaffRoleId is int roleId && roleNames.TryGetValue(roleId, out var role))
            bits.Add(role);
        if (!string.IsNullOrWhiteSpace(entry.PersonName))
            bits.Add(entry.PersonName);
        if (!string.IsNullOrWhiteSpace(entry.PersonEmail))
            bits.Add(entry.PersonEmail);

        return new ServiceSchemaEntryRow
        {
            Id = entry.Id,
            Title = string.IsNullOrWhiteSpace(entry.Title) ? "Recorded response" : entry.Title,
            Narrative = entry.Narrative,
            Meta = bits.Count == 0 ? null : string.Join(" · ", bits),
            Url = entry.ExternalUrl
        };
    }

    private static List<ServiceSchemaChoice> Choices(
        IReadOnlyDictionary<string, List<ServiceSchemaChoice>> lookups,
        string? key) =>
        key != null && lookups.TryGetValue(key, out var list) ? list : new List<ServiceSchemaChoice>();

    private static (List<ServiceSchemaProductCard> Items, int PageNumber) PageOf(List<ServiceSchemaProductCard> cards, int page)
    {
        var pages = Math.Max(1, (int)Math.Ceiling(cards.Count / (double)PageSize));
        var safe = page < 1 ? 1 : Math.Min(page, pages);
        return (cards.Skip((safe - 1) * PageSize).Take(PageSize).ToList(), safe);
    }

    private static int DqScore(ProductSlice product)
    {
        var score = 0;
        if (product.HasPhase) score++;
        if (product.TypeCount > 0) score++;
        if (product.AreaNames.Count > 0) score++;
        if (product.UserGroupCount > 0) score++;
        if (product.ChannelCount > 0) score++;
        if (product.HasOwner) score++;
        if (product.HasSro) score++;
        return score;
    }

    private static int Percent(int score, int max) =>
        max <= 0 ? 0 : (int)Math.Round(score * 100d / max);

    private sealed class ProductSlice
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public bool HasPhase { get; set; }
        public int TypeCount { get; set; }
        public List<string> AreaNames { get; set; } = new();
        public int UserGroupCount { get; set; }
        public int ChannelCount { get; set; }
        public bool HasOwner { get; set; }
        public bool HasSro { get; set; }
    }

    private sealed class AreaBucket
    {
        public AreaBucket(string name) => Name = name;
        public string Name { get; }
        public List<int> Dq { get; } = new();
        public int InProgress { get; set; }
        public int NotStarted { get; set; }
        public int Complete { get; set; }
    }
}
