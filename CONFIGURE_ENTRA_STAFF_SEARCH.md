# Configure Entra ID staff search

## ✅ What's already done

- ✅ Microsoft Graph SDK installed
- ✅ GraphService updated to query Entra ID
- ✅ Autocomplete working
- ✅ Automatic fallback to mock data if not configured

## 🔧 Configuration steps

### Step 1: Azure AD App permissions

1. **Go to Azure Portal** → Azure Active Directory → App registrations
2. **Find your Compass app** (or the app used for authentication)
3. **Click "API permissions"** in the left menu
4. **Click "Add a permission"**
5. **Select "Microsoft Graph"**
6. **Select "Application permissions"** (not Delegated)
7. **Search for and select:** `User.Read.All`
8. **Click "Add permissions"**
9. **IMPORTANT:** Click "Grant admin consent for [Your Organization]"
   - You need Global Admin or Application Admin role for this
   - The button should show a green checkmark after clicking

### Step 2: Create client secret

1. **In your app registration**, click "Certificates & secrets"
2. **Click "New client secret"**
3. **Add a description:** "Compass Staff Search"
4. **Select expiration:** Choose appropriate expiry (e.g., 6 months, 1 year)
5. **Click "Add"**
6. **⚠️ IMPORTANT:** Copy the **Value** immediately
   - You can only see it once!
   - Save it securely (will add to appsettings next)

### Step 3: Get your Azure AD details

You need three values from Azure Portal:

1. **Tenant ID:**
   - Azure AD → Overview → "Tenant ID"
   - Example: `12345678-1234-1234-1234-123456789012`

2. **Client ID (Application ID):**
   - App registrations → Your app → Overview → "Application (client) ID"
   - Example: `87654321-4321-4321-4321-210987654321`

3. **Client Secret:**
   - The value you just copied in Step 2

### Step 4: Update appsettings.json

**Development (local):**

Edit `/Users/andyjones/Source/code-digital-ops/FIPS/compass/appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "education.gov.uk",
    "TenantId": "YOUR-TENANT-ID-HERE",
    "ClientId": "YOUR-CLIENT-ID-HERE",
    "ClientSecret": "YOUR-CLIENT-SECRET-HERE",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**⚠️ Security notes:**
- Add `appsettings.json` to `.gitignore` (should already be there)
- Never commit secrets to Git
- Use environment variables or Azure Key Vault in production

### Step 5: Restart the application

```bash
# Stop current process (Ctrl+C)
dotnet run
```

## ✅ Testing

### Check logs on startup

You should see in the console:
```
Microsoft Graph API configured successfully
```

If you see:
```
Microsoft Graph API credentials not configured. Will use mock data.
```
Then check your appsettings.json values.

### Test the autocomplete

1. Navigate to any RAID form (e.g., Risk → Create)
2. Type in the Owner field
3. You should now see **real Entra ID users** from your tenant!

### Test the API directly

```bash
curl http://localhost:5000/api/staff/search?q=john
```

Should return real users from your Entra tenant.

## 🔍 Troubleshooting

### Error: "Insufficient privileges to complete the operation"

**Solution:**
- Grant admin consent in Azure Portal
- Ensure `User.Read.All` is Application permission (not Delegated)
- Click the "Grant admin consent" button

### Error: "AADSTS7000215: Invalid client secret"

**Solution:**
- The client secret is wrong or expired
- Create a new client secret in Azure Portal
- Update appsettings.json with the new value

### Error: "AADSTS700016: Application not found"

**Solution:**
- The ClientId is incorrect
- Double-check the Application (client) ID in Azure Portal

### Still seeing mock data

**Check:**
1. **Console logs** - Should say "Microsoft Graph API configured successfully"
2. **All three values** in appsettings.json are correct (TenantId, ClientId, ClientSecret)
3. **Admin consent** has been granted
4. **Restart** the application after making changes

### Test if Graph API is accessible

Run this test query:
```bash
# Using Azure CLI (if installed)
az login
az ad user list --filter "startswith(displayName,'John')" --query "[].{name:displayName,email:mail}" --output table
```

## 📊 What it queries

The Graph API searches:
- **Display name** - Full name
- **Given name** - First name  
- **Surname** - Last name
- **Mail** - Email address
- **UserPrincipalName** - UPN (username@domain)

Returns:
- Display name
- Email
- Job title
- Department

## 🔒 Security & permissions

### What `User.Read.All` allows:

✅ **Can:**
- Read user profile information
- Search all users in the tenant
- Get user display names and emails

❌ **Cannot:**
- Modify user accounts
- Access sensitive data
- Read passwords or credentials
- Perform admin operations

### Best practices:

1. **Use Azure Key Vault** in production to store client secret
2. **Rotate secrets regularly** (every 6-12 months)
3. **Monitor API usage** in Azure Portal
4. **Set up alerts** for suspicious activity
5. **Review permissions** periodically

## 🚀 Production deployment

For production (Azure App Service):

### Option 1: Environment variables

Set in Azure Portal → App Service → Configuration → Application settings:

```
AzureAd__ClientSecret = YOUR_SECRET_HERE
```

### Option 2: Azure Key Vault (Recommended)

1. Create Key Vault
2. Add secret: `AzureAd-ClientSecret`
3. Grant App Service managed identity access
4. Reference in appsettings.json:

```json
{
  "AzureAd": {
    "ClientSecret": "@Microsoft.KeyVault(SecretUri=https://yourkeyvault.vault.azure.net/secrets/AzureAd-ClientSecret/)"
  }
}
```

## 📝 Current behavior

### Without configuration:
- Shows 10 mock staff members
- Logs warning: "Using mock data"
- Autocomplete works but users won't be in database

### With configuration:
- Searches real Entra ID tenant
- Returns actual staff members
- Logs info: "Searching Entra ID for: {term}"
- Falls back to mock data if API fails

## ✨ Features

- **Real-time search** as you type
- **Intelligent matching** across multiple fields
- **Automatic fallback** if API unavailable
- **Caching** for performance
- **Detailed logging** for debugging
- **Graceful error handling**

## 🎯 Next steps

After configuration:

1. ✅ Test with a few sample searches
2. ✅ Verify real users appear in autocomplete
3. ✅ Create a Risk/Issue and assign to a real user
4. ✅ Check application logs for any errors
5. ✅ Set up Azure Key Vault for production
6. ✅ Configure production environment variables
7. ✅ Test in production environment

---

## Quick checklist

□ Azure AD app registration exists  
□ `User.Read.All` permission added  
□ `User.Read.All` admin consent granted  
□ Client secret created and copied  
□ Tenant ID added to appsettings.json  
□ Client ID added to appsettings.json  
□ Client secret added to appsettings.json  
□ Application restarted  
□ Console shows "Graph API configured successfully"  
□ Autocomplete shows real Entra users  
□ Production uses Key Vault or environment variables  

**Status:** Ready to configure! Just add the three values to appsettings.json and restart.

