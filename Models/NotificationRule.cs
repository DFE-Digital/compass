using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class NotificationRule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public int NotificationTemplateId { get; set; }

    [ForeignKey(nameof(NotificationTemplateId))]
    public NotificationTemplate NotificationTemplate { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string TriggerCode { get; set; } = string.Empty; // e.g., "team_member_added", "sro_assigned", "rag_status_changed"

    public bool IsEnabled { get; set; } = true;

    // Recipient configuration - JSON stored as string
    // Format: { "recipients": ["team_member", "sro", "primary_contact"], "specific_emails": ["email1@example.com"] }
    public string? RecipientConfiguration { get; set; }

    // Additional conditions - JSON stored as string
    // Format: { "rag_statuses": ["Red", "Amber-Red"], "project_statuses": ["Active"] }
    public string? Conditions { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }
}
