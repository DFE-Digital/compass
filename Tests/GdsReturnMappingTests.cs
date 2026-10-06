using Compass.Models;
using Compass.Services.Modern;
using Xunit;

namespace Compass.Tests;

public class GdsReturnMappingTests
{
    [Theory]
    [InlineData("Q1", 2026, 4, 1, "Q1_2026/27")]
    [InlineData("Q4", 2026, 1, 1, "Q4_2025/26")]
    [InlineData(null, 2025, 10, 1, "Q3_2025/26")]
    [InlineData("", 2026, 7, 1, "Q2_2026/27")]
    public void FormatGdsQuarter_UsesUkFinancialYear(
        string? quarter, int year, int month, int day, string expected)
    {
        var commission = new Commission
        {
            Name = "Test",
            Quarter = quarter,
            StartDate = new DateTime(year, month, day),
            EndDate = new DateTime(year, month, day).AddMonths(2),
            OpenDate = new DateTime(year, month, day),
            DueDate = new DateTime(year, month, day).AddMonths(3)
        };

        Assert.Equal(expected, GdsReturnMapping.FormatGdsQuarter(commission));
    }

    [Theory]
    [InlineData("Transactional", "Yes")]
    [InlineData("Non-transactional", "No")]
    [InlineData("Website", "")]
    public void MapTransactionalOrNot_FromTypeCategory(string typeName, string expected)
    {
        var product = new ProductDto
        {
            Title = "Example",
            CategoryValues =
            [
                new CategoryValueDto
                {
                    Name = typeName,
                    CategoryType = new CategoryTypeDto { Name = "Type" }
                }
            ]
        };

        Assert.Equal(expected, GdsReturnMapping.MapTransactionalOrNot(product));
    }

    [Theory]
    [InlineData("Live", "Live")]
    [InlineData("Public Beta", "Public beta")]
    [InlineData("Private beta", "Private beta")]
    [InlineData("Alpha", "Alpha")]
    [InlineData("Decommissioned", "Retired")]
    [InlineData("Discovery", "")]
    public void MapServiceStatus_FromPhase(string phase, string expected)
    {
        Assert.Equal(expected, GdsReturnMapping.MapServiceStatus(new ProductDto { Phase = phase }));
    }

    [Fact]
    public void MapChannelAvailable_ReturnsYesOrNoWhenChannelsPresent()
    {
        var channels = new[] { "Online", "Telephone" };
        Assert.Equal("Yes", GdsReturnMapping.MapChannelAvailable(channels, "online", "web"));
        Assert.Equal("Yes", GdsReturnMapping.MapChannelAvailable(channels, "telephone", "phone"));
        Assert.Equal("No", GdsReturnMapping.MapChannelAvailable(channels, "post", "postal"));
    }

    [Fact]
    public void NormalizeAccessibilityCompliance_HandlesNumericAndText()
    {
        Assert.Equal("Yes", GdsReturnMapping.NormalizeAccessibilityCompliance("0"));
        Assert.Equal("No", GdsReturnMapping.NormalizeAccessibilityCompliance("3"));
        Assert.Equal("Yes", GdsReturnMapping.NormalizeAccessibilityCompliance("Yes"));
        Assert.Equal("No", GdsReturnMapping.NormalizeAccessibilityCompliance("No"));
    }

    [Fact]
    public void MapServicePurpose_PrefersRegisterUserDescription()
    {
        var product = new ProductDto
        {
            ShortDescription = "CMS short",
            LongDescription = "CMS long"
        };

        Assert.Equal(
            "Register user description",
            GdsReturnMapping.MapServicePurpose("Register user description", "CMDB description", product));
        Assert.Equal(
            "CMDB description",
            GdsReturnMapping.MapServicePurpose(null, "CMDB description", product));
        Assert.Equal(
            "CMS short",
            GdsReturnMapping.MapServicePurpose(null, null, product));
    }

    [Fact]
    public void MapServiceOwner_PrefersRegisterContactThenCms()
    {
        var product = new ProductDto
        {
            ServiceOwners =
            [
                new EntraUserDto { DisplayName = "CMS Owner", EmailAddress = "cms@example.com" }
            ]
        };

        Assert.Equal("Register Owner", GdsReturnMapping.MapServiceOwner("Register Owner", product));
        Assert.Equal("CMS Owner", GdsReturnMapping.MapServiceOwner(null, product));
        Assert.Equal("", GdsReturnMapping.MapServiceOwner(null, null));
    }

    [Fact]
    public void BuildServiceMetricColumnMap_PrefersKnownIdentifiers()
    {
        var metrics = new List<PerformanceMetric>
        {
            new() { Id = 1, Identifier = "perf-1", Title = "USAT total", Description = "", ValidationRules = "{}" },
            new() { Id = 2, Identifier = "perf-2", Title = "Completed digital", Description = "", ValidationRules = "{}" },
            new() { Id = 6, Identifier = "perf-6", Title = "Completed transactions", Description = "", ValidationRules = "{}" }
        };

        var map = GdsReturnMapping.BuildServiceMetricColumnMap(metrics);

        Assert.Equal(1, map["usat_total_number_of_responses"]!.Id);
        Assert.Equal(2, map["completed_digital_transactions"]!.Id);
        Assert.Equal(6, map["completed_transactions"]!.Id);
    }

    [Fact]
    public void ContextAndServiceColumns_MatchTemplateOrderAndNames()
    {
        Assert.Equal(
            new[]
            {
                "service_name", "transactional_or_not", "service_status", "internal_or_external",
                "service_purpose", "service_users", "service_owner", "service_manager", "service_usage",
                "payment_required", "onelogin_available", "online_available", "telephone_available",
                "post_available", "inperson_available", "user_satisfaction_method", "digital_adoption_method",
                "digital_completion_method", "cost_per_transaction_method", "service_kpi", "kpi_value", "comments"
            },
            GdsReturnMapping.ContextDataColumns);

        Assert.Equal(
            new[]
            {
                "quarter", "service_name", "started_transactions", "completed_transactions",
                "started_digital_transactions", "completed_digital_transactions",
                "usat_total_number_of_responses", "usat_number_of_satisfied_responses",
                "usat_number_of_very_satisfied_responses", "accessibility_compliance",
                "cost_per_transaction_amount", "categories_included_in_cpt", "fte_count",
                "govuk_url", "comments"
            },
            GdsReturnMapping.ServiceDataColumns);
    }
}
