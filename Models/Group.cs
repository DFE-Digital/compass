using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsSystemGroup { get; set; } = false; // System groups like "Central Operations Admin" cannot be deleted

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [StringLength(255)]
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<GroupFeaturePermission> GroupFeaturePermissions { get; set; } = new List<GroupFeaturePermission>();
}

