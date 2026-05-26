using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Explicit user access to a RAID register with a defined role.
/// Combined with hybrid org-structure inheritance for visibility.
/// </summary>
public class RaidRegisterUser
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public RaidRegisterRole Role { get; set; } = RaidRegisterRole.Viewer;

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
