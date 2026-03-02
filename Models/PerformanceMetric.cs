using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Compass.Models;

public class PerformanceMetric
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Identifier { get; set; } = string.Empty; // perf-x format

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string? HintText { get; set; }

    /// <summary>
    /// Explanation of why this performance metric is captured
    /// </summary>
    [StringLength(1000)]
    public string? Purpose { get; set; }

    [Required]
    public ValueType ValueType { get; set; }

    [Required]
    public string ValidationRules { get; set; } = "{}"; // JSON structure

    [Required]
    public int ValidFromYear { get; set; }
    
    [Required]
    public int ValidFromMonth { get; set; }
    
    /// <summary>
    /// Comma-separated list of phases this metric applies to (e.g., "Discovery,Alpha,Beta")
    /// Empty string means applies to all phases
    /// </summary>
    public string ApplicablePhases { get; set; } = string.Empty;
    
    /// <summary>
    /// Comma-separated list of types this metric applies to (e.g., "Website,App,Mobile app")
    /// Empty string means applies to all types
    /// </summary>
    public string ApplicableTypes { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates if this metric is disabled and should not appear in new returns
    /// </summary>
    public bool IsDisabled { get; set; } = false;
    
    /// <summary>
    /// Optional ID of another metric that must have a value for this metric to be shown
    /// If null, this metric is always shown (subject to other conditions)
    /// </summary>
    public int? ConditionalOnMetricId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum ValueType
{
    Text = 0,
    Number = 1,
    Decimal = 2,
    Percentage = 3
}

public class ValidationRules
{
    [JsonPropertyName("required")]
    public bool Required { get; set; }
    
    [JsonPropertyName("allowNull")]
    public bool AllowNull { get; set; }
    
    [JsonPropertyName("minimumValue")]
    public decimal? MinimumValue { get; set; }
    
    [JsonPropertyName("maximumValue")]
    public decimal? MaximumValue { get; set; }
    
    [JsonPropertyName("decimalPlaces")]
    public int? DecimalPlaces { get; set; }
    
    // Legacy support for old format
    public Range? Range { get; set; }
}

public class Range
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}

