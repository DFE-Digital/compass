using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels;

public class ProjectTeamMemberFormViewModel
{
    public ProjectTeamMemberInputModel Input { get; set; } = new();

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public IReadOnlyList<SelectListItem> EmploymentTypeOptions { get; set; } = Array.Empty<SelectListItem>();

    public IReadOnlyList<SelectListItem> TeamStatusOptions { get; set; } = Array.Empty<SelectListItem>();

    public string? SelectedUserName { get; set; }

    public string? SelectedUserEmail { get; set; }

    public bool ShowDeleteButton { get; set; }
}

public class ProjectTeamMemberInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? TeamMemberId { get; set; }

    public int? UserId { get; set; }

    [Required]
    [StringLength(200, ErrorMessage = "Role must be 200 characters or fewer.")]
    public string Role { get; set; } = string.Empty;

    [Required]
    [StringLength(200, ErrorMessage = "Funding description must be 200 characters or fewer.")]
    public string FundingArrangement { get; set; } = string.Empty;

    [Required]
    [RegularExpression("(Permanent|MSP)", ErrorMessage = "Select Permanent or MSP.")]
    public string EmploymentType { get; set; } = "Permanent";

    [Required]
    [RegularExpression("(current|previous)", ErrorMessage = "Select current or previous.")]
    public string TeamStatus { get; set; } = "current";

    [StringLength(500, ErrorMessage = "Reason for leaving must be 500 characters or fewer.")]
    public string? LeaveReason { get; set; }
}

public class ProjectTeamMemberDetailsViewModel
{
    public int ProjectId { get; set; }

    public int TeamMemberId { get; set; }

    public string ProjectTitle { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string FundingArrangement { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    public string TeamStatus { get; set; } = "current";

    public string? LeaveReason { get; set; }

    public DateTime AddedAt { get; set; }

    public DateTime? LeftAt { get; set; }

    public string? JobTitle { get; set; }
}

public class ProjectTeamMemberRemovalInputModel
{
    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int TeamMemberId { get; set; }

    [Required(ErrorMessage = "Provide a reason for removing this person from the team.")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

