using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Central Ops / Admin time recording against activity type, themes, optional work item and business area.</summary>
public class ActivityTimeEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Calendar date the time was spent.</summary>
    [Required]
    [Column(TypeName = "date")]
    public DateTime ActivityDate { get; set; }

    /// <summary>Total time spent in minutes (hours × 60 + minutes).</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Short description of what the activity was about.</summary>
    [MaxLength(1000)]
    public string? Narrative { get; set; }

    public int ActivityTimeTypeLookupId { get; set; }

    [ForeignKey(nameof(ActivityTimeTypeLookupId))]
    public ActivityTimeTypeLookup? ActivityTimeTypeLookup { get; set; }

    /// <summary>Optional related work item (Project).</summary>
    public int? ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    /// <summary>Optional business area supported by this activity.</summary>
    public int? BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup? BusinessAreaLookup { get; set; }

    [MaxLength(320)]
    public string CreatedByEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CreatedByName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ActivityTimeEntryTheme> Themes { get; set; } = new List<ActivityTimeEntryTheme>();
}
