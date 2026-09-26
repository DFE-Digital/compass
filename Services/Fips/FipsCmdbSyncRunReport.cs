using System.Text.Json;

namespace Compass.Services.Fips;

/// <summary>Change report stored on <see cref="Models.FipsSyncHistory.ActionsLog"/>.</summary>
public sealed class FipsCmdbSyncRunReport
{
    public int Created { get; init; }
    public int Updated { get; init; }
    public int Unchanged { get; init; }
    public int SkippedRetired { get; init; }
    public int SkippedNoSysId { get; init; }
    public int StatusSetByRules { get; init; }
    public int Errors { get; init; }
    public int NewStatusCount { get; init; }
    public List<string> ErrorSamples { get; init; } = [];
    public List<FipsCmdbProductChange> Changes { get; init; } = [];

    public static FipsCmdbSyncRunReport Parse(string? actionsLog)
    {
        if (string.IsNullOrWhiteSpace(actionsLog))
            return new FipsCmdbSyncRunReport();

        try
        {
            using var doc = JsonDocument.Parse(actionsLog);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return new FipsCmdbSyncRunReport();

            var changes = ReadChanges(root);
            if (changes.Count == 0)
                changes = ReadLegacyCreated(root);

            return new FipsCmdbSyncRunReport
            {
                Created = ReadInt(root, "Created"),
                Updated = ReadInt(root, "Updated"),
                Unchanged = ReadInt(root, "Unchanged"),
                SkippedRetired = ReadInt(root, "SkippedRetired"),
                SkippedNoSysId = ReadInt(root, "SkippedNoSysId"),
                StatusSetByRules = ReadInt(root, "StatusSetByRules"),
                Errors = ReadInt(root, "Errors"),
                NewStatusCount = ReadInt(root, "NewStatusCount"),
                ErrorSamples = ReadStrings(root, "errorSamples", "ErrorSamples"),
                Changes = changes
            };
        }
        catch (JsonException)
        {
            return new FipsCmdbSyncRunReport();
        }
    }

    private static List<FipsCmdbProductChange> ReadChanges(JsonElement root)
    {
        if (!TryArray(root, "changes", "Changes", out var array))
            return [];

        var list = new List<FipsCmdbProductChange>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var fields = new List<FipsCmdbFieldChange>();
            if (TryArray(item, "fields", "Fields", out var fieldArray))
            {
                foreach (var field in fieldArray.EnumerateArray())
                {
                    if (field.ValueKind != JsonValueKind.Object)
                        continue;
                    fields.Add(new FipsCmdbFieldChange
                    {
                        Field = ReadString(field, "field", "Field") ?? "",
                        From = ReadString(field, "from", "From"),
                        To = ReadString(field, "to", "To")
                    });
                }
            }

            var idText = ReadString(item, "id", "Id");
            list.Add(new FipsCmdbProductChange
            {
                Id = Guid.TryParse(idText, out var id) ? id : Guid.Empty,
                SysId = ReadString(item, "sysId", "SysId") ?? "",
                Title = ReadString(item, "title", "Title") ?? "",
                Kind = ReadString(item, "kind", "Kind") ?? "",
                Fields = fields
            });
        }

        return list;
    }

    private static List<FipsCmdbProductChange> ReadLegacyCreated(JsonElement root)
    {
        if (!TryArray(root, "created", out var array))
            return [];

        var list = new List<FipsCmdbProductChange>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            var idText = ReadString(item, "id", "Id");
            var status = ReadString(item, "status", "Status");
            list.Add(new FipsCmdbProductChange
            {
                Id = Guid.TryParse(idText, out var id) ? id : Guid.Empty,
                Title = ReadString(item, "title", "Title") ?? "",
                Kind = "Created",
                Fields = string.IsNullOrWhiteSpace(status)
                    ? []
                    : [new FipsCmdbFieldChange { Field = "Status", To = status }]
            });
        }

        return list;
    }

    private static int ReadInt(JsonElement root, string name)
    {
        if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        var camel = char.ToLowerInvariant(name[0]) + name[1..];
        if (root.TryGetProperty(camel, out value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out number))
            return number;
        return 0;
    }

    private static List<string> ReadStrings(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
                continue;
            return value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString() ?? "")
                .Where(item => item.Length > 0)
                .ToList();
        }

        return [];
    }

    private static string? ReadString(JsonElement item, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }

    private static bool TryArray(JsonElement item, string camel, string pascal, out JsonElement array) =>
        TryArray(item, out array, camel, pascal);

    private static bool TryArray(JsonElement item, string name, out JsonElement array) =>
        TryArray(item, out array, name);

    private static bool TryArray(JsonElement item, out JsonElement array, params string[] names)
    {
        foreach (var name in names)
        {
            if (item.TryGetProperty(name, out array) && array.ValueKind == JsonValueKind.Array)
                return true;
        }

        array = default;
        return false;
    }
}
