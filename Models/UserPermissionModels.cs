using System.ComponentModel.DataAnnotations;

namespace FipsReporting.Models
{
    public class UserPermissionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string? Name { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Product permissions
        [Display(Name = "Can Add Products")]
        public bool CanAddProduct { get; set; } = false;

        [Display(Name = "Can Edit Products")]
        public bool CanEditProduct { get; set; } = false;

        [Display(Name = "Can Delete Products")]
        public bool CanDeleteProduct { get; set; } = false;

        // Metric permissions
        [Display(Name = "Can Add Metrics")]
        public bool CanAddMetric { get; set; } = false;

        [Display(Name = "Can Edit Metrics")]
        public bool CanEditMetric { get; set; } = false;

        [Display(Name = "Can Delete Metrics")]
        public bool CanDeleteMetric { get; set; } = false;

        // Milestone permissions
        [Display(Name = "Can Add Milestones")]
        public bool CanAddMilestone { get; set; } = false;

        [Display(Name = "Can Edit Milestones")]
        public bool CanEditMilestone { get; set; } = false;

        [Display(Name = "Can Delete Milestones")]
        public bool CanDeleteMilestone { get; set; } = false;

        // User management permissions
        [Display(Name = "Can Add Users")]
        public bool CanAddUser { get; set; } = false;

        [Display(Name = "Can Edit Users")]
        public bool CanEditUser { get; set; } = false;

        // Reporting permissions
        [Display(Name = "Can View Reports")]
        public bool CanViewReports { get; set; } = false;

        [Display(Name = "Can Submit Reports")]
        public bool CanSubmitReports { get; set; } = false;
    }
}
