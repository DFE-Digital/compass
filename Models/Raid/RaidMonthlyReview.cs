using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.Raid;

/// <summary>Records that a risk or issue was reviewed in a calendar month (monthly RAID review workflow).</summary>
public class RaidMonthlyReview
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary><c>risk</c> or <c>issue</c>.</summary>
    [Required]
    [MaxLength(10)]
    public string RecordType { get; set; } = string.Empty;

    public int RecordId { get; set; }

    public int ReviewYear { get; set; }

    public int ReviewMonth { get; set; }

    public int ReviewedByUserId { get; set; }

    [ForeignKey(nameof(ReviewedByUserId))]
    public User? ReviewedByUser { get; set; }

    public DateTime ReviewedAtUtc { get; set; }

    [MaxLength(4000)]
    public string? MonthlyComment { get; set; }
}
