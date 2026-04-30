using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Compass.Attributes;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Models.DemandPipeline;
using Compass.Models.Fips;
using Compass.Services;
using Compass.Services.Modern;
using Compass.Services.Fips;
using Compass.Services.Raid;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Compass.Controllers.Modern;

/// <summary>Operations UI at <c>/modern/operations/*</c> — restricted to Super admin, Central Operations Admin, or Admin group.</summary>
[Authorize]
[RequireOperationConsoleUser]
[Route("modern/operations")]
public class ModernOperationsController : Controller
{
    private static readonly JsonSerializerOptions CmdbSyncSseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CompassDbContext _db;
    private readonly IFipsCmdbProductSyncService _fipsCmdbProductSync;
    private readonly IFipsProductWriteService _fipsProductWrite;
    private readonly ILogger<ModernOperationsController> _logger;
    private readonly IGlobalFeatureToggleService _globalFeatureToggle;
    private readonly IModernWorkService _modernWork;
    private readonly IProductsApiService _productsApi;
    private readonly IFipsBusinessAreaLookupSyncService _fipsBusinessAreaLookupSync;
    private readonly IOperationsRiskEditService _operationsRiskEdit;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public ModernOperationsController(
        CompassDbContext db,
        IFipsCmdbProductSyncService fipsCmdbProductSync,
        IFipsProductWriteService fipsProductWrite,
        ILogger<ModernOperationsController> logger,
        IGlobalFeatureToggleService globalFeatureToggle,
        IModernWorkService modernWork,
        IProductsApiService productsApi,
        IFipsBusinessAreaLookupSyncService fipsBusinessAreaLookupSync,
        IOperationsRiskEditService operationsRiskEdit,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _db = db;
        _fipsCmdbProductSync = fipsCmdbProductSync;
        _fipsProductWrite = fipsProductWrite;
        _logger = logger;
        _globalFeatureToggle = globalFeatureToggle;
        _modernWork = modernWork;
        _productsApi = productsApi;
        _fipsBusinessAreaLookupSync = fipsBusinessAreaLookupSync;
        _operationsRiskEdit = operationsRiskEdit;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    private async Task<IActionResult?> DemandDisabledRedirectAsync()
    {
        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Demand, User))
            return RedirectToAction("Index", "ModernDashboard");
        return null;
    }

    private async Task<IActionResult?> FipsDatabaseDisabledRedirectAsync()
    {
        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Fips, User))
        {
            TempData["ErrorMessage"] =
                "The service register is turned off. FIPS product data is read from the CMS when this feature is off.";
            return RedirectToAction(nameof(Dashboard));
        }

        return null;
    }

    private static string NormalizeServiceRegisterProductTab(string? tab) =>
        tab switch
        {
            "history" => "history",
            "cmdb" => "cmdb",
            "risks" => "risks",
            "issues" => "issues",
            _ => "information"
        };

    private static string? FormatCmdbSnapshotJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return raw;
        }
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? "";

    private void SetNav(string subNavItem)
    {
        ViewBag.MainNavSection = "operations";
        ViewBag.SubNavItem = subNavItem;
    }

    private static int[]? MergeWorkTagQuery(int? tagId, int[]? tagIds)
    {
        var list = new List<int>();
        if (tagId.HasValue && tagId.Value > 0)
            list.Add(tagId.Value);
        if (tagIds != null)
        {
            foreach (var t in tagIds)
            {
                if (t > 0)
                    list.Add(t);
            }
        }

        var distinct = list.Distinct().ToArray();
        return distinct.Length == 0 ? null : distinct;
    }

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        SetNav("operations-dashboard");

        var raid = await RaidEscalationManagementViewModelBuilder.BuildAsync(_db, "escalations", ct);
        var vm = new ModernOperationsDashboardViewModel
        {
            PendingTierChangeCount = raid.PendingApprovalCount,
            PendingEscalationsCount = raid.PendingEscalationsCount,
            PendingDeescalationsCount = raid.PendingDeescalationsCount,
            CurrentlyEscalatedCount = raid.CurrentlyEscalatedCount
        };
        return View("~/Views/Modern/Operations/OperationsDashboard.cshtml", vm);
    }

    private static string NormalizeCmsAccessRequestTab(string? tab) =>
        (tab ?? "new").Trim().ToLowerInvariant() switch
        {
            "completed" => "completed",
            "rejected" => "rejected",
            _ => "new"
        };

    [HttpGet("cms-requests")]
    public async Task<IActionResult> CmsRequests(string? tab, CancellationToken ct)
    {
        SetNav("operations-cms-requests");

        var t = NormalizeCmsAccessRequestTab(tab);

        var newCount = await _db.CmsAccessRequests.AsNoTracking().CountAsync(x => x.Status == "New", ct);
        var completedCount = await _db.CmsAccessRequests.AsNoTracking().CountAsync(x => x.Status == "Completed", ct);
        var rejectedCount = await _db.CmsAccessRequests.AsNoTracking().CountAsync(x => x.Status == "Rejected", ct);

        var filtered = _db.CmsAccessRequests.AsNoTracking().AsQueryable();
        filtered = t switch
        {
            "completed" => filtered.Where(x => x.Status == "Completed"),
            "rejected" => filtered.Where(x => x.Status == "Rejected"),
            _ => filtered.Where(x => x.Status == "New")
        };

        var rows = await filtered
            .OrderByDescending(x => x.DateRequested)
            .Select(x => new CmsAccessRequestRowViewModel
            {
                Id = x.Id,
                RequestorDisplayName = (x.RequestorFirstName + " " + x.RequestorLastName).Trim(),
                CmsName = x.CmsName,
                DateRequested = x.DateRequested,
                Status = x.Status,
                Outcome = x.Outcome
            })
            .ToListAsync(ct);

        var vm = new CmsAccessRequestListViewModel
        {
            ActiveTab = t,
            NewCount = newCount,
            CompletedCount = completedCount,
            RejectedCount = rejectedCount,
            Rows = rows
        };

        return View("~/Views/Modern/Operations/CmsAccessRequests.cshtml", vm);
    }

    [HttpGet("cms-requests/{id:int}")]
    public async Task<IActionResult> CmsRequestDetail(int id, string? returnTab, CancellationToken ct)
    {
        SetNav("operations-cms-requests");

        var row = await _db.CmsAccessRequests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row == null)
            return NotFound();

        var displayName = $"{row.RequestorFirstName} {row.RequestorLastName}".Trim();
        var vm = new CmsAccessRequestDetailViewModel
        {
            Id = row.Id,
            CmsName = row.CmsName,
            SignInPageUrl = row.SignInPageUrl,
            RequestorEmail = row.RequestorEmail,
            RequestorFirstName = row.RequestorFirstName,
            RequestorLastName = row.RequestorLastName,
            RequestorDisplayName = string.IsNullOrEmpty(displayName) ? row.RequestorEmail : displayName,
            DateRequested = row.DateRequested,
            PublisherAccessRequired = row.PublisherAccessRequired,
            Comments = row.Comments,
            Status = row.Status,
            Outcome = row.Outcome,
            RegistrationToken = row.RegistrationToken,
            CanProcess = string.Equals(row.Status, "New", StringComparison.OrdinalIgnoreCase),
            ReturnTab = NormalizeCmsAccessRequestTab(returnTab)
        };

        return View("~/Views/Modern/Operations/CmsAccessRequestDetail.cshtml", vm);
    }

    [HttpPost("cms-requests/{id:int}/process")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CmsRequestProcess(
        int id,
        [FromForm] CmsAccessRequestProcessForm form,
        string? returnTab,
        CancellationToken ct)
    {
        SetNav("operations-cms-requests");

        var listTab = NormalizeCmsAccessRequestTab(returnTab);
        var entity = await _db.CmsAccessRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return NotFound();

        if (!string.Equals(entity.Status, "New", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "This request has already been processed.";
            return RedirectToAction(nameof(CmsRequests), new { tab = listTab });
        }

        var outcomeKey = form.Outcome?.Trim();
        if (!string.Equals(outcomeKey, "Granted", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(outcomeKey, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(form.Outcome), "Select granted or rejected.");
        }

        var tokenTrimmed = form.RegistrationToken?.Trim();
        if (string.Equals(outcomeKey, "Granted", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(tokenTrimmed))
        {
            ModelState.AddModelError(nameof(form.RegistrationToken), "Enter the registration link or token for a granted request.");
        }

        var actorId = await ResolveCurrentUserIdForOperationsAsync(ct);
        if (!actorId.HasValue)
        {
            ModelState.AddModelError("", "Your signed-in user could not be resolved.");
        }

        if (!ModelState.IsValid)
        {
            var displayName = $"{entity.RequestorFirstName} {entity.RequestorLastName}".Trim();
            var vm = new CmsAccessRequestDetailViewModel
            {
                Id = entity.Id,
                CmsName = entity.CmsName,
                SignInPageUrl = entity.SignInPageUrl,
                RequestorEmail = entity.RequestorEmail,
                RequestorFirstName = entity.RequestorFirstName,
                RequestorLastName = entity.RequestorLastName,
                RequestorDisplayName = string.IsNullOrEmpty(displayName) ? entity.RequestorEmail : displayName,
                DateRequested = entity.DateRequested,
                PublisherAccessRequired = entity.PublisherAccessRequired,
                Comments = entity.Comments,
                Status = entity.Status,
                Outcome = entity.Outcome,
                RegistrationToken = entity.RegistrationToken,
                CanProcess = true,
                ReturnTab = listTab,
                DraftOutcome = form.Outcome,
                DraftRegistrationToken = form.RegistrationToken
            };
            return View("~/Views/Modern/Operations/CmsAccessRequestDetail.cshtml", vm);
        }

        var outcomeStored = string.Equals(outcomeKey, "Granted", StringComparison.OrdinalIgnoreCase) ? "Granted" : "Rejected";
        entity.Outcome = outcomeStored;

        if (outcomeStored == "Granted")
        {
            var linkText = tokenTrimmed!;
            var subject = $"Access to {entity.CmsName} granted";
            var body =
                "You will need to set up a password to access the CMS, use this link to set your password:\n\n"
                + linkText
                + "\n\nIf you have any problems, reply to this email, or contact design.ops@education.gov.uk";

            var cmsTemplateId = _configuration["GovUkNotify:CmsAccessRequestTemplateId"]?.Trim();
            var templateOverride = string.IsNullOrWhiteSpace(cmsTemplateId) ? null : cmsTemplateId;
            var notifyExtras = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["cms_name"] = entity.CmsName ?? "",
                ["registration_link"] = linkText,
                ["requestor_first_name"] = entity.RequestorFirstName ?? ""
            };

            var send = await _notificationService.SendEmailAsync(
                entity.RequestorEmail,
                subject,
                body,
                triggerCode: "cms_access_request",
                notifyTemplateId: templateOverride,
                notifyPersonalisationExtras: notifyExtras,
                cancellationToken: ct);

            if (!send.Success)
            {
                _logger.LogWarning(
                    "CMS access granted email failed for request {Id}: {Error}",
                    id,
                    send.ErrorMessage);
                TempData["ErrorMessage"] = send.ErrorMessage != null
                    ? $"The email could not be sent: {send.ErrorMessage}"
                    : "The email could not be sent.";
                return RedirectToAction(nameof(CmsRequestDetail), new { id, returnTab = listTab });
            }

            entity.Status = "Completed";
            entity.RegistrationToken = linkText;
        }
        else
        {
            entity.Status = "Rejected";
            if (!string.IsNullOrWhiteSpace(tokenTrimmed))
                entity.RegistrationToken = tokenTrimmed;
        }

        entity.ActionedByUserId = actorId;
        await _db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = outcomeStored == "Granted"
            ? "Access granted and the requester has been emailed."
            : "The request has been rejected.";
        var destinationTab = outcomeStored == "Granted" ? "completed" : "rejected";
        return RedirectToAction(nameof(CmsRequests), new { tab = destinationTab });
    }

    [HttpPost("cms-requests/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CmsRequestDelete(int id, CancellationToken ct)
    {
        SetNav("operations-cms-requests");

        var entity = await _db.CmsAccessRequests.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity == null)
            return NotFound();

        var destinationTab = entity.Status switch
        {
            "Completed" => "completed",
            "Rejected" => "rejected",
            _ => "new"
        };

        _db.CmsAccessRequests.Remove(entity);
        await _db.SaveChangesAsync(ct);

        TempData["SuccessMessage"] = "The CMS access request has been deleted.";
        return RedirectToAction(nameof(CmsRequests), new { tab = destinationTab });
    }

    [HttpGet("accessibility")]
    public IActionResult Accessibility() =>
        RedirectToActionPermanent(nameof(Accessibility), "ModernReporting");

    [HttpGet("manage-work")]
    [HttpGet("~/ModernOperations/ManageWork")]
    public async Task<IActionResult> ManageWork(
        string? tab,
        [FromQuery(Name = "page")] int page = 1,
        string? search = null,
        int? businessAreaId = null,
        int? directorateId = null,
        int? phaseId = null,
        int? ragId = null,
        int? priorityId = null,
        string? monthlyUpdate = null,
        int? primaryContactUserId = null,
        int? tagId = null,
        [FromQuery] int[]? tagIds = null,
        bool mine = false,
        string? sort = null,
        bool sd = false,
        CancellationToken cancellationToken = default)
    {
        SetNav("operations-manage-work");

        var userEmail = User.Identity?.Name;
        if (string.IsNullOrEmpty(userEmail))
            return Unauthorized();

        var currentUser = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower(), cancellationToken);
        if (currentUser == null)
            return Unauthorized();

        var activeTab = (tab?.ToLowerInvariant()) switch
        {
            "completed" => "completed",
            "cancelled" => "cancelled",
            "all" => "all",
            _ => "active"
        };

        var safePage = page < 1 ? 1 : page;

        var mergedTags = MergeWorkTagQuery(tagId, tagIds);

        var vm = await _modernWork.BuildWorkRegisterAsync(
            isMyWork: mine,
            search,
            portfolioId: null,
            directorateId,
            phaseId,
            ragId,
            priorityId,
            monthlyUpdate,
            currentUser,
            userEmail,
            Url,
            registerTab: activeTab,
            registerPage: safePage,
            registerPageSize: 20,
            businessAreaId: businessAreaId,
            primaryContactUserId: primaryContactUserId,
            tagIds: mergedTags,
            registerSort: sort,
            registerSortDesc: sd,
            cancellationToken);

        vm.ActiveFilterChips = SearchAndFilterActiveChipsBuilder.ForWorkRegister(
            vm, Url, nameof(ManageWork), "ModernOperations", activeTab);

        var manageWorkUrl = Url.Action(nameof(ManageWork), "ModernOperations") ?? "/modern/operations/manage-work";
        ViewBag.SearchAndFilter = new SearchAndFilterViewModel
        {
            IdPrefix = "ops-work",
            SearchPlaceholder = "Search titles, aims and tag names…",
            SearchValue = search,
            FormActionUrl = Url.Action(nameof(ManageWork), "ModernOperations", new { tab = activeTab, page = 1 }) ?? manageWorkUrl,
            FormMethod = "get",
            ClearUrl = Url.Action(nameof(ManageWork), "ModernOperations", new { tab = activeTab }) ?? manageWorkUrl,
            ActiveChips = vm.ActiveFilterChips,
            SecondaryActionUrl = Url.Action(nameof(ModernWorkController.ExportRegister), "ModernWork", new
            {
                scope = "allwork",
                tab = activeTab,
                search,
                businessAreaId,
                directorateId,
                phaseId,
                ragId,
                priorityId,
                monthlyUpdate,
                primaryContactUserId,
                tagId,
                tagIds = mergedTags,
                mine,
                sort,
                sd
            }),
            SecondaryActionLabel = "Export this view",
            Fields = new List<SearchAndFilterFieldViewModel>()
        };
        ViewBag.ActiveTab = activeTab;
        ViewBag.AllWorkActiveTab = activeTab;
        ViewBag.OpsManageWork = true;

        return View("~/Views/Modern/Work/AllWork.cshtml", vm);
    }

    [HttpGet("manage-demand")]
    public async Task<IActionResult> ManageDemand(string? search, string? status, string? department, CancellationToken ct)
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        SetNav("operations-manage-demand");

        var all = await _db.DemandPipelineRequests.AsNoTracking()
            .OrderByDescending(d => d.UpdatedAt).ThenByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var filtered = all.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(d =>
                (!string.IsNullOrEmpty(d.Title) && d.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(d.Reference) && d.Reference.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            filtered = filtered.Where(d => string.Equals(d.Status, status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(department) && department != "all")
            filtered = filtered.Where(d => string.Equals(d.DepartmentGroup, department.Trim(), StringComparison.OrdinalIgnoreCase));

        var result = filtered.ToList();

        var statuses = all.Select(d => d.Status).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();
        var departments = all.Select(d => d.DepartmentGroup).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s!).ToList();

        var baseUrl = Url.Action(nameof(ManageDemand), "ModernOperations") ?? "/modern/operations/manage-demand";
        var sf = new SearchAndFilterViewModel
        {
            IdPrefix = "ops-demand",
            SearchPlaceholder = "Search by title or reference…",
            SearchValue = search,
            FormActionUrl = baseUrl,
            ClearUrl = baseUrl,
            Fields = new List<SearchAndFilterFieldViewModel>
            {
                new()
                {
                    Label = "Status", Name = "status",
                    SelectedValue = status,
                    Options = new[] { new SearchAndFilterOption { Value = "all", Text = "All statuses" } }
                        .Concat(statuses.Select(s => new SearchAndFilterOption { Value = s, Text = s }))
                        .ToList()
                },
                new()
                {
                    Label = "Department", Name = "department",
                    SelectedValue = department,
                    Options = new[] { new SearchAndFilterOption { Value = "all", Text = "All departments" } }
                        .Concat(departments.Select(d => new SearchAndFilterOption { Value = d!, Text = d! }))
                        .ToList()
                }
            }
        };
        sf.ActiveChips = SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf, Url, nameof(ManageDemand), "ModernOperations", null);
        ViewBag.SearchAndFilter = sf;

        return View("~/Views/Modern/Operations/ManageDemand.cshtml", result);
    }

    [HttpPost("manage-demand/{id:guid}/update-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ManageDemandUpdateStatus(Guid id, string newStatus, string? search, string? status, string? department)
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        demand.Status = newStatus;
        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Status for \"{demand.Title}\" updated to {newStatus}.";
        return RedirectToAction(nameof(ManageDemand), new { search, status, department });
    }

    [HttpGet("manage-performance")]
    public async Task<IActionResult> ManagePerformance(int? commissionId, CancellationToken cancellationToken = default)
    {
        SetNav("operations-manage-performance");

        var commissionOptions = await _db.Commissions.AsNoTracking()
            .OrderByDescending(c => c.DueDate)
            .Select(c => new CommissionPickerOption { Id = c.Id, Name = c.Name, DueDate = c.DueDate, IsActive = c.IsActive })
            .ToListAsync(cancellationToken);

        var jsonOpts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        if (commissionOptions.Count == 0)
        {
            return View("~/Views/Modern/Operations/ManagePerformance.cshtml",
                new OperationsManagePerformanceViewModel { CommissionOptions = commissionOptions });
        }

        var selectedId = commissionId ?? commissionOptions[0].Id;
        if (!commissionOptions.Any(x => x.Id == selectedId))
        {
            TempData["Error"] = "Choose a commission from the list.";
            selectedId = commissionOptions[0].Id;
        }

        var commissionEntity = await _db.Commissions.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == selectedId, cancellationToken);

        if (commissionEntity == null)
        {
            TempData["Error"] = "Commission not found.";
            return View("~/Views/Modern/Operations/ManagePerformance.cshtml",
                new OperationsManagePerformanceViewModel { CommissionOptions = commissionOptions });
        }

        List<ProductDto> eligibleProducts;
        try
        {
            var allProducts = await _productsApi.GetAllProductsAsync();
            eligibleProducts = CommissionReportingProductScope.GetAllActivePublishedEligible(allProducts)
                .Where(p => CommissionReportingProductScope.ProductMatchesCommissionInScopeRules(commissionEntity, p))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operations manage performance: failed to load product catalogue");
            TempData["Error"] = "Could not load product data from the catalogue. Try again later.";
            eligibleProducts = new List<ProductDto>();
        }

        var submissions = await _db.CommissionSubmissions.AsNoTracking()
            .Where(cs => cs.CommissionId == commissionEntity.Id)
            .ToDictionaryAsync(cs => cs.ProductDocumentId, cs => cs, cancellationToken);

        var submitted = 0;
        var late = 0;
        var inProgress = 0;
        var notStarted = 0;
        foreach (var p in eligibleProducts)
        {
            var doc = p.DocumentId ?? "";
            if (string.IsNullOrEmpty(doc) || !submissions.TryGetValue(doc, out var sub))
            {
                notStarted++;
                continue;
            }

            switch (sub.Status)
            {
                case CommissionSubmissionStatus.Submitted:
                    submitted++;
                    break;
                case CommissionSubmissionStatus.Late:
                    late++;
                    break;
                case CommissionSubmissionStatus.InProgress:
                    inProgress++;
                    break;
                default:
                    notStarted++;
                    break;
            }
        }

        static OpsPerfOrgRow AggregateOrgGroup(
            IGrouping<string, ProductDto> g,
            Dictionary<string, CommissionSubmission> submissionByDoc)
        {
            var potential = 0;
            var ac = 0;
            var al = 0;
            var ip = 0;
            var ns = 0;
            foreach (var p in g)
            {
                potential++;
                var doc = p.DocumentId ?? "";
                if (string.IsNullOrEmpty(doc) || !submissionByDoc.TryGetValue(doc, out var sub))
                {
                    ns++;
                    continue;
                }

                switch (sub.Status)
                {
                    case CommissionSubmissionStatus.Submitted:
                        ac++;
                        break;
                    case CommissionSubmissionStatus.Late:
                        al++;
                        break;
                    case CommissionSubmissionStatus.InProgress:
                        ip++;
                        break;
                    default:
                        ns++;
                        break;
                }
            }

            return new OpsPerfOrgRow
            {
                Name = g.Key,
                PotentialSubmissions = potential,
                ActualSubmitted = ac,
                ActualLate = al,
                InProgress = ip,
                NotStarted = ns
            };
        }

        var baRows = eligibleProducts
            .GroupBy(p => CommissionReportingProductScope.GetBusinessArea(p) ?? "Unassigned")
            .Select(g => AggregateOrgGroup(g, submissions))
            .OrderByDescending(r => r.PotentialSubmissions)
            .ThenBy(r => r.Name)
            .ToList();

        var dirRows = eligibleProducts
            .GroupBy(p => CommissionReportingProductScope.GetDirectorate(p) ?? "Unassigned")
            .Select(g => AggregateOrgGroup(g, submissions))
            .OrderByDescending(r => r.PotentialSubmissions)
            .ThenBy(r => r.Name)
            .ToList();

        var doughnut = new
        {
            labels = new[] { "Submitted", "Late", "In progress", "Not started" },
            values = new[] { submitted, late, inProgress, notStarted },
            colors = new[] { "#00703c", "#d4351c", "#f47738", "#b1b4b6" }
        };

        var topBa = baRows.Take(16).ToList();
        var baBar = new
        {
            labels = topBa.Select(r => r.Name).ToArray(),
            potential = topBa.Select(r => r.PotentialSubmissions).ToArray(),
            actual = topBa.Select(r => r.ActualSubmitted + r.ActualLate).ToArray()
        };

        var timelinePoints = submissions.Values
            .Where(s => s.SubmittedDate.HasValue &&
                        (s.Status == CommissionSubmissionStatus.Submitted || s.Status == CommissionSubmissionStatus.Late))
            .Select(s => s.SubmittedDate!.Value.Date)
            .OrderBy(d => d)
            .ToList();

        object timeline;
        if (timelinePoints.Count == 0)
        {
            timeline = new { labels = Array.Empty<string>(), cumulative = Array.Empty<int>() };
        }
        else
        {
            var byDay = timelinePoints.GroupBy(d => d).OrderBy(g => g.Key).ToList();
            var labels = new List<string>();
            var cumulative = new List<int>();
            var running = 0;
            foreach (var day in byDay)
            {
                running += day.Count();
                labels.Add(day.Key.ToString("d MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-GB")));
                cumulative.Add(running);
            }

            timeline = new { labels, cumulative };
        }

        var vm = new OperationsManagePerformanceViewModel
        {
            CommissionOptions = commissionOptions,
            SelectedCommissionId = commissionEntity.Id,
            Commission = new CommissionSummaryVm
            {
                Id = commissionEntity.Id,
                Name = commissionEntity.Name,
                Quarter = commissionEntity.Quarter,
                StartDate = commissionEntity.StartDate,
                EndDate = commissionEntity.EndDate,
                OpenDate = commissionEntity.OpenDate,
                DueDate = commissionEntity.DueDate,
                IsActive = commissionEntity.IsActive
            },
            EligibleProductCount = eligibleProducts.Count,
            SubmittedCount = submitted,
            LateCount = late,
            InProgressCount = inProgress,
            NotStartedCount = notStarted,
            BusinessAreaRows = baRows,
            DirectorateRows = dirRows,
            StatusDoughnutJson = JsonSerializer.Serialize(doughnut, jsonOpts),
            BusinessAreaBarJson = JsonSerializer.Serialize(baBar, jsonOpts),
            SubmissionTimelineJson = JsonSerializer.Serialize(timeline, jsonOpts),
            HasCommission = true,
            HasEligibleProducts = eligibleProducts.Count > 0
        };

        return View("~/Views/Modern/Operations/ManagePerformance.cshtml", vm);
    }

    [HttpGet("raid/escalations")]
    public async Task<IActionResult> RaidEscalations(string? tab, CancellationToken ct)
    {
        SetNav("operations-raid");

        var vm = await RaidEscalationManagementViewModelBuilder.BuildAsync(_db, tab, ct);

        return View("~/Views/Modern/Operations/RaidEscalations.cshtml", vm);
    }

    [HttpGet("raid/risks/{id:int}/edit")]
    public async Task<IActionResult> RaidRiskOperationsEdit(int id, CancellationToken ct)
    {
        SetNav("operations-raid");
        var form = await _operationsRiskEdit.BuildFormAsync(id, ct);
        if (form == null)
            return NotFound();

        await _operationsRiskEdit.LoadEditorViewBagAsync(this, form.OwnerUserId, form.SroUserId, ct);
        ViewBag.EditorTitle = "Edit risk (Operations)";
        ViewBag.OperationsRiskEdit = true;
        ViewBag.OperationsFormPostUrl = Url.Action(
            nameof(RaidRiskOperationsEditPost),
            "ModernOperations",
            new { id }) ?? $"/modern/operations/raid/risks/{id}/edit";
        ViewBag.OperationsListUrl = Url.Action(nameof(RaidEscalations), "ModernOperations", new { tab = "active" })
            ?? "/modern/operations/raid/escalations?tab=active";
        return View("~/Views/Modern/Raid/RiskEditor.cshtml", form);
    }

    [HttpPost("raid/risks/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaidRiskOperationsEditPost(
        int id,
        [FromForm] ModernRaidRiskEditorForm form,
        [FromForm] string? operationsChangeReason,
        CancellationToken ct)
    {
        SetNav("operations-raid");
        form.Id = id;
        var editorId = await ResolveCurrentUserIdForOperationsAsync(ct);
        var ok = await _operationsRiskEdit.TrySaveAsync(
            id, form, operationsChangeReason, editorId, CurrentUserEmail, ModelState, ct);
        if (!ok)
        {
            await _operationsRiskEdit.LoadEditorViewBagAsync(this, form.OwnerUserId, form.SroUserId, ct);
            ViewBag.EditorTitle = "Edit risk (Operations)";
            ViewBag.OperationsRiskEdit = true;
            ViewBag.OperationsFormPostUrl = Url.Action(
                nameof(RaidRiskOperationsEditPost),
                "ModernOperations",
                new { id }) ?? $"/modern/operations/raid/risks/{id}/edit";
            ViewBag.OperationsListUrl = Url.Action(nameof(RaidEscalations), "ModernOperations", new { tab = "active" })
                ?? "/modern/operations/raid/escalations?tab=active";
            ViewBag.OperationsChangeReasonDraft = operationsChangeReason ?? string.Empty;
            return View("~/Views/Modern/Raid/RiskEditor.cshtml", form);
        }

        TempData["Message"] = "Risk updated from Operations. The change reason is recorded in the risk notes.";
        return RedirectToAction(nameof(RaidEscalations), new { tab = "active" });
    }

    private async Task<int?> ResolveCurrentUserIdForOperationsAsync(CancellationToken ct)
    {
        var email = CurrentUserEmail;
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var e = email.Trim().ToLowerInvariant();
        return await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == e)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static string NormaliseRaidEscalationListTab(string? t) =>
        (t ?? "escalations").Trim().ToLowerInvariant() switch
        {
            "deescalations" or "de-escalations" or "de" => "deescalations",
            "current" or "escalated" or "c" => "current",
            "active" or "a" or "all-risks" or "risks" => "active",
            _ => "escalations"
        };

    private static string InherentLabelFromScore(int score) =>
        score >= 20
            ? "Crisis / likely"
            : score >= 16
                ? "Critical / possible"
                : score >= 11
                    ? "High / possible"
                    : score >= 6
                        ? "Moderate / possible"
                        : "Low / unlikely";

    private static string UserDisplay(User? u) =>
        u == null
            ? "—"
            : (string.IsNullOrWhiteSpace(u.Name) ? (u.Email ?? "Unknown") : u.Name);

    /// <summary>Primary URL for operations to approve/reject a tier request (<c>…/action/{id}</c>).</summary>
    [HttpGet("raid/escalations/action/{requestId:int}")]
    public Task<IActionResult> RaidEscalationAction(int requestId, string? returnTab, string? returnUrl, CancellationToken ct) =>
        RaidEscalationReview(requestId, returnTab, returnUrl, ct);

    /// <summary>Legacy URL — use <see cref="RaidEscalationAction" /> (…/action/{id}).</summary>
    [HttpGet("raid/escalations/manage/{requestId:int}")]
    public Task<IActionResult> ManageRaidEscalationRequest(int requestId, string? returnTab, string? returnUrl, CancellationToken ct) =>
        RaidEscalationReview(requestId, returnTab, returnUrl, ct);

    [HttpGet("raid/escalations/review/{requestId:int}")]
    public async Task<IActionResult> RaidEscalationReview(int requestId, string? returnTab, string? returnUrl, CancellationToken ct)
    {
        SetNav("operations-raid");

        var req = await _db.RaidEscalationTierChangeRequests
            .AsNoTracking()
            .Include(x => x.Risk!).ThenInclude(r => r.RiskStatus)
            .Include(x => x.Risk!).ThenInclude(r => r.RiskTier)
            .Include(x => x.FromRiskTier)
            .Include(x => x.ToRiskTier)
            .Include(x => x.SubmittedByUser)
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);

        if (req == null
            || !string.Equals(req.RecordType, "risk", StringComparison.OrdinalIgnoreCase)
            || req.Risk == null
            || !req.RiskId.HasValue)
        {
            return NotFound();
        }

        if (!string.Equals(req.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "This request has already been decided.";
            return RedirectToAction(nameof(RaidEscalations), new { tab = NormaliseRaidEscalationListTab(returnTab) });
        }

        var activeTiers = await _db.RiskTiers.AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(ct);
        var toTier = req.ToRiskTier ?? activeTiers.FirstOrDefault(t => t.Id == req.ToRiskTierId);
        if (toTier == null)
        {
            TempData["ErrorMessage"] = "The requested target tier is no longer available.";
            return RedirectToAction(nameof(RaidEscalations), new { tab = NormaliseRaidEscalationListTab(returnTab) });
        }

        var listTab = NormaliseRaidEscalationListTab(returnTab);
        var listReturnPath = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : Url.Action(nameof(RaidEscalations), "ModernOperations", new { tab = listTab })
              ?? "/modern/operations/raid/escalations";
        var r = req.Risk;
        var statusLab = r.RiskStatus?.Label ?? r.Status ?? "—";

        // On submit, the risk row is moved to the *proposed* target tier so queues align (see RiskEscalationRequestPost).
        // "Current" for reviewers must be the pre-request band from the request snapshot, not r.RiskTier (which matches "to").
        var currentTierName = "—";
        if (req.FromRiskTier is { } snapFrom)
        {
            var n = snapFrom.Name?.Trim();
            if (!string.IsNullOrEmpty(n))
                currentTierName = n;
        }

        if (string.Equals(currentTierName, "—", StringComparison.Ordinal)
            && !string.IsNullOrEmpty(r.RiskTier?.Name?.Trim()))
        {
            currentTierName = r.RiskTier!.Name!.Trim();
        }

        var opTierRows = activeTiers
            .Where(t => !t.IsProposedTier)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Id)
            .Select(t => new ModernRaidEscalationRejectTierOption(t.Id, t.Name, t.SortOrder))
            .ToList();

        int? rejectDefault = null;
        if (req.FromRiskTier is { } fromSnap)
        {
            if (!fromSnap.IsProposedTier && opTierRows.Any(o => o.Id == fromSnap.Id))
                rejectDefault = fromSnap.Id;
            else
            {
                var rFrom = RiskTierGovernance.ResolveOperationalTierMatchingGovernance(fromSnap, activeTiers);
                if (rFrom != null && opTierRows.Any(o => o.Id == rFrom.Id))
                    rejectDefault = rFrom.Id;
            }
        }

        if (rejectDefault == null && r.RiskTier is { } curRt)
        {
            if (!curRt.IsProposedTier && opTierRows.Any(o => o.Id == r.RiskTierId))
                rejectDefault = r.RiskTierId;
            else
            {
                var resolved = RiskTierGovernance.ResolveOperationalTierMatchingGovernance(curRt, activeTiers);
                if (resolved != null && opTierRows.Any(o => o.Id == resolved.Id))
                    rejectDefault = resolved.Id;
            }
        }

        if (rejectDefault == null && opTierRows.Count > 0)
            rejectDefault = opTierRows[0].Id;

        var defaultRejectName = rejectDefault is int defRid
            ? opTierRows.FirstOrDefault(o => o.Id == defRid)?.Name ?? "—"
            : "—";

        var vm = new ModernRaidEscalationReviewViewModel
        {
            RequestId = req.Id,
            RiskId = r.Id,
            Reference = $"R-{r.Id:D4}",
            Title = r.Title,
            RiskScore = r.RiskScore,
            InherentLabel = InherentLabelFromScore(r.RiskScore),
            StatusLabel = statusLab,
            CurrentRiskTierLabel = currentTierName,
            RequestedToTierLabel = toTier.Name,
            DefaultRejectTierName = defaultRejectName,
            Rationale = string.IsNullOrWhiteSpace(req.Rationale) ? null : req.Rationale.Trim(),
            SubmittedByDisplay = UserDisplay(req.SubmittedByUser),
            SubmittedAt = req.SubmittedAt,
            RiskDetailUrl = Url.Action("RiskDetail", "ModernRaid", new { id = r.Id }) ?? "#",
            ListReturnUrl = listReturnPath,
            ListReturnTab = listTab,
            RejectTierOptions = opTierRows,
            RejectTierDefaultId = rejectDefault
        };

        return View("~/Views/Modern/Operations/RaidEscalationReview.cshtml", vm);
    }

    [HttpPost("raid/escalations/decide")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RaidEscalationDecide(
        int requestId,
        string? decision,
        [FromForm] string? rationale,
        int? rejectTierId,
        string? returnUrl,
        string? returnTab,
        CancellationToken ct)
    {
        SetNav("operations-raid");

        var listTab = NormaliseRaidEscalationListTab(returnTab);

        IActionResult RedirectAfterSuccess()
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(RaidEscalations), new { tab = listTab });
        }

        IActionResult RedirectToReviewError(string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction(nameof(RaidEscalationAction), new { requestId, returnTab = listTab, returnUrl = returnUrl });
        }

        var normalizedDecision = (decision ?? "").Trim().ToLowerInvariant();
        if (normalizedDecision is not ("approve" or "reject"))
        {
            return RedirectToReviewError("Choose whether to approve or reject this request.");
        }

        var req = await _db.RaidEscalationTierChangeRequests
            .Include(x => x.Risk)
            .Include(x => x.FromRiskTier)
            .Include(x => x.ToRiskTier)
            .FirstOrDefaultAsync(x => x.Id == requestId, ct);
        if (req == null || !string.Equals(req.RecordType, "risk", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "Tier change request was not found.";
            return RedirectToAction(nameof(RaidEscalations), new { tab = listTab });
        }

        if (!string.Equals(req.Status, "pending", StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] = "This request has already been decided.";
            return RedirectToAction(nameof(RaidEscalations), new { tab = listTab });
        }

        var userEmail = CurrentUserEmail;
        var currentUser = await _db.Users.FirstOrDefaultAsync(
            u => u.Email != null && u.Email.ToLower() == userEmail.ToLower(),
            ct);
        if (currentUser == null)
            return Unauthorized();

        var utcNow = DateTime.UtcNow;
        if (normalizedDecision == "approve")
        {
            var ac = (rationale ?? Request.Form["rationale"].ToString()).Trim();
            if (string.IsNullOrWhiteSpace(ac))
            {
                return RedirectToReviewError("Enter a rationale for this decision (your approval comment).");
            }

            if (ac.Length > 500)
            {
                return RedirectToReviewError("Rationale must be 500 characters or fewer.");
            }

            if (req.Risk == null)
            {
                return RedirectToReviewError("Risk record for this request could not be loaded.");
            }

            var activeTiers = await _db.RiskTiers.AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync(ct);
            var toTier = req.ToRiskTier ?? activeTiers.FirstOrDefault(t => t.Id == req.ToRiskTierId);
            if (toTier == null)
            {
                return RedirectToReviewError("The requested target tier is no longer available.");
            }

            var operationalTarget = RiskTierGovernance.ResolveOperationalTierMatchingGovernance(toTier, activeTiers);
            if (operationalTarget == null)
            {
                return RedirectToReviewError(
                    "No operational risk tier is configured for the requested governance level. Check Admin → RAID → Risk tiers.");
            }

            req.Risk.RiskTierId = operationalTarget.Id;
            req.Risk.UpdatedByUserId = currentUser.Id;
            req.Risk.UpdatedAt = utcNow;

            req.Status = "approved";
            req.DecidedAt = utcNow;
            req.DecidedByUserId = currentUser.Id;
            req.DecisionNote = ac;

            TempData["SuccessMessage"] = $"Request approved. The risk is now on {operationalTarget.Name}.";
        }
        else
        {
            var note = (rationale ?? Request.Form["rationale"].ToString()).Trim();
            if (string.IsNullOrWhiteSpace(note))
            {
                return RedirectToReviewError("Enter a rationale for this decision (the reason for declining).");
            }

            if (note.Length > 500)
            {
                return RedirectToReviewError("Rationale must be 500 characters or fewer.");
            }

            if (req.Risk == null)
            {
                return RedirectToReviewError("Risk record for this request could not be loaded.");
            }

            var activeTiers = await _db.RiskTiers.AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync(ct);
            var operationalById = activeTiers
                .Where(t => !t.IsProposedTier)
                .ToDictionary(t => t.Id, t => t);
            if (rejectTierId is not int opId || !operationalById.TryGetValue(opId, out var chosenTier))
            {
                return RedirectToReviewError("Choose the operational tier this risk should be set to.");
            }

            req.Risk.RiskTierId = opId;
            req.Risk.UpdatedByUserId = currentUser.Id;
            req.Risk.UpdatedAt = utcNow;

            req.Status = "rejected";
            req.DecidedAt = utcNow;
            req.DecidedByUserId = currentUser.Id;
            req.DecisionNote = note;
            TempData["SuccessMessage"] = $"Request declined. The risk is now on {chosenTier.Name}.";
        }

        await _db.SaveChangesAsync(ct);
        return RedirectAfterSuccess();
    }

    [HttpGet("service-register")]
    public async Task<IActionResult> ServiceRegister(
        string? tab,
        string? search,
        int? businessAreaId,
        int? channelId,
        int? userGroupId,
        int? typeId,
        int? phaseId,
        CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        SetNav("operations-service-register");

        var activeTab = string.IsNullOrWhiteSpace(tab) ? "all" : tab;
        var email = CurrentUserEmail;

        var vm = await FipsProductListingHelper.BuildProductsViewModelAsync(
            _db, activeTab, email, search, businessAreaId, channelId, userGroupId, typeId, phaseId, ct);
        vm.CanSyncFromCmdb = true;

        var baseUrl = Url.Action(nameof(ServiceRegister), "ModernOperations", new { tab = activeTab })
            ?? "/modern/operations/service-register";
        var sf = FipsProductListingHelper.BuildSearchAndFilter(vm, activeTab, baseUrl);
        sf.ActiveChips = SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf, Url, nameof(ServiceRegister), "ModernOperations", new { tab = activeTab });
        ViewBag.SearchAndFilter = sf;

        return View("~/Views/Modern/Operations/ServiceRegister.cshtml", vm);
    }

    [HttpPost("service-register/bulk-new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterBulkNewProducts([FromForm] ServiceRegisterBulkNewForm f, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        var email = CurrentUserEmail;
        var auditName = User.Identity?.Name ?? email;
        var returnTab = string.Equals(f.SourceTab, "all", StringComparison.OrdinalIgnoreCase) ? "all" : "new";

        IActionResult RedirectBack() =>
            RedirectToAction(nameof(ServiceRegister), new
            {
                tab = returnTab,
                search = f.RSearch,
                businessAreaId = f.RBusinessAreaId,
                channelId = f.RChannelId,
                userGroupId = f.RUserGroupId,
                typeId = f.RTypeId,
                phaseId = f.RPhaseId
            });

        var ids = f.ProductIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0)
        {
            TempData["Error"] = "Select at least one product.";
            return RedirectBack();
        }

        if (!f.ApplyStatus && !f.ApplyPhase && !f.ApplyBusinessArea && !f.ApplyChannel && !f.ApplyType)
        {
            TempData["Error"] = "Select at least one action (status, phase, business area, channels, or types).";
            return RedirectBack();
        }

        CMDBProductStatus? newStatus = null;
        if (f.ApplyStatus)
        {
            if (f.TargetStatus is null or < 0 or > 3)
            {
                TempData["Error"] = "Choose a valid target status.";
                return RedirectBack();
            }

            newStatus = (CMDBProductStatus)f.TargetStatus.Value;
        }

        int[]? resolvedBasFromLookups = null;
        if (f.ApplyBusinessArea)
        {
            resolvedBasFromLookups = await _fipsBusinessAreaLookupSync.ResolveToFipsBusinessAreaIdsAsync(
                f.BusinessAreaLookupIds ?? Array.Empty<int>(), ct);
        }

        var isAllTab = string.Equals(f.SourceTab, "all", StringComparison.OrdinalIgnoreCase);
        var productsQ = _db.CMDBProducts
            .AsNoTracking()
            .Include(p => p.BusinessAreas)
            .Include(p => p.Channels)
            .Include(p => p.UserGroups)
            .Include(p => p.Types)
            .Include(p => p.CategorisationItems)
            .Where(p => ids.Contains(p.Id));
        if (!isAllTab)
            productsQ = productsQ.Where(p => p.Status == CMDBProductStatus.New);
        var products = await productsQ.ToListAsync(ct);

        if (products.Count == 0)
        {
            TempData["Error"] = isAllTab
                ? "No products matched your selection. Refresh the list and try again."
                : "No new products matched your selection. Refresh the list and try again.";
            return RedirectBack();
        }

        var fail = 0;
        var ok = 0;

        foreach (var p in products.OrderBy(x => x.Title))
        {
            try
            {
                if (f.ApplyPhase || f.ApplyBusinessArea || f.ApplyChannel || f.ApplyType)
                {
                    var phase = f.ApplyPhase ? f.BulkPhaseId : p.PhaseId;
                    var bas = f.ApplyBusinessArea
                        ? (resolvedBasFromLookups ?? Array.Empty<int>())
                        : p.BusinessAreas.Select(b => b.FipsBusinessAreaId).ToArray();
                    var ch = f.ApplyChannel
                        ? (f.BulkChannelIds ?? Array.Empty<int>())
                        : p.Channels.Select(c => c.FipsChannelId).ToArray();
                    var ug = p.UserGroups.Select(u => u.FipsUserGroupId).ToArray();
                    var ty = f.ApplyType
                        ? (f.BulkTypeIds ?? Array.Empty<int>())
                        : p.Types.Select(t => t.FipsTypeId).ToArray();
                    var cat = p.CategorisationItems.Select(c => c.FipsCategorisationItemId).ToArray();

                    var u = await _fipsProductWrite.TryUpdateAsync(
                        p.Id,
                        email,
                        auditName,
                        false,
                        p.UserDescription,
                        phase,
                        p.ProductURL,
                        bas,
                        ch,
                        ug,
                        ty,
                        cat,
                        null,
                        p.IsEnterpriseService,
                        ct);

                    if (u.NotFound || u.Forbidden)
                    {
                        fail++;
                        continue;
                    }
                }

                if (f.ApplyStatus && newStatus.HasValue)
                {
                    var s = await _fipsProductWrite.TryChangeStatusAsync(
                        p.Id, email, auditName, false, newStatus.Value, ct);
                    if (s.NotFound || s.Forbidden)
                    {
                        fail++;
                        continue;
                    }
                }

                ok++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bulk new product update failed for {ProductId}", p.Id);
                fail++;
            }
        }

        var notInScope = ids.Count - products.Count;
        if (ok > 0)
            TempData["Success"] = $"Updated {ok} product(s).";

        if (fail > 0 || notInScope > 0)
        {
            var parts = new List<string>();
            if (fail > 0)
                parts.Add($"{fail} product(s) could not be updated.");
            if (notInScope > 0)
            {
                parts.Add(
                    isAllTab
                        ? $"{notInScope} selected id(s) were not in this list or no longer match."
                        : $"{notInScope} selected id(s) were not new products in this list or no longer match.");
            }
            TempData["Error"] = string.Join(" ", parts);
        }
        else if (ok == 0)
        {
            TempData["Error"] = "No products were updated.";
        }

        return RedirectBack();
    }

    [HttpPost("service-register/sync-cmdb")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterSync(CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        try
        {
            var r = await _fipsCmdbProductSync.SyncActiveServiceOfferingsAsync(CurrentUserEmail, ct);
            TempData["Success"] =
                $"CMDB sync finished: {r.Updated} product(s) updated. {r.StatusSetByRules} status change(s) from sync rules. " +
                $"Skipped {r.SkippedRetired} inactive (retired) in Compass, {r.SkippedNoSysId} CMDB rows without sys_id, " +
                $"{r.SkippedNoLocalMatch} with no matching Compass product (no new rows created).";
            if (r.Errors > 0)
            {
                TempData["Error"] =
                    $"{r.Errors} error(s). " +
                    (r.ErrorSamples.Count > 0 ? string.Join(" ", r.ErrorSamples) : string.Empty);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operations CMDB product sync failed");
            TempData["Error"] = "CMDB sync failed: " + ex.Message;
        }

        return RedirectToAction(nameof(ServiceRegister), new { tab = "all" });
    }

    /// <summary>Server-sent events while bulk CMDB sync runs (for service register modal).</summary>
    [HttpPost("service-register/sync-cmdb-stream")]
    [ValidateAntiForgeryToken]
    public async Task ServiceRegisterSyncStream(CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        async Task SendEventAsync(object payload)
        {
            var json = JsonSerializer.Serialize(payload, CmdbSyncSseJsonOptions);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        try
        {
            var syncResult = await _fipsCmdbProductSync.SyncActiveServiceOfferingsAsync(
                CurrentUserEmail,
                ct,
                async update => { await SendEventAsync(update); });

            await SendEventAsync(new
            {
                phase = "complete",
                success = true,
                result = new
                {
                    syncResult.Updated,
                    syncResult.SkippedRetired,
                    syncResult.SkippedNoSysId,
                    syncResult.SkippedNoLocalMatch,
                    syncResult.StatusSetByRules,
                    syncResult.Errors,
                    errorSamples = syncResult.ErrorSamples
                }
            });
        }
        catch (OperationCanceledException)
        {
            await SendEventAsync(new { phase = "error", success = false, message = "The sync was cancelled." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operations CMDB product sync stream failed");
            await SendEventAsync(new { phase = "error", success = false, message = ex.Message });
        }
    }

    [HttpPost("service-register/product/{id:guid}/sync-cmdb-json")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterProductSyncCmdbJson(Guid id, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        try
        {
            var r = await _fipsCmdbProductSync.SyncSingleProductAsync(id, CurrentUserEmail, ct);
            return Json(new { success = r.Success, message = r.Message, statusSetByRule = r.StatusSetByRule });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operations single CMDB sync JSON failed for product {ProductId}", id);
            return Json(new { success = false, message = "Sync failed: " + ex.Message });
        }
    }

    [HttpGet("service-register/product/{id:guid}")]
    public async Task<IActionResult> ServiceRegisterProduct(Guid id, string? tab, bool edit, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        SetNav("operations-service-register");

        var product = await _db.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.CategorisationItems).ThenInclude(ci => ci.FipsCategorisationItem).ThenInclude(i => i.Group)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product == null)
            return NotFound();

        var detailTab = NormalizeServiceRegisterProductTab(tab);
        if (detailTab == "cmdb")
            edit = false;

        var email = CurrentUserEmail;
        var productIdStr = product.Id.ToString();
        var auditHistory = await _db.AuditLogs
            .Where(a => a.Entity == "CMDBProduct" && a.EntityId == productIdStr)
            .OrderByDescending(a => a.ChangedUtc)
            .Select(a => new FipsAuditRow
            {
                ChangedAt = a.ChangedUtc,
                ChangedBy = a.ChangedBy ?? a.ChangedByEmail,
                ChangeType = a.Action,
                FieldName = a.EntityReference,
                PreviousValue = a.BeforeJson,
                NewValue = a.AfterJson
            })
            .ToListAsync(ct);

        var editMode = edit && detailTab == "information";

        var vm = new FipsProductDetailViewModel
        {
            Product = product,
            CanManage = true,
            CurrentUserEmail = email,
            AuditHistory = auditHistory,
            NavContext = null,
            IsOperationsServiceRegisterProduct = true,
            CmdbSnapshotJsonFormatted = FormatCmdbSnapshotJson(product.LastCmdbSnapshotJson),
            EditMode = editMode,
            ActiveDetailTab = detailTab,
        };

        if (editMode)
        {
            await _fipsBusinessAreaLookupSync.SyncFromBusinessAreaLookupsAsync(ct);
            vm.PhaseOptions = await _db.PhaseLookups
                .Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(ct);
            vm.BusinessAreaLookupOptions =
                await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_db, product, ct);
            vm.SelectedBusinessAreaLookupIds =
                FipsBusinessAreaLookupUiHelper.GetSelectedBusinessAreaLookupIds(product, vm.BusinessAreaLookupOptions);
            vm.ChannelOptions = await _db.FipsChannels
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            vm.UserGroupOptions = await _db.FipsUserGroups
                .Where(x => x.Active && x.ParentId == null).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
            vm.TypeOptions = await _db.FipsTypes
                .Where(x => x.Active).OrderBy(x => x.DisplayOrder).ToListAsync(ct);
        }

        await FipsProductCategorisationPresentation.PopulateAsync(_db, vm, editMode, ct);

        await FipsProductRaidQuery.PopulateRaidListsAsync(_db, vm, product, ct);

        return View("~/Views/Modern/Operations/ServiceRegisterProduct.cshtml", vm);
    }

    [HttpPost("service-register/product/{id:guid}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterProductUpdate(Guid id, string? userDescription,
        int? phaseId, string? productURL,
        int[]? businessAreaLookupIds, int[]? channelIds, int[]? userGroupIds, int[]? typeIds,
        int[]? categorisationItemIds,
        int? reportingContactUserId,
        bool isEnterpriseService,
        CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        var resolvedBusinessAreaIds =
            await _fipsBusinessAreaLookupSync.ResolveToFipsBusinessAreaIdsAsync(businessAreaLookupIds ?? Array.Empty<int>(), ct);

        var email = CurrentUserEmail;
        var auditName = User.Identity?.Name ?? email;
        var outcome = await _fipsProductWrite.TryUpdateAsync(
            id,
            email,
            auditName,
            requireServiceOwnerManager: false,
            userDescription,
            phaseId,
            productURL,
            resolvedBusinessAreaIds,
            channelIds,
            userGroupIds,
            typeIds,
            categorisationItemIds,
            reportingContactUserId,
            isEnterpriseService,
            ct);

        if (outcome.NotFound)
            return NotFound();

        if (outcome.Changes.Count > 0)
            TempData["Success"] = $"Product updated: {string.Join(", ", outcome.Changes)}.";

        return RedirectToAction(nameof(ServiceRegisterProduct), new { id, tab = "information" });
    }

    [HttpPost("service-register/product/{id:guid}/change-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterProductChangeStatus(Guid id, CMDBProductStatus newStatus, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        var email = CurrentUserEmail;
        var auditName = User.Identity?.Name ?? email;
        var outcome = await _fipsProductWrite.TryChangeStatusAsync(
            id, email, auditName, requireServiceOwnerManager: false, newStatus, ct);

        if (outcome.NotFound)
            return NotFound();

        if (outcome.Changes.Count > 0)
            TempData["Success"] = "Status updated.";

        return RedirectToAction(nameof(ServiceRegisterProduct), new { id });
    }

    [HttpPost("service-register/product/{id:guid}/sync-cmdb")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterProductSyncCmdb(Guid id, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        try
        {
            var r = await _fipsCmdbProductSync.SyncSingleProductAsync(id, CurrentUserEmail, ct);
            if (r.Success)
                TempData["Success"] = r.Message;
            else
                TempData["Error"] = r.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operations single CMDB sync failed for product {ProductId}", id);
            TempData["Error"] = "CMDB sync failed: " + ex.Message;
        }

        return RedirectToAction(nameof(ServiceRegisterProduct), new { id, tab = "information" });
    }

    [HttpGet("service-register/cmdb-sync-rules")]
    public async Task<IActionResult> ServiceRegisterCmdbRules(CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        SetNav("operations-service-register");
        var rules = await _db.FipsCmdbSyncRules.AsNoTracking()
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(ct);
        return View("~/Views/Modern/Operations/ServiceRegisterCmdbRules.cshtml", rules);
    }

    [HttpPost("service-register/cmdb-sync-rules/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterCmdbRulesAdd(
        string? name,
        string fieldScope,
        string matchKind,
        string pattern,
        CMDBProductStatus targetStatus,
        int sortOrder,
        bool isActive,
        CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        fieldScope = (fieldScope ?? "").Trim();
        matchKind = (matchKind ?? "").Trim();
        pattern = (pattern ?? "").Trim();
        if (!FipsCmdbSyncRuleScopes.All.Contains(fieldScope))
        {
            TempData["Error"] = "Invalid field scope.";
            return RedirectToAction(nameof(ServiceRegisterCmdbRules));
        }
        if (!FipsCmdbSyncRuleMatchKinds.All.Contains(matchKind))
        {
            TempData["Error"] = "Match kind must be Contains or Regex.";
            return RedirectToAction(nameof(ServiceRegisterCmdbRules));
        }
        if (pattern.Length == 0)
        {
            TempData["Error"] = "Enter a pattern to match.";
            return RedirectToAction(nameof(ServiceRegisterCmdbRules));
        }
        if (pattern.Length > 2000)
        {
            TempData["Error"] = "Pattern is too long (max 2000 characters).";
            return RedirectToAction(nameof(ServiceRegisterCmdbRules));
        }
        if (targetStatus != CMDBProductStatus.Rejected && targetStatus != CMDBProductStatus.Inactive)
        {
            TempData["Error"] = "Rules may only set status to Rejected or Retired (inactive).";
            return RedirectToAction(nameof(ServiceRegisterCmdbRules));
        }

        var now = DateTime.UtcNow;
        _db.FipsCmdbSyncRules.Add(new FipsCmdbSyncRule
        {
            Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
            FieldScope = fieldScope,
            MatchKind = matchKind,
            Pattern = pattern,
            TargetStatus = targetStatus,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        });
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Rule added.";
        return RedirectToAction(nameof(ServiceRegisterCmdbRules));
    }

    [HttpPost("service-register/cmdb-sync-rules/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterCmdbRulesDelete(int id, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        var row = await _db.FipsCmdbSyncRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row == null) return NotFound();
        _db.FipsCmdbSyncRules.Remove(row);
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = "Rule deleted.";
        return RedirectToAction(nameof(ServiceRegisterCmdbRules));
    }

    [HttpPost("service-register/cmdb-sync-rules/{id:int}/toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceRegisterCmdbRulesToggle(int id, CancellationToken ct)
    {
        var blocked = await FipsDatabaseDisabledRedirectAsync();
        if (blocked != null)
            return blocked;

        var row = await _db.FipsCmdbSyncRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row == null) return NotFound();
        row.IsActive = !row.IsActive;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        TempData["Success"] = row.IsActive ? "Rule enabled." : "Rule disabled.";
        return RedirectToAction(nameof(ServiceRegisterCmdbRules));
    }

    [HttpGet("manage-triage")]
    public async Task<IActionResult> ManageTriage(string? tab = null)
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        SetNav("operations-manage-triage");
        var triageTab = NormalizeManageTriageTab(tab);
        ViewBag.TriageTab = triageTab;

        var meetings = await _db.DemandPipelineTriageMeetings.AsNoTracking()
            .OrderByDescending(m => m.MeetingDate ?? DateTime.MinValue)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync();

        var today = DateTime.UtcNow.Date;
        var upcoming = meetings.Where(m => IsUpcomingTriageMeeting(m, today))
            .OrderBy(m => m.MeetingDate ?? DateTime.MaxValue)
            .ThenBy(m => m.StartTime)
            .ToList();
        var previous = meetings.Where(m => !IsUpcomingTriageMeeting(m, today))
            .OrderByDescending(m => m.MeetingDate ?? DateTime.MinValue)
            .ThenByDescending(m => m.CreatedAt)
            .ToList();

        ViewBag.UpcomingMeetings = upcoming;
        ViewBag.PreviousMeetings = previous;

        var assignedCounts = new Dictionary<Guid, int>();
        if (meetings.Count > 0)
        {
            var meetingIds = meetings.Select(m => m.Id).ToList();
            var rows = await _db.DemandPipelineRequests.AsNoTracking()
                .Where(d => d.TriageMeetingId != null && meetingIds.Contains(d.TriageMeetingId.Value))
                .GroupBy(d => d.TriageMeetingId!.Value)
                .Select(g => new { MeetingId = g.Key, Count = g.Count() })
                .ToListAsync();
            foreach (var row in rows)
                assignedCounts[row.MeetingId] = row.Count;
        }

        ViewBag.TriageMeetingAssignedDemandCounts = assignedCounts;

        return View("~/Views/Modern/Operations/ManageTriage.cshtml");
    }

    [HttpGet("manage-triage/add")]
    public async Task<IActionResult> AddTriageMeeting()
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        SetNav("operations-manage-triage");
        ViewBag.ReturnTab = "upcoming";
        return View("~/Views/Modern/Operations/EditTriageMeeting.cshtml");
    }

    [HttpGet("manage-triage/edit/{id:guid}")]
    public async Task<IActionResult> EditTriageMeeting(Guid id, string? returnTab = null)
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        SetNav("operations-manage-triage");
        var meeting = await _db.DemandPipelineTriageMeetings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (meeting == null)
        {
            TempData["Error"] = "Meeting not found.";
            return RedirectToAction(nameof(ManageTriage), new { tab = returnTab });
        }

        ViewBag.EditingMeeting = meeting;
        ViewBag.ReturnTab = NormalizeManageTriageTab(returnTab);
        ParseChairDisplay(meeting.Chair, out var editName, out var editEmail);
        ViewBag.EditChairName = editName;
        ViewBag.EditChairEmail = editEmail;

        return View("~/Views/Modern/Operations/EditTriageMeeting.cshtml");
    }

    private static string NormalizeManageTriageTab(string? tab)
    {
        var t = (tab ?? "upcoming").Trim().ToLowerInvariant();
        return t == "previous" ? "previous" : "upcoming";
    }

    /// <summary>Scheduled meetings with no date or a date on/after today count as upcoming.</summary>
    private static bool IsUpcomingTriageMeeting(DemandPipelineTriageMeeting m, DateTime todayUtc)
    {
        if (!string.Equals(m.Status, "Scheduled", StringComparison.OrdinalIgnoreCase))
            return false;
        if (m.MeetingDate == null) return true;
        return m.MeetingDate.Value.Date >= todayUtc;
    }

    [HttpPost("manage-triage/save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTriageMeeting(
        Guid? id,
        string name,
        DateTime? meetingDate,
        string? startTime,
        string? endTime,
        string? chairName,
        string? chairEmail,
        string? returnTab = null)
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        var triageTab = NormalizeManageTriageTab(returnTab);
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "Enter a meeting name.";
            if (id.HasValue)
                return RedirectToAction(nameof(EditTriageMeeting), new { id = id.Value, returnTab = triageTab });
            return RedirectToAction(nameof(AddTriageMeeting));
        }

        var chair = BuildChairDisplay(chairName, chairEmail);
        var now = DateTime.UtcNow;
        var user = User.Identity?.Name;

        startTime = NormalizeTimeInput(startTime);
        endTime = NormalizeTimeInput(endTime);

        if (id.HasValue)
        {
            var entity = await _db.DemandPipelineTriageMeetings.FirstOrDefaultAsync(m => m.Id == id.Value);
            if (entity == null)
            {
                TempData["Error"] = "Meeting not found.";
                return RedirectToAction(nameof(ManageTriage), new { tab = triageTab });
            }

            entity.Name = name.Trim();
            entity.MeetingDate = meetingDate;
            entity.StartTime = startTime;
            entity.EndTime = endTime;
            entity.Chair = chair;
            entity.ChairUserId = null;
            entity.UpdatedAt = now;
            entity.UpdatedBy = user;
        }
        else
        {
            _db.DemandPipelineTriageMeetings.Add(new DemandPipelineTriageMeeting
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                MeetingDate = meetingDate,
                StartTime = startTime,
                EndTime = endTime,
                Chair = chair,
                ChairUserId = null,
                Status = "Scheduled",
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = user,
                UpdatedBy = user
            });
        }

        await _db.SaveChangesAsync();
        TempData["Message"] = id.HasValue ? "Triage meeting updated." : "Triage meeting created.";
        return RedirectToAction(nameof(ManageTriage), new { tab = triageTab });
    }

    [HttpPost("manage-triage/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTriageMeeting(Guid id, string? tab = null)
    {
        var blocked = await DemandDisabledRedirectAsync();
        if (blocked != null) return blocked;

        var triageTab = NormalizeManageTriageTab(tab);
        var entity = await _db.DemandPipelineTriageMeetings.FirstOrDefaultAsync(m => m.Id == id);
        if (entity == null)
        {
            TempData["Error"] = "Meeting not found.";
            return RedirectToAction(nameof(ManageTriage), new { tab = triageTab });
        }

        var assignedCount = await _db.DemandPipelineRequests.CountAsync(d => d.TriageMeetingId == id);
        if (assignedCount > 0)
        {
            TempData["Error"] = assignedCount == 1
                ? "There is 1 demand assigned to this meeting. Remove the assignment before deleting."
                : $"There are {assignedCount} demands assigned to this meeting. Remove the assignments before deleting.";
            return RedirectToAction(nameof(ManageTriage), new { tab = triageTab });
        }

        _db.DemandPipelineTriageMeetings.Remove(entity);
        await _db.SaveChangesAsync();
        TempData["Message"] = "Triage meeting deleted.";
        return RedirectToAction(nameof(ManageTriage), new { tab = triageTab });
    }

    private static string? NormalizeTimeInput(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return null;
        t = t.Trim();
        return t.Length > 20 ? t[..20] : t;
    }

    private static string? BuildChairDisplay(string? name, string? email)
    {
        var n = string.IsNullOrWhiteSpace(name) ? "" : name.Trim();
        var e = string.IsNullOrWhiteSpace(email) ? "" : email.Trim();
        if (n.Length == 0 && e.Length == 0) return null;
        if (n.Length == 0) return e.Length > 450 ? e[..450] : e;
        if (e.Length == 0) return n.Length > 450 ? n[..450] : n;
        var combined = $"{n} ({e})";
        return combined.Length > 450 ? combined[..450] : combined;
    }

    private static void ParseChairDisplay(string? chair, out string? name, out string? email)
    {
        name = null;
        email = null;
        if (string.IsNullOrWhiteSpace(chair)) return;
        var m = Regex.Match(chair.Trim(), @"^(.*)\(([^)]+)\)\s*$");
        if (m.Success)
        {
            name = m.Groups[1].Value.Trim();
            email = m.Groups[2].Value.Trim();
        }
        else
            name = chair.Trim();
    }
}
