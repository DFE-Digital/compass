using System.Globalization;
using System.Text.RegularExpressions;
using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

/// <summary>
/// Builds a unified timeline of risk comments and mitigation progress updates (stored in action notes).
/// </summary>
public static class RiskCommentTimelineBuilder
{
    private static readonly Regex AuditLineRegex = new(
        @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}) UTC — (.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static int CountMitigationUpdateLines(string? notes) =>
        ParseMitigationUpdateLines(notes).Count;

    public static IReadOnlyList<(DateTime WhenUtc, string Text)> ParseMitigationUpdateLines(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return Array.Empty<(DateTime, string)>();

        var results = new List<(DateTime, string)>();
        foreach (var rawLine in notes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = AuditLineRegex.Match(rawLine);
            if (match.Success
                && DateTime.TryParseExact(
                    match.Groups[1].Value,
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var when))
            {
                results.Add((when, match.Groups[2].Value.Trim()));
                continue;
            }

            // Legacy or truncated notes: treat whole line as text with unknown date.
            if (!string.IsNullOrWhiteSpace(rawLine))
                results.Add((DateTime.MinValue, rawLine));
        }

        return results;
    }

    public static async Task<List<object>> BuildTimelineJsonAsync(
        CompassDbContext db,
        int riskId,
        CancellationToken cancellationToken = default)
    {
        var comments = await db.Comments.AsNoTracking()
            .Include(c => c.CreatedByUser)
            .Where(c => c.EntityType == "Risk" && c.EntityId == riskId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.CommentText,
                c.CreatedAt,
                CreatedByUser = c.CreatedByUser != null
                    ? new { c.CreatedByUser.Id, c.CreatedByUser.Name, c.CreatedByUser.Email }
                    : null
            })
            .ToListAsync(cancellationToken);

        var mitigations = await db.RiskActions.AsNoTracking()
            .Include(ra => ra.Action)
            .Where(ra => ra.RiskId == riskId && ra.Action != null && !ra.Action.IsDeleted)
            .Select(ra => new { ra.ActionId, ra.Action!.Title, ra.Action.Notes })
            .ToListAsync(cancellationToken);

        var items = new List<(DateTime SortAt, object Payload)>();

        foreach (var c in comments)
        {
            items.Add((c.CreatedAt, new
            {
                kind = "comment",
                id = c.Id,
                commentText = c.CommentText,
                createdAt = c.CreatedAt,
                createdByUser = c.CreatedByUser
            }));
        }

        foreach (var m in mitigations)
        {
            var lineIndex = 0;
            foreach (var (when, text) in ParseMitigationUpdateLines(m.Notes))
            {
                var sortAt = when == DateTime.MinValue ? DateTime.MinValue : when;
                items.Add((sortAt, new
                {
                    kind = "mitigation-update",
                    id = $"m-{m.ActionId}-{lineIndex}",
                    commentText = text,
                    createdAt = when == DateTime.MinValue ? (DateTime?)null : when,
                    mitigationId = m.ActionId,
                    mitigationTitle = m.Title
                }));
                lineIndex++;
            }
        }

        return items
            .OrderByDescending(x => x.SortAt)
            .Select(x => x.Payload)
            .ToList();
    }

    public static async Task<int> CountTimelineItemsAsync(
        CompassDbContext db,
        int riskId,
        CancellationToken cancellationToken = default)
    {
        var commentCount = await db.Comments.AsNoTracking()
            .CountAsync(c => c.EntityType == "Risk" && c.EntityId == riskId && !c.IsDeleted, cancellationToken);

        var mitigationNotes = await db.RiskActions.AsNoTracking()
            .Where(ra => ra.RiskId == riskId && ra.Action != null && !ra.Action.IsDeleted)
            .Select(ra => ra.Action!.Notes)
            .ToListAsync(cancellationToken);

        var mitigationLineCount = mitigationNotes.Sum(CountMitigationUpdateLines);
        return commentCount + mitigationLineCount;
    }
}
