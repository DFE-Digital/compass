using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

/// <summary>
/// Derives the last time inherent risk / issue rating fields changed from <see cref="AuditLog"/> entries,
/// for RAID dashboard “unchanged for 3 months” (90 days) signals.
/// </summary>
public static class RaidDashboardAuditHealth
{
    private static readonly string[] RiskRatingFieldKeys =
    {
        "RiskScore",
        "ImpactRating",
        "LikelihoodRating",
        "RiskImpactLevelId",
        "RiskLikelihoodId"
    };

    private static readonly string[] IssueRatingFieldKeys =
    {
        "SeverityId",
        "Severity",
        "PriorityId",
        "Priority"
    };

    /// <summary>Maps risk id to UTC instant of the most recent create/update that set or changed a rating field.</summary>
    public static async Task<Dictionary<int, DateTime>> GetLastRiskRatingChangeUtcByRiskIdAsync(
        CompassDbContext db,
        IReadOnlyCollection<int> openRiskIds,
        CancellationToken cancellationToken = default)
    {
        if (openRiskIds.Count == 0)
            return new Dictionary<int, DateTime>();

        var asStrings = openRiskIds.Select(x => x.ToString()).ToList();
        var raw = await db.AuditLogs.AsNoTracking()
            .Where(
                a => a.Entity == nameof(Risk) &&
                     (a.Action == "Update" || a.Action == "Create") &&
                     asStrings.Contains(a.EntityId))
            .Select(a => new { a.EntityId, a.Action, a.BeforeJson, a.AfterJson, a.ChangedUtc })
            .ToListAsync(cancellationToken);
        var rows = raw.Select(a => new AuditLogRow(a.EntityId, a.Action, a.BeforeJson, a.AfterJson, a.ChangedUtc)).ToList();

        return BuildLastChangeMap(rows, RiskRatingFieldKeys);
    }

    public static async Task<Dictionary<int, DateTime>> GetLastIssueRatingChangeUtcByIssueIdAsync(
        CompassDbContext db,
        IReadOnlyCollection<int> openIssueIds,
        CancellationToken cancellationToken = default)
    {
        if (openIssueIds.Count == 0)
            return new Dictionary<int, DateTime>();

        var asStrings = openIssueIds.Select(x => x.ToString()).ToList();
        var raw = await db.AuditLogs.AsNoTracking()
            .Where(
                a => a.Entity == nameof(Issue) &&
                     (a.Action == "Update" || a.Action == "Create") &&
                     asStrings.Contains(a.EntityId))
            .Select(a => new { a.EntityId, a.Action, a.BeforeJson, a.AfterJson, a.ChangedUtc })
            .ToListAsync(cancellationToken);
        var rows = raw.Select(a => new AuditLogRow(a.EntityId, a.Action, a.BeforeJson, a.AfterJson, a.ChangedUtc)).ToList();

        return BuildLastChangeMap(rows, IssueRatingFieldKeys);
    }

    private static Dictionary<int, DateTime> BuildLastChangeMap(
        IReadOnlyList<AuditLogRow> rows,
        IReadOnlyList<string> fieldKeys)
    {
        var byId = new Dictionary<int, DateTime>();
        foreach (var g in rows.GroupBy(x => x.EntityId, StringComparer.Ordinal))
        {
            if (!int.TryParse(g.Key, out var eid))
                continue;
            foreach (var log in g.OrderBy(x => x.ChangedUtc))
            {
                if (log.Action.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    byId[eid] = log.ChangedUtc;
                    continue;
                }

                if (!log.Action.Equals("Update", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (RatingFieldsChanged(log.BeforeJson, log.AfterJson, fieldKeys))
                    byId[eid] = log.ChangedUtc;
            }
        }

        return byId;
    }

    private static bool RatingFieldsChanged(string? beforeJson, string? afterJson, IReadOnlyList<string> keys)
    {
        var b = ParseJsonDict(beforeJson);
        var a = ParseJsonDict(afterJson);
        foreach (var k in keys)
        {
            var bePresent = b.TryGetValue(k, out var be);
            var aePresent = a.TryGetValue(k, out var ae);
            if (bePresent != aePresent)
                return true;
            if (bePresent && ElementToString(be) != ElementToString(ae))
                return true;
        }

        return false;
    }

    private static Dictionary<string, JsonElement> ParseJsonDict(string? json)
    {
        var d = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
            return d;
        try
        {
            using var doc = JsonDocument.Parse(json!);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return d;
            foreach (var p in doc.RootElement.EnumerateObject())
                d[p.Name] = p.Value.Clone();
        }
        catch
        {
            /* ignore */
        }

        return d;
    }

    private static string ElementToString(JsonElement? el)
    {
        if (el == null) return "";
        var e = el.Value;
        return e.ValueKind switch
        {
            JsonValueKind.String => e.GetString() ?? "",
            JsonValueKind.Number => e.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => e.ToString()
        };
    }

    private sealed record AuditLogRow(
        string EntityId,
        string Action,
        string? BeforeJson,
        string? AfterJson,
        DateTime ChangedUtc);
}
