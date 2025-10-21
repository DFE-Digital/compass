using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Represents a standalone assessment of a Functional Standard
/// </summary>
public class FunctionalStandardAssessment
{
    public int Id { get; set; }
    
    [Required]
    public int FunctionalStandardId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string AssessmentName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string AssessedBy { get; set; } = string.Empty;
    
    public DateTime AssessmentDate { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime? SubmittedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public FunctionalStandard? FunctionalStandard { get; set; }
    public List<AssessmentCriteriaResponse> CriteriaResponses { get; set; } = new();
}

/// <summary>
/// Attainment level for a criterion
/// </summary>
public enum AttainmentLevel
{
    NotOrSeldomMet = 0,      // Value: 0
    PartiallyMet = 1,        // Value: 0.5
    FullyMet = 2             // Value: 1
}

/// <summary>
/// Represents a response to a specific criterion in an assessment
/// </summary>
public class AssessmentCriteriaResponse
{
    public int Id { get; set; }
    
    [Required]
    public int AssessmentId { get; set; }
    
    [Required]
    public int FunctionalStandardId { get; set; }
    
    [Required]
    public int ThemeId { get; set; }
    
    [Required]
    public decimal PracticeAreaId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string CriteriaCode { get; set; } = string.Empty;
    
    public AttainmentLevel? Attainment { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public FunctionalStandardAssessment? Assessment { get; set; }
    public Criterion? Criterion { get; set; }
    
    /// <summary>
    /// Gets the numeric value for this attainment level
    /// </summary>
    public decimal GetAttainmentValue()
    {
        return Attainment switch
        {
            AttainmentLevel.NotOrSeldomMet => 0m,
            AttainmentLevel.PartiallyMet => 0.5m,
            AttainmentLevel.FullyMet => 1m,
            _ => 0m
        };
    }
}

