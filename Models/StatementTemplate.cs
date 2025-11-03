using System.ComponentModel.DataAnnotations;

namespace Compass.Models
{
    /// <summary>
    /// Represents an accessibility statement template with versioning support.
    /// Templates are used to generate accessibility statements for products.
    /// </summary>
    public class StatementTemplate
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Template name/identifier (e.g., "Compliant", "Non-compliant")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Template version number (incremented when updated)
        /// </summary>
        [Required]
        public int Version { get; set; } = 1;
        
        /// <summary>
        /// Template content in Markdown format with parameter placeholders (e.g., {{ name_of_service }})
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Description of what this template is used for
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Whether this version is the current active version
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Timestamp when this template version was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// User who created this template version
        /// </summary>
        [MaxLength(450)]
        public string? CreatedBy { get; set; }
        
        /// <summary>
        /// Timestamp when this template version was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// User who last updated this template version
        /// </summary>
        [MaxLength(450)]
        public string? UpdatedBy { get; set; }
        
        /// <summary>
        /// Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;
    }
}

