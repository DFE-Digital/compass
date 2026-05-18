using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class NearMissMitigation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int NearMissId { get; set; }

    [ForeignKey(nameof(NearMissId))]
    public NearMiss NearMiss { get; set; } = null!;

    [Required]
    public DateTime MitigationDate { get; set; }

    [Required]
    [MaxLength(4000)]
    public string AssuranceTakenPlace { get; set; } = string.Empty;

    public int? RecordedByUserId { get; set; }

    [ForeignKey(nameof(RecordedByUserId))]
    public User? RecordedByUser { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
