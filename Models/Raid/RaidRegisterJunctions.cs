using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class RaidRegisterRisk
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int RiskId { get; set; }

    [ForeignKey(nameof(RiskId))]
    public Risk Risk { get; set; } = null!;

    [Required] public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int? AddedByUserId { get; set; }

    [ForeignKey(nameof(AddedByUserId))]
    public User? AddedByUser { get; set; }
}

public class RaidRegisterIssue
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int IssueId { get; set; }

    [ForeignKey(nameof(IssueId))]
    public Issue Issue { get; set; } = null!;

    [Required] public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int? AddedByUserId { get; set; }

    [ForeignKey(nameof(AddedByUserId))]
    public User? AddedByUser { get; set; }
}

public class RaidRegisterAssumption
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int AssumptionId { get; set; }

    [ForeignKey(nameof(AssumptionId))]
    public Assumption Assumption { get; set; } = null!;

    [Required] public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int? AddedByUserId { get; set; }

    [ForeignKey(nameof(AddedByUserId))]
    public User? AddedByUser { get; set; }
}

public class RaidRegisterDependency
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int DependencyId { get; set; }

    [ForeignKey(nameof(DependencyId))]
    public Dependency Dependency { get; set; } = null!;

    [Required] public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int? AddedByUserId { get; set; }

    [ForeignKey(nameof(AddedByUserId))]
    public User? AddedByUser { get; set; }
}

public class RaidRegisterNearMiss
{
    public int RaidRegisterId { get; set; }

    [ForeignKey(nameof(RaidRegisterId))]
    public RaidRegister RaidRegister { get; set; } = null!;

    public int NearMissId { get; set; }

    [ForeignKey(nameof(NearMissId))]
    public NearMiss NearMiss { get; set; } = null!;

    [Required] public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public int? AddedByUserId { get; set; }

    [ForeignKey(nameof(AddedByUserId))]
    public User? AddedByUser { get; set; }
}
