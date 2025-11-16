using System.Text.Json.Serialization;

namespace Compass.Models;

/// <summary>
/// DTO for Product data from CMS API
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Phase { get; set; }
    
    [JsonPropertyName("product_url")]
    public string? ProductUrl { get; set; }
    
    public string State { get; set; } = "New";
    
    [JsonPropertyName("category_values")]
    public List<CategoryValueDto>? CategoryValues { get; set; }
    
    [JsonPropertyName("product_contacts")]
    public List<ProductContactDto>? ProductContacts { get; set; }
    
    [JsonPropertyName("service_owner")]
    public List<EntraUserDto>? ServiceOwners { get; set; }
    
    // Helper property to get the first service owner (for convenience)
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? ServiceOwner => ServiceOwners?.FirstOrDefault();
}

public class CategoryValueDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("code")]
    public string? Code { get; set; }
    
    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; set; }
    
    [JsonPropertyName("category_type")]
    public CategoryTypeDto? CategoryType { get; set; }
}

public class CategoryTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductContactDto
{
    public int Id { get; set; }
    
    public string? Role { get; set; }
    
    [JsonPropertyName("contact_name")]
    public string? ContactName { get; set; }
    
    [JsonPropertyName("contact_email")]
    public string? ContactEmail { get; set; }
    
    [JsonPropertyName("name")]
    public string? LegacyName { get; set; }
    
    [JsonPropertyName("users_permissions_user")]
    public UserPermissionsUserDto? UsersPermissionsUser { get; set; }
}

public class UserPermissionsUserDto
{
    public int Id { get; set; }
    
    public string? Email { get; set; }
    
    public string? Username { get; set; }
    
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

public class EntraUserDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("documentId")]
    public string? DocumentId { get; set; }
    
    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }
    
    [JsonPropertyName("entraId")]
    public string? EntraId { get; set; }
    
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
    
    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }
    
    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
}



