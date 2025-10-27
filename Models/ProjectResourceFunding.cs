using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectResourceFunding
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string ResourceType { get; set; } = string.Empty; // "Permanent" or "MSP"

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal ProgrammeFundedPercentage { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal AdminFundedPercentage { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
