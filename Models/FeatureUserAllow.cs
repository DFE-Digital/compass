using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>User explicitly allowed to access a feature when <see cref="Feature.AccessMode"/> is <see cref="FeatureAccessMode.OnForSome"/>.</summary>
public class FeatureUserAllow
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int FeatureId { get; set; }

    [ForeignKey(nameof(FeatureId))]
    public Feature Feature { get; set; } = null!;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
