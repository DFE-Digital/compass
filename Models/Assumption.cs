using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// RAID assumption register entry; may be scoped to a work item, FIPS product, or organisation-wide.
/// </summary>
public class Assumption
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    public int? PrimaryProductId { get; set; }

    [ForeignKey(nameof(PrimaryProductId))]
    public FipsService? PrimaryProduct { get; set; }

    /// <summary>WorkItem, Product, or Organisation — see <see cref="RaidAssociationKinds"/>.</summary>
    [MaxLength(20)]
    public string? RaidAssociationKind { get; set; }

    public int? SroUserId { get; set; }

    [ForeignKey(nameof(SroUserId))]
    public User? SroUser { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public int? OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? OwnerUser { get; set; }

    public int? AssumptionCriticalityId { get; set; }

    [ForeignKey(nameof(AssumptionCriticalityId))]
    public AssumptionCriticality? CriticalityLookup { get; set; }

    public int? AssumptionStatusId { get; set; }

    [ForeignKey(nameof(AssumptionStatusId))]
    public AssumptionStatus? StatusLookup { get; set; }

    public DateTime? ReviewDate { get; set; }

    [MaxLength(500)]
    public string? ValidationOutcome { get; set; }

    public bool IsDeleted { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Modern RAID: divisions (multi-select).</summary>
    public ICollection<AssumptionDivision> AssumptionDivisions { get; set; } = new List<AssumptionDivision>();

    /// <summary>Modern RAID: business areas from admin lookup (multi-select).</summary>
    public ICollection<AssumptionBusinessArea> AssumptionBusinessAreas { get; set; } = new List<AssumptionBusinessArea>();
}
