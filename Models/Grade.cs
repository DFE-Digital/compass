using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a Civil Service grade that can be mapped to capability levels
/// </summary>
public class Grade
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Grade code/name (e.g., "AA", "AO", "EO", "HEO", "SEO", "G7", "G6", "G6 HOP", "SCS1", "SCS2", "SCS3")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the grade (e.g., "Administrative Assistant", "Grade 6 Head of Profession")
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Display order for sorting
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this grade is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

