using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.EnvironmentSync;

public sealed partial class EnvironmentSyncService
{
  private async Task<Dictionary<int, int>> SyncNamedLookupsAsync<TEntity>(
    IQueryable<TEntity> sourceQuery,
    CompassDbContext targetDb,
    DbSet<TEntity> targetSet,
    Func<TEntity, string> getName,
    Action<TEntity, TEntity> updateExisting,
    Func<TEntity, TEntity> createNew,
    bool dryRun,
    CancellationToken cancellationToken) where TEntity : class
  {
    var map = new Dictionary<int, int>();
    var sourceRows = await sourceQuery.ToListAsync(cancellationToken);
    var targetRows = await targetSet.ToListAsync(cancellationToken);
    var targetByName = targetRows.ToDictionary(x => getName(x).Trim(), StringComparer.OrdinalIgnoreCase);

    foreach (var src in sourceRows)
    {
      var srcId = GetEntityId(src);
      var name = getName(src).Trim();
      if (targetByName.TryGetValue(name, out var existing))
      {
        map[srcId] = GetEntityId(existing);
        if (!dryRun)
          updateExisting(src, existing);
      }
      else if (!dryRun)
      {
        var created = createNew(src);
        targetSet.Add(created);
        await targetDb.SaveChangesAsync(cancellationToken);
        map[srcId] = GetEntityId(created);
        targetByName[name] = created;
      }
    }

    if (!dryRun)
      await targetDb.SaveChangesAsync(cancellationToken);

    return map;
  }

  private static int GetEntityId<TEntity>(TEntity entity)
  {
    var prop = typeof(TEntity).GetProperty("Id")
      ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no Id property.");
    return (int)(prop.GetValue(entity) ?? throw new InvalidOperationException("Id was null."));
  }
}
