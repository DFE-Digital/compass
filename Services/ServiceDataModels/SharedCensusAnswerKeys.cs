using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Helpers for resolving answers against the shared base theme set by stable field key.
/// </summary>
public static class SharedCensusAnswerKeys
{
    public static string? ResolveStableKey(ServiceDataModelAnswer answer) =>
        !string.IsNullOrWhiteSpace(answer.FieldStableKey)
            ? answer.FieldStableKey.Trim()
            : answer.Field?.StableKey;

    public static ServiceDataModelAnswer? FindByStableKey(
        IEnumerable<ServiceDataModelAnswer> answers,
        string fieldStableKey)
    {
        if (string.IsNullOrWhiteSpace(fieldStableKey))
            return null;

        return answers.FirstOrDefault(a =>
            string.Equals(ResolveStableKey(a), fieldStableKey, StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<CoreCensusThemeField> FlattenActiveFields(
        IEnumerable<CoreCensusTheme> themes) =>
        themes
            .Where(t => t.IsActive && !t.IsServiceOffering)
            .SelectMany(t => t.Fields.Where(f => !f.IsDisabled))
            .ToList();

    /// <summary>
    /// Projects a shared catalogue field into a transient model field for reuse of
    /// validation / prefill helpers that still accept <see cref="ServiceDataModelField"/>.
    /// </summary>
    public static ServiceDataModelField AsTransientModelField(CoreCensusThemeField field) =>
        new()
        {
            Id = field.Id,
            StableKey = field.StableKey,
            Label = field.Label,
            Guidance = field.Guidance,
            FieldType = field.FieldType,
            IsMandatory = field.IsMandatory,
            CountsTowardsCompletion = field.CountsTowardsCompletion,
            IsReportable = field.IsReportable,
            SortOrder = field.SortOrder,
            IsDisabled = field.IsDisabled,
            VisibilityRuleJson = field.VisibilityRuleJson,
            CanonicalAttributeKey = field.CanonicalAttributeKey,
            ValidationPattern = field.ValidationPattern,
            MinNumber = field.MinNumber,
            MaxNumber = field.MaxNumber,
            Options = field.Options
                .OrderBy(o => o.SortOrder)
                .Select(o => new ServiceDataModelFieldOption
                {
                    Id = o.Id,
                    ValueKey = o.ValueKey,
                    Label = o.Label,
                    SortOrder = o.SortOrder
                })
                .ToList()
        };
}
