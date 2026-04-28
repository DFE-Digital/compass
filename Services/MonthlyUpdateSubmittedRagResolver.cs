using Compass.Models;

namespace Compass.Services;

/// <summary>
/// Finds the <see cref="ProjectRagHistory"/> row that matches a monthly update submission.
/// Uses "at or before" <paramref name="submittedAt"/>, then a short "just after" window when DB timestamps
/// order slightly differently than <c>ChangedAt &lt;= SubmittedAt</c>.
/// </summary>
public static class MonthlyUpdateSubmittedRagResolver
{
    private static readonly TimeSpan SoonAfterSubmitWindow = TimeSpan.FromSeconds(30);

    /// <param name="historyDesc">RAG history for the project, newest first.</param>
    public static ProjectRagHistory? Resolve(IReadOnlyList<ProjectRagHistory> historyDesc, DateTime submittedAt)
    {
        if (historyDesc.Count == 0)
            return null;

        var atOrBefore = historyDesc.FirstOrDefault(r => r.ChangedAt <= submittedAt);
        if (atOrBefore != null)
            return atOrBefore;

        return historyDesc
            .OrderBy(r => r.ChangedAt)
            .ThenBy(r => r.Id)
            .FirstOrDefault(r =>
                r.ChangedAt > submittedAt &&
                r.ChangedAt <= submittedAt + SoonAfterSubmitWindow);
    }
}
