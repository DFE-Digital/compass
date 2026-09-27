using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Portable JSON package for deploying the shared default census theme schema
/// (themes, questions, capabilities) between COMPASS environments.
/// Does not include census answers, assignments, or FIPS user-group taxonomy.
/// </summary>
public sealed class CensusThemeSchemaPackage
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public DateTime? ExportedAtUtc { get; set; }

    public List<CensusThemeSchemaThemeDto> Themes { get; set; } = new();

    public List<CensusThemeSchemaCapabilityDto> Capabilities { get; set; } = new();
}

public sealed class CensusThemeSchemaThemeDto
{
    public string StableKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Guidance { get; set; }
    public int SortOrder { get; set; }
    public bool Disabled { get; set; }
    public List<CensusThemeSchemaQuestionDto> Questions { get; set; } = new();
}

public sealed class CensusThemeSchemaQuestionDto
{
    public string StableKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Guidance { get; set; }

    /// <summary>Field type name (e.g. Text, Lookup) or numeric enum value.</summary>
    public string FieldType { get; set; } = nameof(ServiceDataModelFieldType.Text);

    public List<CensusThemeSchemaOptionDto> Options { get; set; } = new();
    public string? OptionsLookupKey { get; set; }
    public bool AllowMultiple { get; set; }
    public bool IsMandatory { get; set; }
    public bool CountsTowardsCompletion { get; set; } = true;
    public string? VisibilityRuleJson { get; set; }
    public int SortOrder { get; set; }
    public bool Disabled { get; set; }
}

public sealed class CensusThemeSchemaOptionDto
{
    public string ValueKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class CensusThemeSchemaCapabilityDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Reference { get; set; } = string.Empty;
}

public sealed class CensusThemeSchemaImportResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
    public bool DisableNotInFile { get; init; }
    public int ThemesAdded { get; init; }
    public int ThemesUpdated { get; init; }
    public int ThemesDisabled { get; init; }
    public int QuestionsAdded { get; init; }
    public int QuestionsUpdated { get; init; }
    public int QuestionsDisabled { get; init; }
    public int CapabilitiesAdded { get; init; }
    public int CapabilitiesUpdated { get; init; }
    public int OptionsAdded { get; init; }
    public int OptionsUpdated { get; init; }

    public string SummaryMessage
    {
        get
        {
            if (!Ok)
                return Error ?? "Import failed.";

            var disableNote = DisableNotInFile
                ? " Themes/questions absent from the file were disabled."
                : " Themes/questions absent from the file were left unchanged (disable-not-in-file was off).";

            return
                $"Schema imported (version {CensusThemeSchemaPackage.CurrentSchemaVersion}). " +
                $"Themes: {ThemesAdded} added, {ThemesUpdated} updated" +
                (ThemesDisabled > 0 ? $", {ThemesDisabled} disabled" : "") + ". " +
                $"Questions: {QuestionsAdded} added, {QuestionsUpdated} updated" +
                (QuestionsDisabled > 0 ? $", {QuestionsDisabled} disabled" : "") + ". " +
                $"Capabilities: {CapabilitiesAdded} added, {CapabilitiesUpdated} updated. " +
                $"Options: {OptionsAdded} added, {OptionsUpdated} updated." +
                disableNote;
        }
    }
}
