using Compass.Models;
using Compass.Services;
using Xunit;

namespace Compass.Tests;

public class CommissionReportingProductScopeTests
{
    [Theory]
    [InlineData("Delivery Manager")]
    [InlineData("delivery manager")]
    [InlineData("Service Owner")]
    [InlineData("Product manager")]
    [InlineData("Reporting contact")]
    public void ReportingContactRoles_AreRecognised(string role) =>
        Assert.True(CommissionReportingProductScope.IsPerformanceReportingContactRole(role));

    [Theory]
    [InlineData("Information Asset Owner")]
    [InlineData("Senior Responsible Officer")]
    [InlineData(null)]
    public void OtherNamedRoles_DoNotGrantReporting(string? role) =>
        Assert.False(CommissionReportingProductScope.IsPerformanceReportingContactRole(role));

    [Fact]
    public void Combine_KeepsProductThatHasDocumentIdButNoFipsId()
    {
        var product = new ProductDto
        {
            Title = "LRS Compatibility",
            DocumentId = "cms-doc",
            CmdbSysId = "sys-1",
            State = "Active"
        };

        var combined = CommissionReportingProductScope.CombineUserReportingProducts(new[] { product });

        Assert.Single(combined);
        Assert.Equal("cms-doc", combined[0].DocumentId);
    }
}
