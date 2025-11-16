using System;
using Compass.Models;

namespace Compass.ViewModels;

public class FeaturePermissionSummaryViewModel
{
    public string FeatureName { get; set; } = string.Empty;

    public string FeatureCode { get; set; } = string.Empty;

    public PermissionType Permission { get; set; }

    public string PermissionLabel { get; set; } = string.Empty;
}

public class UserGroupPermissionSummaryViewModel
{
    public string GroupName { get; set; } = string.Empty;

    public string? GroupDescription { get; set; }

    public DateTime? AssignedAt { get; set; }

    public IReadOnlyList<FeaturePermissionSummaryViewModel> FeaturePermissions { get; set; }
        = Array.Empty<FeaturePermissionSummaryViewModel>();
}

