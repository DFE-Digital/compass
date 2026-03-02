using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ProductReturn
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string ProductDocumentId { get; set; } = string.Empty; // Product DocumentID from CMS (primary identifier)

    [StringLength(50)]
    public string? FipsId { get; set; } // Product FIPS ID (legacy, kept for backwards compatibility)

    [Required]
    public int Year { get; set; }

    [Required]
    public int Month { get; set; }

    [Required]
    public ReturnStatus Status { get; set; } = ReturnStatus.Upcoming;

    public DateTime? SubmittedDate { get; set; }

    [StringLength(255)]
    public string? SubmittedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<ProductMetricValue> MetricValues { get; set; } = new List<ProductMetricValue>();
}

public enum ReturnStatus
{
    Upcoming = 0,
    Due = 1,
    Late = 2,
    Submitted = 3,
    Draft = 4
}

