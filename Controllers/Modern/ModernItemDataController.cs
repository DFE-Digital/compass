using System.Text;
using System.Text.Json;
using Compass.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Session-authenticated JSON snapshots for item detail download menus.</summary>
[Authorize]
[Route("modern/data")]
public sealed class ModernItemDataController : Controller
{
    private readonly CompassDbContext _db;

    public ModernItemDataController(CompassDbContext db) => _db = db;

    [HttpGet("{kind}/{id}.json")]
    public async Task<IActionResult> ItemJson(string kind, string id, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(id, out var guidId))
            return await ItemJsonGuid(kind, guidId, cancellationToken);

        if (!int.TryParse(id, out var intId))
            return NotFound();

        return await ItemJsonInt(kind, intId, cancellationToken);
    }

    private async Task<IActionResult> ItemJsonInt(string kind, int intId, CancellationToken cancellationToken) =>
        kind.ToLowerInvariant() switch
        {
            "work" => await WorkJson(intId, cancellationToken),
            "risks" => await RiskJson(intId, cancellationToken),
            "issues" => await IssueJson(intId, cancellationToken),
            "assumptions" => await AssumptionJson(intId, cancellationToken),
            "dependencies" => await DependencyJson(intId, cancellationToken),
            "near-misses" => await NearMissJson(intId, cancellationToken),
            _ => NotFound()
        };

    private async Task<IActionResult> ItemJsonGuid(string kind, Guid guidId, CancellationToken cancellationToken) =>
        kind.ToLowerInvariant() switch
        {
            "products" => await ProductJson(guidId, cancellationToken),
            _ => NotFound()
        };

    private async Task<IActionResult> WorkJson(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Aim,
                p.Status,
                p.RagStatusLookupId,
                p.DeliveryPriorityId,
                p.BusinessAreaId,
                p.UpdatedAt,
                p.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"work-item-{id:D8}.json");
    }

    private async Task<IActionResult> RiskJson(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Risks
            .AsNoTracking()
            .Where(r => r.Id == id && !r.IsDeleted)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                r.Status,
                r.RiskScore,
                r.FipsId,
                r.ObjectiveId,
                r.UpdatedAt,
                r.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"risk-{id:D4}.json");
    }

    private async Task<IActionResult> IssueJson(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Issues
            .AsNoTracking()
            .Where(i => i.Id == id && !i.IsDeleted)
            .Select(i => new
            {
                i.Id,
                i.Title,
                i.Description,
                i.Status,
                i.Severity,
                i.FipsId,
                i.ObjectiveId,
                i.UpdatedAt,
                i.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"issue-{id:D4}.json");
    }

    private async Task<IActionResult> AssumptionJson(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Assumptions
            .AsNoTracking()
            .Where(a => a.Id == id && !a.IsDeleted)
            .Select(a => new { a.Id, a.Description, a.AssumptionStatusId, a.UpdatedAt, a.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"assumption-{id}.json");
    }

    private async Task<IActionResult> DependencyJson(int id, CancellationToken cancellationToken)
    {
        var item = await _db.Dependencies
            .AsNoTracking()
            .Where(d => d.Id == id)
            .Select(d => new
            {
                d.Id,
                d.SourceEntityType,
                d.SourceEntityId,
                d.TargetEntityType,
                d.TargetEntityId,
                d.DependencyType,
                d.DependencyLinkTypeId,
                d.UpdatedAt,
                d.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"dependency-{id}.json");
    }

    private async Task<IActionResult> NearMissJson(int id, CancellationToken cancellationToken)
    {
        var item = await _db.NearMisses
            .AsNoTracking()
            .Where(n => n.Id == id && !n.IsDeleted)
            .Select(n => new { n.Id, n.Reference, n.Impact, n.DateLogged, n.UpdatedAt, n.CreatedAt })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"near-miss-{id}.json");
    }

    private async Task<IActionResult> ProductJson(Guid id, CancellationToken cancellationToken)
    {
        var item = await _db.CMDBProducts
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.CMDBID,
                p.Status,
                p.IsEnterpriseService,
                p.UpdatedAt,
                p.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return item == null ? NotFound() : DownloadJson(item, $"product-{id:N}.json");
    }

    private static readonly JsonSerializerOptions JsonDownloadOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static FileContentResult DownloadJson(object data, string fileName)
    {
        var json = JsonSerializer.Serialize(data, JsonDownloadOptions);
        return new FileContentResult(Encoding.UTF8.GetBytes(json), "application/json")
        {
            FileDownloadName = fileName,
        };
    }
}
