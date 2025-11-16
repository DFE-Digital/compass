using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models
{
    public class AuditHistory
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductAccessibilityId { get; set; }
        
        [Required]
        public DateTime AuditDate { get; set; }
        
        [Required]
        public string AuditedBy { get; set; } = string.Empty; // Name or organization that conducted audit
        
        [Required]
        public string AuditType { get; set; } = string.Empty; // Internal, External
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cost { get; set; } // Cost of the audit (optional)
        
        public string? Notes { get; set; } // Optional notes about the audit
        
        public string? ReportUrl { get; set; } // Optional link to audit report
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        
        public bool IsDeleted { get; set; } = false;
        
        // Navigation properties
        public ProductAccessibility ProductAccessibility { get; set; } = null!;
    }
}

