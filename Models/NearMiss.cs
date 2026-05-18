using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>RAID near miss / unexpected issue register entry.</summary>
public class NearMiss
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Business reference (e.g. NM-0042). Auto-generated when empty on create.</summary>
    [Required]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    public DateTime DateLogged { get; set; }

    public int? NearMissTypeId { get; set; }

    [ForeignKey(nameof(NearMissTypeId))]
    public NearMissType? TypeLookup { get; set; }

    public int? DirectorateLookupId { get; set; }

    [ForeignKey(nameof(DirectorateLookupId))]
    public DirectorateLookup? DirectorateLookup { get; set; }

    public int? BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup? BusinessAreaLookup { get; set; }

    [MaxLength(4000)]
    public string? Impact { get; set; }

    public int? NearMissSeriousnessId { get; set; }

    [ForeignKey(nameof(NearMissSeriousnessId))]
    public NearMissSeriousness? SeriousnessLookup { get; set; }

    public int? PostMitigationRagStatusLookupId { get; set; }

    [ForeignKey(nameof(PostMitigationRagStatusLookupId))]
    public RagStatusLookup? PostMitigationRagStatusLookup { get; set; }

    public int? RiskTierId { get; set; }

    [ForeignKey(nameof(RiskTierId))]
    public RiskTier? RiskTier { get; set; }

    public int? NearMissStatusId { get; set; }

    [ForeignKey(nameof(NearMissStatusId))]
    public NearMissStatus? StatusLookup { get; set; }

    public bool IsDeleted { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }

    [ForeignKey(nameof(UpdatedByUserId))]
    public User? UpdatedByUser { get; set; }

    public ICollection<NearMissOwner> NearMissOwners { get; set; } = new List<NearMissOwner>();

    public ICollection<NearMissAction> NearMissActions { get; set; } = new List<NearMissAction>();

    public ICollection<NearMissMitigation> NearMissMitigations { get; set; } = new List<NearMissMitigation>();
}
