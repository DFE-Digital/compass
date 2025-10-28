using System.Text.Json.Serialization;

namespace Compass.Models;

/// <summary>
/// DTO for Standard data from Standards CMS API
/// </summary>
public class StandardDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    [JsonPropertyName("legacyId")]
    public int? LegacyId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Slug { get; set; } = string.Empty;
    
    public string? Summary { get; set; }
    
    public string? Purpose { get; set; }
    
    [JsonPropertyName("howToMeet")]
    public string? HowToMeet { get; set; }
    
    public string? Governance { get; set; }
    
    [JsonPropertyName("governanceApproval")]
    public bool GovernanceApproval { get; set; }
    
    [JsonPropertyName("legalStandard")]
    public bool? LegalStandard { get; set; }
    
    [JsonPropertyName("relatedGuidance")]
    public string? RelatedGuidance { get; set; }
    
    [JsonPropertyName("legalBasis")]
    public string? LegalBasis { get; set; }
    
    [JsonPropertyName("validityPeriod")]
    public int? ValidityPeriod { get; set; }
    
    public decimal Version { get; set; }
    
    [JsonPropertyName("previousVersion")]
    public decimal PreviousVersion { get; set; }
    
    [JsonPropertyName("draftCreated")]
    public DateTime? DraftCreated { get; set; }
    
    [JsonPropertyName("firstPublished")]
    public DateTime? FirstPublished { get; set; }
    
    [JsonPropertyName("lastUpdated")]
    public DateTime? LastUpdated { get; set; }
    
    [JsonPropertyName("standardId")]
    public int? StandardId { get; set; }
    
    [JsonPropertyName("isModified")]
    public bool? IsModified { get; set; }
    
    public StandardStageDto? Stage { get; set; }
    
    public List<StandardCategoryDto>? Categories { get; set; }
    
    [JsonPropertyName("sub_categories")]
    public List<StandardSubCategoryDto>? SubCategories { get; set; }
    
    public List<StandardPhaseDto>? Phases { get; set; }
    
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    public List<StandardOwnerDto>? Owners { get; set; }
    
    public List<StandardContactDto>? Contacts { get; set; }
    
    [JsonPropertyName("approvedProducts")]
    public List<StandardProductDto>? ApprovedProducts { get; set; }
    
    [JsonPropertyName("toleratedProducts")]
    public List<StandardProductDto>? ToleratedProducts { get; set; }
}

public class StandardOwnerDto
{
    public int Id { get; set; }
    
    // API returns 'firstName' (camelCase) - JsonSerializer handles case-insensitive matching
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    // API returns 'lastName' (camelCase)
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    
    public string? Email { get; set; }
    
    [JsonPropertyName("JobRole")]
    public string? JobRole { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class StandardContactDto
{
    public int Id { get; set; }
    
    // API returns 'firstName' (camelCase) - JsonSerializer handles case-insensitive matching
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    // API returns 'lastName' (camelCase)
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    
    public string? Email { get; set; }
    
    [JsonPropertyName("JobRole")]
    public string? JobRole { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class StandardStageDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    // Schema shows 'title' field
    public string Title { get; set; } = string.Empty;
    
    public int Number { get; set; }
    
    public bool Active { get; set; }
}

public class StandardCategoryDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    // API returns 'title' for categories (see Node.js implementation)
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public bool Active { get; set; }
    
    public string Slug { get; set; } = string.Empty;
}

public class StandardSubCategoryDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    // Schema shows 'title' field
    public string Title { get; set; } = string.Empty;
    
    public bool Active { get; set; }
    
    public string Slug { get; set; } = string.Empty;
    
    // Parent category relation
    public StandardCategoryDto? Category { get; set; }
}

public class StandardPhaseDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    // Schema shows 'Title' (capital T) - map to Title property
    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;
    
    // Schema shows 'Enabled' (capital E)
    [JsonPropertyName("Enabled")]
    public bool? Enabled { get; set; }
}

public class StandardProductDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string? Vendor { get; set; }
    
    public string? Version { get; set; }
    
    [JsonPropertyName("useCase")]
    public string? UseCase { get; set; }
    
    [JsonPropertyName("externalLink")]
    public string? ExternalLink { get; set; }
}

