using Compass.Data;
using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

public sealed partial class ServiceDataModelReportingService : IServiceDataModelReportingService
{
    private readonly CompassDbContext _db;
    private readonly IServiceDataModelAccessService _access;
    private readonly IPermissionService _permissions;
    private readonly ICoreCensusThemeService _coreThemes;

    public ServiceDataModelReportingService(
        CompassDbContext db,
        IServiceDataModelAccessService access,
        IPermissionService permissions,
        ICoreCensusThemeService coreThemes)
    {
        _db = db;
        _access = access;
        _permissions = permissions;
        _coreThemes = coreThemes;
    }

    public async Task<ServiceDataModelCompletionReportViewModel> BuildCompletionReportAsync(
        string email,
        ServiceDataModelCompletionReportFilters? filters = null)
    {
        filters ??= new ServiceDataModelCompletionReportFilters();
        if (filters.Page < 1)
            filters.Page = 1;
        if (filters.PageSize < 1)
            filters.PageSize = ServiceDataModelCompletionReportGrouping.DefaultPageSize;

        var calculatedUtc = DateTime.UtcNow;
        var empty = await BuildEmptyAsync(filters, calculatedUtc);

        var modelsQuery = _db.ServiceDataModels.AsNoTracking()
            .Where(m => m.IsReportable && m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft);

        if (filters.ModelId.HasValue)
            modelsQuery = modelsQuery.Where(m => m.Id == filters.ModelId.Value);

        var models = await modelsQuery.ToListAsync();
        if (models.Count == 0)
            return empty;

        var modelIds = models.Select(m => m.Id).ToList();
        var versionsQuery = _db.ServiceDataModelVersions.AsNoTracking()
            .Where(v =>
                modelIds.Contains(v.ServiceDataModelId) &&
                v.Status == ServiceDataModelLifecycleStatus.Published);

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
            // Fall back to latest published per model (including retired published history if none current).
            versions = await _db.ServiceDataModelVersions.AsNoTracking()
                .Where(v =>
                    modelIds.Contains(v.ServiceDataModelId) &&
                    (v.Status == ServiceDataModelLifecycleStatus.Published ||
                     v.PublishedUtc != null))
                .ToListAsync();

            versions = versions
                .GroupBy(v => v.ServiceDataModelId)
                .Select(g => g.OrderByDescending(v => v.VersionNumber).First())
                .ToList();
        }

        if (versions.Count == 0)
        {
            empty.ModelId = filters.ModelId;
            empty.ModelName = models.Count == 1 ? models[0].Name : null;
            return empty;
        }

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
            .Include(a => a.Version)
            .ThenInclude(v => v.Model)
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
                a.Product.Title.ToLower().Contains(term) ||
                a.Version.Model.Name.ToLower().Contains(term));
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

        var facts = assignments
            .Where(a => accessibleSet.Contains(a.CMDBProductId) && a.Product != null && a.Version?.Model != null)
            .Select(a => ToFact(a, calculatedUtc))
            .OrderBy(f => f.ProductTitle, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.ModelName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var summary = ServiceDataModelCompletionReportGrouping.Summarise(facts, calculatedUtc);
        var groupBy = filters.GroupBy;
        var groupByLabel = ServiceDataModelCompletionReportGrouping.GroupByDisplayName(groupBy);

        var breakdown = ServiceDataModelCompletionReportGrouping.BuildBreakdown(facts, groupBy, calculatedUtc);
        var (pageFacts, totalCount, pageNumber, pageSize, totalPages) =
            ServiceDataModelCompletionReportGrouping.Paginate(facts, filters.Page, filters.PageSize);

        var drilldown = groupBy == ServiceDataModelCompletionReportGroupBy.Product
            ? pageFacts.Select(ToDrilldown).ToList()
            : new List<ServiceDataModelCompletionDrilldownRowViewModel>();

        var exportRows = facts.Select(ToExport).ToList();
        var breakdownExport = breakdown.Select(b => new ServiceDataModelCompletionBreakdownExportRowViewModel
        {
            CutDimension = groupByLabel,
            GroupLabel = b.GroupLabel,
            AssignedCount = b.AssignedCount,
            EligibleCount = b.EligibleCount,
            NotStartedCount = b.NotStartedCount,
            InProgressCount = b.InProgressCount,
            SubmittedCount = b.SubmittedCount,
            ReviewedCount = b.ReviewedCount,
            ChangesRequestedCount = b.ChangesRequestedCount,
            OverdueCount = b.OverdueCount,
            NotApplicableCount = b.NotApplicableCount,
            WithdrawnCount = b.WithdrawnCount,
            PortfolioCompletionPercent = b.PortfolioCompletionPercent,
            AverageFieldCompletionPercent = b.AverageFieldCompletionPercent
        }).ToList();

        filters.Page = pageNumber;
        filters.PageSize = pageSize;

        var primaryModel = models.Count == 1 ? models[0] : null;
        var primaryVersion = versions.Count == 1 ? versions[0] : null;
        var filterOptions = await LoadFilterOptionsAsync(modelIds);

        return new ServiceDataModelCompletionReportViewModel
        {
            CalculatedUtc = calculatedUtc,
            ModelId = primaryModel?.Id ?? filters.ModelId,
            ModelName = primaryModel?.Name,
            VersionId = primaryVersion?.Id ?? filters.VersionId,
            VersionNumber = primaryVersion?.VersionNumber,
            PeriodLabel = primaryVersion?.PeriodLabel ?? filters.PeriodLabel,
            EligibleCount = summary.EligibleCount,
            AssignedCount = summary.AssignedCount,
            NotStartedCount = summary.NotStartedCount,
            InProgressCount = summary.InProgressCount,
            SubmittedCount = summary.SubmittedCount,
            ReviewedCount = summary.ReviewedCount,
            OverdueCount = summary.OverdueCount,
            NotApplicableCount = summary.NotApplicableCount,
            WithdrawnCount = summary.WithdrawnCount,
            ChangesRequestedCount = summary.ChangesRequestedCount,
            PortfolioCompletionPercent = summary.PortfolioCompletionPercent,
            AverageFieldCompletionPercent = summary.AverageFieldCompletionPercent,
            Filters = filters,
            GroupByDisplayName = groupByLabel,
            Breakdown = breakdown,
            Drilldown = drilldown,
            DrilldownTotalCount = totalCount,
            DrilldownPage = pageNumber,
            DrilldownPageSize = pageSize,
            DrilldownTotalPages = totalPages,
            ExportRows = exportRows,
            BreakdownExportRows = breakdownExport,
            DirectorateOptions = filterOptions.Directorates,
            BusinessAreaOptions = filterOptions.BusinessAreas,
            PhaseOptions = filterOptions.Phases,
            TypeOptions = filterOptions.Types,
            VersionOptions = filterOptions.Versions,
            PeriodOptions = filterOptions.Periods
        };
    }

    private async Task<ServiceDataModelCompletionReportViewModel> BuildEmptyAsync(
        ServiceDataModelCompletionReportFilters filters,
        DateTime calculatedUtc)
    {
        var options = await LoadFilterOptionsAsync(
            filters.ModelId.HasValue ? new List<Guid> { filters.ModelId.Value } : null);

        return new ServiceDataModelCompletionReportViewModel
        {
            CalculatedUtc = calculatedUtc,
            Filters = filters,
            GroupByDisplayName = ServiceDataModelCompletionReportGrouping.GroupByDisplayName(filters.GroupBy),
            DirectorateOptions = options.Directorates,
            BusinessAreaOptions = options.BusinessAreas,
            PhaseOptions = options.Phases,
            TypeOptions = options.Types,
            VersionOptions = options.Versions,
            PeriodOptions = options.Periods
        };
    }

    private async Task<(
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Directorates,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> BusinessAreas,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Phases,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Types,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Versions,
        IReadOnlyList<ServiceDataModelCompletionReportFilterOption> Periods)> LoadFilterOptionsAsync(
        IReadOnlyList<Guid>? modelIds)
    {
        var directorates = (await _db.FipsDirectorates.AsNoTracking()
            .Where(d => d.Active)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync())
            .Select(d => new ServiceDataModelCompletionReportFilterOption
            {
                Value = d.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(d.Name) ? $"Directorate {d.Id}" : d.Name
            })
            .ToList();

        var businessAreas = (await _db.FipsBusinessAreas.AsNoTracking()
            .Where(b => b.Active)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync())
            .Select(b => new ServiceDataModelCompletionReportFilterOption
            {
                Value = b.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(b.Name) ? $"Business area {b.Id}" : b.Name
            })
            .ToList();

        var phases = (await _db.PhaseLookups.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync())
            .Select(p => new ServiceDataModelCompletionReportFilterOption
            {
                Value = p.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(p.Name) ? $"Phase {p.Id}" : p.Name
            })
            .ToList();

        var types = (await _db.FipsTypes.AsNoTracking()
            .Where(t => t.Active)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync())
            .Select(t => new ServiceDataModelCompletionReportFilterOption
            {
                Value = t.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(t.Name) ? $"Type {t.Id}" : t.Name
            })
            .ToList();

        var versionsQuery = _db.ServiceDataModelVersions.AsNoTracking()
            .Where(v =>
                v.Status == ServiceDataModelLifecycleStatus.Published ||
                v.PublishedUtc != null);

        if (modelIds is { Count: > 0 })
            versionsQuery = versionsQuery.Where(v => modelIds.Contains(v.ServiceDataModelId));
        else
        {
            versionsQuery = versionsQuery.Where(v =>
                v.Model.IsReportable &&
                v.Model.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft);
        }

        var versionRows = await versionsQuery
            .OrderBy(v => v.Model.Name)
            .ThenByDescending(v => v.VersionNumber)
            .Select(v => new
            {
                v.Id,
                v.VersionNumber,
                ModelName = v.Model.Name,
                v.PeriodLabel
            })
            .ToListAsync();

        var versions = versionRows
            .Select(v => new ServiceDataModelCompletionReportFilterOption
            {
                Value = v.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(v.PeriodLabel)
                    ? $"{v.ModelName} v{v.VersionNumber}"
                    : $"{v.ModelName} v{v.VersionNumber} ({v.PeriodLabel})"
            })
            .ToList();

        var periods = versionRows
            .Select(v => v.PeriodLabel)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(p => p)
            .Select(p => new ServiceDataModelCompletionReportFilterOption { Value = p!, Text = p! })
            .ToList();

        return (directorates, businessAreas, phases, types, versions, periods);
    }

    private static ServiceDataModelCompletionAssignmentFact ToFact(
        ServiceDataModelAssignment a,
        DateTime calculatedUtc)
    {
        var product = a.Product;
        var version = a.Version;
        var model = version?.Model;

        var owners = (product?.Contacts ?? Enumerable.Empty<CMDBProductContact>())
            .Where(c => c.CanManage && !string.IsNullOrWhiteSpace(c.UserEmail))
            .Select(c => c.UserEmail!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(e => e, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var title = product?.Title;
        if (string.IsNullOrWhiteSpace(title))
            title = "Untitled product";

        return new ServiceDataModelCompletionAssignmentFact
        {
            AssignmentId = a.Id,
            ProductId = a.CMDBProductId,
            ProductTitle = title,
            ModelName = model?.Name ?? string.Empty,
            ModelStableKey = model?.StableKey ?? string.Empty,
            VersionNumber = version?.VersionNumber,
            PeriodLabel = a.PeriodLabel,
            Status = a.Status,
            FieldCompletionPercent = a.FieldCompletionPercent,
            MandatoryCompletionPercent = a.MandatoryCompletionPercent,
            DueUtc = a.DueUtc,
            IsOverdue = ServiceDataModelCompletionCalculator.IsOverdue(a.Status, a.DueUtc, calculatedUtc),
            UpdatedUtc = a.UpdatedUtc,
            LastAnsweredUtc = a.LastAnsweredUtc,
            SubmittedUtc = a.SubmittedUtc,
            ReviewedUtc = a.ReviewedUtc,
            PhaseName = product?.Phase?.Name,
            BusinessAreaNames = SafeNames(
                product?.BusinessAreas?.Select(b => b.FipsBusinessArea?.Name)),
            DirectorateNames = SafeNames(
                product?.Directorates?.Select(d => d.FipsDirectorate?.Name)),
            ServiceTypeNames = SafeNames(
                product?.Types?.Select(t => t.FipsType?.Name)),
            OwnerEmails = owners
        };
    }

    private static IReadOnlyList<string> SafeNames(IEnumerable<string?>? names)
    {
        if (names is null)
            return Array.Empty<string>();

        return names
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ServiceDataModelCompletionDrilldownRowViewModel ToDrilldown(
        ServiceDataModelCompletionAssignmentFact f) =>
        new()
        {
            AssignmentId = f.AssignmentId,
            ProductId = f.ProductId,
            ProductTitle = f.ProductTitle,
            ModelName = f.ModelName,
            PeriodLabel = f.PeriodLabel,
            Status = f.Status,
            FieldCompletionPercent = f.FieldCompletionPercent,
            MandatoryCompletionPercent = f.MandatoryCompletionPercent,
            DueUtc = f.DueUtc,
            IsOverdue = f.IsOverdue,
            UpdatedUtc = f.UpdatedUtc,
            PhaseName = f.PhaseName,
            BusinessAreasDisplay = JoinNames(f.BusinessAreaNames),
            DirectoratesDisplay = JoinNames(f.DirectorateNames),
            TypesDisplay = JoinNames(f.ServiceTypeNames),
            OwnersDisplay = JoinNames(f.OwnerEmails)
        };

    private static ServiceDataModelCompletionExportRowViewModel ToExport(
        ServiceDataModelCompletionAssignmentFact f) =>
        new()
        {
            ModelName = f.ModelName,
            ModelStableKey = f.ModelStableKey,
            VersionNumber = f.VersionNumber,
            PeriodLabel = f.PeriodLabel,
            ProductTitle = f.ProductTitle,
            ProductId = f.ProductId.ToString(),
            Status = f.Status.ToString(),
            FieldCompletionPercent = f.FieldCompletionPercent,
            MandatoryCompletionPercent = f.MandatoryCompletionPercent,
            DueUtc = f.DueUtc?.ToString("O"),
            IsOverdue = f.IsOverdue,
            LastAnsweredUtc = f.LastAnsweredUtc?.ToString("O"),
            SubmittedUtc = f.SubmittedUtc?.ToString("O"),
            ReviewedUtc = f.ReviewedUtc?.ToString("O"),
            PhaseName = f.PhaseName,
            BusinessAreas = JoinNames(f.BusinessAreaNames),
            Directorates = JoinNames(f.DirectorateNames),
            Types = JoinNames(f.ServiceTypeNames),
            Owners = JoinNames(f.OwnerEmails)
        };

    private static string JoinNames(IReadOnlyList<string> names) =>
        names.Count == 0 ? "" : string.Join("; ", names);
}
