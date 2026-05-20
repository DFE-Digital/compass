using System.Text;
using System.Text.Json;
using Compass.Models;
using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>
/// Audit explorer for <see cref="AuditLog"/> rows, surfaced under
/// <c>/modern/admin/audit-explorer</c>. Provides a filterable, paged list
/// view and a per-record detail page that renders a human-readable diff of
/// <see cref="AuditLog.BeforeJson"/> and <see cref="AuditLog.AfterJson"/>.
/// </summary>
public partial class ModernAdminController
{
    private const int AuditExplorerDefaultPageSize = 50;
    private const int AuditExplorerMaxPageSize = 200;

    private static readonly JsonSerializerOptions _auditDiffJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [HttpGet("audit-explorer")]
    public async Task<IActionResult> AuditExplorer([FromQuery] AuditExplorerFilterVm? filter, CancellationToken cancellationToken)
    {
        SetAdminChrome("admin-audit-explorer");

        filter = NormaliseFilter(filter);

        var distinctEntities = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Entity)
            .Where(e => e != null && e != string.Empty)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(cancellationToken);

        var distinctActions = await _context.AuditLogs
            .AsNoTracking()
            .Select(a => a.Action)
            .Where(a => a != null && a != string.Empty)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(cancellationToken);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Entity))
        {
            var ent = filter.Entity.Trim();
            query = query.Where(a => a.Entity == ent);
        }
        else if (!string.IsNullOrWhiteSpace(filter.Feature) && !string.Equals(filter.Feature, "all", StringComparison.OrdinalIgnoreCase))
        {
            var key = filter.Feature.Trim();
            var entitiesForFeature = distinctEntities
                .Where(e => string.Equals(AuditFeatureMap.FeatureKeyFor(e), key, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (entitiesForFeature.Count == 0)
            {
                query = query.Where(a => false);
            }
            else
            {
                query = query.Where(a => entitiesForFeature.Contains(a.Entity));
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var act = filter.Action.Trim();
            query = query.Where(a => a.Action == act);
        }

        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            var u = filter.User.Trim();
            query = query.Where(a =>
                (a.ChangedBy != null && EF.Functions.Like(a.ChangedBy, $"%{u}%")) ||
                (a.ChangedByEmail != null && EF.Functions.Like(a.ChangedByEmail, $"%{u}%")) ||
                (a.ChangedByUserId != null && EF.Functions.Like(a.ChangedByUserId, $"%{u}%")));
        }

        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim();
            query = query.Where(a =>
                (a.EntityReference != null && EF.Functions.Like(a.EntityReference, $"%{q}%")) ||
                EF.Functions.Like(a.EntityId, $"%{q}%") ||
                EF.Functions.Like(a.Entity, $"%{q}%") ||
                EF.Functions.Like(a.Action, $"%{q}%"));
        }

        if (filter.From.HasValue)
        {
            var from = DateTime.SpecifyKind(filter.From.Value.Date, DateTimeKind.Utc);
            query = query.Where(a => a.ChangedUtc >= from);
        }

        if (filter.To.HasValue)
        {
            var to = DateTime.SpecifyKind(filter.To.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(a => a.ChangedUtc <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(filter.PageSize <= 0 ? AuditExplorerDefaultPageSize : filter.PageSize, 10, AuditExplorerMaxPageSize);
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        var page = Math.Clamp(filter.Page <= 0 ? 1 : filter.Page, 1, totalPages);

        var rows = await query
            .OrderByDescending(a => a.ChangedUtc)
            .ThenByDescending(a => a.AuditLogId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.AuditLogId,
                a.ChangedUtc,
                a.Entity,
                a.EntityId,
                a.EntityReference,
                a.Action,
                a.ChangedBy,
                a.ChangedByEmail,
                HasBefore = a.BeforeJson != null,
                HasAfter = a.AfterJson != null,
            })
            .ToListAsync(cancellationToken);

        DateTime? mostRecent = null;
        if (rows.Count > 0)
        {
            mostRecent = rows[0].ChangedUtc;
        }

        var entityOptions = distinctEntities
            .Select(e => (Key: e, Name: AuditFeatureMap.FriendlyEntityName(e)))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vm = new AuditExplorerListVm
        {
            Filter = new AuditExplorerFilterVm
            {
                Q = filter.Q,
                Feature = filter.Feature,
                Entity = filter.Entity,
                Action = filter.Action,
                User = filter.User,
                From = filter.From,
                To = filter.To,
                Page = page,
                PageSize = pageSize,
            },
            Rows = rows.Select(r => new AuditExplorerRowVm
            {
                AuditLogId = r.AuditLogId,
                ChangedUtc = DateTime.SpecifyKind(r.ChangedUtc, DateTimeKind.Utc),
                Entity = r.Entity ?? string.Empty,
                EntityFriendly = AuditFeatureMap.FriendlyEntityName(r.Entity),
                FeatureKey = AuditFeatureMap.FeatureKeyFor(r.Entity),
                FeatureName = AuditFeatureMap.FeatureNameFor(r.Entity ?? string.Empty),
                EntityId = r.EntityId,
                EntityReference = SanitiseEntityReference(r.EntityReference, r.Entity),
                Action = r.Action ?? string.Empty,
                ActionFriendly = AuditFeatureMap.FriendlyAction(r.Action),
                ActionTagColour = AuditFeatureMap.GovUkTagColourFor(r.Action),
                ChangedBy = r.ChangedBy,
                ChangedByEmail = r.ChangedByEmail,
                HasBefore = r.HasBefore,
                HasAfter = r.HasAfter,
            }).ToList(),
            TotalRows = total,
            TotalPages = totalPages,
            Features = AuditFeatureMap.AllFeatures
                .Where(f => f.Key != AuditFeatureMap.OtherKey || distinctEntities.Any(e => AuditFeatureMap.FeatureKeyFor(e) == AuditFeatureMap.OtherKey))
                .Select(f => (f.Key, f.Name))
                .ToList(),
            Entities = entityOptions,
            Actions = distinctActions,
            MostRecentTimestampUtc = mostRecent,
        };

        return View("~/Views/Modern/Admin/AuditExplorer.cshtml", vm);
    }

    [HttpGet("audit-explorer/{auditLogId:guid}")]
    public async Task<IActionResult> AuditExplorerDetail(Guid auditLogId, CancellationToken cancellationToken)
    {
        SetAdminChrome("admin-audit-explorer");

        var entity = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AuditLogId == auditLogId, cancellationToken);
        if (entity is null) return NotFound();

        var entityName = entity.Entity ?? string.Empty;
        var diffFields = BuildDiff(entity.BeforeJson, entity.AfterJson);
        var summary = BuildPlainEnglishSummary(entity, diffFields);

        var listFilter = HttpContext.Request.Query
            .Where(kv => !string.Equals(kv.Key, "auditLogId", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value.ToString());
        var backUrl = Url.Action(nameof(AuditExplorer), "ModernAdmin", listFilter);

        var vm = new AuditExplorerDetailVm
        {
            AuditLogId = entity.AuditLogId,
            ChangedUtc = DateTime.SpecifyKind(entity.ChangedUtc, DateTimeKind.Utc),
            Entity = entityName,
            EntityFriendly = AuditFeatureMap.FriendlyEntityName(entityName),
            FeatureKey = AuditFeatureMap.FeatureKeyFor(entityName),
            FeatureName = AuditFeatureMap.FeatureNameFor(entityName),
            EntityId = entity.EntityId,
            EntityReference = SanitiseEntityReference(entity.EntityReference, entityName),
            Action = entity.Action ?? string.Empty,
            ActionFriendly = AuditFeatureMap.FriendlyAction(entity.Action),
            ActionTagColour = AuditFeatureMap.GovUkTagColourFor(entity.Action),
            ChangedBy = entity.ChangedBy,
            ChangedByEmail = entity.ChangedByEmail,
            ChangedByUserId = entity.ChangedByUserId,
            IpAddress = entity.IpAddress,
            UserAgent = entity.UserAgent,
            BeforeJson = entity.BeforeJson,
            AfterJson = entity.AfterJson,
            BeforeJsonPretty = PrettyPrintJsonOrNull(entity.BeforeJson),
            AfterJsonPretty = PrettyPrintJsonOrNull(entity.AfterJson),
            DiffFields = diffFields,
            PlainEnglishSummary = summary,
            BackUrl = backUrl,
        };

        return View("~/Views/Modern/Admin/AuditExplorerDetail.cshtml", vm);
    }

    private static AuditExplorerFilterVm NormaliseFilter(AuditExplorerFilterVm? input)
    {
        var f = input ?? new AuditExplorerFilterVm();
        f.Q = Trimmed(f.Q);
        f.Feature = Trimmed(f.Feature);
        f.Entity = Trimmed(f.Entity);
        f.Action = Trimmed(f.Action);
        f.User = Trimmed(f.User);
        if (f.From.HasValue && f.From.Value == default) f.From = null;
        if (f.To.HasValue && f.To.Value == default) f.To = null;
        if (f.From.HasValue && f.To.HasValue && f.To.Value < f.From.Value)
        {
            (f.From, f.To) = (f.To, f.From);
        }
        if (f.Page <= 0) f.Page = 1;
        if (f.PageSize <= 0) f.PageSize = AuditExplorerDefaultPageSize;
        return f;

        static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    /// <summary>
    /// The auto-audit pipeline writes <c>entry.Entity.ToString()</c> into
    /// <c>EntityReference</c>; for entities that don't override <c>ToString()</c>
    /// that comes back as <c>"Compass.Models.Project"</c>, which is noise.
    /// Drop those back to <see langword="null"/> so the view can show "—".
    /// </summary>
    private static string? SanitiseEntityReference(string? reference, string? entity)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var trimmed = reference.Trim();
        if (trimmed.StartsWith("Compass.", StringComparison.Ordinal)) return null;
        if (!string.IsNullOrWhiteSpace(entity) && string.Equals(trimmed, entity, StringComparison.Ordinal)) return null;
        return trimmed;
    }

    private static List<AuditDiffFieldVm> BuildDiff(string? beforeJson, string? afterJson)
    {
        var before = ParseTopLevelObject(beforeJson);
        var after = ParseTopLevelObject(afterJson);

        if (before is null && after is null) return new List<AuditDiffFieldVm>();

        // If one side is a non-object JSON value (string, number, raw blob) or
        // the JSON isn't an object at all, render a synthetic single-row diff
        // so the user still sees side-by-side values.
        if (before is null && !string.IsNullOrWhiteSpace(beforeJson))
        {
            before = new Dictionary<string, string?> { ["Value"] = beforeJson.Trim() };
        }
        if (after is null && !string.IsNullOrWhiteSpace(afterJson))
        {
            after = new Dictionary<string, string?> { ["Value"] = afterJson.Trim() };
        }

        before ??= new Dictionary<string, string?>();
        after ??= new Dictionary<string, string?>();

        var keys = new SortedSet<string>(before.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var k in after.Keys) keys.Add(k);

        var list = new List<AuditDiffFieldVm>(keys.Count);
        foreach (var key in keys)
        {
            var hasB = before.TryGetValue(key, out var bv);
            var hasA = after.TryGetValue(key, out var av);

            AuditDiffStatus status;
            if (hasB && !hasA) status = AuditDiffStatus.Removed;
            else if (!hasB && hasA) status = AuditDiffStatus.Added;
            else status = string.Equals(bv, av, StringComparison.Ordinal) ? AuditDiffStatus.Unchanged : AuditDiffStatus.Changed;

            list.Add(new AuditDiffFieldVm
            {
                Field = key,
                FieldFriendly = AuditFeatureMap.FriendlyFieldName(key),
                Before = bv,
                After = av,
                Status = status,
            });
        }

        // Show "real" changes first (the most useful info), then unchanged at
        // the bottom. Alphabetical within each group.
        return list
            .OrderBy(f => f.Status switch
            {
                AuditDiffStatus.Changed => 0,
                AuditDiffStatus.Added => 1,
                AuditDiffStatus.Removed => 2,
                _ => 3,
            })
            .ThenBy(f => f.FieldFriendly, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, string?>? ParseTopLevelObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = JsonElementToDisplayString(prop.Value);
            }
            return dict;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? JsonElementToDisplayString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.Undefined => null,
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Object => el.GetRawText(),
        JsonValueKind.Array => el.GetRawText(),
        _ => el.GetRawText(),
    };

    private static string? PrettyPrintJsonOrNull(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, _auditDiffJsonOptions);
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static string BuildPlainEnglishSummary(AuditLog log, IReadOnlyList<AuditDiffFieldVm> diff)
    {
        var who = !string.IsNullOrWhiteSpace(log.ChangedBy)
            ? log.ChangedBy
            : (!string.IsNullOrWhiteSpace(log.ChangedByEmail) ? log.ChangedByEmail : "An unknown user");
        var verb = (log.Action ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "create" or "created" or "added" or "add" => "created",
            "update" or "updated" or "modified" or "modify" or "edit" => "updated",
            "delete" or "deleted" or "remove" or "removed" => "deleted",
            "editstarted" => "started editing",
            "edit started" => "started editing",
            "editdiscarded" => "discarded changes to",
            "edit discarded" => "discarded changes to",
            "submitted" or "submit" => "submitted",
            "approved" or "approve" => "approved",
            "rejected" or "reject" => "rejected",
            "published" or "publish" => "published",
            "unpublished" or "unpublish" => "unpublished",
            _ => string.IsNullOrWhiteSpace(log.Action) ? "changed" : log.Action.ToLowerInvariant(),
        };
        var what = AuditFeatureMap.FriendlyEntityName(log.Entity);
        if (string.IsNullOrWhiteSpace(what)) what = "an item";
        else what = what.ToLowerInvariant();
        var reference = !string.IsNullOrWhiteSpace(log.EntityReference) && !log.EntityReference.StartsWith("Compass.", StringComparison.Ordinal)
            ? $" \"{log.EntityReference}\""
            : !string.IsNullOrWhiteSpace(log.EntityId) ? $" (id {log.EntityId})" : string.Empty;

        var sb = new StringBuilder();
        sb.Append(who).Append(' ').Append(verb).Append(' ').Append(what).Append(reference).Append('.');

        var changed = diff.Where(d => d.Status == AuditDiffStatus.Changed).Take(4).ToList();
        if (changed.Count > 0)
        {
            sb.Append(' ').Append(changed.Count == 1 ? "Changed field: " : "Changed fields: ");
            sb.Append(string.Join(", ", changed.Select(c => c.FieldFriendly)));
            sb.Append('.');
        }

        return sb.ToString();
    }
}
