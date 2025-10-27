using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class GovernmentDepartment
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Title { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Abbreviation { get; set; }
        
        [StringLength(1000)]
        public string? Description { get; set; }
        
        [StringLength(200)]
        public string? Format { get; set; } // e.g., "Ministerial department", "Executive agency"
        
        [StringLength(50)]
        public string? GovukStatus { get; set; } // "live", "exempt", etc.
        
        public DateTime? ClosedAt { get; set; }
        
        [StringLength(500)]
        public string? WebUrl { get; set; }
        
        [StringLength(200)]
        public string? AnalyticsIdentifier { get; set; }
        
        // Hierarchy fields
        public int? ParentDepartmentId { get; set; }
        public GovernmentDepartment? ParentDepartment { get; set; }
        public ICollection<GovernmentDepartment> ChildDepartments { get; set; } = new List<GovernmentDepartment>();
        
        // Sync fields
        [StringLength(200)]
        public string? GovukId { get; set; } // The ID from the GOV.UK API
        
        public bool IsDeleted { get; set; } = false; // For soft deletion
        
        public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Helper properties
        public bool IsActive => ClosedAt == null; // Active if not closed (includes "live", "exempt", etc.)
        
        public string FullPath
        {
            get
            {
                var path = new List<string> { Title };
                var current = ParentDepartment;
                while (current != null)
                {
                    path.Insert(0, current.Title);
                    current = current.ParentDepartment;
                }
                return string.Join(" > ", path);
            }
        }
        
        public int Level
        {
            get
            {
                int level = 0;
                var current = ParentDepartment;
                while (current != null)
                {
                    level++;
                    current = current.ParentDepartment;
                }
                return level;
            }
        }
    }
}
