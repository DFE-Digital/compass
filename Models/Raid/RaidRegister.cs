using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public enum RaidRegisterRole
{
    Owner = 0,
    Manager = 1,
    Viewer = 2
}

/// <summary>
/// A user-created RAID register that groups risks, issues, assumptions,
/// dependencies and near misses under a defined scope.
/// </summary>
public class RaidRegister
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? DirectorateLookupId { get; set; }

    [ForeignKey(nameof(DirectorateLookupId))]
    public DirectorateLookup? DirectorateLookup { get; set; }

    public int? BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup? BusinessAreaLookup { get; set; }

    public int CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedByUser { get; set; } = null!;

    public bool IsDeleted { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Required] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Scope: which work items and services this register covers
    public ICollection<RaidRegisterWorkItem> WorkItems { get; set; } = new List<RaidRegisterWorkItem>();
    public ICollection<RaidRegisterService> Services { get; set; } = new List<RaidRegisterService>();

    /// <summary>Organisational scope — directorates (many).</summary>
    public ICollection<RaidRegisterDirectorate> Directorates { get; set; } = new List<RaidRegisterDirectorate>();

    /// <summary>Organisational scope — portfolios / business areas (many).</summary>
    public ICollection<RaidRegisterBusinessArea> BusinessAreas { get; set; } = new List<RaidRegisterBusinessArea>();

    // Access: explicit user management
    public ICollection<RaidRegisterUser> Users { get; set; } = new List<RaidRegisterUser>();

    // RAID items linked to this register (many-to-many)
    public ICollection<RaidRegisterRisk> Risks { get; set; } = new List<RaidRegisterRisk>();
    public ICollection<RaidRegisterIssue> Issues { get; set; } = new List<RaidRegisterIssue>();
    public ICollection<RaidRegisterAssumption> Assumptions { get; set; } = new List<RaidRegisterAssumption>();
    public ICollection<RaidRegisterDependency> Dependencies { get; set; } = new List<RaidRegisterDependency>();
    public ICollection<RaidRegisterNearMiss> NearMisses { get; set; } = new List<RaidRegisterNearMiss>();
}
