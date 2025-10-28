using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class AccessibilityEmailConfiguration
    {
        public int Id { get; set; }
        
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;
        
        [Required]
        public string Purpose { get; set; } = string.Empty; // "RetestRequests", "ReportSummaries"
        
        public string? Description { get; set; } // Optional description
        
        public bool IsActive { get; set; } = true;
        
        public int SortOrder { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
    }
}

