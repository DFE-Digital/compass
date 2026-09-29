using Compass.Models;
using Compass.Services;
using Xunit;

namespace Compass.Tests;

public class PerformanceReportingProductExclusionTests
{
    private static readonly PerformanceReportingEligibilityService Eligibility =
        new(null!);

    [Fact]
    public void ServiceRegisterRowId_MatchesCatalogueProductByCmdbSysId()
    {
        const string rowId = "49f50c8a-cf3b-48c6-b5e8-b13d44fbe7ba";
        const string sysId = "abc123sysid";

        var cache = CacheWith(new PerformanceReportingProductExclusion
        {
            ProductDocumentId = rowId,
            FipsId = sysId,
            ExclusionFromYear = 2025,
            ExclusionFromMonth = 1,
            IsActive = true
        }, rowId, sysId);

        var product = new ProductDto
        {
            Title = "LRS Compatibility",
            DocumentId = "cms-document-id",
            FipsId = "FIPS-49",
            CmdbSysId = sysId
        };

        Assert.True(Eligibility.IsProductExcludedForCommission(product, CommissionIn(2026, 4), cache));
    }

    [Fact]
    public void StoredSysIdInFipsId_MatchesWhenCmdbRowMapIsMissing()
    {
        const string sysId = "abc123sysid";
        var cache = CacheWith(new PerformanceReportingProductExclusion
        {
            ProductDocumentId = "49f50c8a-cf3b-48c6-b5e8-b13d44fbe7ba",
            FipsId = sysId,
            ExclusionFromYear = 2025,
            ExclusionFromMonth = 1,
            IsActive = true
        });

        var product = new ProductDto
        {
            DocumentId = "cms-document-id",
            FipsId = "FIPS-49",
            CmdbSysId = sysId
        };

        Assert.True(Eligibility.IsProductExcluded(product.DocumentId, product.FipsId, 2026, 4, cache, product.CmdbSysId));
    }

    [Fact]
    public void CmsDocumentId_StillMatches()
    {
        var cache = CacheWith(new PerformanceReportingProductExclusion
        {
            ProductDocumentId = "cms-document-id",
            FipsId = "FIPS-49",
            ExclusionFromYear = 2025,
            ExclusionFromMonth = 1,
            IsActive = true
        });

        Assert.True(Eligibility.IsProductExcluded("cms-document-id", "OTHER", 2026, 4, cache, "different-sys"));
    }

    [Fact]
    public void DifferentProduct_IsNotExcluded()
    {
        var cache = CacheWith(new PerformanceReportingProductExclusion
        {
            ProductDocumentId = "49f50c8a-cf3b-48c6-b5e8-b13d44fbe7ba",
            FipsId = "abc123sysid",
            ExclusionFromYear = 2025,
            ExclusionFromMonth = 1,
            IsActive = true
        }, "49f50c8a-cf3b-48c6-b5e8-b13d44fbe7ba", "abc123sysid");

        var product = new ProductDto
        {
            DocumentId = "other-document",
            FipsId = "FIPS-99",
            CmdbSysId = "other-sys"
        };

        Assert.False(Eligibility.IsProductExcludedForCommission(product, CommissionIn(2026, 4), cache));
    }

    [Fact]
    public void ExclusionStarting2025_DoesNotApplyToEarlierCommission()
    {
        const string sysId = "abc123sysid";
        var cache = CacheWith(new PerformanceReportingProductExclusion
        {
            ProductDocumentId = "49f50c8a-cf3b-48c6-b5e8-b13d44fbe7ba",
            FipsId = sysId,
            ExclusionFromYear = 2025,
            ExclusionFromMonth = 1,
            IsActive = true
        }, "49f50c8a-cf3b-48c6-b5e8-b13d44fbe7ba", sysId);

        var product = new ProductDto { DocumentId = "cms", FipsId = "FIPS-49", CmdbSysId = sysId };

        Assert.False(Eligibility.IsProductExcludedForCommission(product, CommissionIn(2024, 10), cache));
    }

    private static Commission CommissionIn(int year, int month) => new()
    {
        StartDate = new DateTime(year, month, 1),
        EndDate = new DateTime(year, month, 28),
        OpenDate = new DateTime(year, month, 1),
        DueDate = new DateTime(year, month, 28)
    };

    private static PerformanceReportingEligibilityCache CacheWith(
        PerformanceReportingProductExclusion exclusion,
        string? cmdbRowId = null,
        string? sysId = null)
    {
        var cache = new PerformanceReportingEligibilityCache
        {
            ProductExclusions = [exclusion]
        };
        if (cmdbRowId != null && sysId != null)
            cache.CmdbRowIdToSysId[cmdbRowId] = sysId;
        return cache;
    }
}
