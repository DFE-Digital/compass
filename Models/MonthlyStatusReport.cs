using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Monthly status report capturing updates on milestones and deliverables for a specific month
/// </summary>
public class MonthlyStatusReport
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Year of the reporting period (e.g., 2025)
    /// </summary>
    [Required]
    public int ReportingYear { get; set; }

    /// <summary>
    /// Month of the reporting period (1-12)
    /// </summary>
    [Required]
    [Range(1, 12)]
    public int ReportingMonth { get; set; }

    /// <summary>
    /// Narrative update for the month
    /// </summary>
    [MaxLength(5000)]
    public string? Narrative { get; set; }

    /// <summary>
    /// Summary of milestone progress for the month
    /// </summary>
    [MaxLength(2000)]
    public string? MilestoneProgress { get; set; }

    /// <summary>
    /// Summary of deliverable progress for the month
    /// </summary>
    [MaxLength(2000)]
    public string? DeliverableProgress { get; set; }

    /// <summary>
    /// Key achievements for the month
    /// </summary>
    [MaxLength(2000)]
    public string? KeyAchievements { get; set; }

    /// <summary>
    /// Challenges or blockers encountered
    /// </summary>
    [MaxLength(2000)]
    public string? Challenges { get; set; }

    /// <summary>
    /// Outlook for next month
    /// </summary>
    [MaxLength(2000)]
    public string? NextMonthOutlook { get; set; }

    /// <summary>
    /// User who created/submitted this report
    /// </summary>
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

}

