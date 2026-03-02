using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class NotificationLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? NotificationRuleId { get; set; }

    [ForeignKey(nameof(NotificationRuleId))]
    public NotificationRule? NotificationRule { get; set; }

    public int? NotificationTemplateId { get; set; }

    [ForeignKey(nameof(NotificationTemplateId))]
    public NotificationTemplate? NotificationTemplate { get; set; }

    [Required]
    [MaxLength(50)]
    public string TriggerCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string RecipientEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    public string? Body { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending, sent, failed

    [MaxLength(1000)]
    public string? StatusMessage { get; set; }

    [MaxLength(100)]
    public string? NotifyMessageId { get; set; }

    // Context data - JSON stored as string
    // Stores information about what triggered the notification (project ID, user ID, etc.)
    public string? ContextData { get; set; }

    public DateTime? SentAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
