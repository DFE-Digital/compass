using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Compass.Controllers.Api.V1;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ChatbotController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<ChatbotController> _logger;

    public ChatbotController(CompassDbContext context, ILogger<ChatbotController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("conversation")]
    public async Task<IActionResult> SaveConversation([FromBody] SaveConversationRequest request)
    {
        try
        {
            // Get user information from claims (multiple fallbacks for different auth providers)
            var userEmail = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;
            
            var userName = User.FindFirst("name")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;
            
            var userAzureObjectId = User.FindFirst("oid")?.Value
                ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            // Try to get the user from the database for more complete information
            User? dbUser = null;
            if (!string.IsNullOrEmpty(userEmail))
            {
                dbUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
            }
            else if (!string.IsNullOrEmpty(userAzureObjectId))
            {
                dbUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.AzureObjectId == userAzureObjectId);
            }

            // Use database user information if available, otherwise use claims
            var finalUserEmail = dbUser?.Email ?? userEmail;
            var finalUserName = dbUser?.Name ?? userName;
            var finalUserAzureObjectId = dbUser?.AzureObjectId ?? userAzureObjectId;

            var conversation = new ChatConversation
            {
                UserId = dbUser?.Id,
                UserEmail = finalUserEmail,
                UserName = finalUserName,
                UserAzureObjectId = finalUserAzureObjectId,
                Messages = JsonSerializer.Serialize(request.Messages),
                StartedAt = request.StartedAt,
                EndedAt = request.EndedAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ChatConversations.Add(conversation);
            await _context.SaveChangesAsync();

            return Ok(new { id = conversation.Id, message = "Conversation saved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chatbot conversation");
            return StatusCode(500, new { error = "Failed to save conversation" });
        }
    }

    [HttpGet("user-photo")]
    public async Task<IActionResult> GetUserPhoto()
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
            
            if (string.IsNullOrEmpty(userEmail))
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            if (user?.Photo != null && user.Photo.Length > 0)
            {
                return File(user.Photo, "image/png");
            }

            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user photo");
            return NotFound();
        }
    }

    [HttpGet("user-projects")]
    public async Task<IActionResult> GetUserProjects()
    {
        try
        {
            var userEmail = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("GetUserProjects: No user email found in claims");
                return Ok(new List<object>());
            }

            _logger.LogInformation($"GetUserProjects: Looking for projects for user email: {userEmail}");

            // Get the current user from database
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            _logger.LogInformation($"GetUserProjects: Found user in database: {currentUser != null}, UserId: {currentUser?.Id}");

            // Get project IDs from ProjectContacts table
            var projectIdsFromContacts = await _context.ProjectContacts
                .Where(pc => 
                    pc.Email.ToLower() == userEmail.ToLower() ||
                    (currentUser != null && pc.UserId == currentUser.Id))
                .Select(pc => pc.ProjectId)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation($"GetUserProjects: Found {projectIdsFromContacts.Count} projects from ProjectContacts");

            // Get project IDs from PrimaryContactUser
            var projectIdsFromPrimary = new List<int>();
            if (currentUser != null)
            {
                projectIdsFromPrimary = await _context.Projects
                    .Where(p => !p.IsDeleted && p.PrimaryContactUserId == currentUser.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            // Also check by email for PrimaryContactUser
            var projectIdsFromPrimaryEmail = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Where(p => !p.IsDeleted && 
                    p.PrimaryContactUser != null && 
                    p.PrimaryContactUser.Email.ToLower() == userEmail.ToLower())
                .Select(p => p.Id)
                .ToListAsync();

            // Combine all project IDs
            var allProjectIds = projectIdsFromContacts
                .Union(projectIdsFromPrimary)
                .Union(projectIdsFromPrimaryEmail)
                .Distinct()
                .ToList();

            _logger.LogInformation($"GetUserProjects: Total unique project IDs: {allProjectIds.Count}");

            if (!allProjectIds.Any())
            {
                return Ok(new List<object>());
            }

            // Now get the projects
            var projects = await _context.Projects
                .Include(p => p.DeliveryPriority)
                .Where(p => !p.IsDeleted && allProjectIds.Contains(p.Id))
                .OrderBy(p => p.Title)
                .Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    code = p.ProjectCode,
                    ragStatus = p.RagStatus,
                    priority = p.DeliveryPriority != null ? p.DeliveryPriority.Name : null
                })
                .ToListAsync();

            _logger.LogInformation($"GetUserProjects: Returning {projects.Count} projects");

            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user projects");
            return StatusCode(500, new { error = "Failed to retrieve projects" });
        }
    }

    [HttpGet("upcoming-milestones")]
    public async Task<IActionResult> GetUpcomingMilestones([FromQuery] int days = 30)
    {
        try
        {
            var userEmail = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Ok(new List<object>());
            }

            // Get the current user from database
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            // Get project IDs from ProjectContacts table
            var projectIdsFromContacts = await _context.ProjectContacts
                .Where(pc => 
                    pc.Email.ToLower() == userEmail.ToLower() ||
                    (currentUser != null && pc.UserId == currentUser.Id))
                .Select(pc => pc.ProjectId)
                .Distinct()
                .ToListAsync();

            // Get project IDs from PrimaryContactUser
            var projectIdsFromPrimary = new List<int>();
            if (currentUser != null)
            {
                projectIdsFromPrimary = await _context.Projects
                    .Where(p => !p.IsDeleted && p.PrimaryContactUserId == currentUser.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            // Also check by email for PrimaryContactUser
            var projectIdsFromPrimaryEmail = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Where(p => !p.IsDeleted && 
                    p.PrimaryContactUser != null && 
                    p.PrimaryContactUser.Email.ToLower() == userEmail.ToLower())
                .Select(p => p.Id)
                .ToListAsync();

            // Combine all project IDs
            var userProjectIds = projectIdsFromContacts
                .Union(projectIdsFromPrimary)
                .Union(projectIdsFromPrimaryEmail)
                .Distinct()
                .ToList();

            if (!userProjectIds.Any())
            {
                return Ok(new List<object>());
            }

            var cutoffDate = DateTime.UtcNow.AddDays(days);

            var milestones = await _context.Milestones
                .Include(m => m.Project)
                .Where(m => !m.IsDeleted &&
                    m.ProjectId.HasValue &&
                    userProjectIds.Contains(m.ProjectId.Value) &&
                    m.DueDate >= DateTime.UtcNow &&
                    m.DueDate <= cutoffDate &&
                    m.Status != "complete" &&
                    m.Status != "cancelled")
                .OrderBy(m => m.DueDate)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    description = m.Description,
                    dueDate = m.DueDate,
                    status = m.Status,
                    progressPercent = m.ProgressPercent,
                    projectId = m.ProjectId,
                    projectTitle = m.Project != null ? m.Project.Title : null,
                    projectCode = m.Project != null ? m.Project.ProjectCode : null
                })
                .ToListAsync();

            return Ok(milestones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming milestones");
            return StatusCode(500, new { error = "Failed to retrieve milestones" });
        }
    }

    [HttpGet("high-priority-issues")]
    public async Task<IActionResult> GetHighPriorityIssues()
    {
        try
        {
            var userEmail = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Ok(new List<object>());
            }

            // Get the current user from database
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            // Get project IDs from ProjectContacts table
            var projectIdsFromContacts = await _context.ProjectContacts
                .Where(pc => 
                    pc.Email.ToLower() == userEmail.ToLower() ||
                    (currentUser != null && pc.UserId == currentUser.Id))
                .Select(pc => pc.ProjectId)
                .Distinct()
                .ToListAsync();

            // Get project IDs from PrimaryContactUser
            var projectIdsFromPrimary = new List<int>();
            if (currentUser != null)
            {
                projectIdsFromPrimary = await _context.Projects
                    .Where(p => !p.IsDeleted && p.PrimaryContactUserId == currentUser.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            // Also check by email for PrimaryContactUser
            var projectIdsFromPrimaryEmail = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Where(p => !p.IsDeleted && 
                    p.PrimaryContactUser != null && 
                    p.PrimaryContactUser.Email.ToLower() == userEmail.ToLower())
                .Select(p => p.Id)
                .ToListAsync();

            // Combine all project IDs
            var userProjectIds = projectIdsFromContacts
                .Union(projectIdsFromPrimary)
                .Union(projectIdsFromPrimaryEmail)
                .Distinct()
                .ToList();

            if (!userProjectIds.Any())
            {
                return Ok(new List<object>());
            }

            var issues = await _context.Issues
                .Include(i => i.Project)
                .Where(i => !i.IsDeleted &&
                    i.ProjectId.HasValue &&
                    userProjectIds.Contains(i.ProjectId.Value) &&
                    i.Status != "resolved" &&
                    i.Status != "closed" &&
                    (i.Severity == "high" || i.Severity == "critical" || i.Priority == "high"))
                .OrderByDescending(i => i.Severity == "critical" ? 1 : i.Severity == "high" ? 2 : 3)
                .ThenByDescending(i => i.DetectedDate)
                .Select(i => new
                {
                    id = i.Id,
                    title = i.Title,
                    description = i.Description,
                    severity = i.Severity,
                    priority = i.Priority,
                    status = i.Status,
                    detectedDate = i.DetectedDate,
                    targetResolutionDate = i.TargetResolutionDate,
                    blocked = i.BlockedFlag,
                    projectId = i.ProjectId,
                    projectTitle = i.Project != null ? i.Project.Title : null,
                    projectCode = i.Project != null ? i.Project.ProjectCode : null
                })
                .Take(20)
                .ToListAsync();

            return Ok(issues);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving high priority issues");
            return StatusCode(500, new { error = "Failed to retrieve issues" });
        }
    }

    [HttpGet("high-priority-risks")]
    public async Task<IActionResult> GetHighPriorityRisks()
    {
        try
        {
            var userEmail = User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Ok(new List<object>());
            }

            // Get the current user from database
            var currentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());

            // Get project IDs from ProjectContacts table
            var projectIdsFromContacts = await _context.ProjectContacts
                .Where(pc => 
                    pc.Email.ToLower() == userEmail.ToLower() ||
                    (currentUser != null && pc.UserId == currentUser.Id))
                .Select(pc => pc.ProjectId)
                .Distinct()
                .ToListAsync();

            // Get project IDs from PrimaryContactUser
            var projectIdsFromPrimary = new List<int>();
            if (currentUser != null)
            {
                projectIdsFromPrimary = await _context.Projects
                    .Where(p => !p.IsDeleted && p.PrimaryContactUserId == currentUser.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
            }

            // Also check by email for PrimaryContactUser
            var projectIdsFromPrimaryEmail = await _context.Projects
                .Include(p => p.PrimaryContactUser)
                .Where(p => !p.IsDeleted && 
                    p.PrimaryContactUser != null && 
                    p.PrimaryContactUser.Email.ToLower() == userEmail.ToLower())
                .Select(p => p.Id)
                .ToListAsync();

            // Combine all project IDs
            var userProjectIds = projectIdsFromContacts
                .Union(projectIdsFromPrimary)
                .Union(projectIdsFromPrimaryEmail)
                .Distinct()
                .ToList();

            if (!userProjectIds.Any())
            {
                return Ok(new List<object>());
            }

            var risks = await _context.Risks
                .Include(r => r.Project)
                .Where(r => !r.IsDeleted &&
                    r.ProjectId.HasValue &&
                    userProjectIds.Contains(r.ProjectId.Value) &&
                    r.Status != "closed" &&
                    r.RiskScore >= 15) // High risk threshold
                .OrderByDescending(r => r.RiskScore)
                .ThenByDescending(r => r.IdentifiedDate ?? r.CreatedAt)
                .Select(r => new
                {
                    id = r.Id,
                    title = r.Title,
                    description = r.Description,
                    riskScore = r.RiskScore,
                    impactRating = r.ImpactRating,
                    likelihoodRating = r.LikelihoodRating,
                    status = r.Status,
                    proximityDate = r.ProximityDate,
                    identifiedDate = r.IdentifiedDate ?? r.CreatedAt,
                    projectId = r.ProjectId,
                    projectTitle = r.Project != null ? r.Project.Title : null,
                    projectCode = r.Project != null ? r.Project.ProjectCode : null
                })
                .Take(20)
                .ToListAsync();

            return Ok(risks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving high priority risks");
            return StatusCode(500, new { error = "Failed to retrieve risks" });
        }
    }

    [HttpGet("user-projects-by-name")]
    public async Task<IActionResult> GetUserProjectsByName([FromQuery] string? userName = null, [FromQuery] string? userEmail = null)
    {
        try
        {
            // If no name/email provided, return current user's projects
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userEmail))
            {
                return await GetUserProjects();
            }

            var projects = new List<object>();
            
            if (!string.IsNullOrEmpty(userEmail))
            {
                var projectList = await _context.Projects
                    .Include(p => p.ProjectContacts)
                    .Include(p => p.PrimaryContactUser)
                    .Include(p => p.DeliveryPriority)
                    .Where(p => !p.IsDeleted && (
                        p.ProjectContacts.Any(pc => pc.Email.ToLower() == userEmail.ToLower()) ||
                        (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == userEmail.ToLower())
                    ))
                    .OrderBy(p => p.Title)
                    .Select(p => new
                    {
                        id = p.Id,
                        title = p.Title,
                        code = p.ProjectCode,
                        ragStatus = p.RagStatus,
                        priority = p.DeliveryPriority != null ? p.DeliveryPriority.Name : null
                    })
                    .ToListAsync();
                
                projects = projectList.Cast<object>().ToList();
            }
            else if (!string.IsNullOrEmpty(userName))
            {
                // Search by name (partial match)
                var users = await _context.Users
                    .Where(u => u.Name.ToLower().Contains(userName.ToLower()) || 
                                u.Email.ToLower().Contains(userName.ToLower()))
                    .ToListAsync();

                if (users.Any())
                {
                    var userEmails = users.Select(u => u.Email.ToLower()).ToList();
                    
                    var projectList = await _context.Projects
                        .Include(p => p.ProjectContacts)
                        .Include(p => p.PrimaryContactUser)
                        .Include(p => p.DeliveryPriority)
                        .Where(p => !p.IsDeleted && (
                            p.ProjectContacts.Any(pc => userEmails.Contains(pc.Email.ToLower())) ||
                            (p.PrimaryContactUser != null && userEmails.Contains(p.PrimaryContactUser.Email.ToLower()))
                        ))
                        .OrderBy(p => p.Title)
                        .Select(p => new
                        {
                            id = p.Id,
                            title = p.Title,
                            code = p.ProjectCode,
                            ragStatus = p.RagStatus,
                            priority = p.DeliveryPriority != null ? p.DeliveryPriority.Name : null,
                            userNames = users.Where(u => 
                                p.ProjectContacts.Any(pc => pc.Email.ToLower() == u.Email.ToLower()) ||
                                (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == u.Email.ToLower())
                            ).Select(u => u.Name).ToList()
                        })
                        .ToListAsync();
                    
                    projects = projectList.Cast<object>().ToList();
                }
            }

            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user projects by name");
            return StatusCode(500, new { error = "Failed to retrieve projects" });
        }
    }
}

public class SaveConversationRequest
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

public class ChatMessageDto
{
    public string Type { get; set; } = string.Empty; // "user" or "bot"
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

