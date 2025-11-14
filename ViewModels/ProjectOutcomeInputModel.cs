using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectOutcomeInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? OutcomeId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Outcome { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string MeasureOfSuccess { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string ConfidenceLevel { get; set; } = "Medium";

    public string? ConfidenceExplanation { get; set; }

    public int SortOrder { get; set; }
}
