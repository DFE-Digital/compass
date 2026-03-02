using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProjectWatchlist
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Unique constraint to prevent duplicate entries
    // This will be enforced at the database level via migration
}


