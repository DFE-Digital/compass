using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Kpi
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? CalculationMethod { get; set; }

    [MaxLength(50)]
    public string? Frequency { get; set; }

    public decimal? TargetValue { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Thresholds { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? DataSource { get; set; }

    [MaxLength(200)]
    public string? ReportingStage { get; set; }

    [MaxLength(50)]
    public string? Status { get; set; }

    [Required]
    [MaxLength(100)]
    public string AssignedToEntityId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string EntityType { get; set; } = "project";

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [MaxLength(100)]
    public string? ProductDocumentId { get; set; } // Product DocumentID from CMS (primary identifier)

    [MaxLength(50)]
    public string? ProductFipsId { get; set; } // Product FIPS ID (legacy, kept for backwards compatibility)

    public int? ObjectiveId { get; set; }

    [ForeignKey(nameof(ObjectiveId))]
    public Objective? Objective { get; set; }

    public int? MilestoneId { get; set; }

    [ForeignKey(nameof(MilestoneId))]
    public Milestone? Milestone { get; set; }

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ValidationRule { get; set; }

    public bool Active { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<KpiDataPoint> PerformanceData { get; set; } = new List<KpiDataPoint>();
}
