using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class IssueHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int AccessibilityIssueId { get; set; }
        
        [Required]
        public string FieldChanged { get; set; } = string.Empty; // Which field was changed
        
        public string? OldValue { get; set; } // Previous value
        
        public string? NewValue { get; set; } // New value
        
        public string? ChangeNote { get; set; } // Optional note about the change
        
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? ChangedBy { get; set; }
        
        // Navigation properties
        public AccessibilityIssue AccessibilityIssue { get; set; } = null!;
    }
}

