using Compass.Helpers;
using Compass.Models;
using Xunit;

namespace Compass.Tests;

public class ProjectDirectorateHelperTests
{
    [Fact]
    public void SetPrimaryDirectorate_IsSingleSelect_AndPreservesSurplusRows()
    {
        var project = new Project { Id = 1 };
        project.Directorates.Add(new ProjectDirectorate { Id = 10, ProjectId = 1, DivisionId = 100, IsPrimary = true, CreatedAt = DateTime.UtcNow.AddDays(-2) });
        project.Directorates.Add(new ProjectDirectorate { Id = 11, ProjectId = 1, DivisionId = 200, IsPrimary = false, CreatedAt = DateTime.UtcNow.AddDays(-1) });

        ProjectDirectorateHelper.SetPrimaryDirectorate(project, 200);

        Assert.Equal(2, project.Directorates.Count);
        Assert.Single(project.Directorates, d => d.IsPrimary);
        Assert.Equal(200, ProjectDirectorateHelper.GetPrimaryDivisionId(project));
        Assert.Contains(project.Directorates, d => d.DivisionId == 100 && !d.IsPrimary);
    }

    [Fact]
    public void ApplySelectedDirectorateIds_UsesFirstAsPrimary_WithoutDroppingExtras()
    {
        var project = new Project { Id = 2 };

        ProjectDirectorateHelper.ApplySelectedDirectorateIds(project, new[] { 5, 6, 7 });

        Assert.Equal(3, project.Directorates.Count);
        Assert.Equal(5, ProjectDirectorateHelper.GetPrimaryDivisionId(project));
        Assert.Equal(2, project.Directorates.Count(d => !d.IsPrimary));
    }

    [Fact]
    public void SetPrimaryDirectorate_Clear_DoesNotDeleteRows()
    {
        var project = new Project { Id = 3 };
        project.Directorates.Add(new ProjectDirectorate { Id = 1, ProjectId = 3, DivisionId = 9, IsPrimary = true });

        ProjectDirectorateHelper.SetPrimaryDirectorate(project, null);

        Assert.Single(project.Directorates);
        Assert.False(project.Directorates.First().IsPrimary);
        Assert.Null(ProjectDirectorateHelper.GetPrimaryDivisionId(project));
        Assert.Equal(string.Empty, WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project));
        Assert.Equal("Division Nine", WorkStrategicAlignmentExport.GetAdditionalDirectorateNames(
            new Project
            {
                Directorates =
                {
                    new ProjectDirectorate
                    {
                        DivisionId = 9,
                        IsPrimary = false,
                        Division = new Division { Name = "Division Nine" }
                    }
                }
            }));
    }
}

public class WorkStrategicAlignmentExportTests
{
    private static Project BuildProject()
    {
        var missionA = new Mission { Id = 1, Title = "Mission Alpha", Theme = "Skills" };
        var missionB = new Mission { Id = 2, Title = "Mission Beta", Theme = "Growth" };
        var outcomeA = new Objective { Id = 1, Title = "Outcome One", Theme = "Skills" };
        var outcomeB = new Objective { Id = 2, Title = "Outcome Two", Theme = "Inclusion" };
        var dirPrimary = new Division { Id = 1, Name = "Education Estates" };
        var dirExtra = new Division { Id = 2, Name = "Digital" };

        return new Project
        {
            Id = 42,
            Directorates =
            {
                new ProjectDirectorate { Id = 1, DivisionId = 1, Division = dirPrimary, IsPrimary = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
                new ProjectDirectorate { Id = 2, DivisionId = 2, Division = dirExtra, IsPrimary = false, CreatedAt = DateTime.UtcNow.AddDays(-1) }
            },
            ProjectMissions =
            {
                new ProjectMission { MissionId = 1, Mission = missionA },
                new ProjectMission { MissionId = 2, Mission = missionB }
            },
            ProjectObjectives =
            {
                new ProjectObjective { ObjectiveId = 1, Objective = outcomeA },
                new ProjectObjective { ObjectiveId = 2, Objective = outcomeB }
            }
        };
    }

    [Fact]
    public void Export_ContainsDirectorate_AsPrimaryReadableName()
    {
        var project = BuildProject();
        Assert.Equal("Education Estates", WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project));
        Assert.Equal("Digital", WorkStrategicAlignmentExport.GetAdditionalDirectorateNames(project));
    }

    [Fact]
    public void Export_ContainsMissionPillars_AllMapped()
    {
        var project = BuildProject();
        var value = WorkStrategicAlignmentExport.GetMissionPillarNames(project);
        Assert.Contains("Mission Alpha", value);
        Assert.Contains("Mission Beta", value);
        Assert.Contains(WorkStrategicAlignmentExport.MultiValueDelimiter, value);
    }

    [Fact]
    public void Export_ContainsPriorityOutcomes_AllMapped()
    {
        var project = BuildProject();
        var value = WorkStrategicAlignmentExport.GetPriorityOutcomeNames(project);
        Assert.Contains("Outcome One", value);
        Assert.Contains("Outcome Two", value);
    }

    [Fact]
    public void Export_ContainsThematicTags_DistinctThemes()
    {
        var project = BuildProject();
        var value = WorkStrategicAlignmentExport.GetThematicTagNames(project);
        Assert.Contains("Skills", value);
        Assert.Contains("Growth", value);
        Assert.Contains("Inclusion", value);
        // Skills appears on mission and outcome — should not duplicate
        Assert.Equal(1, value.Split(new[] { WorkStrategicAlignmentExport.MultiValueDelimiter }, StringSplitOptions.None)
            .Count(v => v.Equals("Skills", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void Export_MultipleValues_UseConsistentDelimiter()
    {
        Assert.Equal("; ", WorkStrategicAlignmentExport.MultiValueDelimiter);
        Assert.Equal("A; B", WorkStrategicAlignmentExport.JoinMultiValues(new[] { "A", "B" }));
    }

    [Fact]
    public void Export_UnmappedStrategicFields_AreEmpty_NotNullBreaking()
    {
        var project = new Project { Id = 99 };
        Assert.Equal(string.Empty, WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project));
        Assert.Equal(string.Empty, WorkStrategicAlignmentExport.GetAdditionalDirectorateNames(project));
        Assert.Equal(string.Empty, WorkStrategicAlignmentExport.GetMissionPillarNames(project));
        Assert.Equal(string.Empty, WorkStrategicAlignmentExport.GetPriorityOutcomeNames(project));
        Assert.Equal(string.Empty, WorkStrategicAlignmentExport.GetThematicTagNames(project));
    }

    [Fact]
    public void DirectorateReferenceData_UsesDivisionName_NotStaleLookup()
    {
        // Simulates Admin renaming Division — export reads Division.Name (source of truth)
        var project = new Project
        {
            Directorates =
            {
                new ProjectDirectorate
                {
                    IsPrimary = true,
                    Division = new Division { Id = 1, Name = "Education Estates" }
                }
            }
        };

        Assert.Equal("Education Estates", WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project));
        project.Directorates.First().Division!.Name = "Education Estates and Net Zero Directorate";
        Assert.Equal("Education Estates and Net Zero Directorate", WorkStrategicAlignmentExport.GetPrimaryDirectorateName(project));
    }

    [Fact]
    public void ExistingExportColumnNames_RemainDefined()
    {
        Assert.Equal("Directorate", WorkStrategicAlignmentExport.DirectorateColumn);
        Assert.Equal("Mission Pillars", WorkStrategicAlignmentExport.MissionPillarsColumn);
        Assert.Equal("Priority Outcomes", WorkStrategicAlignmentExport.PriorityOutcomesColumn);
        Assert.Equal("Thematic Tags", WorkStrategicAlignmentExport.ThematicTagsColumn);
        Assert.Equal("Additional Directorates", WorkStrategicAlignmentExport.AdditionalDirectoratesColumn);
    }
}
