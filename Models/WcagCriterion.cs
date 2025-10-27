using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class WcagCriterion
    {
        public int Id { get; set; }
        
        [Required]
        public string Criterion { get; set; } = string.Empty; // e.g., "1.1.1", "2.4.3"
        
        [Required]
        public string Title { get; set; } = string.Empty; // e.g., "Non-text Content"
        
        public string? Description { get; set; } // Detailed description
        
        public string? Url { get; set; } // Link to WCAG documentation
        
        [Required]
        public string Level { get; set; } = string.Empty; // A, AA, AAA
        
        [Required]
        public string Version { get; set; } = string.Empty; // 2.1, 2.2, 3.0
        
        public int SortOrder { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

