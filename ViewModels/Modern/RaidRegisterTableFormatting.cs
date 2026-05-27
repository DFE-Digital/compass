using Compass.Models;
using Compass.Models.Modern.Work;

namespace Compass.ViewModels.Modern;

/// <summary>Shared labels for RAID register list tables (risks / issues).</summary>
public static class RaidRegisterRelationKinds
{
    public const string Organisation = "Organisation";
    public const string Work = "Work";
    public const string Fips = "FIPS";
    public const string Unknown = "Unknown";
}

/// <summary>Maps risks/issues to relation column values (Work / FIPS / Organisation).</summary>
public static class RaidRegisterTableFormatting
{
    public static string? FormatRiskBusinessAreaLabels(Risk r)
    {
        var fromJunction = r.RiskBusinessAreas
            .Where(x => x.BusinessAreaLookup != null)
            .Select(x => x.BusinessAreaLookup!.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (fromJunction.Count > 0)
            return string.Join("; ", fromJunction);

        if (r.Project?.BusinessAreaLookup != null)
            return r.Project.BusinessAreaLookup.Name;

        return string.IsNullOrWhiteSpace(r.BusinessArea) ? null : r.BusinessArea;
    }

    public static string? FormatIssueBusinessAreaLabels(Issue i)
    {
        var fromJunction = i.IssueBusinessAreas
            .Where(x => x.BusinessAreaLookup != null)
            .Select(x => x.BusinessAreaLookup!.Name)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        if (fromJunction.Count > 0)
            return string.Join("; ", fromJunction);

        if (i.Project?.BusinessAreaLookup != null)
            return i.Project.BusinessAreaLookup.Name;

        return string.IsNullOrWhiteSpace(i.BusinessArea) ? null : i.BusinessArea;
    }

    public static RaidRegisterRelationParts BuildRiskRelation(Risk r)
    {
        var storedKind = r.RaidAssociationKind;
        var hasProductRow = r.PrimaryProductId.HasValue && r.PrimaryProduct != null;

        if (storedKind == RaidAssociationKinds.Organisation)
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Organisation, null, null);

        if (storedKind == RaidAssociationKinds.Product || hasProductRow)
        {
            var label = r.PrimaryProduct != null
                ? (r.PrimaryProduct.DisplayName ?? r.PrimaryProduct.FipsId)
                : (r.FipsId ?? r.ProductDocumentId ??
                   (r.PrimaryProductId.HasValue ? $"Product #{r.PrimaryProductId}" : null));
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Fips, null, label);
        }

        if (storedKind == RaidAssociationKinds.WorkItem || r.ProjectId.HasValue)
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Work, r.ProjectId, r.Project?.Title);

        if (!string.IsNullOrEmpty(r.Project?.Title))
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Work, r.ProjectId, r.Project.Title);

        return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Unknown, null, null);
    }

    public static RaidRegisterRelationParts BuildIssueRelation(Issue i)
    {
        var storedKind = i.RaidAssociationKind;
        var hasProductRow = i.PrimaryProductId.HasValue && i.PrimaryProduct != null;

        if (storedKind == RaidAssociationKinds.Organisation)
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Organisation, null, null);

        if (storedKind == RaidAssociationKinds.Product || hasProductRow)
        {
            var label = i.PrimaryProduct != null
                ? (i.PrimaryProduct.DisplayName ?? i.PrimaryProduct.FipsId)
                : (i.FipsId ?? i.ProductDocumentId ??
                   (i.PrimaryProductId.HasValue ? $"Product #{i.PrimaryProductId}" : null));
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Fips, null, label);
        }

        if (storedKind == RaidAssociationKinds.WorkItem || i.ProjectId.HasValue)
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Work, i.ProjectId, i.Project?.Title);

        if (!string.IsNullOrEmpty(i.Project?.Title))
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Work, i.ProjectId, i.Project.Title);

        return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Unknown, null, null);
    }

    public static string RiskScoreBandClass(decimal? score)
    {
        if (!score.HasValue) return string.Empty;
        var s = score.Value;
        if (s >= 20) return "raid-ss-score-badge--highest";
        if (s >= 15) return "raid-ss-score-badge--elevated";
        if (s >= 8) return "raid-ss-score-badge--medium";
        return "raid-ss-score-badge--lower";
    }

    public static string RiskRefScoreIndicatorClass(decimal? currentScore)
    {
        if (!currentScore.HasValue) return string.Empty;
        var s = currentScore.Value;
        if (s >= 20) return "raid-ss-ref-score--highest";
        if (s >= 15) return "raid-ss-ref-score--elevated";
        if (s >= 8) return "raid-ss-ref-score--medium";
        return "raid-ss-ref-score--lower";
    }

    public static string SpreadsheetBadgeLabel(string? label, bool uppercase = false)
    {
        if (string.IsNullOrWhiteSpace(label)) return "—";
        return uppercase ? label.ToUpperInvariant() : label;
    }

    public static RaidRegisterRelationParts BuildAssumptionRelation(Assumption a)
    {
        var storedKind = a.RaidAssociationKind;
        var hasProductRow = a.PrimaryProductId.HasValue && a.PrimaryProduct != null;

        if (storedKind == RaidAssociationKinds.Organisation)
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Organisation, null, null);

        if (storedKind == RaidAssociationKinds.Product || hasProductRow)
        {
            var label = a.PrimaryProduct != null
                ? (a.PrimaryProduct.DisplayName ?? a.PrimaryProduct.FipsId)
                : (a.PrimaryProductId.HasValue ? $"Product #{a.PrimaryProductId}" : null);
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Fips, null, label);
        }

        if (storedKind == RaidAssociationKinds.WorkItem || a.ProjectId.HasValue)
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Work, a.ProjectId, a.Project?.Title);

        if (!string.IsNullOrEmpty(a.Project?.Title))
            return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Work, a.ProjectId, a.Project.Title);

        return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Unknown, null, null);
    }

    public static RaidRegisterRelationParts BuildNearMissRelation(NearMiss nm)
    {
        var labels = new List<string>();
        if (nm.DirectorateLookup != null && !string.IsNullOrWhiteSpace(nm.DirectorateLookup.Name))
            labels.Add(nm.DirectorateLookup.Name);
        if (nm.BusinessAreaLookup != null && !string.IsNullOrWhiteSpace(nm.BusinessAreaLookup.Name))
            labels.Add(nm.BusinessAreaLookup.Name);

        var target = labels.Count > 0 ? string.Join(" · ", labels) : null;
        return new RaidRegisterRelationParts(RaidRegisterRelationKinds.Organisation, null, target);
    }

    /// <summary>Operational Tier 2/1 (post–Operations review) are read-only on the register spreadsheet.</summary>
    public static bool IsSpreadsheetRiskTierEditable(int? tierId, IReadOnlyList<SelectOption> spreadsheetTierOptions)
    {
        if (!tierId.HasValue)
            return true;
        return spreadsheetTierOptions.Any(o => o.Id == tierId.Value);
    }
}

public readonly record struct RaidRegisterRelationParts(
    string Kind,
    int? ProjectId,
    string? Target,
    string? WorkDetailSection = null,
    string? SourceLabel = null,
    string? RelatedTitle = null,
    string? RelatedDescription = null,
    string? LinkHref = null,
    /// <summary>UI radio value: work, product, organisation.</summary>
    string? AssociationUiKind = null,
    int? PrimaryProductId = null);
