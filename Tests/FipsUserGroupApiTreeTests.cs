using Compass.Models.Fips;
using Compass.Services.Fips;
using Xunit;

namespace Compass.Tests;

public class FipsUserGroupApiTreeTests
{
    [Fact]
    public void Build_IncludesGrandchildrenUnderTheirParent()
    {
        var workforce = Group(13, "Social care workforce", parentId: null, displayOrder: 110);
        var careHome = Group(14, "Children's care home workforce", parentId: 13, displayOrder: 10);
        var socialWorker = Group(20, "Social worker", parentId: 13, displayOrder: 80);
        var chief = Group(21, "Chief Social Worker for Children and Families", parentId: 20, displayOrder: 10);

        var tree = FipsUserGroupApiTree.Build([careHome, chief, socialWorker, workforce]);

        var root = Assert.Single(tree);
        Assert.Equal("Social care workforce", root.Name);
        Assert.Null(root.ParentId);
        Assert.Equal(["Children's care home workforce", "Social worker"], root.Children.Select(c => c.Name).ToArray());

        var worker = root.Children.Single(c => c.Name == "Social worker");
        Assert.Equal(13, worker.ParentId);
        var chiefNode = Assert.Single(worker.Children);
        Assert.Equal(21, chiefNode.Id);
        Assert.Equal(20, chiefNode.ParentId);
        Assert.Equal("Chief Social Worker for Children and Families", chiefNode.Name);
        Assert.Empty(chiefNode.Children);
        Assert.Same(root.Children, root.ChildGroups);
        Assert.Same(worker.Children, worker.ChildGroups);
        Assert.Equal(4, Flatten(tree).Count());
    }

    [Fact]
    public void Build_KeepsGroupsWhoseParentIsMissing()
    {
        var orphan = Group(30, "Practice leader (social worker)", parentId: 999, displayOrder: 30);
        var child = Group(31, "Newly qualified social worker", parentId: 30, displayOrder: 10);

        var tree = FipsUserGroupApiTree.Build([child, orphan]);

        var root = Assert.Single(tree);
        Assert.Equal(30, root.Id);
        Assert.Equal(999, root.ParentId);
        var nested = Assert.Single(root.Children);
        Assert.Equal(31, nested.Id);
        Assert.Equal(30, nested.ParentId);
    }

    [Fact]
    public void Build_StopsWhenTheTreeCyclesBackToAnAncestor()
    {
        var root = Group(1, "Root", parentId: null, displayOrder: 1);
        var child = Group(2, "Child", parentId: 1, displayOrder: 1);
        var backToRoot = Group(1, "Root", parentId: 2, displayOrder: 1);

        var tree = FipsUserGroupApiTree.Build([root, child, backToRoot]);

        var rootNode = Assert.Single(tree);
        var childNode = Assert.Single(rootNode.Children);
        Assert.Equal("Child", childNode.Name);
        var repeatedRoot = Assert.Single(childNode.Children);
        Assert.Equal(1, repeatedRoot.Id);
        Assert.Empty(repeatedRoot.Children);
    }

    private static IEnumerable<FipsUserGroupApiTree.FipsUserGroupApiRow> Flatten(
        IEnumerable<FipsUserGroupApiTree.FipsUserGroupApiRow> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in Flatten(node.Children))
                yield return child;
        }
    }

    private static FipsUserGroup Group(int id, string name, int? parentId, int displayOrder) =>
        new()
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            DisplayOrder = displayOrder,
            Active = true
        };
}
