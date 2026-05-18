using Compass.Models;

namespace Compass.Helpers;

/// <summary>
/// Counts assessment attainment against the current functional standard criteria tree
/// (same shape as the conduct UI), so totals match rendered criteria and orphan response rows
/// do not skew progress.
/// </summary>
public static class FunctionalAssessmentProgress
{
    public static (int Total, int Completed, int FullyMet, int PartiallyMet, int NotMet) CountAgainstStandardTree(
        FunctionalStandardAssessment assessment)
    {
        if (assessment.FunctionalStandard?.Themes == null)
        {
            return (0, 0, 0, 0, 0);
        }

        var total = 0;
        var fullyMet = 0;
        var partiallyMet = 0;
        var notMet = 0;
        var responses = assessment.CriteriaResponses;

        foreach (var theme in assessment.FunctionalStandard.Themes)
        {
            if (theme.PracticeAreas == null)
            {
                continue;
            }

            foreach (var pa in theme.PracticeAreas)
            {
                if (pa.Criteria == null)
                {
                    continue;
                }

                foreach (var c in pa.Criteria)
                {
                    total++;
                    var r = responses.FirstOrDefault(cr =>
                        cr.ThemeId == theme.ThemeId
                        && cr.PracticeAreaId == pa.PracticeAreaId
                        && string.Equals(cr.CriteriaCode, c.CriteriaCode, StringComparison.Ordinal));

                    switch (r?.Attainment)
                    {
                        case AttainmentLevel.FullyMet:
                            fullyMet++;
                            break;
                        case AttainmentLevel.PartiallyMet:
                            partiallyMet++;
                            break;
                        case AttainmentLevel.NotOrSeldomMet:
                            notMet++;
                            break;
                    }
                }
            }
        }

        var completed = fullyMet + partiallyMet + notMet;
        return (total, completed, fullyMet, partiallyMet, notMet);
    }
}
