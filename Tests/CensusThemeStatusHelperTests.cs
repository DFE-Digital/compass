using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class CensusThemeStatusHelperTests
{
    [Fact]
    public void Resolve_NotStarted_WhenNoAnswersAndNotMarkedComplete()
    {
        var result = CensusThemeStatusHelper.Resolve(
            isMarkedComplete: false,
            answeredCountingFields: 0,
            applicableCountingFields: 3);

        Assert.Equal(CensusThemeStatusHelper.NotStarted, result.StatusLabel);
        Assert.Equal("Start", result.ActionLabel);
        Assert.Equal(CensusThemeStatusHelper.TagGrey, result.TagClass);
    }

    [Fact]
    public void Resolve_InProgress_WhenSomeAnswersAndNotMarkedComplete()
    {
        var result = CensusThemeStatusHelper.Resolve(
            isMarkedComplete: false,
            answeredCountingFields: 2,
            applicableCountingFields: 5);

        Assert.Equal(CensusThemeStatusHelper.InProgress, result.StatusLabel);
        Assert.Equal("Continue", result.ActionLabel);
        Assert.Equal(CensusThemeStatusHelper.TagBlue, result.TagClass);
    }

    [Fact]
    public void Resolve_Complete_WhenMarkedComplete_EvenIfNotAllAnswered()
    {
        var result = CensusThemeStatusHelper.Resolve(
            isMarkedComplete: true,
            answeredCountingFields: 1,
            applicableCountingFields: 4);

        Assert.Equal(CensusThemeStatusHelper.Complete, result.StatusLabel);
        Assert.Equal("Review", result.ActionLabel);
        Assert.Equal(CensusThemeStatusHelper.TagGreen, result.TagClass);
    }

    [Fact]
    public void Resolve_InProgress_WhenFullyAnsweredButNotMarkedComplete()
    {
        var result = CensusThemeStatusHelper.Resolve(
            isMarkedComplete: false,
            answeredCountingFields: 4,
            applicableCountingFields: 4);

        Assert.Equal(CensusThemeStatusHelper.InProgress, result.StatusLabel);
        Assert.Equal(CensusThemeStatusHelper.TagBlue, result.TagClass);
    }

    [Fact]
    public void Resolve_NotApplicable_WhenNoCountingFields()
    {
        var result = CensusThemeStatusHelper.Resolve(
            isMarkedComplete: false,
            answeredCountingFields: 0,
            applicableCountingFields: 0);

        Assert.Equal(CensusThemeStatusHelper.NotApplicable, result.StatusLabel);
        Assert.Equal(CensusThemeStatusHelper.TagGrey, result.TagClass);
    }
}
