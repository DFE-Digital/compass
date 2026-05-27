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

/// <summary>Links a RAID register to a directorate within its organisational scope.</summary>
public class RaidRegisterDirectorate
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int DirectorateLookupId { get; set; }

    [ForeignKey(nameof(DirectorateLookupId))]
    public DirectorateLookup DirectorateLookup { get; set; } = null!;
}

/// <summary>Links a RAID register to a portfolio / business area within its organisational scope.</summary>
public class RaidRegisterBusinessArea
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int BusinessAreaLookupId { get; set; }

    [ForeignKey(nameof(BusinessAreaLookupId))]
    public BusinessAreaLookup BusinessAreaLookup { get; set; } = null!;
}
