using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a capability gap identified by a user
/// </summary>
public class CapabilityGap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserProfessionalProfileId { get; set; }

    [ForeignKey(nameof(UserProfessionalProfileId))]
    public UserProfessionalProfile UserProfessionalProfile { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Optional link to an Action that addresses this gap
    /// </summary>
    public int? ActionId { get; set; }

    [ForeignKey(nameof(ActionId))]
    public Action? Action { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

