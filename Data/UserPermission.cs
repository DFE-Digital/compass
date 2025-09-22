using System.ComponentModel.DataAnnotations;

namespace FipsReporting.Data
{
    public class UserPermission
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Name { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Product permissions
        public bool CanAddProduct { get; set; } = false;
        public bool CanEditProduct { get; set; } = false;
        public bool CanDeleteProduct { get; set; } = false;
        
        // Metric permissions
        public bool CanAddMetric { get; set; } = false;
        public bool CanEditMetric { get; set; } = false;
        public bool CanDeleteMetric { get; set; } = false;
        
        // Milestone permissions
        public bool CanAddMilestone { get; set; } = false;
        public bool CanEditMilestone { get; set; } = false;
        public bool CanDeleteMilestone { get; set; } = false;
        
        // User management permissions
        public bool CanAddUser { get; set; } = false;
        public bool CanEditUser { get; set; } = false;
        
        // Reporting permissions
        public bool CanViewReports { get; set; } = false;
        public bool CanSubmitReports { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(255)]
        public string? CreatedBy { get; set; }
        
        [MaxLength(255)]
        public string? UpdatedBy { get; set; }
    }
}
