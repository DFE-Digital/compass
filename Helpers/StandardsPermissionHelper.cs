using Compass.Services;
using System.Security.Claims;

namespace Compass.Helpers;

/// <summary>
/// Helper class for checking standards-related permissions
/// </summary>
public static class StandardsPermissionHelper
{
    private const string StandardOwnerGroupName = "Standard Owners";
    private const string StandardApproverGroupName = "Standard Approvers";
    private const string StandardPublisherGroupName = "Standard Publishers";
    private const string StandardsManagerGroupName = "Standards Managers";
    private const string AppAdminGroupName = "Admin";
    private const string CentralOperationsAdminGroupName = "Central Operations Admin";

    /// <summary>
    /// Modern UI at <c>/modern/standards/management</c> — Super admin, Admin, Central Operations Admin,
    /// Standard Owners/Approvers/Publishers, or Standards Managers.
    /// </summary>
    public static async Task<bool> CanAccessModernStandardsManagementAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        if (await permissionService.IsSuperAdminAsync(userEmail))
            return true;

        if (await permissionService.IsInGroupAsync(userEmail, AppAdminGroupName))
            return true;

        if (await permissionService.IsInGroupAsync(userEmail, CentralOperationsAdminGroupName))
            return true;

        return await permissionService.IsInGroupAsync(userEmail, StandardOwnerGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardApproverGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardPublisherGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardsManagerGroupName);
    }

    /// <summary>
    /// Check if user can draft standards (Standard Owner, Approver, Publisher, or Standards Manager)
    /// </summary>
    public static async Task<bool> CanDraftStandardsAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardOwnerGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardApproverGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardPublisherGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardsManagerGroupName);
    }

    /// <summary>
    /// Check if user is a Standard Owner
    /// </summary>
    public static async Task<bool> IsStandardOwnerAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardOwnerGroupName);
    }

    /// <summary>
    /// Check if user is a Standard Approver
    /// </summary>
    public static async Task<bool> IsStandardApproverAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardApproverGroupName);
    }

    /// <summary>
    /// Check if user is a Standard Publisher
    /// </summary>
    public static async Task<bool> IsStandardPublisherAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardPublisherGroupName);
    }

    /// <summary>
    /// Check if user can manage standards (Owner, Approver, or Publisher)
    /// </summary>
    public static async Task<bool> CanManageStandardsAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        return await CanDraftStandardsAsync(permissionService, user);
    }

    /// <summary>
    /// Check if user can manage standards workflow (approval/publishing) - Standard Publishers or Standards Managers only
    /// </summary>
    public static async Task<bool> CanManageStandardsWorkflowAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardPublisherGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardsManagerGroupName);
    }

    /// <summary>
    /// Get user email from claims
    /// </summary>
    public static string GetUserEmail(ClaimsPrincipal user)
    {
        return user.Identity?.Name 
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value
            ?? user.FindFirst("email")?.Value
            ?? string.Empty;
    }

    /// <summary>
    /// Check if user can approve/reject standards (Standard Approver or Standards Manager)
    /// </summary>
    public static async Task<bool> CanApproveStandardsAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardApproverGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardsManagerGroupName);
    }

    /// <summary>
    /// Check if user can publish standards (Standard Publisher or Standards Manager)
    /// </summary>
    public static async Task<bool> CanPublishStandardsAsync(IPermissionService permissionService, ClaimsPrincipal user)
    {
        var userEmail = GetUserEmail(user);
        if (string.IsNullOrEmpty(userEmail))
            return false;

        return await permissionService.IsInGroupAsync(userEmail, StandardPublisherGroupName) ||
               await permissionService.IsInGroupAsync(userEmail, StandardsManagerGroupName);
    }

}
