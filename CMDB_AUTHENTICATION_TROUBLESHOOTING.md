# CMDB Authentication Troubleshooting

## Issue: 403 Forbidden from ServiceNow CMDB

You're getting a `403 Forbidden` error when trying to access the ServiceNow CMDB API. This means the authentication is failing.

## Current Configuration

From your `appsettings.json`:

```json
"FipsSync": {
  "Cmdb": {
    "Endpoint": "https://dfe.service-now.com/api/now/table/service_offering",
    "Username": "POWERBI-Arch",
    "Password": ">x2@@qnB7idQR=$t*o&p1}BqDouaWx()kGBSxm57"
  }
}
```

## Possible Causes

### 1. Incorrect Credentials ⚠️

**Most likely issue:** The password might be incorrect or expired.

**To verify:**
- Test the credentials directly in Postman or curl:

```bash
curl -u "POWERBI-Arch:>x2@@qnB7idQR=\$t*o&p1}BqDouaWx()kGBSxm57" \
  "https://dfe.service-now.com/api/now/table/service_offering?sysparm_limit=1"
```

**Note:** The `$` character might need escaping in bash as `\$`

### 2. Account Permissions

The `POWERBI-Arch` account might not have permission to:
- Access the REST API
- Read the `service_offering` table
- Query with the `sysparm_query` parameter

**To verify:**
- Contact your ServiceNow administrator
- Check the account's roles and permissions
- Common required roles:
  - `rest_api_explorer` 
  - `web_service_admin`
  - Read access to `service_offering` table

### 3. IP Whitelisting

ServiceNow might be configured to only allow API access from specific IP addresses.

**To verify:**
- Check if you can access the API from your local machine
- If local works but COMPASS doesn't, you need to whitelist the COMPASS server IP
- Contact ServiceNow admin to add COMPASS server IP to whitelist

### 4. Special Characters in Password

The password contains special characters: `>x2@@qnB7idQR=$t*o&p1}BqDouaWx()kGBSxm57`

Special characters that might cause issues: `@`, `$`, `}`, `{`, `(`, `)`

**To verify:**
- If possible, temporarily change the password to something simple (no special chars)
- Test if it works
- If it does, the issue is password encoding

### 5. Account Locked or Disabled

The account might be:
- Locked due to too many failed login attempts
- Disabled by an administrator
- Expired (if it has an expiration date)

**To verify:**
- Try logging into ServiceNow web UI with these credentials
- Contact ServiceNow admin to check account status

## Debugging Steps

### Step 1: Check Logs

Run the sync again and check the application logs. You should now see:

```
Fetching CMDB entries from ServiceNow...
CMDB Endpoint: https://dfe.service-now.com/api/now/table/service_offering
CMDB Username: POWERBI-Arch
Request URI: https://dfe.service-now.com/api/now/table/service_offering?sysparm_query=...
CMDB API Error: Status 403, Content: {error details}
```

The error content will give you more details about why it's failing.

### Step 2: Test with Curl

```bash
# Test basic auth
curl -v -u "POWERBI-Arch:PASSWORD_HERE" \
  "https://dfe.service-now.com/api/now/table/service_offering?sysparm_limit=1"

# Expected: 200 OK with JSON response
# If 403: Check credentials and permissions
# If 401: Wrong password
# If 404: Wrong endpoint
```

### Step 3: Test with Postman

1. Open Postman
2. Create new GET request: `https://dfe.service-now.com/api/now/table/service_offering`
3. Add query parameter: `sysparm_limit=1`
4. Go to Authorization tab
5. Select "Basic Auth"
6. Enter username: `POWERBI-Arch`
7. Enter password: `>x2@@qnB7idQR=$t*o&p1}BqDouaWx()kGBSxm57`
8. Send request

**Expected:** 200 OK
**If 403:** Credentials or permissions issue

### Step 4: Check ServiceNow Logs

If you have admin access to ServiceNow:
1. Go to System Logs → System Log → REST
2. Look for failed authentication attempts
3. Check the error message

### Step 5: Contact ServiceNow Admin

If none of the above works, contact your ServiceNow administrator with:
- Account name: `POWERBI-Arch`
- Error: `403 Forbidden`
- Endpoint: `/api/now/table/service_offering`
- Ask them to check:
  - Is the account active?
  - Does it have REST API access?
  - Does it have read access to `service_offering` table?
  - Are there IP restrictions?

## Temporary Workaround

While you resolve the CMDB authentication, you can:

1. **Use the Node.js sync-app** (if it was working before):
   ```bash
   cd /path/to/sync-app
   node app.js
   ```

2. **Skip CMDB sync** and test Strapi-to-Strapi sync instead (once implemented)

3. **Test with a different account** if you have one available

## Quick Fixes to Try

### Fix 1: URL Encode the Password

Try URL-encoding the password in appsettings.json:

```json
"Password": "%3Ex2%40%40qnB7idQR%3D%24t*o%26p1%7DBqDouaWx%28%29kGBSxm57"
```

### Fix 2: Use Environment Variables

Instead of appsettings.json, set environment variables:

```bash
export FipsSync__Cmdb__Password=">x2@@qnB7idQR=$t*o&p1}BqDouaWx()kGBSxm57"
```

### Fix 3: Escape Special Characters

In appsettings.json, try escaping special characters:

```json
"Password": ">x2@@qnB7idQR=\\$t*o&p1}BqDouaWx()kGBSxm57"
```

### Fix 4: Request New Credentials

Ask your ServiceNow admin to:
1. Reset the password for `POWERBI-Arch`
2. Use a simpler password without special characters
3. Verify the account has correct permissions
4. Provide you with the new credentials

## How to Update Credentials

Once you have new/corrected credentials:

1. **Update appsettings.json:**
   ```json
   "Cmdb": {
     "Username": "NEW_USERNAME",
     "Password": "NEW_PASSWORD"
   }
   ```

2. **Restart COMPASS:**
   ```bash
   # Stop the application
   # Start again
   dotnet run
   ```

3. **Test the sync:**
   - Go to Admin → FIPS Sync
   - Click "Check & Confirm"
   - Verify the endpoint and username are correct
   - Click "Confirm & Run Sync"

## Success Indicators

Once authentication is working, you should see:

```
Fetching CMDB entries from ServiceNow...
Successfully fetched 123 CMDB entries
```

Instead of:

```
CMDB API Error: Status 403
```

## Need More Help?

### Check Application Logs

```bash
# View recent logs
tail -f /path/to/logs

# Or check console output when running
dotnet run
```

### Check Sync History

In COMPASS:
1. Go to Admin → FIPS Sync
2. Click on the failed sync
3. View "Error Details" section

### Run with Verbose Logging

Update `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Compass.Services.Fips": "Debug"
  }
}
```

## Summary

**Most likely issue:** Wrong password or account lacks permissions.

**Next steps:**
1. Test credentials with curl/Postman
2. Check with ServiceNow admin about account status and permissions
3. Try the Quick Fixes above
4. Update credentials in appsettings.json
5. Restart and test

**If still stuck:** Contact your ServiceNow administrator with this error message and ask them to verify the account has REST API access to the `service_offering` table.

---

**Related Documentation:**
- `FIPS_SYNC_FINAL_SETUP.md` - Complete setup guide
- ServiceNow REST API documentation: https://docs.servicenow.com/
