using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;

namespace Compass.Controllers;

[Authorize]
[Route("api/action-sources")]
[ApiController]
public class ActionSourceItemsApiController : ControllerBase
{
    private readonly CompassDbContext _context;

    public ActionSourceItemsApiController(CompassDbContext context)
    {
        _context = context;
    }

    [HttpGet("items")]
    public async Task<IActionResult> GetSourceItems()
    {
        try
        {
            var risks = await _context.Risks
                .Where(r => !r.IsDeleted && r.Status != "closed")
                .OrderBy(r => r.Title)
                .Select(r => new { id = r.Id, title = r.Title })
                .ToListAsync();

            var issues = await _context.Issues
                .Where(i => !i.IsDeleted && i.Status != "closed" && i.Status != "resolved")
                .OrderBy(i => i.Title)
                .Select(i => new { id = i.Id, title = i.Title })
                .ToListAsync();

            var milestones = await _context.Milestones
                .Where(m => !m.IsDeleted && m.Status != "complete" && m.Status != "cancelled")
                .OrderBy(m => m.Name)
                .Select(m => new { id = m.Id, title = m.Name })
                .ToListAsync();

            return Ok(new
            {
                risks,
                issues,
                milestones
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to load source items", message = ex.Message });
        }
    }
}

