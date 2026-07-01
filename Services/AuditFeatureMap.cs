using System.Globalization;
using System.Text;

namespace Compass.Services;

/// <summary>
/// Groups <see cref="Compass.Models.AuditLog.Entity"/> CLR class names into
/// human-friendly "feature" buckets for the modern admin Audit explorer.
/// The map combines exact known entity names with a small set of fallback
/// heuristics so newly-added entities are still reachable through the
/// feature filter without requiring a code change.
/// </summary>
public static class AuditFeatureMap
{
    /// <summary>Canonical feature bucket with a stable URL key and display label.</summary>
    public sealed record Feature(string Key, string Name);

    public const string OtherKey = "other";

    private static readonly Feature[] _featuresOrdered = new[]
    {
        new Feature("work", "Work items"),
        new Feature("monthly-update", "Monthly updates"),
        new Feature("raid", "RAID"),
        new Feature("standards", "Standards"),
        new Feature("service-register", "Service register"),
        new Feature("performance", "Performance"),
        new Feature("demand-triage", "Demand triage"),
        new Feature("surveys", "Surveys & assessments"),
        new Feature("organisation", "Organisation structure"),
        new Feature("lookups", "Lookups & taxonomy"),
        new Feature("access", "Access & roles"),
        new Feature("system", "System settings"),
        new Feature(OtherKey, "Other"),
    };

    public static IReadOnlyList<Feature> AllFeatures => _featuresOrdered;

    public static Feature? GetFeature(string key) =>
        string.IsNullOrWhiteSpace(key)
            ? null
            : _featuresOrdered.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

    public static string FeatureNameFor(string entity)
    {
        var key = FeatureKeyFor(entity);
        return _featuresOrdered.First(f => f.Key == key).Name;
    }

    /// <summary>
    /// Map a CLR class name (e.g. <c>"ProjectMonthlyUpdate"</c>) to a feature
    /// bucket. Falls back to a keyword heuristic, then <see cref="OtherKey"/>.
    /// </summary>
    public static string FeatureKeyFor(string? entity)
    {
        if (string.IsNullOrWhiteSpace(entity)) return OtherKey;

        switch (entity)
        {
            case "Project":
            case "WorkItem":
            case "ProjectAssignment":
            case "ProjectStaffMember":
            case "ProjectContact":
            case "ProjectMilestone":
            case "Milestone":
                return "work";

            case "ProjectMonthlyUpdate":
            case "MonthlyUpdate":
            case "MonthlyUpdateNarrative":
            case "StatusUpdate":
            case "ProjectStatusUpdate":
                return "monthly-update";

            case "Risk":
            case "RiskMitigationAction":
            case "MitigationAction":
            case "Issue":
            case "IssueAction":
            case "NearMiss":
            case "Assumption":
            case "ProjectAssumption":
            case "Dependency":
            case "ProjectDependency":
            case "Decision":
            case "DecisionAction":
                return "raid";

            case "DdtStandard":
            case "DdtStandardVersion":
            case "DdtStandardUnpublishAudit":
            case "FunctionalStandard":
                return "standards";

            case "CMDBProduct":
            case "FipsServiceLine":
            case "FipsServiceAssignment":
            case "FipsContact":
                return "service-register";

            case "PerformanceReport":
            case "PerformanceCommission":
            case "PerformanceAssessment":
            case "PerformanceCycle":
            case "AssessmentCycle":
                return "performance";

            case "DemandTriageItem":
            case "DemandRequest":
            case "DemandTriage":
                return "demand-triage";

            case "SurveyTemplate":
            case "SurveyTemplateQuestion":
            case "SurveyInstance":
            case "SurveyResponse":
            case "SurveyJourney":
            case "ProductAccessibility":
                return "surveys";

            case "BusinessArea":
            case "BusinessAreaLookup":
            case "Directorate":
            case "DirectorateLookup":
            case "Division":
            case "DivisionLookup":
            case "GovernmentDepartment":
            case "Portfolio":
                return "organisation";

            case "User":
            case "UserGroup":
            case "Group":
            case "GroupMember":
            case "BusinessAreaUser":
            case "BusinessAreaAdmin":
            case "BusinessAreaLeadership":
            case "DirectorateLeadership":
            case "ApiToken":
            case "CmsAccessRequestProduct":
                return "access";

            case "FeatureSetting":
            case "FeatureToggle":
            case "NotificationSetting":
            case "NotificationRule":
                return "system";
        }

        if (entity.StartsWith("Fips", StringComparison.Ordinal)) return "service-register";
        if (entity.StartsWith("Demand", StringComparison.Ordinal)) return "demand-triage";
        if (entity.StartsWith("Survey", StringComparison.Ordinal)) return "surveys";
        if (entity.StartsWith("Ddt", StringComparison.Ordinal)) return "standards";
        if (entity.Contains("Standard", StringComparison.Ordinal)) return "standards";
        if (entity.StartsWith("Risk", StringComparison.Ordinal)
            || entity.StartsWith("Issue", StringComparison.Ordinal)
            || entity.StartsWith("NearMiss", StringComparison.Ordinal)
            || entity.StartsWith("Assumption", StringComparison.Ordinal)
            || entity.StartsWith("Dependency", StringComparison.Ordinal)
            || entity.StartsWith("Decision", StringComparison.Ordinal))
            return "raid";
        if (entity.StartsWith("Performance", StringComparison.Ordinal) || entity.StartsWith("Assessment", StringComparison.Ordinal)) return "performance";
        if (entity.EndsWith("Lookup", StringComparison.Ordinal)) return "lookups";
        if (entity.StartsWith("BusinessArea", StringComparison.Ordinal)
            || entity.StartsWith("Directorate", StringComparison.Ordinal)
            || entity.StartsWith("Division", StringComparison.Ordinal))
            return "organisation";
        if (entity.StartsWith("Notification", StringComparison.Ordinal)
            || entity.StartsWith("Feature", StringComparison.Ordinal))
            return "system";
        if (entity.EndsWith("Token", StringComparison.Ordinal)
            || entity.StartsWith("User", StringComparison.Ordinal)
            || entity.StartsWith("Group", StringComparison.Ordinal))
            return "access";
        if (entity.StartsWith("MonthlyUpdate", StringComparison.Ordinal)
            || entity.Contains("Monthly", StringComparison.Ordinal))
            return "monthly-update";
        if (entity.StartsWith("Project", StringComparison.Ordinal)
            || entity.StartsWith("Work", StringComparison.Ordinal)
            || entity.StartsWith("Milestone", StringComparison.Ordinal))
            return "work";

        return OtherKey;
    }

    /// <summary>
    /// Splits CamelCase / PascalCase identifiers into a sentence-cased label
    /// (e.g. <c>"ProjectMonthlyUpdate"</c> → <c>"Project monthly update"</c>).
    /// Treats consecutive uppercase letters (acronyms like <c>"CMDB"</c>) as a
    /// single word.
    /// </summary>
    public static string FriendlyEntityName(string? entity)
    {
        if (string.IsNullOrWhiteSpace(entity)) return string.Empty;
        var sb = new StringBuilder(entity.Length + 8);
        for (var i = 0; i < entity.Length; i++)
        {
            var c = entity[i];
            if (i > 0 && char.IsUpper(c))
            {
                var prev = entity[i - 1];
                var next = i + 1 < entity.Length ? entity[i + 1] : '\0';
                var endOfAcronym = char.IsUpper(prev) && next != '\0' && char.IsLower(next);
                var startOfWord = char.IsLower(prev) || char.IsDigit(prev);
                if (endOfAcronym || startOfWord)
                {
                    sb.Append(' ');
                }
            }
            sb.Append(i == 0 ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Splits a property name (e.g. <c>"ChangedByEmail"</c>) into a sentence-case label.</summary>
    public static string FriendlyFieldName(string? field) => FriendlyEntityName(field);

    /// <summary>
    /// Build a friendly verb label (e.g. <c>"Create"</c>, <c>"Edit started"</c>) for
    /// display next to the action chip.
    /// </summary>
    public static string FriendlyAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return string.Empty;
        var trimmed = action.Trim();
        return FriendlyEntityName(trimmed);
    }

    /// <summary>Choose a GOV.UK tag colour modifier suitable for an action verb.</summary>
    public static string GovUkTagColourFor(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return "govuk-tag--grey";
        return action.Trim().ToLowerInvariant() switch
        {
            "create" or "created" or "added" or "add" or "publish" or "published" or "approve" or "approved" => "govuk-tag--green",
            "delete" or "deleted" or "remove" or "removed" or "rejected" or "reject" => "govuk-tag--red",
            "update" or "updated" or "modified" or "modify" or "edit" or "editstarted" or "edit started" => "govuk-tag--blue",
            "submit" or "submitted" or "sent" => "govuk-tag--purple",
            "discard" or "discarded" or "editdiscarded" or "edit discarded" or "cancel" or "cancelled" => "govuk-tag--yellow",
            "unpublish" or "unpublished" => "govuk-tag--orange",
            _ => "govuk-tag--grey",
        };
    }

    /// <summary>Format a UTC timestamp consistently in en-GB.</summary>
    public static string FormatTimestamp(DateTime utc, string format = "d MMM yyyy HH:mm:ss 'UTC'")
    {
        var utcDate = utc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc.ToUniversalTime();
        return utcDate.ToString(format, CultureInfo.GetCultureInfo("en-GB"));
    }
}
