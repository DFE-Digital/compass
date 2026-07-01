using System.Globalization;
using System.Text.RegularExpressions;
using Compass.ViewModels.Modern;

namespace Compass.Services;

/// <summary>Standards-level analysis for published Service assessments (GSS points 1–14).</summary>
public static class ServiceAssessmentStandardsAnalyticsBuilder
{
    private const string ServiceAssessmentType = "Service assessment";
    private const int ComparisonMonths = 6;
    private const int FinancialQuarterCount = 6;
    private static readonly Regex WhitespaceCollapse = new(@"\s+", RegexOptions.Compiled);

    public static SasStandardsAnalysisVm Build(
        ServiceAssessmentResponse? allAssessmentsWithActions,
        IReadOnlyList<SasPublishedAssessmentRow>? published,
        IReadOnlyList<SasStandardActionRow> standardRows,
        bool outcomeBreakdownAvailable)
    {
        var vm = new SasStandardsAnalysisVm
        {
            OutcomeBreakdownAvailable = outcomeBreakdownAvailable,
            TrendComparisonMonths = ComparisonMonths
        };

        var saPublished = (published ?? Array.Empty<SasPublishedAssessmentRow>())
            .Where(r => string.Equals((r.Type ?? string.Empty).Trim(), ServiceAssessmentType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (saPublished.Count == 0 || allAssessmentsWithActions?.Assessments is not { Count: > 0 } assessments)
        {
            return vm;
        }

        var publishedIds = new HashSet<int>(saPublished.Select(p => p.AssessmentID));
        var outcomeById = saPublished.ToDictionary(p => p.AssessmentID, p => p.Outcome?.Trim() ?? "—");

        var actionsByStandard = new Dictionary<int, List<(ActionItem Action, string Outcome, int AssessmentId, string? AssessmentName)>>();

        foreach (var assessment in assessments)
        {
            if (!publishedIds.Contains(assessment.AssessmentID) || assessment.ActionsByStandard is not { Count: > 0 } blocks)
            {
                continue;
            }

            var outcome = outcomeById.GetValueOrDefault(assessment.AssessmentID, assessment.AssessmentOutcome?.Trim() ?? "—");
            var name = assessment.AssessmentName ?? $"Assessment {assessment.AssessmentID}";

            foreach (var block in blocks)
            {
                if (block.Actions is not { Count: > 0 } list)
                {
                    continue;
                }

                if (!actionsByStandard.ContainsKey(block.Standard))
                {
                    actionsByStandard[block.Standard] = new List<(ActionItem, string, int, string?)>();
                }

                foreach (var action in list)
                {
                    actionsByStandard[block.Standard].Add((action, outcome, assessment.AssessmentID, name));
                }
            }
        }

        if (actionsByStandard.Count == 0)
        {
            return vm;
        }

        var utcNow = DateTime.UtcNow;
        var recentCutoff = utcNow.AddMonths(-ComparisonMonths);
        var priorCutoff = utcNow.AddMonths(-ComparisonMonths * 2);
        var trendPeriods = GetLastFinancialQuarters(utcNow, FinancialQuarterCount);

        var standardPoints = new List<SasStandardAnalysisPointVm>();

        foreach (var std in Enumerable.Range(1, 14))
        {
            if (!actionsByStandard.TryGetValue(std, out var actions) || actions.Count == 0)
            {
                standardPoints.Add(new SasStandardAnalysisPointVm
                {
                    Standard = std,
                    StandardTitle = $"Point {std}",
                    ActionCount = 0
                });
                continue;
            }

            var title = actions.FirstOrDefault().Action.StandardTitle;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = $"Point {std}";
            }

            var recentActions = actions.Where(a => a.Action.Created is { } d && d >= recentCutoff).ToList();
            var priorActions = actions.Where(a => a.Action.Created is { } d && d >= priorCutoff && d < recentCutoff).ToList();
            var withoutDate = actions.Count(a => a.Action.Created is null);

            var recentCount = recentActions.Count;
            var priorCount = priorActions.Count;
            var recentAssessmentCount = recentActions.Select(a => a.AssessmentId).Distinct().Count();
            var priorAssessmentCount = priorActions.Select(a => a.AssessmentId).Distinct().Count();

            var trend = ClassifyProportionateTrend(recentCount, priorCount, recentAssessmentCount, priorAssessmentCount);

            var byPeriod = trendPeriods.ToDictionary(
                p => p.Label,
                p => actions.Count(a =>
                    a.Action.Created is { } d &&
                    string.Equals(GetUkFinancialQuarterKey(d), p.Key, StringComparison.Ordinal)),
                StringComparer.OrdinalIgnoreCase);

            var byYear = actions
                .Where(a => a.Action.Created.HasValue)
                .GroupBy(a => a.Action.Created!.Value.Year)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key.ToString(CultureInfo.InvariantCulture), g => g.Count());

            var byStatus = actions
                .GroupBy(a => string.IsNullOrWhiteSpace(a.Action.Status) ? "Unknown" : a.Action.Status!.Trim(), StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                .ToList();

            var byOutcome = actions
                .GroupBy(a => ClassifyOutcomeLabel(a.Outcome), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var repeated = actions
                .Where(a => !string.IsNullOrWhiteSpace(a.Action.Comments))
                .GroupBy(a => NormalizeCommentKey(a.Action.Comments!))
                .Where(g => g.Count() > 1)
                .Select(g =>
                {
                    var sample = g.First().Action.Comments!.Trim();
                    return new SasRepeatedActionThemeVm
                    {
                        Snippet = Truncate(sample, 120),
                        FullText = sample.Length > 500 ? sample[..500] + "…" : sample,
                        OccurrenceCount = g.Count(),
                        AssessmentCount = g.Select(x => x.AssessmentId).Distinct().Count(),
                        Standard = std
                    };
                })
                .OrderByDescending(x => x.OccurrenceCount)
                .Take(8)
                .ToList();

            var actionDetails = actions
                .OrderBy(a => a.AssessmentName, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(a => a.Action.Created ?? DateTime.MinValue)
                .Select(a => new SasStandardActionDetailVm
                {
                    AssessmentId = a.AssessmentId,
                    AssessmentName = a.AssessmentName ?? $"Assessment {a.AssessmentId}",
                    Outcome = a.Outcome,
                    Status = a.Action.Status,
                    ActionId = a.Action.ActionID,
                    Standard = std,
                    Created = a.Action.Created,
                    Comment = a.Action.Comments?.Trim() ?? ""
                })
                .ToList();

            var row = standardRows.FirstOrDefault(r => r.Standard == std);

            standardPoints.Add(new SasStandardAnalysisPointVm
            {
                Standard = std,
                StandardTitle = title,
                ActionCount = actions.Count,
                AssessmentCount = actions.Select(a => a.AssessmentId).Distinct().Count(),
                ActionsRecentPeriod = recentCount,
                ActionsPriorPeriod = priorCount,
                AssessmentsRecentPeriod = recentAssessmentCount,
                AssessmentsPriorPeriod = priorAssessmentCount,
                ActionsWithoutDate = withoutDate,
                TrendLabel = trend,
                ActionsByPeriod = byPeriod,
                ActionsByYear = byYear,
                ActionsByStatus = byStatus,
                ActionsByOutcome = OrderOutcomeKeys(byOutcome),
                RepeatedThemes = repeated,
                Actions = actionDetails,
                PctOnAmberOrRed = row?.PctOnAmberOrRed,
                ActionsFromRed = row?.ActionsFromRedOutcome ?? byOutcome.GetValueOrDefault("Red", 0),
                ActionsFromAmber = row?.ActionsFromAmberOutcome ?? byOutcome.GetValueOrDefault("Amber", 0),
                ActionsFromGreen = row?.ActionsFromGreenOutcome ?? byOutcome.GetValueOrDefault("Green", 0),
                ActionsFromOther = row?.ActionsFromOtherOutcome ?? byOutcome.GetValueOrDefault("Other", 0)
            });
        }

        vm.StandardPoints = standardPoints.OrderBy(p => p.Standard).ToList();
        vm.IncreasingStandards = standardPoints.Where(p => p.TrendLabel == "Increasing").OrderByDescending(p => p.ActionCount).ToList();
        vm.TopRepeatedThemes = standardPoints
            .SelectMany(p => p.RepeatedThemes)
            .OrderByDescending(t => t.OccurrenceCount)
            .Take(15)
            .ToList();

        vm.TrendPeriodLabels = trendPeriods.Select(p => p.Label).ToList();
        vm.TrendPeriodTotals = trendPeriods
            .Select(p => standardPoints.Sum(sp => sp.ActionsByPeriod.GetValueOrDefault(p.Label, 0)))
            .ToList();

        vm.TrendYears = standardPoints
            .SelectMany(p => p.ActionsByYear.Keys)
            .Distinct()
            .Select(y => int.TryParse(y, out var yi) ? yi : 0)
            .Where(y => y > 0)
            .OrderBy(y => y)
            .Select(y => y.ToString(CultureInfo.InvariantCulture))
            .ToList();

        vm.ActionsByStandardByYear = standardPoints.ToDictionary(
            p => p.Standard,
            p => (IReadOnlyDictionary<string, int>)vm.TrendYears.ToDictionary(
                y => y,
                y => p.ActionsByYear.GetValueOrDefault(y, 0),
                StringComparer.OrdinalIgnoreCase));

        return vm;
    }

    private static string ClassifyProportionateTrend(
        int recentActions,
        int priorActions,
        int recentAssessments,
        int priorAssessments)
    {
        var recentRate = recentAssessments > 0 ? (double)recentActions / recentAssessments : recentActions;
        var priorRate = priorAssessments > 0 ? (double)priorActions / priorAssessments : priorActions;

        if (priorRate <= 0 && recentRate >= 1.5)
        {
            return "Increasing";
        }

        if (recentRate <= 0 && priorRate >= 1.5)
        {
            return "Decreasing";
        }

        if (recentRate > priorRate * 1.15 && recentRate - priorRate >= 0.25)
        {
            return "Increasing";
        }

        if (priorRate > recentRate * 1.15 && priorRate - recentRate >= 0.25)
        {
            return "Decreasing";
        }

        return "Stable";
    }

    private static IReadOnlyList<(string Key, string Label)> GetLastFinancialQuarters(DateTime anchorUtc, int count)
    {
        var quarters = new List<(string Key, string Label)>();
        var cursor = anchorUtc;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        while (quarters.Count < count)
        {
            var (key, label) = GetUkFinancialQuarter(cursor);
            if (seen.Add(key))
            {
                quarters.Add((key, label));
            }

            cursor = cursor.AddMonths(-3);
            if (quarters.Count >= count * 2)
            {
                break;
            }
        }

        quarters.Reverse();
        return quarters;
    }

    private static (string Key, string Label) GetUkFinancialQuarter(DateTime date)
    {
        var fyStartYear = date.Month >= 4 ? date.Year : date.Year - 1;
        var quarter = date.Month switch
        {
            >= 4 and <= 6 => 1,
            >= 7 and <= 9 => 2,
            >= 10 and <= 12 => 3,
            _ => 4
        };
        var fyEndShort = (fyStartYear + 1) % 100;
        var key = $"{fyStartYear}-{fyEndShort:D2}-Q{quarter}";
        var label = $"{fyStartYear}–{fyEndShort:D2} Q{quarter}";
        return (key, label);
    }

    private static string GetUkFinancialQuarterKey(DateTime date) => GetUkFinancialQuarter(date).Key;

    private static string ClassifyOutcomeLabel(string outcome)
    {
        var t = (outcome ?? "—").Trim();
        if (t.Equals("Green", StringComparison.OrdinalIgnoreCase))
        {
            return "Green";
        }

        if (t.Contains("Amber", StringComparison.OrdinalIgnoreCase))
        {
            return "Amber";
        }

        if (t.Equals("Red", StringComparison.OrdinalIgnoreCase) || t.Contains("Not met", StringComparison.OrdinalIgnoreCase))
        {
            return "Red";
        }

        return string.IsNullOrWhiteSpace(t) ? "Other" : "Other";
    }

    private static Dictionary<string, int> OrderOutcomeKeys(Dictionary<string, int> d) =>
        new[] { "Green", "Amber", "Red", "Other" }
            .Where(k => d.ContainsKey(k))
            .Concat(d.Keys.Where(k => !new[] { "Green", "Amber", "Red", "Other" }.Contains(k, StringComparer.OrdinalIgnoreCase))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(k => k, k => d[k], StringComparer.OrdinalIgnoreCase);

    private static string NormalizeCommentKey(string comment) =>
        WhitespaceCollapse.Replace(comment.Trim().ToLowerInvariant(), " ");

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max].TrimEnd() + "…";
}
