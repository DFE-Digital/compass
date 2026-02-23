using Compass.Models.DemandTriage;

namespace Compass.Services.DemandTriage;

/// <summary>
/// Server-side scoring engine for demand triage.
/// All scoring logic is here — the client only submits answers.
/// Section max scores: Strategic Alignment 15, Urgency 10, Funding 22, RICE 42 (total 100).
/// </summary>
public static class DemandScoringEngine
{
    // ── Answer score lookup (question code → answer label → score) ───────────
    private static readonly Dictionary<string, Dictionary<string, int>> SingleSelectScores = new(StringComparer.OrdinalIgnoreCase)
    {
        // 1.2 – DDT Priority Strategic Outcome (all score 5 except "None of the above" = 0)
        ["1.2"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Delivering Digitally - Joined‑up services that empower parents and carers"] = 5,
            ["Delivering Digitally - Services that help learners progress between education stages"] = 5,
            ["Delivering Digitally - Joined‑up school services that support improvement & productivity"] = 5,
            ["Delivering Digitally - Workforce services that address quality and sufficiency gaps"] = 5,
            ["Delivering Digitally - Services that deliver accurate and timely funding to DfE sectors"] = 5,
            ["Powered by Data - Trusted data from end to end"] = 5,
            ["Powered by Data - Ready for any question"] = 5,
            ["Powered by Data - Data‑driven school system"] = 5,
            ["Powered by Data - Efficient multi‑agency information sharing"] = 5,
            ["The Right Technology - AI‑enabled business operations"] = 5,
            ["The Right Technology - Unified platforms for scale, efficiency and resilience"] = 5,
            ["The Right Technology - Exceptional workplace technology"] = 5,
            ["The Right Technology - DfE systems are safe and secure"] = 5,
            ["DDaT Skills and Expertise - The right people are in the right roles, with skills matched to organisation needs"] = 5,
            ["DDaT Skills and Expertise - DfE workforce upskilling"] = 5,
            ["Sector Capability - Safe and effective digital and AI tools for teaching and learning"] = 5,
            ["Sector Capability - Safe and effective case management systems in CSC"] = 5,
            ["Sector Capability - All schools to safe and reliable tech baseline"] = 5,
            ["Sector Capability - School workforce digital and data capability building"] = 5,
            ["Sector Capability - Local authority digital and data capability building"] = 5,
            ["None of the above"] = 0,
        },
        // 1.3 – SoS Opportunity Mission Pillar
        ["1.3"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Best start in life"] = 5,
            ["Every child achieving and thriving"] = 5,
            ["Skills for opportunity and growth"] = 5,
            ["Cross cutting: Family Security (tackling child poverty, keeping children safe)"] = 5,
            ["Does not support SoS Opportunity Mission Pillar"] = 0,
        },
        // 1.4 – Portfolio roadmap
        ["1.4"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Yes"] = 5,
            ["Other"] = 2,
            ["No"] = 0,
        },
        // 2.1 – Compelling reason to act now
        ["2.1"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Yes"] = 5,
            ["Don't know"] = 2,
            ["No"] = 0,
        },
        // 2.3 – Critical delivery date
        ["2.3"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Contract expiry date"] = 5,
            ["Contract start date"] = 5,
            ["Managed service transition or handover date"] = 5,
            ["Supplier notice period deadline"] = 5,
            ["Ministerial commitment start date"] = 5,
            ["Legislative or statutory deadline"] = 5,
            ["Programme or portfolio milestone date"] = 5,
            ["Operational business critical date"] = 5,
            ["Financial year or budget cycle deadline"] = 5,
            ["No critical delivery date"] = 0,
        },
        // 3.2 – Confirmation of funding
        ["3.2"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Yes"] = 10,
            ["No"] = 0,
        },
        // 3.3 – Budget approved for phase or programme
        ["3.3"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Entire Programme + Run & Maintain"] = 10,
            ["Entire Programme"] = 5,
            ["Single Phase Only"] = 2,
        },
        // 3.5 – Cost centre identified
        ["3.5"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Yes"] = 2,
            ["No"] = 0,
        },
        // 4.4 – Proportion of users affected
        ["4.4"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Nearly everyone"] = 10,
            ["More than half"] = 8,
            ["About half"] = 5,
            ["A smaller subset"] = 2,
            ["Not sure"] = 0,
        },
        // 4.6 – Scale of benefit
        ["4.6"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Project is delivered by external partner without DDT engagement"] = 5,
            ["Project is delivered by external partner with DDT engagement"] = 5,
            ["Project benefits are local to a single team"] = 2,
            ["The project benefits a single groups"] = 3,
            ["The project benefits multiple groups"] = 4,
            ["The project benefits the sector"] = 10,
            ["The project benefits the whole department"] = 10,
            ["Not sure"] = 0,
        },
        // 4.7 – Achievability
        ["4.7"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Very achievable"] = 10,
            ["Mostly achievable"] = 8,
            ["Somewhat achievable"] = 5,
            ["Slightly achievable"] = 2,
            ["Not achievable"] = 0,
        },
        // 4.8 – Project length
        ["4.8"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["0 - 3 months"] = 3,
            ["3 - 9 months"] = 2,
            ["longer than 9 months"] = 1,
            ["Not sure"] = 0,
        },
        // 4.9 – Headcount to support delivery
        ["4.9"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Yes"] = 5,
            ["No"] = 0,
        },
    };

    // Multi-select questions: score 2 per selection; "I don't know" = 0 and exclusive
    private static readonly HashSet<string> MultiSelectQuestions = new(StringComparer.OrdinalIgnoreCase) { "4.2", "4.3" };
    private const string IDontKnow = "I don't know";
    private const int MultiSelectScorePerItem = 2;

    // Informational-only questions (no score)
    private static readonly HashSet<string> InformationalQuestions = new(StringComparer.OrdinalIgnoreCase)
    {
        "1.1", "1.5", "1.6",
        "2.2", "2.4", "2.5",
        "3.1", "3.4", "3.6",
        "4.1", "4.5", "4.10", "4.11"
    };

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Calculate the score for a single answer and return the score value.
    /// Returns null for informational/free-text questions.
    /// </summary>
    public static int? ScoreAnswer(string questionCode, string answerValue)
    {
        if (InformationalQuestions.Contains(questionCode))
            return null;

        if (MultiSelectQuestions.Contains(questionCode))
        {
            if (string.Equals(answerValue, IDontKnow, StringComparison.OrdinalIgnoreCase))
                return 0;
            return MultiSelectScorePerItem;
        }

        if (SingleSelectScores.TryGetValue(questionCode, out var options) &&
            options.TryGetValue(answerValue, out var score))
            return score;

        return null;
    }

    /// <summary>
    /// Recalculate all section scores, total, bands, and suggestion band
    /// from the given set of answers. Persists results onto the scorecard.
    /// </summary>
    public static ScorecardCalculationResult Calculate(IEnumerable<DemandAnswer> answers)
    {
        var answerList = answers.ToList();

        // ── Section 1: Strategic Alignment (1.2 + 1.3 + 1.4) ────────────────
        int strategicScore = SumSingleSelect(answerList, "1.2")
                           + SumSingleSelect(answerList, "1.3")
                           + SumSingleSelect(answerList, "1.4");

        // ── Section 2: Urgency (2.1 + 2.3) ──────────────────────────────────
        int urgencyScore = SumSingleSelect(answerList, "2.1")
                         + SumSingleSelect(answerList, "2.3");

        // ── Section 3: Funding (3.2 + 3.3 + 3.5) ────────────────────────────
        int fundingScore = SumSingleSelect(answerList, "3.2")
                         + SumSingleSelect(answerList, "3.3")
                         + SumSingleSelect(answerList, "3.5");

        // ── Section 4: RICE (4.2 + 4.3 + 4.4 + 4.6 + 4.7 + 4.8 + 4.9) ─────
        int riceScore = SumMultiSelect(answerList, "4.2")
                      + SumMultiSelect(answerList, "4.3")
                      + SumSingleSelect(answerList, "4.4")
                      + SumSingleSelect(answerList, "4.6")
                      + SumSingleSelect(answerList, "4.7")
                      + SumSingleSelect(answerList, "4.8")
                      + SumSingleSelect(answerList, "4.9");

        int totalScore = strategicScore + urgencyScore + fundingScore + riceScore;

        return new ScorecardCalculationResult
        {
            StrategicAlignmentScore = strategicScore,
            UrgencyScore = urgencyScore,
            FundingScore = fundingScore,
            RiceScore = riceScore,
            TotalScore = totalScore,
            StrategicAlignmentBand = GetStrategicBand(strategicScore),
            UrgencyBand = GetUrgencyBand(urgencyScore),
            FundingBand = GetFundingBand(fundingScore),
            RiceBand = GetRiceBand(riceScore),
            SuggestionBand = GetSuggestionBand(totalScore)
        };
    }

    // ── Band helpers ─────────────────────────────────────────────────────────

    public static string GetStrategicBand(int score) => score switch
    {
        <= 5 => "Low alignment",
        <= 10 => "Medium alignment",
        _ => "High alignment"
    };

    public static string GetUrgencyBand(int score) => score switch
    {
        <= 2 => "No urgency clearly defined",
        <= 5 => "Some urgency clearly defined",
        _ => "High urgency clearly defined"
    };

    public static string GetFundingBand(int score) => score switch
    {
        <= 2 => "No funding available",
        <= 14 => "Partial funding available",
        _ => "Full funding available"
    };

    public static string GetRiceBand(int score) => score switch
    {
        <= 11 => "High effort",
        <= 24 => "Medium effort",
        _ => "Low effort"
    };

    public static string GetSuggestionBand(int score) => score switch
    {
        <= 20 => "Do not do",
        <= 56 => "Could do",
        _ => "Must do"
    };

    public static string SuggestionBandCssClass(string? band) => band switch
    {
        "Must do" => "govuk-tag govuk-tag--green",
        "Could do" => "govuk-tag govuk-tag--yellow",
        "Do not do" => "govuk-tag govuk-tag--red",
        _ => "govuk-tag govuk-tag--grey"
    };

    // ── Answer option catalogues (for rendering forms) ───────────────────────

    public static IReadOnlyList<string> GetOptions(string questionCode) => questionCode switch
    {
        "1.2" => new[]
        {
            "Delivering Digitally - Joined‑up services that empower parents and carers",
            "Delivering Digitally - Services that help learners progress between education stages",
            "Delivering Digitally - Joined‑up school services that support improvement & productivity",
            "Delivering Digitally - Workforce services that address quality and sufficiency gaps",
            "Delivering Digitally - Services that deliver accurate and timely funding to DfE sectors",
            "Powered by Data - Trusted data from end to end",
            "Powered by Data - Ready for any question",
            "Powered by Data - Data‑driven school system",
            "Powered by Data - Efficient multi‑agency information sharing",
            "The Right Technology - AI‑enabled business operations",
            "The Right Technology - Unified platforms for scale, efficiency and resilience",
            "The Right Technology - Exceptional workplace technology",
            "The Right Technology - DfE systems are safe and secure",
            "DDaT Skills and Expertise - The right people are in the right roles, with skills matched to organisation needs",
            "DDaT Skills and Expertise - DfE workforce upskilling",
            "Sector Capability - Safe and effective digital and AI tools for teaching and learning",
            "Sector Capability - Safe and effective case management systems in CSC",
            "Sector Capability - All schools to safe and reliable tech baseline",
            "Sector Capability - School workforce digital and data capability building",
            "Sector Capability - Local authority digital and data capability building",
            "None of the above"
        },
        "1.3" => new[]
        {
            "Best start in life",
            "Every child achieving and thriving",
            "Skills for opportunity and growth",
            "Cross cutting: Family Security (tackling child poverty, keeping children safe)",
            "Does not support SoS Opportunity Mission Pillar"
        },
        "1.4" => new[] { "Yes", "Other", "No" },
        "2.1" => new[] { "Yes", "Don't know", "No" },
        "2.3" => new[]
        {
            "Contract expiry date",
            "Contract start date",
            "Managed service transition or handover date",
            "Supplier notice period deadline",
            "Ministerial commitment start date",
            "Legislative or statutory deadline",
            "Programme or portfolio milestone date",
            "Operational business critical date",
            "Financial year or budget cycle deadline",
            "No critical delivery date"
        },
        "3.1" => new[] { "Help with advice / guidance", "Delivery" },
        "3.2" => new[] { "Yes", "No" },
        "3.3" => new[] { "Entire Programme + Run & Maintain", "Entire Programme", "Single Phase Only" },
        "3.5" => new[] { "Yes", "No" },
        "3.6" => new[] { "Programme", "Capital", "Admin" },
        "4.1" => new[] { "External users", "Internal users", "Both" },
        "4.2" => new[]
        {
            "Adult learners 18+",
            "Careers advisers or work coaches",
            "Children or young people",
            "Children or young people with SEND",
            "Education providers and early years workforce",
            "Employers",
            "Local authority workforce",
            "NEET or career seekers",
            "Parents or carers",
            "Professional external users of DfE Data",
            "Social care workforce",
            "I don't know"
        },
        "4.3" => new[]
        {
            "DFE Workforce - Strategy and transformation",
            "DFE Workforce - Analysis and Insight",
            "DFE Workforce - Architecture",
            "DFE Workforce - Business planning",
            "DFE Workforce - Communications and engagement",
            "DFE Workforce - Corporate services",
            "DFE Workforce - Data",
            "DFE Workforce - Digital",
            "DFE Workforce - Efficiency and productivity",
            "DFE Workforce - Finance and commercial",
            "DFE Workforce - HR and workforce planning",
            "DFE Workforce - Legal and compliance",
            "DFE Workforce - Policy",
            "DFE Workforce - Risk, assurance and governance",
            "DFE Workforce - Service Management",
            "DFE Workforce - Technology",
            "I don't know"
        },
        "4.4" => new[] { "Nearly everyone", "More than half", "About half", "A smaller subset", "Not sure" },
        "4.6" => new[]
        {
            "Project is delivered by external partner without DDT engagement",
            "Project is delivered by external partner with DDT engagement",
            "Project benefits are local to a single team",
            "The project benefits a single groups",
            "The project benefits multiple groups",
            "The project benefits the sector",
            "The project benefits the whole department",
            "Not sure"
        },
        "4.7" => new[] { "Very achievable", "Mostly achievable", "Somewhat achievable", "Slightly achievable", "Not achievable" },
        "4.8" => new[] { "0 - 3 months", "3 - 9 months", "longer than 9 months", "Not sure" },
        "4.9" => new[] { "Yes", "No" },
        _ => Array.Empty<string>()
    };

    // ── Private helpers ──────────────────────────────────────────────────────

    private static int SumSingleSelect(List<DemandAnswer> answers, string code)
    {
        var answer = answers.FirstOrDefault(a =>
            string.Equals(a.QuestionCode, code, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(a.AnswerValue));

        if (answer == null || string.IsNullOrWhiteSpace(answer.AnswerValue))
            return 0;

        if (SingleSelectScores.TryGetValue(code, out var opts) &&
            opts.TryGetValue(answer.AnswerValue, out var score))
            return score;

        return 0;
    }

    private static int SumMultiSelect(List<DemandAnswer> answers, string code)
    {
        var selected = answers
            .Where(a => string.Equals(a.QuestionCode, code, StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(a.AnswerValue))
            .Select(a => a.AnswerValue!)
            .ToList();

        if (!selected.Any()) return 0;

        // "I don't know" is exclusive and scores 0
        if (selected.Any(v => string.Equals(v, IDontKnow, StringComparison.OrdinalIgnoreCase)))
            return 0;

        return selected.Count * MultiSelectScorePerItem;
    }
}

/// <summary>Result of a scorecard calculation.</summary>
public class ScorecardCalculationResult
{
    public int StrategicAlignmentScore { get; init; }
    public int UrgencyScore { get; init; }
    public int FundingScore { get; init; }
    public int RiceScore { get; init; }
    public int TotalScore { get; init; }
    public string StrategicAlignmentBand { get; init; } = string.Empty;
    public string UrgencyBand { get; init; } = string.Empty;
    public string FundingBand { get; init; } = string.Empty;
    public string RiceBand { get; init; } = string.Empty;
    public string SuggestionBand { get; init; } = string.Empty;
}
