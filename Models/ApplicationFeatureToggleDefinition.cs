using System.Collections.Generic;
using System.Linq;

namespace Compass.Models;

/// <summary>
/// Master list of product areas exposed on Admin → System → Feature settings.
/// Add entries here when introducing new globally toggled features (must match <see cref="Feature.Code"/>).
/// </summary>
public sealed record ApplicationFeatureToggleDefinition(
    string Code,
    string Name,
    string Label,
    string? Hint,
    bool DefaultEnabled = true)
{
    public static IReadOnlyList<ApplicationFeatureToggleDefinition> All { get; } =
        new ApplicationFeatureToggleDefinition[]
        {
            new(
                Code: FeatureCodes.Demand,
                Name: "Demand",
                Label: "Demand",
                Hint: "Demand pipeline, triage, Operations demand tools, and Demand navigation.",
                DefaultEnabled: true),
            new(
                Code: FeatureCodes.Standards,
                Name: "Standards",
                Label: "Standards",
                Hint: "Modern Standards area (/modern/standards), including dashboard, DDT Standards, and Functional Standards navigation.",
                DefaultEnabled: true),
            new(
                Code: FeatureCodes.Fips,
                Name: "FIPS service register",
                Label: "FIPS service register",
                Hint: "When On, Service register and FIPS admin configuration use the **synced database** (CMDB products in Compass). When Off, that area is disabled and FIPS product data should be taken from the **CMS** (Strapi).",
                DefaultEnabled: true),
            new(
                Code: FeatureCodes.Raid,
                Name: "RAID",
                Label: "RAID",
                Hint: "RAID area (/modern/raid), the Risks & Issues tab on work items, RAID panes on products (Service register and Operations), Operations RAID escalations, and the Reporting → RAID intelligence area.",
                DefaultEnabled: true),
            new(
                Code: FeatureCodes.Ddr,
                Name: "Design Decision Records",
                Label: "Design Decision Records (DDRs)",
                Hint: "DDR area (/modern/design-decision-records), DDR panels on FIPS products and work items, the Reporting → Design decision records area, and the DesignOps oversight queue. Default Off — turn on for teams piloting DDRs.",
                DefaultEnabled: false),
        };

    public static HashSet<string> AllowedCodes { get; } =
        All.Select(d => d.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
}
