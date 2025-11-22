using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Validation rules for standards as code - enables programmatic validation
/// </summary>
public class DdtStandardValidationRule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Unique rule identifier (e.g., "req-001")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Rule name/title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rule description
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Rule type: must, should, could
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "must"; // must, should, could

    /// <summary>
    /// Priority: high, medium, low
    /// </summary>
    [MaxLength(20)]
    public string? Priority { get; set; }

    /// <summary>
    /// Category of the rule (e.g., "content", "interaction", "visual")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Validation type: automated, manual, hybrid
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ValidationType { get; set; } = "manual"; // automated, manual, hybrid

    /// <summary>
    /// Validator identifier (for automated validation)
    /// </summary>
    [MaxLength(200)]
    public string? Validator { get; set; }

    /// <summary>
    /// Validation rule configuration (JSON)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Config { get; set; }

    /// <summary>
    /// Severity: error, warning, info
    /// </summary>
    [MaxLength(20)]
    public string? Severity { get; set; }

    /// <summary>
    /// Whether this rule is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

