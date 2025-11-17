using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectNeedInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? NeedId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Need { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Source { get; set; }

    [Required]
    [MaxLength(20)]
    public string Validated { get; set; } = "No";

    [MaxLength(2000)]
    public string? ValidationNotes { get; set; }

    public int SortOrder { get; set; }
}

