using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Issue
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
    public string? Category { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    [Required]
    [MaxLength(10)]
    public string Severity { get; set; } = "medium"; // low, medium, high, critical

    [MaxLength(10)]
    public string? Priority { get; set; } // low, medium, high

    [Required]
    public DateTime DetectedDate { get; set; } = DateTime.UtcNow;

    public DateTime? TargetResolutionDate { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "open"; // open, in_progress, blocked, resolved, closed

    public string? ResolutionSummary { get; set; }

    public string? Workaround { get; set; }

    public bool BlockedFlag { get; set; } = false;

    public DateTime? ClosedDate { get; set; }

    public bool IsDeleted { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<IssueAction> IssueActions { get; set; } = new List<IssueAction>();
    public ICollection<MilestoneIssue> MilestoneIssues { get; set; } = new List<MilestoneIssue>();
    public ICollection<Dependency> DependenciesAsSource { get; set; } = new List<Dependency>();
    public ICollection<Dependency> DependenciesAsTarget { get; set; } = new List<Dependency>();
}

