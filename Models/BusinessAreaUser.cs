using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class BusinessAreaUser
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
