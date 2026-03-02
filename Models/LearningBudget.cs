using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents the L&D budget for a financial year
/// </summary>
public class LearningBudget
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FinancialYear { get; set; } // e.g., 2025

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalBudget { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Spent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Forecasted { get; set; }

    [StringLength(255)]
    public string? CreatedBy { get; set; }

    [StringLength(255)]
    public string? UpdatedBy { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true; // Only one active budget per FY
}

