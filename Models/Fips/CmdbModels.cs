using System;
using System.Text.Json;
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
    
    [JsonPropertyName("owned_by")]
    public object? OwnedBy { get; set; } // Can be string or object with .value
    
    [JsonPropertyName("u_product_manager")]
    public object? ProductManagerId { get; set; } // Can be string or object with .value
    
    [JsonPropertyName("delivery_manager")]
    public object? DeliveryManagerId { get; set; } // Can be string or object with .value
    
    [JsonPropertyName("u_information_asset_owner")]
    public object? InformationAssetOwnerId { get; set; } // Can be string or object with .value
    
    [JsonPropertyName("u_senior_responsible_owner")]
    public object? SeniorResponsibleOwnerId { get; set; } // Can be string or object with .value
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
    
    // ServiceNow returns active as a string ("true"/"false"), not a boolean
    [JsonPropertyName("active")]
    [JsonConverter(typeof(StringToBoolConverter))]
    public bool Active { get; set; }
}

/// <summary>
/// Custom JSON converter to handle ServiceNow's string boolean values
/// </summary>
public class StringToBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            return stringValue?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
        }
        else if (reader.TokenType == JsonTokenType.True)
        {
            return true;
        }
        else if (reader.TokenType == JsonTokenType.False)
        {
            return false;
        }
        
        throw new JsonException($"Unexpected token type {reader.TokenType} when converting to boolean");
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}

/// <summary>
/// Users associated with a service offering
/// </summary>
public class CmdbServiceUsers
{
    public CmdbUser? ServiceOwner { get; set; } // from owned_by
    public CmdbUser? ProductManager { get; set; } // from u_product_manager
    public CmdbUser? DeliveryManager { get; set; } // from delivery_manager
    public CmdbUser? InformationAssetOwner { get; set; } // from u_information_asset_owner
    public CmdbUser? SeniorResponsibleOwner { get; set; } // from u_senior_responsible_owner
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
