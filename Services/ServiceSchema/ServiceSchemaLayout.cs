using Compass.Data;
using Compass.Models.ServiceSchema;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public sealed class ServiceSchemaLayout
{
    private ServiceSchemaLayout(IReadOnlyList<ServiceSchemaArea> areas)
    {
        Areas = areas;
        Topics = areas.SelectMany(a => a.Topics).ToList();
    }

    public IReadOnlyList<ServiceSchemaArea> Areas { get; }

    public IReadOnlyList<ServiceSchemaTopic> Topics { get; }

    public ServiceSchemaArea Find(string? key) =>
        Areas.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase))
        ?? Areas.FirstOrDefault()
        ?? ServiceSchemaAreas.All[0];

    public ServiceSchemaTopic? FindTopic(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        return Topics.FirstOrDefault(topic => ServiceSchemaAreas.DomainMatches(topic, key));
    }

    public ServiceSchemaArea? AreaFor(string topicKey) =>
        Areas.FirstOrDefault(a => a.Topics.Any(t => string.Equals(t.Key, topicKey, StringComparison.OrdinalIgnoreCase)));

    public static async Task<ServiceSchemaLayout> LoadAsync(CompassDbContext db, CancellationToken ct)
    {
        await EnsureDefaultsAsync(db, ct);
        var areaRows = await db.ServiceSchemaAreaConfigs.AsNoTracking()
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync(ct);
        var questionRows = await db.ServiceSchemaQuestions.AsNoTracking()
            .Where(q => q.IsActive)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Heading)
            .ToListAsync(ct);

        var areas = areaRows.Select(row => new ServiceSchemaArea
        {
            Key = row.Key,
            Name = row.Name,
            Group = row.Group,
            Summary = row.Summary,
            Topics = questionRows
                .Where(q => string.Equals(q.AreaKey, row.Key, StringComparison.OrdinalIgnoreCase))
                .Select(ToTopic)
                .ToList()
        }).ToList();

        return areas.Count == 0
            ? new ServiceSchemaLayout(ServiceSchemaAreas.All)
            : new ServiceSchemaLayout(areas);
    }

    public static async Task EnsureDefaultsAsync(CompassDbContext db, CancellationToken ct)
    {
        var areaKeys = await db.ServiceSchemaAreaConfigs.Select(a => a.Key).ToListAsync(ct);
        var questionKeys = await db.ServiceSchemaQuestions.Select(q => q.Key).ToListAsync(ct);
        var knownAreas = areaKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var knownQuestions = questionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = false;
        var areaOrder = areaKeys.Count;
        foreach (var area in ServiceSchemaAreas.All)
        {
            if (knownAreas.Add(area.Key))
            {
                db.ServiceSchemaAreaConfigs.Add(new ServiceSchemaAreaConfig
                {
                    Key = area.Key,
                    Name = area.Name,
                    Group = area.Group,
                    Summary = area.Summary,
                    SortOrder = areaOrder++
                });
                added = true;
            }

            var questionOrder = 0;
            foreach (var topic in area.Topics)
            {
                if (!knownQuestions.Add(topic.Key))
                {
                    questionOrder++;
                    continue;
                }

                db.ServiceSchemaQuestions.Add(new ServiceSchemaQuestion
                {
                    Key = topic.Key,
                    AreaKey = area.Key,
                    Heading = topic.Heading,
                    Help = topic.Help,
                    TitleLabel = topic.TitleLabel,
                    NarrativeLabel = topic.NarrativeLabel,
                    SortOrder = questionOrder,
                    IsActive = true,
                    IsBuiltIn = true,
                    UpdatedAt = DateTime.UtcNow
                });
                added = true;
                questionOrder++;
            }
        }

        if (!added)
            return;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
        }
    }

    private static ServiceSchemaTopic ToTopic(ServiceSchemaQuestion question)
    {
        var code = ServiceSchemaAreas.FindTopic(question.Key);
        return new ServiceSchemaTopic
        {
            Key = question.Key,
            Heading = question.Heading,
            Help = question.Help,
            TitleLabel = question.TitleLabel,
            NarrativeLabel = question.NarrativeLabel,
            Mode = code?.Mode ?? "text",
            CatalogueKind = code?.CatalogueKind,
            LookupSource = code?.LookupSource,
            LookupLabel = code?.LookupLabel,
            CapturePerson = code?.CapturePerson ?? false,
            CaptureUrl = code?.CaptureUrl ?? false
        };
    }
}
