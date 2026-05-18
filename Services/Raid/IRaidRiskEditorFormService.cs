using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;

namespace Compass.Services.Raid;

/// <summary>Shared RAID risk editor lookups and create-from-form persistence (RAID register + work item log-risk).</summary>
public interface IRaidRiskEditorFormService
{
    Task PrepareRiskEditorLookupsAsync(
        Controller controller,
        int? ownerUserId,
        int? sroUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RiskIssueNamedIntOption>> BuildRiskCreateTierOptionsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a risk from the standard editor form. Returns null when validation failed (see <paramref name="modelState"/>).
    /// When <paramref name="forceWorkProjectId"/> is set, association is fixed to that work item regardless of posted association fields.
    /// </summary>
    Task<Risk?> TryCreateRiskFromEditorFormAsync(
        ModelStateDictionary modelState,
        ClaimsPrincipal user,
        ModernRaidRiskEditorForm form,
        int? forceWorkProjectId,
        CancellationToken cancellationToken);
}
