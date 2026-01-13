using System.Text.Json.Serialization;

namespace Compass.Models.Fips;

/// <summary>
/// Represents a CMDB entry from ServiceNow
/// </summary>
public class CmdbEntry
{
    [JsonPropertyName("sys_id")]
    public string SysId { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [JsonPropertyName("parent.name")]
    public string? ParentName { get; set; }
    
    [JsonPropertyName("u_delivery_manager")]
    public string? DeliveryManagerId { get; set; }
    
    [JsonPropertyName("u_information_asset_owner")]
    public string? InformationAssetOwnerId { get; set; }
    
    [JsonPropertyName("u_senior_responsible_owner")]
    public string? SeniorResponsibleOwnerId { get; set; }
}

/// <summary>
/// Represents a user from ServiceNow
/// </summary>
public class CmdbUser
{
    [JsonPropertyName("sys_id")]
    public string SysId { get; set; } = string.Empty;
    
    [JsonPropertyName("federated_id")]
    public string? FederatedId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
    
    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("active")]
    public bool Active { get; set; }
}

/// <summary>
/// Users associated with a service offering
/// </summary>
public class CmdbServiceUsers
{
    public CmdbUser? DeliveryManager { get; set; }
    public CmdbUser? InformationAssetOwner { get; set; }
    public CmdbUser? SeniorResponsibleOwner { get; set; }
}

/// <summary>
/// Paged result for CMDB entries
/// </summary>
public class CmdbPagedResult
{
    public List<CmdbEntry> Results { get; set; } = new();
    public int Total { get; set; }
    public bool HasMore { get; set; }
}

/// <summary>
/// Response wrapper for ServiceNow API
/// </summary>
public class ServiceNowResponse<T>
{
    [JsonPropertyName("result")]
    public T? Result { get; set; }
}
