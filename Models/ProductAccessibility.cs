using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class ProductAccessibility
    {
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string? ProductDocumentId { get; set; } // Product DocumentID from CMS (primary identifier) - nullable initially, will be required after data migration
        
        [MaxLength(50)]
        public string? FipsId { get; set; } // Product FIPS ID (legacy, kept for backwards compatibility)
        
        public string? ProductName { get; set; } // Cached from CMS API
        public string? ProductPhase { get; set; } // Cached from CMS API
        
        [Required]
        [Range(1, 365)]
        public int SlaResponseDays { get; set; } // SLA for responding to queries in working days
        
        [Required]
        [EmailAddress]
        public string ComplaintsEmail { get; set; } = string.Empty; // Contact email for complaints
        
        // WCAG Compliance Settings
        [Required]
        public string WcagVersion { get; set; } = "2.2"; // 2.1, 2.2, 3.0
        
        [Required]
        public string WcagLevel { get; set; } = "AA"; // AA, AAA
        
        // Statement Settings
        public string? StatementUrl { get; set; } // URL to the accessibility statement
        public bool StatementInstalled { get; set; } = false; // Whether statement is installed on website
        public string? VerifiedBy { get; set; } // Accessibility Administrator who verified
        public DateTime? VerifiedAt { get; set; } // When it was verified
        public string? StatementVerificationMethod { get; set; } // "Manual", "Automatic", null
        
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public string? EnrolledBy { get; set; } // User who enrolled the product
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;
        
        // Legacy ID from old accessibility service
        public int? LegacyId { get; set; } // Legacy product ID from old accessibility issues and statement service
        
        // Navigation properties
        public ICollection<StatementVerificationRequest> VerificationRequests { get; set; } = new List<StatementVerificationRequest>();
        
        // Navigation properties
        public ICollection<ContactMethod> ContactMethods { get; set; } = new List<ContactMethod>();
        public ICollection<AuditHistory> AuditHistories { get; set; } = new List<AuditHistory>();
        public ICollection<AccessibilityIssue> Issues { get; set; } = new List<AccessibilityIssue>();
    }
}

