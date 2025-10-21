using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ApiTokenPermission
{
    public int Id { get; set; }
    
    public int ApiTokenId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Resource { get; set; } = string.Empty; // e.g., "Risks", "Issues", "Actions"
    
    public bool CanRead { get; set; } = false;
    
    public bool CanCreate { get; set; } = false;
    
    public bool CanUpdate { get; set; } = false;
    
    public bool CanDelete { get; set; } = false;
    
    // Navigation properties
    public virtual ApiToken ApiToken { get; set; } = null!;
}

