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
    
    [JsonPropertyName("short_description")]
    public string? ShortDescription { get; set; }
    
    [JsonPropertyName("long_description")]
    public string? LongDescription { get; set; }
    
    [JsonPropertyName("cmdb_sys_id")]
    public string? CmdbSysId { get; set; }
    
    public string State { get; set; } = "New";
    
    [JsonPropertyName("publishedAt")]
    public DateTime? PublishedAt { get; set; }
    
    [JsonPropertyName("category_values")]
    public List<CategoryValueDto>? CategoryValues { get; set; }
    
    [JsonPropertyName("product_contacts")]
    public List<ProductContactDto>? ProductContacts { get; set; }
    
    [JsonPropertyName("service_owner")]
    public List<EntraUserDto>? ServiceOwners { get; set; }
    
    [JsonPropertyName("product_manager")]
    public List<EntraUserDto>? ProductManagers { get; set; }
    
    [JsonPropertyName("delivery_manager")]
    public List<EntraUserDto>? DeliveryManagers { get; set; }
    
    [JsonPropertyName("Information_asset_owner")]
    public List<EntraUserDto>? InformationAssetOwners { get; set; }
    
    [JsonPropertyName("senior_responsible_officer")]
    public List<EntraUserDto>? SeniorResponsibleOfficers { get; set; }
    
    [JsonPropertyName("service_designs")]
    public List<EntraUserDto>? ServiceDesigns { get; set; }
    
    [JsonPropertyName("user_researchers")]
    public List<EntraUserDto>? UserResearchers { get; set; }
    
    // Helper properties to get the first user (for convenience)
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? ServiceOwner => ServiceOwners?.FirstOrDefault();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? ProductManager => ProductManagers?.FirstOrDefault();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? DeliveryManager => DeliveryManagers?.FirstOrDefault();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? InformationAssetOwner => InformationAssetOwners?.FirstOrDefault();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? SeniorResponsibleOfficer => SeniorResponsibleOfficers?.FirstOrDefault();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? ServiceDesign => ServiceDesigns?.FirstOrDefault();
    
    [System.Text.Json.Serialization.JsonIgnore]
    public EntraUserDto? UserResearcher => UserResearchers?.FirstOrDefault();
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



