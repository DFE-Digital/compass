using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass;

/// <summary>
/// Seeds DfE three-band risk tiers (operational + proposed). Run:
/// <c>dotnet run -- --seed-risk-tiers [--environment Development]</c>
/// </summary>
public static class SeedRiskTiers
{
    public static async Task RunAsync(string environment = "Development")
    {
        Console.WriteLine($"=== Seeding {environment} risk tiers ===\n");

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

        var (added, updated) = await RiskTierSeedData.ApplyAsync(db);
        if (added == 0 && updated == 0)
            Console.WriteLine("All recommended risk tiers are already present and up to date.");
        else
            Console.WriteLine($"Done: added {added}, updated {updated}.");
    }
}
