using System.Text.Json.Serialization;

namespace Compass.Models;

public class SearchTerm
{
    public int Id { get; set; }
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    [JsonPropertyName("search_term")]
    public string SearchTermText { get; set; } = string.Empty;
    [JsonPropertyName("result_count")]
    public int ResultCount { get; set; }
    [JsonPropertyName("results")]
    public List<SearchTermResult>? Results { get; set; }
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class SearchTermResult
{
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
