using Compass.Models;
using Compass.ViewModels.Modern;

namespace Compass.Services.Raid;

/// <summary>
/// Tier labels and selectable bands for RAID register spreadsheets.
/// Operational Tier 2 and Tier 1 are assigned via Operations review; registers only offer Tier 3 and proposed targets.
/// </summary>
public static class RiskTierSpreadsheet
{
    public sealed record RaidRiskTierRows(
        RiskTier? Tier3,
        RiskTier? Tier2Operational,
        RiskTier? Tier2Proposed,
        RiskTier? Tier1Operational,
        RiskTier? Tier1Proposed);

    public static RaidRiskTierRows ResolveRows(IReadOnlyList<RiskTier> all)
    {
        static string NL(RiskTier t) => (t.Name ?? string.Empty).ToLowerInvariant();
        static string CL(RiskTier t) => (t.Code ?? string.Empty).ToLowerInvariant();

        var tier3 = all.FirstOrDefault(t => !t.IsProposedTier && t.GovernanceLevel == 3);
        tier3 ??= all.FirstOrDefault(t => !t.IsProposedTier && (NL(t).Contains("tier 3") || CL(t).Contains("3")));

        var tier2Op = all.FirstOrDefault(t => !t.IsProposedTier && t.GovernanceLevel == 2);
        tier2Op ??= all.FirstOrDefault(t => !t.IsProposedTier && (NL(t).Contains("tier 2") || CL(t) == "2"));

        var tier2Proposed = all.FirstOrDefault(t => t.IsProposedTier && t.GovernanceLevel == 2);
        tier2Proposed ??= all.FirstOrDefault(t => t.IsProposedTier && (NL(t).Contains("tier 2") || CL(t).Contains("2")));

        var tier1Op = all.FirstOrDefault(t => !t.IsProposedTier && t.GovernanceLevel == 1);
        tier1Op ??= all.FirstOrDefault(t => !t.IsProposedTier && (NL(t).Contains("tier 1") || CL(t) == "1"));

        var tier1Proposed = all.FirstOrDefault(t => t.IsProposedTier && t.GovernanceLevel == 1);
        tier1Proposed ??= all.FirstOrDefault(t => t.IsProposedTier && (NL(t).Contains("tier 1") || CL(t).Contains("1")));

        return new RaidRiskTierRows(tier3, tier2Op, tier2Proposed, tier1Op, tier1Proposed);
    }

    /// <summary>Display label for a risk's current tier (operational bands show as Tier 1/2/3; proposed as Tier N Proposed).</summary>
    public static string GetDisplayName(RiskTier? tier)
    {
        if (tier == null)
            return "—";

        if (tier.IsProposedTier)
        {
            if (tier.GovernanceLevel == 1 || (tier.Name ?? string.Empty).Contains("tier 1", StringComparison.OrdinalIgnoreCase))
                return "Tier 1 Proposed";
            if (tier.GovernanceLevel == 2 || (tier.Name ?? string.Empty).Contains("tier 2", StringComparison.OrdinalIgnoreCase))
                return "Tier 2 Proposed";
            if (tier.GovernanceLevel == 3 || (tier.Name ?? string.Empty).Contains("tier 3", StringComparison.OrdinalIgnoreCase))
                return "Tier 3 Proposed";
        }
        else
        {
            if (tier.GovernanceLevel == 1 || (tier.Name ?? string.Empty).Contains("tier 1", StringComparison.OrdinalIgnoreCase))
                return "Tier 1";
            if (tier.GovernanceLevel == 2 || (tier.Name ?? string.Empty).Contains("tier 2", StringComparison.OrdinalIgnoreCase))
                return "Tier 2";
            if (tier.GovernanceLevel == 3 || (tier.Name ?? string.Empty).Contains("tier 3", StringComparison.OrdinalIgnoreCase))
                return "Tier 3";
        }

        return string.IsNullOrWhiteSpace(tier.Name) ? tier.Code : tier.Name;
    }

    /// <summary>Tier options for inline spreadsheet editing: Tier 3, Tier 2 Proposed, Tier 1 Proposed.</summary>
    public static IReadOnlyList<SelectOption> BuildSpreadsheetSelectOptions(RaidRiskTierRows rows)
    {
        var list = new List<SelectOption>();
        if (rows.Tier3 != null)
            list.Add(new SelectOption(rows.Tier3.Id, "Tier 3"));
        if (rows.Tier2Proposed != null)
            list.Add(new SelectOption(rows.Tier2Proposed.Id, "Tier 2 Proposed"));
        if (rows.Tier1Proposed != null)
            list.Add(new SelectOption(rows.Tier1Proposed.Id, "Tier 1 Proposed"));
        return list;
    }

    public static HashSet<int> AllowedSpreadsheetTierIds(RaidRiskTierRows rows)
    {
        var set = new HashSet<int>();
        if (rows.Tier3 != null) set.Add(rows.Tier3.Id);
        if (rows.Tier2Proposed != null) set.Add(rows.Tier2Proposed.Id);
        if (rows.Tier1Proposed != null) set.Add(rows.Tier1Proposed.Id);
        return set;
    }
}
