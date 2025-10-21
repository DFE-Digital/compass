using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class Objective
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Theme { get; set; }

    public string? Description { get; set; }

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; } // proposed, active, paused, completed, cancelled

    [MaxLength(10)]
    public string? RagStatus { get; set; } // red, amber, green

    public string? SuccessMeasures { get; set; }

    [Range(0, 100)]
    public int? ProgressPercent { get; set; }

    public bool IsDeleted { get; set; } = false;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Risk> Risks { get; set; } = new List<Risk>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    public ICollection<Models.Action> Actions { get; set; } = new List<Models.Action>();
}

