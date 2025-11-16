using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectKpiInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? KpiId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(50)]
    public string? CategoryCode { get; set; }

    [MaxLength(100)]
    public string? CategoryOther { get; set; }

    public string? Description { get; set; }

    [MaxLength(50)]
    public string? UnitOfMeasure { get; set; }

    [MaxLength(100)]
    public string? UnitOfMeasureOther { get; set; }

    public string? CalculationMethod { get; set; }

    [MaxLength(50)]
    public string? Frequency { get; set; }

    public decimal? TargetValue { get; set; }

    public string? Thresholds { get; set; }

    public string? DataSource { get; set; }

    [MaxLength(200)]
    public string? DataSourceOther { get; set; }

    public List<string> ReportingStages { get; set; } = new();

    [MaxLength(50)]
    public string? Status { get; set; }

    public int? ObjectiveId { get; set; }

    public int? MilestoneId { get; set; }

    public bool Active { get; set; } = true;
}
