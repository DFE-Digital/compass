using System.Globalization;
using Compass.ViewModels.Modern;

namespace Compass.Services;

/// <summary>
/// Builds per–Service Standard (GSS) action rows by tagging each action to its published
/// <em>Service assessment’s</em> overall Red/Amber/Green outcome, so the report can show
/// what share of action volume is tied to weaker overall outcomes.
/// </summary>
public static class ServiceAssessmentStandardActionOutcomeBuilder
{
    private const string ServiceAssessmentType = "Service assessment";

    /// <param name="actionsByStandardApi">Aggregate <c>GET …/actions-by-standard</c> (fallback + assessment counts).</param>
    /// <param name="allAssessmentsWithActions">Detailed <c>GET …/assessments/actions/all</c>.</param>
    /// <param name="published">From published summary, same cohort as the rest of this page.</param>
    public static (IReadOnlyList<SasStandardActionRow> Rows, bool OutcomeBreakdownAvailable) Build(
        SasActionsByStandardResponse? actionsByStandardApi,
        ServiceAssessmentResponse? allAssessmentsWithActions,
        IReadOnlyList<SasPublishedAssessmentRow>? published)
    {
        var fromDetailed = BuildFromActionsApiAndPublished(
            allAssessmentsWithActions,
            published,
            actionsByStandardApi);
        if (fromDetailed is { Count: > 0 } ok)
        {
            return (ok, true);
        }

        if (actionsByStandardApi?.ActionsByStandard is { Count: > 0 } apiRows)
        {
            return (MapFromApiOnly(apiRows), false);
        }

        return (Array.Empty<SasStandardActionRow>(), false);
    }

    private static List<SasStandardActionRow>? BuildFromActionsApiAndPublished(
        ServiceAssessmentResponse? allAssessmentsWithActions,
        IReadOnlyList<SasPublishedAssessmentRow>? published,
        SasActionsByStandardResponse? actionsByStandardApi)
    {
        var saPublished = (published ?? Array.Empty<SasPublishedAssessmentRow>())
            .Where(
                r => string.Equals(
                    (r.Type ?? string.Empty).Trim(),
                    ServiceAssessmentType,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (saPublished.Count == 0
            || allAssessmentsWithActions?.Assessments is not { Count: > 0 } fromApi)
        {
            return null;
        }

        var outcomeById = new Dictionary<int, string>(saPublished.Count);
        foreach (var p in saPublished)
        {
            if (!outcomeById.ContainsKey(p.AssessmentID))
            {
                outcomeById[p.AssessmentID] = p.Outcome?.Trim() ?? "—";
            }
        }

        var publishedSet = new HashSet<int>(outcomeById.Keys);
        var totals = new Dictionary<int, (int R, int A, int G, int O)>();
        var idSets = new Dictionary<int, HashSet<int>>();

        foreach (var assessment in fromApi)
        {
            if (!publishedSet.Contains(assessment.AssessmentID)
                || !outcomeById.TryGetValue(assessment.AssessmentID, out var fromSummary)
                || assessment.ActionsByStandard is not { Count: > 0 } absList)
            {
                continue;
            }

            var fromDetail = string.IsNullOrEmpty(assessment.AssessmentOutcome)
                ? null
                : assessment.AssessmentOutcome.Trim();
            var band = ClassifyRag(fromSummary, fromDetail);

            foreach (var block in absList)
            {
                if (block.Actions is not { Count: > 0 } actionsList)
                {
                    continue;
                }

                var count = actionsList.Count;
                if (!totals.ContainsKey(block.Standard))
                {
                    totals[block.Standard] = default;
                    idSets[block.Standard] = new HashSet<int>();
                }

                idSets[block.Standard]!.Add(assessment.AssessmentID);
                var t = totals[block.Standard]!;
                totals[block.Standard] = band switch
                {
                    1 => (t.R + count, t.A, t.G, t.O), // R
                    2 => (t.R, t.A + count, t.G, t.O), // A
                    3 => (t.R, t.A, t.G + count, t.O), // G
                    _ => (t.R, t.A, t.G, t.O + count)
                };
            }
        }

        if (totals.Count == 0)
        {
            return null;
        }

        var apiAssessmentCount = new Dictionary<int, int>();
        if (actionsByStandardApi?.ActionsByStandard is { } apiList)
        {
            foreach (var a in apiList)
            {
                if (int.TryParse(a.AssessmentCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var c))
                {
                    apiAssessmentCount[a.Standard] = c;
                }
            }
        }

        var list = new List<SasStandardActionRow>();
        foreach (var (std, (r, a, g, o)) in totals)
        {
            var total = r + a + g + o;
            var distinctAssess = idSets.TryGetValue(std, out var hs) ? hs.Count : 0;
            if (!apiAssessmentCount.TryGetValue(std, out var apiEc))
            {
                apiEc = distinctAssess;
            }

            list.Add(
                new SasStandardActionRow
                {
                    Standard = std,
                    ActionCount = total,
                    AssessmentCount = Math.Max(apiEc, distinctAssess),
                    ActionsFromRedOutcome = r,
                    ActionsFromAmberOutcome = a,
                    ActionsFromGreenOutcome = g,
                    ActionsFromOtherOutcome = o,
                    PctOnAmberOrRed = total > 0 ? 100.0 * (r + a) / total : null
                });
        }

        return SortByProblematicness(list);
    }

    private static IReadOnlyList<SasStandardActionRow> MapFromApiOnly(
        IReadOnlyList<SasActionByStandardRow> apiRows)
    {
        var ordered = apiRows
            .Select(
                r => new SasStandardActionRow
                {
                    Standard = r.Standard,
                    ActionCount = int.TryParse(
                        r.ActionCount,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var ac)
                        ? ac
                        : 0,
                    AssessmentCount = int.TryParse(
                        r.AssessmentCount,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var ec)
                        ? ec
                        : 0
                })
            .OrderBy(r => r.Standard)
            .ToList();
        var n = 1;
        foreach (var row in ordered)
        {
            row.MostProblematicRank = n++;
        }

        return ordered;
    }

    private static List<SasStandardActionRow> SortByProblematicness(
        IReadOnlyList<SasStandardActionRow> list)
    {
        var ordered = list
            .OrderByDescending(r => r.PctOnAmberOrRed is { } p ? p : -1.0)
            .ThenByDescending(r => r.ActionsFromRedOutcome + r.ActionsFromAmberOutcome)
            .ThenByDescending(r => r.ActionCount)
            .ThenBy(r => r.Standard, Comparer<int>.Default)
            .ToList();
        var n = 1;
        foreach (var row in ordered)
        {
            row.MostProblematicRank = n++;
        }

        return ordered;
    }

    /// <summary>1=Red, 2=Amber, 3=Green, 0=unclassified. Prefer a clear RAG from the published list, then the detail API string.</summary>
    private static int ClassifyRag(string publishedSummary, string? apiOutcome)
    {
        var a = RagFromString(publishedSummary);
        if (a is 1 or 2 or 3)
        {
            return a.Value;
        }

        var b = RagFromString(apiOutcome);
        if (b is 1 or 2 or 3)
        {
            return b.Value;
        }

        return a ?? b ?? 0;
    }

    /// <summary>
    /// <see langword="null"/> = missing so caller may try a fallback string;
    /// 0 = not mapped to a RAG colour; 1=Red, 2=Amber, 3=Green.
    /// Amber (including "Amber-Red") is checked before Red.
    /// </summary>
    private static int? RagFromString(string? raw)
    {
        var t = (raw ?? string.Empty).Trim();
        if (t.Length == 0
            || t.Equals("—", StringComparison.Ordinal)
            || t.Equals("–", StringComparison.Ordinal))
        {
            return null;
        }

        var l = t.ToLowerInvariant();
        if (l.Contains("amber", StringComparison.Ordinal) || l.Equals("yellow", StringComparison.Ordinal))
        {
            return 2;
        }

        if (l.Contains("green", StringComparison.Ordinal) || l.Equals("met", StringComparison.Ordinal)
            || l.Contains("rag green", StringComparison.Ordinal))
        {
            return 3;
        }

        if (l.Contains("not met", StringComparison.Ordinal) || l.Contains("not pass", StringComparison.Ordinal))
        {
            return 1;
        }

        if (l.Contains("red", StringComparison.Ordinal) || l.Contains("rag red", StringComparison.Ordinal))
        {
            return 1;
        }

        if (l.Equals("pass", StringComparison.Ordinal) || l.Contains("outcome met", StringComparison.Ordinal))
        {
            return 3;
        }

        // Present but we did not map it — count as "other" bucket, not a signal to look elsewhere
        return 0;
    }
}

