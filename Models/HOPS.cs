using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a Head of Profession (HOP) assignment
/// </summary>
public class HOPS
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// The profession ID the user is responsible for
    /// </summary>
    [Required]
    public int DdatProfessionId { get; set; }

    [ForeignKey(nameof(DdatProfessionId))]
    public DdatProfession DdatProfession { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }
}

