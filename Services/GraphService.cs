using Compass.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;

namespace Compass.Services;

public class GraphService : IGraphService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphService> _logger;
    private readonly IMemoryCache _cache;
    private readonly GraphServiceClient? _graphClient;
    private readonly bool _isConfigured;

    public GraphService(
        IConfiguration configuration,
        ILogger<GraphService> logger,
        IMemoryCache cache)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;

        // Try to initialize Graph client
        // Use Entra section for Graph API credentials (separate from AzureAd auth)
        var tenantId = configuration["Entra:TenantId"];
        var clientId = configuration["Entra:ClientId"];
        var clientSecret = configuration["Entra:ClientSecret"];

        if (!string.IsNullOrEmpty(tenantId) &&
            !string.IsNullOrEmpty(clientId) &&
            !string.IsNullOrEmpty(clientSecret))
        {
            try
            {
                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var clientSecretCredential = new ClientSecretCredential(
                    tenantId, clientId, clientSecret, options);

                _graphClient = new GraphServiceClient(clientSecretCredential);
                _isConfigured = true;
                _logger.LogInformation("Microsoft Graph API configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Microsoft Graph client");
                _isConfigured = false;
            }
        }
        else
        {
            _logger.LogWarning("Microsoft Graph API credentials not configured. Will use mock data.");
            _isConfigured = false;
        }
    }

    public async Task<List<StaffMember>> SearchStaffAsync(string searchTerm, int maxResults = 20)
    {
        if (_isConfigured && _graphClient != null)
        {
            try
            {
                _logger.LogInformation("Searching Entra ID for: {SearchTerm}", searchTerm);

                var allResults = new List<Microsoft.Graph.Models.User>();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Use startswith filters with multiple case variations for better matching
                    // User.ReadBasic.All supports startswith but not contains
                    var escapedTerm = searchTerm.Replace("'", "''");
                    
                    // Build filter with startswith (case-sensitive, so we try multiple variations)
                    // We'll also search across multiple fields
                    var searchLower = searchTerm.ToLower();
                    var searchUpper = searchTerm.ToUpper();
                    var searchTitleCase = char.ToUpper(searchTerm[0]) + searchTerm.Substring(1).ToLower();
                    
                    var filterParts = new List<string>();
                    
                    // Try different case variations for each field
                    foreach (var field in new[] { "displayName", "givenName", "surname", "mail", "userPrincipalName" })
                    {
                        filterParts.Add($"startswith({field},'{escapedTerm}')");
                        filterParts.Add($"startswith({field},'{searchLower}')");
                        filterParts.Add($"startswith({field},'{searchUpper}')");
                        filterParts.Add($"startswith({field},'{searchTitleCase}')");
                    }
                    
                    var filter = string.Join(" or ", filterParts.Distinct());
                    
                    try
                    {
                        // Get users matching the filter
                        var users = await _graphClient.Users
                            .GetAsync(requestConfiguration =>
                            {
                                requestConfiguration.QueryParameters.Filter = filter;
                                requestConfiguration.QueryParameters.Select = new[] { "displayName", "mail", "jobTitle", "department", "userPrincipalName", "givenName", "surname" };
                                requestConfiguration.QueryParameters.Top = maxResults * 2; // Get more to filter client-side
                            });

                        if (users?.Value != null && users.Value.Count > 0)
                        {
                            // Filter client-side for partial matches (since contains isn't supported)
                            var searchLowerForFilter = searchTerm.ToLower();
                            var filtered = users.Value.Where(u =>
                                (u.DisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                (u.GivenName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                (u.Surname?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                (u.Mail?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                (u.UserPrincipalName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                            ).ToList();
                            
                            allResults.AddRange(filtered);
                        }
                    }
                    catch (Exception filterEx)
                    {
                        _logger.LogWarning(filterEx, "Filter query failed, trying simpler approach");
                        
                        // Fallback: Try simpler single-case startswith
                        try
                        {
                            var simpleFilter = $"startswith(displayName,'{escapedTerm}') or startswith(givenName,'{escapedTerm}') or startswith(surname,'{escapedTerm}') or startswith(mail,'{escapedTerm}') or startswith(userPrincipalName,'{escapedTerm}')";
                            
                            var users = await _graphClient.Users
                                .GetAsync(requestConfiguration =>
                                {
                                    requestConfiguration.QueryParameters.Filter = simpleFilter;
                                    requestConfiguration.QueryParameters.Select = new[] { "displayName", "mail", "jobTitle", "department", "userPrincipalName", "givenName", "surname" };
                                    requestConfiguration.QueryParameters.Top = maxResults * 2;
                                });

                            if (users?.Value != null && users.Value.Count > 0)
                            {
                                // Filter client-side for partial matches
                                var filtered = users.Value.Where(u =>
                                    (u.DisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                    (u.GivenName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                    (u.Surname?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                    (u.Mail?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true) ||
                                    (u.UserPrincipalName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true)
                                ).ToList();
                                
                                allResults.AddRange(filtered);
                            }
                        }
                        catch (Exception simpleEx)
                        {
                            _logger.LogWarning(simpleEx, "Simple filter also failed");
                            throw;
                        }
                    }
                }
                else
                {
                    // No search term - get all users (limited)
                    var users = await _graphClient.Users
                        .GetAsync(requestConfiguration =>
                        {
                            requestConfiguration.QueryParameters.Select = new[] { "displayName", "mail", "jobTitle", "department", "userPrincipalName", "givenName", "surname" };
                            requestConfiguration.QueryParameters.Top = maxResults;
                        });

                    if (users?.Value != null && users.Value.Count > 0)
                    {
                        allResults.AddRange(users.Value);
                    }
                }

                if (allResults.Count == 0)
                {
                    _logger.LogInformation("No users found in Entra ID for: {SearchTerm}", searchTerm);
                    return new List<StaffMember>();
                }

                _logger.LogInformation("Found {Count} users in Entra ID", allResults.Count);

                // Sort results in-memory by display name and remove duplicates
                return allResults
                    .GroupBy(u => u.Id)
                    .Select(g => g.First())
                    .Select(u => new StaffMember
                    {
                        DisplayName = u.DisplayName ?? "",
                        Email = u.Mail ?? u.UserPrincipalName ?? "",
                        JobTitle = u.JobTitle,
                        Department = u.Department
                    })
                    .OrderBy(s => s.DisplayName)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Microsoft Graph API. Falling back to mock data.");
                return GetMockStaffData(searchTerm, maxResults);
            }
        }

        // Fallback to mock data if not configured
        _logger.LogWarning("Using mock data - Graph API not configured");
        return GetMockStaffData(searchTerm, maxResults);
    }

    public async Task<StaffMember?> GetStaffByEmailAsync(string email)
    {
        // For now, return mock data
        // In production, this would call: GET https://graph.microsoft.com/v1.0/users/{email}

        var allStaff = GetMockStaffData("", 100);
        return await Task.FromResult(allStaff.FirstOrDefault(s => s.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }

    private List<StaffMember> GetMockStaffData(string searchTerm, int maxResults)
    {
        // Mock data for development - replace with actual Graph API call in production
        var mockStaff = new List<StaffMember>
        {
            new StaffMember { DisplayName = "Andy Jones", Email = "andy.jones@education.gov.uk", JobTitle = "Technical Architect", Department = "Digital Services" },
            new StaffMember { DisplayName = "Sarah Williams", Email = "sarah.williams@education.gov.uk", JobTitle = "Product Manager", Department = "Product Team" },
            new StaffMember { DisplayName = "John Smith", Email = "john.smith@education.gov.uk", JobTitle = "Delivery Manager", Department = "Delivery" },
            new StaffMember { DisplayName = "Emma Brown", Email = "emma.brown@education.gov.uk", JobTitle = "Developer", Department = "Engineering" },
            new StaffMember { DisplayName = "Michael Davis", Email = "michael.davis@education.gov.uk", JobTitle = "Security Lead", Department = "Security" },
            new StaffMember { DisplayName = "Lisa Johnson", Email = "lisa.johnson@education.gov.uk", JobTitle = "Business Analyst", Department = "Analysis" },
            new StaffMember { DisplayName = "David Wilson", Email = "david.wilson@education.gov.uk", JobTitle = "Service Owner", Department = "Service Delivery" },
            new StaffMember { DisplayName = "Sophie Taylor", Email = "sophie.taylor@education.gov.uk", JobTitle = "UX Designer", Department = "Design" },
            new StaffMember { DisplayName = "James Anderson", Email = "james.anderson@education.gov.uk", JobTitle = "Infrastructure Lead", Department = "Infrastructure" },
            new StaffMember { DisplayName = "Rachel Thomas", Email = "rachel.thomas@education.gov.uk", JobTitle = "Content Designer", Department = "Content" }
        };

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return mockStaff.Take(maxResults).ToList();
        }

        var searchLower = searchTerm.ToLower();
        return mockStaff
            .Where(s =>
                s.DisplayName.ToLower().Contains(searchLower) ||
                s.Email.ToLower().Contains(searchLower) ||
                (s.JobTitle?.ToLower().Contains(searchLower) ?? false))
            .Take(maxResults)
            .ToList();
    }

    // Production implementation would look like this:
    /*
    private async Task<List<StaffMember>> CallGraphApiAsync(string searchTerm, int maxResults)
    {
        try
        {
            var accessToken = await GetGraphAccessTokenAsync();
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var filter = string.IsNullOrEmpty(searchTerm) 
                ? "" 
                : $"&$filter=startswith(displayName,'{searchTerm}') or startswith(mail,'{searchTerm}')";
            
            var url = $"https://graph.microsoft.com/v1.0/users?$top={maxResults}{filter}&$select=displayName,mail,jobTitle,department";
            
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var graphResponse = JsonSerializer.Deserialize<GraphUserResponse>(content);
            
            return graphResponse?.Value?.Select(u => new StaffMember
            {
                DisplayName = u.DisplayName ?? "",
                Email = u.Mail ?? "",
                JobTitle = u.JobTitle,
                Department = u.Department
            }).ToList() ?? new List<StaffMember>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Microsoft Graph API");
            return new List<StaffMember>();
        }
    }
    */
}

