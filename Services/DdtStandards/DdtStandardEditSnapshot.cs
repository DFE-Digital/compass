namespace Compass.Services.DdtStandards;

/// <summary>JSON snapshot of a DDT standard used when discarding in-place edits.</summary>
internal sealed class DdtStandardEditSnapshot
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Purpose { get; set; }
    public string? Criteria { get; set; }
    public string? HowToMeet { get; set; }
    public string? Governance { get; set; }
    public bool GovernanceApproval { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? PreviousVersion { get; set; }
    public bool LegalStandard { get; set; }
    public string? LegalBasis { get; set; }
    public int? ValidityPeriod { get; set; }
    public string? RelatedGuidance { get; set; }
    public List<int> CategoryIds { get; set; } = [];
    public List<int> SubCategoryIds { get; set; } = [];
    public List<int> PhaseIds { get; set; } = [];
    public List<int> OwnerUserIds { get; set; } = [];
    public List<int> ContactUserIds { get; set; } = [];
}
