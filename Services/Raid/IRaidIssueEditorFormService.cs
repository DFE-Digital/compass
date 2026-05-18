using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace Compass.Services.Raid;

/// <summary>Shared RAID issue editor lookups and create-from-form persistence (RAID register + work item log-issue).</summary>
public interface IRaidIssueEditorFormService
{
    Task PrepareIssueEditorLookupsAsync(
        Controller controller,
        int? ownerUserId,
        int? sroUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates an issue from the standard editor form. Returns null when validation failed.
    /// When <paramref name="forceWorkProjectId"/> is set, association is fixed to that work item.
    /// </summary>
    Task<Issue?> TryCreateIssueFromEditorFormAsync(
        ModelStateDictionary modelState,
        ClaimsPrincipal user,
        ModernRaidIssueEditorForm form,
        int? forceWorkProjectId,
        CancellationToken cancellationToken);

    /// <summary>Creates an issue from a risk and optionally closes the risk as materialised.</summary>
    Task<Issue?> TryCreateIssueFromRiskAsync(
        ModelStateDictionary modelState,
        ClaimsPrincipal user,
        ModernRaidMakeIssueFromRiskForm form,
        CancellationToken cancellationToken);
}
