using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers;

[Authorize]
public class HelpController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult HowDoI(string? topic)
    {
        // If no topic specified, show the index
        if (string.IsNullOrWhiteSpace(topic))
        {
            return RedirectToAction(nameof(Index));
        }

        // Map topic to view name
        var viewName = topic.ToLowerInvariant().Replace(" ", "").Replace("-", "");
        
        // List of valid topics
        var validTopics = new[]
        {
            "createproject",
            "addteammember",
            "updateragstatus",
            "addmilestone",
            "logrisk",
            "reportissue",
            "completeprojectinfo",
            "viewreports",
            "managestandards",
            "addstatusupdate",
            "addweeklynoteupdate",
            "managemilestonesupdatessuccesses",
            "enrolproductaccessibility",
            "createaccessibilityissue",
            "manageaccessibilitystatement",
            "trackaccessibilityaudits",
            "linkwcagcriteria",
            "viewdeliveryprojects",
            "viewprojectdetails",
            "manageprojectdeliverables",
            "trackprojectdependencies",
            "submitperformancemetrics",
            "viewproducthistory",
            "understandreportingrequirements",
            "reportotherproduct"
        };

        if (validTopics.Contains(viewName))
        {
            ViewBag.Topic = topic;
            ViewBag.ViewName = viewName;
            return View();
        }

        // If topic not found, redirect to index
        return RedirectToAction(nameof(Index));
    }
}

