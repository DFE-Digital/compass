using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectBudgetOwner
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    public int BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

