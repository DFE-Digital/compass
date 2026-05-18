using Compass.Models;

namespace Compass.Services;

/// <summary>Resolves RAG shown for a monthly update row (draft snapshot or history at submit).</summary>
public static class MonthlyUpdateRagResolver
{
    private static readonly TimeSpan SoonAfterSubmitWindow = TimeSpan.FromMinutes(5);

    public readonly record struct MonthlyUpdateRag(int? StatusId, string? Name, string? CssClass);

    public static MonthlyUpdateRag Resolve(
        ProjectMonthlyUpdate mu,
        IReadOnlyList<ProjectRagHistory> historyDesc,
        Project? project = null,
        IReadOnlyDictionary<int, RagStatusLookup>? lookupById = null)
    {
        if (mu.DraftRagStatusLookupId is int draftId && draftId > 0)
        {
            if (mu.DraftRagStatusLookup is { } draft && !string.IsNullOrWhiteSpace(draft.Name))
                return new(draftId, draft.Name.Trim(), draft.CssClass);
            if (lookupById != null && lookupById.TryGetValue(draftId, out var draftLookup))
                return new(draftId, draftLookup.Name, draftLookup.CssClass);
            return new(draftId, null, null);
        }

        if (!mu.SubmittedAt.HasValue)
            return new(null, null, null);

        var submittedAt = mu.SubmittedAt.Value;

        var atSubmit = MonthlyUpdateSubmittedRagResolver.Resolve(historyDesc, submittedAt);
        if (atSubmit != null)
            return FromHistoryRow(atSubmit, lookupById);

        var soonAfter = historyDesc
            .OrderBy(r => r.ChangedAt)
            .ThenBy(r => r.Id)
            .FirstOrDefault(r =>
                r.ChangedAt > submittedAt &&
                r.ChangedAt <= submittedAt + SoonAfterSubmitWindow);
        if (soonAfter != null)
            return FromHistoryRow(soonAfter, lookupById);

        var periodStart = new DateTime(mu.Year, mu.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var inPeriod = historyDesc
            .Where(r => r.ChangedAt >= periodStart && r.ChangedAt < periodEnd)
            .OrderByDescending(r => r.ChangedAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefault();
        if (inPeriod != null)
            return FromHistoryRow(inPeriod, lookupById);

        if (project != null)
        {
            if (project.RagStatusLookup is { } pl && !string.IsNullOrWhiteSpace(pl.Name))
                return new(project.RagStatusLookupId, pl.Name.Trim(), pl.CssClass);

#pragma warning disable CS0618
            if (!string.IsNullOrWhiteSpace(project.RagStatus))
                return ResolveNameToRag(project.RagStatus.Trim(), lookupById);
#pragma warning restore CS0618
        }

        return new(null, null, null);
    }

    public static void ApplyDisplay(
        MonthlyUpdateRag rag,
        IReadOnlyDictionary<int, RagStatusLookup> lookupById,
        Action<int?, string?, string?> apply)
    {
        var statusId = rag.StatusId;
        var name = rag.Name;
        var cssClass = rag.CssClass;

        if (string.IsNullOrWhiteSpace(name) && statusId is int id && id > 0 && lookupById.TryGetValue(id, out var lookup))
        {
            name = lookup.Name;
            cssClass ??= lookup.CssClass;
        }

        if (statusId == null && !string.IsNullOrWhiteSpace(name))
        {
            var match = lookupById.Values.FirstOrDefault(r =>
                string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                statusId = match.Id;
                cssClass ??= match.CssClass;
                name = match.Name;
            }
        }

        apply(statusId, string.IsNullOrWhiteSpace(name) ? null : name.Trim(), cssClass);
    }

    private static MonthlyUpdateRag FromHistoryRow(
        ProjectRagHistory row,
        IReadOnlyDictionary<int, RagStatusLookup>? lookupById)
    {
        if (row.RagStatusLookup is { } lookup && !string.IsNullOrWhiteSpace(lookup.Name))
            return new(row.RagStatusLookupId, lookup.Name.Trim(), lookup.CssClass);

        if (row.RagStatusLookupId is int histId && histId > 0)
        {
            if (lookupById != null && lookupById.TryGetValue(histId, out var histLookup))
                return new(histId, histLookup.Name, histLookup.CssClass);
            return new(histId, null, null);
        }

#pragma warning disable CS0618
        if (!string.IsNullOrWhiteSpace(row.RagStatus))
            return ResolveNameToRag(row.RagStatus.Trim(), lookupById);
#pragma warning restore CS0618

        return new(null, null, null);
    }

    private static MonthlyUpdateRag ResolveNameToRag(
        string name,
        IReadOnlyDictionary<int, RagStatusLookup>? lookupById)
    {
        if (lookupById != null)
        {
            var match = lookupById.Values.FirstOrDefault(r =>
                string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return new(match.Id, match.Name, match.CssClass);
        }

        return new(null, name, null);
    }
}
