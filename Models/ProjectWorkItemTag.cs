using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Junction: <see cref="Project"/> ↔ <see cref="WorkItemTagLookup"/>.</summary>
public class ProjectWorkItemTag
{
    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public int WorkItemTagLookupId { get; set; }

    public WorkItemTagLookup WorkItemTagLookup { get; set; } = null!;
}
