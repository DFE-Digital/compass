using System.Globalization;
using Compass.Data;
using Compass.Models;
using Compass.Models.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaAudit
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static async Task<List<ServiceSchemaAuditRow>> LoadAsync(
        CompassDbContext db,
        Guid productId,
        CancellationToken ct)
    {
        var layout = await ServiceSchemaLayout.LoadAsync(db, ct);
        var entries = await db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId)
            .Include(e => e.CatalogueItem)
            .Include(e => e.StaffRole)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        var declarations = await db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => d.ProductId == productId)
            .ToListAsync(ct);

        var lookupKeys = layout.Topics
            .Select(t => t.LookupSource)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)!
            .ToList();
        var lookups = await ServiceSchemaAdminLookups.LoadAsync(db, lookupKeys!, ct);

        var actorEmails = entries
            .SelectMany(e => new[] { e.CreatedBy, e.UpdatedBy, e.RemovedBy })
            .Concat(declarations.Select(d => d.UpdatedBy))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();
        Dictionary<string, string> nameMap;
        if (actorEmails.Count == 0)
        {
            nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var namesByEmail = await db.Users.AsNoTracking()
                .Where(u => actorEmails.Contains(u.Email.ToLower()))
                .Select(u => new { u.Email, u.Name })
                .ToListAsync(ct);
            nameMap = namesByEmail
                .GroupBy(u => u.Email, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);
        }

        var rows = new List<ServiceSchemaAuditRow>();
        foreach (var entry in entries)
        {
            var topic = layout.FindTopic(entry.DomainKey);
            var area = topic == null ? null : layout.AreaFor(topic.Key);
            var value = FormatEntryValue(topic, entry, lookups);
            var detail = FormatEntryDetail(entry);

            rows.Add(MakeRow(
                id: "add-" + entry.Id.ToString("N"),
                areaName: area?.Name ?? "Service census",
                questionName: topic == null ? entry.DomainKey : ServiceSchemaAreas.ShortTopicName(topic),
                action: "Add",
                actor: entry.CreatedBy,
                atUtc: entry.CreatedAt,
                oldValue: null,
                newValue: value,
                detail: detail,
                nameMap: nameMap));

            if (entry.RemovedAt is DateTime removedAt)
            {
                rows.Add(MakeRow(
                    id: "remove-" + entry.Id.ToString("N"),
                    areaName: area?.Name ?? "Service census",
                    questionName: topic == null ? entry.DomainKey : ServiceSchemaAreas.ShortTopicName(topic),
                    action: "Remove",
                    actor: entry.RemovedBy ?? entry.UpdatedBy,
                    atUtc: removedAt,
                    oldValue: value,
                    newValue: null,
                    detail: string.IsNullOrWhiteSpace(entry.RemovalReason) ? detail : entry.RemovalReason,
                    nameMap: nameMap));
            }
            else if (entry.UpdatedAt > entry.CreatedAt.AddMinutes(1))
            {
                // In-place edits (for example yes/no answers) do not keep the previous value.
                rows.Add(MakeRow(
                    id: "change-" + entry.Id.ToString("N") + "-" + entry.UpdatedAt.Ticks,
                    areaName: area?.Name ?? "Service census",
                    questionName: topic == null ? entry.DomainKey : ServiceSchemaAreas.ShortTopicName(topic),
                    action: "Change",
                    actor: entry.UpdatedBy ?? entry.CreatedBy,
                    atUtc: entry.UpdatedAt,
                    oldValue: "Previous value not retained",
                    newValue: value,
                    detail: detail,
                    nameMap: nameMap));
            }
        }

        foreach (var declaration in declarations)
        {
            var topic = layout.FindTopic(declaration.SectionKey);
            var area = topic != null
                ? layout.AreaFor(topic.Key)
                : layout.Areas.FirstOrDefault(a => string.Equals(a.Key, declaration.SectionKey, StringComparison.OrdinalIgnoreCase));
            string areaName;
            string questionName;
            if (ServiceSchemaAreas.IsProductDetails(declaration.SectionKey))
            {
                areaName = "Product details";
                questionName = "Product details";
            }
            else if (topic != null)
            {
                areaName = area?.Name ?? "Service census";
                questionName = ServiceSchemaAreas.ShortTopicName(topic);
            }
            else
            {
                areaName = area?.Name ?? "Service census";
                questionName = declaration.SectionKey;
            }

            var action = ServiceSchemaWorkspace.IsSectionCompleteStatus(declaration.StatusCode)
                ? "Marked complete"
                : ServiceSchemaWorkspace.IsClosingStatus(declaration.StatusCode)
                    ? "Nothing to record"
                    : "Status update";
            var newValue = ServiceSchemaWorkspace.IsSectionCompleteStatus(declaration.StatusCode)
                ? "Complete"
                : ServiceSchemaWorkspace.DeclarationLabel(declaration.StatusCode);

            rows.Add(MakeRow(
                id: "decl-" + declaration.Id.ToString("N"),
                areaName: areaName,
                questionName: questionName,
                action: action,
                actor: declaration.UpdatedBy,
                atUtc: declaration.UpdatedAt,
                oldValue: null,
                newValue: newValue,
                detail: declaration.Explanation,
                nameMap: nameMap));
        }

        return rows
            .OrderByDescending(r => r.AtUtc)
            .ThenBy(r => r.AreaName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ServiceSchemaAuditRow MakeRow(
        string id,
        string areaName,
        string questionName,
        string action,
        string? actor,
        DateTime atUtc,
        string? oldValue,
        string? newValue,
        string? detail,
        IReadOnlyDictionary<string, string> nameMap)
    {
        var email = string.IsNullOrWhiteSpace(actor) ? "" : actor.Trim();
        var name = !string.IsNullOrWhiteSpace(email) && nameMap.TryGetValue(email, out var mapped) && !string.IsNullOrWhiteSpace(mapped)
            ? mapped.Trim()
            : DisplayFromEmail(email);
        var local = atUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(atUtc, DateTimeKind.Utc).ToLocalTime()
            : atUtc.ToLocalTime();

        return new ServiceSchemaAuditRow
        {
            Id = id,
            AreaName = areaName,
            QuestionName = questionName,
            Action = action,
            ActorName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
            ActorEmail = email,
            AtUtc = atUtc,
            AtDisplay = local.ToString("d MMM yyyy HH:mm", Uk),
            OldValue = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim(),
            NewValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim(),
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim()
        };
    }

    private static string DisplayFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "";
        var at = email.IndexOf('@');
        if (at <= 0)
            return email;
        var local = email[..at].Replace('.', ' ').Replace('_', ' ').Trim();
        return string.IsNullOrWhiteSpace(local) ? email : local;
    }

    private static string FormatEntryValue(
        ServiceSchemaTopic? topic,
        CensusEntry entry,
        Dictionary<string, List<ServiceSchemaChoice>> lookups)
    {
        if (topic == null)
            return FirstNonEmpty(entry.Title, entry.LookupCode, entry.Narrative) ?? "Recorded response";

        switch (topic.Mode)
        {
            case ServiceSchemaResponseModes.YesChoice:
            {
                var answer = string.Equals(entry.LookupCode, "yes", StringComparison.OrdinalIgnoreCase) ? "Yes"
                    : string.Equals(entry.LookupCode, "no", StringComparison.OrdinalIgnoreCase) ? "No"
                    : entry.LookupCode ?? "Answer";
                var category = topic.ChoiceOptions.FirstOrDefault(c =>
                    string.Equals(c.Value, entry.SecondaryLookupCode, StringComparison.OrdinalIgnoreCase))?.Label;
                return string.IsNullOrWhiteSpace(category) ? answer : answer + " — " + category;
            }
            case ServiceSchemaResponseModes.Catalogue:
            case ServiceSchemaResponseModes.Products:
                return FirstNonEmpty(entry.CatalogueItem?.Name, entry.Title, entry.LookupCode) ?? "Recorded item";
            case ServiceSchemaResponseModes.Lookup:
            case ServiceSchemaResponseModes.Choice:
            {
                var label = ServiceSchemaAdminLookups.LabelFor(lookups, entry.LookupCode);
                return FirstNonEmpty(label, entry.Title, entry.LookupCode) ?? "Recorded response";
            }
            default:
                return FirstNonEmpty(entry.Title, entry.Narrative, entry.PersonName) ?? "Recorded response";
        }
    }

    private static string? FormatEntryDetail(CensusEntry entry)
    {
        var bits = new List<string>();
        if (!string.IsNullOrWhiteSpace(entry.Narrative))
            bits.Add(entry.Narrative.Trim());
        if (!string.IsNullOrWhiteSpace(entry.PersonName) || !string.IsNullOrWhiteSpace(entry.PersonEmail))
        {
            var person = string.Join(" · ", new[] { entry.PersonName, entry.PersonEmail }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()));
            if (!string.IsNullOrWhiteSpace(person))
                bits.Add(person);
        }
        if (!string.IsNullOrWhiteSpace(entry.ExternalUrl))
            bits.Add(entry.ExternalUrl.Trim());
        if (entry.StaffRole != null)
            bits.Add(entry.StaffRole.Name);
        return bits.Count == 0 ? null : string.Join("\n", bits);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}
