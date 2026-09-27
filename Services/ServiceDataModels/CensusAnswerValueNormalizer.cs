using System.Text.Json;
using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>Normalises posted census answer JSON for storage and completion.</summary>
public static class CensusAnswerValueNormalizer
{
    public static string? Normalize(CoreCensusThemeField field, string? valueJson)
    {
        if (field.FieldType is ServiceDataModelFieldType.Services or ServiceDataModelFieldType.ServiceLines)
        {
            if (string.IsNullOrWhiteSpace(valueJson))
                return "[]";

            var ids = ParseStringList(valueJson)
                .Where(v => Guid.TryParse(v, out _))
                .Select(v => Guid.Parse(v).ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return JsonSerializer.Serialize(ids);
        }

        if (field.FieldType is ServiceDataModelFieldType.Text or ServiceDataModelFieldType.MultilineText
            && field.AllowMultiple)
        {
            var values = ParseStringList(valueJson)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();
            return values.Count == 0 ? null : JsonSerializer.Serialize(values);
        }

        if (field.FieldType == ServiceDataModelFieldType.Lookup && field.AllowMultiple)
        {
            var keys = ParseStringList(valueJson)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return keys.Count == 0 ? null : JsonSerializer.Serialize(keys);
        }

        if (field.FieldType == ServiceDataModelFieldType.Lookup && !field.AllowMultiple)
        {
            var keys = ParseStringList(valueJson)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList();
            if (keys.Count == 0)
                return null;
            return JsonSerializer.Serialize(keys[0]);
        }

        return valueJson;
    }

    public static List<string> ParseStringList(string? valueJson)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(valueJson))
            return list;

        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            var el = doc.RootElement;
            if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var x in el.EnumerateArray())
                {
                    if (x.ValueKind == JsonValueKind.String && x.GetString() is { } s)
                        list.Add(s);
                    else if (x.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
                        list.Add(x.GetRawText().Trim('"'));
                }
            }
            else if (el.ValueKind == JsonValueKind.String && el.GetString() is { } one)
            {
                list.Add(one);
            }
            else if (el.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                list.Add(el.GetRawText().Trim('"'));
            }
        }
        catch (JsonException)
        {
            list.Add(valueJson.Trim());
        }

        return list;
    }
}
