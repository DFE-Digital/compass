using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Compass.Services;

/// <summary>
/// Service for generating training recommendations/nudges based on capability gaps
/// </summary>
public class NudgingService : INudgingService
{
    private readonly CompassDbContext _context;
    private readonly ILogger<NudgingService> _logger;

    public NudgingService(CompassDbContext context, ILogger<NudgingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TrainingNudge>> GenerateNudgesForUserAsync(int userId)
    {
        var nudges = new List<TrainingNudge>();

        // Get user profile
        var profile = await _context.UserProfessionalProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return nudges;
        }

        // Get active courses
        var activeCourses = await _context.TrainingCourses
            .Where(tc => tc.Active)
            .ToListAsync();

        // Get existing active nudges for this user
        var existingNudgeCourseIds = await _context.TrainingNudges
            .Where(tn => tn.UserId == userId && tn.IsActive)
            .Select(tn => tn.CourseId)
            .ToListAsync();

        // Get courses user has already requested or completed
        var userCourseIds = await _context.TrainingRequests
            .Where(tr => tr.UserId == userId)
            .Select(tr => tr.CourseId)
            .Where(cid => cid.HasValue)
            .Select(cid => cid!.Value)
            .Union(
                _context.TrainingRecords
                    .Where(tr => tr.UserId == userId)
                    .Select(tr => tr.CourseId)
                    .Where(cid => cid.HasValue)
                    .Select(cid => cid!.Value)
            )
            .Distinct()
            .ToListAsync();

        // Parse capability gaps
        var capabilityGaps = new List<string>();
        
        // Use new CapabilityGaps collection if available
        if (profile.CapabilityGaps != null && profile.CapabilityGaps.Any())
        {
            capabilityGaps.AddRange(profile.CapabilityGaps.Select(cg => cg.Description));
        }
        // Fallback to legacy string field
        else if (!string.IsNullOrEmpty(profile.CapabilityGapsLegacy))
        {
            try
            {
                // Try to parse as JSON array first
                var gapsArray = JsonSerializer.Deserialize<string[]>(profile.CapabilityGapsLegacy);
                if (gapsArray != null)
                {
                    capabilityGaps.AddRange(gapsArray);
                }
            }
            catch
            {
                // If not JSON, treat as comma-separated
                capabilityGaps.AddRange(profile.CapabilityGapsLegacy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        // Parse profession tags from courses
        foreach (var course in activeCourses)
        {
            // Skip if user already has this course
            if (userCourseIds.Contains(course.Id))
            {
                continue;
            }

            // Skip if already has an active nudge for this course
            if (existingNudgeCourseIds.Contains(course.Id))
            {
                continue;
            }

            var shouldNudge = false;
            var reason = "";
            var matchedGap = "";

            // Check profession alignment
            if (!string.IsNullOrEmpty(course.ProfessionTags) && !string.IsNullOrEmpty(profile.Profession))
            {
                var professionTags = course.ProfessionTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (professionTags.Any(pt => pt.Equals(profile.Profession, StringComparison.OrdinalIgnoreCase)))
                {
                    shouldNudge = true;
                    reason = "Profession alignment";
                }
            }

            // Check capability gap alignment
            if (!string.IsNullOrEmpty(course.CapabilityTags) && capabilityGaps.Any())
            {
                var capabilityTags = course.CapabilityTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var gap in capabilityGaps)
                {
                    if (capabilityTags.Any(ct => ct.Contains(gap, StringComparison.OrdinalIgnoreCase) || gap.Contains(ct, StringComparison.OrdinalIgnoreCase)))
                    {
                        shouldNudge = true;
                        reason = "Capability gap";
                        matchedGap = gap;
                        break;
                    }
                }
            }

            if (shouldNudge)
            {
                var nudge = new TrainingNudge
                {
                    UserId = userId,
                    CourseId = course.Id,
                    Reason = reason,
                    CapabilityGap = matchedGap,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                nudges.Add(nudge);
            }
        }

        // Save new nudges
        if (nudges.Any())
        {
            _context.TrainingNudges.AddRange(nudges);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Generated {Count} nudges for user {UserId}", nudges.Count, userId);
        }

        // Return all active nudges for this user
        return await _context.TrainingNudges
            .Where(tn => tn.UserId == userId && tn.IsActive)
            .Include(tn => tn.Course)
            .OrderByDescending(tn => tn.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> DismissNudgeAsync(int nudgeId, int userId)
    {
        var nudge = await _context.TrainingNudges
            .FirstOrDefaultAsync(tn => tn.Id == nudgeId && tn.UserId == userId);

        if (nudge == null)
        {
            return false;
        }

        nudge.IsActive = false;
        nudge.DismissedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nudge {NudgeId} dismissed by user {UserId}", nudgeId, userId);
        return true;
    }

    public async Task<bool> AcceptNudgeAsync(int nudgeId, int userId)
    {
        var nudge = await _context.TrainingNudges
            .Include(tn => tn.Course)
            .FirstOrDefaultAsync(tn => tn.Id == nudgeId && tn.UserId == userId);

        if (nudge == null || nudge.Course == null)
        {
            return false;
        }

        nudge.IsActive = false;
        nudge.AcceptedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nudge {NudgeId} accepted by user {UserId}", nudgeId, userId);
        return true;
    }
}

