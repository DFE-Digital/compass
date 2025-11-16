using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectSuccessFormViewModel
{
    public ProjectSuccessInputModel Input { get; set; } = new();

    public string ProjectTitle { get; set; } = string.Empty;

    public ProjectSummaryViewModel ProjectSummary { get; set; } = new();

    public string? RecordedByName { get; set; }

    public string? RecordedByEmail { get; set; }

    public DateTime? RecordedAt { get; set; }
}

public class ProjectSuccessInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? SuccessId { get; set; }

    [Required]
    [StringLength(2000, ErrorMessage = "Success description must be 2000 characters or fewer.")]
    public string SuccessDescription { get; set; } = string.Empty;

    public bool IsReportedToSlt { get; set; }
}
