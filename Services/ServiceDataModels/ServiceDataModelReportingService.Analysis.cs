using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

public sealed partial class ServiceDataModelReportingService
{
    public async Task<ServiceCensusAnalysisReportViewModel> BuildAnalysisReportAsync(
        string email,
        ServiceCensusAnalysisReportFilters? filters = null)
    {
        filters ??= new ServiceCensusAnalysisReportFilters();
        if (filters.Page < 1)
            filters.Page = 1;
        if (filters.PageSize < 1)
            filters.PageSize = ServiceCensusAnalysisAggregation.DefaultPageSize;

        var calculatedUtc = DateTime.UtcNow;
        var sharedThemes = await _coreThemes.GetSharedStructureAsync();
        var allThemeDefs = sharedThemes
            .OrderBy(t => t.SortOrder)
            .Select(ToThemeDefinition)
            .ToList();

        var filterOptions = await LoadAnalysisFilterOptionsAsync(allThemeDefs);
        var empty = EmptyAnalysis(filters, calculatedUtc, filterOptions);

        if (allThemeDefs.Count == 0)
            return empty;

        var fieldByKey = allThemeDefs
            .SelectMany(t => t.Fields.Select(f => (Theme: t, Field: f)))
            .ToDictionary(
                x => x.Field.StableKey,
                x => x,
                StringComparer.OrdinalIgnoreCase);

        var modelsQuery = _db.ServiceDataModels.AsNoTracking()
            .Where(m =>
                m.IsReportable &&
                m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft &&
                m.StableKey == ServiceCensusDefaults.StableKey);

        if (filters.ModelId.HasValue)
            modelsQuery = modelsQuery.Where(m => m.Id == filters.ModelId.Value);

        var models = await modelsQuery.ToListAsync();
        if (models.Count == 0)
        {
            // Fall back to any reportable models if the standing census key is not seeded yet.
            models = await _db.ServiceDataModels.AsNoTracking()
                .Where(m => m.IsReportable && m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft)
                .ToListAsync();
            if (filters.ModelId.HasValue)
                models = models.Where(m => m.Id == filters.ModelId.Value).ToList();
        }

        if (models.Count == 0)
            return empty;

        var modelIds = models.Select(m => m.Id).ToList();
        var versions = await _db.ServiceDataModelVersions.AsNoTracking()
            .Where(v =>
                modelIds.Contains(v.ServiceDataModelId) &&
                (v.Status == ServiceDataModelLifecycleStatus.Published || v.PublishedUtc != null))
            .ToListAsync();

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
            .ThenInclude(p => p.Directorates)
            .ThenInclude(d => d.FipsDirectorate)
            .Include(a => a.Product)
            .ThenInclude(p => p.BusinessAreas)
            .ThenInclude(b => b.FipsBusinessArea)
            .Include(a => a.Product)
            .ThenInclude(p => p.Types)
            .ThenInclude(t => t.FipsType)
            .Include(a => a.Product)
            .ThenInclude(p => p.Contacts)
            .Include(a => a.Product)
            .ThenInclude(p => p.Phase)
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

        var assignments = await assignmentsQuery.ToListAsync();

        var isOps = !string.IsNullOrWhiteSpace(email) &&
                    await _permissions.IsOperationConsoleUserAsync(email.Trim());
        var productIds = assignments.Select(a => a.CMDBProductId).Distinct().ToList();
        var accessibleIds = isOps
            ? productIds
            : (await _access.FilterAccessibleProductIdsAsync(_db, email, productIds)).ToList();
        var accessibleSet = accessibleIds.ToHashSet();

        var scopedAssignments = assignments
            .Where(a => accessibleSet.Contains(a.CMDBProductId) && a.Product != null)
            .Where(a => a.Status is not (ServiceDataModelAssignmentStatus.Withdrawn
                or ServiceDataModelAssignmentStatus.NotApplicable))
            .ToList();

        var facts = new List<ServiceCensusAnswerFact>();
        foreach (var assignment in scopedAssignments)
        {
            var submission = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
            var answers = submission?.Answers ?? Array.Empty<ServiceDataModelAnswer>();
            var product = assignment.Product!;
            var title = string.IsNullOrWhiteSpace(product.Title) ? "Untitled product" : product.Title;
            var businessAreas = SafeNames(product.BusinessAreas?.Select(b => b.FipsBusinessArea?.Name));
            var directorates = SafeNames(product.Directorates?.Select(d => d.FipsDirectorate?.Name));
            var phaseName = product.Phase?.Name;

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
                    NumberValue = parsed.NumberValue,
                    PhaseName = phaseName,
                    BusinessAreaNames = businessAreas,
                    DirectorateNames = directorates
                });
            }
        }

        // Ensure every scoped product × field combination can be represented when drilling product detail.
        // Facts already cover answered fields; unanswered fields are synthesised only for product detail.

        var optionLabels = await ResolveOptionLabelsAsync(allThemeDefs, facts);
        var answeredServiceCount = facts.Where(f => f.HasAnswer).Select(f => f.ProductId).Distinct().Count();
        var scopedServiceCount = scopedAssignments.Select(a => a.CMDBProductId).Distinct().Count();

        var vm = new ServiceCensusAnalysisReportViewModel
        {
            CalculatedUtc = calculatedUtc,
            Filters = filters,
            AnsweredServiceCount = answeredServiceCount,
            ScopedServiceCount = scopedServiceCount,
            PopulationLabel =
                $"{answeredServiceCount} answered service{(answeredServiceCount == 1 ? "" : "s")} in the filter" +
                $" (of {scopedServiceCount} in scope)",
            DirectorateOptions = filterOptions.Directorates,
            BusinessAreaOptions = filterOptions.BusinessAreas,
            PhaseOptions = filterOptions.Phases,
            TypeOptions = filterOptions.Types,
            ThemeOptions = filterOptions.Themes
        };

        // Product detail takes priority when productId is set.
        if (filters.ProductId.HasValue)
        {
            var productId = filters.ProductId.Value;
            if (!accessibleSet.Contains(productId))
            {
                vm.ViewMode = ServiceCensusAnalysisViewMode.Themes;
                vm.Themes = ServiceCensusAnalysisAggregation.BuildThemeSummaries(allThemeDefs, facts);
                return vm;
            }

            var assignment = scopedAssignments.FirstOrDefault(a => a.CMDBProductId == productId);
            if (assignment?.Product == null)
            {
                vm.ViewMode = ServiceCensusAnalysisViewMode.Themes;
                vm.Themes = ServiceCensusAnalysisAggregation.BuildThemeSummaries(allThemeDefs, facts);
                return vm;
            }

            var identity = new ServiceCensusAnswerFact
            {
                AssignmentId = assignment.Id,
                ProductId = productId,
                ProductTitle = string.IsNullOrWhiteSpace(assignment.Product.Title)
                    ? "Untitled product"
                    : assignment.Product.Title,
                PhaseName = assignment.Product.Phase?.Name,
                BusinessAreaNames = SafeNames(assignment.Product.BusinessAreas?.Select(b => b.FipsBusinessArea?.Name)),
                DirectorateNames = SafeNames(assignment.Product.Directorates?.Select(d => d.FipsDirectorate?.Name))
            };
            var productFacts = facts.Where(f => f.ProductId == productId).ToList();

            vm.ViewMode = ServiceCensusAnalysisViewMode.ProductDetail;
            vm.ProductDetail = ServiceCensusAnalysisAggregation.BuildProductDetail(
                identity, allThemeDefs, productFacts, optionLabels);
            return vm;
        }

        if (!string.IsNullOrWhiteSpace(filters.FieldStableKey) &&
            fieldByKey.TryGetValue(filters.FieldStableKey.Trim(), out var selectedField))
        {
            var fieldFacts = facts
                .Where(f => string.Equals(f.FieldStableKey, selectedField.Field.StableKey, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(filters.OptionKey))
            {
                var optionKey = filters.OptionKey.Trim();
                var optionLabel = optionLabels.TryGetValue(optionKey, out var lbl) && !string.IsNullOrWhiteSpace(lbl)
                    ? lbl
                    : optionKey;
                // Text values use the raw text as the key; prefer the value itself as the label.
                if (selectedField.Field.FieldType is ServiceDataModelFieldType.Text
                    or ServiceDataModelFieldType.MultilineText
                    or ServiceDataModelFieldType.Url
                    or ServiceDataModelFieldType.Date)
                {
                    optionLabel = optionKey;
                }

                vm.ViewMode = ServiceCensusAnalysisViewMode.OptionProducts;
                vm.OptionDrill = ServiceCensusAnalysisAggregation.BuildOptionDrill(
                    selectedField.Theme,
                    selectedField.Field,
                    optionKey,
                    optionLabel,
                    fieldFacts,
                    filters.Page,
                    filters.PageSize);
                return vm;
            }

            vm.ViewMode = ServiceCensusAnalysisViewMode.Metric;
            vm.SelectedMetric = ServiceCensusAnalysisAggregation.BuildMetricDetail(
                selectedField.Theme,
                selectedField.Field,
                fieldFacts,
                optionLabels,
                filters.Page,
                filters.PageSize);
            return vm;
        }

        if (!string.IsNullOrWhiteSpace(filters.ThemeStableKey))
        {
            var theme = allThemeDefs.FirstOrDefault(t =>
                string.Equals(t.StableKey, filters.ThemeStableKey, StringComparison.OrdinalIgnoreCase));
            if (theme != null)
            {
                var themeFacts = facts
                    .Where(f => string.Equals(f.ThemeStableKey, theme.StableKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                vm.ViewMode = ServiceCensusAnalysisViewMode.Theme;
                vm.SelectedTheme = ServiceCensusAnalysisAggregation.BuildThemeDetail(theme, themeFacts);
                return vm;
            }
        }

        vm.ViewMode = ServiceCensusAnalysisViewMode.Themes;
        vm.Themes = ServiceCensusAnalysisAggregation.BuildThemeSummaries(allThemeDefs, facts);
        return vm;
    }

    private static ServiceCensusThemeDefinition ToThemeDefinition(CoreCensusTheme t) =>
        new()
        {
            StableKey = t.StableKey,
            Name = t.Name,
            SortOrder = t.SortOrder,
            Fields = t.Fields
                .Where(f => !f.IsDisabled && f.IsReportable)
                .OrderBy(f => f.SortOrder)
                .Select(f => new ServiceCensusFieldDefinition
                {
                    StableKey = f.StableKey,
                    Label = f.Label,
                    FieldType = f.FieldType,
                    AllowMultiple = f.AllowMultiple,
                    SortOrder = f.SortOrder,
                    OptionsLookupKey = f.OptionsLookupKey,
                    OptionKeys = f.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => o.ValueKey)
                        .ToList()
                })
                .ToList()
        };

    private static ServiceCensusAnalysisReportViewModel EmptyAnalysis(
        ServiceCensusAnalysisReportFilters filters,
        DateTime calculatedUtc,
        (
            IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Directorates,
            IReadOnlyList<ServiceDataModelCompletionReportFilterOption> BusinessAreas,
            IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Phases,
            IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Types,
            IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Themes
        ) options) =>
        new()
        {
            CalculatedUtc = calculatedUtc,
            Filters = filters,
            ViewMode = ServiceCensusAnalysisViewMode.Themes,
            PopulationLabel = "0 answered services in the filter",
            DirectorateOptions = options.Directorates,
            BusinessAreaOptions = options.BusinessAreas,
            PhaseOptions = options.Phases,
            TypeOptions = options.Types,
            ThemeOptions = options.Themes
        };

    private async Task<(
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Directorates,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> BusinessAreas,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Phases,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Types,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Themes)> LoadAnalysisFilterOptionsAsync(
        IReadOnlyList<ServiceCensusThemeDefinition> themeDefs)
    {
        var baseOptions = await LoadFilterOptionsAsync(null);
        var themes = themeDefs
            .OrderBy(t => t.SortOrder)
            .Select(t => new ServiceDataModelCompletionReportFilterOption
            {
                Value = t.StableKey,
                Text = t.Name
            })
            .ToList();
        return (baseOptions.Directorates, baseOptions.BusinessAreas, baseOptions.Phases, baseOptions.Types, themes);
    }

    private async Task<Dictionary<string, string>> ResolveOptionLabelsAsync(
        IReadOnlyList<ServiceCensusThemeDefinition> themes,
        IReadOnlyList<ServiceCensusAnswerFact> facts)
    {
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Yes"] = "Yes",
            ["No"] = "No"
        };

        var lookupKeys = themes
            .SelectMany(t => t.Fields)
            .Select(f => f.OptionsLookupKey)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var lookupKey in lookupKeys)
        {
            var lookup = await CensusAdminLookupOptions.ResolveAsync(_db, lookupKey);
            foreach (var opt in lookup)
            {
                if (!string.IsNullOrWhiteSpace(opt.ValueKey))
                    labels[opt.ValueKey] = opt.Label;
            }
        }

        var fieldKeys = themes.SelectMany(t => t.Fields).Select(f => f.StableKey).Distinct().ToList();
        var coreOptions = await _db.CoreCensusThemeFieldOptions.AsNoTracking()
            .Where(o => fieldKeys.Contains(o.Field.StableKey))
            .Select(o => new { o.ValueKey, o.Label })
            .ToListAsync();
        foreach (var opt in coreOptions)
        {
            if (!string.IsNullOrWhiteSpace(opt.ValueKey))
                labels[opt.ValueKey] = opt.Label;
        }

        // Linked services / service lines referenced in answers.
        var linkedIds = facts
            .SelectMany(f => f.LinkedItemKeys)
            .Where(k => Guid.TryParse(k, out _))
            .Select(Guid.Parse)
            .Distinct()
            .ToList();

        if (linkedIds.Count > 0)
        {
            var products = await _db.CMDBProducts.AsNoTracking()
                .Where(p => linkedIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Title })
                .ToListAsync();
            foreach (var p in products)
                labels[p.Id.ToString()] = string.IsNullOrWhiteSpace(p.Title) ? p.Id.ToString() : p.Title!;

            var lines = await _db.ServiceLines.AsNoTracking()
                .Where(s => linkedIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToListAsync();
            foreach (var s in lines)
                labels[s.Id.ToString()] = string.IsNullOrWhiteSpace(s.Name) ? s.Id.ToString() : s.Name;
        }

        return labels;
    }
}
