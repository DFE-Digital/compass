using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class GroupFeaturePermission
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public Group Group { get; set; } = null!;

    [Required]
    public int FeatureId { get; set; }

    [ForeignKey(nameof(FeatureId))]
    public Feature Feature { get; set; } = null!;

    [Required]
    public PermissionType Permission { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? CreatedBy { get; set; }
}

public enum PermissionType
{
    View = 1,
    Create = 2,
    Update = 3,
    Delete = 4
}

