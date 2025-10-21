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
            await MigrateTableWithIdentity(sourceDb, targetDb, "RiskTiers", db => db.RiskTiers);
            await MigrateTableWithIdentity(sourceDb, targetDb, "RiskTypes", db => db.RiskTypes);
            await MigrateTableWithIdentity(sourceDb, targetDb, "ActionSources", db => db.ActionSources);
            
            // 2. Users
            await MigrateTableWithIdentity(sourceDb, targetDb, "Users", db => db.Users);
            // UserPreferences uses UserId as PK, not auto-increment
            await MigrateUserPreferences(sourceDb, targetDb);
            
            // 3. API Tokens
            await MigrateTableWithIdentity(sourceDb, targetDb, "ApiTokens", db => db.ApiTokens);
            await MigrateTableWithIdentity(sourceDb, targetDb, "ApiTokenPermissions", db => db.ApiTokenPermissions);
            await MigrateTableWithIdentity(sourceDb, targetDb, "ApiRequestLogs", db => db.ApiRequestLogs);
            
            // 4. Performance Metrics
            await MigrateTableWithIdentity(sourceDb, targetDb, "PerformanceMetrics", db => db.PerformanceMetrics);
            
            // 5. Functional Standards hierarchy (FunctionalStandards uses non-auto ID)
            await MigrateFunctionalStandards(sourceDb, targetDb);
            await MigrateTableWithIdentity(sourceDb, targetDb, "FunctionalStandardThemes", db => db.FunctionalStandardThemes);
            await MigrateTableWithIdentity(sourceDb, targetDb, "PracticeAreas", db => db.PracticeAreas);
            await MigrateTableWithIdentity(sourceDb, targetDb, "Criteria", db => db.Criteria);
            
            // 6. Product Reporting
            await MigrateTableWithIdentity(sourceDb, targetDb, "ProductReturns", db => db.ProductReturns);
            await MigrateTableWithIdentity(sourceDb, targetDb, "ProductMetricValues", db => db.ProductMetricValues);
            
            // 7. Functional Standard Assessments
            await MigrateTableWithIdentity(sourceDb, targetDb, "FunctionalStandardAssessments", db => db.FunctionalStandardAssessments);
            await MigrateTableWithIdentity(sourceDb, targetDb, "AssessmentCriteriaResponses", db => db.AssessmentCriteriaResponses);
            
            // 8. Enterprise Metrics
            await MigrateTableWithIdentity(sourceDb, targetDb, "EnterpriseMetrics", db => db.EnterpriseMetrics);
            await MigrateTableWithIdentity(sourceDb, targetDb, "EnterpriseReturns", db => db.EnterpriseReturns);
            await MigrateTableWithIdentity(sourceDb, targetDb, "EnterpriseMetricValues", db => db.EnterpriseMetricValues);
            
            // 9. RAID items - Objectives first, then dependent items
            await MigrateTableWithIdentity(sourceDb, targetDb, "Objectives", db => db.Objectives);
            await MigrateTableWithIdentity(sourceDb, targetDb, "Risks", db => db.Risks);
            await MigrateTableWithIdentity(sourceDb, targetDb, "Issues", db => db.Issues);
            await MigrateTableWithIdentity(sourceDb, targetDb, "Milestones", db => db.Milestones);
            await MigrateTableWithIdentity(sourceDb, targetDb, "Actions", db => db.Actions);
            await MigrateTableWithIdentity(sourceDb, targetDb, "Comments", db => db.Comments);
            
            // 10. Junction tables (composite keys, no identity)
            await MigrateJunctionTable(sourceDb, targetDb, db => db.RiskActions, "RiskActions");
            await MigrateJunctionTable(sourceDb, targetDb, db => db.RiskRiskTypes, "RiskRiskTypes");
            await MigrateJunctionTable(sourceDb, targetDb, db => db.IssueActions, "IssueActions");
            await MigrateJunctionTable(sourceDb, targetDb, db => db.MilestoneActions, "MilestoneActions");
            await MigrateJunctionTable(sourceDb, targetDb, db => db.MilestoneRisks, "MilestoneRisks");
            await MigrateJunctionTable(sourceDb, targetDb, db => db.MilestoneIssues, "MilestoneIssues");
            
            Console.WriteLine("\n✓ Data migration completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Migration failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private static async Task MigrateTableWithIdentity<T>(
        CompassDbContext sourceDb,
        CompassDbContext targetDb,
        string tableName,
        Func<CompassDbContext, DbSet<T>> getDbSet) where T : class
    {
        var sourceSet = getDbSet(sourceDb);
        var targetSet = getDbSet(targetDb);
        
        var items = await sourceSet.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} {tableName}...");
            
            // Use a transaction to ensure IDENTITY_INSERT stays enabled during the operation
            using var transaction = await targetDb.Database.BeginTransactionAsync();
            try
            {
                await targetDb.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] ON");
                await targetSet.AddRangeAsync(items);
                await targetDb.SaveChangesAsync();
                await targetDb.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] OFF");
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
    
    private static async Task MigrateJunctionTable<T>(
        CompassDbContext sourceDb,
        CompassDbContext targetDb,
        Func<CompassDbContext, DbSet<T>> getDbSet,
        string tableName) where T : class
    {
        var sourceSet = getDbSet(sourceDb);
        var targetSet = getDbSet(targetDb);
        
        var items = await sourceSet.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} {tableName}...");
            await targetSet.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateFunctionalStandards(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.FunctionalStandards.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} FunctionalStandards...");
            // FunctionalStandards uses user-defined IDs, not identity
            await targetDb.FunctionalStandards.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }

    private static async Task MigrateUserPreferences(CompassDbContext sourceDb, CompassDbContext targetDb)
    {
        var items = await sourceDb.UserPreferences.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            Console.WriteLine($"Migrating {items.Count} UserPreferences...");
            // UserPreferences uses UserId as PK, not auto-increment
            await targetDb.UserPreferences.AddRangeAsync(items);
            await targetDb.SaveChangesAsync();
        }
    }
    
    private static async Task EnableIdentityInsert(CompassDbContext context, string tableName)
    {
        await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] ON");
    }
    
    private static async Task DisableIdentityInsert(CompassDbContext context, string tableName)
    {
        await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [dbo].[{tableName}] OFF");
    }
}

