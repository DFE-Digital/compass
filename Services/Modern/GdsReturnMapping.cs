using Compass.Models;

namespace Compass.Services.Modern;

/// <summary>
/// Maps Compass commission / service-register fields onto the GDS Performance Commission
/// spreadsheet column values (Context Data Input and Service Data Input).
/// </summary>
public static class GdsReturnMapping
{
    public static readonly string[] ContextDataColumns =
    [
        "service_name",
        "transactional_or_not",
        "service_status",
        "internal_or_external",
        "service_purpose",
        "service_users",
        "service_owner",
        "service_manager",
        "service_usage",
        "payment_required",
        "onelogin_available",
        "online_available",
        "telephone_available",
        "post_available",
        "inperson_available",
        "user_satisfaction_method",
        "digital_adoption_method",
        "digital_completion_method",
        "cost_per_transaction_method",
        "service_kpi",
        "kpi_value",
        "comments"
    ];

    public static readonly string[] ServiceDataColumns =
    [
        "quarter",
        "service_name",
        "started_transactions",
        "completed_transactions",
        "started_digital_transactions",
        "completed_digital_transactions",
        "usat_total_number_of_responses",
        "usat_number_of_satisfied_responses",
        "usat_number_of_very_satisfied_responses",
        "accessibility_compliance",
        "cost_per_transaction_amount",
        "categories_included_in_cpt",
        "fte_count",
        "govuk_url",
        "comments"
    ];

    /// <summary>Formats a commission period as <c>Q#_YYYY/YY</c> (UK financial year).</summary>
    public static string FormatGdsQuarter(Commission commission)
    {
        ArgumentNullException.ThrowIfNull(commission);

        var q = NormalizeQuarterLabel(commission.Quarter, commission.StartDate);
        var fyStartYear = FinancialYearStartYear(commission.StartDate, q);
        var yyNext = ((fyStartYear + 1) % 100).ToString("D2");
        return $"{q}_{fyStartYear}/{yyNext}";
    }

    public static string NormalizeQuarterLabel(string? quarter, DateTime startDate)
    {
        var raw = quarter?.Trim() ?? "";
        if (raw.Length >= 2 &&
            (raw[0] == 'Q' || raw[0] == 'q') &&
            char.IsDigit(raw[1]))
        {
            return $"Q{raw[1]}";
        }

        // UK financial year: Apr–Jun Q1, Jul–Sep Q2, Oct–Dec Q3, Jan–Mar Q4
        var qNum = startDate.Month switch
        {
            >= 4 and <= 6 => 1,
            >= 7 and <= 9 => 2,
            >= 10 and <= 12 => 3,
            _ => 4
        };
        return $"Q{qNum}";
    }

    public static int FinancialYearStartYear(DateTime startDate, string quarterLabel)
    {
        // Q4 (Jan–Mar) belongs to the FY that started the previous calendar year.
        if (quarterLabel.Equals("Q4", StringComparison.OrdinalIgnoreCase))
            return startDate.Month <= 3 ? startDate.Year - 1 : startDate.Year;

        return startDate.Month >= 4 ? startDate.Year : startDate.Year - 1;
    }

    public static string MapTransactionalOrNot(ProductDto? product)
    {
        var typeName = GetCategoryName(product, "Type");
        if (string.IsNullOrWhiteSpace(typeName))
            return "";

        if (typeName.Contains("non", StringComparison.OrdinalIgnoreCase) &&
            typeName.Contains("transaction", StringComparison.OrdinalIgnoreCase))
            return "No";

        if (typeName.Equals("Transactional", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("transactional", StringComparison.OrdinalIgnoreCase))
            return "Yes";

        return "";
    }

    public static string MapServiceStatus(ProductDto? product)
    {
        var phase = product?.Phase?.Trim() ?? "";
        if (string.IsNullOrEmpty(phase))
            return "";

        if (phase.Equals("Live", StringComparison.OrdinalIgnoreCase))
            return "Live";
        if (phase.Equals("Public beta", StringComparison.OrdinalIgnoreCase) ||
            phase.Equals("Public Beta", StringComparison.OrdinalIgnoreCase))
            return "Public beta";
        if (phase.Equals("Private beta", StringComparison.OrdinalIgnoreCase) ||
            phase.Equals("Private Beta", StringComparison.OrdinalIgnoreCase))
            return "Private beta";
        if (phase.Equals("Alpha", StringComparison.OrdinalIgnoreCase))
            return "Alpha";
        if (phase.Equals("Retired", StringComparison.OrdinalIgnoreCase) ||
            phase.Equals("Decommissioned", StringComparison.OrdinalIgnoreCase) ||
            phase.Equals("Decommissioning", StringComparison.OrdinalIgnoreCase))
            return "Retired";
        if (phase.Equals("Beta", StringComparison.OrdinalIgnoreCase))
            return "Public beta";

        return "";
    }

    public static string MapInternalOrExternal(ProductDto? product)
    {
        var names = GetCategoryNames(product,
            "Facing", "Service facing", "Audience", "Internal or external", "Categorisation");

        foreach (var name in names)
        {
            if (name.Contains("external", StringComparison.OrdinalIgnoreCase))
                return "External";
            if (name.Contains("internal", StringComparison.OrdinalIgnoreCase))
                return "Internal";
        }

        var users = MapServiceUsers(product);
        if (users.Equals("Civil servants or contractors or ALBs", StringComparison.Ordinal))
            return "Internal";
        if (!string.IsNullOrEmpty(users))
            return "External";

        return "";
    }

    /// <summary>
    /// Service purpose from the service register description (user description, then CMDB description),
    /// falling back to CMS short/long description when register data is unavailable.
    /// </summary>
    public static string MapServicePurpose(
        string? registerUserDescription,
        string? registerCmdbDescription,
        ProductDto? product)
    {
        if (!string.IsNullOrWhiteSpace(registerUserDescription))
            return registerUserDescription.Trim();
        if (!string.IsNullOrWhiteSpace(registerCmdbDescription))
            return registerCmdbDescription.Trim();
        if (product == null)
            return "";
        if (!string.IsNullOrWhiteSpace(product.ShortDescription))
            return product.ShortDescription.Trim();
        if (!string.IsNullOrWhiteSpace(product.LongDescription))
            return product.LongDescription.Trim();
        return "";
    }

    /// <summary>Service owner display name from the service register contact (same source as the register table).</summary>
    public static string MapServiceOwner(string? registerServiceOwner, ProductDto? product)
    {
        if (!string.IsNullOrWhiteSpace(registerServiceOwner))
            return registerServiceOwner.Trim();

        var fromCms = product?.ServiceOwner?.DisplayName
            ?? product?.ServiceOwner?.EmailAddress;
        return string.IsNullOrWhiteSpace(fromCms) ? "" : fromCms.Trim();
    }

    public static string MapServiceUsers(ProductDto? product)
    {
        var groups = GetCategoryNames(product,
            "User group", "User Group", "User groups", "User Groups",
            "User Type", "User Types", "Audience", "Target Audience");

        foreach (var g in groups)
        {
            if (ContainsAny(g, "general public", "public", "citizen", "parent", "learner", "student"))
                return "General public";
            if (ContainsAny(g, "professional", "licensed", "teacher", "school", "provider", "employer"))
                return "Professional or licensed users";
            if (ContainsAny(g, "civil servant", "contractor", "alb", "internal", "staff", "dfe"))
                return "Civil servants or contractors or ALBs";
        }

        return "";
    }

    public static string MapChannelAvailable(IReadOnlyCollection<string> channelNames, params string[] tokens)
    {
        if (channelNames.Count == 0 || tokens.Length == 0)
            return "";

        foreach (var channel in channelNames)
        {
            foreach (var token in tokens)
            {
                if (channel.Contains(token, StringComparison.OrdinalIgnoreCase))
                    return "Yes";
            }
        }

        return "No";
    }

    public static IReadOnlyList<string> GetChannelNames(ProductDto? product)
    {
        return GetCategoryNames(product, "Channel", "Channels", "Delivery channel", "Access channel");
    }

    /// <summary>
    /// Builds a map from GDS Service Data Input metric column → best matching <see cref="PerformanceMetric"/>.
    /// Known identifiers (perf-1, perf-2, perf-6) take priority; remaining columns use fuzzy title/identifier match.
    /// </summary>
    public static Dictionary<string, PerformanceMetric?> BuildServiceMetricColumnMap(
        IReadOnlyList<PerformanceMetric> performanceMetrics)
    {
        var byId = performanceMetrics.ToDictionary(m => m.Identifier, m => m, StringComparer.OrdinalIgnoreCase);
        string Norm(string? s) =>
            (s ?? "").Replace(" ", "_", StringComparison.Ordinal).Replace("-", "_", StringComparison.Ordinal)
                .ToLowerInvariant();

        PerformanceMetric? Fuzzy(Func<PerformanceMetric, bool> predicate) =>
            performanceMetrics.FirstOrDefault(predicate);

        return new Dictionary<string, PerformanceMetric?>(StringComparer.OrdinalIgnoreCase)
        {
            ["completed_transactions"] = byId.GetValueOrDefault("perf-6"),
            ["completed_digital_transactions"] = byId.GetValueOrDefault("perf-2"),
            ["usat_total_number_of_responses"] = byId.GetValueOrDefault("perf-1"),
            ["started_transactions"] = Fuzzy(m =>
                Norm(m.Title).Contains("started") && Norm(m.Title).Contains("transaction") &&
                !Norm(m.Title).Contains("digital") && !Norm(m.Identifier).Contains("digital")),
            ["started_digital_transactions"] = Fuzzy(m =>
                (Norm(m.Identifier).Contains("started") && Norm(m.Identifier).Contains("digital")) ||
                (Norm(m.Title).Contains("started") && Norm(m.Title).Contains("digital"))),
            ["usat_number_of_satisfied_responses"] = Fuzzy(m =>
                (Norm(m.Identifier).Contains("usat") || Norm(m.Title).Contains("usat") || Norm(m.Title).Contains("satisfied")) &&
                (Norm(m.Title).Contains("satisfied") || Norm(m.Identifier).Contains("satisfied")) &&
                !Norm(m.Title).Contains("very")),
            ["usat_number_of_very_satisfied_responses"] = Fuzzy(m =>
                (Norm(m.Identifier).Contains("usat") || Norm(m.Title).Contains("usat") || Norm(m.Title).Contains("very")) &&
                Norm(m.Title).Contains("very") && Norm(m.Title).Contains("satisfied")),
            ["accessibility_compliance"] = Fuzzy(m =>
                Norm(m.Identifier).Contains("accessibility") || Norm(m.Identifier).Contains("acc") ||
                Norm(m.Title).Contains("accessibility") || Norm(m.Title).Contains("compliance")),
            ["cost_per_transaction_amount"] = Fuzzy(m =>
                Norm(m.Identifier).Contains("cost") || Norm(m.Title).Contains("cost per transaction") ||
                (Norm(m.Title).Contains("cost") && Norm(m.Title).Contains("transaction"))),
            ["categories_included_in_cpt"] = Fuzzy(m =>
                Norm(m.Title).Contains("categories") &&
                (Norm(m.Title).Contains("cpt") || Norm(m.Title).Contains("cost"))),
            ["fte_count"] = Fuzzy(m =>
                Norm(m.Identifier).Contains("fte") || Norm(m.Title).Contains("fte count") ||
                (Norm(m.Title).Contains("fte") && Norm(m.Title).Contains("count")))
        };
    }

    public static string GetMetricValue(
        CommissionSubmission submission,
        PerformanceMetric? metric)
    {
        if (metric == null)
            return "";

        var mv = submission.MetricValues?.FirstOrDefault(m => m.PerformanceMetricId == metric.Id);
        if (mv == null || mv.IsNotCaptured)
            return "";

        return mv.Value?.Trim() ?? "";
    }

    public static string NormalizeAccessibilityCompliance(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        if (raw.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("No", StringComparison.OrdinalIgnoreCase))
            return raw.Equals("Yes", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No";

        if (decimal.TryParse(raw, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var num))
        {
            // Historical Compass convention: 0 issues / compliant → Yes
            return num == 0 ? "Yes" : "No";
        }

        if (raw.Contains("compliant", StringComparison.OrdinalIgnoreCase) &&
            !raw.Contains("non", StringComparison.OrdinalIgnoreCase))
            return "Yes";

        return "";
    }

    private static string? GetCategoryName(ProductDto? product, params string[] typeNames)
    {
        return GetCategoryNames(product, typeNames).FirstOrDefault();
    }

    private static List<string> GetCategoryNames(ProductDto? product, params string[] typeNames)
    {
        if (product?.CategoryValues == null || typeNames.Length == 0)
            return [];

        var typeSet = new HashSet<string>(typeNames, StringComparer.OrdinalIgnoreCase);
        return product.CategoryValues
            .Where(cv => cv.CategoryType?.Name != null && typeSet.Contains(cv.CategoryType.Name.Trim()))
            .Select(cv => cv.Name?.Trim() ?? "")
            .Where(n => n.Length > 0)
            .ToList();
    }

    private static bool ContainsAny(string value, params string[] tokens) =>
        tokens.Any(t => value.Contains(t, StringComparison.OrdinalIgnoreCase));
}
