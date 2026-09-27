using System.Globalization;
using System.Text.Json;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Database-backed shared census theme catalogue. Seed once from
/// <see cref="CoreCensusThemeCatalog"/>; thereafter admin edits are the source of truth
/// for every census.
/// </summary>
public sealed class CoreCensusThemeService : ICoreCensusThemeService
{
    private static readonly HashSet<string> UnsupportedSchemaTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "EVIDENCE_REF",
        "ENTITY_REF",
        "EXTERNAL_REF",
        "UUID",
        "STATE",
        "DATETIME"
    };

    private readonly CompassDbContext _db;
    private readonly IServiceDataModelAccessService _access;

    public CoreCensusThemeService(CompassDbContext db, IServiceDataModelAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        // Catalogue already populated — database is the only source of truth.
        if (await _db.CoreCensusThemes.AnyAsync(cancellationToken))
        {
            await RepairDuplicateSectionStatusKeysAsync(cancellationToken);
            await RepairCatalogFieldDefinitionsAsync(cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var def in CoreCensusThemeCatalog.All)
        {
            var theme = new CoreCensusTheme
            {
                Id = Guid.NewGuid(),
                StableKey = def.StableKey,
                Name = def.Name,
                Guidance = def.Guidance,
                SortOrder = def.SortOrder,
                IsServiceOffering = def.IsServiceOffering,
                IsActive = true,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            foreach (var fieldDef in def.Fields)
            {
                if (IsUnsupported(fieldDef.SchemaSourceType))
                    continue;
                theme.Fields.Add(MapField(fieldDef, theme.Id));
            }

            _db.CoreCensusThemes.Add(theme);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Idempotent repair: each theme's <c>section-status</c> field is renamed to
    /// <c>{themeKey}-section-status</c>, and answer / proposed-change rows that still
    /// point at those fields by FieldId are remapped so answers are not dropped.
    /// </summary>
    internal async Task RepairDuplicateSectionStatusKeysAsync(CancellationToken cancellationToken = default)
    {
        var duplicates = await _db.CoreCensusThemeFields
            .Include(f => f.Theme)
            .Where(f => f.StableKey == "section-status")
            .ToListAsync(cancellationToken);

        if (duplicates.Count == 0)
            return;

        var renames = new List<(Guid FieldId, string NewKey)>();
        foreach (var field in duplicates)
        {
            var themeKey = field.Theme?.StableKey;
            if (string.IsNullOrWhiteSpace(themeKey))
                continue;

            var newKey = CoreCensusThemeCatalog.SectionStatusStableKey(themeKey);

            // Avoid colliding with an already-renamed sibling.
            var clash = await _db.CoreCensusThemeFields.AnyAsync(
                f => f.Id != field.Id && f.StableKey == newKey, cancellationToken);
            if (clash)
            {
                var candidate = $"{newKey}-{field.Id:N}";
                newKey = candidate.Length <= 100 ? candidate : candidate[..100];
            }

            renames.Add((field.Id, newKey));
            field.StableKey = newKey;
            field.Theme!.UpdatedUtc = DateTime.UtcNow;
        }

        if (renames.Count == 0)
            return;

        foreach (var (fieldId, newKey) in renames)
        {
            var answers = await _db.ServiceDataModelAnswers
                .Where(a => a.ServiceDataModelFieldId == fieldId)
                .ToListAsync(cancellationToken);
            foreach (var answer in answers)
                answer.FieldStableKey = newKey;

            var changes = await _db.ServiceDataModelProposedRegisterChanges
                .Where(p => p.ServiceDataModelFieldId == fieldId)
                .ToListAsync(cancellationToken);
            foreach (var change in changes)
                change.FieldStableKey = newKey;
        }

        // Answers stored only by FieldStableKey (FieldId null) cannot be attributed per theme.
        // Map them to the first renamed key so the value is kept rather than orphaned.
        var firstNewKey = renames
            .Select(r => (r.NewKey, Sort: duplicates.First(d => d.Id == r.FieldId).Theme.SortOrder))
            .OrderBy(x => x.Sort)
            .Select(x => x.NewKey)
            .First();

        var orphanAnswers = await _db.ServiceDataModelAnswers
            .Where(a => a.FieldStableKey == "section-status" && a.ServiceDataModelFieldId == null)
            .ToListAsync(cancellationToken);
        foreach (var answer in orphanAnswers)
            answer.FieldStableKey = firstNewKey;

        var orphanChanges = await _db.ServiceDataModelProposedRegisterChanges
            .Where(p => p.FieldStableKey == "section-status" && p.ServiceDataModelFieldId == null)
            .ToListAsync(cancellationToken);
        foreach (var change in orphanChanges)
            change.FieldStableKey = firstNewKey;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Idempotent repair for structural catalogue gaps only.
    /// Does <strong>not</strong> overwrite admin-edited field type, label, guidance, allow-multiple, or options lookup.
    /// May force Services/ServiceLines list types and fill a missing OptionsLookupKey from the catalogue.
    /// </summary>
    internal async Task RepairCatalogFieldDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var catalogByKey = CoreCensusThemeCatalog.Importable
            .SelectMany(t => t.Fields.Select(f => (ThemeKey: t.StableKey, Field: f)))
            .ToDictionary(x => x.Field.StableKey, x => x.Field, StringComparer.OrdinalIgnoreCase);

        var fields = await _db.CoreCensusThemeFields
            .Include(f => f.Theme)
            .Where(f => !f.Theme.IsServiceOffering)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var field in fields)
        {
            if (!catalogByKey.TryGetValue(field.StableKey, out var def))
                continue;

            var fieldChanged = false;

            // Preserve admin FieldType/Label/Guidance/AllowMultiple. Only force structural list types.
            if (def.FieldType is ServiceDataModelFieldType.Services or ServiceDataModelFieldType.ServiceLines
                && field.FieldType != def.FieldType)
            {
                field.FieldType = def.FieldType;
                fieldChanged = true;
            }

            var desiredLookup = string.IsNullOrWhiteSpace(def.OptionsLookupKey) ? null : def.OptionsLookupKey.Trim();
            var currentLookup = string.IsNullOrWhiteSpace(field.OptionsLookupKey) ? null : field.OptionsLookupKey.Trim();
            // Fill a missing binding from the catalogue; never clear or replace an admin-set lookup.
            if (desiredLookup != null && currentLookup == null)
            {
                field.OptionsLookupKey = desiredLookup;
                fieldChanged = true;
            }

            if (fieldChanged)
            {
                field.Theme.UpdatedUtc = DateTime.UtcNow;
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoreCensusTheme>> GetSharedStructureAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSeededAsync(cancellationToken);

        return await _db.CoreCensusThemes
            .AsNoTracking()
            .Include(t => t.Fields)
            .ThenInclude(f => f.Options)
            .Where(t => t.IsActive && !t.IsServiceOffering)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoreCensusThemeListItem>> ListManagedThemesAsync(
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return Array.Empty<CoreCensusThemeListItem>();

        await EnsureSeededAsync(cancellationToken);

        return await _db.CoreCensusThemes.AsNoTracking()
            .Where(t => !t.IsServiceOffering)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .Select(t => new CoreCensusThemeListItem
            {
                Id = t.Id,
                StableKey = t.StableKey,
                Name = t.Name,
                Guidance = t.Guidance,
                SortOrder = t.SortOrder,
                FieldCount = t.Fields.Count(f => !f.IsDisabled),
                IsActive = t.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CoreCensusTheme?> GetManagedThemeAsync(
        Guid themeId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        await EnsureSeededAsync(cancellationToken);

        return await _db.CoreCensusThemes
            .Include(t => t.Fields)
            .ThenInclude(f => f.Options)
            .FirstOrDefaultAsync(t => t.Id == themeId && !t.IsServiceOffering, cancellationToken);
    }

    public async Task<Guid?> CreateThemeAsync(
        ServiceDataModelGroupInput input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        await EnsureSeededAsync(cancellationToken);

        var name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            return null;

        var existingKeys = await _db.CoreCensusThemes
            .Select(t => t.StableKey)
            .ToListAsync(cancellationToken);
        // Service offering key is reserved even if somehow absent from the table.
        if (!existingKeys.Contains(CoreCensusThemeCatalog.ServiceOfferingKey, StringComparer.OrdinalIgnoreCase))
            existingKeys.Add(CoreCensusThemeCatalog.ServiceOfferingKey);

        var key = CensusStableKeyHelper.EnsureUnique(
            CensusStableKeyHelper.FromName(name, fallback: "theme"),
            existingKeys);

        var maxSort = await _db.CoreCensusThemes
            .Where(t => !t.IsServiceOffering)
            .Select(t => (int?)t.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTime.UtcNow;
        var theme = new CoreCensusTheme
        {
            StableKey = key,
            Name = name,
            Guidance = NullIfWhiteSpace(input.Guidance),
            SortOrder = input.SortOrder > 0 ? input.SortOrder : maxSort + 10,
            IsServiceOffering = false,
            IsActive = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        _db.CoreCensusThemes.Add(theme);
        WriteAudit("CoreCensusTheme", theme.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { theme.StableKey, theme.Name }));
        await _db.SaveChangesAsync(cancellationToken);
        return theme.Id;
    }

    public async Task<bool> UpdateThemeAsync(
        Guid themeId,
        ServiceDataModelGroupInput input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var theme = await _db.CoreCensusThemes.FirstOrDefaultAsync(
            t => t.Id == themeId && !t.IsServiceOffering, cancellationToken);
        if (theme == null)
            return false;

        var name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            return false;

        // Stable key is immutable — ignore any posted value so answers stay linked.
        theme.Name = name;
        theme.Guidance = NullIfWhiteSpace(input.Guidance);
        if (input.SortOrder > 0)
            theme.SortOrder = input.SortOrder;
        theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusTheme", theme.Id.ToString(), "Update", actorEmail,
            JsonSerializer.Serialize(new { theme.StableKey, theme.Name, theme.SortOrder }));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetThemeActiveAsync(
        Guid themeId,
        bool isActive,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var theme = await _db.CoreCensusThemes.FirstOrDefaultAsync(
            t => t.Id == themeId && !t.IsServiceOffering, cancellationToken);
        if (theme == null)
            return false;

        theme.IsActive = isActive;
        theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusTheme", theme.Id.ToString(), isActive ? "Enable" : "Disable", actorEmail,
            JsonSerializer.Serialize(new { theme.StableKey, theme.Name }));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReorderThemesAsync(
        IReadOnlyList<Guid> orderedThemeIds,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail) || orderedThemeIds.Count == 0)
            return false;

        var themes = await _db.CoreCensusThemes
            .Where(t => !t.IsServiceOffering)
            .ToListAsync(cancellationToken);
        if (themes.Count == 0)
            return false;

        var byId = themes.ToDictionary(t => t.Id);
        var order = 10;
        var now = DateTime.UtcNow;
        foreach (var id in orderedThemeIds)
        {
            if (!byId.TryGetValue(id, out var theme))
                continue;
            theme.SortOrder = order;
            theme.UpdatedUtc = now;
            order += 10;
        }

        foreach (var theme in themes.Where(t => !orderedThemeIds.Contains(t.Id)).OrderBy(t => t.SortOrder))
        {
            theme.SortOrder = order;
            theme.UpdatedUtc = now;
            order += 10;
        }

        WriteAudit("CoreCensusTheme", "catalogue", "ReorderThemes", actorEmail,
            JsonSerializer.Serialize(new { orderedThemeIds }));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Guid?> AddFieldAsync(
        Guid themeId,
        ServiceDataModelFieldInput input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var theme = await _db.CoreCensusThemes.FirstOrDefaultAsync(
            t => t.Id == themeId && !t.IsServiceOffering, cancellationToken);
        if (theme == null)
            return null;

        var label = input.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(label))
            return null;

        var existingKeys = await _db.CoreCensusThemeFields
            .Select(f => f.StableKey)
            .ToListAsync(cancellationToken);
        var key = CensusStableKeyHelper.EnsureUnique(
            CensusStableKeyHelper.FromName(label, fallback: "question"),
            existingKeys);

        if (input.SortOrder <= 0)
        {
            var maxSort = await _db.CoreCensusThemeFields
                .Where(f => f.CoreCensusThemeId == themeId)
                .Select(f => (int?)f.SortOrder)
                .MaxAsync(cancellationToken) ?? 0;
            input.SortOrder = maxSort + 1;
        }

        input.StableKey = key;
        input.Label = label;
        var field = MapFieldFromInput(input, themeId);
        _db.CoreCensusThemeFields.Add(field);
        theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusThemeField", field.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { themeId, field.StableKey, field.Label }));
        await _db.SaveChangesAsync(cancellationToken);
        return field.Id;
    }

    public async Task<bool> UpdateFieldAsync(
        Guid fieldId,
        ServiceDataModelFieldInput input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var field = await _db.CoreCensusThemeFields
            .Include(f => f.Theme)
            .FirstOrDefaultAsync(f => f.Id == fieldId && !f.Theme.IsServiceOffering, cancellationToken);
        if (field == null)
            return false;

        var label = input.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(label))
            return false;

        // Stable key is immutable — ignore any posted value so answers stay linked.
        field.Label = label;
        field.Guidance = NullIfWhiteSpace(input.Guidance);
        field.FieldType = input.FieldType;
        field.IsMandatory = input.IsMandatory;
        field.CountsTowardsCompletion = input.CountsTowardsCompletion;
        field.IsReportable = input.IsReportable;
        if (input.SortOrder > 0)
            field.SortOrder = input.SortOrder;
        field.VisibilityRuleJson = NullIfWhiteSpace(input.VisibilityRuleJson);
        field.CanonicalAttributeKey = NullIfWhiteSpace(input.CanonicalAttributeKey);
        field.ValidationPattern = NullIfWhiteSpace(input.ValidationPattern);
        field.MinNumber = input.MinNumber;
        field.MaxNumber = input.MaxNumber;
        field.OptionsLookupKey = NullIfWhiteSpace(input.OptionsLookupKey);
        field.AllowMultiple = input.AllowMultiple;
        field.Theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusThemeField", field.Id.ToString(), "Update", actorEmail,
            JsonSerializer.Serialize(new { field.StableKey, field.Label, field.OptionsLookupKey, field.AllowMultiple }));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetFieldDisabledAsync(
        Guid fieldId,
        bool isDisabled,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var field = await _db.CoreCensusThemeFields
            .Include(f => f.Theme)
            .FirstOrDefaultAsync(f => f.Id == fieldId && !f.Theme.IsServiceOffering, cancellationToken);
        if (field == null)
            return false;

        field.IsDisabled = isDisabled;
        field.Theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusThemeField", field.Id.ToString(), isDisabled ? "Disable" : "Enable", actorEmail,
            JsonSerializer.Serialize(new { field.StableKey, field.Label }));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReorderFieldsAsync(
        Guid themeId,
        IReadOnlyList<Guid> orderedFieldIds,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail) || orderedFieldIds.Count == 0)
            return false;

        var theme = await _db.CoreCensusThemes.FirstOrDefaultAsync(
            t => t.Id == themeId && !t.IsServiceOffering, cancellationToken);
        if (theme == null)
            return false;

        var fields = await _db.CoreCensusThemeFields
            .Where(f => f.CoreCensusThemeId == themeId)
            .ToListAsync(cancellationToken);
        if (fields.Count == 0)
            return false;

        var byId = fields.ToDictionary(f => f.Id);
        var order = 1;
        foreach (var id in orderedFieldIds)
        {
            if (!byId.TryGetValue(id, out var field))
                continue;
            field.SortOrder = order++;
        }

        foreach (var field in fields.Where(f => !orderedFieldIds.Contains(f.Id)).OrderBy(f => f.SortOrder))
            field.SortOrder = order++;

        theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusTheme", themeId.ToString(), "ReorderFields", actorEmail,
            JsonSerializer.Serialize(new { orderedFieldIds }));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Guid?> AddFieldOptionAsync(
        Guid fieldId,
        ServiceDataModelFieldOptionInput input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var field = await _db.CoreCensusThemeFields
            .Include(f => f.Theme)
            .FirstOrDefaultAsync(f => f.Id == fieldId && !f.Theme.IsServiceOffering, cancellationToken);
        if (field == null)
            return null;

        var maxSort = await _db.CoreCensusThemeFieldOptions
            .Where(o => o.CoreCensusThemeFieldId == fieldId)
            .Select(o => (int?)o.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var option = new CoreCensusThemeFieldOption
        {
            CoreCensusThemeFieldId = fieldId,
            ValueKey = input.ValueKey.Trim(),
            Label = input.Label.Trim(),
            SortOrder = input.SortOrder > 0 ? input.SortOrder : maxSort + 1
        };
        _db.CoreCensusThemeFieldOptions.Add(option);
        field.Theme.UpdatedUtc = DateTime.UtcNow;
        WriteAudit("CoreCensusThemeFieldOption", option.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { fieldId, option.ValueKey, option.Label }));
        await _db.SaveChangesAsync(cancellationToken);
        return option.Id;
    }

    private void WriteAudit(string entityType, string entityId, string action, string actorEmail, string? metadataJson)
    {
        _db.ServiceDataModelAuditEvents.Add(new ServiceDataModelAuditEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorEmail = string.IsNullOrWhiteSpace(actorEmail) ? null : actorEmail.Trim(),
            OccurredUtc = DateTime.UtcNow,
            MetadataJson = metadataJson is { Length: > 4000 } ? metadataJson[..4000] : metadataJson
        });
    }

    private static bool IsUnsupported(string? schemaSourceType) =>
        !string.IsNullOrWhiteSpace(schemaSourceType) && UnsupportedSchemaTypes.Contains(schemaSourceType);

    private static CoreCensusThemeField MapField(CoreFieldDefinition def, Guid themeId)
    {
        var field = new CoreCensusThemeField
        {
            Id = Guid.NewGuid(),
            CoreCensusThemeId = themeId,
            StableKey = def.StableKey,
            Label = def.Label,
            Guidance = def.Guidance,
            FieldType = def.FieldType,
            SchemaSourceType = def.SchemaSourceType,
            IsMandatory = def.IsMandatory,
            CountsTowardsCompletion = def.CountsTowardsCompletion,
            IsReportable = true,
            SortOrder = def.SortOrder,
            VisibilityRuleJson = def.VisibilityRuleJson,
            OptionsLookupKey = string.IsNullOrWhiteSpace(def.OptionsLookupKey) ? null : def.OptionsLookupKey.Trim(),
            AllowMultiple = def.AllowMultiple,
            IsDisabled = false
        };

        foreach (var opt in def.Options)
        {
            field.Options.Add(new CoreCensusThemeFieldOption
            {
                Id = Guid.NewGuid(),
                ValueKey = opt.ValueKey,
                Label = opt.Label,
                SortOrder = opt.SortOrder
            });
        }

        return field;
    }

    private static CoreCensusThemeField MapFieldFromInput(ServiceDataModelFieldInput input, Guid themeId) =>
        new()
        {
            CoreCensusThemeId = themeId,
            StableKey = input.StableKey.Trim(),
            Label = input.Label.Trim(),
            Guidance = NullIfWhiteSpace(input.Guidance),
            FieldType = input.FieldType,
            IsMandatory = input.IsMandatory,
            CountsTowardsCompletion = input.CountsTowardsCompletion,
            IsReportable = input.IsReportable,
            SortOrder = input.SortOrder,
            VisibilityRuleJson = NullIfWhiteSpace(input.VisibilityRuleJson),
            CanonicalAttributeKey = NullIfWhiteSpace(input.CanonicalAttributeKey),
            ValidationPattern = NullIfWhiteSpace(input.ValidationPattern),
            MinNumber = input.MinNumber,
            MaxNumber = input.MaxNumber,
            OptionsLookupKey = NullIfWhiteSpace(input.OptionsLookupKey),
            AllowMultiple = input.AllowMultiple,
            IsDisabled = false
        };

    public async Task<CensusThemeSchemaPackage?> ExportSchemaAsync(
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        await EnsureSeededAsync(cancellationToken);

        var themes = await _db.CoreCensusThemes
            .AsNoTracking()
            .Include(t => t.Fields)
            .ThenInclude(f => f.Options)
            .Where(t => !t.IsServiceOffering)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);

        var capabilities = await _db.CapabilityLookups
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Title)
            .ToListAsync(cancellationToken);

        return new CensusThemeSchemaPackage
        {
            SchemaVersion = CensusThemeSchemaPackage.CurrentSchemaVersion,
            ExportedAtUtc = DateTime.UtcNow,
            Themes = themes.Select(t => new CensusThemeSchemaThemeDto
            {
                StableKey = t.StableKey,
                Name = t.Name,
                Guidance = t.Guidance,
                SortOrder = t.SortOrder,
                Disabled = !t.IsActive,
                Questions = t.Fields
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Label)
                    .Select(f => new CensusThemeSchemaQuestionDto
                    {
                        StableKey = f.StableKey,
                        Label = f.Label,
                        Guidance = f.Guidance,
                        FieldType = f.FieldType.ToString(),
                        Options = f.Options
                            .OrderBy(o => o.SortOrder)
                            .ThenBy(o => o.Label)
                            .Select(o => new CensusThemeSchemaOptionDto
                            {
                                ValueKey = o.ValueKey,
                                Label = o.Label,
                                SortOrder = o.SortOrder
                            }).ToList(),
                        OptionsLookupKey = f.OptionsLookupKey,
                        AllowMultiple = f.AllowMultiple,
                        IsMandatory = f.IsMandatory,
                        CountsTowardsCompletion = f.CountsTowardsCompletion,
                        VisibilityRuleJson = f.VisibilityRuleJson,
                        SortOrder = f.SortOrder,
                        Disabled = f.IsDisabled
                    }).ToList()
            }).ToList(),
            Capabilities = capabilities.Select(c => new CensusThemeSchemaCapabilityDto
            {
                Title = c.Title,
                Description = c.Description,
                Reference = c.Reference
            }).ToList()
        };
    }

    public async Task<CensusThemeSchemaImportResult> ImportSchemaAsync(
        CensusThemeSchemaPackage package,
        bool disableNotInFile,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
        {
            return new CensusThemeSchemaImportResult
            {
                Ok = false,
                Error = "You do not have permission to import the census theme schema.",
                DisableNotInFile = disableNotInFile
            };
        }

        var validationError = ValidatePackage(package);
        if (validationError != null)
        {
            return new CensusThemeSchemaImportResult
            {
                Ok = false,
                Error = validationError,
                DisableNotInFile = disableNotInFile
            };
        }

        await EnsureSeededAsync(cancellationToken);

        var themesAdded = 0;
        var themesUpdated = 0;
        var themesDisabled = 0;
        var questionsAdded = 0;
        var questionsUpdated = 0;
        var questionsDisabled = 0;
        var capabilitiesAdded = 0;
        var capabilitiesUpdated = 0;
        var optionsAdded = 0;
        var optionsUpdated = 0;

        var now = DateTime.UtcNow;
        var existingCapabilities = await _db.CapabilityLookups.ToListAsync(cancellationToken);
        var capsByRef = existingCapabilities
            .ToDictionary(c => c.Reference, StringComparer.OrdinalIgnoreCase);

        foreach (var capDto in package.Capabilities)
        {
            var reference = capDto.Reference.Trim();
            var title = capDto.Title.Trim();
            var description = NullIfWhiteSpace(capDto.Description);

            if (capsByRef.TryGetValue(reference, out var existing))
            {
                // Preserve local Id so answers that store capability Guids still resolve.
                existing.Title = title;
                existing.Description = description;
                existing.UpdatedUtc = now;
                capabilitiesUpdated++;
            }
            else
            {
                var entity = new CapabilityLookup
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    Description = description,
                    Reference = reference,
                    SortOrder = existingCapabilities.Count + capabilitiesAdded + 1,
                    IsActive = true,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                _db.CapabilityLookups.Add(entity);
                capsByRef[reference] = entity;
                capabilitiesAdded++;
            }
        }

        var existingThemes = await _db.CoreCensusThemes
            .Include(t => t.Fields)
            .ThenInclude(f => f.Options)
            .Where(t => !t.IsServiceOffering)
            .ToListAsync(cancellationToken);

        var themesByKey = existingThemes
            .ToDictionary(t => t.StableKey, StringComparer.OrdinalIgnoreCase);

        // Field stable keys are unique across the catalogue — look up globally for moves/updates.
        var fieldsByKey = existingThemes
            .SelectMany(t => t.Fields)
            .ToDictionary(f => f.StableKey, StringComparer.OrdinalIgnoreCase);

        var packageThemeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var packageFieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var themeDto in package.Themes)
        {
            var themeKey = themeDto.StableKey.Trim();
            if (string.Equals(themeKey, CoreCensusThemeCatalog.ServiceOfferingKey, StringComparison.OrdinalIgnoreCase))
                continue;

            packageThemeKeys.Add(themeKey);
            CoreCensusTheme theme;
            if (themesByKey.TryGetValue(themeKey, out var existingTheme))
            {
                theme = existingTheme;
                theme.Name = themeDto.Name.Trim();
                theme.Guidance = NullIfWhiteSpace(themeDto.Guidance);
                theme.SortOrder = themeDto.SortOrder;
                theme.IsActive = !themeDto.Disabled;
                theme.UpdatedUtc = now;
                themesUpdated++;
            }
            else
            {
                theme = new CoreCensusTheme
                {
                    Id = Guid.NewGuid(),
                    StableKey = themeKey,
                    Name = themeDto.Name.Trim(),
                    Guidance = NullIfWhiteSpace(themeDto.Guidance),
                    SortOrder = themeDto.SortOrder,
                    IsServiceOffering = false,
                    IsActive = !themeDto.Disabled,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                _db.CoreCensusThemes.Add(theme);
                themesByKey[themeKey] = theme;
                themesAdded++;
            }

            foreach (var qDto in themeDto.Questions)
            {
                var fieldKey = qDto.StableKey.Trim();
                packageFieldKeys.Add(fieldKey);

                if (!TryParseFieldType(qDto.FieldType, out var fieldType))
                {
                    return new CensusThemeSchemaImportResult
                    {
                        Ok = false,
                        Error = $"Question \"{fieldKey}\" has an invalid field type \"{qDto.FieldType}\".",
                        DisableNotInFile = disableNotInFile
                    };
                }

                if (fieldsByKey.TryGetValue(fieldKey, out var existingField))
                {
                    existingField.CoreCensusThemeId = theme.Id;
                    existingField.Label = qDto.Label.Trim();
                    existingField.Guidance = NullIfWhiteSpace(qDto.Guidance);
                    existingField.FieldType = fieldType;
                    existingField.OptionsLookupKey = NullIfWhiteSpace(qDto.OptionsLookupKey);
                    existingField.AllowMultiple = qDto.AllowMultiple;
                    existingField.IsMandatory = qDto.IsMandatory;
                    existingField.CountsTowardsCompletion = qDto.CountsTowardsCompletion;
                    existingField.VisibilityRuleJson = NullIfWhiteSpace(qDto.VisibilityRuleJson);
                    existingField.SortOrder = qDto.SortOrder;
                    existingField.IsDisabled = qDto.Disabled;
                    questionsUpdated++;

                    var optCounts = UpsertOptions(existingField, qDto.Options);
                    optionsAdded += optCounts.Added;
                    optionsUpdated += optCounts.Updated;
                }
                else
                {
                    var field = new CoreCensusThemeField
                    {
                        Id = Guid.NewGuid(),
                        CoreCensusThemeId = theme.Id,
                        StableKey = fieldKey,
                        Label = qDto.Label.Trim(),
                        Guidance = NullIfWhiteSpace(qDto.Guidance),
                        FieldType = fieldType,
                        OptionsLookupKey = NullIfWhiteSpace(qDto.OptionsLookupKey),
                        AllowMultiple = qDto.AllowMultiple,
                        IsMandatory = qDto.IsMandatory,
                        CountsTowardsCompletion = qDto.CountsTowardsCompletion,
                        IsReportable = true,
                        VisibilityRuleJson = NullIfWhiteSpace(qDto.VisibilityRuleJson),
                        SortOrder = qDto.SortOrder,
                        IsDisabled = qDto.Disabled
                    };

                    foreach (var opt in qDto.Options
                                 .Where(o => !string.IsNullOrWhiteSpace(o.ValueKey))
                                 .OrderBy(o => o.SortOrder))
                    {
                        field.Options.Add(new CoreCensusThemeFieldOption
                        {
                            Id = Guid.NewGuid(),
                            ValueKey = opt.ValueKey.Trim(),
                            Label = string.IsNullOrWhiteSpace(opt.Label) ? opt.ValueKey.Trim() : opt.Label.Trim(),
                            SortOrder = opt.SortOrder
                        });
                        optionsAdded++;
                    }

                    _db.CoreCensusThemeFields.Add(field);
                    fieldsByKey[fieldKey] = field;
                    questionsAdded++;
                }
            }
        }

        if (disableNotInFile)
        {
            foreach (var theme in existingThemes)
            {
                if (packageThemeKeys.Contains(theme.StableKey))
                    continue;
                if (!theme.IsActive)
                    continue;
                theme.IsActive = false;
                theme.UpdatedUtc = now;
                themesDisabled++;
            }

            foreach (var field in fieldsByKey.Values)
            {
                if (packageFieldKeys.Contains(field.StableKey))
                    continue;
                if (field.IsDisabled)
                    continue;
                field.IsDisabled = true;
                questionsDisabled++;
            }
        }

        WriteAudit("CoreCensusThemeSchema", "catalogue", "Import", actorEmail,
            JsonSerializer.Serialize(new
            {
                disableNotInFile,
                themesAdded,
                themesUpdated,
                themesDisabled,
                questionsAdded,
                questionsUpdated,
                questionsDisabled,
                capabilitiesAdded,
                capabilitiesUpdated
            }));

        await _db.SaveChangesAsync(cancellationToken);

        return new CensusThemeSchemaImportResult
        {
            Ok = true,
            DisableNotInFile = disableNotInFile,
            ThemesAdded = themesAdded,
            ThemesUpdated = themesUpdated,
            ThemesDisabled = themesDisabled,
            QuestionsAdded = questionsAdded,
            QuestionsUpdated = questionsUpdated,
            QuestionsDisabled = questionsDisabled,
            CapabilitiesAdded = capabilitiesAdded,
            CapabilitiesUpdated = capabilitiesUpdated,
            OptionsAdded = optionsAdded,
            OptionsUpdated = optionsUpdated
        };
    }

    private static (int Added, int Updated) UpsertOptions(
        CoreCensusThemeField field,
        IReadOnlyList<CensusThemeSchemaOptionDto> options)
    {
        var added = 0;
        var updated = 0;
        var byKey = field.Options.ToDictionary(o => o.ValueKey, StringComparer.OrdinalIgnoreCase);

        foreach (var opt in options.Where(o => !string.IsNullOrWhiteSpace(o.ValueKey)))
        {
            var valueKey = opt.ValueKey.Trim();
            var label = string.IsNullOrWhiteSpace(opt.Label) ? valueKey : opt.Label.Trim();
            if (byKey.TryGetValue(valueKey, out var existing))
            {
                existing.Label = label;
                existing.SortOrder = opt.SortOrder;
                updated++;
            }
            else
            {
                var entity = new CoreCensusThemeFieldOption
                {
                    Id = Guid.NewGuid(),
                    CoreCensusThemeFieldId = field.Id,
                    ValueKey = valueKey,
                    Label = label,
                    SortOrder = opt.SortOrder
                };
                field.Options.Add(entity);
                byKey[valueKey] = entity;
                added++;
            }
        }

        return (added, updated);
    }

    private static string? ValidatePackage(CensusThemeSchemaPackage? package)
    {
        if (package == null)
            return "The uploaded file is empty or is not valid JSON.";

        if (package.SchemaVersion != CensusThemeSchemaPackage.CurrentSchemaVersion)
        {
            return $"Unsupported schemaVersion {package.SchemaVersion}. " +
                   $"This environment accepts version {CensusThemeSchemaPackage.CurrentSchemaVersion}.";
        }

        if (package.Themes == null)
            return "Invalid schema: \"themes\" is required.";

        if (package.Capabilities == null)
            return "Invalid schema: \"capabilities\" is required.";

        var themeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var ti = 0; ti < package.Themes.Count; ti++)
        {
            var theme = package.Themes[ti];
            if (theme == null)
                return $"Invalid schema: themes[{ti}] is null.";

            var themeKey = theme.StableKey?.Trim() ?? "";
            if (string.IsNullOrEmpty(themeKey))
                return $"Invalid schema: themes[{ti}] is missing a stableKey.";

            if (!themeKeys.Add(themeKey))
                return $"Invalid schema: duplicate theme stableKey \"{themeKey}\".";

            if (string.IsNullOrWhiteSpace(theme.Name))
                return $"Invalid schema: theme \"{themeKey}\" is missing a name.";

            theme.Questions ??= new List<CensusThemeSchemaQuestionDto>();
            for (var qi = 0; qi < theme.Questions.Count; qi++)
            {
                var q = theme.Questions[qi];
                if (q == null)
                    return $"Invalid schema: theme \"{themeKey}\" questions[{qi}] is null.";

                var fieldKey = q.StableKey?.Trim() ?? "";
                if (string.IsNullOrEmpty(fieldKey))
                    return $"Invalid schema: theme \"{themeKey}\" questions[{qi}] is missing a stableKey.";

                if (!fieldKeys.Add(fieldKey))
                    return $"Invalid schema: duplicate question stableKey \"{fieldKey}\".";

                if (string.IsNullOrWhiteSpace(q.Label))
                    return $"Invalid schema: question \"{fieldKey}\" is missing a label.";

                if (!TryParseFieldType(q.FieldType, out _))
                    return $"Invalid schema: question \"{fieldKey}\" has an invalid fieldType \"{q.FieldType}\".";

                q.Options ??= new List<CensusThemeSchemaOptionDto>();
                var optionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var opt in q.Options)
                {
                    if (opt == null)
                        return $"Invalid schema: question \"{fieldKey}\" has a null option.";
                    var vk = opt.ValueKey?.Trim() ?? "";
                    if (string.IsNullOrEmpty(vk))
                        return $"Invalid schema: question \"{fieldKey}\" has an option without valueKey.";
                    if (!optionKeys.Add(vk))
                        return $"Invalid schema: question \"{fieldKey}\" has duplicate option valueKey \"{vk}\".";
                }
            }
        }

        var capRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var ci = 0; ci < package.Capabilities.Count; ci++)
        {
            var cap = package.Capabilities[ci];
            if (cap == null)
                return $"Invalid schema: capabilities[{ci}] is null.";

            var reference = cap.Reference?.Trim() ?? "";
            if (string.IsNullOrEmpty(reference))
                return $"Invalid schema: capabilities[{ci}] is missing a reference.";

            if (string.IsNullOrWhiteSpace(cap.Title))
                return $"Invalid schema: capability \"{reference}\" is missing a title.";

            if (!capRefs.Add(reference))
                return $"Invalid schema: duplicate capability reference \"{reference}\".";
        }

        return null;
    }

    private static bool TryParseFieldType(string? raw, out ServiceDataModelFieldType fieldType)
    {
        fieldType = ServiceDataModelFieldType.Text;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.Trim();
        if (Enum.TryParse(trimmed, ignoreCase: true, out fieldType) &&
            Enum.IsDefined(typeof(ServiceDataModelFieldType), fieldType))
            return true;

        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) &&
            Enum.IsDefined(typeof(ServiceDataModelFieldType), numeric))
        {
            fieldType = (ServiceDataModelFieldType)numeric;
            return true;
        }

        return false;
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
