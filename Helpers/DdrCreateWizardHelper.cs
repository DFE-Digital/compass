using Compass.ViewModels.Modern.Ddr;

namespace Compass.Helpers;

/// <summary>
/// Section completion for the DDR create wizard (steps 2–7). Order: Decision (includes retrospective),
/// Deviation, Alternatives, Scope, Review, DesignOps. Mirrors validation used by
/// <see cref="Controllers.Modern.ModernDesignDecisionRecordsController.ApplyConditionalValidation"/>.
/// </summary>
public static class DdrCreateWizardHelper
{
    public const int StepCount = 7;

    public static IReadOnlyDictionary<int, bool> GetSectionCompletion(DdrCreateViewModel m)
    {
        var d = new Dictionary<int, bool>();
        for (var s = 2; s <= 7; s++)
            d[s] = IsSectionComplete(m, s);
        return d;
    }

    /// <summary>True when sections 2–7 all satisfy completion rules (submit allowed).</summary>
    public static bool AllSectionsComplete(DdrCreateViewModel m)
    {
        for (var s = 2; s <= 7; s++)
        {
            if (!IsSectionComplete(m, s))
                return false;
        }

        return true;
    }

    public static bool IsSectionComplete(DdrCreateViewModel m, int step) =>
        step switch
        {
            2 => IsDecisionSectionComplete(m),
            3 => IsDeviationComplete(m),
            4 => IsAlternativesComplete(m),
            5 => IsScopeComplete(m),
            6 => IsReviewComplete(m),
            7 => IsMessageComplete(m),
            _ => true,
        };

    /// <summary>Decision fields plus retrospective (historic decision) rules.</summary>
    static bool IsDecisionSectionComplete(DdrCreateViewModel m) =>
        IsDecisionComplete(m) && IsRetrospectiveComplete(m);

    static bool IsScopeComplete(DdrCreateViewModel m) =>
        !string.IsNullOrWhiteSpace(m.Category)
        && (m.LinkedProductIds.Count > 0 || m.LinkedWorkItemIds.Count > 0);

    static bool IsDecisionComplete(DdrCreateViewModel m)
    {
        var st = m.ShortTitle ?? "";
        var ctx = m.ContextProblemStatement ?? "";
        var dec = m.Decision ?? "";
        var rat = m.Rationale ?? "";
        var cons = m.ConsequencesTradeoffs ?? "";
        return st.Length is >= 10 and <= 120
               && ctx.Length is >= 50 and <= 4000
               && dec.Length is >= 50 and <= 3000
               && rat.Length is >= 50 and <= 4000
               && cons.Length is >= 30 and <= 3000;
    }

    static List<string> GetAlternativeLines(string? text) =>
        (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

    static bool IsAlternativesComplete(DdrCreateViewModel m)
    {
        var lines = GetAlternativeLines(m.AlternativesText);
        if (lines.Count is < 1 or > 10)
            return false;
        return lines.All(l => l.Length is >= 20 and <= 1500);
    }

    static bool IsDeviationComplete(DdrCreateViewModel m)
    {
        if (!m.DeviationFlag)
            return true;
        if (string.IsNullOrWhiteSpace(m.DeviationType))
            return false;
        var det = m.DeviationDetails ?? "";
        if (det.Length is < 50 or > 3000)
            return false;
        return !string.IsNullOrWhiteSpace(m.ApprovalRoute);
    }

    static bool IsRetrospectiveComplete(DdrCreateViewModel m)
    {
        if (!m.RetrospectiveRecord)
            return true;
        var ctx = m.RetrospectiveContext ?? "";
        if (ctx.Length is < 50 or > 2000)
            return false;
        if (string.IsNullOrWhiteSpace(m.CurrentValidity))
            return false;
        if (!string.IsNullOrWhiteSpace(m.CurrentValidity)
            && (m.CurrentValidity == "Partially valid" || m.CurrentValidity == "No longer valid" || m.CurrentValidity == "Unknown")
            && string.IsNullOrWhiteSpace(m.CurrentValidityRationale))
            return false;
        return true;
    }

    static bool IsReviewComplete(DdrCreateViewModel m)
    {
        var trig = m.ReviewTrigger ?? "";
        if (trig.Length is < 10 or > 500)
            return false;

        var today = DateTime.UtcNow.Date;
        if (m.ReviewDate is { } d && d.Date < today)
            return false;

        if (m.DeviationFlag && m.ReviewDate is null)
            return false;

        return true;
    }

    static bool IsMessageComplete(DdrCreateViewModel m)
    {
        var msg = m.MessageToDesignOps ?? "";
        return msg.Length <= 1000;
    }
}
