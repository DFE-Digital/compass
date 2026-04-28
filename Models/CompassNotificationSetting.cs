using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>Default toggles for a Compass notification type (who receives email when automation runs).</summary>
public class CompassNotificationSetting
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    /// <summary>Bitmask of <see cref="CompassNotificationRecipientFlags"/>.</summary>
    public int RecipientFlags { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
