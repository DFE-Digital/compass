using Compass.Models;
using Compass.Models.Modern.Work;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Services.Modern;

public interface IModernWorkService
{
    Task<WorkItem?> GetWorkItemAsync(int projectId, CancellationToken cancellationToken = default);

    Task PopulateWorkDashboardAsync(Controller controller, User currentUser, string userEmail, string? tab, CancellationToken cancellationToken = default);

    Task<WorkRegisterViewModel> BuildWorkRegisterAsync(
        bool isMyWork,
        string? search,
        int? portfolioId,
        int? directorateId,
        int? phaseId,
        int? ragId,
        int? priorityId,
        string? monthlyUpdate,
        User currentUser,
        string userEmail,
        IUrlHelper url,
        string? registerTab = null,
        int? registerPage = null,
        int registerPageSize = 20,
        int? businessAreaId = null,
        int? primaryContactUserId = null,
        int[]? tagIds = null,
        string? registerSort = null,
        bool registerSortDesc = false,
        CancellationToken cancellationToken = default);

    /// <summary>Rows for Excel export — loads only the requested tab (not the full register).</summary>
    Task<IReadOnlyList<WorkRegisterRow>> BuildWorkRegisterExportRowsAsync(
        bool isMyWork,
        string? search,
        int? portfolioId,
        int? directorateId,
        int? phaseId,
        int? ragId,
        int? priorityId,
        string? monthlyUpdate,
        User currentUser,
        string userEmail,
        IUrlHelper url,
        string exportTab,
        int? businessAreaId = null,
        int? primaryContactUserId = null,
        int[]? tagIds = null,
        string? registerSort = null,
        bool registerSortDesc = false,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a work item for the modern detail page and populates <see cref="Controller.ViewBag"/>.</summary>
    Task<WorkItem?> PopulateWorkDetailAsync(
        Controller controller,
        int projectId,
        User currentUser,
        string userEmail,
        string? tab,
        string? milestonestab,
        CancellationToken cancellationToken = default);

    /// <summary>Whether the user may edit work item fields (same rule as summary-list Change actions).</summary>
    Task<bool> CanUserEditWorkItemAsync(int projectId, string userEmail, CancellationToken cancellationToken = default);

    /// <summary>Projects on the user&apos;s watchlist, with optional filters (maps to <see cref="ModernWorkController.Watching"/>).</summary>
    Task<List<WorkItem>> GetWatchingWorkItemsAsync(
        User currentUser,
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>All non-deleted projects with optional filters (maps to <see cref="ModernWorkController.ByPriority"/>).</summary>
    Task<List<WorkItem>> GetByPriorityWorkItemsAsync(
        string? search,
        int? portfolioId,
        int? directorateId,
        int? priorityId,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>Ministerial flagship projects with optional filters (maps to <see cref="ModernWorkController.Flagship"/>).</summary>
    Task<List<WorkItem>> GetFlagshipWorkItemsAsync(
        string? search,
        int? portfolioId,
        int? directorateId,
        string? status,
        CancellationToken cancellationToken = default);
}
