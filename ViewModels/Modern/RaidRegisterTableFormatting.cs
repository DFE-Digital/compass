using Compass.Models;

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
}

public readonly record struct RaidRegisterRelationParts(string Kind, int? ProjectId, string? Target);
