using System.Text.Json;
using System.Text.RegularExpressions;
using Compass.Models.Fips;

namespace Compass.Services.Fips;

/// <summary>Evaluates <see cref="FipsCmdbSyncRule"/> against CMDB entry data during sync.</summary>
public static class FipsCmdbSyncRuleEvaluator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    public static CMDBProductStatus? EvaluateFirstStatusMatch(
        IReadOnlyList<FipsCmdbSyncRule> rules,
        CmdbEntry entry,
        string entryJson,
        CMDBProduct product,
        string titleForProduct,
        ILogger logger)
    {
        foreach (var rule in rules)
        {
            if (!IsStatusRule(rule) || !rule.IsActive || string.IsNullOrWhiteSpace(rule.Pattern))
                continue;
            var haystack = BuildHaystack(rule.FieldScope, entry, entryJson, product, titleForProduct);
            if (haystack == null)
                continue;
            if (RuleMatches(rule, haystack, logger))
                return rule.TargetStatus;
        }

        return null;
    }

    /// <summary>Returns true if any active enterprise rule matches (sets <see cref="CMDBProduct.IsEnterpriseService"/> during sync).</summary>
    public static bool EvaluateSetsEnterpriseService(
        IReadOnlyList<FipsCmdbSyncRule> rules,
        CmdbEntry entry,
        string entryJson,
        CMDBProduct product,
        string titleForProduct,
        ILogger logger)
    {
        foreach (var rule in rules)
        {
            if (!IsEnterpriseRule(rule) || !rule.IsActive || string.IsNullOrWhiteSpace(rule.Pattern))
                continue;
            var haystack = BuildHaystack(rule.FieldScope, entry, entryJson, product, titleForProduct);
            if (haystack == null)
                continue;
            if (RuleMatches(rule, haystack, logger))
                return true;
        }

        return false;
    }

    private static bool IsStatusRule(FipsCmdbSyncRule rule) =>
        string.IsNullOrWhiteSpace(rule.Action)
        || string.Equals(rule.Action, FipsCmdbSyncRuleActions.SetStatus, StringComparison.OrdinalIgnoreCase);

    private static bool IsEnterpriseRule(FipsCmdbSyncRule rule) =>
        string.Equals(rule.Action, FipsCmdbSyncRuleActions.SetEnterpriseService, StringComparison.OrdinalIgnoreCase);

    private static string? BuildHaystack(
        string scope,
        CmdbEntry entry,
        string entryJson,
        CMDBProduct product,
        string title)
    {
        return scope switch
        {
            FipsCmdbSyncRuleScopes.Title => title,
            FipsCmdbSyncRuleScopes.Description => entry.Description ?? "",
            FipsCmdbSyncRuleScopes.ParentName => entry.ParentName ?? "",
            FipsCmdbSyncRuleScopes.UserDescription => product.UserDescription ?? "",
            FipsCmdbSyncRuleScopes.MappedText => string.Join('\n', new[]
            {
                title,
                entry.Description,
                entry.ParentName,
                product.UserDescription
            }.Where(s => !string.IsNullOrWhiteSpace(s))),
            FipsCmdbSyncRuleScopes.RawJson => entryJson,
            FipsCmdbSyncRuleScopes.ServiceClassification => ExtractServiceClassification(entryJson) ?? "",
            _ => null
        };
    }

    /// <summary>Reads <c>service_classification</c> from CMDB row JSON (plain string or ServiceNow reference).</summary>
    private static string? ExtractServiceClassification(string? entryJson)
    {
        if (string.IsNullOrWhiteSpace(entryJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(entryJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("service_classification", out var el))
                return null;
            if (el.ValueKind == JsonValueKind.String)
                return el.GetString();
            if (el.ValueKind == JsonValueKind.Object)
            {
                if (el.TryGetProperty("display_value", out var dv) && dv.ValueKind == JsonValueKind.String)
                    return dv.GetString();
                if (el.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
            return el.GetRawText();
        }
        catch
        {
            return null;
        }
    }

    private static bool RuleMatches(FipsCmdbSyncRule rule, string haystack, ILogger logger)
    {
        var pattern = rule.Pattern.Trim();
        if (pattern.Length == 0)
            return false;

        try
        {
            if (string.Equals(rule.MatchKind, FipsCmdbSyncRuleMatchKinds.Regex, StringComparison.OrdinalIgnoreCase))
            {
                var r = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
                return r.IsMatch(haystack);
            }

            return haystack.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }
        catch (RegexMatchTimeoutException)
        {
            logger.LogWarning("CMDB sync rule {RuleId} regex timed out", rule.Id);
            return false;
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "CMDB sync rule {RuleId} has invalid regex", rule.Id);
            return false;
        }
    }
}
