using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Compass.Data;

namespace Compass;

public static class AzureSqlEnvironmentMigration
{
    public static async Task RunAsync(string sourceEnvironment, string targetEnvironment, bool referenceOnly = false)
    {
        Console.WriteLine($"=== Migrating data: {sourceEnvironment} → {targetEnvironment} ===\n");

        // Load config for both environments
        var baseBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false);

        var sourceConfig = baseBuilder
            .AddJsonFile($"appsettings.{sourceEnvironment}.json", optional: true)
            .Build();

        var targetConfig = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{targetEnvironment}.json", optional: true)
            .Build();

        var sourceConn = sourceConfig.GetConnectionString("DefaultConnection");
        var targetConn = targetConfig.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(sourceConn))
        {
            Console.WriteLine($"Error: DefaultConnection not found for source environment '{sourceEnvironment}'.");
            return;
        }
        if (string.IsNullOrWhiteSpace(targetConn))
        {
            Console.WriteLine($"Error: DefaultConnection not found for target environment '{targetEnvironment}'.");
            return;
        }

        Console.WriteLine($"Source: Azure SQL ({sourceEnvironment})\nTarget: Azure SQL ({targetEnvironment})\n");

        // Build DbContexts
        var sourceOptions = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(sourceConn)
            .Options;
        var targetOptions = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(targetConn)
            .Options;

        using var sourceDb = new CompassDbContext(sourceOptions);
        using var targetDb = new CompassDbContext(targetOptions);

        // Verify connectivity
        Console.WriteLine("Testing connections...");
        await sourceDb.Database.CanConnectAsync();
        Console.WriteLine($"✓ Connected to Azure SQL source ({sourceEnvironment})");
        await targetDb.Database.CanConnectAsync();
        Console.WriteLine($"✓ Connected to Azure SQL target ({targetEnvironment})\n");

        // Apply migrations, purge target, then migrate data using existing utility
        if (referenceOnly)
        {
            // Purge only reference tables
            Console.WriteLine("Purging reference data in target...");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[Criteria]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[PracticeAreas]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[FunctionalStandardThemes]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[FunctionalStandards]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[PerformanceMetrics]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[EnterpriseMetrics]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[ActionSources]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[RiskTypes]");
            await targetDb.Database.ExecuteSqlRawAsync("DELETE FROM [dbo].[RiskTiers]");

            await DataMigrationUtility.MigrateReferenceDataOnlyAsync(sourceDb, targetDb);
        }
        else
        {
            await DataMigrationUtility.PurgeTargetDataAsync(targetDb);
            await DataMigrationUtility.MigrateDataAsync(sourceDb, targetDb);
        }

        Console.WriteLine($"\n✓ Migration {sourceEnvironment} → {targetEnvironment} completed successfully!");
    }
}


