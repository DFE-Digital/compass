using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

public class ChatConversation
{
    [Key]
    public int Id { get; set; }

    [StringLength(255)]
    public string? UserEmail { get; set; }

    [StringLength(255)]
    public string? UserName { get; set; }

    [StringLength(36)]
    public string? UserAzureObjectId { get; set; }

    public int? UserId { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string Messages { get; set; } = string.Empty; // JSON array of messages

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

