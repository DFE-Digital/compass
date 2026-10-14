using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Many-to-many: activity time entry ↔ theme lookup.</summary>
public class ActivityTimeEntryTheme
{
    public int ActivityTimeEntryId { get; set; }

    [ForeignKey(nameof(ActivityTimeEntryId))]
    public ActivityTimeEntry ActivityTimeEntry { get; set; } = null!;

    public int ActivityTimeThemeLookupId { get; set; }

    [ForeignKey(nameof(ActivityTimeThemeLookupId))]
    public ActivityTimeThemeLookup ActivityTimeThemeLookup { get; set; } = null!;
}
