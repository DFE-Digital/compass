using System.Text.Json.Serialization;

namespace Compass.Models;

// API Response wrappers matching the frontend pattern
public class ApiResponse<T>
{
    public T? Data { get; set; }
    public ApiMeta? Meta { get; set; }
}

public class ApiCollectionResponse<T>
{
    public List<T>? Data { get; set; }
    public ApiMeta? Meta { get; set; }
}

public class ApiMeta
{
    public ApiPagination? Pagination { get; set; }
}

public class ApiPagination
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int PageCount { get; set; }
    public int Total { get; set; }
}