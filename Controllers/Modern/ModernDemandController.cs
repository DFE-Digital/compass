using Compass.Data;
using Compass.Filters;
using Compass.Models;
using Compass.Models.DemandPipeline;
using Compass.Services.DemandPipeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Compass2-style demand pipeline at <c>/modern/demand/*</c>.</summary>
[Authorize]
[ServiceFilter(typeof(DemandFeatureGateFilter))]
[Route("modern/demand")]
public partial class ModernDemandController : Controller
{
    private readonly CompassDbContext _db;
    private readonly Compass.Services.DemandPipeline.IDemandScoringFrameworkService _demandScoringFramework;

    public ModernDemandController(CompassDbContext db, Compass.Services.DemandPipeline.IDemandScoringFrameworkService demandScoringFramework)
    {
        _db = db;
        _demandScoringFramework = demandScoringFramework;
    }

    /// <summary>Landing URL for demand — redirects to the dashboard.</summary>
    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    /// <summary>Demand dashboard — metrics, recent submissions, triage sidebar (Compass2-style).</summary>
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard() => BuildDashboardAsync();

    /// <summary>Pipeline register view (same content as <c>/register</c>, alternative URL).</summary>
    [HttpGet("pipeline")]
    public async Task<IActionResult> Pipeline(string? tab, string? status, string? band, string? search, string? stage, string? department)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";
        ViewBag.RegisterActionName = "Pipeline";
        ViewBag.RegisterBreadcrumbCurrent = "Pipeline";
        var demands = await LoadDemandRegisterAsync(tab, status, band, search, stage, department);
        return View("~/Views/Modern/Demand/Register.cshtml", demands);
    }

    /// <summary>Demand register with tabbed active/draft/outcome views.</summary>
    [HttpGet("register")]
    public async Task<IActionResult> Register(string? tab, string? status, string? band, string? search, string? stage, string? department)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-register";
        ViewBag.RegisterActionName = "Register";
        ViewBag.RegisterBreadcrumbCurrent = "Demand register";

        var demands = await LoadDemandRegisterAsync(tab, status, band, search, stage, department);
        return View("~/Views/Modern/Demand/Register.cshtml", demands);
    }

    private async Task<List<DemandPipelineRequest>> LoadDemandRegisterAsync(
        string? tab,
        string? status,
        string? band,
        string? search,
        string? stage,
        string? department)
    {
        var all = await _db.DemandPipelineRequests.AsNoTracking().ToListAsync();

        var currentTab = (tab ?? "all").Trim().ToLowerInvariant();
        if (currentTab is not ("all" or "drafts" or "indelivery" or "rejected" or "paused"))
            currentTab = "all";

        bool IsInDelivery(DemandPipelineRequest d) =>
            d.Status is "Progressed to delivery" or "Closed - Progressed to delivery";

        var tabBase = currentTab switch
        {
            "drafts" => all.Where(d => d.Status == "Draft"),
            "indelivery" => all.Where(IsInDelivery),
            "rejected" => all.Where(d => d.Status == "Rejected"),
            "paused" => all.Where(d => d.Status == "Paused"),
            _ => all.Where(d => d.Status != "Draft" && d.Status != "Rejected" && d.Status != "Paused" && !IsInDelivery(d))
        };

        var filterStage = (stage ?? status ?? "all").Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(filterStage) && filterStage != "all")
        {
            tabBase = filterStage switch
            {
                "submitted" => tabBase.Where(d => d.Status == "Submitted"),
                "in-review" => tabBase.Where(d => d.Status == "ExploratoryReview"),
                "scoring" => tabBase.Where(d => d.Status == "Scoring"),
                "scored" => tabBase.Where(d => d.Status == "Scored"),
                "triaged" => tabBase.Where(d => d.Status == "Triaged"),
                _ => tabBase
            };
        }

        if (!string.IsNullOrWhiteSpace(band))
            tabBase = tabBase.Where(d => d.SuggestedBand == band);

        if (!string.IsNullOrWhiteSpace(department))
        {
            var dep = department.Trim();
            tabBase = tabBase.Where(d => string.Equals(d.DepartmentGroup, dep, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            tabBase = tabBase.Where(d =>
                (!string.IsNullOrEmpty(d.Title) && d.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(d.Reference) && d.Reference.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var result = tabBase
            .OrderByDescending(d => d.UpdatedAt)
            .ThenByDescending(d => d.CreatedAt)
            .ToList();

        ViewBag.DemandTab = currentTab;
        ViewBag.DemandTabCountAll = all.Count(d => d.Status != "Draft" && d.Status != "Rejected" && d.Status != "Paused" && !IsInDelivery(d));
        ViewBag.DemandTabCountDrafts = all.Count(d => d.Status == "Draft");
        ViewBag.DemandTabCountIndelivery = all.Count(IsInDelivery);
        ViewBag.DemandTabCountRejected = all.Count(d => d.Status == "Rejected");
        ViewBag.DemandTabCountPaused = all.Count(d => d.Status == "Paused");
        ViewBag.TotalInTab = result.Count;
        ViewBag.StageFilter = filterStage;
        ViewBag.BandFilter = band;
        ViewBag.DepartmentFilter = department;
        ViewBag.Search = search;
        ViewBag.DepartmentGroups = all
            .Where(d => !string.IsNullOrWhiteSpace(d.DepartmentGroup))
            .Select(d => d.DepartmentGroup!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return result;
    }

    private async Task<IActionResult> BuildDashboardAsync()
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";

        var demands = await _db.DemandPipelineRequests.AsNoTracking().ToListAsync();
        var bcs = await _db.DemandPipelineBusinessCases.AsNoTracking()
            .Where(b => b.Status == "Active")
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var totalActiveRegister = demands.Count(d =>
            d.Status != "Draft"
            && d.Status != "Progressed to delivery"
            && d.Status != "Closed - Progressed to delivery"
            && d.Status != "Rejected"
            && d.Status != "Paused");

        var closedSettled = demands.Count(d => d.Status is "Triaged" or "Closed" or "Progressed to delivery" or "Closed - Progressed to delivery" or "Rejected" or "Paused");

        var cutoff90 = today.AddDays(-90);
        var band90Must = 0;
        var band90Could = 0;
        var band90Not = 0;
        foreach (var d in demands)
        {
            if (string.IsNullOrEmpty(d.SuggestedBand)) continue;
            var dt = (d.SubmittedDate ?? d.CreatedAt).Date;
            if (dt < cutoff90) continue;
            switch (d.SuggestedBand)
            {
                case "MustDo": band90Must++; break;
                case "CouldDo": band90Could++; break;
                case "DoNotDo": band90Not++; break;
            }
        }

        var nextMeeting = await _db.DemandPipelineTriageMeetings.AsNoTracking()
            .Where(m => m.Status == "Scheduled" && m.MeetingDate >= today)
            .OrderBy(m => m.MeetingDate)
            .FirstOrDefaultAsync();

        var agendaPreview = new List<DemandDashboardAgendaPreviewLine>();
        var meetingItems = new List<DemandDashboardTriageItem>();
        var readyCount = 0;
        if (nextMeeting != null)
        {
            var ids = TriageAgendaJsonHelper.GetDemandIdsInAgenda(nextMeeting.AgendaJson);
            var demandById = demands.ToDictionary(d => d.Id);
            var readyIds = ids
                .Where(id => demandById.TryGetValue(id, out var x) && (x.Status == "Scored" || x.Status == "TriagePending" || x.Status == "Triage Pending"))
                .Distinct()
                .ToList();
            readyCount = readyIds.Count;
            foreach (var id in readyIds)
            {
                var d = demandById[id];
                var band = BandDisplayForSuggested(d.SuggestedBand);
                var (statusLabel, statusTagClass) = d.Status switch
                {
                    "Scored" => ("SCORED", "dfe-c-tag--teal"),
                    "TriagePending" or "Triage Pending" => ("TRIAGE PENDING", "dfe-c-tag--red"),
                    _ => ((d.Status ?? "—").ToUpperInvariant(), "dfe-c-tag--grey")
                };
                meetingItems.Add(new DemandDashboardTriageItem
                {
                    Id = d.Id,
                    Title = d.Title ?? "—",
                    Reference = d.Reference ?? "—",
                    Department = string.IsNullOrWhiteSpace(d.DepartmentGroup) ? "—" : d.DepartmentGroup,
                    TotalScore = d.TotalScore,
                    SuggestedBand = d.SuggestedBand,
                    BandClass = band.CssClass,
                    BandLabel = band.Label,
                    StatusLabel = statusLabel,
                    StatusTagClass = statusTagClass
                });
                if (agendaPreview.Count < 2)
                {
                    agendaPreview.Add(new DemandDashboardAgendaPreviewLine
                    {
                        Reference = d.Reference ?? "—",
                        BandClass = band.CssClass,
                        BandLabel = band.Label
                    });
                }
            }
        }

        int? daysUntil = null;
        if (nextMeeting?.MeetingDate != null)
            daysUntil = (nextMeeting.MeetingDate.Value.Date - today).Days;

        var recent = demands
            .Where(d => d.Status != "Draft")
            .OrderByDescending(d => d.SubmittedDate ?? d.CreatedAt)
            .Take(5)
            .Select(MapDemandDashboardRecentRow)
            .ToList();

        var stages = await _db.DemandPipelineStages.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
        if (stages.Count == 0)
            stages = await _db.DemandPipelineStages.AsNoTracking().OrderBy(s => s.DisplayOrder).ToListAsync();
        var stageCount = stages.Count;
        var stageCounts = new int[stageCount];
        foreach (var bc in bcs.Where(b => !demands.Any(d => d.BusinessCaseId == b.Id)))
        {
            var idx = bc.Stage == "Idea" ? 0 : bc.Stage == "Developing" ? 1 : 2;
            if (idx >= 0 && idx < stageCount) stageCounts[idx]++;
        }
        foreach (var d in demands)
        {
            var idx = d.Status switch
            {
                "Submitted" or "ExploratoryReview" or "Scoring" or "Scored" or "TriagePending" or "Triage Pending" => 3,
                "Triaged" or "Rejected" or "Paused" => 4,
                "Closed" or "Progressed to delivery" or "Closed - Progressed to delivery" => stageCount > 0 ? stageCount - 1 : 0,
                _ => 3
            };
            if (idx >= 0 && idx < stageCount) stageCounts[idx]++;
        }

        var groupSpansForTracker = new List<PipelineTrackerGroupSpan>();
        for (var i = 0; i < stageCount;)
        {
            var grouping0 = stages[i].Grouping;
            var key = string.IsNullOrWhiteSpace(grouping0) ? null : grouping0.Trim();
            var spanCount = 0;
            var itemCount = 0;
            while (i < stageCount)
            {
                var grouping = stages[i].Grouping;
                var k = string.IsNullOrWhiteSpace(grouping) ? null : grouping.Trim();
                if (k != key) break;
                spanCount++;
                itemCount += stageCounts[i];
                i++;
            }
            groupSpansForTracker.Add(new PipelineTrackerGroupSpan { Key = key, SpanCount = spanCount, ItemCount = itemCount });
        }

        var pipelineTracker = new PipelineTrackerViewModel
        {
            Stages = stages.Select(s => new PipelineStage
            {
                Id = s.Id,
                Title = s.Title,
                DisplayOrder = s.DisplayOrder,
                Description = s.Description,
                IsActive = s.IsActive,
                Grouping = s.Grouping
            }).ToList(),
            StageCounts = stageCounts,
            GroupSpans = groupSpansForTracker,
            StageIndex = null,
            ViewMode = "columns"
        };

        var triageQueueCount = meetingItems.Count;

        var demandBcIds = demands
            .Where(d => d.BusinessCaseId.HasValue)
            .Select(d => d.BusinessCaseId!.Value)
            .ToHashSet();
        var orphanBcs = bcs
            .Where(b => !demandBcIds.Contains(b.Id))
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new DemandDashboardOrphanBc
            {
                Id = b.Id,
                Title = b.Title ?? "—",
                Reference = b.Reference ?? "",
                Stage = b.Stage ?? "—",
                BusinessArea = b.BusinessArea,
                CreatedAt = b.CreatedAt
            })
            .ToList();

        var vm = new DemandDashboardViewModel
        {
            TotalRequests = totalActiveRegister,
            SubmittedCount = demands.Count(d => d.Status == "Submitted"),
            InExploreCount = demands.Count(d => d.Status == "ExploratoryReview"),
            ScoringCount = demands.Count(d => d.Status == "Scoring"),
            TriagePendingCount = triageQueueCount,
            ClosedCount = closedSettled,
            RecentSubmissions = recent,
            NextTriageMeetingDate = nextMeeting?.MeetingDate,
            NextTriageMeetingName = nextMeeting?.Name,
            NextTriageMeetingDaysUntil = daysUntil,
            NextMeetingDemandsReadyCount = readyCount,
            NextMeetingAgendaPreview = agendaPreview,
            NextMeetingItems = meetingItems,
            Band90MustDo = band90Must,
            Band90CouldDo = band90Could,
            Band90DoNotDo = band90Not,
            OrphanBusinessCases = orphanBcs,
            PipelineTracker = pipelineTracker
        };

        return View("~/Views/Modern/Demand/Dashboard.cshtml", vm);
    }

    private static (string CssClass, string Label) BandDisplayForSuggested(string? band) => band switch
    {
        "MustDo" => ("dfe-c-band--must", "MUST DO"),
        "CouldDo" => ("dfe-c-band--could", "COULD DO"),
        "DoNotDo" => ("dfe-c-band--not", "DO NOT DO"),
        _ => ("dfe-c-band--could", "—")
    };

    private static DemandDashboardRecentRow MapDemandDashboardRecentRow(DemandPipelineRequest d)
    {
        var (label, tagClass) = d.Status switch
        {
            "Submitted" => ("SUBMITTED", "dfe-c-tag--grey"),
            "ExploratoryReview" => ("EXPLORE", "dfe-c-tag--amber"),
            "Scoring" => ("SCORING", "dfe-c-tag--blue"),
            "Scored" => ("SCORED", "dfe-c-tag--teal"),
            "TriagePending" or "Triage Pending" => ("TRIAGE PENDING", "dfe-c-tag--red"),
            "Triaged" => ("TRIAGED", "dfe-c-tag--green"),
            "Closed" => ("CLOSED", "dfe-c-tag--grey"),
            "Progressed to delivery" => ("PROGRESSED", "dfe-c-tag--green"),
            "Closed - Progressed to delivery" => ("PROGRESSED", "dfe-c-tag--green"),
            "Rejected" => ("REJECTED", "dfe-c-tag--red"),
            "Paused" => ("PAUSED", "dfe-c-tag--grey"),
            "Returned" => ("RETURNED", "dfe-c-tag--amber"),
            _ => ((d.Status ?? "—").ToUpperInvariant(), "dfe-c-tag--grey")
        };

        return new DemandDashboardRecentRow
        {
            Id = d.Id,
            Title = d.Title ?? "—",
            Reference = d.Reference ?? "",
            Department = string.IsNullOrWhiteSpace(d.DepartmentGroup) ? "—" : d.DepartmentGroup,
            StatusTagClass = tagClass,
            StatusLabel = label,
            TotalScore = d.TotalScore,
            SuggestedBand = d.SuggestedBand
        };
    }

    /// <summary>Business case register (replaces Horizon / BusinessCase naming).</summary>
    [HttpGet("businesscase")]
    public async Task<IActionResult> BusinessCase(string? stage, string? search, string? tab, string? department)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-businesscase";

        var activeTab = tab?.ToLowerInvariant() switch
        {
            "all" => "all",
            "submitted" => "submitted",
            _ => "mine"
        };
        var q = _db.DemandPipelineBusinessCases.AsNoTracking();
        if (activeTab == "submitted")
        {
            // Everyone's business cases that have been submitted / linked to a demand
            q = q.Where(b => b.LinkedDemandRequestId != null);
        }
        else if (activeTab == "mine")
        {
            // Current user's cases only — include submitted (linked) as well as in-flight pipeline
            var userName = User.Identity?.Name;
            q = q.Where(b => b.CreatedBy == userName || b.Lead == userName);
        }
        else
        {
            // All — in-pipeline only (not yet linked to demand); submitted stays on its own tab
            q = q.Where(b => b.LinkedDemandRequestId == null);
        }
        if (!string.IsNullOrEmpty(stage)) q = q.Where(b => b.Stage == stage);
        if (!string.IsNullOrWhiteSpace(department))
            q = q.Where(b => b.DepartmentGroup == department);
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.Trim();
            q = q.Where(b => b.Title.Contains(term) || b.Reference.Contains(term));
        }

        var cases = await q.OrderByDescending(b => b.UpdatedAt).ToListAsync();

        ViewBag.StageFilter = stage;
        ViewBag.Search = search;
        ViewBag.DepartmentFilter = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
        ViewBag.Tab = activeTab;
        var pipeline = _db.DemandPipelineBusinessCases.AsNoTracking()
            .Where(b => b.LinkedDemandRequestId == null);
        ViewBag.ReadyCounts = await pipeline.CountAsync(b => b.Stage == "Ready");
        ViewBag.DevCount = await pipeline.CountAsync(b => b.Stage == "Developing");
        ViewBag.IdeaCount = await pipeline.CountAsync(b => b.Stage == "Idea");
        var pipelineTotal = await pipeline.CountAsync();
        ViewBag.DraftCount = pipelineTotal;
        ViewBag.PipelineTotal = pipelineTotal;
        ViewBag.AllCasesCount = pipelineTotal;
        var currentUser = User.Identity?.Name;
        ViewBag.MineCasesCount = string.IsNullOrEmpty(currentUser)
            ? 0
            : await _db.DemandPipelineBusinessCases.AsNoTracking()
                .CountAsync(b => b.CreatedBy == currentUser || b.Lead == currentUser);
        ViewBag.SubmittedCount = await _db.DemandPipelineBusinessCases.AsNoTracking()
            .CountAsync(b => b.LinkedDemandRequestId != null);
        ViewBag.TotalBusinessCasesCount = await _db.DemandPipelineBusinessCases.AsNoTracking().CountAsync();

        var deptOptions = await _db.DemandPipelineBusinessCases.AsNoTracking()
            .Where(b => !string.IsNullOrEmpty(b.DepartmentGroup))
            .Select(b => b.DepartmentGroup!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
        ViewBag.DepartmentOptions = deptOptions;
        ViewBag.TableMode = activeTab;

        return View("~/Views/Modern/Demand/BusinessCase.cshtml", cases);
    }

    /// <summary>Business case detail page.</summary>
    [HttpGet("businesscase/{id:guid}")]
    public async Task<IActionResult> BusinessCaseDetail(Guid id)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-businesscase";

        var bc = await _db.DemandPipelineBusinessCases.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (bc == null) return NotFound();

        var linked = bc.LinkedDemandRequestId.HasValue
            ? await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == bc.LinkedDemandRequestId.Value)
            : null;
        ViewBag.LinkedDemand = linked;

        // Resolve priority outcome names from comma-separated IDs
        var poNames = new List<string>();
        if (!string.IsNullOrWhiteSpace(bc.PriorityOutcomeIds))
        {
            var poIds = bc.PriorityOutcomeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : (int?)null).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (poIds.Any())
                poNames = await _db.Objectives.AsNoTracking().Where(o => poIds.Contains(o.Id)).Select(o => o.Title).ToListAsync();
        }
        ViewBag.PriorityOutcomeNames = poNames;

        // Resolve mission pillar names from comma-separated IDs
        var mpNames = new List<string>();
        if (!string.IsNullOrWhiteSpace(bc.MissionPillarIds))
        {
            var mpIds = bc.MissionPillarIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : (int?)null).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (mpIds.Any())
                mpNames = await _db.Missions.AsNoTracking().Where(m => mpIds.Contains(m.Id)).Select(m => m.Title).ToListAsync();
        }
        ViewBag.MissionPillarNames = mpNames;

        var stages = await _db.DemandPipelineStages.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
        if (stages.Count == 0)
            stages = await _db.DemandPipelineStages.AsNoTracking().OrderBy(s => s.DisplayOrder).ToListAsync();
        ViewBag.PipelineStages = stages;
        ViewBag.PipelineStageIndex = 0;

        return View("~/Views/Modern/Demand/BusinessCaseDetail.cshtml", bc);
    }
}
