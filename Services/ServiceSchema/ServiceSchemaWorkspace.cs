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

    public static async Task<(int SectionsComplete, int SectionCount)> LoadProgressAsync(
        CompassDbContext db,
        Guid productId,
        CancellationToken ct)
    {
        var layout = await ServiceSchemaLayout.LoadAsync(db, ct);
        var areas = layout.Areas.Where(a => a.Key != "overview").ToList();
        var sectionCount = areas.Count + 1; // + product details

        var domainKeys = await db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .Select(e => e.DomainKey)
            .ToListAsync(ct);
        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => d.ProductId == productId)
            .Select(d => new { d.SectionKey, d.StatusCode })
            .ToListAsync(ct);
        var nothingToRecord = declarations
            .Where(d => IsClosingStatus(d.StatusCode))
            .Select(d => layout.FindTopic(d.SectionKey)?.Key ?? d.SectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var topicsMarkedComplete = declarations
            .Where(d => IsSectionCompleteStatus(d.StatusCode))
            .Select(d => layout.FindTopic(d.SectionKey)?.Key ?? d.SectionKey)
            .Where(key => !string.IsNullOrWhiteSpace(key) && !key.StartsWith(SectionCompletePrefix, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        var productDetailsComplete = declarations.Any(d =>
            ServiceSchemaAreas.IsProductDetails(d.SectionKey) && IsSectionCompleteStatus(d.StatusCode));
        var registerContactCount = areas.Any(a => a.Topics.Any(t => ServiceSchemaAreas.IsServiceResponsibility(t.Key)))
            ? await db.CMDBProductContacts.AsNoTracking().CountAsync(c => c.CMDBProductId == productId, ct)
            : 0;

        var complete = productDetailsComplete ? 1 : 0;
        foreach (var area in areas)
        {
            if (area.Topics.Count == 0)
                continue;
            var allRecorded = area.Topics.All(topic =>
            {
                var started = domainKeys.Any(domain => ServiceSchemaAreas.DomainMatches(topic, domain))
                    || (ServiceSchemaAreas.IsServiceResponsibility(topic.Key) && registerContactCount > 0);
                return TopicProgress(started, nothingToRecord.Contains(topic.Key), topicsMarkedComplete.Contains(topic.Key)) == "recorded";
            });
            if (allRecorded)
                complete++;
        }

        return (complete, sectionCount);
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
                HasSro = p.Contacts.Any(c => c.FipsContactRole.Name == "Senior Responsible Officer"),
                HasDescription = p.UserDescription != null && p.UserDescription != "",
                HasDirectorate = p.Directorates.Any(),
                HasBusinessArea = p.BusinessAreas.Any(),
                HasType = p.Types.Count() > 0,
                HasChannel = p.Channels.Count() > 0,
                HasUrl = p.ProductURL != null && p.ProductURL != "",
                HasCategorisation = p.CategorisationItems.Any()
            })
            .FirstOrDefaultAsync(ct);
        if (product == null)
            return null;

        var layout = await ServiceSchemaLayout.LoadAsync(db, ct);
        var auditRequested = ServiceSchemaAreas.IsAudit(areaKey);
        var productDetailsRequested = ServiceSchemaAreas.IsProductDetails(areaKey);
        var requested = auditRequested || productDetailsRequested ? null : layout.FindTopic(topicKey);
        var area = auditRequested || productDetailsRequested
            ? null
            : layout.Find(areaKey);
        if (requested != null)
            area = layout.AreaFor(requested.Key) ?? area;
        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);
        var registerContactCount = await db.CMDBProductContacts.AsNoTracking()
            .CountAsync(c => c.CMDBProductId == productId, ct);
        var activeTopics = area?.Topics ?? Array.Empty<ServiceSchemaTopic>();
        var registerContacts = new List<ServiceSchemaEntryRow>();
        if (activeTopics.Any(t => ServiceSchemaAreas.IsServiceResponsibility(t.Key)))
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
        var lookupSources = activeTopics.Select(t => t.LookupSource);
        var lookups = await ServiceSchemaAdminLookups.LoadAsync(db, lookupSources, ct);
        var kinds = activeTopics.Where(t => t.CatalogueKind != null).Select(t => t.CatalogueKind!).Distinct().ToList();
        var catalogue = kinds.Count == 0
            ? new List<CensusCatalogueItem>()
            : await db.CensusCatalogueItems.AsNoTracking()
                .Where(c => kinds.Contains(c.Kind) && c.RemovedAt == null && (c.StatusCode == "ACTIVE" || c.StatusCode == "NEW"))
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        var catalogueNames = catalogue.ToDictionary(c => c.Id, c => c.Name);
        var ownProvidedApis = entries
            .Where(e => e.CatalogueItemId != null && string.Equals(e.DomainKey, ServiceSchemaAreas.ApiProvideKey, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.CatalogueItemId!.Value)
            .ToHashSet();
        var apiProviders = area == null
            ? new Dictionary<Guid, string>()
            : await ApiProviderLabelsAsync(db, area, productId, ct);

        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => d.ProductId == productId)
            .Select(d => new { d.SectionKey, d.StatusCode })
            .ToListAsync(ct);
        var nothingToRecord = declarations
            .Where(d => IsClosingStatus(d.StatusCode))
            .Select(d => layout.FindTopic(d.SectionKey)?.Key ?? d.SectionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var topicsMarkedComplete = declarations
            .Where(d => IsSectionCompleteStatus(d.StatusCode))
            .Select(d => layout.FindTopic(d.SectionKey)?.Key ?? d.SectionKey)
            .Where(key => !string.IsNullOrWhiteSpace(key) && !key.StartsWith(SectionCompletePrefix, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
        var productDetailsMarkedComplete = declarations.Any(d =>
            ServiceSchemaAreas.IsProductDetails(d.SectionKey) && IsSectionCompleteStatus(d.StatusCode));
        var productDetailsHasInformation = product.HasDescription
            || product.HasPhase
            || product.HasDirectorate
            || product.HasBusinessArea
            || product.HasChannel
            || product.HasType
            || product.HasUrl
            || product.HasCategorisation;
        var productDetailsState = TopicProgress(productDetailsHasInformation, false, productDetailsMarkedComplete);
        var productDetailsNav = new ServiceSchemaAreaNav
        {
            Key = ServiceSchemaAreas.ProductDetailsKey,
            Name = "Product details",
            Summary = "Check and update the core product information used on the service register.",
            HelpPanel = "These fields come from the product Details tab. User groups are recorded in the service census, so they are not listed here. Mark this section complete when the product details are accurate.",
            Group = "Sections",
            State = productDetailsState,
            StateLabel = ProgressLabel(productDetailsState),
            TopicsRecorded = productDetailsState == "recorded" ? 1 : 0,
            TopicCount = 1,
            Topics = []
        };
        var nav = layout.Areas.Select(item =>
        {
            var topicTotal = item.Topics.Count;
            var topicNav = item.Topics.Select(topic =>
            {
                var started = TopicStarted(topic, entries, registerContactCount);
                var itemCount = entries.Count(e => ServiceSchemaAreas.DomainMatches(topic, e.DomainKey));
                if (ServiceSchemaAreas.IsServiceResponsibility(topic.Key))
                    itemCount += registerContactCount;
                var state = TopicProgress(
                    started,
                    nothingToRecord.Contains(topic.Key),
                    topicsMarkedComplete.Contains(topic.Key));
                return new ServiceSchemaTopicNav
                {
                    Key = topic.Key,
                    Name = ServiceSchemaAreas.ShortTopicName(topic),
                    State = state,
                    StateLabel = ProgressLabel(state),
                    ItemCount = itemCount
                };
            }).ToList();
            var topicDone = topicNav.Count(t => t.State == "recorded");
            var areaState = item.Key == "overview" || topicTotal == 0
                ? ""
                : topicNav.All(t => t.State == "recorded") ? "recorded"
                : topicNav.All(t => t.State == "empty") ? "empty"
                : "partial";
            return new ServiceSchemaAreaNav
            {
                Key = item.Key,
                Name = ServiceSchemaAreas.ShortAreaName(item),
                Summary = item.Summary,
                HelpPanel = item.HelpPanel,
                Group = item.Group,
                State = areaState,
                StateLabel = string.IsNullOrEmpty(areaState) ? "" : ProgressLabel(areaState),
                TopicsRecorded = topicDone,
                TopicCount = topicTotal,
                Topics = topicNav
            };
        }).ToList();
        var overviewIndex = nav.FindIndex(n => string.Equals(n.Key, "overview", StringComparison.OrdinalIgnoreCase));
        if (overviewIndex >= 0)
        {
            nav[overviewIndex].Name = "Task summary";
            nav.Insert(overviewIndex + 1, productDetailsNav);
        }
        else
            nav.Insert(0, productDetailsNav);
        var scored = nav.Where(n => n.Key != "overview").ToList();
        var addressed = scored.Count(n => n.State == "recorded");
        var names = product.Areas.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var dq = (product.HasPhase ? 1 : 0) + (product.TypeCount > 0 ? 1 : 0) + (names.Count > 0 ? 1 : 0)
            + (product.UserGroupCount > 0 ? 1 : 0) + (product.ChannelCount > 0 ? 1 : 0)
            + (product.HasOwner ? 1 : 0) + (product.HasSro ? 1 : 0);

        var activeIsProductDetails = productDetailsRequested;
        var activeIsAudit = auditRequested;
        var auditRows = activeIsAudit
            ? await ServiceSchemaAudit.LoadAsync(db, productId, ct)
            : new List<ServiceSchemaAuditRow>();
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
            ActiveArea = activeIsAudit ? ServiceSchemaAreas.AuditKey
                : activeIsProductDetails ? ServiceSchemaAreas.ProductDetailsKey
                : area!.Key,
            SectionComplete = activeIsProductDetails && productDetailsMarkedComplete,
            ActiveTopic = activeIsAudit || activeIsProductDetails ? "" : (requested?.Key ?? ""),
            AreaName = activeIsAudit ? "Audit"
                : activeIsProductDetails ? productDetailsNav.Name
                : string.Equals(area!.Key, "overview", StringComparison.OrdinalIgnoreCase) ? "Task summary"
                : area!.Name,
            AreaGroup = activeIsAudit ? "Audit"
                : activeIsProductDetails ? productDetailsNav.Group
                : area!.Group,
            AreaSummary = activeIsAudit
                ? "Changes recorded for this product’s service census."
                : activeIsProductDetails ? productDetailsNav.Summary
                : area!.Summary,
            AreaHelpPanel = activeIsAudit
                ? "Shows adds, removals, status changes and in-place updates for census answers on this product."
                : activeIsProductDetails ? productDetailsNav.HelpPanel
                : area!.HelpPanel,
            Areas = nav,
            ProductDetails = new ServiceSchemaProductDetailsPanel
            {
                MarkedComplete = productDetailsMarkedComplete,
                HasInformation = productDetailsHasInformation,
                State = productDetailsState,
                StateLabel = ProgressLabel(productDetailsState)
            },
            AuditRows = auditRows,
            Topics = activeIsAudit || activeIsProductDetails
                ? new List<ServiceSchemaTopicBlock>()
                : activeTopics.Select(topic =>
            {
                var choices = topic.Mode switch
                {
                    "lookup" => Choices(lookups, topic.LookupSource),
                    "catalogue" => catalogue.Where(c => string.Equals(c.Kind, topic.CatalogueKind, StringComparison.OrdinalIgnoreCase))
                        .Where(c => topic.Key != ServiceSchemaAreas.ApiUseKey || !ownProvidedApis.Contains(c.Id))
                        .Select(c => new ServiceSchemaChoice
                        {
                            Value = c.Id.ToString(),
                            Label = CatalogueChoiceLabel(c, topic.Key, apiProviders)
                        }).ToList(),
                    "choice" or "yes-choice" => topic.ChoiceOptions.ToList(),
                    _ => new List<ServiceSchemaChoice>()
                };
                var matched = entries.Where(e => ServiceSchemaAreas.DomainMatches(topic, e.DomainKey)).ToList();
                var declaredNone = nothingToRecord.Contains(topic.Key);
                var topicComplete = topicsMarkedComplete.Contains(topic.Key);
                var started = matched.Count > 0 || (ServiceSchemaAreas.IsServiceResponsibility(topic.Key) && registerContacts.Count > 0);
                var progress = TopicProgress(started, declaredNone, topicComplete);
                var topicEntries = matched
                    .Select(e => new ServiceSchemaEntryRow
                    {
                        Id = e.Id,
                        Title = CatalogueTitle(e, catalogueNames, lookups),
                        Narrative = e.Narrative,
                        Meta = topic.Key == ServiceSchemaAreas.ApiUseKey && e.CatalogueItemId is Guid apiId && apiProviders.TryGetValue(apiId, out var providedBy)
                            ? providedBy
                            : PersonMeta(e),
                        Url = e.ExternalUrl,
                        AnswerCode = e.LookupCode,
                        CategoryCode = e.SecondaryLookupCode,
                        LinkedProductId = Guid.TryParse(e.LookupCode, out var linkedId) ? linkedId : null
                    }).ToList();
                if (ServiceSchemaAreas.IsServiceResponsibility(topic.Key))
                    topicEntries = registerContacts.ToList();
                return new ServiceSchemaTopicBlock
                {
                    Key = topic.Key,
                    Heading = topic.Heading,
                    NavLabel = ServiceSchemaAreas.ShortTopicName(topic),
                    Help = topic.Help,
                    HelpPanel = topic.HelpPanel,
                    TitleLabel = topic.TitleLabel,
                    NarrativeLabel = topic.NarrativeLabel,
                    Mode = topic.Mode,
                    CatalogueKind = topic.CatalogueKind,
                    LookupLabel = topic.LookupLabel,
                    LookupAdminPanel = topic.LookupSource,
                    CapturePerson = topic.CapturePerson,
                    CaptureUrl = topic.CaptureUrl,
                    Choices = choices,
                    State = progress,
                    StateLabel = ProgressLabel(progress),
                    NothingToRecord = declaredNone,
                    TopicComplete = topicComplete,
                    Entries = topicEntries
                };
            }).ToList()
        };
    }

    private static string CatalogueChoiceLabel(
        CensusCatalogueItem item,
        string topicKey,
        IReadOnlyDictionary<Guid, string> apiProviders)
    {
        var name = string.Equals(item.StatusCode, "NEW", StringComparison.OrdinalIgnoreCase)
            ? item.Name + " (New)"
            : item.Name;
        if (string.Equals(topicKey, ServiceSchemaAreas.ApiUseKey, StringComparison.OrdinalIgnoreCase)
            && apiProviders.TryGetValue(item.Id, out var providedBy))
            name += " — " + providedBy;
        return name;
    }

    private static async Task<Dictionary<Guid, string>> ApiProviderLabelsAsync(
        CompassDbContext db,
        ServiceSchemaArea area,
        Guid productId,
        CancellationToken ct)
    {
        if (!area.Topics.Any(t => t.Key == ServiceSchemaAreas.ApiUseKey))
            return new Dictionary<Guid, string>();

        var provided = await db.CensusEntries.AsNoTracking()
            .Where(e => e.RemovedAt == null
                && e.ProductId != productId
                && e.CatalogueItemId != null
                && e.DomainKey == ServiceSchemaAreas.ApiProvideKey)
            .Select(e => new { CatalogueItemId = e.CatalogueItemId!.Value, e.ProductId })
            .ToListAsync(ct);
        if (provided.Count == 0)
            return new Dictionary<Guid, string>();

        var productIds = provided.Select(p => p.ProductId).Distinct().ToList();
        var titles = await db.CMDBProducts.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);
        var titleById = titles.ToDictionary(p => p.Id, p => p.Title);
        return provided
            .Where(p => titleById.ContainsKey(p.ProductId))
            .GroupBy(p => p.CatalogueItemId)
            .ToDictionary(
                g => g.Key,
                g => "Provided by " + string.Join(", ", g
                    .Select(p => titleById[p.ProductId])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)));
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

    public const string SectionCompletePrefix = "done:";
    public const string SectionCompleteStatus = "SECTION_COMPLETE";

    public static string SectionCompleteKey(string areaKey) => SectionCompletePrefix + areaKey;

    public static string TopicProgress(bool started, bool nothingToRecord, bool topicMarkedComplete) =>
        nothingToRecord || topicMarkedComplete ? "recorded"
        : started ? "partial"
        : "empty";

    public static string ProgressLabel(string state) => state switch
    {
        "recorded" => "Completed",
        "partial" => "In progress",
        _ => "Not yet started"
    };

    private static bool TopicStarted(ServiceSchemaTopic topic, IReadOnlyList<CensusEntry> entries, int registerContactCount) =>
        entries.Any(e => ServiceSchemaAreas.DomainMatches(topic, e.DomainKey)) ||
        (ServiceSchemaAreas.IsServiceResponsibility(topic.Key) && registerContactCount > 0);

    public static bool IsSectionCompleteStatus(string? status) =>
        string.Equals(status, SectionCompleteStatus, StringComparison.OrdinalIgnoreCase);

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
