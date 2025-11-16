using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class TriageMeeting
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DemandRequest> DemandRequests { get; set; } = new List<DemandRequest>();
}
