using System.Text.Json;
using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

public sealed record ServiceDataModelFieldAnswerInput(
    Guid FieldId,
    string StableKey,
    ServiceDataModelFieldType FieldType,
    bool IsMandatory,
    bool CountsTowardsCompletion,
    string? VisibilityRuleJson,
    string? ValueJson,
    bool IsValid = true,
    bool IsDisabled = false);

public sealed record ServiceDataModelCompletionResult(
    int ApplicableCountingFields,
    int AnsweredCountingFields,
    int ApplicableMandatoryFields,
    int AnsweredMandatoryFields,
    decimal? FieldCompletionPercent,
    decimal? MandatoryCompletionPercent,
    IReadOnlyDictionary<string, ServiceDataModelGroupCompletionResult> GroupBreakdown);

public sealed record ServiceDataModelGroupCompletionResult(
    string GroupKey,
    int ApplicableCountingFields,
    int AnsweredCountingFields,
    decimal? FieldCompletionPercent);

/// <summary>
/// Field completion % = answered applicable fields that count ÷ applicable fields that count × 100.
/// Empty denominator is not applicable (null), not 100%. Hidden conditional and disabled fields are excluded.
/// Valid zero and explicit No count as answers.
/// </summary>
public static class ServiceDataModelCompletionCalculator
{
    public static ServiceDataModelCompletionResult Calculate(
        IEnumerable<ServiceDataModelFieldAnswerInput> fields,
        IEnumerable<(string GroupKey, Guid FieldId)>? fieldGroupKeys = null)
    {
        var fieldList = fields.ToList();
        var answersByKey = fieldList
            .Where(f => !f.IsDisabled)
            .ToDictionary(f => f.StableKey, StringComparer.OrdinalIgnoreCase);
        var groupMap = fieldGroupKeys?.ToDictionary(x => x.FieldId, x => x.GroupKey) ?? new Dictionary<Guid, string>();

        var applicableCounting = 0;
        var answeredCounting = 0;
        var applicableMandatory = 0;
        var answeredMandatory = 0;
        var groupStats = new Dictionary<string, (int Applicable, int Answered)>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in fieldList)
        {
            if (field.IsDisabled)
                continue;

            if (!IsFieldVisible(field.VisibilityRuleJson, answersByKey))
                continue;

            var answered = IsAnswered(field);
            var groupKey = groupMap.TryGetValue(field.FieldId, out var gk) ? gk : "_ungrouped";

            if (field.CountsTowardsCompletion)
            {
                applicableCounting++;
                if (answered)
                    answeredCounting++;

                if (!groupStats.TryGetValue(groupKey, out var gs))
                    gs = (0, 0);
                groupStats[groupKey] = (gs.Applicable + 1, gs.Answered + (answered ? 1 : 0));
            }

            if (field.IsMandatory)
            {
                applicableMandatory++;
                if (answered && field.IsValid)
                    answeredMandatory++;
            }
        }

        var groupBreakdown = groupStats.ToDictionary(
            kv => kv.Key,
            kv => new ServiceDataModelGroupCompletionResult(
                kv.Key,
                kv.Value.Applicable,
                kv.Value.Answered,
                PercentOrNull(kv.Value.Answered, kv.Value.Applicable)),
            StringComparer.OrdinalIgnoreCase);

        return new ServiceDataModelCompletionResult(
            applicableCounting,
            answeredCounting,
            applicableMandatory,
            answeredMandatory,
            PercentOrNull(answeredCounting, applicableCounting),
            PercentOrNull(answeredMandatory, applicableMandatory),
            groupBreakdown);
    }

    public static bool IsFieldVisible(
        string? visibilityRuleJson,
        IReadOnlyDictionary<string, ServiceDataModelFieldAnswerInput> answersByKey)
    {
        if (string.IsNullOrWhiteSpace(visibilityRuleJson))
            return true;

        try
        {
            using var doc = JsonDocument.Parse(visibilityRuleJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("fieldKey", out var fieldKeyEl))
                return true;

            var fieldKey = fieldKeyEl.GetString();
            if (string.IsNullOrWhiteSpace(fieldKey))
                return true;

            if (!answersByKey.TryGetValue(fieldKey, out var controlling))
                return false;

            if (!IsAnswered(controlling))
                return false;

            var actual = NormalizeScalar(controlling.ValueJson);
            if (root.TryGetProperty("equals", out var equalsEl))
                return string.Equals(actual, NormalizeScalar(equalsEl.GetRawText().Trim('"')), StringComparison.OrdinalIgnoreCase);

            if (root.TryGetProperty("notEquals", out var notEqualsEl))
                return !string.Equals(actual, NormalizeScalar(notEqualsEl.GetRawText().Trim('"')), StringComparison.OrdinalIgnoreCase);

            return true;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    public static bool IsAnswered(ServiceDataModelFieldAnswerInput field)
    {
        if (string.IsNullOrWhiteSpace(field.ValueJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(field.ValueJson);
            var el = doc.RootElement;
            return el.ValueKind switch
            {
                JsonValueKind.Null => false,
                JsonValueKind.String => !string.IsNullOrWhiteSpace(el.GetString()),
                JsonValueKind.Number => true, // valid zero counts
                JsonValueKind.True => true,
                JsonValueKind.False => true, // explicit No / false counts
                JsonValueKind.Array => el.EnumerateArray().Any(x =>
                    x.ValueKind == JsonValueKind.String
                        ? !string.IsNullOrWhiteSpace(x.GetString())
                        : x.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)),
                JsonValueKind.Object => el.EnumerateObject().Any(),
                _ => false
            };
        }
        catch (JsonException)
        {
            // Non-JSON plain text fallback
            return !string.IsNullOrWhiteSpace(field.ValueJson);
        }
    }

    public static bool IsOverdue(ServiceDataModelAssignmentStatus status, DateTime? dueUtc, DateTime utcNow)
    {
        if (dueUtc == null)
            return false;
        if (status is ServiceDataModelAssignmentStatus.Reviewed
            or ServiceDataModelAssignmentStatus.Withdrawn
            or ServiceDataModelAssignmentStatus.NotApplicable)
            return false;
        return dueUtc.Value < utcNow;
    }

    private static decimal? PercentOrNull(int numerator, int denominator)
    {
        if (denominator <= 0)
            return null;
        return Math.Round((decimal)numerator / denominator * 100m, 1, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeScalar(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
            trimmed = trimmed[1..^1];
        if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
            return "Yes";
        if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase))
            return "No";
        return trimmed;
    }
}
