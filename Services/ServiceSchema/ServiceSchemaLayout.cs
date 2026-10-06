using Compass.Data;
using Compass.Models.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public sealed class ServiceSchemaLayout
{
    private ServiceSchemaLayout(IReadOnlyList<ServiceSchemaArea> areas)
    {
        Areas = areas;
        Topics = areas.SelectMany(a => a.Topics).ToList();
    }

    public IReadOnlyList<ServiceSchemaArea> Areas { get; }

    public IReadOnlyList<ServiceSchemaTopic> Topics { get; }

    public ServiceSchemaArea Find(string? key) =>
        Areas.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? Areas.FirstOrDefault()
        ?? ServiceSchemaAreas.All[0];

    public ServiceSchemaTopic? FindTopic(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        return Topics.FirstOrDefault(topic => ServiceSchemaAreas.DomainMatches(topic, key));
    }

    public ServiceSchemaArea? AreaFor(string topicKey) =>
        Areas.FirstOrDefault(a => a.Topics.Any(t => string.Equals(t.Key, topicKey, StringComparison.OrdinalIgnoreCase)));

    public static async Task<ServiceSchemaLayout> LoadAsync(CompassDbContext db, CancellationToken ct)
    {
        await EnsureDefaultsAsync(db, ct);
        var anyConfigured = await db.ServiceSchemaAreaConfigs.AsNoTracking().AnyAsync(ct);
        var areaRows = await db.ServiceSchemaAreaConfigs.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync(ct);
        var questionRows = await db.ServiceSchemaQuestions.AsNoTracking()
            .Where(q => q.IsActive)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Heading)
            .ToListAsync(ct);

        var areas = areaRows.Select(row => new ServiceSchemaArea
        {
            Key = row.Key,
            Name = row.Name,
            Group = row.Group,
            Summary = row.Summary,
            HelpPanel = row.HelpPanel,
            Topics = questionRows
                .Where(q => string.Equals(q.AreaKey, row.Key, StringComparison.OrdinalIgnoreCase))
                .Select(ToTopic)
                .ToList()
        }).ToList();

        return !anyConfigured
            ? new ServiceSchemaLayout(ServiceSchemaAreas.All)
            : new ServiceSchemaLayout(areas);
    }

    public static async Task EnsureDefaultsAsync(CompassDbContext db, CancellationToken ct)
    {
        var unset = await db.ServiceSchemaQuestions
            .Where(q => q.ResponseMode == null || q.ResponseMode == "")
            .ToListAsync(ct);
        var added = false;
        foreach (var question in unset)
        {
            var builtIn = ServiceSchemaAreas.FindTopic(question.Key);
            question.ResponseMode = builtIn?.Mode ?? ServiceSchemaResponseModes.Text;
            question.LookupSource = builtIn?.LookupSource;
            question.CatalogueKind = builtIn?.CatalogueKind;
            added = true;
        }

        var apiQuestions = await db.ServiceSchemaQuestions
            .Where(q => q.Key == ServiceSchemaAreas.ApiProvideKey || q.Key == ServiceSchemaAreas.ApiUseKey)
            .ToListAsync(ct);
        foreach (var question in apiQuestions)
        {
            if (string.Equals(question.ResponseMode, ServiceSchemaResponseModes.Catalogue, StringComparison.OrdinalIgnoreCase)
                && string.Equals(question.CatalogueKind, ServiceSchemaAreas.ApiCatalogueKind, StringComparison.OrdinalIgnoreCase))
                continue;
            question.ResponseMode = ServiceSchemaResponseModes.Catalogue;
            question.CatalogueKind = ServiceSchemaAreas.ApiCatalogueKind;
            question.UpdatedAt = DateTime.UtcNow;
            added = true;
        }

        var misplacedApis = await db.CensusCatalogueItems
            .Where(c => c.Kind == ServiceSchemaAreas.ApiProvideKey || c.Kind == ServiceSchemaAreas.ApiUseKey)
            .ToListAsync(ct);
        if (misplacedApis.Count > 0)
        {
            var taken = (await db.CensusCatalogueItems.AsNoTracking()
                    .Where(c => c.Kind == ServiceSchemaAreas.ApiCatalogueKind)
                    .Select(c => c.Reference)
                    .ToListAsync(ct))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var item in misplacedApis)
            {
                if (taken.Contains(item.Reference))
                    item.Reference = Guid.NewGuid().ToString("N")[..32].ToUpperInvariant();
                item.Kind = ServiceSchemaAreas.ApiCatalogueKind;
                taken.Add(item.Reference);
            }
            added = true;
        }

        var areaKeys = await db.ServiceSchemaAreaConfigs.Select(a => a.Key).ToListAsync(ct);
        var questionKeys = await db.ServiceSchemaQuestions.Select(q => q.Key).ToListAsync(ct);
        var knownAreas = areaKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownQuestions = questionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var areaOrder = areaKeys.Count;
        var inserted = new List<string>();
        foreach (var area in ServiceSchemaAreas.All)
        {
            if (knownAreas.Add(area.Key))
            {
                db.ServiceSchemaAreaConfigs.Add(new ServiceSchemaAreaConfig
                {
                    Key = area.Key,
                    Name = area.Name,
                    Group = area.Group,
                    Summary = area.Summary,
                    SortOrder = areaOrder++,
                    IsActive = true
                });
                added = true;
            }

            var questionOrder = 0;
            foreach (var topic in area.Topics)
            {
                if (!knownQuestions.Add(topic.Key))
                {
                    questionOrder++;
                    continue;
                }

                db.ServiceSchemaQuestions.Add(new ServiceSchemaQuestion
                {
                    Key = topic.Key,
                    AreaKey = area.Key,
                    Heading = topic.Heading,
                    Help = topic.Help,
                    TitleLabel = topic.TitleLabel,
                    NarrativeLabel = topic.NarrativeLabel,
                    ResponseMode = topic.Mode,
                    LookupSource = topic.LookupSource,
                    CatalogueKind = topic.CatalogueKind,
                    ChoiceOptions = topic.ChoiceOptions.Count == 0 ? null : string.Join('\n', topic.ChoiceOptions.Select(c => c.Label)),
                    SortOrder = questionOrder,
                    IsActive = true,
                    IsBuiltIn = true,
                    UpdatedAt = DateTime.UtcNow
                });
                added = true;
                inserted.Add(topic.Key);
                questionOrder++;
            }
        }

        if (await AlignBuiltInWordingAsync(db, ct))
            added = true;
        if (inserted.Count > 0 && await PlaceInsertedTopicsAsync(db, inserted, ct))
            added = true;

        if (!added)
            return;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }

    private static async Task<bool> AlignBuiltInWordingAsync(CompassDbContext db, CancellationToken ct)
    {
        var changed = false;
        var questions = await db.ServiceSchemaQuestions
            .Where(q => q.Key == "pattern" || q.Key == "technology" || q.Key == "dependency" || q.Key == "responsibility" || q.Key == ServiceSchemaAreas.ServiceResponsibilityKey)
            .ToListAsync(ct);
        foreach (var question in questions)
        {
            if (question.Key == "pattern"
                && question.Heading == "Patterns, components and APIs"
                && question.Help == "Patterns, components, or APIs this product provides to other services. Record ones this product uses under Technology.")
            {
                question.Heading = "Patterns and components";
                question.Help = "Patterns and components this product provides to other services.";
                question.TitleLabel = "Pattern or component";
                question.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
            else if (question.Key == "technology"
                && question.Heading == "Technologies and components"
                && question.Help == "Technology, components, and APIs this product uses.")
            {
                question.Help = "Technology and components this product uses.";
                question.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
            else if (question.Key == "dependency"
                && question.Heading == "Dependencies"
                && question.Help == "What this product depends on.")
            {
                question.Heading = "Other product dependencies";
                question.Help = "Products and services this product depends on. Internal dependencies are on the service register, including Enterprise services. External dependencies are outside DfE.";
                question.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
            else if (question.Key == "responsibility"
                && (question.Heading == "Responsibilities"
                    || question.Help == "People already named on the service register, and any further responsibilities."))
            {
                question.Heading = "Additional roles and responsibilities";
                question.Help = "Further people with a role or responsibility for this product who are not already named as service offering contacts.";
                question.TitleLabel = "Role or responsibility";
                question.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
            else if (question.Key == ServiceSchemaAreas.ServiceResponsibilityKey
                && (question.Help == "People already named as contacts on this service register product. Change these on the product record."
                    || string.IsNullOrWhiteSpace(question.HelpPanel)))
            {
                question.Help = "These contacts come from the CMDB and cannot be changed here. Use the update service offering form in ServiceNow if they need changing. Add further contacts in Additional roles and responsibilities.";
                question.HelpPanel = "Service offering contacts are maintained in the CMDB, not in Compass.\n\nIf you need to update these contacts, complete the update service offering form in ServiceNow.\n\nYou can add additional contacts such as architects, designers and analysts in the next section, Additional roles and responsibilities.";
                question.UpdatedAt = DateTime.UtcNow;
                changed = true;
            }
        }

        var schemaGroups = await db.ServiceSchemaAreaConfigs
            .Where(a => a.Group == "Schema" || a.Group == "Evidence and review" || a.Group == "This record")
            .ToListAsync(ct);
        foreach (var area in schemaGroups)
        {
            area.Group = "Sections";
            changed = true;
        }

        var overviewAreas = await db.ServiceSchemaAreaConfigs
            .Where(a => a.Key == "overview" && a.Name == "Completion")
            .ToListAsync(ct);
        foreach (var area in overviewAreas)
        {
            area.Name = "Task summary";
            changed = true;
        }

        var areas = await db.ServiceSchemaAreaConfigs
            .Where(a => a.Key == "functionality" || a.Key == "technology" || a.Key == "purpose" || a.Key == "ownership")
            .ToListAsync(ct);
        foreach (var area in areas)
        {
            if (area.Key == "functionality"
                && area.Summary == "Features this product implements, and the patterns, components and APIs it provides to other services.")
            {
                area.Summary = "Features this product implements, patterns and components it provides, and APIs other services can use.";
                changed = true;
            }
            else if (area.Key == "purpose"
                && area.Summary == "Outcomes, benefits and the strategic objectives this product supports.")
            {
                area.Summary = "Outcomes, benefits, strategic objectives, and whether this product provides enabling functionality.";
                changed = true;
            }
            else if (area.Key == "technology"
                && area.Summary == "Technology, components, APIs, platforms, integrations and dependencies this product uses.")
            {
                area.Summary = "Technology, components, platforms, integrations, dependencies, and APIs this product uses.";
                changed = true;
            }
            else if (area.Key == "ownership"
                && (area.Summary == "Service owner and the other contacts on the register, plus any further responsibilities."
                    || area.Summary == "Service owner and the other contacts on the register, plus any further responsibilities"))
            {
                area.Summary = "Service offering contacts on the register, plus any further roles and responsibilities.";
                changed = true;
            }
        }

        return changed;
    }

    private static async Task<bool> PlaceInsertedTopicsAsync(CompassDbContext db, List<string> inserted, CancellationToken ct)
    {
        var changed = false;
        changed |= await PlaceAfterAsync(db, inserted, "functionality", "pattern", ServiceSchemaAreas.ApiProvideKey, ct);
        changed |= await PlaceAfterAsync(db, inserted, "technology", "technology", ServiceSchemaAreas.ApiUseKey, ct);
        changed |= await PlaceAfterAsync(db, inserted, "purpose", "strategic-objective", ServiceSchemaAreas.EnablingProductKey, ct);
        changed |= await PlaceBeforeAsync(db, inserted, "ownership", ServiceSchemaAreas.AdditionalResponsibilityKey, ServiceSchemaAreas.ServiceResponsibilityKey, ct);
        return changed;
    }

    private static async Task<bool> PlaceBeforeAsync(
        CompassDbContext db,
        List<string> inserted,
        string areaKey,
        string beforeKey,
        string key,
        CancellationToken ct)
    {
        if (!inserted.Contains(key, StringComparer.OrdinalIgnoreCase))
            return false;

        var questions = await db.ServiceSchemaQuestions.Where(q => q.AreaKey == areaKey).ToListAsync(ct);
        var added = db.ServiceSchemaQuestions.Local.FirstOrDefault(q =>
            string.Equals(q.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(q.Key, key, StringComparison.OrdinalIgnoreCase));
        if (added != null && !questions.Contains(added))
            questions.Add(added);
        var before = questions.FirstOrDefault(q => string.Equals(q.Key, beforeKey, StringComparison.OrdinalIgnoreCase));
        if (added == null || before == null)
            return false;

        var desired = before.SortOrder;
        foreach (var question in questions)
        {
            if (question.Id == added.Id || ReferenceEquals(question, added))
                continue;
            if (question.SortOrder >= desired)
                question.SortOrder++;
        }

        added.SortOrder = desired;
        return true;
    }

    private static async Task<bool> PlaceAfterAsync(
        CompassDbContext db,
        List<string> inserted,
        string areaKey,
        string afterKey,
        string key,
        CancellationToken ct)
    {
        if (!inserted.Contains(key, StringComparer.OrdinalIgnoreCase))
            return false;

        var questions = await db.ServiceSchemaQuestions.Where(q => q.AreaKey == areaKey).ToListAsync(ct);
        var added = db.ServiceSchemaQuestions.Local.FirstOrDefault(q =>
            string.Equals(q.AreaKey, areaKey, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(q.Key, key, StringComparison.OrdinalIgnoreCase));
        if (added != null && !questions.Contains(added))
            questions.Add(added);
        var after = questions.FirstOrDefault(q => string.Equals(q.Key, afterKey, StringComparison.OrdinalIgnoreCase));
        if (added == null || after == null)
            return false;

        var desired = after.SortOrder + 1;
        foreach (var question in questions)
        {
            if (question.Id == added.Id || ReferenceEquals(question, added))
                continue;
            if (question.SortOrder >= desired)
                question.SortOrder++;
        }

        added.SortOrder = desired;
        return true;
    }

    private static IReadOnlyList<ServiceSchemaChoice> StoredOrBuiltInChoices(string? stored, ServiceSchemaTopic? code)
    {
        var parsed = ServiceSchemaResponseModes.ParseChoices(stored);
        if (parsed.Count == 0)
            return code?.ChoiceOptions ?? [];
        if (code == null)
            return parsed;
        foreach (var choice in parsed)
        {
            var match = code.ChoiceOptions.FirstOrDefault(c =>
                string.Equals(c.Label, choice.Label, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Value, choice.Value, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                choice.Hint = match.Hint;
        }
        return parsed;
    }

    private static ServiceSchemaTopic ToTopic(ServiceSchemaQuestion question)
    {
        var code = ServiceSchemaAreas.FindTopic(question.Key);
        var mode = string.IsNullOrWhiteSpace(question.ResponseMode) ? code?.Mode ?? "text" : question.ResponseMode;
        var sameMode = code != null && string.Equals(mode, code.Mode, StringComparison.OrdinalIgnoreCase);
        return new ServiceSchemaTopic
        {
            Key = question.Key,
            Heading = question.Heading,
            Help = question.Help,
            HelpPanel = question.HelpPanel,
            TitleLabel = question.TitleLabel,
            NarrativeLabel = question.NarrativeLabel,
            Mode = mode,
            CatalogueKind = mode == "catalogue" ? question.CatalogueKind ?? code?.CatalogueKind ?? question.Key : null,
            LookupSource = mode == "lookup" ? question.LookupSource ?? code?.LookupSource : null,
            LookupLabel = question.TitleLabel,
            CapturePerson = sameMode && code!.CapturePerson,
            CaptureUrl = sameMode && code!.CaptureUrl,
            ChoiceOptions = mode is "choice" or "yes-choice"
                ? StoredOrBuiltInChoices(question.ChoiceOptions, code)
                : [],
            NavLabel = code != null && !string.IsNullOrWhiteSpace(code.NavLabel) && string.Equals(question.Heading, code.Heading, StringComparison.Ordinal)
                ? code.NavLabel
                : question.Heading
        };
    }
}
