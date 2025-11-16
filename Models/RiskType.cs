using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Compass.Models;

public class RiskType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Summary { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    [MaxLength(20)]
    public string Severity { get; set; } = "Medium";

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DemandRequestRiskType> DemandRequestLinks { get; set; } = new List<DemandRequestRiskType>();
}

