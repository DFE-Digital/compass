using Compass.Controllers;
using Compass.Data;
using Compass.Models;
using Compass.Services.Aiss;
using Compass.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds the Intelligence panel narrative from monthly report data (returns, RAG, priority, RAID, accessibility).</summary>
public static class MonthlyReportIntelligenceBuilder
{
    private const int MaxItemsPerSection = 10;

    public static async Task<MonthlyReportIntelligence> BuildAsync(
        CompassDbContext db,
        IAissSummaryService aissSummary,
        int? businessAreaId,
        int? directorateId,
        string monthDisplay,
        string prevMonthDisplay,
        string? filterBusinessAreaName,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Project> prevMonthProjects,
        MonthlyUpdateStats? submissionStats,
        IReadOnlyList<ProjectChangeRow> ragChanges,
        IReadOnlyList<ProjectChangeRow> priorityChanges,
        MonthlyReportRaidSummary raidSummary,
        IReadOnlyList<AissByBusinessAreaRow> accessibilityAreaRows,
        AissPlatformSummary? accessibilitySummary,
        DateTime monthStart,
        DateTime monthEnd,
        CancellationToken cancellationToken = default)
    {
        var scopeLabel = string.IsNullOrWhiteSpace(filterBusinessAreaName)
            ? "All business areas"
            : filterBusinessAreaName.Trim();

        var intel = new MonthlyReportIntelligence
        {
            ScopeLabel = scopeLabel,
            MonthDisplay = monthDisplay,
            PrevMonthDisplay = prevMonthDisplay
        };

        var paragraphs = new List<string>();
        paragraphs.Add(
            $"This is an automated read of {monthDisplay} for {scopeLabel}, compared with the picture at the start of the month and with {prevMonthDisplay} where Compass holds history. " +
            "It is intended as a conversation prompt—not a scorecard or formal assurance judgement.");

        if (projects.Count == 0)
        {
            paragraphs.Add("No active work items match the current filters, so there is nothing to summarise.");
            intel.SummaryParagraphs = paragraphs;
            return intel;
        }

        intel.Sections.Add(BuildSubmissionSection(submissionStats, monthDisplay));
        intel.Sections.Add(BuildRagSection(ragChanges, prevMonthDisplay));
        intel.Sections.Add(BuildPrioritySection(priorityChanges, prevMonthDisplay));
        intel.Sections.Add(await BuildRaidSectionAsync(
            db, businessAreaId, directorateId, monthStart, monthEnd, raidSummary, cancellationToken));
        intel.Sections.Add(await BuildAccessibilitySectionAsync(
            aissSummary, accessibilityAreaRows, accessibilitySummary, monthStart, monthEnd, cancellationToken));

        var signalCount = intel.Sections.Sum(s =>
            s.Items.Count + s.OverflowCount +
            s.Groups.Sum(g => g.Items.Count + g.OverflowCount));
        intel.SignalCount = signalCount;

        if (signalCount == 0)
            paragraphs.Add(
                $"For {monthDisplay}, nothing notable stood out against the checks below (RAG or priority movement in the month, new RAID items, or accessibility pressure). " +
                "That may mean a steady month—or that updates are still to land in Compass or AISS.");
        else
            paragraphs.Add(
                $"We surfaced {signalCount} notable signal{(signalCount == 1 ? "" : "s")} across returns, RAG, priority, RAID, and accessibility. " +
                "Review the sections below for specifics.");

        intel.SummaryParagraphs = paragraphs;
        return intel;
    }

    private static MonthlyReportIntelligenceSection BuildSubmissionSection(
        MonthlyUpdateStats? stats,
        string monthDisplay)
    {
        var section = new MonthlyReportIntelligenceSection
        {
            Id = "submission",
            Title = "Monthly returns",
            Intro = null
        };

        if (stats is null || stats.TotalProjects == 0)
        {
            section.Intro = "No work items were in scope for monthly returns in this view.";
            return section;
        }

        var pct = stats.TotalProjects == 0
            ? 0
            : Math.Round(100m * stats.Submitted / stats.TotalProjects, 1, MidpointRounding.AwayFromZero);

        section.Intro =
            $"For {monthDisplay}, {stats.Submitted} of {stats.TotalProjects} returns ({pct}%) were submitted by the due date ({stats.DueDate:d MMMM yyyy}).";

        if (stats.Late > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{stats.Late} return{(stats.Late == 1 ? "" : "s")} still unreturned after the due date",
                Tone = "warning"
            });
        }

        if (stats.InProgress > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{stats.InProgress} return{(stats.InProgress == 1 ? "" : "s")} in progress (draft started, not yet submitted)",
                Tone = "neutral"
            });
        }

        if (stats.NotStarted > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{stats.NotStarted} return{(stats.NotStarted == 1 ? "" : "s")} not yet started for this period",
                Tone = stats.NotStarted > stats.Submitted ? "warning" : "neutral"
            });
        }

        if (stats.Submitted == stats.TotalProjects)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = "All in-scope work items have a submitted return for this period",
                Tone = "positive"
            });
        }

        return section;
    }

    private static MonthlyReportIntelligenceSection BuildRagSection(
        IReadOnlyList<ProjectChangeRow> ragChanges,
        string prevMonthDisplay)
    {
        var section = new MonthlyReportIntelligenceSection
        {
            Id = "rag",
            Title = "RAG changes",
            Intro = null
        };

        if (ragChanges.Count == 0)
        {
            section.Intro =
                "No RAG changes were recorded in Compass during this reporting month for work items in scope.";
            return section;
        }

        section.Intro =
            $"{ragChanges.Count} work item{(ragChanges.Count == 1 ? "" : "s")} had a RAG change this month " +
            $"(recorded in Compass, compared with the start of {prevMonthDisplay} where history exists).";

        var worsening = ragChanges.Where(r => RagWorsened(r.From, r.To)).ToList();
        var improving = ragChanges.Where(r => RagImproved(r.From, r.To)).ToList();
        var lateral = ragChanges.Where(r => !RagWorsened(r.From, r.To) && !RagImproved(r.From, r.To)).ToList();

        if (worsening.Count > 0)
        {
            section.Groups.Add(BuildProjectChangeGroup(
                "rag-worse",
                CountLabel("Moved to a worse RAG", worsening.Count),
                "RAG moved toward red or a higher risk level (for example Green to Amber-Red).",
                "warning",
                worsening,
                FormatRagChangeSubtext));
        }

        if (improving.Count > 0)
        {
            section.Groups.Add(BuildProjectChangeGroup(
                "rag-better",
                CountLabel("Moved to a better RAG", improving.Count),
                "RAG moved toward green or a lower risk level.",
                "positive",
                improving,
                FormatRagChangeSubtext));
        }

        if (lateral.Count > 0)
        {
            section.Groups.Add(BuildProjectChangeGroup(
                "rag-other",
                CountLabel("Other RAG changes", lateral.Count),
                "Same tier, moved from not set, or a lateral move between named RAG levels.",
                "neutral",
                lateral,
                r => $"{r.From} → {r.To} (other movement)" + BaSuffix(r.BusinessArea) + " · " + r.ChangedAt.ToString("d MMM yyyy"),
                maxItems: 5));
        }

        return section;
    }

    private static MonthlyReportIntelligenceSection BuildPrioritySection(
        IReadOnlyList<ProjectChangeRow> priorityChanges,
        string prevMonthDisplay)
    {
        var section = new MonthlyReportIntelligenceSection
        {
            Id = "priority",
            Title = "Priority changes",
            Intro = null
        };

        if (priorityChanges.Count == 0)
        {
            section.Intro = "No delivery priority changes were recorded in this period for work items in scope.";
            return section;
        }

        section.Intro =
            $"{priorityChanges.Count} work item{(priorityChanges.Count == 1 ? "" : "s")} had a delivery priority change this month " +
            $"(versus the snapshot from {prevMonthDisplay}).";

        var worsening = priorityChanges.Where(r => PriWorsened(r.From, r.To)).ToList();
        var improving = priorityChanges.Where(r => PriImproved(r.From, r.To)).ToList();
        var other = priorityChanges.Where(r => !PriWorsened(r.From, r.To) && !PriImproved(r.From, r.To)).ToList();

        if (worsening.Count > 0)
        {
            section.Groups.Add(BuildProjectChangeGroup(
                "pri-worse",
                CountLabel("Priority increased", worsening.Count),
                "Priority moved toward critical or higher urgency.",
                "warning",
                worsening,
                FormatPriorityChangeSubtext));
        }

        if (improving.Count > 0)
        {
            section.Groups.Add(BuildProjectChangeGroup(
                "pri-better",
                CountLabel("Priority decreased", improving.Count),
                "Priority moved toward low or lower urgency.",
                "positive",
                improving,
                FormatPriorityChangeSubtext));
        }

        if (other.Count > 0)
        {
            section.Groups.Add(BuildProjectChangeGroup(
                "pri-other",
                CountLabel("Other priority changes", other.Count),
                "Involves not set, or a lateral change between named levels.",
                "neutral",
                other,
                r => $"{r.From} → {r.To}" + BaSuffix(r.BusinessArea) + " · " + r.ChangedAt.ToString("d MMM yyyy"),
                maxItems: 5));
        }

        return section;
    }

    private static async Task<MonthlyReportIntelligenceSection> BuildRaidSectionAsync(
        CompassDbContext db,
        int? businessAreaId,
        int? directorateId,
        DateTime monthStart,
        DateTime monthEnd,
        MonthlyReportRaidSummary raidSummary,
        CancellationToken cancellationToken)
    {
        var section = new MonthlyReportIntelligenceSection
        {
            Id = "raid",
            Title = "Risks and issues (RAID)",
            Intro =
                $"New risks and issues logged in {monthStart:MMMM yyyy}, plus the current open RAID picture in scope."
        };

        var riskQuery = db.Risks.AsNoTracking()
            .Where(r => !r.IsDeleted && r.CreatedAt >= monthStart && r.CreatedAt <= monthEnd);
        var issueQuery = db.Issues.AsNoTracking()
            .Where(i => !i.IsDeleted && i.CreatedAt >= monthStart && i.CreatedAt <= monthEnd);

        if (businessAreaId is { } baid)
        {
            riskQuery = riskQuery.Where(r =>
                r.RiskBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
                || (r.Project != null && r.Project.BusinessAreaId == baid));
            issueQuery = issueQuery.Where(i =>
                i.IssueBusinessAreas.Any(b => b.BusinessAreaLookupId == baid)
                || (i.Project != null && i.Project.BusinessAreaId == baid));
        }

        if (directorateId is { } did)
        {
            riskQuery = riskQuery.Where(r => r.RiskDivisions.Any(d => d.DivisionId == did));
            issueQuery = issueQuery.Where(i => i.IssueDivisions.Any(d => d.DivisionId == did));
        }

        var risksOpenedCount = await riskQuery.CountAsync(cancellationToken);
        var issuesOpenedCount = await issueQuery.CountAsync(cancellationToken);

        if (risksOpenedCount == 0 && issuesOpenedCount == 0 && raidSummary.TotalOpen == 0)
        {
            section.Intro = "No new risks or issues were opened this month, and there are no open RAID items in scope.";
            return section;
        }

        var introParts = new List<string>();
        if (risksOpenedCount > 0)
            introParts.Add($"{risksOpenedCount} new risk{(risksOpenedCount == 1 ? "" : "s")} opened this month");
        if (issuesOpenedCount > 0)
            introParts.Add($"{issuesOpenedCount} new issue{(issuesOpenedCount == 1 ? "" : "s")} opened this month");
        if (introParts.Count > 0)
            section.Intro += " " + string.Join("; ", introParts) + ".";

        var risksOpened = await riskQuery
            .OrderByDescending(r => r.RiskScore)
            .ThenBy(r => r.Title)
            .Take(MaxItemsPerSection)
            .Select(r => new { r.Id, r.Title, r.RiskScore, r.ProjectId })
            .ToListAsync(cancellationToken);

        var issuesOpened = await issueQuery
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Title)
            .Take(MaxItemsPerSection)
            .Select(i => new { i.Id, i.Title, i.ProjectId })
            .ToListAsync(cancellationToken);

        if (risksOpenedCount > MaxItemsPerSection)
            section.OverflowCount += risksOpenedCount - MaxItemsPerSection;

        if (issuesOpenedCount > MaxItemsPerSection)
            section.OverflowCount += issuesOpenedCount - MaxItemsPerSection;

        foreach (var r in risksOpened)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                RiskId = r.Id,
                ProjectId = r.ProjectId,
                Text = r.Title,
                Subtext = $"New risk · score {r.RiskScore}",
                Tone = r.RiskScore >= 15 ? "warning" : "neutral"
            });
        }

        foreach (var i in issuesOpened)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                IssueId = i.Id,
                ProjectId = i.ProjectId,
                Text = i.Title,
                Subtext = "New issue",
                Tone = "neutral"
            });
        }

        if (raidSummary.HighRisks > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{raidSummary.HighRisks} open high risk{(raidSummary.HighRisks == 1 ? "" : "s")} (score ≥ 15) in the current RAID register",
                Tone = "warning"
            });
        }

        if (raidSummary.OpenCriticalIssues > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{raidSummary.OpenCriticalIssues} open critical issue{(raidSummary.OpenCriticalIssues == 1 ? "" : "s")}",
                Tone = "warning"
            });
        }

        if (raidSummary.RisksReviewOverdue > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{raidSummary.RisksReviewOverdue} open risk{(raidSummary.RisksReviewOverdue == 1 ? "" : "s")} with a review date overdue",
                Tone = "warning"
            });
        }

        return section;
    }

    private static async Task<MonthlyReportIntelligenceSection> BuildAccessibilitySectionAsync(
        IAissSummaryService aissSummary,
        IReadOnlyList<AissByBusinessAreaRow> areaRows,
        AissPlatformSummary? summary,
        DateTime monthStart,
        DateTime monthEnd,
        CancellationToken cancellationToken)
    {
        var section = new MonthlyReportIntelligenceSection
        {
            Id = "accessibility",
            Title = "Accessibility (AISS)",
            Intro = "Accessibility issues from the Accessibility issues and statement service."
        };

        var open = areaRows.Sum(r => r.Open);
        var overdue = areaRows.Sum(r => r.Overdue);

        if (summary is null && areaRows.Count == 0)
        {
            section.Intro = "Accessibility data could not be loaded for this view.";
            return section;
        }

        if (open == 0 && overdue == 0)
        {
            section.Intro = "No open or overdue accessibility issues in scope for this view.";
            return section;
        }

        if (open > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{open} open accessibility issue{(open == 1 ? "" : "s")} across services in scope",
                Tone = open > 20 ? "warning" : "neutral"
            });
        }

        if (overdue > 0)
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{overdue} accessibility issue{(overdue == 1 ? "" : "s")} overdue (open past target date)",
                Tone = "warning"
            });
        }

        foreach (var row in areaRows.Where(r => r.Overdue > 0).OrderByDescending(r => r.Overdue).Take(5))
        {
            section.Items.Add(new MonthlyReportIntelligenceItem
            {
                Text = $"{row.BusinessArea}: {row.Overdue} overdue, {row.Open} open",
                Tone = "warning"
            });
        }

        try
        {
            var (trends, trendsErr) = await aissSummary.GetCriterionTrendsAsync(12, cancellationToken);
            if (trends is { MonthLabels.Count: > 0 } && string.IsNullOrEmpty(trendsErr))
            {
                var monthKey = monthStart.ToString("MMM yyyy");
                var idx = trends.MonthLabels.FindIndex(l =>
                    string.Equals(l?.Trim(), monthKey, StringComparison.OrdinalIgnoreCase));

                if (idx >= 0 && trends.ClosedInMonth is { Count: > 0 })
                {
                    var closedInMonth = trends.ClosedInMonth.Values.Sum(arr =>
                        arr != null && idx < arr.Count ? arr[idx] : 0);
                    if (closedInMonth > 0)
                    {
                        section.Items.Add(new MonthlyReportIntelligenceItem
                        {
                            Text = $"{closedInMonth} accessibility issue{(closedInMonth == 1 ? "" : "s")} closed in {monthKey} (all criteria, AISS trends)",
                            Tone = "positive"
                        });
                    }
                }

                if (idx > 0 && trends.OpenAtMonthEnd is { Count: > 0 })
                {
                    var openEnd = trends.OpenAtMonthEnd.Values.Sum(arr =>
                        arr != null && idx < arr.Count ? arr[idx] : 0);
                    var openPrev = trends.OpenAtMonthEnd.Values.Sum(arr =>
                        arr != null && idx - 1 < arr.Count ? arr[idx - 1] : 0);
                    var delta = openEnd - openPrev;
                    if (delta != 0)
                    {
                        section.Items.Add(new MonthlyReportIntelligenceItem
                        {
                            Text = delta > 0
                                ? $"Open accessibility issues at month-end rose by {delta} compared with the previous month ({openPrev} → {openEnd})"
                                : $"Open accessibility issues at month-end fell by {Math.Abs(delta)} compared with the previous month ({openPrev} → {openEnd})",
                            Tone = delta > 0 ? "warning" : "positive"
                        });
                    }
                }
            }
        }
        catch
        {
            // Optional enhancement
        }

        return section;
    }

    private static string CountLabel(string label, int count) =>
        count > 0 ? $"{label} ({count})" : label;

    private static string FormatRagChangeSubtext(ProjectChangeRow r) =>
        $"{r.From} → {r.To}" + BaSuffix(r.BusinessArea) + " · " + r.ChangedAt.ToString("d MMM yyyy");

    private static string FormatPriorityChangeSubtext(ProjectChangeRow r) =>
        $"{r.From} → {r.To}" + BaSuffix(r.BusinessArea) + " · " + r.ChangedAt.ToString("d MMM yyyy");

    private static MonthlyReportIntelligenceGroup BuildProjectChangeGroup(
        string id,
        string title,
        string? hint,
        string tone,
        List<ProjectChangeRow> rows,
        Func<ProjectChangeRow, string> formatSubtext,
        int? maxItems = null)
    {
        var group = new MonthlyReportIntelligenceGroup
        {
            Id = id,
            Title = title,
            Hint = hint,
            Tone = tone
        };

        if (rows.Count == 0)
            return group;

        var limit = maxItems ?? MaxItemsPerSection;
        var take = Math.Min(rows.Count, limit);
        foreach (var r in rows.Take(take))
        {
            group.Items.Add(new MonthlyReportIntelligenceItem
            {
                ProjectId = r.ProjectId,
                Text = r.Title,
                Subtext = formatSubtext(r),
                Tone = tone
            });
        }

        if (rows.Count > take)
            group.OverflowCount = rows.Count - take;

        return group;
    }

    private static string BaSuffix(string? ba) =>
        string.IsNullOrWhiteSpace(ba) ? "" : $" · {ba.Trim()}";

    private static int RagRiskOrder(string rag) => rag switch
    {
        "Green" => 5,
        "Amber-Green" => 4,
        "Amber-Red" => 2,
        "Red" => 1,
        _ => 3
    };

    private static bool RagWorsened(string from, string to) =>
        RagRiskOrder(to) < RagRiskOrder(from);

    private static bool RagImproved(string from, string to) =>
        RagRiskOrder(to) > RagRiskOrder(from);

    private static int PriRank(string pri) => pri switch
    {
        "Low" => 1,
        "Medium" => 2,
        "High" => 3,
        "Critical" => 4,
        _ => 0
    };

    private static bool PriWorsened(string from, string to)
    {
        var fr = PriRank(from);
        var tr = PriRank(to);
        return fr > 0 && tr > 0 && tr > fr;
    }

    private static bool PriImproved(string from, string to)
    {
        var fr = PriRank(from);
        var tr = PriRank(to);
        return fr > 0 && tr > 0 && tr < fr;
    }
}
