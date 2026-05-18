using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.Services;

namespace Compass.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductsApiService _productsApiService;
    private readonly CompassDbContext _context;
    private readonly IGlobalFeatureToggleService _globalFeatureToggle;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductsApiService productsApiService,
        CompassDbContext context,
        IGlobalFeatureToggleService globalFeatureToggle,
        ILogger<ProductsController> logger)
    {
        _productsApiService = productsApiService;
        _context = context;
        _globalFeatureToggle = globalFeatureToggle;
        _logger = logger;
    }

    // GET: api/products/search?q=searchterm — browser/session (Compass UI); not the Bearer Service Register API.
    [Authorize]
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            var searchTerm = (q ?? "").Trim();
            _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Ok(new { results = new List<object>() });
            }

            var allProducts = await _productsApiService.GetProductsAsync(null);

            var filteredProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId) &&
                           (p.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                            p.FipsId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            p.Phase?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                            p.CategoryValues?.Any(cv =>
                                cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true &&
                                cv.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) == true))
                .Take(20)
                .Select(p =>
                {
                    var businessArea = p.CategoryValues?
                        .FirstOrDefault(cv =>
                            cv.CategoryType?.Name?.Equals("Business area", StringComparison.OrdinalIgnoreCase) == true)?.Name ?? "";
                    var phase = p.Phase ?? "Not specified";

                    return new
                    {
                        documentId = p.DocumentId ?? p.FipsId,
                        fipsId = p.FipsId,
                        title = p.Title,
                        phase = phase,
                        businessArea = businessArea,
                        text = $"{p.Title} ({p.DocumentId ?? p.FipsId})" +
                               (!string.IsNullOrEmpty(phase) && phase != "Not specified" ? $" - {phase}" : "") +
                               (!string.IsNullOrEmpty(businessArea) ? $" - {businessArea}" : "")
                    };
                })
                .ToList();

            _logger.LogInformation("Found {Count} matching products", filteredProducts.Count);

            return Ok(new { results = filteredProducts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { error = "An error occurred while searching products" });
        }
    }

    /// <summary>
    /// All FIPS service-register lookup values (same source as Admin → FIPS configuration).
    /// </summary>
    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips")]
    [HttpGet("fips/configuration")]
    public async Task<IActionResult> GetFipsConfiguration(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;

        try
        {
            return Ok(new
            {
                channels = await QueryFipsChannelsAsync(cancellationToken),
                types = await QueryFipsTypesAsync(cancellationToken),
                businessAreas = await QueryFipsBusinessAreasAsync(cancellationToken),
                userGroups = await QueryFipsUserGroupsAsync(cancellationToken),
                contactRoles = await QueryFipsContactRolesAsync(cancellationToken),
                categorisationGroups = await QueryFipsCategorisationAsync(cancellationToken)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS configuration bundle");
            return StatusCode(500, new { error = "An error occurred while loading FIPS configuration" });
        }
    }

    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips/channels")]
    public async Task<IActionResult> GetFipsChannelsEndpoint(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;
        try
        {
            var data = await QueryFipsChannelsAsync(cancellationToken);
            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS channels");
            return StatusCode(500, new { error = "An error occurred while loading FIPS channels" });
        }
    }

    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips/types")]
    public async Task<IActionResult> GetFipsTypesEndpoint(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;
        try
        {
            var data = await QueryFipsTypesAsync(cancellationToken);
            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS types");
            return StatusCode(500, new { error = "An error occurred while loading FIPS types" });
        }
    }

    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips/business-areas")]
    public async Task<IActionResult> GetFipsBusinessAreasEndpoint(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;
        try
        {
            var data = await QueryFipsBusinessAreasAsync(cancellationToken);
            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS business areas");
            return StatusCode(500, new { error = "An error occurred while loading FIPS business areas" });
        }
    }

    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips/user-groups")]
    public async Task<IActionResult> GetFipsUserGroupsEndpoint(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;
        try
        {
            var data = await QueryFipsUserGroupsAsync(cancellationToken);
            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS user groups");
            return StatusCode(500, new { error = "An error occurred while loading FIPS user groups" });
        }
    }

    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips/contact-roles")]
    public async Task<IActionResult> GetFipsContactRolesEndpoint(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;
        try
        {
            var data = await QueryFipsContactRolesAsync(cancellationToken);
            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS contact roles");
            return StatusCode(500, new { error = "An error occurred while loading FIPS contact roles" });
        }
    }

    [RequireApiPermission("ServiceRegister", "read")]
    [HttpGet("fips/categorisation")]
    public async Task<IActionResult> GetFipsCategorisationEndpoint(CancellationToken cancellationToken = default)
    {
        var blocked = await RequireFipsFeatureAsync();
        if (blocked != null)
            return blocked;
        try
        {
            var data = await QueryFipsCategorisationAsync(cancellationToken);
            return Ok(new { data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading FIPS categorisation");
            return StatusCode(500, new { error = "An error occurred while loading FIPS categorisation" });
        }
    }

    private async Task<IActionResult?> RequireFipsFeatureAsync()
    {
        // Bearer Service Register API tokens bypass the UI feature gate (permission already enforced by RequireApiPermission).
        if (HttpContext.Items["ApiToken"] is ApiToken)
            return null;

        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Fips, User))
        {
            return StatusCode(403, new
            {
                error = "The Compass service register (FIPS) is not available for this user or environment."
            });
        }

        return null;
    }

    private async Task<List<FipsLookupApiRow>> QueryFipsChannelsAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.FipsChannels.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description, x.DisplayOrder, x.Active })
            .ToListAsync(cancellationToken);
        return rows.Select(x => new FipsLookupApiRow(x.Id, x.Name, x.Description, x.DisplayOrder, x.Active)).ToList();
    }

    private async Task<List<FipsLookupApiRow>> QueryFipsTypesAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.FipsTypes.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description, x.DisplayOrder, x.Active })
            .ToListAsync(cancellationToken);
        return rows.Select(x => new FipsLookupApiRow(x.Id, x.Name, x.Description, x.DisplayOrder, x.Active)).ToList();
    }

    private async Task<List<FipsBusinessAreaApiRow>> QueryFipsBusinessAreasAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.FipsBusinessAreas.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.BusinessAreaLookupId, x.Name, x.Description, x.DisplayOrder, x.Active })
            .ToListAsync(cancellationToken);
        return rows.Select(x => new FipsBusinessAreaApiRow(
            x.Id,
            x.BusinessAreaLookupId,
            x.Name,
            x.Description,
            x.DisplayOrder,
            x.Active)).ToList();
    }

    private async Task<List<FipsUserGroupApiRow>> QueryFipsUserGroupsAsync(CancellationToken cancellationToken)
    {
        var roots = await _context.FipsUserGroups.AsNoTracking()
            .Include(g => g.Children)
            .Include(g => g.Synonyms)
            .Where(g => g.ParentId == null)
            .OrderBy(g => g.DisplayOrder)
            .ToListAsync(cancellationToken);

        return roots.Select(g => new FipsUserGroupApiRow(
                g.Id,
                g.Name,
                g.Description,
                g.DisplayOrder,
                g.Active,
                g.Children.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).Select(c => c.Name).ToList(),
                g.Synonyms.Select(s => s.Synonym).OrderBy(s => s).ToList()))
            .ToList();
    }

    private async Task<List<FipsContactRoleApiRow>> QueryFipsContactRolesAsync(CancellationToken cancellationToken)
    {
        var rows = await _context.FipsContactRoles.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.AllowMultiple,
                x.DisplayOrder,
                x.Active
            })
            .ToListAsync(cancellationToken);
        return rows.Select(x => new FipsContactRoleApiRow(
            x.Id,
            x.Name,
            x.Description,
            x.AllowMultiple,
            x.DisplayOrder,
            x.Active)).ToList();
    }

    private async Task<List<FipsCategorisationGroupApiRow>> QueryFipsCategorisationAsync(CancellationToken cancellationToken)
    {
        var groups = await _context.FipsCategorisationGroups.AsNoTracking()
            .Include(g => g.Items)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return groups.Select(g => new FipsCategorisationGroupApiRow(
                g.Id,
                g.Name,
                g.Description,
                g.DisplayOrder,
                g.Active,
                g.Items
                    .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Name)
                    .Select(i => new FipsCategorisationItemApiRow(
                        i.Id,
                        i.Name,
                        i.Description,
                        i.DisplayOrder,
                        i.Active,
                        i.FipsCategorisationGroupId))
                    .ToList()))
            .ToList();
    }

    private sealed record FipsLookupApiRow(int Id, string Name, string? Description, int DisplayOrder, bool Active);

    private sealed record FipsBusinessAreaApiRow(
        int Id,
        int? BusinessAreaLookupId,
        string Name,
        string? Description,
        int DisplayOrder,
        bool Active);

    private sealed record FipsUserGroupApiRow(
        int Id,
        string Name,
        string? Description,
        int DisplayOrder,
        bool Active,
        IReadOnlyList<string> Children,
        IReadOnlyList<string> Synonyms);

    private sealed record FipsContactRoleApiRow(
        int Id,
        string Name,
        string? Description,
        bool AllowMultiple,
        int DisplayOrder,
        bool Active);

    private sealed record FipsCategorisationGroupApiRow(
        int Id,
        string Name,
        string? Description,
        int DisplayOrder,
        bool Active,
        IReadOnlyList<FipsCategorisationItemApiRow> Items);

    private sealed record FipsCategorisationItemApiRow(
        int Id,
        string Name,
        string? Description,
        int DisplayOrder,
        bool Active,
        int CategorisationGroupId);
}
