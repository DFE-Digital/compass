using System.Security.Claims;

namespace Compass.Services;

/// <summary>Reads global feature availability from <see cref="Models.Feature"/> and optional per-user allow list (Admin → Feature settings).</summary>
public interface IGlobalFeatureToggleService
{
    /// <summary>Whether the signed-in user may use the feature, including <c>On for some</c>. If there is no <c>Features</c> row, known codes use <see cref="Models.ApplicationFeatureToggleDefinition.DefaultEnabled"/>; unknown codes remain on for backwards compatibility.</summary>
    Task<bool> IsFeatureEnabledForUserAsync(string featureCode, int? userId);

    /// <summary>Resolves the Compass user from claims and calls <see cref="IsFeatureEnabledForUserAsync"/>.</summary>
    Task<bool> IsFeatureEnabledForPrincipalAsync(string featureCode, ClaimsPrincipal? user);
}
