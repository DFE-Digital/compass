using System.Globalization;
using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

/// <summary>Builds previous-month review comment and plain-English change summary for RAID review work.</summary>
public static class RaidMonthlyReviewWorkTimelineBuilder
{
    public static async Task<RaidReviewPreviousMonthViewModel> BuildAsync(
        CompassDbContext db,
        string recordType,
        int recordId,
        int currentReviewYear,
        int currentReviewMonth,
        CancellationToken cancellationToken = default)
    {
        var previousPeriodStart = new DateTime(currentReviewYear, currentReviewMonth, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-1);
        var prevYear = previousPeriodStart.Year;
        var prevMonth = previousPeriodStart.Month;
        var prevLabel = previousPeriodStart.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-GB"));

        var review = await db.RaidMonthlyReviews.AsNoTracking()
            .Where(r => r.RecordType == recordType
                        && r.RecordId == recordId
                        && r.ReviewYear == prevYear
                        && r.ReviewMonth == prevMonth)
            .Include(r => r.ReviewedByUser)
            .FirstOrDefaultAsync(cancellationToken);

        var sinceUtc = review?.ReviewedAtUtc
                       ?? new DateTime(prevYear, prevMonth, 1, 0, 0, 0, DateTimeKind.Utc);

        string? changes = null;
        if (string.Equals(recordType, "risk", StringComparison.OrdinalIgnoreCase))
        {
            changes = await RiskAuditTimelineBuilder.BuildPlainEnglishSummarySinceAsync(
                db, recordId, sinceUtc, cancellationToken);
        }
        else if (string.Equals(recordType, "issue", StringComparison.OrdinalIgnoreCase))
        {
            changes = await BuildIssuePlainEnglishSummarySinceAsync(
                db, recordId, sinceUtc, cancellationToken);
        }

        return new RaidReviewPreviousMonthViewModel
        {
            PreviousMonthLabel = prevLabel,
            HadReview = review != null,
            ReviewedAtUtc = review?.ReviewedAtUtc,
            ReviewerDisplay = review?.ReviewedByUser?.Name
                              ?? review?.ReviewedByUser?.Email,
            MonthlyComment = string.IsNullOrWhiteSpace(review?.MonthlyComment)
                ? null
                : review!.MonthlyComment!.Trim(),
            ChangesSincePreviousReview = changes
        };
    }

    private static async Task<string?> BuildIssuePlainEnglishSummarySinceAsync(
        CompassDbContext db,
        int issueId,
        DateTime sinceUtc,
        CancellationToken cancellationToken)
    {
        var issueIdStr = issueId.ToString();
        var logs = await db.AuditLogs.AsNoTracking()
            .Where(a => a.Entity == nameof(Issue) && a.EntityId == issueIdStr && a.Action == "Update" && a.ChangedUtc > sinceUtc)
            .OrderBy(a => a.ChangedUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
            return null;

        var themes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var log in logs)
        {
            var before = ParseJsonDict(log.BeforeJson);
            var after = ParseJsonDict(log.AfterJson);
            foreach (var key in before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(key, "UpdatedAt", StringComparison.OrdinalIgnoreCase))
                    continue;
                before.TryGetValue(key, out var b);
                after.TryGetValue(key, out var a);
                if (ElementToString(b) == ElementToString(a))
                    continue;

                themes.Add(key switch
                {
                    "Title" or "Description" or "RootCause" => "text",
                    "IssueStatusId" or "Status" => "status",
                    "SeverityId" or "PriorityId" => "severity",
                    "ProjectId" or "PrimaryProductId" => "link",
                    "OwnerUserId" => "owner",
                    "DueDate" or "IdentifiedDate" => "dates",
                    _ => "other"
                });
            }
        }

        var sentences = new List<string>();
        if (themes.Contains("status"))
            sentences.Add("The issue status was updated.");
        if (themes.Contains("severity"))
            sentences.Add("Severity or priority was changed.");
        if (themes.Contains("text"))
            sentences.Add("The description or title was updated.");
        if (themes.Contains("link"))
            sentences.Add("Which work item or product this is linked to was changed.");
        if (themes.Contains("owner"))
            sentences.Add("The owner was changed.");
        if (themes.Contains("dates"))
            sentences.Add("Due or identification dates were updated.");
        if (themes.Contains("other") && sentences.Count == 0)
            sentences.Add("Some details on the issue were updated.");

        return sentences.Count == 0 ? null : string.Join(" ", sentences.Take(4));
    }

    private static Dictionary<string, JsonElement> ParseJsonDict(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                   ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? ElementToString(JsonElement? el)
    {
        if (el == null || el.Value.ValueKind == JsonValueKind.Null)
            return null;
        return el.Value.ValueKind switch
        {
            JsonValueKind.String => el.Value.GetString(),
            JsonValueKind.Number => el.Value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => el.Value.GetRawText()
        };
    }
}
