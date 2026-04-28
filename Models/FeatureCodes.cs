namespace Compass.Models;

/// <summary>Stable <see cref="Feature.Code"/> values used for RBAC and global availability.</summary>
public static class FeatureCodes
{
    public const string Demand = "demand";

    /// <summary>Modern Standards UI (<c>/modern/standards</c>) — global toggle in Admin feature settings.</summary>
    public const string Standards = "standards";

    /// <summary>FIPS service register + CMDB-synced admin lists. When off, Compass reads product data from the CMS instead.</summary>
    public const string Fips = "fips";
}
