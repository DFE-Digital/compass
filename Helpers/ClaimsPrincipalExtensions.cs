using System.Security.Claims;

namespace Compass.Helpers;

/// <summary>
/// Extension methods for ClaimsPrincipal to check Compass-specific roles
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Checks if the user has an Admin or SuperAdmin role
    /// </summary>
    public static bool IsCompassAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") || user.IsInRole("SuperAdmin");
    }

    /// <summary>
    /// Checks if the user has a SuperAdmin role
    /// </summary>
    public static bool IsCompassSuperAdmin(this ClaimsPrincipal user)
    {
        return user.IsInRole("SuperAdmin");
    }

    /// <summary>
    /// Checks if the user has an Admin role (but not SuperAdmin)
    /// </summary>
    public static bool IsCompassAdminOnly(this ClaimsPrincipal user)
    {
        return user.IsInRole("Admin") && !user.IsInRole("SuperAdmin");
    }

    /// <summary>
    /// Gets the Compass role from claims (Admin, SuperAdmin, Reporter, or Visitor)
    /// </summary>
    public static string? GetCompassRole(this ClaimsPrincipal user)
    {
        if (user.IsInRole("SuperAdmin"))
            return "SuperAdmin";
        if (user.IsInRole("Admin"))
            return "Admin";
        if (user.IsInRole("Reporter"))
            return "Reporter";
        if (user.IsInRole("Visitor"))
            return "Visitor";
        
        return null;
    }
}

