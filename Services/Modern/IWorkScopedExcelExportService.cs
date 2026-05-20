using Compass.Models;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Services.Modern;

/// <summary>Standard multi-sheet Excel export for a set of work items (projects).</summary>
public interface IWorkScopedExcelExportService
{
    Task<byte[]> BuildWorkbookAsync(
        IReadOnlyList<int> projectIds,
        User currentUser,
        string userEmail,
        IUrlHelper urlHelper,
        CancellationToken cancellationToken = default);
}
