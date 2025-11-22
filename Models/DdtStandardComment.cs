using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>
/// Comments on DDT Standards (for review/feedback)
/// </summary>
public class DdtStandardComment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int DdtStandardId { get; set; }

    [ForeignKey(nameof(DdtStandardId))]
    public DdtStandard DdtStandard { get; set; } = null!;

    /// <summary>
    /// Comment title
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Comment content
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? Comments { get; set; }

    /// <summary>
    /// User who created the comment
    /// </summary>
    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    /// <summary>
    /// Comment type (e.g., "review", "feedback", "rejection")
    /// </summary>
    [MaxLength(50)]
    public string? CommentType { get; set; }

    /// <summary>
    /// Parent comment ID for threaded comments
    /// </summary>
    public int? ParentCommentId { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public DdtStandardComment? ParentComment { get; set; }

    /// <summary>
    /// Child comments (replies)
    /// </summary>
    public ICollection<DdtStandardComment> Replies { get; set; } = new List<DdtStandardComment>();

    /// <summary>
    /// Whether this comment has been resolved
    /// </summary>
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// User who resolved this comment
    /// </summary>
    public int? ResolvedByUserId { get; set; }

    [ForeignKey(nameof(ResolvedByUserId))]
    public User? ResolvedByUser { get; set; }

    /// <summary>
    /// When the comment was resolved
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

