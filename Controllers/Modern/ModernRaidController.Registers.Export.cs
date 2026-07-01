using Compass.Services.Modern;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernRaidController
{
    [HttpGet("registers/{id:int}/export")]
    public async Task<IActionResult> ExportRegisterSpreadsheet(int id, CancellationToken cancellationToken = default)
    {
        var userId = await ResolveCurrentUserIdAsync(cancellationToken);

        var register = await _db.RaidRegisters
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (register == null)
            return NotFound();

        if (!CanViewRegister(userId))
            return Forbid();

        await RaidRegisterScopedEntitySync.SyncAsync(_db, id, userId, cancellationToken);

        var entityRows = await LoadRaidRegisterEntityRowsAsync(id, cancellationToken);
        var exportModel = new RaidRegisterSpreadsheetExportModel
        {
            RegisterName = register.Name,
            Risks = entityRows.Risks,
            Issues = entityRows.Issues,
            Assumptions = entityRows.Assumptions,
            NearMisses = entityRows.NearMisses,
            Dependencies = entityRows.Dependencies
        };

        var bytes = RaidRegisterSpreadsheetExcelExport.BuildWorkbook(exportModel);
        var fileName = RaidRegisterSpreadsheetExcelExport.BuildFileName(register.Name);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
