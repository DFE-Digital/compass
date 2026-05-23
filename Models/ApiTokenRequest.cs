using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ApiTokenRequest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(256)]
    public string RequestorEmail { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Environment { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ProjectSlug { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Justification { get; set; }

    public ApiTokenRequestStatus Status { get; set; } = ApiTokenRequestStatus.Pending;

  /// <summary>JSON map of resource → { read, create, update, delete }.</summary>
    [Required]
    public string PermissionsJson { get; set; } = "{}";

    public bool IsReadOnlyAllData { get; set; }

    [MaxLength(256)]
    public string? ReviewedByEmail { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    public int? IssuedApiTokenId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ApiToken? IssuedApiToken { get; set; }
}
