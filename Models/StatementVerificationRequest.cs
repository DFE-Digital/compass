using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class StatementVerificationRequest
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductAccessibilityId { get; set; }
        
        [Required]
        public string RequestedBy { get; set; } = string.Empty; // Email of person requesting verification
        
        public string? RequestorEmail { get; set; } // Email for notifications
        
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        
        public string? RequestNotes { get; set; } // Notes from requestor
        
        // Admin fields
        public bool? IsCompleted { get; set; } // null = pending, true = completed, false = cancelled
        
        public bool? VerificationResult { get; set; } // true = verified, false = not verified, null if not completed
        
        public string? AdminNotes { get; set; } // Notes from admin for requestor
        
        public string? CompletedBy { get; set; } // Email of admin who completed the verification
        
        public DateTime? CompletedAt { get; set; }
        
        // Email notification tracking
        public bool EmailSentToRequestor { get; set; } = false;
        public bool EmailSentToAdmin { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ProductAccessibility ProductAccessibility { get; set; } = null!;
    }
}

