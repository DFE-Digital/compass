using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Monthly updates for projects - required to be completed by the 5th working day of the following month
/// </summary>
public class ProjectMonthlyUpdate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    public int Year { get; set; }

    [Required]
    [Range(1, 12)]
    public int Month { get; set; }

    [Required]
    public string Narrative { get; set; } = string.Empty;

    // EntraUser fields
    public string? CreatedByEntraId { get; set; }

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    // Legacy User fields (nullable for migration compatibility)
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    // Navigation property for individual narrative entries
    public ICollection<MonthlyUpdateNarrative> MonthlyUpdateNarratives { get; set; } = new List<MonthlyUpdateNarrative>();
}

