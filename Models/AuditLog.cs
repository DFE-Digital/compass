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

	[StringLength(200)]
	public string? EntityReference { get; set; }

	[StringLength(50)]
	public string Action { get; set; } = string.Empty; // create/update/delete

	[StringLength(200)]
	public string? ChangedBy { get; set; }

	[StringLength(100)]
	public string? ChangedByUserId { get; set; }

	[StringLength(320)]
	public string? ChangedByEmail { get; set; }

	[StringLength(64)]
	public string? IpAddress { get; set; }

	[StringLength(400)]
	public string? UserAgent { get; set; }

	public DateTime ChangedUtc { get; set; } = DateTime.UtcNow;

	public string? BeforeJson { get; set; }

	public string? AfterJson { get; set; }
}


