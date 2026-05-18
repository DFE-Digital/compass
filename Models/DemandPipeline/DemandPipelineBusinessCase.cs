using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>
/// Pre–demand-register business case (Compass2-aligned). Table is separate from legacy <see cref="Compass.Models.BusinessCase"/>.
/// </summary>
[Table("DemandPipelineBusinessCases")]
public class DemandPipelineBusinessCase
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Stage { get; set; } = "Idea";

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    [MaxLength(450)]
    public string? Lead { get; set; }

    public int? LeadUserId { get; set; }

    [MaxLength(450)]
    public string? Sro { get; set; }

    public int? SroUserId { get; set; }

    [MaxLength(450)]
    public string? SubmittedBy { get; set; }

    public DateTime? SubmittedDate { get; set; }

    [MaxLength(200)]
    public string? BusinessArea { get; set; }

    [MaxLength(200)]
    public string? DepartmentGroup { get; set; }

    public int? DirectorateId { get; set; }
    public int? PortfolioId { get; set; }
    public int? GovernmentDepartmentId { get; set; }

    public string? ProblemStatement { get; set; }
    public string? ProposedSolution { get; set; }
    public string? Evidence { get; set; }
    public string? Benefits { get; set; }

    public bool? StatutoryDriver { get; set; }
    public string? StatutoryDriverComments { get; set; }
    public string? StatutoryReference { get; set; }

    [MaxLength(50)]
    public string? FundingPosition { get; set; }

    public string? FundingComments { get; set; }
    public string? LinkedWorkAndDemands { get; set; }
    public bool? HeadcountIdentified { get; set; }

    [MaxLength(10)]
    public string? SubjectToInvestco { get; set; }

    public int? PriorityOutcomeId { get; set; }
    public int? MissionPillarId { get; set; }

    [MaxLength(500)]
    public string? PriorityOutcomeIds { get; set; }

    [MaxLength(500)]
    public string? MissionPillarIds { get; set; }

    public DateTime? TargetSubmissionDate { get; set; }
    public Guid? LinkedDemandRequestId { get; set; }

    public DateTime CreatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
