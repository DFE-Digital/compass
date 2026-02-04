using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Represents a business case in the planning and demand management system
/// </summary>
public class BusinessCase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique business case identifier
    /// </summary>
    [Required]
    [StringLength(50)]
    public string BusinessCaseId { get; set; } = string.Empty;

    /// <summary>
    /// Business case title
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Business case description
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    /// <summary>
    /// Requestor (Entra user ID stored as email)
    /// </summary>
    [StringLength(255)]
    public string? RequestorEmail { get; set; }

    /// <summary>
    /// Requestor display name
    /// </summary>
    [StringLength(255)]
    public string? RequestorName { get; set; }

    /// <summary>
    /// Business case date
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Business area (from lookups)
    /// </summary>
    [StringLength(100)]
    public string? BusinessArea { get; set; }

    /// <summary>
    /// Business case status (from BusinessCaseStatusLookup)
    /// </summary>
    public int? StatusLookupId { get; set; }
    [ForeignKey(nameof(StatusLookupId))]
    public BusinessCaseStatusLookup? StatusLookup { get; set; }

    /// <summary>
    /// Status name (for backward compatibility and convenience)
    /// </summary>
    [StringLength(100)]
    public string? Status { get; set; }

    // Timestamps
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<BusinessCaseDdtFeedback> DdtFeedbacks { get; set; } = new List<BusinessCaseDdtFeedback>();
    public ICollection<BusinessCaseReviewer> Reviewers { get; set; } = new List<BusinessCaseReviewer>();
    public ICollection<BusinessCaseProject> BusinessCaseProjects { get; set; } = new List<BusinessCaseProject>();
    public ICollection<BusinessCaseProduct> BusinessCaseProducts { get; set; } = new List<BusinessCaseProduct>();
}

/// <summary>
/// DDT feedback for a business case
/// </summary>
public class BusinessCaseDdtFeedback
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BusinessCaseId { get; set; }

    /// <summary>
    /// DDT feedback text (max 4000 characters)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    [MaxLength(4000)]
    public string? Feedback { get; set; }

    /// <summary>
    /// DDT feedback provider (Entra user ID stored as email)
    /// </summary>
    [StringLength(255)]
    public string? FeedbackProviderEmail { get; set; }

    /// <summary>
    /// DDT feedback provider display name
    /// </summary>
    [StringLength(255)]
    public string? FeedbackProviderName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("BusinessCaseId")]
    public BusinessCase? BusinessCase { get; set; }
}

/// <summary>
/// Reviewer for a business case
/// </summary>
public class BusinessCaseReviewer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BusinessCaseId { get; set; }

    /// <summary>
    /// Reviewer (Entra user ID stored as email)
    /// </summary>
    [StringLength(255)]
    public string? ReviewerEmail { get; set; }

    /// <summary>
    /// Reviewer display name
    /// </summary>
    [StringLength(255)]
    public string? ReviewerName { get; set; }

    /// <summary>
    /// Reviewer role
    /// </summary>
    [StringLength(100)]
    public string? ReviewerRole { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("BusinessCaseId")]
    public BusinessCase? BusinessCase { get; set; }
}

/// <summary>
/// Junction table linking business cases to projects
/// </summary>
public class BusinessCaseProject
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BusinessCaseId { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("BusinessCaseId")]
    public BusinessCase? BusinessCase { get; set; }

    [ForeignKey("ProjectId")]
    public Project? Project { get; set; }
}

/// <summary>
/// Junction table linking business cases to products (stored by FIPS ID from CMS)
/// </summary>
public class BusinessCaseProduct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BusinessCaseId { get; set; }

    /// <summary>
    /// Product FIPS ID from CMS (e.g., ABC-123)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ProductFipsId { get; set; } = string.Empty;

    /// <summary>
    /// Product title (cached for display)
    /// </summary>
    [StringLength(255)]
    public string? ProductTitle { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey("BusinessCaseId")]
    public BusinessCase? BusinessCase { get; set; }
}

/// <summary>
/// Lookup table for business case statuses
/// </summary>
public class BusinessCaseStatusLookup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? CssClass { get; set; } // CSS class for styling the badge

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
