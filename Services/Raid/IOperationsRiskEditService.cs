using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Compass.Services.Raid;

public interface IOperationsRiskEditService
{
    Task LoadEditorViewBagAsync(
        Controller controller,
        ModernRaidRiskEditorForm form,
        CancellationToken cancellationToken);

    Task<ModernRaidRiskEditorForm?> BuildFormAsync(int riskId, CancellationToken cancellationToken);

    /// <summary>Applies the same field rules as <c>ModernRaidController.RiskEditPost</c>, appends the operations reason, sets <see cref="Compass.Models.Risk.UpdatedByUserId" />.</summary>
    Task<bool> TrySaveAsync(
        int riskId,
        ModernRaidRiskEditorForm form,
        string? operationsChangeReason,
        int? editorUserId,
        string? editorEmail,
        ModelStateDictionary modelState,
        CancellationToken cancellationToken);
}
