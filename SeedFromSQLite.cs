using Microsoft.EntityFrameworkCore;
using Compass.Data;
using System;
using System.Threading.Tasks;

namespace Compass;

/// <summary>
/// Utility to seed Azure SQL databases from local SQLite database
/// Run with: dotnet run --seed-from-sqlite [--environment Development|Production]
/// </summary>
public class SeedFromSQLite
{
    public static async Task RunAsync(string environment)
    {
        Console.WriteLine($"=== Seeding {environment} Database from SQLite ===\n");

        // Get connection strings based on environment
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        builder.AddJsonFile("appsettings.json", optional: false);
        builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        var configuration = builder.Build();

        var azureSqlConnectionString = configuration.GetConnectionString("DefaultConnection");
        var sqliteConnectionString = configuration.GetConnectionString("CompassDb_SQLite_Backup") ?? "Data Source=compass.db";

        if (string.IsNullOrEmpty(azureSqlConnectionString))
        {
            Console.WriteLine("Error: Azure SQL connection string not found");
            return;
        }

        Console.WriteLine($"Source: SQLite ({sqliteConnectionString})");
        Console.WriteLine($"Target: Azure SQL ({environment})");
        Console.WriteLine();

        // Create SQLite context (source)
        var sqliteOptions = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlite(sqliteConnectionString)
            .Options;
        using var sourceDb = new CompassDbContext(sqliteOptions);

        // Create Azure SQL context (target)
        var azureSqlOptions = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(azureSqlConnectionString)
            .Options;
        using var targetDb = new CompassDbContext(azureSqlOptions);

        // Test connections
        Console.WriteLine("Testing connections...");
        try
        {
            await sourceDb.Database.CanConnectAsync();
            Console.WriteLine("✓ Connected to SQLite source");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect to SQLite: {ex.Message}");
            return;
        }

        try
        {
            await targetDb.Database.CanConnectAsync();
            Console.WriteLine($"✓ Connected to Azure SQL ({environment})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect to Azure SQL: {ex.Message}");
            return;
        }

        Console.WriteLine();

        // Run seeding
        var seeder = new CompassDbSeeder(targetDb, sourceDb);
        await seeder.SeedFromSQLiteAsync();

        Console.WriteLine("\n✓ Seeding completed successfully!");
    }
}

