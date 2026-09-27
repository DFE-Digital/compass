using Compass.Models.ServiceDataModels;
using Compass.Services.ServiceDataModels;
using Xunit;

namespace Compass.Tests;

public class ServiceDataModelStructureVersionTests
{
    [Fact]
    public void Resolve_PrefersPublished_OverDraft()
    {
        var published = new ServiceDataModelVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = 1,
            Status = ServiceDataModelLifecycleStatus.Published
        };
        var draft = new ServiceDataModelVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = 2,
            Status = ServiceDataModelLifecycleStatus.Draft
        };

        var resolved = ServiceDataModelStructureVersion.Resolve(published, draft);

        Assert.Same(published, resolved);
    }

    [Fact]
    public void Resolve_FallsBackToDraft_WhenNothingPublished()
    {
        var draft = new ServiceDataModelVersion
        {
            Id = Guid.NewGuid(),
            VersionNumber = 1,
            Status = ServiceDataModelLifecycleStatus.Draft
        };

        var resolved = ServiceDataModelStructureVersion.Resolve(null, draft);

        Assert.Same(draft, resolved);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoVersions()
    {
        Assert.Null(ServiceDataModelStructureVersion.Resolve(null, null));
    }

    [Theory]
    [InlineData(ServiceDataModelLifecycleStatus.Draft, true)]
    [InlineData(ServiceDataModelLifecycleStatus.Published, true)]
    [InlineData(ServiceDataModelLifecycleStatus.Retired, false)]
    public void AllowsEdits_OnlyDraftOrPublished(ServiceDataModelLifecycleStatus status, bool expected)
    {
        Assert.Equal(expected, ServiceDataModelStructureVersion.AllowsEdits(status));
    }
}
