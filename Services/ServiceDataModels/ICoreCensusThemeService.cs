using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Manages the single shared base set of census themes/questions and exposes it
/// for every census to inherit (no per-model copies).
/// </summary>
public interface ICoreCensusThemeService
{
    /// <summary>Seeds the catalogue from the hard-coded catalog only when the table is empty.</summary>
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);

    /// <summary>Active, non-service-offering themes with non-disabled questions — the shared census structure.</summary>
    Task<IReadOnlyList<CoreCensusTheme>> GetSharedStructureAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoreCensusThemeListItem>> ListManagedThemesAsync(
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<CoreCensusTheme?> GetManagedThemeAsync(
        Guid themeId,
        string actorEmail,
        CancellationToken cancellationToken = default);

    Task<Guid?> CreateThemeAsync(ServiceDataModelGroupInput input, string actorEmail, CancellationToken cancellationToken = default);

    Task<bool> UpdateThemeAsync(Guid themeId, ServiceDataModelGroupInput input, string actorEmail, CancellationToken cancellationToken = default);

    Task<bool> SetThemeActiveAsync(Guid themeId, bool isActive, string actorEmail, CancellationToken cancellationToken = default);

    Task<bool> ReorderThemesAsync(IReadOnlyList<Guid> orderedThemeIds, string actorEmail, CancellationToken cancellationToken = default);

    Task<Guid?> AddFieldAsync(Guid themeId, ServiceDataModelFieldInput input, string actorEmail, CancellationToken cancellationToken = default);

    Task<bool> UpdateFieldAsync(Guid fieldId, ServiceDataModelFieldInput input, string actorEmail, CancellationToken cancellationToken = default);

    Task<bool> SetFieldDisabledAsync(Guid fieldId, bool isDisabled, string actorEmail, CancellationToken cancellationToken = default);

    Task<bool> ReorderFieldsAsync(Guid themeId, IReadOnlyList<Guid> orderedFieldIds, string actorEmail, CancellationToken cancellationToken = default);

    Task<Guid?> AddFieldOptionAsync(Guid fieldId, ServiceDataModelFieldOptionInput input, string actorEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a portable JSON package of the shared base schema (themes, questions, capabilities).
    /// Excludes answers, assignments, and FIPS user-group taxonomy.
    /// </summary>
    Task<CensusThemeSchemaPackage?> ExportSchemaAsync(
        string actorEmail,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a schema package to this environment’s shared base set.
    /// Matches themes/questions by stable key; upserts capabilities by reference (preserves local ids).
    /// Does not delete entities absent from the file unless <paramref name="disableNotInFile"/> is true.
    /// </summary>
    Task<CensusThemeSchemaImportResult> ImportSchemaAsync(
        CensusThemeSchemaPackage package,
        bool disableNotInFile,
        string actorEmail,
        CancellationToken cancellationToken = default);
}

public sealed class CoreCensusThemeListItem
{
    public Guid Id { get; init; }
    public string StableKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Guidance { get; init; }
    public int SortOrder { get; init; }
    public int FieldCount { get; init; }
    public bool IsActive { get; init; } = true;
}
