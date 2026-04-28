using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class CustomReport
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    public int OwnerUserId { get; set; }

    [ForeignKey(nameof(OwnerUserId))]
    public User? Owner { get; set; }

    [Required]
    public CustomReportDataSource DataSource { get; set; }

    /// <summary>Optional multi–data set definition; when set, <see cref="DataSource"/> is a legacy mirror (first block) for older code.</summary>
    [MaxLength(32000)]
    public string? DefinitionJson { get; set; }

    [Required]
    public CustomReportVisibility Visibility { get; set; } = CustomReportVisibility.Private;

    /// <summary>JSON of default run filters (date range, business area, directorate).</summary>
    [MaxLength(8000)]
    public string? DefaultFilterJson { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CustomReportShare> Shares { get; set; } = new List<CustomReportShare>();
}
