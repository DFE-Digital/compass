using System.ComponentModel.DataAnnotations;
using Compass.Models;

namespace Compass.ViewModels.Admin;

public class UserLeadershipAssignmentPageViewModel
{
    public User? SelectedUser { get; set; }
    public IReadOnlyCollection<LeadershipBusinessAreaOption> BusinessAreas { get; set; } = Array.Empty<LeadershipBusinessAreaOption>();
    public IReadOnlyCollection<LeadershipRoleOption> RoleOptions { get; set; } = Array.Empty<LeadershipRoleOption>();
    public IReadOnlyCollection<UserBusinessAreaRoleAssignment> Assignments { get; set; } = Array.Empty<UserBusinessAreaRoleAssignment>();
    public IReadOnlyCollection<UserLeadershipAssigneeSummary> Assignees { get; set; } = Array.Empty<UserLeadershipAssigneeSummary>();
    public UserLeadershipAssignmentInputModel Input { get; set; } = new();
    public bool HasSelectedUser => SelectedUser != null;
}

public class LeadershipRoleOption
{
    public LeadershipRoleTier Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class LeadershipBusinessAreaOption
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UserLeadershipAssigneeSummary
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AssignmentCount { get; set; }
    public IReadOnlyCollection<string> BusinessAreas { get; set; } = Array.Empty<string>();
    public IReadOnlyCollection<LeadershipRoleTier> Roles { get; set; } = Array.Empty<LeadershipRoleTier>();
}

public class UserLeadershipAssignmentInputModel
{
    [Required]
    public int? UserId { get; set; }

    [Required(ErrorMessage = "Select at least one business area.")]
    [MinLength(1, ErrorMessage = "Select at least one business area.")]
    public string[] BusinessAreas { get; set; } = Array.Empty<string>();

    [Required(ErrorMessage = "Select at least one leadership role.")]
    [MinLength(1, ErrorMessage = "Select at least one leadership role.")]
    public LeadershipRoleTier[] Roles { get; set; } = Array.Empty<LeadershipRoleTier>();
}

