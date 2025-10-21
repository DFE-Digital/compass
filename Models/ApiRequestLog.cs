using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ApiRequestLog
{
    public int Id { get; set; }
    
    public int ApiTokenId { get; set; }
    
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(10)]
    public string HttpMethod { get; set; } = string.Empty; // GET, POST, PUT, DELETE
    
    [Required]
    [MaxLength(500)]
    public string RequestPath { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string? QueryString { get; set; }
    
    [Column(TypeName = "text")]
    public string? RequestBody { get; set; }
    
    public int ResponseStatusCode { get; set; }
    
    [Column(TypeName = "text")]
    public string? ResponseBody { get; set; }
    
    public int ResponseTimeMs { get; set; }
    
    [MaxLength(100)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    public bool IsSuccess { get; set; }
    
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    public virtual ApiToken ApiToken { get; set; } = null!;
}

