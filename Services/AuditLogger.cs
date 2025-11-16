using System.Text.Json;
using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class AuditLogger : IAuditLogger
{
	private readonly CompassDbContext _db;
	private readonly ILogger<AuditLogger> _logger;

	public AuditLogger(CompassDbContext db, ILogger<AuditLogger> logger)
	{
		_db = db;
		_logger = logger;
	}

	public async Task LogAsync(string entity, string entityId, string action, string? changedBy, string? payloadJson)
	{
		try
		{
			_db.AuditLogs.Add(new Models.AuditLog
			{
				Entity = entity,
				EntityId = entityId,
				Action = action,
				ChangedBy = changedBy,
				ChangedUtc = DateTime.UtcNow,
				AfterJson = payloadJson
			});
			await _db.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to write audit log for {Entity} {Action}", entity, action);
		}
	}
}


