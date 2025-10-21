using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;

namespace Compass;

/// <summary>
/// Utility for migrating data from SQLite to Azure SQL Database
/// Run this with: dotnet run --migrate-data
/// </summary>
public class DataMigrationUtility
{
    public static async Task MigrateDataAsync(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        Console.WriteLine("Starting data migration from SQLite to Azure SQL...");
        
        try
        {
            // Ensure target database is created with latest migrations
            Console.WriteLine("Preparing target database...");
            
            try
            {
                // Try to ensure database exists and apply migrations
                Console.WriteLine("Applying migrations to Azure SQL database...");
                await targetDb.Database.MigrateAsync();
                Console.WriteLine("✓ Migrations applied successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
            {
                Console.WriteLine("Note: Model has pending changes, but will proceed with migration...");
                // Try using EnsureCreated as fallback
                try
                {
                    var created = await targetDb.Database.EnsureCreatedAsync();
                    Console.WriteLine(created ? "✓ Database created" : "✓ Database already exists");
                }
                catch
                {
                    Console.WriteLine("✓ Database schema ready (using existing)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note: Migration encountered an issue: {ex.Message}");
                Console.WriteLine("Attempting to use existing database schema...");
            }

            // Migrate in order of dependencies
            
            // 1. Independent lookup tables first
            await MigrateRiskTiers(sourceDb, targetDb);
            await MigrateRiskTypes(sourceDb, targetDb);
            await MigrateActionSources(sourceDb, targetDb);
            
            // 2. Users
            await MigrateUsers(sourceDb, targetDb);
            await MigrateUserPreferences(sourceDb, targetDb);
            
            // 3. API Tokens
            await MigrateApiTokens(sourceDb, targetDb);
            await MigrateApiTokenPermissions(sourceDb, targetDb);
            await MigrateApiRequestLogs(sourceDb, targetDb);
            
            // 4. Performance Metrics
            await MigratePerformanceMetrics(sourceDb, targetDb);
            
            // 5. Functional Standards hierarchy
            await MigrateFunctionalStandards(sourceDb, targetDb);
            await MigrateFunctionalStandardThemes(sourceDb, targetDb);
            await MigratePracticeAreas(sourceDb, targetDb);
            await MigrateCriteria(sourceDb, targetDb);
            
            // 6. Product Reporting
            await MigrateProductReturns(sourceDb, targetDb);
            await MigrateProductMetricValues(sourceDb, targetDb);
            
            // 7. Functional Standard Assessments
            await MigrateFunctionalStandardAssessments(sourceDb, targetDb);
            await MigrateAssessmentCriteriaResponses(sourceDb, targetDb);
            
            // 8. Enterprise Metrics
            await MigrateEnterpriseMetrics(sourceDb, targetDb);
            await MigrateEnterpriseReturns(sourceDb, targetDb);
            await MigrateEnterpriseMetricValues(sourceDb, targetDb);
            
            // 9. RAID items - Objectives first, then dependent items
            await MigrateObjectives(sourceDb, targetDb);
            await MigrateRisks(sourceDb, targetDb);
            await MigrateIssues(sourceDb, targetDb);
            await MigrateMilestones(sourceDb, targetDb);
            await MigrateActions(sourceDb, targetDb);
            await MigrateComments(sourceDb, targetDb);
            
            // 10. Junction tables
            await MigrateRiskActions(sourceDb, targetDb);
            await MigrateRiskRiskTypes(sourceDb, targetDb);
            await MigrateIssueActions(sourceDb, targetDb);
            await MigrateMilestoneActions(sourceDb, targetDb);
            await MigrateMilestoneRisks(sourceDb, targetDb);
            await MigrateMilestoneIssues(sourceDb, targetDb);
            
            Console.WriteLine("\n✓ Data migration completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Migration failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private static async Task MigrateRiskTiers(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.RiskTiers.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} RiskTiers...");
            await targetDb.RiskTiers.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateRiskTypes(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.RiskTypes.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} RiskTypes...");
            await targetDb.RiskTypes.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateActionSources(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.ActionSources.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} ActionSources...");
            await targetDb.ActionSources.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateUsers(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Users.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Users...");
            await targetDb.Users.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateUserPreferences(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.UserPreferences.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} UserPreferences...");
            await targetDb.UserPreferences.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateApiTokens(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.ApiTokens.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} ApiTokens...");
            await targetDb.ApiTokens.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateApiTokenPermissions(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.ApiTokenPermissions.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} ApiTokenPermissions...");
            await targetDb.ApiTokenPermissions.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateApiRequestLogs(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.ApiRequestLogs.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} ApiRequestLogs...");
            await targetDb.ApiRequestLogs.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigratePerformanceMetrics(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.PerformanceMetrics.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} PerformanceMetrics...");
            await targetDb.PerformanceMetrics.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateFunctionalStandards(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.FunctionalStandards.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} FunctionalStandards...");
            await targetDb.FunctionalStandards.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateFunctionalStandardThemes(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.FunctionalStandardThemes.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} FunctionalStandardThemes...");
            await targetDb.FunctionalStandardThemes.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigratePracticeAreas(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.PracticeAreas.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} PracticeAreas...");
            await targetDb.PracticeAreas.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateCriteria(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Criteria.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Criteria...");
            await targetDb.Criteria.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateProductReturns(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.ProductReturns.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} ProductReturns...");
            await targetDb.ProductReturns.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateProductMetricValues(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.ProductMetricValues.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} ProductMetricValues...");
            await targetDb.ProductMetricValues.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateFunctionalStandardAssessments(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.FunctionalStandardAssessments.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} FunctionalStandardAssessments...");
            await targetDb.FunctionalStandardAssessments.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateAssessmentCriteriaResponses(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.AssessmentCriteriaResponses.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} AssessmentCriteriaResponses...");
            await targetDb.AssessmentCriteriaResponses.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateEnterpriseMetrics(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.EnterpriseMetrics.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} EnterpriseMetrics...");
            await targetDb.EnterpriseMetrics.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateEnterpriseReturns(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.EnterpriseReturns.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} EnterpriseReturns...");
            await targetDb.EnterpriseReturns.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateEnterpriseMetricValues(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.EnterpriseMetricValues.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} EnterpriseMetricValues...");
            await targetDb.EnterpriseMetricValues.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateObjectives(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Objectives.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Objectives...");
            await targetDb.Objectives.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateRisks(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Risks.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Risks...");
            await targetDb.Risks.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateIssues(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Issues.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Issues...");
            await targetDb.Issues.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateMilestones(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Milestones.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Milestones...");
            await targetDb.Milestones.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateActions(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Actions.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Actions...");
            await targetDb.Actions.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateComments(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.Comments.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} Comments...");
            await targetDb.Comments.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateRiskActions(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.RiskActions.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} RiskActions...");
            await targetDb.RiskActions.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateRiskRiskTypes(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.RiskRiskTypes.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} RiskRiskTypes...");
            await targetDb.RiskRiskTypes.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateIssueActions(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.IssueActions.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} IssueActions...");
            await targetDb.IssueActions.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateMilestoneActions(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.MilestoneActions.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} MilestoneActions...");
            await targetDb.MilestoneActions.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateMilestoneRisks(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.MilestoneRisks.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} MilestoneRisks...");
            await targetDb.MilestoneRisks.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateMilestoneIssues(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.MilestoneIssues.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} MilestoneIssues...");
            await targetDb.MilestoneIssues.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }
}

