namespace Compass.Models;

/// <summary>Stable <see cref="Feature.Code"/> values used for RBAC and global availability.</summary>
public static class FeatureCodes
{
    public const string Demand = "demand";

    /// <summary>Modern Standards UI (<c>/modern/standards</c>) — global toggle in Admin feature settings.</summary>
    public const string Standards = "standards";

    /// <summary>FIPS service register + CMDB-synced admin lists. When off, Compass reads product data from the CMS instead.</summary>
    public const string Fips = "fips";

    /// <summary>RAID area (<c>/modern/raid</c>) plus risks/issues panes embedded in work, products, operations and reporting.</summary>
    public const string Raid = "raid";

    /// <summary>Design Decision Records (<c>/modern/design-decision-records</c>) plus DDR panels on FIPS products and work items, and the Reporting → DDR area.</summary>
    public const string Ddr = "ddr";
}
