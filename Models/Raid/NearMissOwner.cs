using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class NearMissOwner
{
    public int NearMissId { get; set; }

    [ForeignKey(nameof(NearMissId))]
    public NearMiss NearMiss { get; set; } = null!;

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
