using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class UserPreference
{
    [Key]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    public string? PreferredBusinessAreas { get; set; } // Comma-separated list

    [MaxLength(50)]
    public string PreferredTaskGrouping { get; set; } = "priority";

    [MaxLength(50)]
    public string? DashboardFocus { get; set; }

    public bool ShowTasksPanel { get; set; } = true;

    public bool ShowProductPanel { get; set; } = true;

    public bool ShowRiskPanel { get; set; } = true;

    public bool ShowMilestonePanel { get; set; } = true;

    public bool ShowRemindersPanel { get; set; } = true;

    public bool ShowSuccessPanel { get; set; } = true;

    [MaxLength(500)]
    public string? QuickLaunchShortcuts { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? DashboardLayout { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

