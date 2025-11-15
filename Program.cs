using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Azure.Identity;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Compass.Services;
using Compass.Data;
using Compass.Middlewares;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using System.IO;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Check for database cleanup command
if (args.Length > 0 && args[0] == "--clean-database")
{
    var cleanConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(cleanConnectionString))
    {
        Console.WriteLine("Error: DefaultConnection string not found in configuration.");
        return;
    }
    await Compass.CleanDatabase.RunAsync(cleanConnectionString);
    return;
}

// Check for data seeding command
if (args.Length > 0 && args[0] == "--seed-from-sqlite")
{
    var environment = args.Length > 1 && args[1] == "--environment" && args.Length > 2 
        ? args[2] 
        : "Development";
    await Compass.SeedFromSQLite.RunAsync(environment);
    return;
}

// Check for GDD Framework seeding command
if (args.Length > 0 && args[0] == "--seed-gdd-framework")
{
    var environment = "Development";
    string? csvFilePath = null;

    for (int i = 1; i < args.Length - 1; i++)
    {
        if (args[i] == "--environment") environment = args[i + 1];
        if (args[i] == "--csv-file") csvFilePath = args[i + 1];
    }

    await Compass.SeedGddFramework.RunAsync(environment, csvFilePath);
    return;
}

// Check for query GDD roles command
if (args.Length > 0 && args[0] == "--query-gdd-roles")
{
    var environment = args.Length > 2 && args[1] == "--environment" ? args[2] : "Development";
    
    var builder2 = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
    builder2.AddJsonFile("appsettings.json", optional: false);
    builder2.AddJsonFile($"appsettings.{environment}.json", optional: true);
    var config = builder2.Build();
    
    var options = new DbContextOptionsBuilder<CompassDbContext>()
        .UseSqlServer(config.GetConnectionString("DefaultConnection"))
        .Options;
    using var db = new CompassDbContext(options);
    
    var roles = await db.GddRoles.OrderBy(r => r.RoleFamily).ThenBy(r => r.RoleName).ThenBy(r => r.RoleLevel).ToListAsync();
    Console.WriteLine($"Total GDD Roles: {roles.Count}\n");
    
    Console.WriteLine("By Role Family:\n");
    foreach (var family in roles.GroupBy(r => r.RoleFamily))
    {
        Console.WriteLine($"{family.Key}: {family.Count()} roles");
        foreach (var role in family.Take(8))
        {
            Console.WriteLine($"  - {role.DisplayName}");
        }
        if (family.Count() > 8) Console.WriteLine($"  ... and {family.Count() - 8} more");
        Console.WriteLine();
    }
    
    Console.WriteLine("\nUnique Role Levels:");
    var uniqueLevels = roles.Select(r => r.RoleLevel).Where(r => !string.IsNullOrEmpty(r)).Distinct().OrderBy(l => l).ToList();
    Console.WriteLine($"Total unique levels: {uniqueLevels.Count}");
    foreach (var level in uniqueLevels.Take(40))
    {
        Console.WriteLine($"  - {level}");
    }
    
    return;
}

// Check for query Skills command
if (args.Length > 0 && args[0] == "--query-skills")
{
    var environment = args.Length > 2 && args[1] == "--environment" ? args[2] : "Development";
    
    var builder2 = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
    builder2.AddJsonFile("appsettings.json", optional: false);
    builder2.AddJsonFile($"appsettings.{environment}.json", optional: true);
    var config = builder2.Build();
    
    var options = new DbContextOptionsBuilder<CompassDbContext>()
        .UseSqlServer(config.GetConnectionString("DefaultConnection"))
        .Options;
    using var db = new CompassDbContext(options);
    
    var skills = await db.Skills.OrderBy(s => s.SkillName).ToListAsync();
    Console.WriteLine($"Total Skills: {skills.Count}\n");
    
    Console.WriteLine("First 20 Skills:\n");
    foreach (var skill in skills.Take(20))
    {
        Console.WriteLine($"  - {skill.SkillName}");
    }
    
    return;
}

// Check for data migration command
if (args.Length > 0 && args[0] == "--migrate-data")
{
    await RunDataMigration(builder);
    return;
}

// Check for Azure SQL → Azure SQL environment migration
if (args.Length > 0 && args[0] == "--migrate-sql")
{
    // Usage: --migrate-sql --source Development --target Test|Production
    string source = "Development";
    string target = "Production";
    bool referenceOnly = false;

    for (int i = 1; i < args.Length - 1; i++)
    {
        if (args[i] == "--source") source = args[i + 1];
        if (args[i] == "--target") target = args[i + 1];
        if (args[i] == "--reference-only") referenceOnly = true;
    }

    await Compass.AzureSqlEnvironmentMigration.RunAsync(source, target, referenceOnly);
    return;
}

// Add file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("logs/compass-{Date}.log");

// Add services to the container
builder.Services.AddRazorPages();

// Configure authentication with Entra ID
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Add Controllers with Views
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization();

// Microsoft Graph app-to-app client using Entra configuration
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var tenantId = configuration["Entra:TenantId"];
    var clientId = configuration["Entra:ClientId"];
    var clientSecret = configuration["Entra:ClientSecret"];

    if (string.IsNullOrWhiteSpace(tenantId) ||
        string.IsNullOrWhiteSpace(clientId) ||
        string.IsNullOrWhiteSpace(clientSecret))
    {
        throw new InvalidOperationException("Entra configuration is missing TenantId, ClientId, or ClientSecret.");
    }

    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
    return new Microsoft.Graph.GraphServiceClient(credential);
});

// Configure database - Use Azure SQL for all environments
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Diagnostic logging to help debug connection string issues
var currentEnvironment = builder.Environment.EnvironmentName;
Console.WriteLine($"\n[CONFIG DEBUG] ASPNETCORE_ENVIRONMENT = {currentEnvironment}");
Console.WriteLine($"[CONFIG DEBUG] Expected files: appsettings.json, appsettings.{currentEnvironment}.json");

// Show which connection string values exist in each source (for debugging)
var baseConnection = builder.Configuration["ConnectionStrings:DefaultConnection"];
Console.WriteLine($"[CONFIG DEBUG] Resolved ConnectionStrings:DefaultConnection exists: {!string.IsNullOrEmpty(baseConnection)}");

if (connectionString != null)
{
    var catalogMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Initial Catalog=([^;]+)");
    if (catalogMatch.Success)
    {
        Console.WriteLine($"[CONFIG DEBUG] ✓ Database catalog: {catalogMatch.Groups[1].Value}");
    }
    Console.WriteLine($"[CONFIG DEBUG] Connection string preview: {connectionString.Substring(0, Math.Min(80, connectionString.Length))}...\n");
}

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<CompassDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlServerOptions.CommandTimeout(60);
        sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

// Register HTTP clients for API services
builder.Services.AddHttpClient<ICmsApiService, CmsApiService>();
builder.Services.AddHttpClient<IStandardsCmsApiService, StandardsCmsApiService>();
builder.Services.AddHttpClient<IProductsApiService, ProductsApiService>(client =>
{
    var baseUrl = builder.Configuration["CmsApi:BaseUrl"];
    var apiKey = builder.Configuration["CmsApi:ReadApiKey"];
    
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
});

// Register HttpClient for GovernmentDepartmentController
builder.Services.AddHttpClient<Compass.Controllers.GovernmentDepartmentController>();

// Register services
builder.Services.AddScoped<IReturnStatusService, ReturnStatusService>();
builder.Services.AddScoped<IPerformanceReportingEligibilityService, PerformanceReportingEligibilityService>();
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<IApiTokenService, ApiTokenService>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();
builder.Services.AddScoped<IAuditContextProvider, HttpAuditContextProvider>();

// Register HttpClientFactory for PerformanceReportingManagementController
builder.Services.AddHttpClient();

builder.Services.AddHttpContextAccessor();

// Add memory caching
builder.Services.AddMemoryCache(options =>
{
    options.CompactionPercentage = builder.Configuration.GetValue<double>("Caching:MemoryCache:CompactionPercentage", 0.25);
});

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "Compass.Session";
});

// Respect proxy forwarded headers so RemoteIpAddress reflects the client
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetGlobalPartitionKey(httpContext),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // More conservative named policies for surveys endpoints (per IP)
    options.AddPolicy("ResponsesPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetFipsPartitionKey(httpContext) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("SurveysGetPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetFipsPartitionKey(httpContext) ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";
        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra) ? ra : TimeSpan.FromSeconds(60);
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            error = "rate_limited",
            message = "Too many requests. Try again later.",
            retryAfterSeconds = (int)retryAfter.TotalSeconds
        });
        await context.HttpContext.Response.WriteAsync(payload, token);
    };
});

var app = builder.Build();

// Configure port for local development
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://localhost:5500");
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Configure HTTPS redirection
// Skip in development since we're running on HTTP only (localhost:5500)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

// Add security headers
app.Use(async (context, next) =>
{
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
    }
    else
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=300; includeSubDomains";
    }
    
    var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    context.Items["Nonce"] = nonce;
    
    context.Response.Headers["Content-Security-Policy"] = 
        $"default-src 'self'; " +
        $"script-src 'self' 'nonce-{nonce}' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net; " +
        $"style-src 'self' 'unsafe-inline' https://rsms.me https://fonts.googleapis.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://cdn.datatables.net; " +
        $"img-src 'self' data: https:; " +
        $"font-src 'self' data: https://rsms.me https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        $"connect-src 'self' https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
        $"frame-ancestors 'none'; " +
        $"base-uri 'self'; " +
        $"form-action 'self'; " +
        $"object-src 'none'; " +
        $"upgrade-insecure-requests";
    
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    await next();
});

app.UseRouting();

// API token authentication and logging (must be before UseAuthentication for API routes)
app.UseMiddleware<ApiAuthenticationMiddleware>();
app.UseMiddleware<ApiLoggingMiddleware>();

app.UseAuthentication();
app.UseMiddleware<EnsureCompassUserMiddleware>();
app.UseAuthorization();

app.UseSession();
app.UseRateLimiter();

// Map controllers with attribute routing (must be called to enable attribute routing)
app.MapControllers();

// API routes (conventional routing for non-attribute controllers)
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action=Index}/{id?}");

// Default routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CompassDbContext>();
    
    try
    {
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        
        // Seed statement templates if they don't exist
        await SeedStatementTemplatesAsync(context);
        
        // Seed RBAC initial data (groups, features, super admin)
        await SeedRbacInitialDataAsync(context);
        
        Console.WriteLine("Compass database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Compass database initialization error: {ex.Message}");
        throw;
    }
}

app.Run();

static async Task SeedRbacInitialDataAsync(Compass.Data.CompassDbContext context)
{
    const string superAdminEmail = "andy.jones@education.gov.uk";
    const string centralOpsAdminGroupName = "Central Operations Admin";

    // Check if already seeded
    var centralOpsGroup = await context.Groups
        .FirstOrDefaultAsync(g => g.Name == centralOpsAdminGroupName);
    
    if (centralOpsGroup != null)
    {
        return; // Already seeded
    }

    Console.WriteLine("Seeding RBAC initial data...");

    // Create or get super admin user
    var superAdmin = await context.Users
        .FirstOrDefaultAsync(u => u.Email.ToLower() == superAdminEmail.ToLower());

    if (superAdmin == null)
    {
        superAdmin = new Compass.Models.User
        {
            Email = superAdminEmail,
            Name = "Andy Jones",
            Role = Compass.Models.UserRole.SuperAdmin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Users.Add(superAdmin);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created super admin user: {superAdminEmail}");
    }

    // Create Central Operations Admin group
    centralOpsGroup = new Compass.Models.Group
    {
        Name = centralOpsAdminGroupName,
        Description = "Default administrative group with full access to all features",
        IsActive = true,
        IsSystemGroup = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = "System",
        UpdatedBy = "System"
    };
    context.Groups.Add(centralOpsGroup);
    await context.SaveChangesAsync();
    Console.WriteLine($"✓ Created group: {centralOpsAdminGroupName}");

    // Create default features if they don't exist
    var defaultFeatures = new[]
    {
        new { Code = "delivery_reporting", Name = "Delivery Reporting", Description = "Delivery reporting functionality" },
        new { Code = "risks", Name = "Risks", Description = "Risk management functionality" },
        new { Code = "issues", Name = "Issues", Description = "Issue management functionality" },
        new { Code = "actions", Name = "Actions", Description = "Action management functionality" },
        new { Code = "objectives", Name = "Objectives", Description = "Objective management functionality" },
        new { Code = "projects", Name = "Projects", Description = "Project management functionality" },
        new { Code = "accessibility", Name = "Accessibility", Description = "Accessibility management functionality" },
        new { Code = "surveys", Name = "Surveys", Description = "Survey management functionality" },
        new { Code = "enterprise_reporting", Name = "Enterprise Reporting", Description = "Enterprise reporting functionality" },
        new { Code = "group_management", Name = "Group Management", Description = "Group and permission management functionality" }
    };

    foreach (var featureData in defaultFeatures)
    {
        var feature = await context.Features
            .FirstOrDefaultAsync(f => f.Code == featureData.Code);

        if (feature == null)
        {
            feature = new Compass.Models.Feature
            {
                Name = featureData.Name,
                Code = featureData.Code,
                Description = featureData.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Features.Add(feature);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Created feature: {featureData.Name}");
        }

        // Grant all permissions to Central Operations Admin group for this feature
        foreach (Compass.Models.PermissionType permission in Enum.GetValues<Compass.Models.PermissionType>())
        {
            // Check if permission already exists
            var exists = await context.GroupFeaturePermissions
                .AnyAsync(gfp => gfp.GroupId == centralOpsGroup.Id && 
                                gfp.FeatureId == feature.Id && 
                                gfp.Permission == permission);
            
            if (!exists)
            {
                var groupFeaturePermission = new Compass.Models.GroupFeaturePermission
                {
                    GroupId = centralOpsGroup.Id,
                    FeatureId = feature.Id,
                    Permission = permission,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                context.GroupFeaturePermissions.Add(groupFeaturePermission);
            }
        }
    }

    await context.SaveChangesAsync();

    // Assign super admin to Central Operations Admin group
    var userGroup = new Compass.Models.UserGroup
    {
        UserId = superAdmin.Id,
        GroupId = centralOpsGroup.Id,
        AssignedAt = DateTime.UtcNow,
        AssignedBy = "System"
    };
    context.UserGroups.Add(userGroup);
    await context.SaveChangesAsync();
    Console.WriteLine($"✓ Assigned super admin to {centralOpsAdminGroupName} group");
    Console.WriteLine("✓ RBAC initial data seeding completed");
}

static async Task SeedStatementTemplatesAsync(Compass.Data.CompassDbContext context)
{
    // Check if templates already exist
    if (await context.StatementTemplates.AnyAsync())
    {
        return; // Templates already seeded
    }

    // Get the markdown template files
    var docsPath = Path.Combine(Directory.GetCurrentDirectory(), "docs");
    var compliantPath = Path.Combine(docsPath, "statement.md");
    var nonCompliantPath = Path.Combine(docsPath, "non-compliant.md");

    // Seed compliant template
    if (File.Exists(compliantPath))
    {
        var compliantContent = await File.ReadAllTextAsync(compliantPath);
        var compliantTemplate = new Compass.Models.StatementTemplate
        {
            Name = "Compliant",
            Version = 1,
            Content = compliantContent,
            Description = "Accessibility statement template for fully compliant products",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "System"
        };
        context.StatementTemplates.Add(compliantTemplate);
    }

    // Seed non-compliant template (used for both partially-compliant and non-compliant)
    if (File.Exists(nonCompliantPath))
    {
        var nonCompliantContent = await File.ReadAllTextAsync(nonCompliantPath);
        var nonCompliantTemplate = new Compass.Models.StatementTemplate
        {
            Name = "Non-compliant",
            Version = 1,
            Content = nonCompliantContent,
            Description = "Accessibility statement template for partially-compliant and non-compliant products",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = "System"
        };
        context.StatementTemplates.Add(nonCompliantTemplate);
    }

    if (context.ChangeTracker.HasChanges())
    {
        await context.SaveChangesAsync();
        Console.WriteLine("✓ Statement templates seeded successfully");
    }
}

static async Task RunDataMigration(WebApplicationBuilder builder)
{
    Console.WriteLine("=== Compass Data Migration Utility ===\n");
    
    // Build minimal services needed for migration
    builder.Services.AddDbContext<CompassDbContext>(options =>
        options.UseSqlite("Data Source=compass.db"), ServiceLifetime.Transient);
    
    var sqlConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(sqlConnectionString))
    {
        Console.WriteLine("Error: DefaultConnection string not found in configuration.");
        Console.WriteLine("Please ensure appsettings.Production.json has the Azure SQL connection string.");
        return;
    }
    
    builder.Services.AddDbContext<CompassDbContext>(options =>
        options.UseSqlServer(sqlConnectionString), ServiceLifetime.Transient);
    
    var app = builder.Build();
    
    using var scope = app.Services.CreateScope();
    var serviceProvider = scope.ServiceProvider;
    
    // Create SQLite context (source)
    var sqliteOptions = new DbContextOptionsBuilder<CompassDbContext>()
        .UseSqlite("Data Source=compass.db")
        .Options;
    using var sourceDb = new CompassDbContext(sqliteOptions);
    
    // Create Azure SQL context (target)
    var sqlServerOptions = new DbContextOptionsBuilder<CompassDbContext>()
        .UseSqlServer(sqlConnectionString)
        .Options;
    using var targetDb = new CompassDbContext(sqlServerOptions);
    
    // Test connections
    Console.WriteLine("Testing database connections...");
    try
    {
        await sourceDb.Database.CanConnectAsync();
        Console.WriteLine("✓ Connected to SQLite source database");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Failed to connect to SQLite: {ex.Message}");
        return;
    }
    
    try
    {
        await targetDb.Database.CanConnectAsync();
        Console.WriteLine("✓ Connected to Azure SQL target database");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Failed to connect to Azure SQL: {ex.Message}");
        Console.WriteLine("Please verify the connection string and ensure the Azure SQL firewall allows your IP.");
        return;
    }
    
    Console.WriteLine("\nStarting migration...\n");
    await Compass.DataMigrationUtility.MigrateDataAsync(sourceDb, targetDb);
}

static string? GetFipsPartitionKey(HttpContext httpContext)
{
    try
    {
        // Prefer route value for GET /api/v1/surveys/{fipsId}
        if (httpContext.Request.RouteValues.TryGetValue("fipsId", out var routeVal))
        {
            var s = routeVal?.ToString();
            if (!string.IsNullOrWhiteSpace(s)) return $"fips:{s}";
        }
    }
    catch { }
    return null;
}

static string GetGlobalPartitionKey(HttpContext httpContext)
{
    var identityName = httpContext.User.Identity?.IsAuthenticated == true
        ? httpContext.User.Identity?.Name
        : null;
    if (!string.IsNullOrWhiteSpace(identityName))
    {
        return $"user:{identityName}";
    }

    string? forwardedFor = httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedValues)
        ? forwardedValues.FirstOrDefault()
        : null;

    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        var firstIp = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstIp))
        {
            return $"ip:{firstIp}";
        }
    }

    var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
    if (!string.IsNullOrWhiteSpace(remoteIp))
    {
        return $"ip:{remoteIp}";
    }

    return $"host:{httpContext.Request.Headers.Host}";
}

