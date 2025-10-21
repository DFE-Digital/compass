# Setting up Entra ID staff autocomplete

## Current state
✅ Autocomplete working with mock data  
❌ Not connected to real Entra ID staff  

## Option 1: Quick fix - Use existing Entra auth users

Since you're already using Entra ID authentication, users are created when they log in. The autocomplete will automatically find them.

**To populate users:**
1. Have staff members log in to Compass
2. Their user records will be created automatically
3. Autocomplete will then find them

## Option 2: Connect to Microsoft Graph API (Full solution)

This allows searching ALL Entra ID users, not just those who have logged in.

### Prerequisites
- Azure AD app registration (you already have this)
- Application permission: `User.Read.All`
- Admin consent granted

### Steps

#### 1. Update Azure AD app permissions

Go to: Azure Portal → App Registrations → Your Compass App → API Permissions

Add permission:
- **API**: Microsoft Graph
- **Type**: Application permissions
- **Permission**: `User.Read.All`
- Click "Grant admin consent"

#### 2. Create client secret

Go to: Certificates & secrets → New client secret

**Important**: Copy the secret value immediately (you can't see it again)

#### 3. Update appsettings.json

Add the client secret:

```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "Domain": "education.gov.uk",
  "TenantId": "YOUR_TENANT_ID",
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET_HERE",  // ← ADD THIS
  "CallbackPath": "/signin-oidc",
  "SignedOutCallbackPath": "/signout-callback-oidc"
}
```

#### 4. Install Microsoft Graph SDK

```bash
cd /Users/andyjones/Source/code-digital-ops/FIPS/compass
dotnet add package Microsoft.Graph
dotnet add package Microsoft.Identity.Client
dotnet add package Azure.Identity
```

#### 5. Update GraphService.cs

Replace the mock implementation with real Graph API calls:

```csharp
using Microsoft.Graph;
using Azure.Identity;

public class GraphService : IGraphService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphService> _logger;
    private readonly GraphServiceClient _graphClient;

    public GraphService(IConfiguration configuration, ILogger<GraphService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Initialize Graph client
        var tenantId = configuration["AzureAd:TenantId"];
        var clientId = configuration["AzureAd:ClientId"];
        var clientSecret = configuration["AzureAd:ClientSecret"];

        var clientSecretCredential = new ClientSecretCredential(
            tenantId, clientId, clientSecret);

        _graphClient = new GraphServiceClient(clientSecretCredential);
    }

    public async Task<List<StaffMember>> SearchStaffAsync(string searchTerm, int maxResults = 20)
    {
        try
        {
            var users = await _graphClient.Users
                .Request()
                .Filter($"startswith(displayName,'{searchTerm}') or startswith(mail,'{searchTerm}')")
                .Select(u => new { u.DisplayName, u.Mail, u.JobTitle, u.Department })
                .Top(maxResults)
                .GetAsync();

            return users.Select(u => new StaffMember
            {
                DisplayName = u.DisplayName ?? "",
                Email = u.Mail ?? "",
                JobTitle = u.JobTitle,
                Department = u.Department
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Microsoft Graph API");
            // Fallback to mock data
            return GetMockStaffData(searchTerm, maxResults);
        }
    }
}
```

## Option 3: Hybrid approach (Recommended for now)

Keep the mock data for development, but also search existing logged-in users:

### Update GraphService to search database first

```csharp
public async Task<List<StaffMember>> SearchStaffAsync(string searchTerm, int maxResults = 20)
{
    // Try database first
    var dbUsers = await _context.Users
        .Where(u => u.Name.Contains(searchTerm) || u.Email.Contains(searchTerm))
        .Take(maxResults)
        .ToListAsync();

    if (dbUsers.Any())
    {
        return dbUsers.Select(u => new StaffMember
        {
            DisplayName = u.Name,
            Email = u.Email,
            JobTitle = null,
            Department = null
        }).ToList();
    }

    // Fallback to mock data
    return GetMockStaffData(searchTerm, maxResults);
}
```

## Testing

### Test with mock data
```bash
curl http://localhost:5000/api/staff/search?q=andy
```

Should return:
```json
{
  "results": [
    {
      "id": 0,
      "displayName": "Andy Anderson",
      "email": "andy.anderson@education.gov.uk",
      "jobTitle": "Infrastructure Lead"
    }
  ]
}
```

### Test after Graph API configured
Same endpoint should return real Entra users.

## Current mock users

The system includes 10 mock staff members:
1. Andy Jones - Technical Architect
2. Sarah Williams - Product Manager
3. John Smith - Delivery Manager
4. Emma Brown - Developer
5. Michael Davis - Security Lead
6. Lisa Johnson - Business Analyst
7. David Wilson - Service Owner
8. Sophie Taylor - UX Designer
9. James Anderson - Infrastructure Lead
10. Rachel Thomas - Content Designer

## Security notes

⚠️ **Never commit client secrets to git**

Use:
- Azure Key Vault (production)
- User secrets (development): `dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_SECRET"`
- Environment variables

## Troubleshooting

### "Insufficient privileges"
- Grant admin consent in Azure AD
- Ensure `User.Read.All` application permission

### "Invalid client secret"
- Regenerate secret in Azure AD
- Update configuration

### "403 Forbidden"
- Check app has correct permissions
- Verify admin consent granted

## Production deployment

For production, ensure:
- [ ] Client secret stored in Azure Key Vault
- [ ] Application permissions granted
- [ ] Admin consent provided
- [ ] TenantId, ClientId configured
- [ ] Graph API tested and working

## Current status

✅ Autocomplete UI working  
✅ API endpoint working  
✅ Mock data returning  
⏳ Entra ID integration pending  
⏳ Graph API credentials needed  

---

**Next step**: Choose Option 1 (quick - let users log in) or Option 2 (full Graph API integration)

