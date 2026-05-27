using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

/// <summary>Builds relation column values with modal metadata for RAID register tables.</summary>
public static class RaidRegisterRelationEnrichment
{
    private const int DescriptionPreviewMaxLength = 400;

    public static async Task<Dictionary<string, CMDBProduct>> LoadCmdbProductsByFipsIdAsync(
        CompassDbContext db,
        IEnumerable<string> fipsIds,
        CancellationToken ct)
    {
        var ids = fipsIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0)
            return new Dictionary<string, CMDBProduct>(StringComparer.OrdinalIgnoreCase);

        var cmdbIds = ids.Where(id => !id.StartsWith("SR-", StringComparison.OrdinalIgnoreCase)).ToList();
        var srUniqueIds = ids
            .Where(id => id.StartsWith("SR-", StringComparison.OrdinalIgnoreCase))
            .Select(id => int.TryParse(id.AsSpan(3), out var n) ? n : (int?)null)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .ToList();

        var products = await db.CMDBProducts.AsNoTracking()
            .Where(p =>
                (p.CMDBID != null && cmdbIds.Contains(p.CMDBID)) ||
                srUniqueIds.Contains(p.UniqueID))
            .ToListAsync(ct);

        var map = new Dictionary<string, CMDBProduct>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in products)
        {
            var key = ResolveServiceRegisterFipsId(product);
            if (!string.IsNullOrEmpty(key) && !map.ContainsKey(key))
                map[key] = product;
        }

        return map;
    }

    public static RaidRegisterRelationParts EnrichRiskRelation(
        Risk risk,
        IUrlHelper url,
        string workDetailSection,
        IReadOnlyDictionary<string, CMDBProduct> cmdbByFipsId)
    {
        var baseRel = RaidRegisterTableFormatting.BuildRiskRelation(risk);
        return Enrich(baseRel, risk.Project, risk.PrimaryProduct, risk.RaidAssociationKind, url, workDetailSection, cmdbByFipsId);
    }

    public static RaidRegisterRelationParts EnrichIssueRelation(
        Issue issue,
        IUrlHelper url,
        string workDetailSection,
        IReadOnlyDictionary<string, CMDBProduct> cmdbByFipsId)
    {
        var baseRel = RaidRegisterTableFormatting.BuildIssueRelation(issue);
        return Enrich(baseRel, issue.Project, issue.PrimaryProduct, issue.RaidAssociationKind, url, workDetailSection, cmdbByFipsId);
    }

    public static RaidRegisterRelationParts EnrichAssumptionRelation(
        Assumption assumption,
        IUrlHelper url,
        IReadOnlyDictionary<string, CMDBProduct> cmdbByFipsId)
    {
        var baseRel = RaidRegisterTableFormatting.BuildAssumptionRelation(assumption);
        return Enrich(baseRel, assumption.Project, assumption.PrimaryProduct, assumption.RaidAssociationKind, url, "assumptions", cmdbByFipsId);
    }

    public static RaidRegisterRelationParts EnrichNearMissRelation(NearMiss nearMiss)
    {
        var baseRel = RaidRegisterTableFormatting.BuildNearMissRelation(nearMiss);
        if (baseRel.Kind != RaidRegisterRelationKinds.Organisation)
            return baseRel with { SourceLabel = "Organisation" };

        var scope = string.IsNullOrWhiteSpace(baseRel.Target) ? null : baseRel.Target;
        var description =
            "This is an organisational near miss. It is not linked to a work item or service register entry.";
        if (!string.IsNullOrWhiteSpace(scope))
            description += " Scope: " + scope + ".";

        return baseRel with
        {
            SourceLabel = "Organisation",
            RelatedTitle = "Organisation",
            RelatedDescription = description
        };
    }

    private static RaidRegisterRelationParts Enrich(
        RaidRegisterRelationParts baseRel,
        Project? project,
        FipsService? primaryProduct,
        string? storedKind,
        IUrlHelper url,
        string workDetailSection,
        IReadOnlyDictionary<string, CMDBProduct> cmdbByFipsId)
    {
        if (baseRel.Kind == RaidRegisterRelationKinds.Organisation ||
            string.Equals(storedKind, RaidAssociationKinds.Organisation, StringComparison.OrdinalIgnoreCase))
        {
            return baseRel with
            {
                SourceLabel = "Organisation",
                RelatedTitle = "Organisation",
                RelatedDescription =
                    "This is an organisational risk or issue. It is not linked to a work item or service register entry.",
                LinkHref = null
            };
        }

        if (baseRel.Kind == RaidRegisterRelationKinds.Work && baseRel.ProjectId is int projectId)
        {
            var wdHash = string.Equals(workDetailSection, "issues", StringComparison.OrdinalIgnoreCase)
                ? "wd-issues"
                : "wd-risks";
            var href = url.Action("Detail", "ModernWork", new { id = projectId, tab = workDetailSection })
                + "#" + wdHash;
            var title = project?.Title ?? baseRel.Target;
            var description = TruncateDescription(project?.Aim);
            return baseRel with
            {
                SourceLabel = "Work item",
                RelatedTitle = title,
                RelatedDescription = description,
                LinkHref = href
            };
        }

        if (baseRel.Kind == RaidRegisterRelationKinds.Fips)
        {
            CMDBProduct? cmdb = null;
            if (primaryProduct != null &&
                cmdbByFipsId.TryGetValue(primaryProduct.FipsId, out var found))
                cmdb = found;

            var title = cmdb?.Title
                ?? primaryProduct?.DisplayName
                ?? baseRel.Target;
            var description = TruncateDescription(
                cmdb?.UserDescription ?? cmdb?.CMDBDescription);
            string? href = null;
            if (cmdb != null)
                href = url.Action("FipsProduct", "ModernManage", new { id = cmdb.Id });

            return baseRel with
            {
                SourceLabel = "Service register",
                RelatedTitle = title,
                RelatedDescription = description,
                LinkHref = href
            };
        }

        return baseRel;
    }

    private static string? TruncateDescription(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        text = text.Trim();
        if (text.Length <= DescriptionPreviewMaxLength)
            return text;
        return text[..DescriptionPreviewMaxLength].TrimEnd() + "…";
    }

    private static string ResolveServiceRegisterFipsId(CMDBProduct product)
    {
        if (!string.IsNullOrWhiteSpace(product.CMDBID))
            return product.CMDBID.Trim();
        return product.UniqueID > 0 ? $"SR-{product.UniqueID}" : string.Empty;
    }
}
