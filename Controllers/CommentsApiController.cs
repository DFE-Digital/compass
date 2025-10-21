using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;

namespace Compass.Controllers;

[Authorize]
[Route("api/comments")]
[ApiController]
public class CommentsApiController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<CommentsApiController> _logger;

    public CommentsApiController(CompassDbContext context, ILogger<CommentsApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{entityType}/{entityId}")]
    public async Task<IActionResult> GetComments(string entityType, int entityId)
    {
        try
        {
            var comments = await _context.Comments
                .Include(c => c.CreatedByUser)
                .Where(c => c.EntityType == entityType && c.EntityId == entityId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.CommentText,
                    c.CreatedAt,
                    CreatedByUser = c.CreatedByUser != null ? new
                    {
                        c.CreatedByUser.Id,
                        c.CreatedByUser.Name,
                        c.CreatedByUser.Email
                    } : null
                })
                .ToListAsync();

            return Ok(comments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, new { error = "Failed to load comments" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddComment([FromBody] CommentRequest request)
    {
        try
        {
            var userEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail!.ToLower());

            if (currentUser == null)
            {
                return Unauthorized(new { error = "User not found" });
            }

            if (string.IsNullOrWhiteSpace(request.CommentText))
            {
                return BadRequest(new { error = "Comment text is required" });
            }

            var comment = new Comment
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                CommentText = request.CommentText,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var createdComment = await _context.Comments
                .Include(c => c.CreatedByUser)
                .Where(c => c.Id == comment.Id)
                .Select(c => new
                {
                    c.Id,
                    c.CommentText,
                    c.CreatedAt,
                    CreatedByUser = c.CreatedByUser != null ? new
                    {
                        c.CreatedByUser.Id,
                        c.CreatedByUser.Name,
                        c.CreatedByUser.Email
                    } : null
                })
                .FirstOrDefaultAsync();

            return Ok(createdComment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            return StatusCode(500, new { error = "Failed to add comment" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment == null || comment.IsDeleted)
            {
                return NotFound(new { error = "Comment not found" });
            }

            var userEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail!.ToLower());

            if (currentUser == null || comment.CreatedByUserId != currentUser.Id)
            {
                return Forbid();
            }

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment");
            return StatusCode(500, new { error = "Failed to delete comment" });
        }
    }

    public class CommentRequest
    {
        public string EntityType { get; set; } = string.Empty;
        public int EntityId { get; set; }
        public string CommentText { get; set; } = string.Empty;
    }
}

