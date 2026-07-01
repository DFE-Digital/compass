using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ApiToken
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastUsedAt { get; set; }
    
    public DateTime? ExpiresAt { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string CreatedByEmail { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? OwnerEmail { get; set; }

    [MaxLength(10)]
    public string? Environment { get; set; }

    [MaxLength(50)]
    public string? ProjectSlug { get; set; }

  /// <summary>RO, CREATE, UPDATE, CRU or FULL — appended to generated token names.</summary>
    [MaxLength(20)]
    public string? AccessTier { get; set; }

    public bool IsSelfService { get; set; }

    // Navigation properties
    public virtual ICollection<ApiTokenPermission> Permissions { get; set; } = new List<ApiTokenPermission>();

    public virtual ICollection<ApiRequestLog> RequestLogs { get; set; } = new List<ApiRequestLog>();

    public virtual ICollection<ApiTokenMember> Members { get; set; } = new List<ApiTokenMember>();
}

