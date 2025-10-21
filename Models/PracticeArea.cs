using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class PracticeArea
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
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public FunctionalStandardTheme? Theme { get; set; }

    public ICollection<Criterion> Criteria { get; set; } = new List<Criterion>();
}

