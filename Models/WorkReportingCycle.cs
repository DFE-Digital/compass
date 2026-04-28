using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>
/// Reporting cycle (e.g. monthly work). Maps to <c>ReportingCycles</c> — same concept as Compass2.
/// Named <see cref="WorkReportingCycle"/> to avoid clashing with view-model types under <c>Models.Modern.Work</c>.
/// Production monthly due dates and status use <see cref="MonthlyUpdateDeadlineConfig"/> via <see cref="IMonthlyUpdateService"/>; this entity is not wired into that path unless explicitly integrated later.
/// </summary>
public class WorkReportingCycle
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string PeriodType { get; set; } = "Monthly";

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public ICollection<WorkReportingCyclePeriod> Periods { get; set; } = new List<WorkReportingCyclePeriod>();
}
