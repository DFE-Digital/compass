using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

[Table("DemandScoringFrameworkSections")]
public class DemandScoringFrameworkSection
{
    public int Id { get; set; }

    /// <summary>Short slug for display and scripts (e.g. strategic, rice).</summary>
    [Required]
    [MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Cap for this section after summing scored questions.</summary>
    public int MaxPoints { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Optional mapping to DemandPipelineRequest.Score* columns (Strategic, Urgency, Funding, Rice).</summary>
    [MaxLength(20)]
    public string? LegacyColumn { get; set; }

    public ICollection<DemandScoringFrameworkQuestion> Questions { get; set; } = new List<DemandScoringFrameworkQuestion>();
}
