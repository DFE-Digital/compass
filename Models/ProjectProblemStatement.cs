using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectProblemStatement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string ProblemStatement { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? CreatedByEmail { get; set; }

    [MaxLength(200)]
    public string? CreatedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for history
    public ICollection<ProjectProblemStatementHistory> History { get; set; } = new List<ProjectProblemStatementHistory>();
}

