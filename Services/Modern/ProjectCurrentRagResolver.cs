using Compass.Models;

#pragma warning disable CS0618 // Legacy RagStatus string fields on Project / ProjectRagHistory

namespace Compass.Services.Modern;

/// <summary>Resolves the current RAG for a project from lookup, legacy fields, and history.</summary>
internal static class ProjectCurrentRagResolver
{
    internal readonly record struct ProjectCurrentRag(string? Name, string? CssClass, int? StatusId);

    internal static ProjectCurrentRag Resolve(Project p)
    {
        if (p.RagStatusLookup is { } lookup && !string.IsNullOrWhiteSpace(lookup.Name))
            return new(lookup.Name.Trim(), lookup.CssClass, p.RagStatusLookupId);

        if (!string.IsNullOrWhiteSpace(p.RagStatus))
            return new(p.RagStatus.Trim(), null, p.RagStatusLookupId);

        var latestHistory = p.RagHistory?
            .OrderByDescending(r => r.ChangedAt)
            .ThenByDescending(r => r.Id)
            .FirstOrDefault();

        if (latestHistory?.RagStatusLookup is { } histLookup && !string.IsNullOrWhiteSpace(histLookup.Name))
            return new(histLookup.Name.Trim(), histLookup.CssClass, latestHistory.RagStatusLookupId);

        if (!string.IsNullOrWhiteSpace(latestHistory?.RagStatus))
            return new(latestHistory.RagStatus.Trim(), null, latestHistory.RagStatusLookupId);

        var latestSubmittedMu = p.MonthlyUpdates?
            .Where(m => m.SubmittedAt.HasValue && m.DraftRagStatusLookupId is > 0)
            .OrderByDescending(m => m.Year)
            .ThenByDescending(m => m.Month)
            .FirstOrDefault();

        if (latestSubmittedMu?.DraftRagStatusLookup is { } muLookup
            && !string.IsNullOrWhiteSpace(muLookup.Name))
            return new(muLookup.Name.Trim(), muLookup.CssClass, latestSubmittedMu.DraftRagStatusLookupId);

        return new(null, null, null);
    }

#pragma warning restore CS0618

    internal static RagStatus? ToRagStatus(ProjectCurrentRag current)
    {
        if (string.IsNullOrWhiteSpace(current.Name))
            return null;

        return new RagStatus
        {
            Id = current.StatusId ?? 0,
            Name = current.Name,
            CssClass = current.CssClass
        };
    }
}
