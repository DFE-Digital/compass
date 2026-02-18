using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ProductDqReview
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string ProductDocumentId { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? ProductFipsId { get; set; }
    
    [Required]
    public DateTime LastReviewedDate { get; set; }
    
    [Required]
    public DateTime NextDueDate { get; set; }
    
    [Required]
    [StringLength(320)]
    public string ReviewedByEmail { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? ReviewedByName { get; set; }
    
    [StringLength(4000)]
    public string? ChangesMade { get; set; }
    
    [StringLength(8000)]
    public string? ContactChangesJson { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property (optional, if we want to link to product)
    // Note: Product is in CMS, not in this database, so we use DocumentId as foreign key
}
