using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Identity;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using GraphUser = Microsoft.Graph.Models.User;

namespace Compass;

/// <summary>
/// HTTP load test: creates risks via POST /api/v1/risks against a running Compass instance.
/// Resolves the owner from Entra (same path as the UI user picker) before each create.
/// Run: <c>dotnet run -- --load-test-risks [--environment Development] [--count 100] [--concurrency 2] [--delay-ms 2000] [--base-url http://localhost:5500] [--owner-email andy.jones@education.gov.uk]</c>
/// </summary>
public static class LoadTestRiskCreation
{
    private const string DevTokenName = "load-test-risks-dev";
    private const string DefaultOwnerEmail = "andy.jones@education.gov.uk";

    private static readonly string[] UserSelectFields =
    {
        "id", "displayName", "givenName", "surname", "mail", "userPrincipalName", "jobTitle"
    };

    public static async Task RunAsync(
        string environment = "Development",
        int count = 100,
        int concurrency = 2,
        int delayMs = 2000,
        string baseUrl = "http://localhost:5500",
        string? apiTokenOverride = null,
        string ownerEmail = DefaultOwnerEmail)
    {
        ownerEmail = ownerEmail.Trim();

        Console.WriteLine($"=== Risk creation load test ({environment}) ===");
        Console.WriteLine($"Target: {baseUrl.TrimEnd('/')}/api/v1/risks");
        Console.WriteLine($"Count: {count}, concurrency: {concurrency}, delay between starts: {delayMs}ms");
        Console.WriteLine($"Owner (Entra query): {ownerEmail}\n");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: Connection string not found.");
            return;
        }

        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var db = new CompassDbContext(options);

        if (!await db.Database.CanConnectAsync())
        {
            Console.WriteLine("Error: Could not connect to the database.");
            return;
        }

        var graphClient = TryCreateGraphClient(configuration);
        if (graphClient == null)
        {
            Console.WriteLine("Error: Entra configuration is missing or invalid (Entra:TenantId, ClientId, ClientSecret).");
            return;
        }

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var userDirectoryLogger = loggerFactory.CreateLogger<UserDirectoryService>();

        var bearerToken = apiTokenOverride ?? Environment.GetEnvironmentVariable("COMPASS_API_TOKEN");
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            bearerToken = await EnsureDevApiTokenAsync(db);
            Console.WriteLine($"Using API token '{DevTokenName}' from database.");
        }

        Console.WriteLine("Verifying owner lookup against Entra...");
        try
        {
            var probe = await ResolveOwnerFromEntraAsync(options, graphClient, userDirectoryLogger, ownerEmail);
            Console.WriteLine($"Owner resolved: {probe.Name} ({probe.Email}), Compass user id {probe.UserId}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Could not resolve owner from Entra: {ex.Message}");
            return;
        }

        using var http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var semaphore = new SemaphoreSlim(concurrency, concurrency);
        var tasks = new List<Task<(int index, bool ok, int? id, long ms, long entraMs, string? error)>>();
        var sw = Stopwatch.StartNew();

        for (var i = 1; i <= count; i++)
        {
            var index = i;
            await semaphore.WaitAsync();

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    if (delayMs > 0)
                        await Task.Delay(delayMs);

                    var entraSw = Stopwatch.StartNew();
                    var owner = await ResolveOwnerFromEntraAsync(options, graphClient, userDirectoryLogger, ownerEmail);
                    entraSw.Stop();

                    var payload = new
                    {
                        title = $"Load test risk #{index} ({runId})",
                        description = $"Automated load-test risk created at {DateTime.UtcNow:O}.",
                        impactRating = (index % 5) + 1,
                        likelihoodRating = ((index + 2) % 5) + 1,
                        status = "new",
                        ownerEmail = owner.Email,
                        ownerUserId = owner.UserId
                    };

                    var reqSw = Stopwatch.StartNew();
                    using var response = await http.PostAsJsonAsync("api/v1/risks", payload);
                    reqSw.Stop();

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        return (index, false, (int?)null, reqSw.ElapsedMilliseconds, entraSw.ElapsedMilliseconds,
                            $"{(int)response.StatusCode}: {Truncate(body, 200)}");
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    int? id = doc.RootElement.TryGetProperty("id", out var idEl) && idEl.TryGetInt32(out var parsed)
                        ? parsed
                        : null;

                    return (index, true, id, reqSw.ElapsedMilliseconds, entraSw.ElapsedMilliseconds, (string?)null);
                }
                catch (Exception ex)
                {
                    return (index, false, (int?)null, 0L, 0L, ex.Message);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        var results = (await Task.WhenAll(tasks)).OrderBy(r => r.index).ToList();
        sw.Stop();

        var ok = results.Count(r => r.ok);
        var failed = results.Count(r => !r.ok);
        var avgMs = results.Where(r => r.ok).Select(r => r.ms).DefaultIfEmpty(0).Average();
        var avgEntraMs = results.Where(r => r.ok).Select(r => r.entraMs).DefaultIfEmpty(0).Average();

        Console.WriteLine($"Completed in {sw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine($"Success: {ok}, failed: {failed}");
        Console.WriteLine($"Avg API latency (success): {avgMs:F0}ms, avg Entra resolve (success): {avgEntraMs:F0}ms");

        if (failed > 0)
        {
            Console.WriteLine("\nFailures:");
            foreach (var r in results.Where(r => !r.ok).Take(10))
                Console.WriteLine($"  #{r.index}: {r.error}");
            if (failed > 10)
                Console.WriteLine($"  ... and {failed - 10} more");
        }

        var sampleIds = results.Where(r => r.ok && r.id.HasValue).Take(5).Select(r => r.id!.Value);
        if (sampleIds.Any())
            Console.WriteLine($"\nSample risk IDs: {string.Join(", ", sampleIds)}");
    }

    private sealed record OwnerContext(int UserId, string Email, string Name);

    /// <summary>Search Entra and ensure a Compass user row (mirrors /api/users/search + /api/users/select).</summary>
    private static async Task<OwnerContext> ResolveOwnerFromEntraAsync(
        DbContextOptions<CompassDbContext> dbOptions,
        GraphServiceClient graph,
        ILogger<UserDirectoryService> userDirectoryLogger,
        string ownerEmail)
    {
        var graphUser = await FindGraphUserByEmailAsync(graph, ownerEmail)
            ?? throw new InvalidOperationException($"No Entra user found for {ownerEmail}");

        if (!Guid.TryParse(graphUser.Id, out var objectId))
            throw new InvalidOperationException($"Entra user id is not a valid GUID: {graphUser.Id}");

        await using var resolveDb = new CompassDbContext(dbOptions);
        var userDirectory = new UserDirectoryService(resolveDb, graph, userDirectoryLogger);
        var compassUser = await userDirectory.EnsureUserAsync(objectId);
        var email = compassUser.Email ?? ownerEmail;
        var name = compassUser.Name ?? GraphUserNameFormatter.FormatFriendlyName(graphUser, email);

        return new OwnerContext(compassUser.Id, email, name);
    }

    private static async Task<GraphUser?> FindGraphUserByEmailAsync(GraphServiceClient graph, string ownerEmail)
    {
        try
        {
            return await graph.Users[ownerEmail].GetAsync(requestConfiguration =>
            {
                requestConfiguration.QueryParameters.Select = UserSelectFields;
            });
        }
        catch
        {
            // Fall through to search
        }

        var localPart = ownerEmail.Split('@')[0];
        var escapedEmail = ownerEmail.Replace("'", "''");
        var domainFilter = "(endswith(mail,'@education.gov.uk') or endswith(userPrincipalName,'@education.gov.uk'))";
        var filter =
            $"{domainFilter} and (mail eq '{escapedEmail}' or userPrincipalName eq '{escapedEmail}' or startswith(mail,'{localPart.Replace("'", "''")}') or startswith(userPrincipalName,'{localPart.Replace("'", "''")}'))";

        var response = await graph.Users.GetAsync(requestConfiguration =>
        {
            requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
            requestConfiguration.QueryParameters.Count = true;
            requestConfiguration.QueryParameters.Filter = filter;
            requestConfiguration.QueryParameters.Top = 10;
            requestConfiguration.QueryParameters.Select = UserSelectFields;
        });

        return response?.Value?
            .FirstOrDefault(u =>
                string.Equals(u.Mail, ownerEmail, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.UserPrincipalName, ownerEmail, StringComparison.OrdinalIgnoreCase))
            ?? response?.Value?.FirstOrDefault();
    }

    private static GraphServiceClient? TryCreateGraphClient(IConfiguration configuration)
    {
        var tenantId = configuration["Entra:TenantId"];
        var clientId = configuration["Entra:ClientId"];
        var clientSecret = configuration["Entra:ClientSecret"];

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            return null;

        var credential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret,
            new ClientSecretCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud });

        return new GraphServiceClient(credential);
    }

    private static async Task<string> EnsureDevApiTokenAsync(CompassDbContext db)
    {
        var existing = await db.ApiTokens
            .Include(t => t.Permissions)
            .FirstOrDefaultAsync(t => t.Name == DevTokenName);

        if (existing != null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await db.SaveChangesAsync();
            }

            EnsureRisksCreatePermission(existing);
            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync();

            return existing.Token;
        }

        var tokenValue = GenerateSecureToken();
        var apiToken = new ApiToken
        {
            Name = DevTokenName,
            Description = "Auto-created for local risk load tests. Safe to delete in dev.",
            Token = tokenValue,
            CreatedByEmail = DefaultOwnerEmail,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        EnsureRisksCreatePermission(apiToken);

        db.ApiTokens.Add(apiToken);
        await db.SaveChangesAsync();
        return tokenValue;
    }

    private static void EnsureRisksCreatePermission(ApiToken apiToken)
    {
        var perm = apiToken.Permissions.FirstOrDefault(p => p.Resource == "Risks");
        if (perm == null)
        {
            apiToken.Permissions.Add(new ApiTokenPermission
            {
                Resource = "Risks",
                CanRead = true,
                CanCreate = true,
                CanUpdate = true,
                CanDelete = false
            });
            return;
        }

        perm.CanRead = true;
        perm.CanCreate = true;
    }

    private static string GenerateSecureToken()
    {
        const string prefix = "cps_";
        var randomBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return prefix + Convert.ToBase64String(randomBytes).Replace("+", "").Replace("/", "").Replace("=", "");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
