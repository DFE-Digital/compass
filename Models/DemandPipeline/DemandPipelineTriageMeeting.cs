using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>Triage meeting for Compass2-style demand pipeline.</summary>
[Table("DemandPipelineTriageMeetings")]
public class DemandPipelineTriageMeeting
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime? MeetingDate { get; set; }

    [MaxLength(20)]
    public string? StartTime { get; set; }

    [MaxLength(20)]
    public string? EndTime { get; set; }

    [MaxLength(500)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? MeetingReference { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Scheduled";

    [MaxLength(450)]
    public string? Chair { get; set; }

    public int? ChairUserId { get; set; }

    public string? Attendees { get; set; }
    public string? Notes { get; set; }
    public string? CopilotSummaryNotes { get; set; }
    public string? AgendaJson { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
