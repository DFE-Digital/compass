using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class AuditLog
{
	[Key]
	public Guid AuditLogId { get; set; } = Guid.NewGuid();

	[StringLength(100)]
	public string Entity { get; set; } = string.Empty;

	[StringLength(100)]
	public string EntityId { get; set; } = string.Empty;

	[StringLength(50)]
	public string Action { get; set; } = string.Empty; // create/update/delete

	[StringLength(200)]
	public string? ChangedBy { get; set; }

	public DateTime ChangedUtc { get; set; } = DateTime.UtcNow;

	public string? PayloadJson { get; set; }
}


