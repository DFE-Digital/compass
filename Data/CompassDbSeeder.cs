using Microsoft.EntityFrameworkCore;
using Compass.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Compass.Data;

/// <summary>
/// Seeds the Azure SQL database with reference data from SQLite
/// </summary>
public class CompassDbSeeder
{
    private readonly CompassDbContext _targetDb;
    private readonly CompassDbContext? _sourceDb;

    public CompassDbSeeder(CompassDbContext targetDb, CompassDbContext? sourceDb = null)
    {
        _targetDb = targetDb;
        _sourceDb = sourceDb;
    }

    /// <summary>
    /// Seeds all reference data
    /// </summary>
    public async Task SeedAsync()
    {
        Console.WriteLine("Starting database seeding...");

        await SeedRiskTiersAsync();
        await SeedRiskTypesAsync();
        await SeedEnterpriseMetricsAsync();
        await SeedFunctionalStandardsAsync();
        await SeedObjectivesAsync();

        Console.WriteLine("✓ Database seeding completed");
    }

    /// <summary>
    /// Seeds from SQLite source database if available
    /// </summary>
    public async Task SeedFromSQLiteAsync()
    {
        if (_sourceDb == null)
        {
            Console.WriteLine("No source database provided for seeding");
            return;
        }

        Console.WriteLine("Starting data migration from SQLite...");

        await MigrateRiskTiersAsync();
        await MigrateRiskTypesAsync();
        await MigrateEnterpriseMetricsAsync();
        await MigrateFunctionalStandardsAsync();
        await MigrateObjectivesAsync();

        Console.WriteLine("✓ SQLite data migration completed");
    }

    #region Risk Tiers

    private async Task SeedRiskTiersAsync()
    {
        if (await _targetDb.RiskTiers.AnyAsync())
        {
            Console.WriteLine("Risk Tiers already exist, skipping seed");
            return;
        }

        var riskTiers = new[]
        {
            new RiskTier { Code = "TIER1", Name = "Tier 1", Description = "Critical risks requiring immediate attention", SortOrder = 1, IsActive = true },
            new RiskTier { Code = "TIER2", Name = "Tier 2", Description = "High priority risks", SortOrder = 2, IsActive = true },
            new RiskTier { Code = "TIER3", Name = "Tier 3", Description = "Medium priority risks", SortOrder = 3, IsActive = true },
            new RiskTier { Code = "TIER4", Name = "Tier 4", Description = "Low priority risks", SortOrder = 4, IsActive = true },
            new RiskTier { Code = "NONE", Name = "Not Tiered", Description = "Risks not assigned to a tier", SortOrder = 5, IsActive = true }
        };

        await _targetDb.RiskTiers.AddRangeAsync(riskTiers);
        await _targetDb.SaveChangesAsync();
        Console.WriteLine($"✓ Seeded {riskTiers.Length} Risk Tiers");
    }

    private async Task MigrateRiskTiersAsync()
    {
        if (_sourceDb == null || await _targetDb.RiskTiers.AnyAsync()) return;

        var items = await _sourceDb.RiskTiers.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[RiskTiers] ON");
                await _targetDb.RiskTiers.AddRangeAsync(items);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[RiskTiers] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {items.Count} Risk Tiers");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    #endregion

    #region Risk Types

    private async Task SeedRiskTypesAsync()
    {
        if (await _targetDb.RiskTypes.AnyAsync())
        {
            Console.WriteLine("Risk Types already exist, skipping seed");
            return;
        }

        var riskTypes = new[]
        {
            new RiskType { Code = "TECH", Name = "Technical", Description = "Technical or technology-related risks", IsActive = true },
            new RiskType { Code = "SECURITY", Name = "Security", Description = "Security and data protection risks", IsActive = true },
            new RiskType { Code = "COMPLIANCE", Name = "Compliance", Description = "Regulatory and compliance risks", IsActive = true },
            new RiskType { Code = "RESOURCE", Name = "Resource", Description = "Resource availability risks", IsActive = true },
            new RiskType { Code = "FINANCIAL", Name = "Financial", Description = "Budget and financial risks", IsActive = true },
            new RiskType { Code = "DELIVERY", Name = "Delivery", Description = "Project delivery risks", IsActive = true },
            new RiskType { Code = "STRATEGIC", Name = "Strategic", Description = "Strategic alignment risks", IsActive = true },
            new RiskType { Code = "OPERATIONAL", Name = "Operational", Description = "Day-to-day operational risks", IsActive = true },
            new RiskType { Code = "THIRD_PARTY", Name = "Third Party", Description = "Third party and vendor risks", IsActive = true },
            new RiskType { Code = "CHANGE", Name = "Change", Description = "Change management risks", IsActive = true },
            new RiskType { Code = "REPUTATION", Name = "Reputational", Description = "Reputational risks", IsActive = true },
            new RiskType { Code = "DATA", Name = "Data", Description = "Data quality and management risks", IsActive = true },
            new RiskType { Code = "OTHER", Name = "Other", Description = "Other risk types", IsActive = true }
        };

        await _targetDb.RiskTypes.AddRangeAsync(riskTypes);
        await _targetDb.SaveChangesAsync();
        Console.WriteLine($"✓ Seeded {riskTypes.Length} Risk Types");
    }

    private async Task MigrateRiskTypesAsync()
    {
        if (_sourceDb == null || await _targetDb.RiskTypes.AnyAsync()) return;

        var items = await _sourceDb.RiskTypes.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[RiskTypes] ON");
                await _targetDb.RiskTypes.AddRangeAsync(items);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[RiskTypes] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {items.Count} Risk Types");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    #endregion

    #region Enterprise Metrics

    private async Task SeedEnterpriseMetricsAsync()
    {
        if (await _targetDb.EnterpriseMetrics.AnyAsync())
        {
            Console.WriteLine("Enterprise Metrics already exist, skipping seed");
            return;
        }

        // Add default enterprise metrics if needed
        Console.WriteLine("No default Enterprise Metrics to seed");
    }

    private async Task MigrateEnterpriseMetricsAsync()
    {
        if (_sourceDb == null || await _targetDb.EnterpriseMetrics.AnyAsync()) return;

        var items = await _sourceDb.EnterpriseMetrics.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[EnterpriseMetrics] ON");
                await _targetDb.EnterpriseMetrics.AddRangeAsync(items);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[EnterpriseMetrics] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {items.Count} Enterprise Metrics");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    #endregion

    #region Functional Standards

    private async Task SeedFunctionalStandardsAsync()
    {
        if (await _targetDb.FunctionalStandards.AnyAsync())
        {
            Console.WriteLine("Functional Standards already exist, skipping seed");
            return;
        }

        // Add default functional standards if needed
        Console.WriteLine("No default Functional Standards to seed");
    }

    private async Task MigrateFunctionalStandardsAsync()
    {
        if (_sourceDb == null || await _targetDb.FunctionalStandards.AnyAsync()) return;

        // Migrate in order: Standards -> Themes -> Practice Areas -> Criteria
        // FunctionalStandards uses user-defined IDs, not identity
        var standards = await _sourceDb.FunctionalStandards.AsNoTracking().ToListAsync();
        if (standards.Any())
        {
            await _targetDb.FunctionalStandards.AddRangeAsync(standards);
            await _targetDb.SaveChangesAsync();
            Console.WriteLine($"✓ Migrated {standards.Count} Functional Standards");
        }

        var themes = await _sourceDb.FunctionalStandardThemes.AsNoTracking().ToListAsync();
        if (themes.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[FunctionalStandardThemes] ON");
                await _targetDb.FunctionalStandardThemes.AddRangeAsync(themes);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[FunctionalStandardThemes] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {themes.Count} Functional Standard Themes");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        var practiceAreas = await _sourceDb.PracticeAreas.AsNoTracking().ToListAsync();
        if (practiceAreas.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[PracticeAreas] ON");
                await _targetDb.PracticeAreas.AddRangeAsync(practiceAreas);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[PracticeAreas] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {practiceAreas.Count} Practice Areas");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        var criteria = await _sourceDb.Criteria.AsNoTracking().ToListAsync();
        if (criteria.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Criteria] ON");
                await _targetDb.Criteria.AddRangeAsync(criteria);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Criteria] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {criteria.Count} Criteria");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    #endregion

    #region Objectives

    private async Task SeedObjectivesAsync()
    {
        if (await _targetDb.Objectives.AnyAsync())
        {
            Console.WriteLine("Objectives already exist, skipping seed");
            return;
        }

        // Add default objectives if needed
        Console.WriteLine("No default Objectives to seed");
    }

    private async Task MigrateObjectivesAsync()
    {
        if (_sourceDb == null || await _targetDb.Objectives.AnyAsync()) return;

        var items = await _sourceDb.Objectives.AsNoTracking().ToListAsync();
        if (items.Any())
        {
            using var transaction = await _targetDb.Database.BeginTransactionAsync();
            try
            {
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Objectives] ON");
                await _targetDb.Objectives.AddRangeAsync(items);
                await _targetDb.SaveChangesAsync();
                await _targetDb.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [dbo].[Objectives] OFF");
                await transaction.CommitAsync();
                Console.WriteLine($"✓ Migrated {items.Count} Objectives");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    #endregion
}

