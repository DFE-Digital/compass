using Compass.Data;
using Microsoft.EntityFrameworkCore;

namespace Compass;

/// <summary>
/// Seeds 50 DDT-related risks and issues into a "Development" RAID register.
/// Run: <c>dotnet run -- --seed-dev-risk-register [--environment Development]</c>
/// </summary>
public static class SeedDevRiskRegister
{
    public static async Task RunAsync(string environment = "Development")
    {
        Console.WriteLine($"=== Seeding Development risk register ({environment}) ===\n");

        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false);
        builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        var configuration = builder.Build();

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

        var (risksAdded, issuesAdded) = await DevRiskRegisterSeedData.ApplyAsync(db);

        Console.WriteLine($"\nDone: {risksAdded} risks and {issuesAdded} issues seeded into the Development register.");
    }
}
