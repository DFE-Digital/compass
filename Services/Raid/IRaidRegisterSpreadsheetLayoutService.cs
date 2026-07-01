namespace Compass.Services.Raid;

public interface IRaidRegisterSpreadsheetLayoutService
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetColumnOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetColumnOrderAsync(string entityType, CancellationToken cancellationToken = default);

    Task SaveColumnOrderAsync(string entityType, IReadOnlyList<string> columnOrder, int? userId, CancellationToken cancellationToken = default);

    Task DeleteSavedLayoutAsync(string entityType, CancellationToken cancellationToken = default);
}
