using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class FunctionalStandard
{
    [Key]
    public int Id { get; set; } // User-defined, not auto-generated

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime? PublishedDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<FunctionalStandardTheme> Themes { get; set; } = new List<FunctionalStandardTheme>();
}

