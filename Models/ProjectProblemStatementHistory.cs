using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectProblemStatementHistory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectProblemStatementId { get; set; }

    [ForeignKey(nameof(ProjectProblemStatementId))]
    public ProjectProblemStatement ProjectProblemStatement { get; set; } = null!;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ProblemStatement { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? ChangedByEmail { get; set; }

    [MaxLength(200)]
    public string? ChangedByName { get; set; }

    [Required]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

