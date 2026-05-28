using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectRagHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    // RAG Status - using foreign key to RagStatusLookup
    public int? RagStatusLookupId { get; set; }
    [ForeignKey(nameof(RagStatusLookupId))]
    public RagStatusLookup? RagStatusLookup { get; set; }

    [MaxLength(20)]
    [Obsolete("Use RagStatusLookupId instead. This property is kept for backward compatibility.")]
    public string RagStatus { get; set; } = string.Empty; // Deprecated: Use RagStatusLookupId

    [MaxLength(4000)]
    public string? Justification { get; set; }

    [MaxLength(4000)]
    public string? PathToGreen { get; set; }

    [Required]
    [MaxLength(255)]
    public string ChangedByEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ChangedByName { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
