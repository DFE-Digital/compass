using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    /// <summary>Service census reporting section — Overview and Analysis entry points.</summary>
    [HttpGet("service-census")]
    [HttpGet("~/ModernReporting/ServiceCensus")]
    public IActionResult ServiceCensus()
    {
        SetNav("reporting-service-census");
        return View("~/Views/Modern/Reporting/ServiceCensus.cshtml");
    }

    /// <summary>Reports landing — lists reportable service data models.</summary>
    [HttpGet("reports")]
    public async Task<IActionResult> Reports(CancellationToken cancellationToken = default)
    {
        SetNav("reporting-reports");

        var models = await _context.ServiceDataModels.AsNoTracking()
            .Where(m => m.IsReportable && m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft)
            .OrderBy(m => m.Name)
            .Select(m => new ServiceDataModelListRowViewModel
            {
                Id = m.Id,
                Name = m.Name,
                StableKey = m.StableKey,
                OwnerDisplayName = m.OwnerDisplayName,
                OwnerEmail = m.OwnerEmail,
                LifecycleStatus = m.LifecycleStatus,
                UpdatedUtc = m.UpdatedUtc
            })
            .ToListAsync(cancellationToken);

        return View("~/Views/Modern/Reporting/Reports.cshtml", new ServiceDataModelListViewModel
        {
            Rows = models,
            CanManage = false
        });
    }

    /// <summary>
    /// Service census overview (completion). Pretty URL plus legacy completion aliases.
    /// </summary>
    [HttpGet("service-census/overview")]
    [HttpGet("reports/service-census-completion")]
    [HttpGet("~/ModernReporting/ServiceCensusCompletion")]
    [HttpGet("~/ModernReporting/ServiceCensusOverview")]
    public async Task<IActionResult> ServiceCensusOverview(
        Guid? modelId,
        Guid? versionId,
        string? periodLabel,
        int? directorateId,
        int? businessAreaId,
        CMDBProductStatus? productStatus,
        int? phaseId,
        int? typeId,
        string? ownerEmail,
        string? search,
        string? groupBy,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        SetNav("reporting-service-census");
        _ = cancellationToken;

        var email = CurrentUserEmail ?? "";
        var filters = BuildCensusFilters(
            modelId, versionId, periodLabel, directorateId, businessAreaId,
            productStatus, phaseId, typeId, ownerEmail, search, groupBy, page);

        var vm = await _serviceDataModelReporting.BuildCompletionReportAsync(email, filters);

        var reportableModels = await _context.ServiceDataModels.AsNoTracking()
            .Where(m => m.IsReportable && m.LifecycleStatus != ServiceDataModelLifecycleStatus.Draft)
            .OrderBy(m => m.Name)
            .Select(m => new ServiceDataModelListRowViewModel { Id = m.Id, Name = m.Name })
            .ToListAsync(cancellationToken);
        ViewBag.ReportableModels = reportableModels;
        ViewBag.CensusReportActive = "overview";

        ViewBag.SearchAndFilter = BuildCensusCompletionSearchAndFilter(vm, reportableModels);

        return View("~/Views/Modern/Reporting/ServiceCensusOverview.cshtml", vm);
    }

    /// <summary>
    /// Excel data export — one worksheet per shared census theme, all in-scope submissions.
    /// Pretty URL plus legacy completion export aliases. Does not require a period.
    /// </summary>
    [HttpGet("service-census/overview/export")]
    [HttpGet("reports/service-census-completion/export")]
    [HttpGet("~/ModernReporting/ExportServiceCensusCompletion")]
    [HttpGet("~/ModernReporting/ExportServiceCensusOverview")]
    public async Task<IActionResult> ExportServiceCensusOverview(
        Guid? modelId,
        Guid? versionId,
        string? periodLabel,
        int? directorateId,
        int? businessAreaId,
        CMDBProductStatus? productStatus,
        int? phaseId,
        int? typeId,
        string? ownerEmail,
        string? search,
        string? groupBy,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        _ = groupBy; // cut dimension applies to the overview table only, not the data workbook
        var email = CurrentUserEmail ?? "";
        var filters = BuildCensusFilters(
            modelId, versionId, periodLabel, directorateId, businessAreaId,
            productStatus, phaseId, typeId, ownerEmail, search, groupBy: null, page: 1);

        var data = await _serviceDataModelReporting.BuildDataExportAsync(email, filters);
        var bytes = ServiceCensusDataExcelExport.BuildWorkbook(data);
        return File(bytes, ServiceCensusDataExcelExport.ContentType, ServiceCensusDataExcelExport.FileName);
    }

    [HttpGet("service-census/analysis")]
    [HttpGet("~/ModernReporting/ServiceCensusAnalysis")]
    public async Task<IActionResult> ServiceCensusAnalysis(
        Guid? modelId,
        int? directorateId,
        int? businessAreaId,
        CMDBProductStatus? productStatus,
        int? phaseId,
        int? typeId,
        string? ownerEmail,
        string? search,
        string? themeStableKey,
        string? fieldStableKey,
        string? optionKey,
        Guid? productId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        SetNav("reporting-service-census");
        _ = cancellationToken;

        var email = CurrentUserEmail ?? "";
        var filters = new ServiceCensusAnalysisReportFilters
        {
            ModelId = modelId,
            DirectorateId = directorateId,
            BusinessAreaId = businessAreaId,
            ProductStatus = productStatus,
            PhaseId = phaseId,
            TypeId = typeId,
            OwnerEmail = ownerEmail,
            Search = search,
            ThemeStableKey = themeStableKey,
            FieldStableKey = fieldStableKey,
            OptionKey = optionKey,
            ProductId = productId,
            Page = page < 1 ? 1 : page,
            PageSize = ServiceCensusAnalysisAggregation.DefaultPageSize
        };

        var vm = await _serviceDataModelReporting.BuildAnalysisReportAsync(email, filters);
        ViewBag.CensusReportActive = "analysis";
        ViewBag.SearchAndFilter = BuildCensusAnalysisSearchAndFilter(vm);

        return View("~/Views/Modern/Reporting/ServiceCensusAnalysis.cshtml", vm);
    }

    private static ServiceDataModelCompletionReportFilters BuildCensusFilters(
        Guid? modelId,
        Guid? versionId,
        string? periodLabel,
        int? directorateId,
        int? businessAreaId,
        CMDBProductStatus? productStatus,
        int? phaseId,
        int? typeId,
        string? ownerEmail,
        string? search,
        string? groupBy,
        int page) =>
        new()
        {
            ModelId = modelId,
            VersionId = versionId,
            PeriodLabel = periodLabel,
            DirectorateId = directorateId,
            BusinessAreaId = businessAreaId,
            ProductStatus = productStatus,
            PhaseId = phaseId,
            TypeId = typeId,
            OwnerEmail = ownerEmail,
            Search = search,
            GroupBy = ServiceDataModelCompletionReportGrouping.ParseGroupBy(groupBy),
            Page = page < 1 ? 1 : page,
            PageSize = ServiceDataModelCompletionReportGrouping.DefaultPageSize
        };

    private Compass.Models.SearchAndFilterViewModel BuildCensusCompletionSearchAndFilter(
        ServiceDataModelCompletionReportViewModel vm,
        IReadOnlyList<ServiceDataModelListRowViewModel> reportableModels)
    {
        var filters = vm.Filters;
        var groupBy = filters.GroupBy.ToString();
        var formAction = Url.Action(nameof(ServiceCensusOverview), "ModernReporting")
            ?? "/modern/reporting/service-census/overview";
        var clearUrl = Url.Action(nameof(ServiceCensusOverview), "ModernReporting", new { groupBy })
            ?? formAction;

        var sf = new Compass.Models.SearchAndFilterViewModel
        {
            IdPrefix = "census-report",
            SearchPlaceholder = "Search services or models…",
            SearchValue = filters.Search,
            FormActionUrl = formAction,
            FormMethod = "get",
            ClearUrl = clearUrl,
            HiddenFields = new List<KeyValuePair<string, string>>
            {
                new("groupBy", groupBy)
            },
            Fields = new List<Compass.Models.SearchAndFilterFieldViewModel>
            {
                new()
                {
                    Label = "Model",
                    Name = "modelId",
                    SelectedValue = filters.ModelId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All reportable models" } }
                        .Concat(reportableModels.Select(m => new Compass.Models.SearchAndFilterOption
                        {
                            Value = m.Id.ToString(),
                            Text = m.Name
                        }))
                        .ToList()
                },
                new()
                {
                    Label = "Business area",
                    Name = "businessAreaId",
                    SelectedValue = filters.BusinessAreaId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All business areas" } }
                        .Concat(vm.BusinessAreaOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Directorate",
                    Name = "directorateId",
                    SelectedValue = filters.DirectorateId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All directorates" } }
                        .Concat(vm.DirectorateOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Phase",
                    Name = "phaseId",
                    SelectedValue = filters.PhaseId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All phases" } }
                        .Concat(vm.PhaseOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Type",
                    Name = "typeId",
                    SelectedValue = filters.TypeId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All types" } }
                        .Concat(vm.TypeOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Product status",
                    Name = "productStatus",
                    SelectedValue = filters.ProductStatus?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "Any status" } }
                        .Concat(Enum.GetValues<CMDBProductStatus>()
                            .Select(s => new Compass.Models.SearchAndFilterOption { Value = s.ToString(), Text = s.ToString() }))
                        .ToList()
                },
                new()
                {
                    Label = "Model version",
                    Name = "versionId",
                    SelectedValue = filters.VersionId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "Any published version" } }
                        .Concat(vm.VersionOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Period (optional)",
                    Name = "periodLabel",
                    SelectedValue = filters.PeriodLabel,
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "Any / standing" } }
                        .Concat(vm.PeriodOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                }
            }
        };

        if (!string.IsNullOrWhiteSpace(filters.OwnerEmail))
            sf.HiddenFields.Add(new KeyValuePair<string, string>("ownerEmail", filters.OwnerEmail.Trim()));

        sf.ActiveChips = Compass.Helpers.SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf,
            Url,
            nameof(ServiceCensusOverview),
            "ModernReporting",
            new { groupBy, ownerEmail = filters.OwnerEmail });

        return sf;
    }

    private Compass.Models.SearchAndFilterViewModel BuildCensusAnalysisSearchAndFilter(
        ServiceCensusAnalysisReportViewModel vm)
    {
        var filters = vm.Filters;
        var formAction = Url.Action(nameof(ServiceCensusAnalysis), "ModernReporting")
            ?? "/modern/reporting/service-census/analysis";

        // Clear returns to themes summary, preserving register-dimension filters only when clearing chips individually.
        var clearUrl = Url.Action(nameof(ServiceCensusAnalysis), "ModernReporting") ?? formAction;

        var hidden = new List<KeyValuePair<string, string>>();
        // Preserve drill context when applying filters from a deeper page.
        if (!string.IsNullOrWhiteSpace(filters.FieldStableKey))
            hidden.Add(new("fieldStableKey", filters.FieldStableKey));
        if (!string.IsNullOrWhiteSpace(filters.OptionKey))
            hidden.Add(new("optionKey", filters.OptionKey));
        if (filters.ProductId.HasValue)
            hidden.Add(new("productId", filters.ProductId.Value.ToString()));
        if (!string.IsNullOrWhiteSpace(filters.OwnerEmail))
            hidden.Add(new("ownerEmail", filters.OwnerEmail.Trim()));

        var sf = new Compass.Models.SearchAndFilterViewModel
        {
            IdPrefix = "census-analysis",
            SearchPlaceholder = "Search services…",
            SearchValue = filters.Search,
            FormActionUrl = formAction,
            FormMethod = "get",
            ClearUrl = clearUrl,
            HiddenFields = hidden,
            Fields = new List<Compass.Models.SearchAndFilterFieldViewModel>
            {
                new()
                {
                    Label = "Theme",
                    Name = "themeStableKey",
                    SelectedValue = filters.ThemeStableKey,
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All themes" } }
                        .Concat(vm.ThemeOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Business area",
                    Name = "businessAreaId",
                    SelectedValue = filters.BusinessAreaId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All business areas" } }
                        .Concat(vm.BusinessAreaOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Directorate",
                    Name = "directorateId",
                    SelectedValue = filters.DirectorateId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All directorates" } }
                        .Concat(vm.DirectorateOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Phase",
                    Name = "phaseId",
                    SelectedValue = filters.PhaseId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All phases" } }
                        .Concat(vm.PhaseOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Type",
                    Name = "typeId",
                    SelectedValue = filters.TypeId?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "All types" } }
                        .Concat(vm.TypeOptions.Select(o => new Compass.Models.SearchAndFilterOption { Value = o.Value, Text = o.Text }))
                        .ToList()
                },
                new()
                {
                    Label = "Product status",
                    Name = "productStatus",
                    SelectedValue = filters.ProductStatus?.ToString(),
                    Options = new List<Compass.Models.SearchAndFilterOption> { new() { Value = "", Text = "Any status" } }
                        .Concat(Enum.GetValues<CMDBProductStatus>()
                            .Select(s => new Compass.Models.SearchAndFilterOption { Value = s.ToString(), Text = s.ToString() }))
                        .ToList()
                }
            }
        };

        sf.ActiveChips = Compass.Helpers.SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf,
            Url,
            nameof(ServiceCensusAnalysis),
            "ModernReporting",
            new
            {
                ownerEmail = filters.OwnerEmail,
                fieldStableKey = filters.FieldStableKey,
                optionKey = filters.OptionKey,
                productId = filters.ProductId
            });

        return sf;
    }
}
