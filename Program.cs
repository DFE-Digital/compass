using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Compass.Services;
using Compass.Data;
using Compass.Middlewares;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

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

// Check for data migration command
if (args.Length > 0 && args[0] == "--migrate-data")
{
    await RunDataMigration(builder);
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

// Configure database - Use Azure SQL for all environments
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

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
builder.Services.AddScoped<IGraphService, GraphService>();
builder.Services.AddScoped<IApiTokenService, ApiTokenService>();

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

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure port for local development
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://localhost:5500");
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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
app.UseAuthorization();

app.UseSession();
app.UseRateLimiter();

// API routes
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
        
        Console.WriteLine("Compass database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Compass database initialization error: {ex.Message}");
        throw;
    }
}

app.Run();

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

