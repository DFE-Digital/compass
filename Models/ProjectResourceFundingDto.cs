using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ProjectResourceFundingDto
{
    public int Id { get; set; }
    
    public int ProjectId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string ResourceType { get; set; } = string.Empty; // "Permanent" or "MSP"
    
    [Required]
    [Range(0, 100, ErrorMessage = "Programme funded percentage must be between 0 and 100")]
    public decimal ProgrammeFundedPercentage { get; set; }
    
    [Required]
    [Range(0, 100, ErrorMessage = "Admin funded percentage must be between 0 and 100")]
    public decimal AdminFundedPercentage { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
