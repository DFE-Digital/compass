using System.Collections.Generic;
using System.Linq;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Data;

/// <summary>Default DfE three-band risk tiers (operational + proposed rows for escalation).</summary>
public sealed record RiskTierSeedItem(
    string Code,
    string Name,
    string? Description,
    string? Summary,
    int SortOrder,
    int GovernanceLevel,
    bool IsProposedTier);

public static class RiskTierSeedData
{
    /// <summary>
    /// Six rows: Tier 1–3 operational and Tier 1–3 — Proposed (<see cref="RiskTier.IsProposedTier" />).
    /// Governance level 1 = highest (Tier 1 / PRC); 3 = lowest (Tier 3 / team).
    /// </summary>
    public static IReadOnlyList<RiskTierSeedItem> Defaults { get; } = new[]
    {
        Item("TIER1", "Tier 1", "Highest governance — portfolio / PRC visibility.", "Tier 1 — governance", 10, 1, false),
        Item("TIER1_PROP", "Tier 1 - Proposed", "Proposed move to Tier 1 (Operations approval).", null, 11, 1, true),
        Item("TIER2", "Tier 2", "Director-level governance.", "Tier 2 — Director", 20, 2, false),
        Item("TIER2_PROP", "Tier 2 - Proposed", "Proposed move to Tier 2 (Operations approval).", null, 21, 2, true),
        Item("TIER3", "Tier 3", "Team-level risk management (default band).", "Tier 3 — Team", 30, 3, false),
        Item("TIER3_PROP", "Tier 3 - Proposed", "Proposed move to Tier 3 (Operations approval).", null, 31, 3, true)
    };

    public static RiskTier ToEntity(RiskTierSeedItem seed, DateTime utcNow) => new()
    {
        Code = seed.Code,
        Name = seed.Name,
        Description = seed.Description,
        Summary = seed.Summary,
        SortOrder = seed.SortOrder,
        GovernanceLevel = seed.GovernanceLevel,
        IsProposedTier = seed.IsProposedTier,
        IsActive = true,
        CreatedAt = utcNow,
        UpdatedAt = utcNow
    };

    private static RiskTierSeedItem Item(
        string code,
        string name,
        string? description,
        string? summary,
        int sortOrder,
        int governanceLevel,
        bool isProposedTier) =>
        new(code, name, description, summary, sortOrder, governanceLevel, isProposedTier);

    /// <summary>Adds missing recommended tiers and aligns existing rows matched by <see cref="RiskTier.Code"/>.</summary>
    public static async Task<(int Added, int Updated)> ApplyAsync(
        CompassDbContext context,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.RiskTiers.ToListAsync(cancellationToken);
        var utcNow = DateTime.UtcNow;
        var added = 0;
        var updated = 0;

        foreach (var seed in Defaults)
        {
            var row = existing.FirstOrDefault(x =>
                string.Equals(x.Code.Trim(), seed.Code, StringComparison.OrdinalIgnoreCase));
            if (row == null)
            {
                context.RiskTiers.Add(ToEntity(seed, utcNow));
                added++;
                continue;
            }

            var changed = false;
            if (!string.Equals(row.Name.Trim(), seed.Name, StringComparison.Ordinal))
            {
                row.Name = seed.Name;
                changed = true;
            }

            if (row.GovernanceLevel != seed.GovernanceLevel)
            {
                row.GovernanceLevel = seed.GovernanceLevel;
                changed = true;
            }

            if (row.IsProposedTier != seed.IsProposedTier)
            {
                row.IsProposedTier = seed.IsProposedTier;
                changed = true;
            }

            if (row.SortOrder != seed.SortOrder)
            {
                row.SortOrder = seed.SortOrder;
                changed = true;
            }

            var desc = string.IsNullOrWhiteSpace(seed.Description) ? null : seed.Description.Trim();
            if ((row.Description ?? "") != (desc ?? ""))
            {
                row.Description = desc;
                changed = true;
            }

            var summary = string.IsNullOrWhiteSpace(seed.Summary) ? null : seed.Summary.Trim();
            if ((row.Summary ?? "") != (summary ?? ""))
            {
                row.Summary = summary;
                changed = true;
            }

            if (!changed)
                continue;

            row.UpdatedAt = utcNow;
            updated++;
        }

        if (added > 0 || updated > 0)
            await context.SaveChangesAsync(cancellationToken);

        return (added, updated);
    }
}
