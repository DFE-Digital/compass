using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>Audit row for each outbound Compass notification email attempt.</summary>
public class CompassNotificationEmailLog
{
    public long Id { get; set; }

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(256)]
    public string RecipientEmail { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? RecipientName { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional reference e.g. <c>risk-42</c>, <c>issue-7</c>, <c>work-period-12</c>.</summary>
    [MaxLength(200)]
    public string? ContextReference { get; set; }

    public bool SendSucceeded { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }
}
