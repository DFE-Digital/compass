namespace Compass.Models.Fips;

public sealed class FipsManageDashboardViewModel
{
    public int MyProductsCount { get; init; }
    public int ActiveProductsCount { get; init; }
    public int EnterpriseProductsCount { get; init; }
    public int ServiceLinesCount { get; init; }

    public string FipsPublicBaseUrl { get; init; } = "https://fips.education.gov.uk";

    /// <summary>Active catalogue products shown on the dashboard.</summary>
    public FipsProductsViewModel ActiveProducts { get; init; } = new() { ActiveTab = "active" };
}
