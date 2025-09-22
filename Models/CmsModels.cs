using Newtonsoft.Json;
using System.Text;

namespace FipsReporting.Models
{
    public class CmsProduct
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("fips_id")]
        public string? FipsId { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("short_description")]
        public string? ShortDescription { get; set; }

        [JsonProperty("long_description")]
        public string? LongDescription { get; set; }

        [JsonProperty("product_url")]
        public string? ProductUrl { get; set; }

        [JsonProperty("state")]
        public string State { get; set; } = "New";

        [JsonProperty("category_values")]
        public List<CmsCategoryValue>? CategoryValues { get; set; }

        [JsonProperty("product_contacts")]
        public List<CmsProductContact>? ProductContacts { get; set; }

        [JsonProperty("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
    }

    public class CmsCategoryValue
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonProperty("short_description")]
        public string? ShortDescription { get; set; }

        [JsonProperty("category_type")]
        public CmsCategoryType? CategoryType { get; set; }
    }

    public class CmsCategoryType
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("slug")]
        public string Slug { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string? Description { get; set; }
    }

    public class CmsProductContact
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("role")]
        public string? Role { get; set; }

        [JsonProperty("users_permissions_user")]
        public CmsUser? User { get; set; }

        [JsonProperty("product")]
        public CmsProduct? Product { get; set; }
    }

    public class CmsUser
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty("display_name")]
        public string? DisplayName { get; set; }

        [JsonProperty("first_name")]
        public string? FirstName { get; set; }

        [JsonProperty("last_name")]
        public string? LastName { get; set; }
    }

    public class CmsApiResponse<T>
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; } = new List<T>();

        [JsonProperty("meta")]
        public CmsMeta? Meta { get; set; }
    }

    public class CmsMeta
    {
        [JsonProperty("pagination")]
        public CmsPagination? Pagination { get; set; }
    }

    public class CmsPagination
    {
        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("pageCount")]
        public int PageCount { get; set; }

        [JsonProperty("total")]
        public int Total { get; set; }
    }

    public class ProductFilter
    {
        public string? Search { get; set; }
        public string? State { get; set; }
        public string? CategoryType { get; set; }
        public string? CategoryValue { get; set; }
        public string? UserRole { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "title";
        public string? SortOrder { get; set; } = "asc";
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string? FipsId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? ProductUrl { get; set; }
        public string State { get; set; } = "New";
        public List<string> CategoryValues { get; set; } = new List<string>();
        public List<string> CategoryTypes { get; set; } = new List<string>();
        public List<ProductContactViewModel> ProductContacts { get; set; } = new List<ProductContactViewModel>();
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsAllocatedToUser { get; set; }
    }

    public class ProductContactViewModel
    {
        public int Id { get; set; }
        public string? Role { get; set; }
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
    }

    public class ProductPerformanceViewModel : ProductViewModel
    {
        public string ReportingStatus { get; set; } = "Not started";
        public int CompletedMetrics { get; set; }
        public int TotalMetrics { get; set; }
        public int ProgressPercentage => TotalMetrics > 0 ? (int)Math.Round((double)CompletedMetrics / TotalMetrics * 100) : 0;
        public string ProgressStatus
        {
            get
            {
                if (ProgressPercentage == 0) return "Not started";
                if (ProgressPercentage == 100) return "Completed";
                return "In progress";
            }
        }
    }

    public class ProductPerformanceFormViewModel
    {
        public ProductViewModel Product { get; set; } = new ProductViewModel();
        public List<PerformanceMetricFormItem> Metrics { get; set; } = new List<PerformanceMetricFormItem>();
        public string ReportingPeriod { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Month { get; set; } = string.Empty;
    }

    public class PerformanceMetricFormItem
    {
        public int Id { get; set; }
        public string UniqueId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Measure { get; set; } = string.Empty;
        public bool Mandatory { get; set; }
        public bool CanReportNullReturn { get; set; }
        public string? Value { get; set; }
        public bool IsNullReturn { get; set; }
        public bool IsCompleted => !string.IsNullOrEmpty(Value) || IsNullReturn;
    }
}
