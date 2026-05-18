using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>End-to-end pipeline stage row for the demand pipeline tracker (Compass2-aligned).</summary>
[Table("DemandPipelineStages")]
public class DemandPipelineStage
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? Grouping { get; set; }
}
