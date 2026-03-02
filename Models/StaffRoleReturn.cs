using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Staff Role Return - Annual submission of staff member's primary role and secondary skills
/// Due annually by 31 March
/// </summary>
public class StaffRoleReturn
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    /// <summary>
    /// Year of the due date (31 March). 
    /// E.g., 2026 for period Apr 2024-Mar 2025 (due 31 Mar 2026)
    /// </summary>
    [Required]
    public int Year { get; set; }
    
    /// <summary>
    /// Primary GDD Role
    /// </summary>
    [Required]
    public int GddRoleId { get; set; }
    
    /// <summary>
    /// Civil Service Grade
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Grade { get; set; } = string.Empty; // EO, HEO, SEO, G7, G6, SCS1, SCS2, SCS3
    
    /// <summary>
    /// Submission status
    /// </summary>
    [Required]
    public ReturnStatus Status { get; set; } = ReturnStatus.Draft;
    
    /// <summary>
    /// When the return was submitted
    /// </summary>
    public DateTime? SubmittedDate { get; set; }
    
    /// <summary>
    /// Last modified date for draft changes
    /// </summary>
    public DateTime? LastModifiedDate { get; set; }
    
    /// <summary>
    /// When the return is due (always 31 March of the year stored in Year field)
    /// </summary>
    [Required]
    public DateTime DueDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation properties
    /// </summary>
    public User User { get; set; } = null!;
    public GddRole GddRole { get; set; } = null!;
    public List<StaffRoleReturnSkill> SecondarySkills { get; set; } = new();
}

/// <summary>
/// Junction table for Staff Role Return secondary skills (up to 5)
/// </summary>
public class StaffRoleReturnSkill
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int StaffRoleReturnId { get; set; }
    
    [Required]
    public int SkillId { get; set; }
    
    /// <summary>
    /// Display order for the skill (1-5)
    /// </summary>
    [Required]
    [Range(1, 5)]
    public int DisplayOrder { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation properties
    /// </summary>
    public StaffRoleReturn StaffRoleReturn { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}

/// <summary>
/// GDD Role from the Government Digital and Data Capability Framework
/// </summary>
public class GddRole
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Role family (e.g., "Architecture", "User-centred design")
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RoleFamily { get; set; } = string.Empty;
    
    /// <summary>
    /// Full role name (e.g., "Lead business architect", "Senior user researcher")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string RoleName { get; set; } = string.Empty;
    
    /// <summary>
    /// Role level (e.g., "Associate", "Working", "Practitioner", "Senior")
    /// </summary>
    [StringLength(50)]
    public string? RoleLevel { get; set; }
    
    /// <summary>
    /// Role description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Display name combining level and role name
    /// </summary>
    [StringLength(250)]
    public string DisplayName { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public int SortOrder { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation properties
    /// </summary>
    public List<StaffRoleReturn> StaffRoleReturns { get; set; } = new();
}

/// <summary>
/// Skill from the Government Digital and Data Capability Framework Skills A-Z
/// </summary>
public class Skill
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Skill name (e.g., "Business architecture", "Communicating information")
    /// </summary>
    [Required]
    [StringLength(200)]
    public string SkillName { get; set; } = string.Empty;
    
    /// <summary>
    /// Skill description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Skill category/family
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int SortOrder { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Navigation properties
    /// </summary>
    public List<StaffRoleReturnSkill> StaffRoleReturns { get; set; } = new();

    /// <summary>
    /// Professions that include this skill
    /// </summary>
    public virtual ICollection<ProfessionSkill> ProfessionSkills { get; set; } = new List<ProfessionSkill>();
}

