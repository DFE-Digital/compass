using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// DDT Standard entity for managing standards within COMPASS.
/// Implements standards as code with validation rules and phase allocation.
/// </summary>
public class DdtStandard
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Legacy ID from external CMS system (if migrated)
    /// </summary>
    [MaxLength(100)]
    public string? LegacyId { get; set; }

    /// <summary>
    /// UUID for standards as code (immutable identifier)
    /// </summary>
    [Required]
    [MaxLength(36)]
    public string StandardUuid { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Standard title (required, unique)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug (auto-generated from title)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Brief summary of the standard
    /// </summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }

    /// <summary>
    /// Purpose and rationale (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Criteria for meeting the standard (stored as JSON array)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Criteria { get; set; }

    /// <summary>
    /// How to meet the standard requirements (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? HowToMeet { get; set; }

    /// <summary>
    /// Governance information (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Governance { get; set; }

    /// <summary>
    /// Whether governance approval has been granted
    /// </summary>
    public bool GovernanceApproval { get; set; } = false;

    /// <summary>
    /// Current version (semantic versioning: MAJOR.MINOR.PATCH)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = "0.1.0";

    /// <summary>
    /// Previous version number
    /// </summary>
    [MaxLength(20)]
    public string? PreviousVersion { get; set; }

    /// <summary>
    /// Current lifecycle stage
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Stage { get; set; } = "Draft"; // Draft, Under Review, Approved, Published, Rejected, Archived

    /// <summary>
    /// When draft was first created
    /// </summary>
    [Required]
    public DateTime DraftCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// First publication date
    /// </summary>
    public DateTime? FirstPublished { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime? LastUpdated { get; set; }

    /// <summary>
    /// Whether this is a legal requirement
    /// </summary>
    public bool LegalStandard { get; set; } = false;

    /// <summary>
    /// Legal basis for the standard
    /// </summary>
    [MaxLength(1000)]
    public string? LegalBasis { get; set; }

    /// <summary>
    /// Validity period in months
    /// </summary>
    public int? ValidityPeriod { get; set; }

    /// <summary>
    /// Related guidance links/information (markdown supported)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? RelatedGuidance { get; set; }

    /// <summary>
    /// Flag indicating if standard has been modified since last publication
    /// </summary>
    public bool IsModified { get; set; } = false;

    /// <summary>
    /// Whether standard is published (has publishedAt set)
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Publication date
    /// </summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// User who created the standard
    /// </summary>
    public int? CreatorUserId { get; set; }

    [ForeignKey(nameof(CreatorUserId))]
    public User? CreatorUser { get; set; }

    /// <summary>
    /// Standard owners (many-to-many)
    /// </summary>
    public ICollection<DdtStandardOwner> Owners { get; set; } = new List<DdtStandardOwner>();

    /// <summary>
    /// Standard contacts (many-to-many)
    /// </summary>
    public ICollection<DdtStandardContact> Contacts { get; set; } = new List<DdtStandardContact>();

    /// <summary>
    /// Categories (many-to-many)
    /// </summary>
    public ICollection<DdtStandardCategory> Categories { get; set; } = new List<DdtStandardCategory>();

    /// <summary>
    /// Sub-categories (many-to-many)
    /// </summary>
    public ICollection<DdtStandardSubCategory> SubCategories { get; set; } = new List<DdtStandardSubCategory>();

    /// <summary>
    /// Phases this standard applies to (many-to-many)
    /// </summary>
    public ICollection<DdtStandardPhase> Phases { get; set; } = new List<DdtStandardPhase>();

    /// <summary>
    /// Validation rules for standards as code
    /// </summary>
    public ICollection<DdtStandardValidationRule> ValidationRules { get; set; } = new List<DdtStandardValidationRule>();

    /// <summary>
    /// Version history
    /// </summary>
    public ICollection<DdtStandardVersion> Versions { get; set; } = new List<DdtStandardVersion>();

    /// <summary>
    /// Comments on this standard
    /// </summary>
    public ICollection<DdtStandardComment> Comments { get; set; } = new List<DdtStandardComment>();

    /// <summary>
    /// Products (approved and tolerated) for this standard
    /// </summary>
    public ICollection<DdtStandardProduct> Products { get; set; } = new List<DdtStandardProduct>();

    /// <summary>
    /// Audit log entries
    /// </summary>
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;
}

