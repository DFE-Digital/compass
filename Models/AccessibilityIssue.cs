using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class AccessibilityIssue
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductAccessibilityId { get; set; }
        
        [Required]
        public string IssueType { get; set; } = "WCAG"; // WCAG, Best Practice, or Usability
        
        public string? IssueTitle { get; set; } // Title for Best Practice/Usability issues
        
        // For backward compatibility and Best Practice issues
        public string? WcagCriteria { get; set; } // e.g., "1.2.2", "3.1.1" - deprecated, use WcagCriteriaLinks
        
        public string? WcagLevel { get; set; } // A, AA, AAA - deprecated, use WcagCriteriaLinks
        
        public string? WcagVersion { get; set; } // 2.1, 2.2 - deprecated, use WcagCriteriaLinks
        
        [Required]
        public DateTime IdentifiedDate { get; set; } // When the issue was identified
        
        [Required]
        public string IdentifiedVia { get; set; } = string.Empty; // Audit, Testing, Feedback, Complaint, GDS
        
        public string? IssueDescription { get; set; } // Description of the issue
        
        [Required]
        public bool IsResolving { get; set; } = true; // Yes/No - whether we plan to resolve
        
        public DateTime? PlannedResolutionDate { get; set; } // Date plan to resolve by (if resolving = Yes)
        
        public string? NonResolutionReason { get; set; } // Reason for not resolving (if resolving = No)
        
        public DateTime? ActualResolutionDate { get; set; } // When actually resolved
        
        public string? ResolutionNotes { get; set; } // Notes about the resolution - how it was fixed
        
        public string? VerificationNotes { get; set; } // How it was tested and confirmed fixed
        
        public string Status { get; set; } = "open"; // open, in_progress, resolved, wont_fix
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // Navigation properties
        public ProductAccessibility ProductAccessibility { get; set; } = null!;
        public ICollection<IssueComment> Comments { get; set; } = new List<IssueComment>();
        public ICollection<IssueHistory> History { get; set; } = new List<IssueHistory>();
        public ICollection<IssueWcagCriterion> WcagCriteriaLinks { get; set; } = new List<IssueWcagCriterion>();
    }
}

