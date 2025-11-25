using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Individual narrative entries for a monthly update - allows multiple narratives per month
/// </summary>
public class MonthlyUpdateNarrative
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectMonthlyUpdateId { get; set; }

    [ForeignKey(nameof(ProjectMonthlyUpdateId))]
    public ProjectMonthlyUpdate ProjectMonthlyUpdate { get; set; } = null!;

    [Required]
    [MaxLength(450)]
    public string Narrative { get; set; } = string.Empty;

    // EntraUser fields
    public string? CreatedByEntraId { get; set; }

    public string? CreatedByName { get; set; }

    public string? CreatedByEmail { get; set; }

    // Legacy User fields (nullable for migration compatibility)
    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

