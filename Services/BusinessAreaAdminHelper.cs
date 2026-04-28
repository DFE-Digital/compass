using Compass.Models;

namespace Compass.Services;

/// <summary>Resolve Compass business area ids from entities for permission checks.</summary>
public static class BusinessAreaAdminHelper
{
    public static IReadOnlyList<int> GetBusinessAreaLookupIdsForIssue(Issue issue)
    {
        var ids = new HashSet<int>();
        if (issue.Project?.BusinessAreaId is int pb)
            ids.Add(pb);
        foreach (var iba in issue.IssueBusinessAreas)
            ids.Add(iba.BusinessAreaLookupId);
        return ids.Count > 0 ? ids.ToList() : Array.Empty<int>();
    }

    /// <summary>Primary <see cref="Project.BusinessAreaId"/> for work item permission checks.</summary>
    public static IReadOnlyList<int> GetBusinessAreaLookupIdsForProject(Project project)
    {
        if (project.BusinessAreaId is int id && id > 0)
            return new[] { id };
        return Array.Empty<int>();
    }
}
