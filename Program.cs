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

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    // Use SQLite for development
    var sqliteConnectionString = builder.Configuration.GetConnectionString("CompassDb") ?? "Data Source=compass.db";
    builder.Services.AddDbContext<CompassDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
}
else
{
    // Use Azure SQL for test and production
    builder.Services.AddDbContext<CompassDbContext>(options =>
        options.UseSqlServer(connectionString));
}

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
        $"script-src 'self' 'nonce-{nonce}' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
        $"style-src 'self' 'nonce-{nonce}' 'unsafe-inline' https://rsms.me https://fonts.googleapis.com https://cdnjs.cloudflare.com https://cdn.jsdelivr.net; " +
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
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        
        Console.WriteLine("Compass database initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Compass database initialization error: {ex.Message}");
    }
}

app.Run();

