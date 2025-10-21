using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Services;
using Compass.Data;

namespace Compass.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly IGraphService _graphService;
    private readonly CompassDbContext _context;
    private readonly ILogger<StaffController> _logger;

    public StaffController(IGraphService graphService, CompassDbContext context, ILogger<StaffController> logger)
    {
        _graphService = graphService;
        _context = context;
        _logger = logger;
    }

    // GET: api/staff/search?q=john
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            var searchTerm = q ?? "";
            _logger.LogInformation("Searching staff with term: {SearchTerm}", searchTerm);
            
            var staff = await _graphService.SearchStaffAsync(searchTerm, 20);
            _logger.LogInformation("Found {Count} staff members", staff.Count);

            // Match staff to existing users in database by email
            var results = new List<object>();
            foreach (var s in staff)
            {
                // Try to find existing user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == s.Email);
                
                results.Add(new
                {
                    id = user?.Id ?? 0, // Use 0 if user doesn't exist yet
                    text = $"{s.DisplayName} ({s.Email})",
                    displayName = s.DisplayName,
                    email = s.Email,
                    jobTitle = s.JobTitle,
                    department = s.Department,
                    existsInDatabase = user != null
                });
            }

            return Ok(new { results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching staff");
            return StatusCode(500, new { error = "An error occurred while searching staff" });
        }
    }
}

