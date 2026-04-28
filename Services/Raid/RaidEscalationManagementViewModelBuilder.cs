using Compass.Data;
using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

/// <summary>Builds Operations RAID escalation tabs from persisted tier-change requests and live escalated records.</summary>
public static class RaidEscalationManagementViewModelBuilder
{
    private static string RiskRatingLabel(int score)
    {
        if (score >= 20) return "Crisis / Likely";
        if (score >= 16) return "Critical / Possible";
        if (score >= 11) return "High / Possible";
        if (score >= 6) return "Moderate / Possible";
        return "Low / Unlikely";
    }

    private static string IssueTierLabel(Issue issue)
    {
        var sev = issue.SeverityId ?? 0;
        if (sev >= 5) return "Tier 1 — PRC";
        if (sev >= 3) return "Tier 2 — Director";
        return "Tier 3 — Team";
    }

    private static string DisplayUser(User? u) =>
        u == null ? "Unknown" : (string.IsNullOrWhiteSpace(u.Name) ? u.Email : u.Name);

    private static string Snippet(string? text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim();
        return t.Length <= maxLen ? t : t[..maxLen].TrimEnd() + "…";
    }

    private static string FormatRiskTierChangeSummary(RaidEscalationTierChangeRequest r, IReadOnlyList<RiskTier> activeTiers)
    {
        var toName = r.ToRiskTier?.Name ?? "Tier";
        if (r.FromRiskTier == null)
            return $"Proposed: {toName}";
        var from = r.FromRiskTier;
        var to = r.ToRiskTier;
        if (from == null || to == null)
            return $"Proposed: {toName}";
        if (RiskTierGovernance.IsEscalation(from, to, activeTiers))
            return $"Escalate to {toName}";
        if (RiskTierGovernance.ResolveLevel(to, activeTiers) > RiskTierGovernance.ResolveLevel(from, activeTiers))
            return $"De-escalate to {toName}";
        return $"Change to {toName}";
    }

    public static async Task<ModernRaidEscalationManagementViewModel> BuildAsync(
        CompassDbContext db,
        string? activeTab,
        CancellationToken cancellationToken)
    {
        var activeTiers = await db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var approvedRiskMoves = await db.RaidEscalationTierChangeRequests.AsNoTracking()
            .Where(x => x.RecordType == "risk" && x.Status == "approved" && x.RiskId != null)
            .Include(x => x.Risk).ThenInclude(r => r!.RiskTier)
            .Include(x => x.Risk).ThenInclude(r => r!.OwnerUser)
            .Include(x => x.FromRiskTier)
            .Include(x => x.ToRiskTier)
            .Include(x => x.DecidedByUser)
            .OrderByDescending(x => x.DecidedAt ?? x.SubmittedAt)
            .ToListAsync(cancellationToken);

        var latestApprovedByRisk = approvedRiskMoves
            .GroupBy(x => x.RiskId!.Value)
            .Select(g => g.First())
            .ToList();

        var currentRows = latestApprovedByRisk
            .Where(x => x.Risk != null && !x.Risk.IsDeleted && x.Risk.ClosedDate == null)
            .Where(x => x.FromRiskTier != null && x.ToRiskTier != null)
            .Where(x => RiskTierGovernance.IsEscalation(x.FromRiskTier!, x.ToRiskTier!, activeTiers))
            .Select(x =>
            {
                var currentTier = x.Risk!.RiskTier?.Name ?? "Unassigned";
                var previousTier = x.FromRiskTier!.Name;
                var targetTier = x.ToRiskTier!.Name;
                return new ModernRaidEscalationCurrentRow(
                    "risk",
                    x.RiskId!.Value,
                    $"R-{x.RiskId:D4}",
                    x.Risk!.Title,
                    previousTier,
                    currentTier,
                    $"{previousTier} → {targetTier}",
                    DisplayUser(x.DecidedByUser),
                    x.DecidedAt,
                    x.Risk.OwnerUser != null ? (x.Risk.OwnerUser.Name ?? x.Risk.OwnerUser.Email) : x.Risk.OwnerEmail,
                    x.Risk.UpdatedAt);
            })
            .OrderByDescending(x => x.ApprovedAt ?? x.UpdatedAt)
            .Take(50)
            .ToList();

        var riskIdsWithPendingTierRequest = await db.RaidEscalationTierChangeRequests.AsNoTracking()
            .Where(x => x.RecordType == "risk" && x.Status == "pending" && x.RiskId != null)
            .Select(x => x.RiskId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var proposedRisks = await db.Risks.AsNoTracking()
            .Include(r => r.RiskTier)
            .Include(r => r.OwnerUser)
            .Where(r => !r.IsDeleted && r.ClosedDate == null)
            .Where(r => r.RiskTier != null && (r.RiskTier.IsProposedTier
                || (riskIdsWithPendingTierRequest.Count > 0 && riskIdsWithPendingTierRequest.Contains(r.Id))))
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);

        var proposedRiskIds = proposedRisks.Select(r => r.Id).ToList();
        var pendingReqs = proposedRiskIds.Count == 0
            ? new List<RaidEscalationTierChangeRequest>()
            : await db.RaidEscalationTierChangeRequests.AsNoTracking()
                .Where(x => x.Status == "pending" && x.RiskId != null && proposedRiskIds.Contains(x.RiskId.Value))
                .Include(x => x.Risk)
                .Include(x => x.FromRiskTier)
                .Include(x => x.ToRiskTier)
                .Include(x => x.SubmittedByUser)
                .OrderByDescending(x => x.SubmittedAt)
                .ToListAsync(cancellationToken);

        // Latest pending request per risk (submitted time). Used for list row + id for Operations action URL.
        var latestPendingByRiskId = pendingReqs
            .GroupBy(x => x.RiskId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.SubmittedAt).First());

        static bool ClassifyEscalation(
            Risk risk,
            RaidEscalationTierChangeRequest? req,
            IReadOnlyList<RiskTier> tiers)
        {
            if (req?.FromRiskTier != null && req.ToRiskTier != null)
                return RiskTierGovernance.IsEscalation(req.FromRiskTier, req.ToRiskTier, tiers);
            if (req?.ToRiskTier != null && risk.RiskTier != null)
                return RiskTierGovernance.IsEscalation(risk.RiskTier, req.ToRiskTier, tiers);
            return true;
        }

        var pendingRows = proposedRisks
            .Select(r =>
            {
                latestPendingByRiskId.TryGetValue(r.Id, out var req);
                var requestedBy = req != null
                    ? DisplayUser(req.SubmittedByUser)
                    : (r.OwnerUser != null ? (r.OwnerUser.Name ?? r.OwnerUser.Email) : (r.OwnerEmail ?? "Unknown"));
                var requestedAt = req?.SubmittedAt ?? r.UpdatedAt;
                var reason = req != null
                    ? Snippet(req.Rationale, 200)
                    : Snippet(r.Description, 200);
                var fullReason = req != null
                    ? (string.IsNullOrWhiteSpace(req.Rationale) ? null : req.Rationale.Trim())
                    : (string.IsNullOrWhiteSpace(r.Description) ? null : r.Description.Trim());
                var changeLabel = req != null
                    ? FormatRiskTierChangeSummary(req, activeTiers)
                    : $"In proposed tier: {r.RiskTier?.Name ?? "Proposed"}";

                var currentTierLabel = req?.FromRiskTier?.Name ?? r.RiskTier?.Name ?? "—";
                var proposedTierLabel = req?.ToRiskTier?.Name ?? "—";
                var isEscalation = ClassifyEscalation(r, req, activeTiers);

                return new ModernRaidEscalationPendingRow(
                    req?.Id,
                    "risk",
                    r.Id,
                    $"R-{r.Id:D4}",
                    r.Title,
                    changeLabel,
                    currentTierLabel,
                    proposedTierLabel,
                    isEscalation,
                    requestedBy,
                    requestedAt,
                    reason,
                    fullReason);
            })
            .ToList();

        var pendingEscalations = pendingRows.Where(x => x.IsEscalation).ToList();
        var pendingDeescalations = pendingRows.Where(x => !x.IsEscalation).ToList();

        var t = (activeTab ?? "escalations").Trim().ToLowerInvariant() switch
        {
            "deescalations" or "de-escalations" => "deescalations",
            "current" or "escalated" => "current",
            "active" or "a" or "all-risks" or "risks" => "active",
            _ => "escalations"
        };

        var activeRisksRaw = await db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.ClosedDate == null)
            .Include(r => r.RiskTier)
            .Include(r => r.RiskStatus)
            .OrderByDescending(r => r.UpdatedAt)
            .Take(1000)
            .ToListAsync(cancellationToken);
        var activeRisks = activeRisksRaw
            .Select(r => new ModernRaidActiveRiskRow(
                r.Id,
                $"R-{r.Id:D4}",
                r.Title,
                r.RiskTier?.Name,
                r.RiskStatus?.Label ?? r.Status,
                r.UpdatedAt))
            .ToList();

        return new ModernRaidEscalationManagementViewModel
        {
            ActiveTab = t,
            PendingApprovalCount = pendingRows.Count,
            PendingEscalationsCount = pendingEscalations.Count,
            PendingDeescalationsCount = pendingDeescalations.Count,
            CurrentlyEscalatedCount = currentRows.Count,
            ActiveRisksCount = activeRisks.Count,
            PendingEscalations = pendingEscalations,
            PendingDeescalations = pendingDeescalations,
            Current = currentRows,
            ActiveRisks = activeRisks
        };
    }
}
