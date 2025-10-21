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
    
    // Navigation properties
    public virtual ICollection<ApiTokenPermission> Permissions { get; set; } = new List<ApiTokenPermission>();
    
    public virtual ICollection<ApiRequestLog> RequestLogs { get; set; } = new List<ApiRequestLog>();
}

