using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ProductMetricValue
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductReturnId { get; set; }

    [Required]
    public int PerformanceMetricId { get; set; }

    public string? Value { get; set; }

    public bool IsComplete { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ProductReturnId")]
    public ProductReturn? ProductReturn { get; set; }

    [ForeignKey("PerformanceMetricId")]
    public PerformanceMetric? PerformanceMetric { get; set; }
}

