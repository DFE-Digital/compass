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

    public bool IsNotCaptured { get; set; } = false;
    
    public string? NotCapturedReason { get; set; }
    
    public string? ReasonForDifference { get; set; }

    // Not mapped - calculated property for previous month's value
    [NotMapped]
    public string? PreviousValue { get; set; }

    // Navigation properties
    [ForeignKey("ProductReturnId")]
    public ProductReturn? ProductReturn { get; set; }

    [ForeignKey("PerformanceMetricId")]
    public PerformanceMetric? PerformanceMetric { get; set; }
}

