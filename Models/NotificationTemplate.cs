using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class NotificationTemplate
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
    [MaxLength(100)]
    public string TriggerCode { get; set; } = string.Empty; // e.g., "team_member_added", "sro_assigned", "rag_status_changed"

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty; // Template with placeholders like {{project_title}}, {{user_name}}

    [Required]
    public string Body { get; set; } = string.Empty; // Template with placeholders

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    // Navigation properties
    public ICollection<NotificationRule> NotificationRules { get; set; } = new List<NotificationRule>();
}
