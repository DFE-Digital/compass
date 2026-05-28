using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Compass.Models.Raid;

namespace Compass.Models;

/// <summary>
/// Boards, reviews, or other assurance events tracked for an issue (dates and decisions / expectations).
/// </summary>
public class IssueAssuranceEvent
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    /// <summary>board, review, event, or short free-text label.</summary>
    [Required]
    [MaxLength(50)]
    public string EventKind { get; set; } = "review";

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = "";

    public DateTime? EventDate { get; set; }

    [MaxLength(RaidFieldLimits.NarrativeMaxLength)]
    public string? DecisionSummary { get; set; }

    public int SortOrder { get; set; }
}
