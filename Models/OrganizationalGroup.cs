using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class OrganizationalGroup
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public int? ParentGroupId { get; set; }
        public OrganizationalGroup? ParentGroup { get; set; }
        public ICollection<OrganizationalGroup> ChildGroups { get; set; } = new List<OrganizationalGroup>();
        
        public int SortOrder { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public ICollection<OrganizationalRole> Roles { get; set; } = new List<OrganizationalRole>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        
        // Helper properties
        public string FullPath
        {
            get
            {
                var path = new List<string> { Name };
                var current = ParentGroup;
                while (current != null)
                {
                    path.Insert(0, current.Name);
                    current = current.ParentGroup;
                }
                return string.Join(" > ", path);
            }
        }
        
        public int Level
        {
            get
            {
                int level = 0;
                var current = ParentGroup;
                while (current != null)
                {
                    level++;
                    current = current.ParentGroup;
                }
                return level;
            }
        }
    }
}
