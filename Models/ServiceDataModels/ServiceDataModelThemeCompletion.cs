using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.ServiceDataModels;

/// <summary>
/// Records that a user marked a census theme complete for an assignment.
/// Keyed by theme stable key so shared-base theme id churn does not lose the flag.
/// </summary>
[Table("ServiceDataModelThemeCompletions")]
public class ServiceDataModelThemeCompletion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ServiceDataModelAssignmentId { get; set; }

    [ForeignKey(nameof(ServiceDataModelAssignmentId))]
    public ServiceDataModelAssignment Assignment { get; set; } = null!;

    /// <summary>Stable key of the shared base theme (survives theme catalogue id changes).</summary>
    [Required, MaxLength(100)]
    public string ThemeStableKey { get; set; } = string.Empty;

    public DateTime CompletedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(320)]
    public string? CompletedByEmail { get; set; }
}
