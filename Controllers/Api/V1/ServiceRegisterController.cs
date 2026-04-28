using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services.Fips;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class ServiceRegisterController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly IFipsProductWriteService _fipsProductWrite;
    private readonly ILogger<ServiceRegisterController> _logger;

    public ServiceRegisterController(
        CompassDbContext context,
        IFipsProductWriteService fipsProductWrite,
        ILogger<ServiceRegisterController> logger)
    {
        _context = context;
        _fipsProductWrite = fipsProductWrite;
        _logger = logger;
    }

    /// <summary>
    /// List FIPS / service register (CMDB) products. Grant <c>read</c> on resource <c>ServiceRegister</c> for the API token.
    /// </summary>
    [HttpGet("products")]
    [RequireApiPermission("ServiceRegister", "read")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string[]? status = null,
        CancellationToken cancellationToken = default)
    {
        if (pageSize > 1000) pageSize = 1000;
        if (pageSize < 1) pageSize = 50;
        if (page < 1) page = 1;

        List<CMDBProductStatus>? statusFilter = null;
        if (status is { Length: > 0 })
        {
            statusFilter = new List<CMDBProductStatus>();
            foreach (var s in status)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                if (!Enum.TryParse<CMDBProductStatus>(s.Trim(), true, out var st))
                {
                    return BadRequest(new
                    {
                        error = new
                        {
                            code = "INVALID_STATUS",
                            message = $"Unknown status value '{s}'. Use New, Active, Inactive, or Rejected."
                        }
                    });
                }
                if (!statusFilter.Contains(st)) statusFilter.Add(st);
            }
        }

        var baseQuery = _context.CMDBProducts.AsNoTracking();
        if (statusFilter is { Count: > 0 })
            baseQuery = baseQuery.Where(p => statusFilter.Contains(p.Status));

        var totalRecords = await baseQuery.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var products = await baseQuery
            .OrderBy(p => p.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas)
            .ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Contacts)
            .ThenInclude(c => c.FipsContactRole)
            .ToListAsync(cancellationToken);

        var rows = products.Select(p => new
        {
            id = p.Id,
            uniqueId = p.UniqueID,
            productName = p.Title,
            phase = p.Phase?.Name,
            businessArea = string.Join(", ", p.BusinessAreas
                .OrderBy(ba => ba.FipsBusinessArea.Name)
                .Select(ba => ba.FipsBusinessArea.Name)),
            productUrl = p.ProductURL,
            status = p.Status.ToString(),
            contacts = p.Contacts
                .OrderBy(c => c.FipsContactRole != null ? c.FipsContactRole.Name : "")
                .ThenBy(c => c.UserEmail)
                .Select(c => new
                {
                    role = c.FipsContactRole != null ? c.FipsContactRole.Name : null,
                    roleId = c.FipsContactRoleId,
                    email = c.UserEmail,
                    name = c.UserName,
                    canManage = c.CanManage
                })
                .ToList()
        }).ToList();

        return Ok(new
        {
            data = rows,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalPages,
                totalRecords
            }
        });
    }

    /// <summary>
    /// Get a single FIPS / service register product by id. Grant <c>read</c> on resource <c>ServiceRegister</c> for the API token.
    /// </summary>
    [HttpGet("products/{id:guid}")]
    [RequireApiPermission("ServiceRegister", "read")]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await _context.CMDBProducts.AsNoTracking()
            .Where(x => x.Id == id)
            .Include(x => x.Phase)
            .Include(x => x.BusinessAreas)
            .ThenInclude(ba => ba.FipsBusinessArea)
            .Include(x => x.Contacts)
            .ThenInclude(c => c.FipsContactRole)
            .FirstOrDefaultAsync(cancellationToken);

        if (p == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Product not found."
                }
            });
        }

        var row = new
        {
            id = p.Id,
            uniqueId = p.UniqueID,
            productName = p.Title,
            phase = p.Phase?.Name,
            businessArea = string.Join(", ", p.BusinessAreas
                .OrderBy(ba => ba.FipsBusinessArea.Name)
                .Select(ba => ba.FipsBusinessArea.Name)),
            productUrl = p.ProductURL,
            status = p.Status.ToString(),
            contacts = p.Contacts
                .OrderBy(c => c.FipsContactRole != null ? c.FipsContactRole.Name : "")
                .ThenBy(c => c.UserEmail)
                .Select(c => new
                {
                    role = c.FipsContactRole != null ? c.FipsContactRole.Name : null,
                    roleId = c.FipsContactRoleId,
                    email = c.UserEmail,
                    name = c.UserName,
                    canManage = c.CanManage
                })
                .ToList()
        };

        return Ok(new { data = row });
    }

    /// <summary>
    /// Update a product’s service URL. Grant <c>update</c> on resource <c>ServiceRegister</c> for the API token.
    /// </summary>
    [HttpPatch("products/{id:guid}")]
    [RequireApiPermission("ServiceRegister", "update")]
    public async Task<IActionResult> PatchProduct(
        Guid id,
        [FromBody] ServiceRegisterProductUrlRequest? body,
        CancellationToken cancellationToken = default)
    {
        if (body == null)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_REQUEST",
                    message = "Request body is required (JSON: { \"productUrl\": \"...\" })."
                }
            });
        }

        var normalized = string.IsNullOrWhiteSpace(body.ProductUrl) ? null : body.ProductUrl.Trim();
        if (normalized != null && normalized.Length > 2000)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Product URL must be at most 2000 characters."
                }
            });
        }

        var token = HttpContext.Items["ApiToken"] as ApiToken;
        var actorEmail = token != null ? $"api-token:{token.Id}" : "api-token";
        var display = token != null && !string.IsNullOrWhiteSpace(token.Name)
            ? TruncateForAudit("API: " + token.Name)
            : "API token";

        var outcome = await _fipsProductWrite.TryUpdateProductUrlOnlyAsync(
            id, actorEmail, display, normalized, cancellationToken);

        if (outcome.NotFound)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = "Product not found."
                }
            });
        }

        _logger.LogInformation("Service register product URL API update for {ProductId}, changes: {Changes}", id, outcome.Changes);

        return Ok(new
        {
            data = new
            {
                id,
                productUrl = normalized,
                message = outcome.Changes.Count == 0 ? "No change (URL already matches)." : "Product URL updated."
            }
        });
    }

    private static string TruncateForAudit(string s) => s.Length > 200 ? s[..200] : s;
}

public class ServiceRegisterProductUrlRequest
{
    /// <summary>Public URL of the product (live service). Omit or set null/empty to clear.</summary>
    [StringLength(2000)]
    public string? ProductUrl { get; set; }
}
