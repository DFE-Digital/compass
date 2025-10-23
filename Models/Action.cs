using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Action
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ObjectiveId { get; set; }

    [ForeignKey(nameof(ObjectiveId))]
    public Objective? Objective { get; set; }

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    [MaxLength(50)]
    public string? FipsId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    public int? ActionSourceId { get; set; }

    [ForeignKey(nameof(ActionSourceId))]
    public ActionSource? ActionSource { get; set; }

    [MaxLength(255)]
    public string? AssignedToEmail { get; set; }

    [MaxLength(10)]
    public string? Priority { get; set; } // low, medium, high

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "not_started"; // not_started, in_progress, blocked, done, cancelled

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public int? ParentActionId { get; set; }

    [ForeignKey(nameof(ParentActionId))]
    public Action? ParentAction { get; set; }

    [MaxLength(500)]
    public string? EvidenceUrl { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Action> SubActions { get; set; } = new List<Action>();
    public ICollection<RiskAction> RiskActions { get; set; } = new List<RiskAction>();
    public ICollection<IssueAction> IssueActions { get; set; } = new List<IssueAction>();
    public ICollection<MilestoneAction> MilestoneActions { get; set; } = new List<MilestoneAction>();
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();
}

