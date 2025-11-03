using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Visitor;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for role-based access control
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
}

public enum UserRole
{
    Visitor = 0,
    Reporter = 1,
    Admin = 2,
    SuperAdmin = 3
}

