using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class KpiDataPoint
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int KpiId { get; set; }

    [ForeignKey(nameof(KpiId))]
    public Kpi Kpi { get; set; } = null!;

    [Required]
    public DateTime ReportingPeriodStart { get; set; }

    public DateTime? ReportingPeriodEnd { get; set; }

    public decimal? Value { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ValueNarrative { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Notes { get; set; }

    public bool IsValidated { get; set; } = false;

    [MaxLength(30)]
    public string? SubmissionStatus { get; set; }

    public int? SubmittedByUserId { get; set; }

    [ForeignKey(nameof(SubmittedByUserId))]
    public User? SubmittedByUser { get; set; }

    [Required]
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
