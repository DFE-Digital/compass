using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Junction table for DDT Standard contacts (many-to-many relationship)
/// </summary>
public class DdtStandardContact
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// User who is a contact (Entra ID user)
    /// </summary>
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

