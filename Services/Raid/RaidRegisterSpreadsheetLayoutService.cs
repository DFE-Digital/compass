using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

public class RaidRegisterSpreadsheetLayoutService(CompassDbContext db) : IRaidRegisterSpreadsheetLayoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetColumnOrdersAsync(CancellationToken cancellationToken = default)
    {
        var saved = await db.RaidRegisterSpreadsheetLayouts.AsNoTracking()
            .ToDictionaryAsync(x => x.EntityType, x => x, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entityType in RaidRegisterSpreadsheetColumnCatalog.EntityTypes)
        {
            if (saved.TryGetValue(entityType, out var row))
                result[entityType] = ParseAndNormalize(entityType, row.ColumnOrderJson);
            else
                result[entityType] = RaidRegisterSpreadsheetColumnCatalog.GetBuiltInColumnOrder(entityType);
        }

        return result;
    }

    public async Task<IReadOnlyList<string>> GetColumnOrderAsync(string entityType, CancellationToken cancellationToken = default)
    {
        var normalized = entityType.Trim().ToLowerInvariant();
        if (!RaidRegisterSpreadsheetColumnCatalog.IsKnownEntityType(normalized))
            return Array.Empty<string>();

        var row = await db.RaidRegisterSpreadsheetLayouts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EntityType == normalized, cancellationToken);
        if (row == null)
            return RaidRegisterSpreadsheetColumnCatalog.GetBuiltInColumnOrder(normalized);

        return ParseAndNormalize(normalized, row.ColumnOrderJson);
    }

    public async Task SaveColumnOrderAsync(string entityType, IReadOnlyList<string> columnOrder, int? userId, CancellationToken cancellationToken = default)
    {
        var normalized = entityType.Trim().ToLowerInvariant();
        if (!RaidRegisterSpreadsheetColumnCatalog.IsKnownEntityType(normalized))
            throw new ArgumentException("Unknown entity type.", nameof(entityType));

        var order = RaidRegisterSpreadsheetColumnCatalog.NormalizeColumnOrder(normalized, columnOrder);
        var json = JsonSerializer.Serialize(order, JsonOptions);

        var row = await db.RaidRegisterSpreadsheetLayouts
            .FirstOrDefaultAsync(x => x.EntityType == normalized, cancellationToken);
        if (row == null)
        {
            row = new RaidRegisterSpreadsheetLayout { EntityType = normalized };
            db.RaidRegisterSpreadsheetLayouts.Add(row);
        }

        row.ColumnOrderJson = json;
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedByUserId = userId;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSavedLayoutAsync(string entityType, CancellationToken cancellationToken = default)
    {
        var normalized = entityType.Trim().ToLowerInvariant();
        var row = await db.RaidRegisterSpreadsheetLayouts
            .FirstOrDefaultAsync(x => x.EntityType == normalized, cancellationToken);
        if (row == null) return;

        db.RaidRegisterSpreadsheetLayouts.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> ParseAndNormalize(string entityType, string json)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return RaidRegisterSpreadsheetColumnCatalog.NormalizeColumnOrder(entityType, parsed);
        }
        catch (JsonException)
        {
            return RaidRegisterSpreadsheetColumnCatalog.GetBuiltInColumnOrder(entityType);
        }
    }
}
