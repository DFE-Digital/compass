using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class IssueComment
    {
        public int Id { get; set; }
        
        [Required]
        public int AccessibilityIssueId { get; set; }
        
        [Required]
        public string CommentText { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // Navigation properties
        public AccessibilityIssue AccessibilityIssue { get; set; } = null!;
    }
}

