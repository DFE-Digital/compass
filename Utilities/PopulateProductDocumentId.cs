using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Compass.Data;
using Compass.Models;
using Compass.Services;
using System.Net.Http;

namespace Compass;

/// <summary>
/// Utility for populating ProductDocumentId from FipsId by looking up products in CMS
/// Run this with: dotnet run --populate-product-document-ids
/// </summary>
public static class PopulateProductDocumentId
{
    public static async Task RunAsync(string environment = "Development")
    {
        Console.WriteLine("=== Populate Product DocumentId Migration ===\n");

        // Build configuration
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);

        var configuration = builder.Build();

        // Setup database context
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Error: DefaultConnection string not found in configuration.");
            return;
        }

        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        // Setup HTTP client for CMS API
        var httpClientFactory = new HttpClientFactory(configuration);
        var httpClient = httpClientFactory.CreateClient();
        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var logger = loggerFactory.CreateLogger<ProductsApiService>();
        var memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());

        var productsApiService = new ProductsApiService(httpClient, memoryCache, logger, configuration);

        using var context = new CompassDbContext(options);

        // Test connection
        Console.WriteLine("Testing database connection...");
        try
        {
            await context.Database.CanConnectAsync();
            Console.WriteLine("✓ Connected to database\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect to database: {ex.Message}");
            return;
        }

        // Get all products from CMS
        Console.WriteLine("Fetching products from CMS...");
        var cmsProducts = await productsApiService.GetAllProductsAsync();
        Console.WriteLine($"✓ Found {cmsProducts.Count} products in CMS\n");

        // Create mapping of FipsId -> DocumentId
        var fipsIdToDocumentId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var product in cmsProducts)
        {
            if (!string.IsNullOrEmpty(product.FipsId) && !string.IsNullOrEmpty(product.DocumentId))
            {
                fipsIdToDocumentId[product.FipsId] = product.DocumentId;
            }
        }

        Console.WriteLine($"✓ Created mapping for {fipsIdToDocumentId.Count} products with FipsId\n");

        var stats = new MigrationStats();

        // Update ProductAccessibility
        Console.WriteLine("Updating ProductAccessibility records...");
        var productAccessibilities = await context.ProductAccessibilities
            .Where(pa => string.IsNullOrEmpty(pa.ProductDocumentId) && !string.IsNullOrEmpty(pa.FipsId))
            .ToListAsync();

        foreach (var pa in productAccessibilities)
        {
            if (fipsIdToDocumentId.TryGetValue(pa.FipsId!, out var documentId))
            {
                pa.ProductDocumentId = documentId;
                stats.ProductAccessibilityUpdated++;
            }
            else
            {
                Console.WriteLine($"  ⚠ Warning: No DocumentId found for FipsId '{pa.FipsId}' in ProductAccessibility ID {pa.Id}");
                stats.ProductAccessibilitySkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.ProductAccessibilityUpdated} ProductAccessibility records\n");

        // Update Risks
        Console.WriteLine("Updating Risk records...");
        var risks = await context.Risks
            .Where(r => string.IsNullOrEmpty(r.ProductDocumentId) && !string.IsNullOrEmpty(r.FipsId))
            .ToListAsync();

        foreach (var risk in risks)
        {
            if (fipsIdToDocumentId.TryGetValue(risk.FipsId!, out var documentId))
            {
                risk.ProductDocumentId = documentId;
                stats.RisksUpdated++;
            }
            else
            {
                stats.RisksSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.RisksUpdated} Risk records\n");

        // Update Issues
        Console.WriteLine("Updating Issue records...");
        var issues = await context.Issues
            .Where(i => string.IsNullOrEmpty(i.ProductDocumentId) && !string.IsNullOrEmpty(i.FipsId))
            .ToListAsync();

        foreach (var issue in issues)
        {
            if (fipsIdToDocumentId.TryGetValue(issue.FipsId!, out var documentId))
            {
                issue.ProductDocumentId = documentId;
                stats.IssuesUpdated++;
            }
            else
            {
                stats.IssuesSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.IssuesUpdated} Issue records\n");

        // Update Milestones
        Console.WriteLine("Updating Milestone records...");
        var milestones = await context.Milestones
            .Where(m => string.IsNullOrEmpty(m.ProductDocumentId) && !string.IsNullOrEmpty(m.FipsId))
            .ToListAsync();

        foreach (var milestone in milestones)
        {
            if (fipsIdToDocumentId.TryGetValue(milestone.FipsId!, out var documentId))
            {
                milestone.ProductDocumentId = documentId;
                stats.MilestonesUpdated++;
            }
            else
            {
                stats.MilestonesSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.MilestonesUpdated} Milestone records\n");

        // Update Actions
        Console.WriteLine("Updating Action records...");
        var actions = await context.Actions
            .Where(a => string.IsNullOrEmpty(a.ProductDocumentId) && !string.IsNullOrEmpty(a.FipsId))
            .ToListAsync();

        foreach (var action in actions)
        {
            if (fipsIdToDocumentId.TryGetValue(action.FipsId!, out var documentId))
            {
                action.ProductDocumentId = documentId;
                stats.ActionsUpdated++;
            }
            else
            {
                stats.ActionsSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.ActionsUpdated} Action records\n");

        // Update Decisions
        Console.WriteLine("Updating Decision records...");
        var decisions = await context.Decisions
            .Where(d => string.IsNullOrEmpty(d.ProductDocumentId) && !string.IsNullOrEmpty(d.FipsId))
            .ToListAsync();

        foreach (var decision in decisions)
        {
            if (fipsIdToDocumentId.TryGetValue(decision.FipsId!, out var documentId))
            {
                decision.ProductDocumentId = documentId;
                stats.DecisionsUpdated++;
            }
            else
            {
                stats.DecisionsSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.DecisionsUpdated} Decision records\n");

        // Update ProjectProducts
        Console.WriteLine("Updating ProjectProduct records...");
        var projectProducts = await context.ProjectProducts
            .Where(pp => string.IsNullOrEmpty(pp.ProductDocumentId) && !string.IsNullOrEmpty(pp.ProductFipsId))
            .ToListAsync();

        foreach (var pp in projectProducts)
        {
            if (fipsIdToDocumentId.TryGetValue(pp.ProductFipsId!, out var documentId))
            {
                pp.ProductDocumentId = documentId;
                stats.ProjectProductsUpdated++;
            }
            else
            {
                Console.WriteLine($"  ⚠ Warning: No DocumentId found for ProductFipsId '{pp.ProductFipsId}' in ProjectProduct ID {pp.Id}");
                stats.ProjectProductsSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.ProjectProductsUpdated} ProjectProduct records\n");

        // Update ProductReturns
        Console.WriteLine("Updating ProductReturn records...");
        var productReturns = await context.ProductReturns
            .Where(pr => string.IsNullOrEmpty(pr.ProductDocumentId) && !string.IsNullOrEmpty(pr.FipsId))
            .ToListAsync();

        foreach (var pr in productReturns)
        {
            if (fipsIdToDocumentId.TryGetValue(pr.FipsId!, out var documentId))
            {
                pr.ProductDocumentId = documentId;
                stats.ProductReturnsUpdated++;
            }
            else
            {
                Console.WriteLine($"  ⚠ Warning: No DocumentId found for FipsId '{pr.FipsId}' in ProductReturn ID {pr.Id}");
                stats.ProductReturnsSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.ProductReturnsUpdated} ProductReturn records\n");

        // Update PerformanceReportingProductExclusions
        Console.WriteLine("Updating PerformanceReportingProductExclusion records...");
        var exclusions = await context.PerformanceReportingProductExclusions
            .Where(e => string.IsNullOrEmpty(e.ProductDocumentId) && !string.IsNullOrEmpty(e.FipsId))
            .ToListAsync();

        foreach (var exclusion in exclusions)
        {
            if (fipsIdToDocumentId.TryGetValue(exclusion.FipsId!, out var documentId))
            {
                exclusion.ProductDocumentId = documentId;
                stats.ExclusionsUpdated++;
            }
            else
            {
                Console.WriteLine($"  ⚠ Warning: No DocumentId found for FipsId '{exclusion.FipsId}' in PerformanceReportingProductExclusion ID {exclusion.Id}");
                stats.ExclusionsSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.ExclusionsUpdated} PerformanceReportingProductExclusion records\n");

        // Update Kpis
        Console.WriteLine("Updating Kpi records...");
        var kpis = await context.Kpis
            .Where(k => string.IsNullOrEmpty(k.ProductDocumentId) && !string.IsNullOrEmpty(k.ProductFipsId))
            .ToListAsync();

        foreach (var kpi in kpis)
        {
            if (fipsIdToDocumentId.TryGetValue(kpi.ProductFipsId!, out var documentId))
            {
                kpi.ProductDocumentId = documentId;
                stats.KpisUpdated++;
            }
            else
            {
                stats.KpisSkipped++;
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Updated {stats.KpisUpdated} Kpi records\n");

        // Print summary
        Console.WriteLine("\n=== Migration Summary ===");
        Console.WriteLine($"ProductAccessibility: {stats.ProductAccessibilityUpdated} updated, {stats.ProductAccessibilitySkipped} skipped");
        Console.WriteLine($"Risks: {stats.RisksUpdated} updated, {stats.RisksSkipped} skipped");
        Console.WriteLine($"Issues: {stats.IssuesUpdated} updated, {stats.IssuesSkipped} skipped");
        Console.WriteLine($"Milestones: {stats.MilestonesUpdated} updated, {stats.MilestonesSkipped} skipped");
        Console.WriteLine($"Actions: {stats.ActionsUpdated} updated, {stats.ActionsSkipped} skipped");
        Console.WriteLine($"Decisions: {stats.DecisionsUpdated} updated, {stats.DecisionsSkipped} skipped");
        Console.WriteLine($"ProjectProducts: {stats.ProjectProductsUpdated} updated, {stats.ProjectProductsSkipped} skipped");
        Console.WriteLine($"ProductReturns: {stats.ProductReturnsUpdated} updated, {stats.ProductReturnsSkipped} skipped");
        Console.WriteLine($"PerformanceReportingProductExclusions: {stats.ExclusionsUpdated} updated, {stats.ExclusionsSkipped} skipped");
        Console.WriteLine($"Kpis: {stats.KpisUpdated} updated, {stats.KpisSkipped} skipped");
        Console.WriteLine("\n✓ Migration completed successfully!");
    }

    private class MigrationStats
    {
        public int ProductAccessibilityUpdated { get; set; }
        public int ProductAccessibilitySkipped { get; set; }
        public int RisksUpdated { get; set; }
        public int RisksSkipped { get; set; }
        public int IssuesUpdated { get; set; }
        public int IssuesSkipped { get; set; }
        public int MilestonesUpdated { get; set; }
        public int MilestonesSkipped { get; set; }
        public int ActionsUpdated { get; set; }
        public int ActionsSkipped { get; set; }
        public int DecisionsUpdated { get; set; }
        public int DecisionsSkipped { get; set; }
        public int ProjectProductsUpdated { get; set; }
        public int ProjectProductsSkipped { get; set; }
        public int ProductReturnsUpdated { get; set; }
        public int ProductReturnsSkipped { get; set; }
        public int ExclusionsUpdated { get; set; }
        public int ExclusionsSkipped { get; set; }
        public int KpisUpdated { get; set; }
        public int KpisSkipped { get; set; }
    }

    private class HttpClientFactory
    {
        private readonly IConfiguration _configuration;

        public HttpClientFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public HttpClient CreateClient()
        {
            var httpClient = new HttpClient();
            var baseUrl = _configuration["CmsApi:BaseUrl"];
            var apiKey = _configuration["CmsApi:ReadApiKey"];

            if (!string.IsNullOrEmpty(baseUrl))
            {
                httpClient.BaseAddress = new Uri(baseUrl);
            }

            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            }

            return httpClient;
        }
    }
}

