using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class ServiceDataModelCompletionCalculatorTests
{
    private static ServiceDataModelFieldAnswerInput Field(
        string key,
        ServiceDataModelFieldType type,
        string? valueJson,
        bool mandatory = false,
        bool counts = true,
        string? visibility = null,
        bool isValid = true,
        Guid? id = null) =>
        new(
            id ?? Guid.NewGuid(),
            key,
            type,
            mandatory,
            counts,
            visibility,
            valueJson,
            isValid);

    [Fact]
    public void EmptyDenominator_IsNotApplicable_Not100()
    {
        var result = ServiceDataModelCompletionCalculator.Calculate(Array.Empty<ServiceDataModelFieldAnswerInput>());

        Assert.Null(result.FieldCompletionPercent);
        Assert.Null(result.MandatoryCompletionPercent);
        Assert.Equal(0, result.ApplicableCountingFields);
    }

    [Fact]
    public void ValidZeroAndExplicitNo_CountAsAnswers()
    {
        var fields = new[]
        {
            Field("count", ServiceDataModelFieldType.Number, "0"),
            Field("active", ServiceDataModelFieldType.YesNo, "false"),
        };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields);

        Assert.Equal(2, result.ApplicableCountingFields);
        Assert.Equal(2, result.AnsweredCountingFields);
        Assert.Equal(100m, result.FieldCompletionPercent);
    }

    [Fact]
    public void BlankAndInvalid_DoNotCountAsAnswers()
    {
        var fields = new[]
        {
            Field("a", ServiceDataModelFieldType.Text, null),
            Field("b", ServiceDataModelFieldType.Text, "\"\""),
            Field("c", ServiceDataModelFieldType.Text, "\"hello\""),
        };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields);

        Assert.Equal(3, result.ApplicableCountingFields);
        Assert.Equal(1, result.AnsweredCountingFields);
        Assert.Equal(33.3m, result.FieldCompletionPercent);
    }

    [Fact]
    public void HiddenConditionalFields_ExcludedFromNumeratorAndDenominator()
    {
        var fields = new[]
        {
            Field("has-url", ServiceDataModelFieldType.YesNo, "false"),
            Field(
                "url",
                ServiceDataModelFieldType.Url,
                null,
                mandatory: true,
                visibility: """{"fieldKey":"has-url","equals":"Yes"}"""),
        };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields);

        Assert.Equal(1, result.ApplicableCountingFields);
        Assert.Equal(1, result.AnsweredCountingFields);
        Assert.Equal(100m, result.FieldCompletionPercent);
        Assert.Equal(0, result.ApplicableMandatoryFields);
        Assert.Null(result.MandatoryCompletionPercent);
    }

    [Fact]
    public void VisibleConditionalMandatory_IncludedInMandatoryCompletion()
    {
        var fields = new[]
        {
            Field("has-url", ServiceDataModelFieldType.YesNo, "true"),
            Field(
                "url",
                ServiceDataModelFieldType.Url,
                null,
                mandatory: true,
                visibility: """{"fieldKey":"has-url","equals":"Yes"}"""),
        };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields);

        Assert.Equal(2, result.ApplicableCountingFields);
        Assert.Equal(1, result.AnsweredCountingFields);
        Assert.Equal(50m, result.FieldCompletionPercent);
        Assert.Equal(1, result.ApplicableMandatoryFields);
        Assert.Equal(0, result.AnsweredMandatoryFields);
        Assert.Equal(0m, result.MandatoryCompletionPercent);
    }

    [Fact]
    public void CountsTowardsCompletionFalse_ExcludedFromFieldPercent_StillInMandatory()
    {
        var fields = new[]
        {
            Field("notes", ServiceDataModelFieldType.Text, "\"x\"", counts: false),
            Field("owner", ServiceDataModelFieldType.Text, null, mandatory: true, counts: false),
            Field("name", ServiceDataModelFieldType.Text, "\"Service\""),
        };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields);

        Assert.Equal(1, result.ApplicableCountingFields);
        Assert.Equal(1, result.AnsweredCountingFields);
        Assert.Equal(100m, result.FieldCompletionPercent);
        Assert.Equal(1, result.ApplicableMandatoryFields);
        Assert.Equal(0, result.AnsweredMandatoryFields);
        Assert.Equal(0m, result.MandatoryCompletionPercent);
    }

    [Fact]
    public void IsOverdue_IsDerivedFlag_NotForTerminalStatuses()
    {
        var due = DateTime.UtcNow.AddDays(-1);
        Assert.True(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.InProgress, due, DateTime.UtcNow));
        Assert.False(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.Reviewed, due, DateTime.UtcNow));
        Assert.False(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.NotApplicable, due, DateTime.UtcNow));
        Assert.False(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.InProgress, null, DateTime.UtcNow));
    }

    [Fact]
    public void MissingDueDate_IsNeverOverdue()
    {
        Assert.False(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.NotStarted, null, DateTime.UtcNow));
        Assert.False(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.InProgress, null, DateTime.UtcNow.AddYears(1)));
        Assert.False(ServiceDataModelCompletionCalculator.IsOverdue(
            ServiceDataModelAssignmentStatus.Submitted, null, DateTime.UtcNow));
    }

    [Fact]
    public void DisabledFields_ExcludedFromCompletion()
    {
        var fields = new[]
        {
            Field("active", ServiceDataModelFieldType.Text, "\"yes\""),
            new ServiceDataModelFieldAnswerInput(
                Guid.NewGuid(),
                "retired-q",
                ServiceDataModelFieldType.Text,
                IsMandatory: true,
                CountsTowardsCompletion: true,
                VisibilityRuleJson: null,
                ValueJson: null,
                IsValid: true,
                IsDisabled: true),
        };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields);

        Assert.Equal(1, result.ApplicableCountingFields);
        Assert.Equal(1, result.AnsweredCountingFields);
        Assert.Equal(100m, result.FieldCompletionPercent);
        Assert.Equal(0, result.ApplicableMandatoryFields);
    }

    [Fact]
    public void GroupBreakdown_UsesSameRules()
    {
        var g1 = Guid.NewGuid();
        var g2 = Guid.NewGuid();
        var fields = new[]
        {
            Field("a", ServiceDataModelFieldType.Text, "\"1\"", id: g1),
            Field("b", ServiceDataModelFieldType.Text, null, id: g2),
        };
        var groups = new[] { ("identity", g1), ("ops", g2) };

        var result = ServiceDataModelCompletionCalculator.Calculate(fields, groups);

        Assert.Equal(100m, result.GroupBreakdown["identity"].FieldCompletionPercent);
        Assert.Equal(0m, result.GroupBreakdown["ops"].FieldCompletionPercent);
    }

    [Fact]
    public void AddingUnansweredCountingField_AfterPriorAnswers_LowersCompletion_AndKeepsOldAnswers()
    {
        var answeredId = Guid.NewGuid();
        var newId = Guid.NewGuid();
        var answered = Field(
            "existing",
            ServiceDataModelFieldType.Text,
            "\"kept-answer\"",
            id: answeredId);
        var before = ServiceDataModelCompletionCalculator.Calculate(new[] { answered });
        Assert.Equal(100m, before.FieldCompletionPercent);

        var afterAdd = ServiceDataModelCompletionCalculator.Calculate(new[]
        {
            answered,
            Field("new-question", ServiceDataModelFieldType.Text, null, id: newId)
        });

        Assert.Equal(2, afterAdd.ApplicableCountingFields);
        Assert.Equal(1, afterAdd.AnsweredCountingFields);
        Assert.Equal(50m, afterAdd.FieldCompletionPercent);
        Assert.Equal("\"kept-answer\"", answered.ValueJson);
    }
}

/// <summary>
/// Guards the access model for Service Data Models / Census via controller attribute wiring.
/// </summary>
public class ServiceDataModelAuthorizationAttributeTests
{
    [Fact]
    public void AdminServiceDataModelsController_RequiresAuthorizeAndRequireAdmin()
    {
        var type = typeof(Compass.Controllers.Modern.ModernAdminServiceDataModelsController);
        Assert.Contains(type.GetCustomAttributes(inherit: true), a => a is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
        Assert.Contains(type.GetCustomAttributes(inherit: true), a => a.GetType().Name == "RequireAdminAttribute");
    }

    [Fact]
    public void CensusController_RequiresAuthorize_ButNotRequireAdmin()
    {
        var type = typeof(Compass.Controllers.Modern.ModernCensusController);
        Assert.Contains(type.GetCustomAttributes(inherit: true), a => a is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
        Assert.DoesNotContain(type.GetCustomAttributes(inherit: true), a => a.GetType().Name == "RequireAdminAttribute");
    }

    [Fact]
    public void ReportingController_RequiresAuthorize_CensusActionsPresent()
    {
        var type = typeof(Compass.Controllers.Modern.ModernReportingController);
        Assert.Contains(type.GetCustomAttributes(inherit: true), a => a is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
        Assert.NotNull(type.GetMethod("ServiceCensusOverview"));
        Assert.NotNull(type.GetMethod("ExportServiceCensusOverview"));
        Assert.NotNull(type.GetMethod("ServiceCensusAnalysis"));
        Assert.NotNull(type.GetMethod("ServiceCensus"));
        Assert.NotNull(type.GetMethod("Reports"));
    }
}

/// <summary>
/// Guards the standing Service Census seed defaults (not annual / period-based).
/// </summary>
public class ServiceCensusDefaultsTests
{
    [Fact]
    public void SeedDefaults_AreStandingNotPeriodBased()
    {
        Assert.Equal("service-census", ServiceCensusDefaults.StableKey);
        Assert.Equal("Service Census", ServiceCensusDefaults.Name);
        Assert.False(ServiceCensusDefaults.IsRepeatable);
        Assert.Null(ServiceCensusDefaults.DefaultDueDaysAfterPublish);
        Assert.Null(ServiceCensusDefaults.ReviewCadenceLabel);
        Assert.DoesNotContain("Annual service", ServiceCensusDefaults.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("standing", ServiceCensusDefaults.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("any time", ServiceCensusDefaults.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not an annual", ServiceCensusDefaults.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeedDefaults_RemainReportableWithReviewerAttestation()
    {
        Assert.True(ServiceCensusDefaults.IsReportable);
        Assert.True(ServiceCensusDefaults.RequiresReviewerAttestation);
        Assert.Equal("Official", ServiceCensusDefaults.Classification);
    }
}
