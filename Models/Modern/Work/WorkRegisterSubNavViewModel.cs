namespace Compass.Models.Modern.Work;

/// <summary>Tab links for the work register (All work, Business areas, Directorates).</summary>
public sealed class WorkRegisterSubNavViewModel
{
    public string ActiveTab { get; init; } = "active";

    public bool IsMyWork { get; init; }

    public int RegisterActivePausedCountMine { get; init; }

    public int RegisterActivePausedCountOrg { get; init; }

    public int CompletedCount { get; init; }

    public int CancelledCount { get; init; }

    public int AllItemsCount { get; init; }

    public string? FilterSearch { get; init; }

    public int? FilterBusinessAreaId { get; init; }

    public int? FilterDirectorateId { get; init; }

    public int? FilterPhaseId { get; init; }

    public int? FilterRagId { get; init; }

    public int? FilterPriorityId { get; init; }

    public string? FilterMonthlyUpdate { get; init; }

    public int? FilterPrimaryContactUserId { get; init; }

    public int? FilterTagId { get; init; }

    public IReadOnlyList<int> FilterTagIds { get; init; } = [];

    public string RegisterSortField { get; init; } = "title";

    public bool RegisterSortDescending { get; init; }

    public string ListAction { get; init; } = "AllWork";

    public string ListController { get; init; } = "ModernWork";

    /// <summary>Business area scope key for <see cref="ListAction"/> BusinessAreas (id, all, unassigned).</summary>
    public string? BusinessAreaFilterKey { get; init; }

    /// <summary>Directorate scope key for <see cref="ListAction"/> Directorates (id or all).</summary>
    public string? DirectorateFilterKey { get; init; }

    /// <summary>Thematic tag scope key for <see cref="ListAction"/> ByTheme (id or all).</summary>
    public string? ThemeFilterKey { get; init; }

    public bool HideYourWorkTab { get; init; }

    public static WorkRegisterSubNavViewModel FromRegister(
        WorkRegisterViewModel vm,
        string activeTab,
        bool isMyWork,
        string? listAction = null,
        string? businessAreaFilterKey = null,
        string? directorateFilterKey = null,
        string? themeFilterKey = null) =>
        new()
        {
            ActiveTab = activeTab,
            IsMyWork = isMyWork,
            RegisterActivePausedCountMine = vm.RegisterActivePausedCountMine,
            RegisterActivePausedCountOrg = vm.RegisterActivePausedCountOrg,
            CompletedCount = vm.CompletedCount,
            CancelledCount = vm.CancelledCount,
            AllItemsCount = vm.ActiveCount + vm.PausedCount + vm.CompletedCount + vm.CancelledCount,
            FilterSearch = vm.FilterSearch,
            FilterBusinessAreaId = vm.FilterBusinessAreaId,
            FilterDirectorateId = vm.FilterDirectorateId,
            FilterPhaseId = vm.FilterPhaseId,
            FilterRagId = vm.FilterRagId,
            FilterPriorityId = vm.FilterPriorityId,
            FilterMonthlyUpdate = vm.FilterMonthlyUpdate,
            FilterPrimaryContactUserId = vm.FilterPrimaryContactUserId,
            FilterTagId = vm.FilterTagId,
            FilterTagIds = vm.FilterTagIds,
            RegisterSortField = vm.RegisterSortField,
            RegisterSortDescending = vm.RegisterSortDescending,
            ListAction = listAction ?? "AllWork",
            BusinessAreaFilterKey = businessAreaFilterKey,
            DirectorateFilterKey = directorateFilterKey,
            ThemeFilterKey = themeFilterKey,
            HideYourWorkTab = string.Equals(listAction, "BusinessAreas", StringComparison.Ordinal)
                || string.Equals(listAction, "Directorates", StringComparison.Ordinal)
                || string.Equals(listAction, "ByTheme", StringComparison.Ordinal)
        };
}
