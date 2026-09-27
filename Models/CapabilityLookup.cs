using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Admin-managed capability catalogue entry used by census Lookup questions.
/// </summary>
[Table("CapabilityLookups")]
public class CapabilityLookup
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Human reference code (e.g. GRC27372). Unique among capabilities; not the system id.</summary>
    [Required, MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Display label for census options: Title (Reference).</summary>
    public string DisplayLabel => $"{Title} ({Reference})";
}
