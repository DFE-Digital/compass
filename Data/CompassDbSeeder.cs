using Microsoft.EntityFrameworkCore;
using Compass.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

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

    #region GDD Framework Seeding

    /// <summary>
    /// Seeds GDD Roles and Skills from CSV file
    /// CSV expected format: Role Family,Role,Role Description,Role Level,Role Level Description,Skill Name,Skill Description,Skill Level,Skill Level Description
    /// </summary>
    public async Task SeedGddFrameworkFromCsvAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            Console.WriteLine($"Warning: CSV file not found at {csvFilePath}. Skipping GDD Framework seeding.");
            return;
        }

        Console.WriteLine($"Seeding GDD Framework from {csvFilePath}...");

        try
        {
            // Use composite key (RoleFamily + RoleName + RoleLevel) to ensure all role levels are captured
            var roles = new HashSet<(string RoleFamily, string RoleName, string RoleLevel)>(); 
            var skills = new HashSet<string>(); // Use SkillName as key
            var rolesData = new List<(string RoleFamily, string RoleName, string RoleLevel, string DisplayName, string Description)>();
            var skillsData = new List<(string SkillName, string Description, string Category)>();

            // Parse CSV file using CsvHelper
            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim
            });
            
            await foreach (var record in csv.GetRecordsAsync<CsvRoleRow>())
            {
                var roleFamily = record.RoleFamily ?? "";
                var roleName = record.Role ?? "";
                var roleDescription = record.RoleDescription ?? "";
                var roleLevel = record.RoleLevel ?? "";
                var skillName = record.SkillName ?? "";
                var skillDescription = record.SkillDescription ?? "";

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(roleFamily))
                    continue;
                
                // Skip rows where roleFamily or roleName look like fragments from descriptions
                // These typically start with lowercase, contain excessive commas, or are too long
                if (roleFamily.Length > 100 || roleName.Length > 100)
                    continue;
                    
                if (!char.IsUpper(roleFamily[0]) || !char.IsUpper(roleName[0]))
                    continue;
                    
                // Skip if the field contains too many commas (likely CSV parsing issues)
                if (roleFamily.Count(c => c == ',') > 2 || roleName.Count(c => c == ',') > 2)
                    continue;
                
                // Skip common fragments we've seen
                var lowerRoleFamily = roleFamily.ToLowerInvariant();
                var lowerRoleName = roleName.ToLowerInvariant();
                
                if (lowerRoleFamily.Contains("assist in") || lowerRoleFamily.Contains("coach and develop") ||
                    lowerRoleFamily.Contains("have an understanding") || lowerRoleFamily.Contains("work closely") ||
                    lowerRoleFamily.Contains("work independently") || lowerRoleFamily.Contains("in some organisations") ||
                    lowerRoleFamily.Contains("manage the development") || lowerRoleFamily.Contains("own the operational") ||
                    lowerRoleFamily.Contains("potentially manage") || lowerRoleFamily.Contains("liaise with") ||
                    lowerRoleFamily.Contains("at this level") || lowerRoleFamily.Contains("your responsibilities") ||
                    lowerRoleName.Contains("as required") || lowerRoleName.Contains("and manage") ||
                    lowerRoleName.Contains("data centre") || lowerRoleName.Contains("such as hardware") ||
                    lowerRoleName.Contains("throughout the service") || lowerRoleName.Contains("ensuring services") ||
                    lowerRoleName.Contains("and the likely") || lowerRoleName.Contains("capacity managers") ||
                    lowerRoleName.Contains("share information"))
                    continue;

                // Build display name for role
                string displayName;
                if (!string.IsNullOrEmpty(roleLevel) && !string.IsNullOrEmpty(roleName))
                {
                    displayName = $"{roleLevel} {roleName}";
                }
                else if (!string.IsNullOrEmpty(roleName))
                {
                    displayName = roleName;
                }
                else
                {
                    continue; // Skip if no role name
                }

                // Add role if not already added using composite key
                var roleKey = (roleFamily, roleName, roleLevel);
                if (!roles.Contains(roleKey))
                {
                    roles.Add(roleKey);
                    rolesData.Add((roleFamily, roleName, roleLevel, displayName, roleDescription));
                }

                // Add skill if not already added and not empty
                if (!string.IsNullOrEmpty(skillName) && !skills.Contains(skillName))
                {
                    skills.Add(skillName);
                    
                    // Try to determine category from skill name/description
                    string category = "General";
                    if (skillName.Contains("Business", StringComparison.OrdinalIgnoreCase))
                        category = "Business";
                    else if (skillName.Contains("Technical", StringComparison.OrdinalIgnoreCase) || skillName.Contains("Design", StringComparison.OrdinalIgnoreCase))
                        category = "Technical";
                    else if (skillName.Contains("Communication", StringComparison.OrdinalIgnoreCase) || skillName.Contains("Stakeholder", StringComparison.OrdinalIgnoreCase))
                        category = "Communication";
                    else if (skillName.Contains("Leadership", StringComparison.OrdinalIgnoreCase) || skillName.Contains("Management", StringComparison.OrdinalIgnoreCase))
                        category = "Leadership";
                    
                    skillsData.Add((skillName, skillDescription, category));
                }
            }

            // Seed GDD Roles
            var existingRoles = await _targetDb.GddRoles.ToListAsync();
            var existingRoleKeys = existingRoles.Select(r => (r.RoleFamily, r.RoleName, r.RoleLevel)).ToHashSet();
            
            var newRoles = rolesData
                .Where(r => !existingRoleKeys.Contains((r.RoleFamily, r.RoleName, r.RoleLevel)))
                .Select(r => new GddRole
                {
                    RoleFamily = r.RoleFamily,
                    RoleName = r.RoleName,
                    RoleLevel = r.RoleLevel,
                    DisplayName = r.DisplayName,
                    Description = r.Description,
                    IsActive = true,
                    SortOrder = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            if (newRoles.Any())
            {
                // Assign sort order by role family
                var roleFamilyOrder = newRoles.GroupBy(r => r.RoleFamily).Select((g, idx) => new { Family = g.Key, Order = idx * 100 }).ToList();
                foreach (var role in newRoles)
                {
                    var familyOrder = roleFamilyOrder.FirstOrDefault(f => f.Family == role.RoleFamily);
                    role.SortOrder = (familyOrder?.Order ?? 0) + newRoles.Where(r => r.RoleFamily == role.RoleFamily).ToList().IndexOf(role);
                }

                _targetDb.GddRoles.AddRange(newRoles);
                await _targetDb.SaveChangesAsync();
                Console.WriteLine($"✓ Seeded {newRoles.Count} new GDD Roles");
            }
            else
            {
                Console.WriteLine("✓ No new GDD Roles to seed");
            }

            // Seed Skills
            var existingSkills = await _targetDb.Skills.ToListAsync();
            var existingSkillKeys = existingSkills.Select(s => s.SkillName).ToHashSet();
            
            var newSkills = skillsData
                .Where(s => !existingSkillKeys.Contains(s.SkillName))
                .Select((s, idx) => new Skill
                {
                    SkillName = s.SkillName,
                    Description = s.Description,
                    Category = s.Category,
                    IsActive = true,
                    SortOrder = idx,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            if (newSkills.Any())
            {
                _targetDb.Skills.AddRange(newSkills);
                await _targetDb.SaveChangesAsync();
                Console.WriteLine($"✓ Seeded {newSkills.Count} new Skills");
            }
            else
            {
                Console.WriteLine("✓ No new Skills to seed");
            }

            Console.WriteLine("✓ GDD Framework seeding completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding GDD Framework: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Parse a CSV line handling quoted fields
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var parts = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                parts.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }
        parts.Add(current); // Add the last part

        return parts.ToArray();
    }

    #endregion
}

/// <summary>
/// CSV row mapping for GDD Framework CSV
/// </summary>
public class CsvRoleRow
{
    [CsvHelper.Configuration.Attributes.Name("Role Family")]
    public string? RoleFamily { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Role")]
    public string? Role { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Role Description")]
    public string? RoleDescription { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Role Level")]
    public string? RoleLevel { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Role Level Description")]
    public string? RoleLevelDescription { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Skill Name")]
    public string? SkillName { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Skill Description")]
    public string? SkillDescription { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Skill Level")]
    public string? SkillLevel { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Skill Level Description")]
    public string? SkillLevelDescription { get; set; }
    
    [CsvHelper.Configuration.Attributes.Name("Role Type")]
    public string? RoleType { get; set; }
}

