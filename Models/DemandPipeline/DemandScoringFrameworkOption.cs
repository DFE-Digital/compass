using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

[Table("DemandScoringFrameworkOptions")]
public class DemandScoringFrameworkOption
{
    public int Id { get; set; }

    public int QuestionId { get; set; }

    [Required]
    [MaxLength(500)]
    public string Label { get; set; } = string.Empty;

    public int Points { get; set; }

    public int SortOrder { get; set; }

    public DemandScoringFrameworkQuestion Question { get; set; } = null!;
}
