using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Feature
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Code { get; set; } = string.Empty; // e.g., "delivery_reporting", "risks", "issues"

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>When <see cref="AccessMode"/> is not <see cref="FeatureAccessMode.Off"/>, kept in sync for legacy code paths that read the flag on <see cref="Feature"/>.</summary>
    public bool IsActive { get; set; } = true;

    [Required]
    public FeatureAccessMode AccessMode { get; set; } = FeatureAccessMode.OnForAll;

    public ICollection<FeatureUserAllow> UserAllows { get; set; } = new List<FeatureUserAllow>();

    public ICollection<FeatureGroupAllow> GroupAllows { get; set; } = new List<FeatureGroupAllow>();

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<GroupFeaturePermission> GroupFeaturePermissions { get; set; } = new List<GroupFeaturePermission>();
}

