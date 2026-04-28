namespace Compass.ViewModels.Modern;

/// <summary>Data for <c>/modern/reporting/assessments</c> — service assessment (SAS) published summary and standard actions.</summary>
public class ModernServiceAssessmentsReportViewModel
{
    public bool SummaryLoadFailed { get; set; }
    public bool ActionsByStandardLoadFailed { get; set; }

    public int TotalAssessments { get; set; }
    public IReadOnlyList<KeyValuePair<string, int>> ByOutcome { get; set; } = Array.Empty<KeyValuePair<string, int>>();
    public IReadOnlyList<KeyValuePair<string, int>> ByType { get; set; } = Array.Empty<KeyValuePair<string, int>>();
    public IReadOnlyList<KeyValuePair<string, int>> ByPhase { get; set; } = Array.Empty<KeyValuePair<string, int>>();
    public IReadOnlyList<KeyValuePair<string, int>> ByYear { get; set; } = Array.Empty<KeyValuePair<string, int>>();

    public IReadOnlyList<SasAssessmentListItem> AssessmentRows { get; set; } = Array.Empty<SasAssessmentListItem>();

    /// <summary>Same as <see cref="AssessmentRows"/> but grouped for display (by assessment type).</summary>
    public IReadOnlyList<SasAssessmentsTypeGroup> AssessmentsGroupedByType { get; set; } = Array.Empty<SasAssessmentsTypeGroup>();

    public IReadOnlyList<SasStandardActionRow> StandardActionRows { get; set; } = Array.Empty<SasStandardActionRow>();

    /// <summary>True when actions are split by parent Service assessment RAG; includes published cohort + <c>actions/all</c> join.</summary>
    public bool ServiceStandardOutcomeBreakdownAvailable { get; set; }

    /// <summary>Base URL for published report pages, e.g. <c>https://service-assessments.education.gov.uk/reports/report</c> (no trailing slash).</summary>
    public string SasReportBaseUrl { get; set; } = "https://service-assessments.education.gov.uk/reports/report";
}

public class SasAssessmentsTypeGroup
{
    public string TypeLabel { get; set; } = "Other";
    public IReadOnlyList<SasAssessmentListItem> Items { get; set; } = Array.Empty<SasAssessmentListItem>();
}

public class SasAssessmentListItem
{
    public int AssessmentId { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Phase { get; set; }
    public string? Outcome { get; set; }
    public string? Portfolio { get; set; }
    public DateTime? AssessmentDate { get; set; }
}

public class SasStandardActionRow
{
    public int Standard { get; set; }
    public int ActionCount { get; set; }
    public int AssessmentCount { get; set; }

    public int ActionsFromRedOutcome { get; set; }
    public int ActionsFromAmberOutcome { get; set; }
    public int ActionsFromGreenOutcome { get; set; }
    public int ActionsFromOtherOutcome { get; set; }

    /// <summary>0–100: share of this standard’s actions on assessments with Amber or Red overall outcome. Null in legacy (aggregate-only) mode.</summary>
    public double? PctOnAmberOrRed { get; set; }

    /// <summary>1 = highest concern by Red/Amber share (when outcome breakdown available); otherwise by standard number.</summary>
    public int MostProblematicRank { get; set; }
}
