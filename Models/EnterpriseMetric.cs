using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Enterprise-level metrics reported monthly (not tied to products)
/// </summary>
public class EnterpriseMetric
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Identifier { get; set; } = string.Empty; // ent-x format

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? HintText { get; set; }

    [Required]
    public ValueType ValueType { get; set; }

    [Required]
    public string ValidationRules { get; set; } = "{}"; // JSON structure

    [Required]
    public int ValidFromYear { get; set; }
    
    [Required]
    public int ValidFromMonth { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a monthly enterprise reporting period
/// </summary>
public class EnterpriseReturn
{
    public int Id { get; set; }
    
    [Required]
    public int Year { get; set; }
    
    [Required]
    public int Month { get; set; }
    
    [Required]
    public ReturnStatus Status { get; set; }
    
    public DateTime? SubmittedDate { get; set; }
    
    [StringLength(200)]
    public string? SubmittedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public List<EnterpriseMetricValue> MetricValues { get; set; } = new();
}

/// <summary>
/// Value submitted for a specific enterprise metric in a specific month
/// </summary>
public class EnterpriseMetricValue
{
    public int Id { get; set; }
    
    [Required]
    public int EnterpriseReturnId { get; set; }
    
    [Required]
    public int EnterpriseMetricId { get; set; }
    
    public string? Value { get; set; }
    
    public bool IsComplete { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public EnterpriseReturn? EnterpriseReturn { get; set; }
    public EnterpriseMetric? EnterpriseMetric { get; set; }
}

