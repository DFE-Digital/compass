using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Services;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

/// <summary>
/// Builds admin-facing work item history rows from <see cref="AuditLog"/> entries
/// and related entities for a <see cref="Project"/>.
/// </summary>
public static class WorkItemAuditTimelineBuilder
{
    private const int MaxRows = 500;

    public static async Task<IReadOnlyList<LifecycleAuditEntry>> BuildAsync(
        CompassDbContext db,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var projectIdStr = projectId.ToString();

        var milestoneIds = await db.Milestones.AsNoTracking()
            .Where(m => m.ProjectId == projectId && !m.IsDeleted)
            .Select(m => m.Id.ToString())
            .ToListAsync(cancellationToken);

        var monthlyUpdateIds = await db.ProjectMonthlyUpdates.AsNoTracking()
            .Where(mu => mu.ProjectId == projectId)
            .Select(mu => mu.Id.ToString())
            .ToListAsync(cancellationToken);

        var narrativeIds = await db.MonthlyUpdateNarratives.AsNoTracking()
            .Where(n => n.ProjectMonthlyUpdate.ProjectId == projectId)
            .Select(n => n.Id.ToString())
            .ToListAsync(cancellationToken);

        var statusUpdateIds = await db.ProjectStatusUpdates.AsNoTracking()
            .Where(psu => psu.ProjectId == projectId)
            .Select(psu => psu.Id.ToString())
            .ToListAsync(cancellationToken);

        var ragHistoryIds = await db.ProjectRagHistories.AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .Select(r => r.Id.ToString())
            .ToListAsync(cancellationToken);

        var riskIds = await db.Risks.AsNoTracking()
            .Where(r => r.ProjectId == projectId && !r.IsDeleted)
            .Select(r => r.Id.ToString())
            .ToListAsync(cancellationToken);

        var issueIds = await db.Issues.AsNoTracking()
            .Where(i => i.ProjectId == projectId && !i.IsDeleted)
            .Select(i => i.Id.ToString())
            .ToListAsync(cancellationToken);

        var assumptionIds = await db.Assumptions.AsNoTracking()
            .Where(a => a.ProjectId == projectId && !a.IsDeleted)
            .Select(a => a.Id.ToString())
            .ToListAsync(cancellationToken);

        var dependencyIds = await db.Dependencies.AsNoTracking()
            .Where(d =>
                (d.SourceEntityType == "Project" && d.SourceEntityId == projectId) ||
                (d.TargetEntityType == "Project" && d.TargetEntityId == projectId))
            .Select(d => d.Id.ToString())
            .ToListAsync(cancellationToken);

        var contactIds = await db.ProjectContacts.AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .Select(c => c.Id.ToString())
            .ToListAsync(cancellationToken);

        var actionIds = await db.Actions.AsNoTracking()
            .Where(a => a.ProjectId == projectId && !a.IsDeleted)
            .Select(a => a.Id.ToString())
            .ToListAsync(cancellationToken);

        var auditLogs = await db.AuditLogs.AsNoTracking()
            .Where(a =>
                (a.Entity == nameof(Project) && a.EntityId == projectIdStr) ||
                (a.Entity == nameof(Milestone) && milestoneIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(ProjectMonthlyUpdate) && monthlyUpdateIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(MonthlyUpdateNarrative) && narrativeIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(ProjectStatusUpdate) && statusUpdateIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(ProjectRagHistory) && ragHistoryIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(Risk) && riskIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(Issue) && issueIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(Assumption) && assumptionIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(Dependency) && dependencyIds.Contains(a.EntityId)) ||
                (a.Entity == nameof(ProjectContact) && contactIds.Contains(a.EntityId)) ||
                (a.Entity == "Action" && actionIds.Contains(a.EntityId)))
            .OrderByDescending(a => a.ChangedUtc)
            .Take(MaxRows)
            .ToListAsync(cancellationToken);

        auditLogs = auditLogs
            .Where(a => a.Entity != nameof(Project) || a.EntityId == projectIdStr)
            .ToList();

        var projectCreateLog = await FindProjectCreateLogAsync(db, projectId, cancellationToken);

        var creatorName = await ResolveProjectCreatorNameAsync(db, projectId, cancellationToken);

        var logsForLookup = auditLogs.ToList();
        if (projectCreateLog != null
            && !logsForLookup.Any(l => l.AuditLogId == projectCreateLog.AuditLogId))
        {
            logsForLookup.Add(projectCreateLog);
        }

        var nameLookup = await BuildUserNameLookupAsync(db, logsForLookup, cancellationToken);

        var items = auditLogs
            .Select(log => MapLog(log, nameLookup))
            .ToList();

        EnrichWorkItemCreateWho(items, projectCreateLog, creatorName, nameLookup);

        var hasProjectCreate = items.Any(i =>
            i.Event.StartsWith("Work item creat", StringComparison.OrdinalIgnoreCase));

        if (!hasProjectCreate)
        {
            var project = await db.Projects.AsNoTracking()
                .Where(p => p.Id == projectId && !p.IsDeleted)
                .Select(p => new { p.CreatedAt })
                .FirstOrDefaultAsync(cancellationToken);

            if (project != null)
            {
                items.Add(new LifecycleAuditEntry
                {
                    When = projectCreateLog?.ChangedUtc ?? project.CreatedAt,
                    Event = "Work item created",
                    Who = ResolveWorkItemCreateWho(projectCreateLog, creatorName, nameLookup),
                    Highlight = true
                });
            }
        }

        return items
            .OrderByDescending(i => i.When)
            .Take(MaxRows)
            .ToList();
    }

    /// <summary>
    /// Finds the Project Create audit row. Older rows may have a temporary EF negative
    /// <see cref="AuditLog.EntityId"/> because the id was captured before SaveChanges assigned it.
    /// </summary>
    private static async Task<Compass.Models.AuditLog?> FindProjectCreateLogAsync(
        CompassDbContext db,
        int projectId,
        CancellationToken cancellationToken)
    {
        var projectIdStr = projectId.ToString();

        var exact = await db.AuditLogs.AsNoTracking()
            .Where(a =>
                a.Entity == nameof(Project)
                && a.EntityId == projectIdStr
                && a.Action == "Create")
            .OrderBy(a => a.ChangedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (exact != null)
            return exact;

        var project = await db.Projects.AsNoTracking()
            .Where(p => p.Id == projectId && !p.IsDeleted)
            .Select(p => new { p.CreatedAt, p.ProjectCode, p.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (project == null)
            return null;

        var windowStart = project.CreatedAt.AddSeconds(-2);
        var windowEnd = project.CreatedAt.AddSeconds(2);

        var candidates = await db.AuditLogs.AsNoTracking()
            .Where(a =>
                a.Entity == nameof(Project)
                && a.Action == "Create"
                && a.ChangedUtc >= windowStart
                && a.ChangedUtc <= windowEnd)
            .OrderBy(a => a.ChangedUtc)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(project.ProjectCode))
        {
            var codeNeedle = JsonSerializer.Serialize(project.ProjectCode.Trim());
            var byCode = candidates.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.AfterJson)
                && a.AfterJson.Contains(codeNeedle, StringComparison.Ordinal));
            if (byCode != null)
                return byCode;
        }

        if (!string.IsNullOrWhiteSpace(project.Title))
        {
            var titleNeedle = JsonSerializer.Serialize(project.Title.Trim());
            var byTitle = candidates.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.AfterJson)
                && a.AfterJson.Contains(titleNeedle, StringComparison.Ordinal));
            if (byTitle != null)
                return byTitle;
        }

        return candidates.Count == 1 ? candidates[0] : null;
    }

    private static LifecycleAuditEntry MapLog(Compass.Models.AuditLog log, IReadOnlyDictionary<string, string> nameLookup)
    {
        var who = ResolveWho(log, nameLookup);
        var entityLabel = EntityLabel(log.Entity);
        var actionLabel = AuditFeatureMap.FriendlyAction(log.Action);
        if (string.IsNullOrWhiteSpace(actionLabel))
            actionLabel = log.Action;

        var detail = BuildChangeSummary(log);
        string @event;
        if (log.Entity == nameof(Project)
            && string.Equals(log.Action, "Create", StringComparison.OrdinalIgnoreCase))
        {
            @event = "Work item created";
        }
        else
        {
            @event = string.IsNullOrWhiteSpace(detail)
                ? $"{entityLabel} {actionLabel.ToLowerInvariant()}"
                : $"{entityLabel} {actionLabel.ToLowerInvariant()} — {detail}";
        }

        var reference = SanitiseReference(log.EntityReference, log.Entity);
        if (!string.IsNullOrWhiteSpace(reference)
            && !@event.Contains(reference, StringComparison.OrdinalIgnoreCase))
        {
            @event += $" ({reference})";
        }

        return new LifecycleAuditEntry
        {
            When = log.ChangedUtc,
            Event = @event,
            Who = who,
            Highlight = log.Entity == nameof(Project)
                && string.Equals(log.Action, "Create", StringComparison.OrdinalIgnoreCase)
        };
    }

    private static string ResolveWho(Compass.Models.AuditLog log, IReadOnlyDictionary<string, string> nameLookup)
    {
        if (!string.IsNullOrWhiteSpace(log.ChangedByUserId)
            && nameLookup.TryGetValue("id:" + log.ChangedByUserId, out var byId))
            return byId;

        if (!string.IsNullOrWhiteSpace(log.ChangedByEmail))
        {
            var emailKey = "email:" + log.ChangedByEmail.Trim().ToLowerInvariant();
            if (nameLookup.TryGetValue(emailKey, out var byEmail))
                return byEmail;
        }

        if (!string.IsNullOrWhiteSpace(log.ChangedBy))
        {
            var trimmed = log.ChangedBy.Trim();
            if (trimmed.Contains('@', StringComparison.Ordinal))
            {
                var emailKey = "email:" + trimmed.ToLowerInvariant();
                if (nameLookup.TryGetValue(emailKey, out var byEmail))
                    return byEmail;
            }
            return trimmed;
        }

        if (!string.IsNullOrWhiteSpace(log.ChangedByEmail))
            return log.ChangedByEmail.Trim();

        return "—";
    }

    private static string ResolveWorkItemCreateWho(
        Compass.Models.AuditLog? projectCreateLog,
        string? creatorName,
        IReadOnlyDictionary<string, string> nameLookup)
    {
        if (projectCreateLog != null)
        {
            var fromAudit = ResolveWho(projectCreateLog, nameLookup);
            if (fromAudit != "—")
                return fromAudit;
        }

        return string.IsNullOrWhiteSpace(creatorName) ? "—" : creatorName;
    }

    private static void EnrichWorkItemCreateWho(
        List<LifecycleAuditEntry> items,
        Compass.Models.AuditLog? projectCreateLog,
        string? creatorName,
        IReadOnlyDictionary<string, string> nameLookup)
    {
        var resolved = ResolveWorkItemCreateWho(projectCreateLog, creatorName, nameLookup);
        if (resolved == "—")
            return;

        foreach (var item in items)
        {
            if (item.Event.StartsWith("Work item creat", StringComparison.OrdinalIgnoreCase)
                && item.Who == "—")
            {
                item.Who = resolved;
            }
        }
    }

    private static async Task<string?> ResolveProjectCreatorNameAsync(
        CompassDbContext db,
        int projectId,
        CancellationToken cancellationToken)
    {
        var ps = await db.ProjectProblemStatements.AsNoTracking()
            .Where(p => p.ProjectId == projectId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new { p.CreatedByName, p.CreatedByEmail })
            .FirstOrDefaultAsync(cancellationToken);

        if (ps == null)
            return null;

        if (!string.IsNullOrWhiteSpace(ps.CreatedByName))
            return ps.CreatedByName.Trim();

        if (string.IsNullOrWhiteSpace(ps.CreatedByEmail))
            return null;

        var email = ps.CreatedByEmail.Trim().ToLowerInvariant();
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == email)
            .Select(u => new { u.Name, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(user?.Name))
            return user.Name.Trim();

        return ps.CreatedByEmail.Trim();
    }

    private static async Task<Dictionary<string, string>> BuildUserNameLookupAsync(
        CompassDbContext db,
        IReadOnlyList<Compass.Models.AuditLog> logs,
        CancellationToken cancellationToken)
    {
        var userIds = logs
            .Select(l => l.ChangedByUserId)
            .Where(id => !string.IsNullOrWhiteSpace(id) && int.TryParse(id, out _))
            .Select(id => int.Parse(id!))
            .Distinct()
            .ToList();

        var emails = logs
            .SelectMany(l => new[] { l.ChangedByEmail, l.ChangedBy })
            .Where(e => !string.IsNullOrWhiteSpace(e) && e!.Contains('@', StringComparison.Ordinal))
            .Select(e => e!.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var lookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (userIds.Count > 0)
        {
            var byId = await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToListAsync(cancellationToken);

            foreach (var u in byId)
            {
                var display = !string.IsNullOrWhiteSpace(u.Name) ? u.Name.Trim() : u.Email ?? "—";
                lookup["id:" + u.Id] = display;
                if (!string.IsNullOrWhiteSpace(u.Email))
                    lookup["email:" + u.Email.Trim().ToLowerInvariant()] = display;
            }
        }

        if (emails.Count > 0)
        {
            var byEmail = await db.Users.AsNoTracking()
                .Where(u => u.Email != null && emails.Contains(u.Email.ToLower()))
                .Select(u => new { u.Name, u.Email })
                .ToListAsync(cancellationToken);

            foreach (var u in byEmail)
            {
                if (string.IsNullOrWhiteSpace(u.Email)) continue;
                var display = !string.IsNullOrWhiteSpace(u.Name) ? u.Name.Trim() : u.Email;
                lookup["email:" + u.Email.Trim().ToLowerInvariant()] = display;
            }
        }

        return lookup;
    }

    private static string EntityLabel(string entity) => entity switch
    {
        nameof(Project) => "Work item",
        nameof(Milestone) => "Milestone",
        nameof(ProjectMonthlyUpdate) => "Monthly update",
        nameof(MonthlyUpdateNarrative) => "Monthly update narrative",
        nameof(ProjectStatusUpdate) => "Status update",
        nameof(ProjectRagHistory) => "RAG status",
        nameof(Risk) => "Risk",
        nameof(Issue) => "Issue",
        nameof(Assumption) => "Assumption",
        nameof(Dependency) => "Dependency",
        nameof(ProjectContact) => "Contact",
        "Action" => "Action",
        _ => AuditFeatureMap.FriendlyEntityName(entity)
    };

    private static string? SanitiseReference(string? reference, string entity)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var trimmed = reference.Trim();
        if (trimmed.StartsWith("Compass.", StringComparison.Ordinal)) return null;
        if (string.Equals(trimmed, entity, StringComparison.Ordinal)) return null;
        return trimmed.Length > 80 ? trimmed[..77] + "…" : trimmed;
    }

    private static string? BuildChangeSummary(Compass.Models.AuditLog log)
    {
        if (string.Equals(log.Action, "Create", StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.Equals(log.Action, "Delete", StringComparison.OrdinalIgnoreCase))
            return null;

        var changedFields = GetChangedFieldLabels(log.BeforeJson, log.AfterJson);
        return changedFields.Count == 0 ? null : string.Join(", ", changedFields);
    }

    private static List<string> GetChangedFieldLabels(string? beforeJson, string? afterJson)
    {
        var before = ParseTopLevelObject(beforeJson);
        var after = ParseTopLevelObject(afterJson);
        if (before == null && after == null) return new List<string>();

        before ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        after ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var keys = new SortedSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var key in after.Keys) keys.Add(key);

        var labels = new List<string>();
        foreach (var key in keys)
        {
            before.TryGetValue(key, out var beforeVal);
            after.TryGetValue(key, out var afterVal);
            if (string.Equals(beforeVal, afterVal, StringComparison.Ordinal)) continue;
            labels.Add(AuditFeatureMap.FriendlyFieldName(key));
        }

        return labels;
    }

    private static Dictionary<string, string?>? ParseTopLevelObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => prop.Value.GetString(),
                    _ => prop.Value.ToString()
                };
            }
            return dict;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
