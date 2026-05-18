using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

[Table("DemandScoringBandDefinitions")]
public class DemandScoringBandDefinition
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Label { get; set; } = string.Empty;

    public int MinScaledInclusive { get; set; }

    public int MaxScaledInclusive { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
