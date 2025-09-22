using FipsReporting.Models;

namespace FipsReporting.Services
{
    public class MockCmsApiService
    {
        private readonly ILogger<MockCmsApiService> _logger;

        public MockCmsApiService(ILogger<MockCmsApiService> logger)
        {
            _logger = logger;
        }

        public Task<CmsApiResponse<CmsProduct>> GetProductsAsync(ProductFilter filter)
        {
            _logger.LogInformation("Using mock CMS API - returning mock product for development");
            
            // Create a mock product that andy.jones@education.gov.uk is assigned to
            var mockProduct = new CmsProduct
            {
                Id = 1,
                FipsId = "FIPS-001",
                Title = "Test Product 1",
                ShortDescription = "This is a test product for reporting.",
                LongDescription = "This is a comprehensive test product used for development and testing of the COMPASS reporting system.",
                ProductUrl = "https://example.gov.uk/test-product",
                State = "Live",
                PublishedAt = DateTime.UtcNow.AddDays(-30),
                CreatedAt = DateTime.UtcNow.AddDays(-60),
                UpdatedAt = DateTime.UtcNow.AddDays(-7),
                ProductContacts = new List<CmsProductContact>
                {
                    new CmsProductContact
                    {
                        Id = 1,
                        Role = "service_owner",
                        User = new CmsUser
                        {
                            Id = 1,
                            Email = "andy.jones@education.gov.uk",
                            Username = "andy.jones",
                            DisplayName = "Andy Jones"
                        }
                    },
                    new CmsProductContact
                    {
                        Id = 2,
                        Role = "reporting",
                        User = new CmsUser
                        {
                            Id = 1,
                            Email = "andy.jones@education.gov.uk",
                            Username = "andy.jones",
                            DisplayName = "Andy Jones"
                        }
                    }
                },
                CategoryValues = new List<CmsCategoryValue>
                {
                    new CmsCategoryValue
                    {
                        Id = 1,
                        Name = "Digital Service",
                        Slug = "digital-service",
                        ShortDescription = "A digital service",
                        CategoryType = new CmsCategoryType
                        {
                            Id = 1,
                            Name = "Service Type",
                            Slug = "service-type",
                            Description = "The type of service"
                        }
                    }
                }
            };
            
            var mockResponse = new CmsApiResponse<CmsProduct>
            {
                Data = new List<CmsProduct> { mockProduct },
                Meta = new CmsMeta
                {
                    Pagination = new CmsPagination
                    {
                        Page = filter.Page,
                        PageSize = filter.PageSize,
                        PageCount = 1,
                        Total = 1
                    }
                }
            };

            return Task.FromResult(mockResponse);
        }

        public Task<CmsProduct?> GetProductByIdAsync(int id)
        {
            _logger.LogInformation("Using mock CMS API - returning mock product {Id}", id);
            
            if (id == 1)
            {
                // Return the same mock product
                var mockProduct = new CmsProduct
                {
                    Id = 1,
                    FipsId = "FIPS-001",
                    Title = "Test Product 1",
                    ShortDescription = "This is a test product for reporting.",
                    LongDescription = "This is a comprehensive test product used for development and testing of the COMPASS reporting system.",
                    ProductUrl = "https://example.gov.uk/test-product",
                    State = "Live",
                    PublishedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7),
                    ProductContacts = new List<CmsProductContact>
                    {
                        new CmsProductContact
                        {
                            Id = 1,
                            Role = "service_owner",
                            User = new CmsUser
                            {
                                Id = 1,
                                Email = "andy.jones@education.gov.uk",
                                Username = "andy.jones",
                                DisplayName = "Andy Jones"
                            }
                        },
                        new CmsProductContact
                        {
                            Id = 2,
                            Role = "reporting",
                            User = new CmsUser
                            {
                                Id = 1,
                                Email = "andy.jones@education.gov.uk",
                                Username = "andy.jones",
                                DisplayName = "Andy Jones"
                            }
                        }
                    },
                    CategoryValues = new List<CmsCategoryValue>
                    {
                        new CmsCategoryValue
                        {
                            Id = 1,
                            Name = "Digital Service",
                            Slug = "digital-service",
                            ShortDescription = "A digital service",
                            CategoryType = new CmsCategoryType
                            {
                                Id = 1,
                                Name = "Service Type",
                                Slug = "service-type",
                                Description = "The type of service"
                            }
                        }
                    }
                };
                
                return Task.FromResult<CmsProduct?>(mockProduct);
            }
            
            return Task.FromResult<CmsProduct?>(null);
        }

        public Task<List<CmsProduct>> GetProductsByIdsAsync(List<string> productIds)
        {
            _logger.LogInformation("Using mock CMS API - returning mock product for IDs: {ProductIds}", string.Join(", ", productIds));
            
            var mockProducts = new List<CmsProduct>();
            
            if (productIds.Contains("1") || productIds.Contains("FIPS-001"))
            {
                var mockProduct = new CmsProduct
                {
                    Id = 1,
                    FipsId = "FIPS-001",
                    Title = "Test Product 1",
                    ShortDescription = "This is a test product for reporting.",
                    LongDescription = "This is a comprehensive test product used for development and testing of the COMPASS reporting system.",
                    ProductUrl = "https://example.gov.uk/test-product",
                    State = "Live",
                    PublishedAt = DateTime.UtcNow.AddDays(-30),
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7),
                    ProductContacts = new List<CmsProductContact>
                    {
                        new CmsProductContact
                        {
                            Id = 1,
                            Role = "service_owner",
                            User = new CmsUser
                            {
                                Id = 1,
                                Email = "andy.jones@education.gov.uk",
                                Username = "andy.jones",
                                DisplayName = "Andy Jones"
                            }
                        },
                        new CmsProductContact
                        {
                            Id = 2,
                            Role = "reporting",
                            User = new CmsUser
                            {
                                Id = 1,
                                Email = "andy.jones@education.gov.uk",
                                Username = "andy.jones",
                                DisplayName = "Andy Jones"
                            }
                        }
                    },
                    CategoryValues = new List<CmsCategoryValue>
                    {
                        new CmsCategoryValue
                        {
                            Id = 1,
                            Name = "Digital Service",
                            Slug = "digital-service",
                            ShortDescription = "A digital service",
                            CategoryType = new CmsCategoryType
                            {
                                Id = 1,
                                Name = "Service Type",
                                Slug = "service-type",
                                Description = "The type of service"
                            }
                        }
                    }
                };
                
                mockProducts.Add(mockProduct);
            }
            
            return Task.FromResult(mockProducts);
        }

        // Add the missing MapToViewModel method
        public ProductViewModel MapToViewModel(CmsProduct product, bool isAllocatedToUser = false)
        {
            return new ProductViewModel
            {
                Id = product.Id,
                FipsId = product.FipsId,
                Title = product.Title,
                ShortDescription = product.ShortDescription,
                LongDescription = product.LongDescription,
                ProductUrl = product.ProductUrl,
                State = product.State,
                IsPublished = product.PublishedAt.HasValue,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                IsAllocatedToUser = isAllocatedToUser,
                CategoryValues = product.CategoryValues?.Select(cv => cv.Name ?? "").ToList() ?? new List<string>(),
                CategoryTypes = product.CategoryValues?.Select(cv => cv.CategoryType?.Name ?? "").ToList() ?? new List<string>(),
                ProductContacts = product.ProductContacts?.Select(pc => new ProductContactViewModel
                {
                    Id = pc.Id,
                    Role = pc.Role,
                    UserEmail = pc.User?.Email,
                    UserName = pc.User?.Username,
                    DisplayName = pc.User?.DisplayName
                }).ToList() ?? new List<ProductContactViewModel>()
            };
        }
    }
}
