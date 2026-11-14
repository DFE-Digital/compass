using System.ComponentModel.DataAnnotations;
using Compass.Models.Fips;

namespace Compass.Models.Census;

public static class ServiceCensusStatuses
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Complete = "COMPLETE";
}

/// <summary>Draft-to-submit workspace for the redesigned service census.</summary>
public class ServiceCensusResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public CMDBProduct Product { get; set; } = null!;

    [Required, MaxLength(40)]
    public string Status { get; set; } = ServiceCensusStatuses.Draft;

    [MaxLength(4000)]
    public string? ServiceDescription { get; set; }

    [MaxLength(4000)]
    public string? ServiceLineDescription { get; set; }

    [MaxLength(4000)]
    public string? AccessibilityNotEnrolledReason { get; set; }

    public DateTime? SubmittedAt { get; set; }

    [MaxLength(255)]
    public string? SubmittedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    public ICollection<ServiceCensusTaskState> Tasks { get; set; } = new List<ServiceCensusTaskState>();
    public ICollection<ServiceCensusContactReview> ContactReviews { get; set; } = new List<ServiceCensusContactReview>();
}

public class ServiceCensusTaskState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResponseId { get; set; }

    public ServiceCensusResponse Response { get; set; } = null!;

    [Required, MaxLength(80)]
    public string TaskKey { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Status { get; set; } = ServiceCensusStatuses.NotStarted;

    public bool CannotAnswer { get; set; }

    [MaxLength(4000)]
    public string? CannotAnswerReason { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    public ICollection<ServiceCensusAnswer> Answers { get; set; } = new List<ServiceCensusAnswer>();
}

public class ServiceCensusAnswer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskStateId { get; set; }

    public ServiceCensusTaskState TaskState { get; set; } = null!;

    [MaxLength(300)]
    public string? Title { get; set; }

    [MaxLength(10000)]
    public string? Narrative { get; set; }

    [MaxLength(80)]
    public string? LookupCode { get; set; }

    [MaxLength(80)]
    public string? SecondaryLookupCode { get; set; }

    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    public int SortOrder { get; set; }
}

public class ServiceCensusContactReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResponseId { get; set; }

    public ServiceCensusResponse Response { get; set; } = null!;

    [Required, MaxLength(80)]
    public string RoleKey { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CmdbName { get; set; }

    [MaxLength(320)]
    public string? CmdbEmail { get; set; }

    public bool IsIncorrect { get; set; }

    [MaxLength(4000)]
    public string? IncorrectReason { get; set; }
}
