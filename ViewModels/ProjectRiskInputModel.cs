using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectRiskInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? RiskId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [MaxLength(100)]
    public string? BusinessArea { get; set; }

    [MaxLength(1000)]
    public string? HowIdentified { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? OwnerEmail { get; set; }

    public int? OwnerUserId { get; set; }

    [MaxLength(255)]
    public string? OwnerName { get; set; }

    public int? RiskStatusId { get; set; }

    public int? RiskPriorityId { get; set; }

    public int? RiskCategoryId { get; set; }

    public int? RiskImpactLevelId { get; set; }

    public int? RiskLikelihoodId { get; set; }

    public int? RiskProximityId { get; set; }

    public int? RiskTierId { get; set; }

    public int? GovernanceBoardId { get; set; }

    [DataType(DataType.Date)]
    public DateTime? IdentifiedDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? NextReviewDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ProximityDate { get; set; }

    [MaxLength(20)]
    public string? Response { get; set; }

    public string? ResponseStrategy { get; set; }

    public string? Notes { get; set; }

    public List<int> SelectedRiskTypeIds { get; set; } = new();

    public List<int> LinkedIssueIds { get; set; } = new();

    public List<int> LinkedActionIds { get; set; } = new();

    public List<int> LinkedDecisionIds { get; set; } = new();
}

