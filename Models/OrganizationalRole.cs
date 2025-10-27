using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    public class OrganizationalRole
    {
        public int Id { get; set; }
        
        [Required]
        public int OrganizationalGroupId { get; set; }
        public OrganizationalGroup OrganizationalGroup { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string RoleType { get; set; } = string.Empty; // Director General, Director, Deputy Director
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(200)]
        public string? Email { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Helper properties
        public string DisplayName => $"{RoleType}: {Name}";
    }
}
