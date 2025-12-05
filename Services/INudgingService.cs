using Compass.Models;

namespace Compass.Services;

/// <summary>
/// Service for generating training recommendations/nudges based on capability gaps
/// </summary>
public interface INudgingService
{
    /// <summary>
    /// Generate nudges for a user based on their capability gaps and profession
    /// </summary>
    Task<List<TrainingNudge>> GenerateNudgesForUserAsync(int userId);

    /// <summary>
    /// Dismiss a nudge
    /// </summary>
    Task<bool> DismissNudgeAsync(int nudgeId, int userId);

    /// <summary>
    /// Accept a nudge (user clicked to request training)
    /// </summary>
    Task<bool> AcceptNudgeAsync(int nudgeId, int userId);
}

