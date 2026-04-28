using System.Text.Json;
using Compass.Models.DemandPipeline;

namespace Compass.Services.DemandPipeline;

public static class ExploreRelatedLinksHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<ExploreRelatedLinkDto> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<ExploreRelatedLinkDto>();
        try
        {
            var list = JsonSerializer.Deserialize<List<ExploreRelatedLinkDto>>(json, JsonOptions);
            return list ?? new List<ExploreRelatedLinkDto>();
        }
        catch
        {
            return Array.Empty<ExploreRelatedLinkDto>();
        }
    }

    public static string Serialize(IReadOnlyList<ExploreRelatedLinkDto>? links)
    {
        var list = links ?? Array.Empty<ExploreRelatedLinkDto>();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    /// <summary>Normalise and drop invalid rows.</summary>
    public static List<ExploreRelatedLinkDto> Sanitize(IEnumerable<ExploreRelatedLinkDto>? raw)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ExploreRelatedLinkDto>();
        foreach (var x in raw ?? Array.Empty<ExploreRelatedLinkDto>())
        {
            var kind = (x.Kind ?? "").Trim();
            var isWork = string.Equals(kind, "Work", StringComparison.OrdinalIgnoreCase);
            var isLiveService = string.Equals(kind, "LiveService", StringComparison.OrdinalIgnoreCase);
            if (!isWork && !isLiveService)
                continue;

            var label = (x.Label ?? "").Trim();
            if (isWork)
            {
                if (!x.ProjectId.HasValue || x.ProjectId.Value <= 0) continue;
                var key = $"Work:{x.ProjectId.Value}";
                if (!seen.Add(key)) continue;
                var code = string.IsNullOrWhiteSpace(x.ProjectCode) ? null : x.ProjectCode.Trim();
                result.Add(new ExploreRelatedLinkDto
                {
                    Kind = "Work",
                    ProjectId = x.ProjectId,
                    ProjectCode = code,
                    Label = string.IsNullOrEmpty(label) ? $"Work #{x.ProjectId}" : label
                });
            }
            else if (isLiveService)
            {
                if (x.CmdbProductId.HasValue && x.CmdbProductId.Value != Guid.Empty)
                {
                    var key = $"LiveService:{x.CmdbProductId.Value}";
                    if (!seen.Add(key)) continue;
                    result.Add(new ExploreRelatedLinkDto
                    {
                        Kind = "LiveService",
                        CmdbProductId = x.CmdbProductId,
                        FipsId = string.IsNullOrWhiteSpace(x.FipsId) ? null : x.FipsId.Trim(),
                        Label = string.IsNullOrEmpty(label) ? $"FIPS #{x.FipsId}" : label
                    });
                }
                else if (x.ServiceId.HasValue && x.ServiceId.Value > 0)
                {
                    var key = $"LiveService:{x.ServiceId.Value}";
                    if (!seen.Add(key)) continue;
                    result.Add(new ExploreRelatedLinkDto
                    {
                        Kind = "LiveService",
                        ServiceId = x.ServiceId,
                        FipsId = string.IsNullOrWhiteSpace(x.FipsId) ? null : x.FipsId.Trim(),
                        Label = string.IsNullOrEmpty(label) ? $"Service #{x.ServiceId}" : label
                    });
                }
            }
        }

        return result;
    }
}
