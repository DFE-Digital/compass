using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class FunctionalStandardTheme
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FunctionalStandardId { get; set; }

    [Required]
    public int ThemeId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("FunctionalStandardId")]
    public FunctionalStandard? FunctionalStandard { get; set; }

    public ICollection<PracticeArea> PracticeAreas { get; set; } = new List<PracticeArea>();
}

