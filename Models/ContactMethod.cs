using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class ContactMethod
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductAccessibilityId { get; set; }
        
        [Required]
        public string ContactType { get; set; } = string.Empty; // Email, Phone, Web form, Support portal, etc.
        
        [Required]
        public string ContactDetail { get; set; } = string.Empty; // The actual contact info (email address, phone number, URL, etc.)
        
        public string? Description { get; set; } // Optional description or notes
        
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ProductAccessibility ProductAccessibility { get; set; } = null!;
    }
}

