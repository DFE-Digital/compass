using System.Text.Json;
using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Prefills census answers from canonical Service Register attributes and suppresses
/// "ask only when missing" prompts (e.g. has-public-url) when the register already has a value.
/// </summary>
public static class ServiceDataModelCanonicalPrefill
{
    /// <summary>
    /// Applies register values into empty answers for fields with a canonical attribute key,
    /// and synthesises Yes for visibility-controlling fields when a URL already exists on the register.
    /// </summary>
    /// <param name="suppressedFieldKeys">Stable keys hidden from respondents (already answered by the register).</param>
    public static IReadOnlyList<ServiceDataModelFieldAnswerInput> Apply(
        IReadOnlyList<ServiceDataModelFieldAnswerInput> inputs,
        IReadOnlyList<ServiceDataModelField> fields,
        CMDBProduct? product,
        out IReadOnlySet<string> suppressedFieldKeys)
    {
        var suppressed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        suppressedFieldKeys = suppressed;

        if (inputs.Count == 0)
            return inputs;

        // Prefer the first enabled field when duplicate stable keys exist so the form still loads.
        var fieldsByKey = IndexFieldsByStableKey(fields);
        var fieldsById = fields.ToDictionary(f => f.Id);

        // 1) Prefill empty canonical-backed answers from the product record.
        // Title is always locked to the register name (never a proposed correction).
        var updated = new List<ServiceDataModelFieldAnswerInput>(inputs.Count);
        foreach (var input in inputs)
        {
            if (!fieldsById.TryGetValue(input.FieldId, out var field) ||
                string.IsNullOrWhiteSpace(field.CanonicalAttributeKey))
            {
                updated.Add(input);
                continue;
            }

            if (IsTitleCanonicalKey(field.CanonicalAttributeKey))
            {
                var title = ResolveDisplayValue(product, field.CanonicalAttributeKey);
                updated.Add(string.IsNullOrWhiteSpace(title)
                    ? input
                    : input with { ValueJson = ToValueJson(title, field.FieldType) });
                continue;
            }

            if (ServiceDataModelCompletionCalculator.IsAnswered(input))
            {
                updated.Add(input);
                continue;
            }

            var registerValue = ResolveDisplayValue(product, field.CanonicalAttributeKey);
            if (string.IsNullOrWhiteSpace(registerValue))
            {
                updated.Add(input);
                continue;
            }

            updated.Add(input with { ValueJson = ToValueJson(registerValue, field.FieldType) });
        }

        // 2) When a URL is already on the register, do not ask "does it have a URL?" —
        // synthesise Yes on controlling fields and suppress them from the form.
        var registerUrl = ResolveDisplayValue(product, "url");
        if (!string.IsNullOrWhiteSpace(registerUrl))
        {
            foreach (var field in fields)
            {
                if (field.IsDisabled)
                    continue;
                if (!IsUrlCanonicalKey(field.CanonicalAttributeKey))
                    continue;
                if (string.IsNullOrWhiteSpace(field.VisibilityRuleJson))
                    continue;

                var controllingKey = TryGetEqualsYesControllingFieldKey(field.VisibilityRuleJson);
                if (string.IsNullOrWhiteSpace(controllingKey))
                    continue;

                suppressed.Add(controllingKey);

                for (var i = 0; i < updated.Count; i++)
                {
                    if (!string.Equals(updated[i].StableKey, controllingKey, StringComparison.OrdinalIgnoreCase))
                        continue;
                    updated[i] = updated[i] with { ValueJson = "true" };
                }
            }
        }

        _ = fieldsByKey; // retained for future key-based lookups
        return updated;
    }

    /// <summary>
    /// Builds a stable-key index without throwing on duplicates. When keys collide,
    /// keeps the first enabled field (then any first occurrence) and skips the rest.
    /// </summary>
    public static Dictionary<string, ServiceDataModelField> IndexFieldsByStableKey(
        IEnumerable<ServiceDataModelField> fields)
    {
        var result = new Dictionary<string, ServiceDataModelField>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.StableKey))
                continue;

            if (!result.TryGetValue(field.StableKey, out var existing))
            {
                result[field.StableKey] = field;
                continue;
            }

            // Prefer an enabled field over a disabled duplicate.
            if (existing.IsDisabled && !field.IsDisabled)
                result[field.StableKey] = field;
            else
                System.Diagnostics.Debug.WriteLine(
                    $"Duplicate ServiceDataModelField StableKey '{field.StableKey}' skipped (kept Id={existing.Id}, skipped Id={field.Id}).");
        }

        return result;
    }

    /// <summary>True when proposed equals the current register value (no change to propose).</summary>
    public static bool IsSameAsRegister(string? proposedDisplay, string? currentRegisterDisplay) =>
        string.Equals(
            NormalizeForCompare(proposedDisplay),
            NormalizeForCompare(currentRegisterDisplay),
            StringComparison.OrdinalIgnoreCase);

    public static string ResolveDisplayValue(CMDBProduct? product, string key)
    {
        if (product == null || string.IsNullOrWhiteSpace(key))
            return string.Empty;

        return key.Trim().ToLowerInvariant() switch
        {
            "title" or "name" or "producttitle" or "product-title" => product.Title ?? string.Empty,
            "url" or "producturl" or "product-url" =>
                string.IsNullOrWhiteSpace(product.ProductURL) ? string.Empty : product.ProductURL.Trim(),
            "description" or "summary" or "userdescription" or "user-description" =>
                FirstNonEmpty(product.UserDescription, product.CMDBDescription) ?? string.Empty,
            "phase" => product.Phase?.Name ?? string.Empty,
            _ => string.Empty
        };
    }

    public static string ToValueJson(string displayValue, ServiceDataModelFieldType fieldType)
    {
        if (fieldType == ServiceDataModelFieldType.YesNo)
        {
            if (string.Equals(displayValue, "Yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(displayValue, "true", StringComparison.OrdinalIgnoreCase))
                return "true";
            if (string.Equals(displayValue, "No", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(displayValue, "false", StringComparison.OrdinalIgnoreCase))
                return "false";
        }

        if (fieldType == ServiceDataModelFieldType.Number &&
            decimal.TryParse(displayValue, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var n))
            return JsonSerializer.Serialize(n);

        return JsonSerializer.Serialize(displayValue);
    }

    public static bool IsTitleCanonicalKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.Trim().ToLowerInvariant() is "title" or "name" or "producttitle" or "product-title";

    public static bool IsUrlCanonicalKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.Trim().ToLowerInvariant() is "url" or "producturl" or "product-url";

    public static bool IsDescriptionCanonicalKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.Trim().ToLowerInvariant() is "description" or "summary" or "userdescription" or "user-description";

    /// <summary>
    /// Title is confirmed from the register only — never proposed or edited in census.
    /// URL and description are applied to the product on save (not left as pending proposals).
    /// </summary>
    public static bool IsDirectProductUpdateCanonicalKey(string? key) =>
        IsUrlCanonicalKey(key) || IsDescriptionCanonicalKey(key);

    private static string? TryGetEqualsYesControllingFieldKey(string visibilityRuleJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(visibilityRuleJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("fieldKey", out var fieldKeyEl))
                return null;
            var fieldKey = fieldKeyEl.GetString();
            if (string.IsNullOrWhiteSpace(fieldKey))
                return null;
            if (!root.TryGetProperty("equals", out var equalsEl))
                return null;
            var equals = equalsEl.ValueKind == JsonValueKind.String
                ? equalsEl.GetString()
                : equalsEl.GetRawText().Trim('"');
            if (!string.Equals(equals, "Yes", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(equals, "true", StringComparison.OrdinalIgnoreCase))
                return null;
            return fieldKey;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }

    private static string NormalizeForCompare(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
