using System.Threading.Tasks;

namespace Compass.Services;

public interface IAuditLogger
{
	Task LogAsync(string entity, string entityId, string action, string? changedBy, string? payloadJson);
}


