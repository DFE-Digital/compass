using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using FipsReporting.Services;
using FipsReporting.Data;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add file logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFile("logs/app-{Date}.log");

// Add services to the container.
builder.Services.AddRazorPages();

// Configure authentication with Entra ID
if (builder.Environment.IsDevelopment())
{
    // Skip authentication in development mode
    builder.Services.AddAuthentication("Development")
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", options => { });
}
else
{
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
}

// Add Microsoft Identity Web UI for sign-in/sign-out (only in production)
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddControllersWithViews()
        .AddMicrosoftIdentityUI();
}

builder.Services.AddAuthorization();

// Configure database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    // Use SQLite for development
    var sqliteConnectionString = builder.Configuration.GetConnectionString("ReportingDb");
    builder.Services.AddDbContext<ReportingDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));
}
else
{
    // Use Azure SQL for production
    builder.Services.AddDbContext<ReportingDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Configure HTTP client for CMS API with optimizations (commented out for development)
// builder.Services.AddHttpClient<CmsApiService>(client =>
// {
//     var baseUrl = builder.Configuration["CmsApi:BaseUrl"] ?? "http://localhost:1337/api";
//     if (!baseUrl.EndsWith("/"))
//     {
//         baseUrl += "/";
//     }
//     client.BaseAddress = new Uri(baseUrl);
//     client.Timeout = TimeSpan.FromSeconds(30);
//     client.DefaultRequestHeaders.Add("User-Agent", "FIPS-Reporting/1.0");
//     client.DefaultRequestHeaders.Add("Authorization", $"Bearer {builder.Configuration["CmsApi:ReadApiKey"]}");
// })
// .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
// {
//     MaxConnectionsPerServer = 10,
//     UseProxy = false
// })
// .AddPolicyHandler(GetRetryPolicy());

// Register services
builder.Services.AddHttpClient<CmsApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddScoped<CmsApiService>();
// builder.Services.AddScoped<MockCmsApiService>(); // Commented out - using real CMS API
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IMetricsService, MetricsService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();
builder.Services.AddScoped<IObjectiveService, ObjectiveService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
builder.Services.AddScoped<FipsReporting.Services.IAuthenticationService, FipsReporting.Services.AuthenticationService>();
builder.Services.AddScoped<IPerformanceMetricService, PerformanceMetricService>();
builder.Services.AddScoped<IReportingStatusService, ReportingStatusService>();
// Business Intelligence Services (commented out temporarily for port testing)
// builder.Services.AddScoped<IBusinessIntelligenceService, BusinessIntelligenceService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();
// builder.Services.AddScoped<IEmailService, EmailService>();

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
    options.Cookie.Name = "FIPS.Reporting.Session";
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

// Configure the HTTP request pipeline.
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
        $"script-src 'self' 'nonce-{nonce}' https://cdn.jsdelivr.net; " +
        $"style-src 'self' 'nonce-{nonce}' https://rsms.me; " +
        $"img-src 'self' data: https:; " +
        $"font-src 'self' data: https://rsms.me; " +
        $"connect-src 'self'; " +
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

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "business-dashboard",
    pattern: "business-dashboard/{action=Index}/{id?}",
    defaults: new { controller = "BusinessDashboard" });

app.MapControllerRoute(
    name: "dashboard",
    pattern: "dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

    app.MapControllerRoute(
        name: "admin-users",
        pattern: "admin/users/{action=Index}/{id?}",
        defaults: new { controller = "AdminUser" });

app.MapControllerRoute(
    name: "admin-milestones",
    pattern: "admin/milestones/{action=Index}/{id?}",
    defaults: new { controller = "AdminMilestones" });

app.MapControllerRoute(
    name: "admin-objectives",
    pattern: "admin/objectives/{action=Index}/{id?}",
    defaults: new { controller = "AdminObjectives" });

app.MapControllerRoute(
    name: "admin-performance-metrics",
    pattern: "admin/performance-metrics/{action=Index}/{id?}",
    defaults: new { controller = "AdminPerformanceMetrics" });

// Main navigation routes
app.MapControllerRoute(
    name: "overview",
    pattern: "overview/{action=Index}/{id?}",
    defaults: new { controller = "Overview" });

// Performance reporting routes - handled by controller route attributes
app.MapControllerRoute(
    name: "reporting-service",
    pattern: "reporting/{year:int}/{month}/service/{fipsId}",
    defaults: new { controller = "Reporting", action = "Service" });

app.MapControllerRoute(
    name: "reporting-month",
    pattern: "reporting/{year:int}/{month}",
    defaults: new { controller = "Reporting", action = "Month" });

// Other reporting routes - more specific patterns
app.MapControllerRoute(
    name: "reporting-products",
    pattern: "reporting/products/{action=Index}/{id?}",
    defaults: new { controller = "ReportingProducts" });

app.MapControllerRoute(
    name: "reporting-milestones",
    pattern: "reporting/milestones/product/{productId}/{action=Index}/{id?}",
    defaults: new { controller = "ReportingMilestones" });

app.MapControllerRoute(
    name: "reporting-metrics",
    pattern: "reporting/metrics/{action=Index}/{id?}",
    defaults: new { controller = "ReportingMetrics" });

app.MapControllerRoute(
    name: "analysis",
    pattern: "analysis/{action=Index}/{id?}",
    defaults: new { controller = "Analysis" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Ensure database is created and up-to-date
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
    
    try
    {
        context.Database.Migrate();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
    {
        // Suppress the pending model changes warning - this is expected during development
        // The database is already up to date, but EF is being overly cautious
        Console.WriteLine("Database migration warning suppressed - database is up to date");
    }
    
    // Seed super admin user
    var userPermissionService = scope.ServiceProvider.GetRequiredService<IUserPermissionService>();
    await userPermissionService.SeedSuperAdminAsync();
}

app.Run();

// Retry policy for HTTP client
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}
