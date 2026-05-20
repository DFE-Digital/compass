using Compass.Models;
using Compass.ViewModels;

namespace Compass.Services;

/// <summary>Six-month RAG trend classification for the monthly report.</summary>
public static class MonthlyReportRagTrendAnalyzer
{
    public const string TrendStable = "Stable";
    public const string TrendStale = "Stale";
    public const string TrendImproving = "Improving";
    public const string TrendWorsening = "Worsening";

    public static readonly string[] TrendCategories = [TrendStable, TrendImproving, TrendWorsening, TrendStale];

    public static List<WorkItemRagSixMonthTrendRow> Build(
        IReadOnlyList<Project> projects,
        Dictionary<int, List<ProjectRagHistory>> historyByProject,
        int reportYear,
        int reportMonth,
        Func<Project, DateTime, Dictionary<int, List<ProjectRagHistory>>, string> resolveRagAtCutoff)
    {
        var reportMonthStart = new DateTime(reportYear, reportMonth, 1);
        var monthStarts = Enumerable.Range(-5, 6)
            .Select(i => reportMonthStart.AddMonths(i))
            .ToList();

        var monthLabels = monthStarts
            .Select(d => d.ToString("MMM"))
            .ToList();

        var cutoffs = monthStarts
            .Select(d => d.AddMonths(1))
            .ToList();

        var rows = new List<WorkItemRagSixMonthTrendRow>();

        foreach (var project in projects)
        {
            var buckets = cutoffs
                .Select(c => BucketRag(resolveRagAtCutoff(project, c, historyByProject)))
                .ToList();

            var monthSnapshots = buckets
                .Select((rag, idx) => new RagSixMonthSnapshot
                {
                    Label = monthLabels[idx],
                    Rag = rag
                })
                .ToList();

            rows.Add(new WorkItemRagSixMonthTrendRow
            {
                ProjectId = project.Id,
                Title = project.Title,
                BusinessArea = project.BusinessAreaLookup?.Name,
                TrendCategory = ClassifyTrend(buckets),
                Months = monthSnapshots
            });
        }

        return rows.OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string RagAbbreviation(string rag) => rag switch
    {
        "Red" => "R",
        "Amber-Red" => "A-R",
        "Amber-Green" => "A-G",
        "Green" => "G",
        _ => "—"
    };

    /// <summary>Maps a resolved RAG name to report buckets (matches <see cref="ModernMonthlyReportService"/> distribution).</summary>
    public static string BucketRag(string? ragStatus)
    {
        if (string.IsNullOrWhiteSpace(ragStatus))
            return "Not Set";
        if (string.Equals(ragStatus, "Amber", StringComparison.OrdinalIgnoreCase))
            return "Not Set";
        return ragStatus is "Red" or "Amber-Red" or "Amber-Green" or "Green" ? ragStatus : "Not Set";
    }

    public static string ClassifyTrend(IReadOnlyList<string> monthBuckets)
    {
        if (monthBuckets.Count == 0)
            return TrendStale;

        if (monthBuckets.Any(b => b == "Not Set"))
            return TrendStale;

        if (monthBuckets.All(b => b == "Green"))
            return TrendStable;

        if (monthBuckets.Distinct(StringComparer.Ordinal).Count() == 1)
            return TrendStale;

        var improving = 0;
        var worsening = 0;

        for (var i = 0; i < monthBuckets.Count - 1; i++)
        {
            var fromRank = RagTrendRank(monthBuckets[i]);
            var toRank = RagTrendRank(monthBuckets[i + 1]);
            if (fromRank < 0 || toRank < 0)
                return TrendStale;
            if (toRank > fromRank)
                improving++;
            else if (toRank < fromRank)
                worsening++;
        }

        if (improving > worsening)
            return TrendImproving;
        if (worsening > improving)
            return TrendWorsening;

        return TrendStable;
    }

    /// <summary>Higher rank = closer to Green (Red = 0, Green = 3).</summary>
    public static int RagTrendRank(string rag) => rag switch
    {
        "Red" => 0,
        "Amber-Red" => 1,
        "Amber-Green" => 2,
        "Green" => 3,
        _ => -1
    };
}
