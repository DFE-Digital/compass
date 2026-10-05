using Compass.Data;
using Compass.Models.ServiceSchema;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaSeed
{
    public static async Task EnsureAsync(CompassDbContext db, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var sets = await db.ServiceSchemaLookupSets.Include(s => s.Values).ToListAsync(cancellationToken);
        var setByKey = sets.ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        var sort = 0;
        foreach (var seed in ServiceSchemaCatalog.Lookups)
        {
            sort++;
            if (!setByKey.TryGetValue(seed.Key, out var set))
            {
                set = new ServiceSchemaLookupSet
                {
                    Key = seed.Key,
                    Name = seed.Name,
                    Description = seed.Description,
                    IsSystem = true,
                    SortOrder = sort,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.ServiceSchemaLookupSets.Add(set);
                setByKey[seed.Key] = set;
            }

            var valueSort = 0;
            foreach (var (code, label) in seed.Values)
            {
                valueSort++;
                if (set.Values.Any(v => string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase)))
                    continue;
                set.Values.Add(new ServiceSchemaLookupValue
                {
                    Code = code,
                    Label = label,
                    SortOrder = valueSort,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        var existingCodes = await db.StaffRoles.Select(r => r.Code).ToListAsync(cancellationToken);
        var known = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roleSort = 0;
        foreach (var (family, name) in DdatStaffRoleCatalog.Roles)
        {
            roleSort++;
            var code = DdatStaffRoleCatalog.CodeFor(family, name);
            if (!known.Add(code))
                continue;
            db.StaffRoles.Add(new StaffRole
            {
                Family = family,
                Name = name,
                Code = code,
                SourceUrl = ServiceSchemaCatalog.FrameworkHomeUrl,
                SortOrder = roleSort,
                IsActive = true,
                IsFrameworkRole = true,
                Description = "Role from the Government Digital and Data Profession Capability Framework.",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
