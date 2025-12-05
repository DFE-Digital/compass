using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a user's professional profile including profession, skills, and capability gaps
/// </summary>
public class UserProfessionalProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [StringLength(100)]
    public string? Profession { get; set; } // DDaT or equivalent

    [Column(TypeName = "nvarchar(max)")]
    public string? Skills { get; set; } // JSON array or comma-separated declared skills

    [Column(TypeName = "nvarchar(max)")]
    public string? CapabilityGaps { get; set; } // Derived gaps

    [StringLength(255)]
    public string? HeadOfProfessionId { get; set; } // Governance mapping to Entra Users (email or object ID)

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }
}

