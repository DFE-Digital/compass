using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Dependency
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string SourceEntityType { get; set; } = string.Empty; // "Project", "Risk", "Issue", "Milestone", "Action"

    [Required]
    public int SourceEntityId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TargetEntityType { get; set; } = string.Empty; // "Project", "Risk", "Issue", "Milestone", "Action"

    [Required]
    public int TargetEntityId { get; set; }

    [MaxLength(200)]
    public string? DependencyType { get; set; } // e.g., "Blocking", "Related", "Prerequisite", "Successor"

    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; } = "Active"; // Active, Resolved, Cancelled

    public DateTime? ResolvedDate { get; set; }

    [MaxLength(255)]
    public string? ResolvedByEmail { get; set; }

    [MaxLength(200)]
    public string? ResolvedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed properties for display
    [NotMapped]
    public string SourceEntityTitle { get; set; } = string.Empty;

    [NotMapped]
    public string TargetEntityTitle { get; set; } = string.Empty;
}
