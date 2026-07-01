using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ApiTokenMember
{
    public int Id { get; set; }

    public int ApiTokenId { get; set; }

    [Required]
    [MaxLength(256)]
    public string UserEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string AddedByEmail { get; set; } = string.Empty;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public virtual ApiToken ApiToken { get; set; } = null!;
}
