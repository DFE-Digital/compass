using System.Text.Json.Serialization;

namespace Compass.Models.Fips;

public class StrapiProduct
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;
    
    [JsonPropertyName("attributes")]
    public StrapiProductAttributes? Attributes { get; set; }
    
    // Flat properties for easier access
    [JsonIgnore]
    public string? CmdbSysId => Attributes?.CmdbSysId;
    
    [JsonIgnore]
    public string? Title => Attributes?.Title;
    
    [JsonIgnore]
    public string? State => Attributes?.State;
}

public class StrapiProductAttributes
{
    [JsonPropertyName("cmdb_sys_id")]
    public string? CmdbSysId { get; set; }
    
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    
    [JsonPropertyName("short_description")]
    public string? ShortDescription { get; set; }
    
    [JsonPropertyName("long_description")]
    public string? LongDescription { get; set; }
    
    [JsonPropertyName("parent_category")]
    public string? ParentCategory { get; set; }
    
    [JsonPropertyName("state")]
    public string? State { get; set; }
    
    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }
    
    [JsonPropertyName("cmdb_last_sync")]
    public DateTime? CmdbLastSync { get; set; }
    
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
}

public class StrapiResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("meta")]
    public StrapiMeta? Meta { get; set; }
}

public class StrapiMeta
{
    [JsonPropertyName("pagination")]
    public StrapiPagination? Pagination { get; set; }
}

public class StrapiPagination
{
    [JsonPropertyName("page")]
    public int Page { get; set; }
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
    
    [JsonPropertyName("pageCount")]
    public int PageCount { get; set; }
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class StrapiCreateProductRequest
{
    [JsonPropertyName("data")]
    public StrapiProductData Data { get; set; } = new();
}

public class StrapiProductData
{
    [JsonPropertyName("cmdb_sys_id")]
    public string CmdbSysId { get; set; } = string.Empty;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("short_description")]
    public string ShortDescription { get; set; } = string.Empty;
    
    [JsonPropertyName("long_description")]
    public string LongDescription { get; set; } = string.Empty;
    
    [JsonPropertyName("parent_category")]
    public string? ParentCategory { get; set; }
    
    [JsonPropertyName("state")]
    public string State { get; set; } = "New";
    
    [JsonPropertyName("cmdb_last_sync")]
    public DateTime CmdbLastSync { get; set; }
    
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
}
