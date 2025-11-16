using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class UserBusinessAreaRoleAssignment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string BusinessAreaName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string BusinessAreaKey { get; set; } = string.Empty;

    [Required]
    public LeadershipRoleTier Role { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum LeadershipRoleTier
{
    PortfolioLead = 10, // G6 / Portfolio
    DeputyDirectorOrSro = 20,
    CLevel = 30,
    DirectorGeneral = 40,
    PermanentSecretary = 50
}

