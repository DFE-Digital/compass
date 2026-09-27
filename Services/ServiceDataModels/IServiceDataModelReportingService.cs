using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

public interface IServiceDataModelReportingService
{
    Task<ServiceDataModelCompletionReportViewModel> BuildCompletionReportAsync(
        string email,
        ServiceDataModelCompletionReportFilters? filters = null);

    Task<ServiceCensusAnalysisReportViewModel> BuildAnalysisReportAsync(
        string email,
        ServiceCensusAnalysisReportFilters? filters = null);

    /// <summary>
    /// Service Census Excel data export population (one sheet per theme, all in-scope submissions).
    /// </summary>
    Task<ServiceCensusDataExportModel> BuildDataExportAsync(
        string email,
        ServiceDataModelCompletionReportFilters? filters = null);
}
