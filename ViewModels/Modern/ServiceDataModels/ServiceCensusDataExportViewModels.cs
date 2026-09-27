using Compass.Services.ServiceDataModels;

namespace Compass.ViewModels.Modern.ServiceDataModels;

/// <summary>Workbook source for the Service Census Excel data export (one sheet per theme).</summary>
public sealed class ServiceCensusDataExportModel
{
    public IReadOnlyList<ServiceCensusThemeDefinition> Themes { get; init; } =
        Array.Empty<ServiceCensusThemeDefinition>();

    public IReadOnlyList<ServiceCensusDataExportServiceRow> Services { get; init; } =
        Array.Empty<ServiceCensusDataExportServiceRow>();
}

/// <summary>One service row; answer cells keyed by field stable key (empty string if unanswered).</summary>
public sealed class ServiceCensusDataExportServiceRow
{
    public Guid ProductId { get; init; }
    public int RegisterId { get; init; }
    public string ServiceName { get; init; } = string.Empty;

    /// <summary>Display values by field stable key. Missing keys are treated as unanswered (empty).</summary>
    public IReadOnlyDictionary<string, string> AnswersByFieldStableKey { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
