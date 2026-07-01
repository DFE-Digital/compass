using Compass.Data;
using Compass.Services.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass;

/// <summary>
/// Seeds FIPS user groups (and hierarchy) from the restructured CMS export JSON.
/// Run: <c>dotnet run -- --seed-fips-user-groups [--environment Development] [--json-file path] [--from-cms] [--deactivate-missing]</c>
/// </summary>
public static class SeedFipsUserGroups
{
    public static async Task RunAsync(
        string environment = "Development",
        string? jsonFilePath = null,
        bool fromCms = false,
        bool deactivateMissing = false)
    {
        Console.WriteLine($"=== Seeding {environment} FIPS user groups ===\n");

        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false);
        builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        var configuration = builder.Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: Connection string not found");
            return;
        }

        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var db = new CompassDbContext(options);

        if (!await db.Database.CanConnectAsync())
        {
            Console.WriteLine("Error: Could not connect to the database");
            return;
        }

        FipsUserGroupCmsSeedService.SeedResult result;
        if (fromCms)
        {
            var cmsBaseUrl = configuration["CmsApi:BaseUrl"];
            if (string.IsNullOrWhiteSpace(cmsBaseUrl))
            {
                Console.WriteLine("Error: CmsApi:BaseUrl not configured");
                return;
            }

            Console.WriteLine($"Source: CMS ({cmsBaseUrl.TrimEnd('/')})");
            Console.WriteLine("Warning: live CMS seed uses parent links only and may not match the restructured hierarchy.\n");
            result = await FipsUserGroupCmsSeedService.ApplyFromCmsAsync(db, configuration);
        }
        else
        {
            var resolvedPath = FipsUserGroupCmsSeedService.ResolveSeedJsonPath(jsonFilePath);
            Console.WriteLine($"Source: {resolvedPath}\n");
            result = await FipsUserGroupCmsSeedService.ApplyFromJsonAsync(
                db,
                resolvedPath,
                deactivateMissing);
        }

        Console.WriteLine($"Source rows: {result.TotalFromSource}");
        Console.WriteLine($"Added: {result.Added}");
        Console.WriteLine($"Updated: {result.Updated}");
        Console.WriteLine($"Unchanged: {result.Skipped}");
        if (deactivateMissing)
            Console.WriteLine($"Deactivated (not in source): {result.Deactivated}");

        var totalActive = await db.FipsUserGroups.CountAsync(g => g.Active);
        var rootCount = await db.FipsUserGroups.CountAsync(g => g.Active && g.ParentId == null);
        Console.WriteLine($"\nActive groups in database: {totalActive} ({rootCount} roots)");
        Console.WriteLine("\n✓ FIPS user group seeding completed");
    }
}
