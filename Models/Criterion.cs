using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Criterion
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FunctionalStandardId { get; set; }

    [Required]
    public int ThemeId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PracticeAreaId { get; set; }

    [Required]
    [StringLength(50)]
    public string CriteriaCode { get; set; } = string.Empty;

    [Required]
    public string Criteria { get; set; } = string.Empty;

    [Required]
    public CriteriaRating Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public PracticeArea? PracticeArea { get; set; }
}

public enum CriteriaRating
{
    Good = 0,
    Better = 1,
    Best = 2
}

