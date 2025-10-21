using System.Text.Json.Serialization;

namespace Compass.Models;

/// <summary>
/// DTO for Product data from CMS API
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    
    [JsonPropertyName("fips_id")]
    public string? FipsId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Phase { get; set; }
    
    [JsonPropertyName("category_values")]
    public List<CategoryValueDto>? CategoryValues { get; set; }
    
    [JsonPropertyName("product_contacts")]
    public List<ProductContactDto>? ProductContacts { get; set; }
}

public class CategoryValueDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
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
    
    [JsonPropertyName("users_permissions_user")]
    public UserPermissionsUserDto? UsersPermissionsUser { get; set; }
}

public class UserPermissionsUserDto
{
    public int Id { get; set; }
    
    public string? Email { get; set; }
    
    public string? Username { get; set; }
}

