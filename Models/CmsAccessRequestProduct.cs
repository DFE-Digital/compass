using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>CMS product name and sign-in URL for API access requests (managed in Admin).</summary>
public class CmsAccessRequestProduct
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>Must match <c>cmsName</c> sent by integrations (case-insensitive).</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Required]
    public string SignInPageUrl { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
