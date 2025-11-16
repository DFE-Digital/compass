using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public class PerformanceReportingEligibilityService : IPerformanceReportingEligibilityService
{
    private readonly CompassDbContext _context;

    public PerformanceReportingEligibilityService(CompassDbContext context)
    {
        _context = context;
    }

    public async Task<PerformanceReportingEligibilityCache> LoadEligibilityCacheAsync()
    {
        var cache = new PerformanceReportingEligibilityCache();
        
        // Load all active configurations sequentially (DbContext is not thread-safe)
        cache.PeriodExclusions = await _context.PerformanceReportingPeriodExclusions
            .Where(e => e.IsActive)
            .ToListAsync();
            
        cache.BusinessAreaConfigs = await _context.PerformanceReportingBusinessAreaConfigs
            .Where(c => c.IsActive)
            .ToListAsync();
            
        cache.ProductExclusions = await _context.PerformanceReportingProductExclusions
            .Where(e => e.IsActive)
            .ToListAsync();
        
        return cache;
    }

    public bool IsReportingRequired(string fipsId, string? businessArea, int year, int month, PerformanceReportingEligibilityCache cache)
    {
        // Step 1: Check if period is excluded at base level
        var periodExcluded = cache.PeriodExclusions.Any(e => e.Year == year && e.Month == month);
        
        // Step 2: Check if business area is configured to report (overrides period exclusion)
        var businessAreaReporting = false;
        if (!string.IsNullOrEmpty(businessArea))
        {
            businessAreaReporting = cache.BusinessAreaConfigs.Any(c => 
                c.BusinessAreaName == businessArea &&
                // Check if the period falls within the applicable range
                (c.ApplicableFromYear < year || 
                 (c.ApplicableFromYear == year && c.ApplicableFromMonth <= month)) &&
                (!c.ApplicableUntilYear.HasValue || 
                 c.ApplicableUntilYear.Value > year || 
                 (c.ApplicableUntilYear.Value == year && c.ApplicableUntilMonth >= month)));
        }
        
        // If period is excluded and business area is NOT configured to report, no reporting required
        if (periodExcluded && !businessAreaReporting)
        {
            return false;
        }
        
        // Step 3: Check if product is specifically excluded (final override)
        var productExcluded = cache.ProductExclusions.Any(e => 
            e.FipsId == fipsId &&
            // Check if the period falls within the exclusion range
            (e.ExclusionFromYear < year || 
             (e.ExclusionFromYear == year && e.ExclusionFromMonth <= month)) &&
            (!e.ExclusionUntilYear.HasValue || 
             e.ExclusionUntilYear.Value > year || 
             (e.ExclusionUntilYear.Value == year && e.ExclusionUntilMonth >= month)));
        
        if (productExcluded)
        {
            return false;
        }
        
        // If we get here, reporting is required
        return true;
    }

    public async Task<bool> IsReportingRequiredAsync(string fipsId, string? businessArea, int year, int month)
    {
        var cache = await LoadEligibilityCacheAsync();
        return IsReportingRequired(fipsId, businessArea, year, month, cache);
    }

    public async Task<bool> IsPeriodExcludedAsync(int year, int month)
    {
        var exclusion = await _context.PerformanceReportingPeriodExclusions
            .FirstOrDefaultAsync(e => e.Year == year && e.Month == month && e.IsActive);
        
        return exclusion != null;
    }

    public async Task<bool> IsBusinessAreaReportingAsync(string businessArea, int year, int month)
    {
        var config = await _context.PerformanceReportingBusinessAreaConfigs
            .Where(c => c.BusinessAreaName == businessArea && c.IsActive)
            .FirstOrDefaultAsync(c => 
                // Check if the period falls within the applicable range
                (c.ApplicableFromYear < year || 
                 (c.ApplicableFromYear == year && c.ApplicableFromMonth <= month)) &&
                (!c.ApplicableUntilYear.HasValue || 
                 c.ApplicableUntilYear.Value > year || 
                 (c.ApplicableUntilYear.Value == year && c.ApplicableUntilMonth >= month)));
        
        return config != null;
    }

    public async Task<bool> IsProductExcludedAsync(string fipsId, int year, int month)
    {
        var exclusion = await _context.PerformanceReportingProductExclusions
            .Where(e => e.FipsId == fipsId && e.IsActive)
            .FirstOrDefaultAsync(e => 
                // Check if the period falls within the exclusion range
                (e.ExclusionFromYear < year || 
                 (e.ExclusionFromYear == year && e.ExclusionFromMonth <= month)) &&
                (!e.ExclusionUntilYear.HasValue || 
                 e.ExclusionUntilYear.Value > year || 
                 (e.ExclusionUntilYear.Value == year && e.ExclusionUntilMonth >= month)));
        
        return exclusion != null;
    }
}

