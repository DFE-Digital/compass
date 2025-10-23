using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectDraft
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Aim { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? TargetDeliveryDate { get; set; }

    public DateTime? ActualDeliveryDate { get; set; }

    [Required]
    public bool IsFlagship { get; set; } = false;

    [Required]
    public bool IsAiInitiative { get; set; } = false;

    [Required]
    [MaxLength(20)]
    public string RagStatus { get; set; } = "Green";

    [MaxLength(500)]
    public string? RagJustification { get; set; }

    [MaxLength(50)]
    public string? Phase { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? TotalPermFte { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? TotalMspFte { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    // JSON fields to store complex data
    public string? MissionIdsJson { get; set; } // JSON array of mission IDs
    public string? FundingAllocationsJson { get; set; } // JSON array of funding allocations
    public string? OutcomesJson { get; set; } // JSON array of outcomes
    public string? ContactsJson { get; set; } // JSON array of project contacts
    public string? ObjectiveIdsJson { get; set; } // JSON array of objective IDs

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty; // To associate with user session

    [Required]
    [MaxLength(100)]
    public string UserEmail { get; set; } = string.Empty; // To track who created the draft

    public bool IsConfirmed { get; set; } = false; // Whether this draft has been converted to a project
}
