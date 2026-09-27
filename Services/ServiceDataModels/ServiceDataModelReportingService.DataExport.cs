using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

public sealed partial class ServiceDataModelReportingService
{
    /// <summary>
    /// Builds the Service Census Excel data population: shared themes + all in-scope
    /// assignments (same access and overview filters). Does not require a period.
    /// </summary>
    public async Task<ServiceCensusDataExportModel> BuildDataExportAsync(
        string email,
        ServiceDataModelCompletionReportFilters? filters = null)
    {
        filters ??= new ServiceDataModelCompletionReportFilters();

        var sharedThemes = await _coreThemes.GetSharedStructureAsync();
        var themeDefs = sharedThemes
            .OrderBy(t => t.SortOrder)
            .Select(ToThemeDefinition)
            .ToList();

        var empty = new ServiceCensusDataExportModel { Themes = themeDefs };

        if (themeDefs.Count == 0)
            return empty;

        var fieldByKey = themeDefs
            .SelectMany(t => t.Fields.Select(f => (Theme: t, Field: f)))
            .ToDictionary(
                x => x.Field.StableKey,
                x => x,
                StringComparer.OrdinalIgnoreCase);

        var modelsQuery = _db.ServiceDataModels.AsNoTracking()
            .Where(m =>
                m.IsReportable &&
                m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft);

        if (filters.ModelId.HasValue)
            modelsQuery = modelsQuery.Where(m => m.Id == filters.ModelId.Value);
        else
            modelsQuery = modelsQuery.Where(m => m.StableKey == ServiceCensusDefaults.StableKey);

        var models = await modelsQuery.ToListAsync();
        if (models.Count == 0 && !filters.ModelId.HasValue)
        {
            models = await _db.ServiceDataModels.AsNoTracking()
                .Where(m => m.IsReportable && m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft)
                .ToListAsync();
        }

        if (models.Count == 0)
            return empty;

        var modelIds = models.Select(m => m.Id).ToList();
        var versionsQuery = _db.ServiceDataModelVersions.AsNoTracking()
            .Where(v =>
                modelIds.Contains(v.ServiceDataModelId) &&
                (v.Status == ServiceDataModelLifecycleStatus.Published || v.PublishedUtc != null));

        if (filters.VersionId.HasValue)
            versionsQuery = versionsQuery.Where(v => v.Id == filters.VersionId.Value);
        if (!string.IsNullOrWhiteSpace(filters.PeriodLabel))
        {
            var period = filters.PeriodLabel.Trim();
            versionsQuery = versionsQuery.Where(v => v.PeriodLabel == period);
        }

        var versions = await versionsQuery.ToListAsync();
        if (versions.Count == 0 && filters.VersionId == null && string.IsNullOrWhiteSpace(filters.PeriodLabel))
        {
            versions = await _db.ServiceDataModelVersions.AsNoTracking()
                .Where(v =>
                    modelIds.Contains(v.ServiceDataModelId) &&
                    (v.Status == ServiceDataModelLifecycleStatus.Published || v.PublishedUtc != null))
                .ToListAsync();
        }

        versions = versions
            .GroupBy(v => v.ServiceDataModelId)
            .Select(g => g.OrderByDescending(v => v.VersionNumber).First())
            .ToList();

        if (versions.Count == 0)
            return empty;

        var versionIds = versions.Select(v => v.Id).ToList();
        var assignmentsQuery = _db.ServiceDataModelAssignments
            .AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.Submissions.Where(s => s.IsCurrent))
            .ThenInclude(s => s.Answers)
            .Where(a => versionIds.Contains(a.ServiceDataModelVersionId));

        if (filters.ProductStatus.HasValue)
            assignmentsQuery = assignmentsQuery.Where(a => a.Product.Status == filters.ProductStatus.Value);
        if (filters.PhaseId.HasValue)
            assignmentsQuery = assignmentsQuery.Where(a => a.Product.PhaseId == filters.PhaseId.Value);
        if (filters.TypeId.HasValue)
        {
            var typeId = filters.TypeId.Value;
            assignmentsQuery = assignmentsQuery.Where(a =>
                a.Product.Types.Any(t => t.FipsTypeId == typeId));
        }
        if (filters.DirectorateId.HasValue)
        {
            var dirId = filters.DirectorateId.Value;
            assignmentsQuery = assignmentsQuery.Where(a =>
                a.Product.Directorates.Any(d => d.FipsDirectorateId == dirId));
        }
        if (filters.BusinessAreaId.HasValue)
        {
            var baId = filters.BusinessAreaId.Value;
            assignmentsQuery = assignmentsQuery.Where(a =>
                a.Product.BusinessAreas.Any(b => b.FipsBusinessAreaId == baId));
        }
        if (!string.IsNullOrWhiteSpace(filters.OwnerEmail))
        {
            var owner = filters.OwnerEmail.Trim().ToLowerInvariant();
            assignmentsQuery = assignmentsQuery.Where(a =>
                a.Product.Contacts.Any(c =>
                    c.CanManage &&
                    c.UserEmail != null &&
                    c.UserEmail.Trim().ToLower() == owner));
        }
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim().ToLowerInvariant();
            assignmentsQuery = assignmentsQuery.Where(a =>
                a.Product.Title.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(filters.PeriodLabel))
        {
            var period = filters.PeriodLabel.Trim();
            assignmentsQuery = assignmentsQuery.Where(a => a.PeriodLabel == period);
        }

        var assignments = await assignmentsQuery.ToListAsync();

        var isOps = !string.IsNullOrWhiteSpace(email) &&
                    await _permissions.IsOperationConsoleUserAsync(email.Trim());
        var productIds = assignments.Select(a => a.CMDBProductId).Distinct().ToList();
        var accessibleIds = isOps
            ? productIds
            : (await _access.FilterAccessibleProductIdsAsync(_db, email, productIds)).ToList();
        var accessibleSet = accessibleIds.ToHashSet();

        // All census records in scope (including blank themes); exclude withdrawn / not applicable.
        var scoped = assignments
            .Where(a => accessibleSet.Contains(a.CMDBProductId) && a.Product != null)
            .Where(a => a.Status is not (ServiceDataModelAssignmentStatus.Withdrawn
                or ServiceDataModelAssignmentStatus.NotApplicable))
            .GroupBy(a => a.CMDBProductId)
            .Select(g => g.OrderByDescending(a => a.UpdatedUtc).First())
            .OrderBy(a => a.Product!.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var facts = new List<ServiceCensusAnswerFact>();
        foreach (var assignment in scoped)
        {
            var submission = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
            var answers = submission?.Answers ?? Array.Empty<ServiceDataModelAnswer>();
            var product = assignment.Product!;
            var title = string.IsNullOrWhiteSpace(product.Title) ? "Untitled product" : product.Title;

            foreach (var answer in answers)
            {
                var stableKey = SharedCensusAnswerKeys.ResolveStableKey(answer);
                if (string.IsNullOrWhiteSpace(stableKey) || !fieldByKey.TryGetValue(stableKey, out var mapped))
                    continue;

                var parsed = ServiceCensusAnalysisAggregation.ParseAnswer(
                    mapped.Field.FieldType,
                    mapped.Field.AllowMultiple,
                    answer.ValueJson);

                facts.Add(new ServiceCensusAnswerFact
                {
                    AssignmentId = assignment.Id,
                    ProductId = assignment.CMDBProductId,
                    ProductTitle = title,
                    ThemeStableKey = mapped.Theme.StableKey,
                    ThemeName = mapped.Theme.Name,
                    FieldStableKey = mapped.Field.StableKey,
                    FieldLabel = mapped.Field.Label,
                    FieldType = mapped.Field.FieldType,
                    AllowMultiple = mapped.Field.AllowMultiple,
                    HasAnswer = parsed.HasAnswer,
                    ChoiceKeys = parsed.ChoiceKeys,
                    TextValues = parsed.TextValues,
                    LinkedItemKeys = parsed.LinkedItemKeys,
                    NumberValue = parsed.NumberValue
                });
            }
        }

        var optionLabels = await ResolveOptionLabelsAsync(themeDefs, facts);
        // Prefer plain names in cells (lookup trees may indent for UI).
        optionLabels = optionLabels.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.TrimStart(),
            StringComparer.OrdinalIgnoreCase);

        var answersByProduct = facts
            .GroupBy(f => f.ProductId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var fact in g)
                    {
                        map[fact.FieldStableKey] =
                            ServiceCensusAnalysisAggregation.FormatAnswerForExport(fact, optionLabels);
                    }

                    return (IReadOnlyDictionary<string, string>)map;
                });

        var services = scoped.Select(a =>
        {
            var product = a.Product!;
            answersByProduct.TryGetValue(a.CMDBProductId, out var answers);
            return new ServiceCensusDataExportServiceRow
            {
                ProductId = a.CMDBProductId,
                RegisterId = product.UniqueID,
                ServiceName = string.IsNullOrWhiteSpace(product.Title) ? "Untitled product" : product.Title,
                AnswersByFieldStableKey = answers ??
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }).ToList();

        return new ServiceCensusDataExportModel
        {
            Themes = themeDefs,
            Services = services
        };
    }
}
