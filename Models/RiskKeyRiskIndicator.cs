using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Measurable indicator tracked for a RAID risk (title, narrative, metric).</summary>
public class RiskKeyRiskIndicator
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required]
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>What is measured.</summary>
    [MaxLength(2000)]
    public string? Metric { get; set; }

    /// <summary>Threshold at which the risk escalates or is reviewed.</summary>
    [MaxLength(2000)]
    public string? Threshold { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
