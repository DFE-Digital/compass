using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Monthly updates for projects; due date comes from <see cref="MonthlyUpdateDeadlineConfig"/> (admin-configured).
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

    /// <summary>Permanent FTE for this reporting month (from monthly return; headcount scale, not named individuals).</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MonthlyPermFte { get; set; }

    /// <summary>MSP (contractor) FTE for this reporting month.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? MonthlyMspFte { get; set; }

    /// <summary>Narrative on people changes for this reporting month (moves, onboarding, offboarding).</summary>
    [MaxLength(4000)]
    public string? PeopleNarrative { get; set; }

    /// <summary>Draft RAG selection for an in-progress monthly return (before submit). Persisted on save draft; cleared on submit.</summary>
    public int? DraftRagStatusLookupId { get; set; }

    [ForeignKey(nameof(DraftRagStatusLookupId))]
    public RagStatusLookup? DraftRagStatusLookup { get; set; }

    [MaxLength(4000)]
    public string? DraftRagJustification { get; set; }

    [MaxLength(4000)]
    public string? DraftPathToGreen { get; set; }

    // Navigation property for individual narrative entries
    public ICollection<MonthlyUpdateNarrative> MonthlyUpdateNarratives { get; set; } = new List<MonthlyUpdateNarrative>();
}

