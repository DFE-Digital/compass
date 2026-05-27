using Compass.Controllers.Modern;
using Compass.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

namespace Compass.Helpers;

/// <summary>Maps legacy <c>/Project/*</c> GET requests to modern work UI routes under <c>/modern/work/*</c>.</summary>
public static class ProjectLegacyRedirectMap
{
    public static async Task<IActionResult?> TryBuildRedirectAsync(
        ActionExecutingContext context,
        IUrlHelper urlHelper,
        CompassDbContext db,
        CancellationToken cancellationToken = default)
    {
        var action = context.RouteData.Values["action"]?.ToString();
        if (string.IsNullOrEmpty(action))
            return null;

        var routeValues = context.ActionArguments;

        return action switch
        {
            "Index" or "YourWork" => PermanentRedirect(urlHelper, nameof(ModernWorkController.AllWork), "ModernWork", new { mine = true }),
            "Watched" => PermanentRedirect(urlHelper, nameof(ModernWorkController.Watching), "ModernWork", null),
            "All" or "ViewAs" or "ViewAsUser" => PermanentRedirect(urlHelper, nameof(ModernWorkController.AllWork), "ModernWork", CopyListFilters(routeValues)),
            "ExportUserProjects" => PermanentRedirect(urlHelper, nameof(ModernWorkController.ExportRegister), "ModernWork", new { scope = "allwork", mine = true }),
            "ExportWatchedProjects" => PermanentRedirect(urlHelper, nameof(ModernWorkController.ExportRegister), "ModernWork", new { scope = "watching" }),
            "ExportAllProjects" => PermanentRedirect(urlHelper, nameof(ModernWorkController.ExportRegister), "ModernWork", new { scope = "allwork" }),
            "Create" => PermanentRedirect(urlHelper, nameof(ModernWorkController.Create), "ModernWork", CopyBusinessCaseId(routeValues)),
            "Details" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "id"), GetString(routeValues, "tab"), GetString(routeValues, "milestonestab")),
            "Edit" or "EditDeliveryPhases" => PermanentRedirect(urlHelper, nameof(ModernWorkController.Edit), "ModernWork", new { id = RequireInt(routeValues, "id") }),
            "Delete" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "id"), "overview", null),
            "StrategicAlignment" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "id"), "strategicalignment", null),
            "EditPriorityOutcomes" or "EditMissionPillars" => PermanentRedirect(urlHelper, nameof(ModernWorkController.EditStrategicAlignment), "ModernWork", new { id = RequireInt(routeValues, "projectId") }),
            "CreateTeamMember" or "AddTeamMember" => PermanentRedirect(urlHelper, nameof(ModernWorkController.AddTeamMember), "ModernWork", new { id = RequireInt(routeValues, "projectId") }),
            "EditTeamMember" or "TeamMemberDetails" or "RemoveTeamMember" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "contacts", null),
            "CreateProduct" or "CreateProductConfirmation" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "serviceregister", null),
            "CreateStatusUpdate" or "StatusUpdateDetails" or "EditStatusUpdate" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "updates", null),
            "CreateArtefact" or "ArtefactDetails" or "EditArtefact" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "overview", null),
            "CreateMilestone" or "AddMilestone" => PermanentRedirect(urlHelper, nameof(ModernWorkController.AddMilestone), "ModernWork", new { id = RequireInt(routeValues, "projectId") }),
            "EditMilestone" or "UpdateMilestone" => PermanentRedirect(urlHelper, nameof(ModernWorkController.EditMilestone), "ModernWork", new { id = RequireInt(routeValues, "projectId"), milestoneId = RequireInt(routeValues, "milestoneId") }),
            "CreateRisk" or "AddRisk" => PermanentRedirect(urlHelper, nameof(ModernWorkController.LogRisk), "ModernWork", new { id = RequireInt(routeValues, "projectId") }),
            "CreateIssue" or "AddIssue" => PermanentRedirect(urlHelper, nameof(ModernWorkController.LogIssue), "ModernWork", new { id = RequireInt(routeValues, "projectId") }),
            "EditIssue" => PermanentRedirect(urlHelper, nameof(ModernWorkController.IssueDetail), "ModernWork", new { workId = RequireInt(routeValues, "projectId"), id = RequireInt(routeValues, "issueId") }),
            "CreateAction" or "EditAction" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "overview", null),
            "CreateKpi" or "EditKpi" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "overview", null),
            "CreateOutcome" or "EditOutcome" or "AddOutcome" or "UpdateOutcome" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "overview", null),
            "CreateSuccess" or "EditSuccess" or "SuccessDetails" => RedirectToWorkDetail(urlHelper, RequireInt(routeValues, "projectId"), "updates", null),
            "MilestoneDetails" => await RedirectFromMilestoneAsync(urlHelper, db, RequireInt(routeValues, "id"), cancellationToken),
            "OutcomeDetails" => await RedirectFromOutcomeAsync(urlHelper, db, RequireInt(routeValues, "id"), cancellationToken),
            _ => TryFallbackRedirect(urlHelper, action, routeValues)
        };
    }

    private static IActionResult? TryFallbackRedirect(IUrlHelper urlHelper, string action, IDictionary<string, object?> routeValues)
    {
        if (TryGetProjectId(routeValues, out var projectId))
        {
            if (action.Contains("Risk", StringComparison.OrdinalIgnoreCase))
                return PermanentRedirect(urlHelper, nameof(ModernWorkController.LogRisk), "ModernWork", new { id = projectId });

            if (action.Contains("Issue", StringComparison.OrdinalIgnoreCase) && TryGetInt(routeValues, "issueId", out var issueId))
                return PermanentRedirect(urlHelper, nameof(ModernWorkController.IssueDetail), "ModernWork", new { workId = projectId, id = issueId });

            if (action.Contains("Milestone", StringComparison.OrdinalIgnoreCase))
                return RedirectToWorkDetail(urlHelper, projectId, "milestones", null);

            if (action.Contains("Contact", StringComparison.OrdinalIgnoreCase) || action.Contains("People", StringComparison.OrdinalIgnoreCase) || action.Contains("Team", StringComparison.OrdinalIgnoreCase))
                return RedirectToWorkDetail(urlHelper, projectId, "contacts", null);

            if (action.Contains("Strategic", StringComparison.OrdinalIgnoreCase) || action.Contains("Mission", StringComparison.OrdinalIgnoreCase) || action.Contains("Outcome", StringComparison.OrdinalIgnoreCase))
                return PermanentRedirect(urlHelper, nameof(ModernWorkController.EditStrategicAlignment), "ModernWork", new { id = projectId });

            return RedirectToWorkDetail(urlHelper, projectId, GetString(routeValues, "tab"), GetString(routeValues, "milestonestab"));
        }

        if (TryGetInt(routeValues, "id", out var id) && action is not ("Index" or "All" or "YourWork" or "Watched"))
            return RedirectToWorkDetail(urlHelper, id, GetString(routeValues, "tab"), GetString(routeValues, "milestonestab"));

        return PermanentRedirect(urlHelper, nameof(ModernWorkController.AllWork), "ModernWork", null);
    }

    private static async Task<IActionResult> RedirectFromMilestoneAsync(
        IUrlHelper urlHelper,
        CompassDbContext db,
        int milestoneId,
        CancellationToken cancellationToken)
    {
        var projectId = await db.Milestones.AsNoTracking()
            .Where(m => m.Id == milestoneId && !m.IsDeleted)
            .Select(m => m.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId.HasValue
            ? RedirectToWorkDetail(urlHelper, projectId.Value, "milestones", null)
            : PermanentRedirect(urlHelper, nameof(ModernWorkController.AllWork), "ModernWork", null);
    }

    private static async Task<IActionResult> RedirectFromOutcomeAsync(
        IUrlHelper urlHelper,
        CompassDbContext db,
        int outcomeId,
        CancellationToken cancellationToken)
    {
        var projectId = await db.ProjectOutcomes.AsNoTracking()
            .Where(o => o.Id == outcomeId)
            .Select(o => o.ProjectId)
            .FirstOrDefaultAsync(cancellationToken);

        return projectId > 0
            ? RedirectToWorkDetail(urlHelper, projectId, "overview", null)
            : PermanentRedirect(urlHelper, nameof(ModernWorkController.AllWork), "ModernWork", null);
    }

    private static IActionResult RedirectToWorkDetail(IUrlHelper urlHelper, int id, string? tab, string? milestoneTab)
    {
        if (id <= 0)
            return PermanentRedirect(urlHelper, nameof(ModernWorkController.AllWork), "ModernWork", null);

        var normalizedTab = NormalizeDetailTab(tab);
        object? routeValues = normalizedTab == null && milestoneTab == null
            ? new { id }
            : new { id, tab = normalizedTab, milestonestab = NormalizeMilestoneTab(milestoneTab) };

        var url = urlHelper.Action(nameof(ModernWorkController.Detail), "ModernWork", routeValues) ?? $"/modern/work/detail/{id}";
        var fragment = DetailFragment(normalizedTab);
        return new RedirectResult(string.IsNullOrEmpty(fragment) ? url : $"{url}{fragment}", permanent: true);
    }

    private static string? NormalizeDetailTab(string? tab)
    {
        if (string.IsNullOrWhiteSpace(tab))
            return null;

        return tab.Trim().ToLowerInvariant() switch
        {
            "monthlyupdates" or "statusupdates" => "updates",
            "team" => "contacts",
            "governance" => "strategicalignment",
            "service-register" => "serviceregister",
            "links" => "dependencies",
            "milestones" or "risks" or "issues" or "contacts" or "serviceregister"
                or "strategicalignment" or "dependencies" or "assumptions" or "updates" or "overview"
                => tab.Trim().ToLowerInvariant(),
            _ => "overview"
        };
    }

    private static string? NormalizeMilestoneTab(string? milestoneTab)
    {
        if (string.IsNullOrWhiteSpace(milestoneTab))
            return null;

        return milestoneTab.Trim().ToLowerInvariant() switch
        {
            "complete" => "complete",
            _ => "inprogress"
        };
    }

    private static string? DetailFragment(string? tab) =>
        tab switch
        {
            "issues" => "#wd-issues",
            "risks" => "#wd-risks",
            "milestones" => "#wd-milestones",
            "dependencies" => "#wd-dependencies",
            "contacts" => "#wd-contacts",
            "updates" => "#wd-updates",
            "strategicalignment" => "#wd-strategic-alignment",
            "serviceregister" => "#wd-service-register",
            _ => null
        };

    private static object? CopyListFilters(IDictionary<string, object?> routeValues)
    {
        var search = GetString(routeValues, "search");
        var page = TryGetInt(routeValues, "page", out var pageNumber) ? pageNumber : (int?)null;
        var primaryContactUserId = TryGetInt(routeValues, "primaryContactId", out var contactId) ? contactId : (int?)null;

        if (search == null && page == null && primaryContactUserId == null)
            return null;

        return new
        {
            search,
            page,
            primaryContactUserId
        };
    }

    private static object? CopyBusinessCaseId(IDictionary<string, object?> routeValues) =>
        TryGetInt(routeValues, "businessCaseId", out var businessCaseId)
            ? new { businessCaseId }
            : null;

    private static bool TryGetProjectId(IDictionary<string, object?> routeValues, out int projectId)
    {
        if (TryGetInt(routeValues, "projectId", out projectId))
            return true;

        if (TryGetInt(routeValues, "id", out projectId))
            return true;

        projectId = default;
        return false;
    }

    private static int RequireInt(IDictionary<string, object?> routeValues, string key) =>
        TryGetInt(routeValues, key, out var value) ? value : 0;

    private static bool TryGetInt(IDictionary<string, object?> routeValues, string key, out int value)
    {
        value = default;
        if (!routeValues.TryGetValue(key, out var raw) || raw == null)
            return false;

        return raw switch
        {
            int i => (value = i) > 0 || key == "page",
            long l => (value = (int)l) > 0 || key == "page",
            string s when int.TryParse(s, out var parsed) => (value = parsed) > 0 || key == "page",
            _ => false
        };
    }

    private static string? GetString(IDictionary<string, object?> routeValues, string key) =>
        routeValues.TryGetValue(key, out var raw) ? raw?.ToString() : null;

    private static RedirectToActionResult PermanentRedirect(
        IUrlHelper urlHelper,
        string action,
        string controller,
        object? routeValues) =>
        new(action, controller, routeValues, permanent: true);
}
