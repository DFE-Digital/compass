using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectContact
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? RoleDescription { get; set; }

    [Required]
    public int SortOrder { get; set; } = 1;

    [MaxLength(200)]
    public string FundingArrangement { get; set; } = "Admin";

    [MaxLength(50)]
    public string? TimeAllocation { get; set; }

    [MaxLength(20)]
    public string EmploymentType { get; set; } = "Permanent";

    [MaxLength(20)]
    public string TeamStatus { get; set; } = "current";

    [MaxLength(500)]
    public string? LeaveReason { get; set; }

    public DateTime? LeftAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
