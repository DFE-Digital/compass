using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Compass.Models.Fips;

namespace Compass.Models.ServiceSchema;

/// <summary>DDaT or locally managed staff role. Used for contacts that are not CMDB contact types.</summary>
public class StaffRole
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Family { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? SourceUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsFrameworkRole { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ServiceSchemaLookupSet
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ServiceSchemaLookupValue> Values { get; set; } = new List<ServiceSchemaLookupValue>();
}

public class ServiceSchemaLookupValue
{
    public int Id { get; set; }

    public int LookupSetId { get; set; }

    public ServiceSchemaLookupSet LookupSet { get; set; } = null!;

    [Required, MaxLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Local service line for the service census. Distinct from portfolio <see cref="ServiceLine"/>.</summary>
public class CensusServiceLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(40)]
    public string Reference { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(4000)]
    public string? BoundaryIn { get; set; }

    [MaxLength(4000)]
    public string? BoundaryOut { get; set; }

    [Required, MaxLength(40)]
    public string StatusCode { get; set; } = "DRAFT";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    public DateTime? RemovedAt { get; set; }
}

/// <summary>Local service record in the census. A service may use several register products.</summary>
public class CensusService
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(40)]
    public string Reference { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? PurposeNarrative { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(4000)]
    public string? BoundaryIn { get; set; }

    [MaxLength(4000)]
    public string? BoundaryOut { get; set; }

    [MaxLength(80)]
    public string? ServiceTypeCode { get; set; }

    [Required, MaxLength(40)]
    public string StatusCode { get; set; } = "DRAFT";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    public DateTime? RemovedAt { get; set; }
}

public class CensusServiceLineService
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceLineId { get; set; }

    public CensusServiceLine ServiceLine { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public CensusService Service { get; set; } = null!;

    [Required, MaxLength(40)]
    public string RelationshipCode { get; set; } = "PART_OF";

    public bool IsPrimary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RemovedAt { get; set; }
}

public class CensusServiceProduct
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceId { get; set; }

    public CensusService Service { get; set; } = null!;

    public Guid ProductId { get; set; }

    public CMDBProduct Product { get; set; } = null!;

    [Required, MaxLength(40)]
    public string RelationshipCode { get; set; } = "SUPPORTS";

    [MaxLength(4000)]
    public string? Narrative { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    public DateTime? RemovedAt { get; set; }

    [MaxLength(255)]
    public string? RemovedBy { get; set; }
}

/// <summary>Shared definition (user group, need, journey, feature) reused across register entries.</summary>
public class CensusCatalogueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(40)]
    public string Kind { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Reference { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Statement { get; set; }

    [MaxLength(80)]
    public string? LookupCode { get; set; }

    [Required, MaxLength(40)]
    public string StatusCode { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    public DateTime? RemovedAt { get; set; }
}

/// <summary>One repeatable census assertion on a service register product.</summary>
public class CensusEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public CMDBProduct Product { get; set; } = null!;

    [Required, MaxLength(40)]
    public string DomainKey { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Title { get; set; }

    [MaxLength(10000)]
    public string? Narrative { get; set; }

    [MaxLength(80)]
    public string? LookupCode { get; set; }

    [MaxLength(80)]
    public string? SecondaryLookupCode { get; set; }

    public Guid? CatalogueItemId { get; set; }

    public CensusCatalogueItem? CatalogueItem { get; set; }

    public int? StaffRoleId { get; set; }

    public StaffRole? StaffRole { get; set; }

    [MaxLength(200)]
    public string? PersonName { get; set; }

    [MaxLength(255)]
    public string? PersonEmail { get; set; }

    [MaxLength(500)]
    public string? ExternalUrl { get; set; }

    [Required, MaxLength(40)]
    public string VerificationStatusCode { get; set; } = "UNVERIFIED";

    public int SortOrder { get; set; }

    public int RowVersion { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }

    public DateTime? RemovedAt { get; set; }

    [MaxLength(255)]
    public string? RemovedBy { get; set; }

    [MaxLength(500)]
    public string? RemovalReason { get; set; }
}

/// <summary>Deliberate section declaration. An empty list is not treated as confirmed none.</summary>
public class CensusSectionDeclaration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProductId { get; set; }

    public CMDBProduct Product { get; set; } = null!;

    [Required, MaxLength(40)]
    public string SectionKey { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string StatusCode { get; set; } = "NOT_COLLECTED";

    [MaxLength(2000)]
    public string? Explanation { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }
}

/// <summary>Admin-managed schema area. Wording and order override the built-in defaults.</summary>
public class ServiceSchemaAreaConfig
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Group { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Summary { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}

/// <summary>Admin-managed schema question. The key stays stable so saved responses keep their place.</summary>
public class ServiceSchemaQuestion
{
    public int Id { get; set; }

    [Required, MaxLength(40)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string AreaKey { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Heading { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Help { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string TitleLabel { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string NarrativeLabel { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsBuiltIn { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(255)]
    public string? UpdatedBy { get; set; }
}
