using Microsoft.EntityFrameworkCore;
using Compass.Data;
using System.Net.Http;

namespace Compass;

/// <summary>
/// Utility to seed GDD Framework (Roles and Skills) from CSV
/// Run with: dotnet run --seed-gdd-framework [--environment Development|Production] [--csv-file path/to/file.csv]
/// </summary>
public class SeedGddFramework
{
    public static async Task RunAsync(string environment, string? csvFilePath = null)
    {
        Console.WriteLine($"=== Seeding {environment} Database with GDD Framework ===\n");

        // Get connection string based on environment
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

        Console.WriteLine($"Target: Azure SQL ({environment})");
        Console.WriteLine();

        // Create Azure SQL context
        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        using var targetDb = new CompassDbContext(options);

        // Test connection
        Console.WriteLine("Testing connection...");
        try
        {
            await targetDb.Database.CanConnectAsync();
            Console.WriteLine("✓ Connected to Azure SQL");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect: {ex.Message}");
            return;
        }

        Console.WriteLine();

        // Determine CSV file path
        string finalCsvPath;
        if (!string.IsNullOrEmpty(csvFilePath) && File.Exists(csvFilePath))
        {
            // Use provided file path
            finalCsvPath = csvFilePath;
        }
        else if (!string.IsNullOrEmpty(csvFilePath))
        {
            // Try to download from provided URL
            Console.WriteLine($"Downloading CSV from {csvFilePath}...");
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);
                
                var response = await httpClient.GetAsync(csvFilePath);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                finalCsvPath = Path.Combine(Path.GetTempPath(), "gdd-framework.csv");
                await File.WriteAllTextAsync(finalCsvPath, content);
                Console.WriteLine($"✓ Downloaded CSV to {finalCsvPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to download CSV: {ex.Message}");
                return;
            }
        }
        else
        {
            // Try default location in the project
            finalCsvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "gdd-framework.csv");
            if (!File.Exists(finalCsvPath))
            {
                Console.WriteLine($"✗ CSV file not found. Please specify --csv-file with path or URL");
                Console.WriteLine($"  Example: dotnet run --seed-gdd-framework --environment Development --csv-file https://cf-production-data-exports.s3.eu-west-2.amazonaws.com/exports/Role%20and%20skill%20content%20-%20Capability%20Framework%20-%20Government%20Digital%20and%20Data%20profession2025-10-28_13-40-49.csv");
                return;
            }
        }

        Console.WriteLine($"Using CSV file: {finalCsvPath}");
        Console.WriteLine();

        // Clear existing GDD data for clean re-seed
        Console.WriteLine("Clearing existing GDD data...");
        try
        {
            var staffRoleReturns = await targetDb.StaffRoleReturns.ToListAsync();
            if (staffRoleReturns.Any())
            {
                targetDb.StaffRoleReturns.RemoveRange(staffRoleReturns);
                await targetDb.SaveChangesAsync();
                Console.WriteLine($"  Deleted {staffRoleReturns.Count} Staff Role Returns");
            }
            
            var staffRoleReturnSkills = await targetDb.StaffRoleReturnSkills.ToListAsync();
            if (staffRoleReturnSkills.Any())
            {
                targetDb.StaffRoleReturnSkills.RemoveRange(staffRoleReturnSkills);
                await targetDb.SaveChangesAsync();
                Console.WriteLine($"  Deleted {staffRoleReturnSkills.Count} Staff Role Return Skills");
            }
            
            var skills = await targetDb.Skills.ToListAsync();
            if (skills.Any())
            {
                targetDb.Skills.RemoveRange(skills);
                await targetDb.SaveChangesAsync();
                Console.WriteLine($"  Deleted {skills.Count} Skills");
            }
            
            var roles = await targetDb.GddRoles.ToListAsync();
            if (roles.Any())
            {
                targetDb.GddRoles.RemoveRange(roles);
                await targetDb.SaveChangesAsync();
                Console.WriteLine($"  Deleted {roles.Count} GDD Roles");
            }
            
            Console.WriteLine("✓ GDD data cleared");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to clear GDD data: {ex.Message}");
            return;
        }

        // Seed GDD Framework
        var seeder = new CompassDbSeeder(targetDb);
        await seeder.SeedGddFrameworkFromCsvAsync(finalCsvPath);

        Console.WriteLine();
        Console.WriteLine("✓ GDD Framework seeding completed");
    }
}

