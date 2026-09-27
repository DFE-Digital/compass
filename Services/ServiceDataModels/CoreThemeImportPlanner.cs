using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Pure planning for core-theme import: excludes service offering and already-present themes.
/// </summary>
public static class CoreThemeImportPlanner
{
    public static CoreThemeImportPlan Plan(
        IEnumerable<CoreThemeDefinition> candidates,
        IReadOnlySet<string> existingCoreKeys,
        IReadOnlySet<string> existingStableKeys)
    {
        var toImport = new List<string>();
        var skippedPresent = new List<string>();
        var skippedOffering = new List<string>();

        foreach (var theme in candidates)
        {
            if (theme.IsServiceOffering)
            {
                skippedOffering.Add(theme.StableKey);
                continue;
            }

            if (existingCoreKeys.Contains(theme.StableKey) ||
                existingStableKeys.Contains(theme.StableKey))
            {
                skippedPresent.Add(theme.StableKey);
                continue;
            }

            toImport.Add(theme.StableKey);
        }

        return new CoreThemeImportPlan(toImport, skippedPresent, skippedOffering);
    }
}

public sealed record CoreThemeImportPlan(
    IReadOnlyList<string> ToImport,
    IReadOnlyList<string> SkippedAlreadyPresent,
    IReadOnlyList<string> SkippedServiceOffering);
