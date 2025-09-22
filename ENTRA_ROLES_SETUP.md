# Entra ID Group Roles Setup Guide

This guide explains how to configure Microsoft Entra ID (Azure AD) group roles for the FIPS Reporting Platform.

## Overview

The FIPS Reporting Platform uses a hierarchical role-based access control (RBAC) system that maps Azure AD security groups to application roles. This provides centralized user management through Azure AD while maintaining fine-grained access control within the application.

## Role Hierarchy

The system uses three main roles in hierarchical order:

1. **`Admin`** - Full administrative access to all features
2. **`Central Operations`** - Administrative access to reporting system
3. **`reporting_user`** - Standard reporting user access

## Step 1: Azure AD App Registration Configuration

### A. Configure Token Claims

1. **Navigate to Azure Portal**:
   - Go to **Azure Active Directory** → **App registrations**
   - Find your app: `31ff7feb-28d4-450b-9eef-0d79bb8edb1f`

2. **Add Groups Claim**:
   - Go to **Token configuration** → **Add groups claim**
   - Select:
     - **Token types**: `ID`, `Access`, `SAML`
     - **Group types**: `Security groups`
     - **Group ID**: `Group ID` (not group name)
     - **Optional claims**: Add `groups` claim

3. **Configure Optional Claims**:
   - Add `roles` claim if you want direct role assignment
   - Add `email` claim for user identification

### B. Create Security Groups

Create these security groups in Azure AD:

| Group Name | Application Role | Description |
|------------|------------------|-------------|
| `FIPS-Admin` | `Admin` | Full administrative access |
| `FIPS-Central-Operations` | `Central Operations` | Administrative access to reporting |
| `FIPS-Reporting-Users` | `reporting_user` | Standard reporting user access |

### C. Assign Users to Groups

1. **Add Users to Groups**:
   - Go to **Azure Active Directory** → **Groups**
   - Select each group and add appropriate users
   - Ensure users have proper licenses for group membership

2. **Group Membership Types**:
   - **Assigned**: Manually assigned users
   - **Dynamic**: Automatically assigned based on user attributes

## Step 2: Application Configuration

### A. Role Mapping Configuration

The application maps Azure AD groups to application roles via `appsettings.json`:

```json
{
  "AzureAd": {
    "RoleMapping": {
      "FIPS-Admin": "Admin",
      "FIPS-Central-Operations": "Central Operations",
      "FIPS-Reporting-Users": "reporting_user"
    }
  }
}
```

### B. Claims Transformation

The application uses a `ClaimsTransformationService` to:
1. Extract group claims from Azure AD tokens
2. Map group IDs to application roles
3. Add role claims to the user's identity

## Step 3: Testing Role Assignment

### A. Debug Endpoint

Use the debug endpoint to verify role assignment:

```
GET /debug/claims
```

This returns JSON with:
- User authentication status
- All claims from Azure AD
- Group memberships
- Mapped application roles
- Role check results

### B. Expected Output

```json
{
  "userName": "user@domain.com",
  "email": "user@domain.com",
  "isAuthenticated": true,
  "authenticationType": "OpenIdConnect",
  "claims": [...],
  "roles": ["Admin", "Central Operations"],
  "groups": ["group-id-1", "group-id-2"],
  "directRoles": [],
  "isAdmin": true,
  "isCentralOperations": true,
  "isReportingUser": true
}
```

## Step 4: Authorization Policies

The application defines authorization policies in `Program.cs`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Central Operations", "Admin"));
    
    options.AddPolicy("ReportingUser", policy =>
        policy.RequireRole("reporting_user", "Central Operations", "Admin"));
});
```

### Policy Usage

- **`[Authorize(Policy = "AdminOnly")]`** - Admin controllers
- **`[Authorize(Policy = "ReportingUser")]`** - Reporting user controllers

## Step 5: View-Level Role Checking

### A. Navigation Menu

```html
@if (User.IsInRole("Central Operations") || User.IsInRole("Admin"))
{
    <!-- Admin-only navigation items -->
}

@if (User.IsInRole("reporting_user") || User.IsInRole("Central Operations") || User.IsInRole("Admin"))
{
    <!-- Reporting user navigation items -->
}
```

### B. Controller Methods

```csharp
protected bool IsAdmin()
{
    return User.IsInRole("Central Operations") || User.IsInRole("Admin");
}

protected bool IsReportingUser()
{
    return User.IsInRole("reporting_user") || IsAdmin();
}
```

## Troubleshooting

### Common Issues

1. **No Roles Assigned**:
   - Check Azure AD group membership
   - Verify token configuration includes groups claim
   - Check application logs for claims transformation errors

2. **Wrong Role Mapping**:
   - Verify `RoleMapping` configuration in `appsettings.json`
   - Check group IDs match Azure AD group IDs
   - Use debug endpoint to verify group claims

3. **Authentication Failures**:
   - Verify Azure AD app registration configuration
   - Check tenant ID and client ID
   - Ensure proper redirect URIs

### Debugging Steps

1. **Check Application Logs**:
   ```bash
   dotnet run --environment Development
   ```
   Look for claims transformation log messages.

2. **Use Debug Endpoint**:
   Visit `/debug/claims` to see all user claims and role mappings.

3. **Verify Azure AD Configuration**:
   - Check app registration settings
   - Verify group membership
   - Test token generation

## Security Considerations

### Best Practices

1. **Principle of Least Privilege**:
   - Assign users to the minimum required role
   - Use hierarchical role inheritance

2. **Regular Audits**:
   - Review group memberships regularly
   - Monitor role assignments
   - Check for unused accounts

3. **Secure Configuration**:
   - Store sensitive configuration in Azure Key Vault
   - Use managed identities where possible
   - Enable conditional access policies

### Group Management

1. **Naming Conventions**:
   - Use consistent group naming (e.g., `FIPS-{Role}`)
   - Document group purposes
   - Maintain group descriptions

2. **Membership Management**:
   - Use dynamic groups for automatic assignment
   - Implement approval workflows for sensitive roles
   - Regular membership reviews

## Production Deployment

### Azure App Service Configuration

1. **Application Settings**:
   ```json
   {
     "AzureAd:TenantId": "your-tenant-id",
     "AzureAd:ClientId": "your-client-id",
     "AzureAd:ClientSecret": "your-client-secret",
     "AzureAd:RoleMapping:FIPS-Admin": "Admin",
     "AzureAd:RoleMapping:FIPS-Central-Operations": "Central Operations",
     "AzureAd:RoleMapping:FIPS-Reporting-Users": "reporting_user"
   }
   ```

2. **Managed Identity**:
   - Use managed identity instead of client secrets
   - Grant necessary permissions to the managed identity

3. **Monitoring**:
   - Enable Application Insights
   - Monitor authentication failures
   - Track role assignment changes

## Support

For issues with role assignment:

1. Check application logs for claims transformation errors
2. Use the debug endpoint to verify role mapping
3. Verify Azure AD group membership
4. Check Azure AD app registration configuration

## Related Documentation

- [Azure AD App Registration Guide](https://docs.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app)
- [Azure AD Groups Management](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-groups-create-azure-portal)
- [ASP.NET Core Authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/)
