using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Links a RAID register to a work item (Project) within its scope.
/// </summary>
public class RaidRegisterWorkItem
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int ProjectId { get; set; }

    [ForeignKey(nameof(ProjectId))]
    public Project Project { get; set; } = null!;
}

/// <summary>
/// Links a RAID register to a service register entry (FipsService) within its scope.
/// </summary>
public class RaidRegisterService
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int FipsServiceId { get; set; }

    [ForeignKey(nameof(FipsServiceId))]
    public FipsService FipsService { get; set; } = null!;
}
