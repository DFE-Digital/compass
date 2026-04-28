using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

[Table("DemandScoringFrameworkQuestions")]
public class DemandScoringFrameworkQuestion
{
    public int Id { get; set; }

    public int SectionId { get; set; }

    /// <summary>Unique code used in forms and JSON answers (e.g. S1Q2).</summary>
    [Required]
    [MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Prompt { get; set; } = string.Empty;

    public string? Hint { get; set; }

    /// <summary>Radio, Number, Text, Context</summary>
    [Required]
    [MaxLength(20)]
    public string QuestionType { get; set; } = "Radio";

    public bool IsScored { get; set; } = true;

    public int SortOrder { get; set; }

    public int? NumberMin { get; set; }

    public int? NumberMax { get; set; }

    /// <summary>For Context type: which demand field to render (see DemandScoringContextRenderer).</summary>
    [MaxLength(50)]
    public string? ContextKey { get; set; }

    public DemandScoringFrameworkSection Section { get; set; } = null!;

    public ICollection<DemandScoringFrameworkOption> Options { get; set; } = new List<DemandScoringFrameworkOption>();
}
