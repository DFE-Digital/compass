using Compass.Data;
using Compass.Models.Fips;
using Compass.Services.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public static class ServiceLineReport
{
    public static async Task<ServiceLineReportViewModel> LoadAsync(CompassDbContext db, bool includeSchema, CancellationToken ct)
    {
        var products = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active)
            .OrderBy(p => p.Title)
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                Areas = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList()
            })
            .ToListAsync(ct);

        var lines = await db.ServiceLines.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Slug,
                DirectorateCount = s.ServiceLineDivisions.Count(),
                WorkItemCount = s.ServiceLineProjects.Count()
            })
            .ToListAsync(ct);
        var linkedAreaRows = await db.ServiceLineBusinessAreas.AsNoTracking()
            .Select(b => new { b.ServiceLineId, b.BusinessAreaLookup.Name })
            .ToListAsync(ct);
        var linkedAreasByLine = linkedAreaRows
            .GroupBy(b => b.ServiceLineId)
            .ToDictionary(g => g.Key, g => g.Select(b => b.Name).ToList());

        var links = await db.ServiceLineProducts.AsNoTracking()
            .Select(x => new { x.ServiceLineId, x.CMDBProductId })
            .ToListAsync(ct);

        var schema = includeSchema
            ? (await ServiceSchemaReport.LoadAsync(db, ct)).Products.ToDictionary(p => p.Id, p => p.Percent)
            : new Dictionary<Guid, int>();

        var activeIds = products.Select(p => p.Id).ToHashSet();
        var lineById = lines.ToDictionary(s => s.Id);
        var linesByProduct = links
            .Where(x => activeIds.Contains(x.CMDBProductId) && lineById.ContainsKey(x.ServiceLineId))
            .GroupBy(x => x.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => lineById[x.ServiceLineId].Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList());

        var productRows = products.Select(p =>
        {
            var areas = p.Areas.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            linesByProduct.TryGetValue(p.Id, out var names);
            names ??= new List<string>();
            return new ServiceLineReportProductRow
            {
                Id = p.Id,
                UniqueId = p.UniqueID,
                Title = p.Title,
                BusinessAreas = areas.Count == 0 ? "No business area" : string.Join(", ", areas),
                BusinessAreaCount = areas.Count,
                LineCount = names.Count,
                Lines = names.Count == 0 ? "" : string.Join(", ", names),
                SchemaPercent = includeSchema && schema.TryGetValue(p.Id, out var percent) ? percent : null
            };
        }).ToList();

        var areaNamesByProduct = products.ToDictionary(
            p => p.Id,
            p => p.Areas.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        var productsByLine = links
            .Where(x => activeIds.Contains(x.CMDBProductId))
            .GroupBy(x => x.ServiceLineId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CMDBProductId).Distinct().ToList());

        var lineRows = lines.Select(line =>
        {
            productsByLine.TryGetValue(line.Id, out var ids);
            ids ??= new List<Guid>();
            var productAreas = ids
                .SelectMany(id => areaNamesByProduct.TryGetValue(id, out var names) ? names : new List<string>())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
            linkedAreasByLine.TryGetValue(line.Id, out var linkedNames);
            var linked = (linkedNames ?? new List<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            var shared = ids.Count(id => linesByProduct.TryGetValue(id, out var membership) && membership.Count > 1);
            var multiAreaProducts = ids.Count(id => areaNamesByProduct.TryGetValue(id, out var names) && names.Count > 1);
            int? schemaPercent = null;
            if (includeSchema && ids.Count > 0)
            {
                var values = ids.Where(schema.ContainsKey).Select(id => schema[id]).ToList();
                if (values.Count > 0)
                    schemaPercent = (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
            }

            return new ServiceLineReportLineRow
            {
                Name = line.Name,
                Slug = line.Slug,
                ProductCount = ids.Count,
                BusinessAreaCount = productAreas.Count,
                ProductAreas = productAreas.Count == 0 ? "—" : string.Join(", ", productAreas),
                ProductsInMultipleAreas = multiAreaProducts,
                SharedProductCount = shared,
                DirectorateCount = line.DirectorateCount,
                WorkItemCount = line.WorkItemCount,
                LinkedAreas = linked.Count == 0 ? "—" : string.Join(", ", linked),
                AreaMismatch = linked.Count > 0 && !SameSet(linked, productAreas),
                ProductsSpanMultipleAreas = productAreas.Count > 1,
                SchemaPercent = schemaPercent
            };
        })
        .OrderByDescending(r => r.ProductsSpanMultipleAreas)
        .ThenByDescending(r => r.BusinessAreaCount)
        .ThenByDescending(r => r.SharedProductCount)
        .ThenByDescending(r => r.ProductCount)
        .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

        int? Average(IEnumerable<ServiceLineReportProductRow> rows)
        {
            var values = rows.Where(r => r.SchemaPercent != null).Select(r => r.SchemaPercent!.Value).ToList();
            return values.Count == 0 ? null : (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
        }

        var inLine = productRows.Where(p => p.LineCount > 0).OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList();
        var unassigned = productRows.Where(p => p.LineCount == 0).ToList();

        return new ServiceLineReportViewModel
        {
            ShowSchema = includeSchema,
            ServiceLineCount = lines.Count,
            ActiveProducts = productRows.Count,
            InALine = inLine.Count,
            NotInALine = unassigned.Count,
            InMultipleLines = productRows.Count(p => p.LineCount > 1),
            LinesSpanningBusinessAreas = lineRows.Count(r => r.ProductsSpanMultipleAreas),
            EmptyLines = lineRows.Count(r => r.ProductCount == 0),
            SchemaInALine = includeSchema ? Average(inLine) : null,
            SchemaNotInALine = includeSchema ? Average(unassigned) : null,
            Lines = lineRows,
            InLineProducts = inLine,
            UnassignedProducts = unassigned,
            SharedProducts = productRows.Where(p => p.LineCount > 1).OrderByDescending(p => p.LineCount).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            MultiAreaProducts = inLine.Where(p => p.BusinessAreaCount > 1).OrderByDescending(p => p.BusinessAreaCount).ThenBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList()
        };
    }

    private static bool SameSet(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        if (left.Count != right.Count)
            return false;
        var other = right.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return left.All(other.Contains);
    }
}
