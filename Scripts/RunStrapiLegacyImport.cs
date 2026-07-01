using Compass.Data;
using Compass.Services.Fips;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Compass.Scripts;

/// <summary>
/// Import legacy Strapi/CMS export JSON into the Compass service register (dev/ops one-off).
/// Usage:
///   dotnet run -- --strapi-legacy-import requirements/data.json --dry-run
///   dotnet run -- --strapi-legacy-import requirements/data.json
/// </summary>
public static class RunStrapiLegacyImport
{
    public static async Task Main(string[] args)
    {
        var pathArg = args.Skip(1).FirstOrDefault(a => !a.StartsWith("-", StringComparison.Ordinal));
        if (string.IsNullOrWhiteSpace(pathArg))
        {
            Console.WriteLine("Usage: dotnet run -- --strapi-legacy-import <path-to-json> [--dry-run]");
            Environment.Exit(1);
            return;
        }

        var dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));
        var jsonPath = Path.GetFullPath(pathArg);
        if (!File.Exists(jsonPath))
        {
            Console.WriteLine($"File not found: {jsonPath}");
            Environment.Exit(1);
            return;
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Error: DefaultConnection not configured.");
            Environment.Exit(1);
            return;
        }

        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new CompassDbContext(options);
        var write = new FipsProductWriteService(db);
        var importer = new FipsStrapiLegacyImportService(db, write);

        Console.WriteLine(dryRun
            ? $"DRY RUN — no database writes ({jsonPath})"
            : $"Applying import ({jsonPath})");
        Console.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        if (!dryRun)
        {
            Console.WriteLine("Press Enter to continue or Ctrl+C to cancel...");
            Console.ReadLine();
        }

        await using var stream = File.OpenRead(jsonPath);
        var result = await importer.ImportAsync(
            stream,
            actorEmail: "script:strapi-legacy-import",
            auditDisplayName: "Strapi legacy JSON import",
            dryRun: dryRun);

        Console.WriteLine($"Total: {result.TotalRows} · Updated: {result.UpdatedCount} · Skipped: {result.SkippedCount} · Failed: {result.FailedCount}");
        Console.WriteLine();

        foreach (var row in result.Rows.Where(r => r.Errors.Count > 0).Take(30))
        {
            Console.WriteLine($"[{row.RowNumber}] {row.ProductTitle} ({row.FipsId})");
            foreach (var err in row.Errors)
                Console.WriteLine($"  ERROR: {err}");
        }

        if (result.FailedCount > 30)
            Console.WriteLine($"... and {result.FailedCount - 30} more failed rows (see full log).");

        var logDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, $"strapi-legacy-import-{(dryRun ? "dryrun-" : "")}{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        await using var log = new StreamWriter(logPath);
        await log.WriteLineAsync($"Dry run: {dryRun}");
        await log.WriteLineAsync($"Total: {result.TotalRows} · Updated: {result.UpdatedCount} · Skipped: {result.SkippedCount} · Failed: {result.FailedCount}");
        await log.WriteLineAsync();
        foreach (var row in result.Rows)
        {
            await log.WriteLineAsync($"[{row.RowNumber}] {row.ProductTitle} | CMDB: {row.FipsId} | Success: {row.Success}");
            foreach (var m in row.Messages)
                await log.WriteLineAsync($"  {m}");
            foreach (var e in row.Errors)
                await log.WriteLineAsync($"  ERROR: {e}");
        }

        Console.WriteLine();
        Console.WriteLine($"Log: {logPath}");
        Console.WriteLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }
}
