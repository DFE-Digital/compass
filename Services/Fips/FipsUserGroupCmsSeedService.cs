using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Compass.Data;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Compass.Services.Fips;

/// <summary>
/// Seeds <see cref="FipsUserGroup"/> rows from the restructured CMS export or live CMS API.
/// </summary>
public static class FipsUserGroupCmsSeedService
{
    private const string UserGroupCategoryTypeName = "User group";
    public const string DefaultSeedJsonRelativePath = "Data/SeedData/fips-user-groups.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed record SeedResult(
        int Added,
        int Updated,
        int Skipped,
        int Deactivated,
        int TotalFromSource);

    public static async Task<SeedResult> ApplyFromJsonAsync(
        CompassDbContext context,
        string jsonFilePath,
        bool deactivateMissing = false,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException($"Seed JSON not found: {jsonFilePath}", jsonFilePath);

        var payload = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);
        var document = JsonSerializer.Deserialize<RestructuredUserGroupsDocument>(payload, JsonOptions)
            ?? throw new InvalidOperationException("Seed JSON did not deserialize.");

        var sourceRows = document.Flat?
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToList() ?? [];

        if (sourceRows.Count == 0)
            return new SeedResult(0, 0, 0, 0, 0);

        return await ApplyRowsAsync(context, sourceRows, deactivateMissing, cancellationToken);
    }

    public static async Task<SeedResult> ApplyFromCmsAsync(
        CompassDbContext context,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var cmsRows = await FetchUserGroupsFromCmsAsync(configuration, cancellationToken);
        if (cmsRows.Count == 0)
            return new SeedResult(0, 0, 0, 0, 0);

        var sourceRows = cmsRows
            .Select(x => new RestructuredUserGroupRow
            {
                Id = 0,
                DocumentId = x.DocumentId,
                Name = x.Name,
                SortOrder = x.SortOrder,
                ShortDescription = x.ShortDescription,
                ParentId = null,
                Level = x.Depth,
                Path = x.Name
            })
            .ToList();

        // CMS flat fetch lacks reliable hierarchy; prefer bundled JSON for seeding.
        return await ApplyRowsAsync(context, sourceRows, deactivateMissing: false, cancellationToken);
    }

    private static async Task<SeedResult> ApplyRowsAsync(
        CompassDbContext context,
        IReadOnlyList<RestructuredUserGroupRow> sourceRows,
        bool deactivateMissing,
        CancellationToken cancellationToken)
    {
        var existing = await context.FipsUserGroups.ToListAsync(cancellationToken);

        var byName = existing
            .GroupBy(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sourceIdToCompassId = new Dictionary<int, int>();
        var matchedCompassIds = new HashSet<int>();

        var added = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in sourceRows
                     .OrderBy(r => r.Level)
                     .ThenBy(r => r.SortOrder ?? 0)
                     .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            int? parentId = null;
            if (row.ParentId.HasValue
                && sourceIdToCompassId.TryGetValue(row.ParentId.Value, out var mappedParentId))
            {
                parentId = mappedParentId;
            }

            var name = row.Name.Trim();
            var description = string.IsNullOrWhiteSpace(row.ShortDescription) ? null : row.ShortDescription.Trim();
            var sortOrder = row.SortOrder ?? 0;

            if (byName.TryGetValue(name, out var entity))
            {
                var changed = false;

                if (entity.DisplayOrder != sortOrder)
                {
                    entity.DisplayOrder = sortOrder;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(entity.Description) && description != null)
                {
                    entity.Description = description;
                    changed = true;
                }

                if (!entity.Active)
                {
                    entity.Active = true;
                    changed = true;
                }

                if (entity.ParentId != parentId)
                {
                    entity.ParentId = parentId;
                    changed = true;
                }

                if (changed)
                    updated++;
                else
                    skipped++;

                matchedCompassIds.Add(entity.Id);
                if (row.Id != 0)
                    sourceIdToCompassId[row.Id] = entity.Id;

                continue;
            }

            entity = new FipsUserGroup
            {
                Name = name,
                Description = description,
                DisplayOrder = sortOrder,
                ParentId = parentId,
                Active = true
            };
            context.FipsUserGroups.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            byName[name] = entity;
            matchedCompassIds.Add(entity.Id);
            if (row.Id != 0)
                sourceIdToCompassId[row.Id] = entity.Id;
            added++;
        }

        var deactivated = 0;
        if (deactivateMissing)
        {
            foreach (var entity in existing.Where(e => e.Active && !matchedCompassIds.Contains(e.Id)))
            {
                entity.Active = false;
                deactivated++;
            }
        }

        if (updated > 0 || deactivated > 0)
            await context.SaveChangesAsync(cancellationToken);

        return new SeedResult(added, updated, skipped, deactivated, sourceRows.Count);
    }

    public static string ResolveSeedJsonPath(string? jsonFilePath)
    {
        if (!string.IsNullOrWhiteSpace(jsonFilePath))
            return Path.GetFullPath(jsonFilePath);

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), DefaultSeedJsonRelativePath),
            Path.Combine(AppContext.BaseDirectory, DefaultSeedJsonRelativePath)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return candidates[0];
    }

    private static async Task<List<CmsUserGroupRow>> FetchUserGroupsFromCmsAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var baseUrl = configuration["CmsApi:BaseUrl"]?.TrimEnd('/') ?? "https://fips-cms-test.azurewebsites.net/api";
        if (!baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            baseUrl += "/api";

        var readApiKey = configuration["CmsApi:ReadApiKey"];
        using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl + "/") };
        if (!string.IsNullOrWhiteSpace(readApiKey))
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readApiKey);

        var allRows = new List<CmsCategoryValueDto>();
        var page = 1;
        const int pageSize = 100;
        var pageCount = 1;

        while (page <= pageCount)
        {
            var query = string.Join("&", new[]
            {
                $"filters[category_type][name][$eq]={Uri.EscapeDataString(UserGroupCategoryTypeName)}",
                "sort=sort_order:asc",
                "fields[0]=name",
                "fields[1]=slug",
                "fields[2]=sort_order",
                "fields[3]=short_description",
                "fields[4]=documentId",
                "populate[parent][fields][0]=documentId",
                $"pagination[page]={page}",
                $"pagination[pageSize]={pageSize}"
            });

            var response = await httpClient.GetAsync($"category-values?{query}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<CmsApiCollectionResponse<CmsCategoryValueDto>>(payload, JsonOptions);
            if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
                break;

            allRows.AddRange(apiResponse.Data);
            pageCount = apiResponse.Meta?.Pagination?.PageCount ?? page;
            page++;
        }

        var byDocumentId = allRows
            .Where(x => !string.IsNullOrWhiteSpace(x.DocumentId))
            .ToDictionary(x => x.DocumentId!, x => x, StringComparer.OrdinalIgnoreCase);

        return allRows
            .Where(x => !string.IsNullOrWhiteSpace(x.DocumentId) && !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new CmsUserGroupRow(
                x.DocumentId!,
                x.Name.Trim(),
                x.Parent?.DocumentId,
                x.SortOrder ?? 0,
                x.ShortDescription?.Trim(),
                ComputeDepth(x, byDocumentId)))
            .ToList();
    }

    private static int ComputeDepth(CmsCategoryValueDto item, IReadOnlyDictionary<string, CmsCategoryValueDto> byDocumentId)
    {
        var depth = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parentDocumentId = item.Parent?.DocumentId;

        while (!string.IsNullOrWhiteSpace(parentDocumentId))
        {
            if (!seen.Add(parentDocumentId))
                break;

            depth++;
            if (!byDocumentId.TryGetValue(parentDocumentId, out var parent))
                break;

            parentDocumentId = parent.Parent?.DocumentId;
        }

        return depth;
    }

    private sealed class RestructuredUserGroupsDocument
    {
        public List<RestructuredUserGroupRow>? Flat { get; set; }
    }

    private sealed class RestructuredUserGroupRow
    {
        public int Id { get; set; }

        [JsonPropertyName("documentId")]
        public string? DocumentId { get; set; }

        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sort_order")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("short_description")]
        public string? ShortDescription { get; set; }

        [JsonPropertyName("parent_id")]
        public int? ParentId { get; set; }

        public int Level { get; set; }

        public string? Path { get; set; }
    }

    private sealed record CmsUserGroupRow(
        string DocumentId,
        string Name,
        string? ParentDocumentId,
        int SortOrder,
        string? ShortDescription,
        int Depth);

    private sealed class CmsApiCollectionResponse<T>
    {
        public List<T> Data { get; set; } = new();
        public CmsApiMeta? Meta { get; set; }
    }

    private sealed class CmsApiMeta
    {
        public CmsApiPagination? Pagination { get; set; }
    }

    private sealed class CmsApiPagination
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public int Total { get; set; }
    }

    private sealed class CmsCategoryValueDto
    {
        [JsonPropertyName("documentId")]
        public string? DocumentId { get; set; }

        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("sort_order")]
        public int? SortOrder { get; set; }

        [JsonPropertyName("short_description")]
        public string? ShortDescription { get; set; }

        public CmsCategoryValueParentDto? Parent { get; set; }
    }

    private sealed class CmsCategoryValueParentDto
    {
        [JsonPropertyName("documentId")]
        public string? DocumentId { get; set; }
    }
}
