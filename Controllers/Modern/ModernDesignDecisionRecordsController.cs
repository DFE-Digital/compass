using Compass.Data;
using Compass.Filters;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.Ddr;
using Compass.Models.Fips;
using Compass.Services;
using Compass.ViewModels.Modern.Ddr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;

namespace Compass.Controllers.Modern;

/// <summary>
/// Design Decision Records (DDRs). All routes live under <c>/modern/design-decision-records</c>
/// and are gated by <see cref="DdrFeatureGateFilter"/> so they 404-equivalent (redirect to the
/// modern dashboard) when the DDR feature flag is off (BR-014 / BR-015).
/// </summary>
[Authorize]
[Route("modern/design-decision-records")]
[ServiceFilter(typeof(DdrFeatureGateFilter))]
public class ModernDesignDecisionRecordsController : Controller
{
    private readonly CompassDbContext _db;
    private readonly IPermissionService _permissions;
    private readonly ILogger<ModernDesignDecisionRecordsController> _logger;

    public ModernDesignDecisionRecordsController(
        CompassDbContext db,
        IPermissionService permissions,
        ILogger<ModernDesignDecisionRecordsController> logger)
    {
        _db = db;
        _permissions = permissions;
        _logger = logger;
    }

    private void SetChrome(string subNavItem)
    {
        ViewBag.MainNavSection = "ddr";
        ViewBag.SubNavItem = subNavItem;
    }

    private string GetUserEmail() =>
        User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue("preferred_username")
        ?? User.Identity?.Name
        ?? string.Empty;

    private string GetUserDisplayName() =>
        User.FindFirstValue("name")
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? GetUserEmail();

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Register));

    /// <summary>DDR dashboard — counts by category and deviation type with links to the filtered register.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken ct = default)
    {
        SetChrome("ddr-dashboard");

        var baseQ = _db.DesignDecisionRecords.AsNoTracking()
            .Where(r => r.DeletedAt == null);

        var total = await baseQ.CountAsync(ct);

        var byCategoryRaw = await baseQ
            .GroupBy(r => r.Category)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byCategory = byCategoryRaw
            .OrderByDescending(x => x.Count)
            .Select(x => new DdrDashboardBreakdownRow
            {
                Label = x.Key ?? "(unknown)",
                Count = x.Count,
                RegisterUrl = Url.Action(nameof(Register), "ModernDesignDecisionRecords", new { category = x.Key }) ?? "#",
            })
            .ToList();

        var typedDeviations = await baseQ
            .Where(r => r.DeviationFlag && r.DeviationType != null && r.DeviationType != "")
            .GroupBy(r => r.DeviationType!)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var unsetCount = await baseQ.CountAsync(
            r => r.DeviationFlag && (r.DeviationType == null || r.DeviationType == ""),
            ct);

        var byDeviation = typedDeviations
            .Select(x => new DdrDashboardBreakdownRow
            {
                Label = x.Key,
                Count = x.Count,
                RegisterUrl = Url.Action(nameof(Register), "ModernDesignDecisionRecords",
                    new { deviation = "true", deviationType = x.Key }) ?? "#",
            })
            .ToList();

        if (unsetCount > 0)
        {
            byDeviation.Add(new DdrDashboardBreakdownRow
            {
                Label = "Type not set",
                Count = unsetCount,
                RegisterUrl = Url.Action(nameof(Register), "ModernDesignDecisionRecords",
                    new { deviation = "true", deviationType = DdrRegisterQueryValues.UnsetDeviationType }) ?? "#",
            });
        }

        byDeviation.Sort((a, b) => b.Count.CompareTo(a.Count));

        var model = new DdrDashboardViewModel
        {
            TotalDdrs = total,
            ByCategory = byCategory,
            ByDeviationType = byDeviation,
        };

        return View("~/Views/Modern/DesignDecisionRecords/Dashboard.cshtml", model);
    }

    /// <summary>
    /// Register / search view (§10.1). Supports keyword search across reference + title + context,
    /// filters by category, status, deviation / deviation type, retrospective, review-overdue, plus pre-filter
    /// by linked product or work item (used from the embedded panels).
    /// </summary>
    [HttpGet("register")]
    public async Task<IActionResult> Register(
        string? search,
        string? q,
        string? category,
        string? status,
        string? deviation,
        string? deviationType,
        string? retrospective,
        string? overdue,
        Guid? productId,
        int? workItemId,
        CancellationToken ct = default)
    {
        SetChrome("ddr-register");

        static bool? ParseYes(string? v) =>
            string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ? true : null;

        var deviationFilter = ParseYes(deviation);
        var retrospectiveFilter = ParseYes(retrospective);
        var overdueFilter = ParseYes(overdue);
        var searchTerm = string.IsNullOrWhiteSpace(search) ? q?.Trim() : search?.Trim();
        var deviationTypeFilter = string.IsNullOrWhiteSpace(deviationType) ? null : deviationType.Trim();

        var baseQuery = _db.DesignDecisionRecords.AsNoTracking()
            .Where(r => r.DeletedAt == null);

        if (productId is { } pid)
        {
            baseQuery = baseQuery.Where(r => r.ProductLinks.Any(pl => pl.FipsProductId == pid));
        }
        if (workItemId is { } wid)
        {
            baseQuery = baseQuery.Where(r => r.WorkItemLinks.Any(wl => wl.WorkItemId == wid));
        }

        var totalCount = await baseQuery.CountAsync(ct);

        var filtered = baseQuery;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var needle = searchTerm.Trim().ToLower();
            filtered = filtered.Where(r =>
                r.Reference.ToLower().Contains(needle) ||
                r.ShortTitle.ToLower().Contains(needle) ||
                r.ContextProblemStatement.ToLower().Contains(needle));
        }
        if (!string.IsNullOrWhiteSpace(category))
            filtered = filtered.Where(r => r.Category == category);
        if (!string.IsNullOrWhiteSpace(status))
            filtered = filtered.Where(r => r.Status == status);
        if (deviationFilter == true)
            filtered = filtered.Where(r => r.DeviationFlag);
        if (!string.IsNullOrWhiteSpace(deviationTypeFilter))
        {
            if (string.Equals(deviationTypeFilter, DdrRegisterQueryValues.UnsetDeviationType, StringComparison.Ordinal))
                filtered = filtered.Where(r =>
                    r.DeviationFlag && (r.DeviationType == null || r.DeviationType == ""));
            else
                filtered = filtered.Where(r =>
                    r.DeviationFlag && r.DeviationType == deviationTypeFilter);
        }
        if (retrospectiveFilter == true)
            filtered = filtered.Where(r => r.RetrospectiveRecord);

        var today = DateTime.UtcNow.Date;
        var openStatuses = new[] { "Proposed", "In use", "Approved", "Under review" };
        if (overdueFilter == true)
        {
            filtered = filtered.Where(r =>
                r.ReviewDate != null
                && r.ReviewDate < today
                && openStatuses.Contains(r.Status));
        }

        var raw = await filtered
            .OrderByDescending(r => r.UpdatedAt)
            .Take(500)
            .Select(r => new
            {
                r.Id,
                r.Reference,
                r.ShortTitle,
                r.Category,
                r.Status,
                r.DeviationFlag,
                r.ReviewDate,
                r.UpdatedAt,
                r.AuthorDisplayName,
                ProductIds = r.ProductLinks.Select(p => p.FipsProductId).ToList(),
                WorkItemIds = r.WorkItemLinks.Select(w => w.WorkItemId).ToList(),
            })
            .ToListAsync(ct);

        var allProductIds = raw.SelectMany(r => r.ProductIds).Distinct().ToList();
        var allWorkItemIds = raw.SelectMany(r => r.WorkItemIds).Distinct().ToList();

        var productTitles = await _db.CMDBProducts.AsNoTracking()
            .Where(p => allProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title, ct);

        var workItemTitles = await _db.Projects.AsNoTracking()
            .Where(p => allWorkItemIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title, ct);

        string? prefilterProductTitle = null;
        if (productId is { } pidFilter && productTitles.TryGetValue(pidFilter, out var pTitle))
            prefilterProductTitle = pTitle;
        else if (productId is { } pidFilter2)
            prefilterProductTitle = (await _db.CMDBProducts.AsNoTracking()
                .Where(p => p.Id == pidFilter2)
                .Select(p => p.Title)
                .FirstOrDefaultAsync(ct)) ?? "Selected product";

        string? prefilterWorkItemTitle = null;
        if (workItemId is { } widFilter && workItemTitles.TryGetValue(widFilter, out var wTitle))
            prefilterWorkItemTitle = wTitle;
        else if (workItemId is { } widFilter2)
            prefilterWorkItemTitle = (await _db.Projects.AsNoTracking()
                .Where(p => p.Id == widFilter2)
                .Select(p => p.Title)
                .FirstOrDefaultAsync(ct)) ?? "Selected work item";

        var rows = raw.Select(r => new DdrRegisterRow
        {
            Id = r.Id,
            Reference = r.Reference,
            ShortTitle = r.ShortTitle,
            Category = r.Category,
            Status = r.Status,
            DeviationFlag = r.DeviationFlag,
            ReviewDate = r.ReviewDate,
            UpdatedAt = r.UpdatedAt,
            AuthorDisplayName = r.AuthorDisplayName,
            ProductTitles = r.ProductIds.Select(id => productTitles.TryGetValue(id, out var t) ? t : "(unknown product)").ToList(),
            WorkItemTitles = r.WorkItemIds.Select(id => workItemTitles.TryGetValue(id, out var t) ? t : "(unknown work item)").ToList(),
            ReviewOverdue = r.ReviewDate is { } d && d < today && openStatuses.Contains(r.Status),
        }).ToList();

        var model = new DdrRegisterViewModel
        {
            Search = searchTerm,
            Category = category,
            Status = status,
            Deviation = deviationFilter,
            DeviationType = deviationTypeFilter,
            RetrospectiveOnly = retrospectiveFilter,
            ReviewOverdue = overdueFilter,
            TotalCount = totalCount,
            FilteredCount = rows.Count,
            Rows = rows,
            ProductIdFilter = productId,
            ProductTitle = prefilterProductTitle,
            WorkItemIdFilter = workItemId,
            WorkItemTitle = prefilterWorkItemTitle,
            CanCreate = true,
        };

        var baseUrl = Url.Action(nameof(Register), "ModernDesignDecisionRecords") ?? "";
        var hidden = new List<KeyValuePair<string, string>>();
        if (productId.HasValue)
            hidden.Add(new KeyValuePair<string, string>("productId", productId.Value.ToString()));
        if (workItemId.HasValue)
            hidden.Add(new KeyValuePair<string, string>("workItemId", workItemId.Value.ToString()));

        object? chipRoute = productId.HasValue || workItemId.HasValue
            ? new { productId, workItemId }
            : null;

        var categoryOptions = new List<SearchAndFilterOption> { new() { Value = "", Text = "All categories" } }
            .Concat(DdrControlledValues.Categories.Select(c => new SearchAndFilterOption { Value = c, Text = c }))
            .ToList();
        var statusOptions = new List<SearchAndFilterOption> { new() { Value = "", Text = "All statuses" } }
            .Concat(DdrControlledValues.Statuses.Select(s => new SearchAndFilterOption { Value = s, Text = s }))
            .ToList();

        var sf = new SearchAndFilterViewModel
        {
            IdPrefix = "ddr",
            SearchPlaceholder = "Search by reference, title or context…",
            SearchValue = model.Search,
            FormActionUrl = baseUrl,
            ClearUrl = Url.Action(nameof(Register), "ModernDesignDecisionRecords", chipRoute) ?? baseUrl,
            HiddenFields = hidden,
            Fields = new List<SearchAndFilterFieldViewModel>
            {
                new()
                {
                    Label = "Category",
                    Name = "category",
                    SelectedValue = category ?? "",
                    Options = categoryOptions,
                },
                new()
                {
                    Label = "Status",
                    Name = "status",
                    SelectedValue = status ?? "",
                    Options = statusOptions,
                },
                new()
                {
                    Label = "Deviation",
                    Name = "deviation",
                    SelectedValue = deviationFilter == true ? "true" : "",
                    Options = new List<SearchAndFilterOption>
                    {
                        new() { Value = "", Text = "All" },
                        new() { Value = "true", Text = "Deviations only" },
                    },
                },
                new()
                {
                    Label = "Deviation type",
                    Name = "deviationType",
                    SelectedValue = deviationTypeFilter ?? "",
                    Options = new List<SearchAndFilterOption> { new() { Value = "", Text = "All types" } }
                        .Concat(DdrControlledValues.DeviationTypes.Select(t =>
                            new SearchAndFilterOption { Value = t, Text = t }))
                        .Append(new SearchAndFilterOption
                        {
                            Value = DdrRegisterQueryValues.UnsetDeviationType,
                            Text = "Flagged — type not set",
                        })
                        .ToList(),
                },
                new()
                {
                    Label = "Retrospective",
                    Name = "retrospective",
                    SelectedValue = retrospectiveFilter == true ? "true" : "",
                    Options = new List<SearchAndFilterOption>
                    {
                        new() { Value = "", Text = "All" },
                        new() { Value = "true", Text = "Retrospective only" },
                    },
                },
                new()
                {
                    Label = "Review",
                    Name = "overdue",
                    SelectedValue = overdueFilter == true ? "true" : "",
                    Options = new List<SearchAndFilterOption>
                    {
                        new() { Value = "", Text = "All" },
                        new() { Value = "true", Text = "Review overdue" },
                    },
                },
            },
        };
        sf.ActiveChips = SearchAndFilterActiveChipsBuilder.FromViewModel(
            sf, Url, nameof(Register), "ModernDesignDecisionRecords", chipRoute);
        ViewBag.SearchAndFilter = sf;

        return View("~/Views/Modern/DesignDecisionRecords/Register.cshtml", model);
    }

    [HttpGet("oversight")]
    public async Task<IActionResult> Oversight(string? filter, CancellationToken ct = default)
    {
        SetChrome("ddr-oversight");

        var today = DateTime.UtcNow.Date;
        var openStatuses = new[] { "Proposed", "In use", "Approved", "Under review" };

        var queue = _db.DesignDecisionRecords.AsNoTracking()
            .Where(r => r.DeletedAt == null);

        var activeFilter = (filter ?? "new").Trim().ToLowerInvariant();
        switch (activeFilter)
        {
            case "deviations":
                queue = queue.Where(r => r.DeviationFlag);
                break;
            case "accessibility":
                queue = queue.Where(r => r.Category == "Accessibility");
                break;
            case "pending-insight":
                queue = queue.Where(r =>
                    r.SubmittedAt != null && !r.InsightClassifications.Any());
                break;
            case "no-classification":
                queue = queue.Where(r => !r.InsightClassifications.Any());
                break;
            case "overdue":
                queue = queue.Where(r => r.ReviewDate != null && r.ReviewDate < today && openStatuses.Contains(r.Status));
                break;
            case "all":
                break;
            default: // "new"
                queue = queue.Where(r => r.SubmittedAt != null);
                break;
        }

        var rawRows = await queue
            .OrderByDescending(r => r.SubmittedAt ?? r.UpdatedAt)
            .Take(200)
            .Select(r => new DdrRegisterRow
            {
                Id = r.Id,
                Reference = r.Reference,
                ShortTitle = r.ShortTitle,
                Category = r.Category,
                Status = r.Status,
                DeviationFlag = r.DeviationFlag,
                ReviewDate = r.ReviewDate,
                UpdatedAt = r.UpdatedAt,
                AuthorDisplayName = r.AuthorDisplayName,
            })
            .ToListAsync(ct);

        var counts = new Dictionary<string, int>
        {
            ["new"] = await _db.DesignDecisionRecords.AsNoTracking()
                .CountAsync(r => r.DeletedAt == null && r.SubmittedAt != null, ct),
            ["pending-insight"] = await _db.DesignDecisionRecords.AsNoTracking()
                .CountAsync(r => r.DeletedAt == null && r.SubmittedAt != null && !r.InsightClassifications.Any(), ct),
            ["deviations"] = await _db.DesignDecisionRecords.AsNoTracking()
                .CountAsync(r => r.DeletedAt == null && r.DeviationFlag, ct),
            ["accessibility"] = await _db.DesignDecisionRecords.AsNoTracking()
                .CountAsync(r => r.DeletedAt == null && r.Category == "Accessibility", ct),
            ["no-classification"] = await _db.DesignDecisionRecords.AsNoTracking()
                .CountAsync(r => r.DeletedAt == null && !r.InsightClassifications.Any(), ct),
            ["overdue"] = await _db.DesignDecisionRecords.AsNoTracking()
                .CountAsync(r => r.DeletedAt == null && r.ReviewDate != null && r.ReviewDate < today && openStatuses.Contains(r.Status), ct),
        };

        var model = new DdrOversightViewModel
        {
            ActiveFilter = activeFilter,
            Rows = rawRows,
            Counts = counts,
        };

        return View("~/Views/Modern/DesignDecisionRecords/Oversight.cshtml", model);
    }

    [HttpGet("{reference}")]
    public async Task<IActionResult> Detail(string reference, CancellationToken ct = default)
    {
        SetChrome("ddr-register");

        var record = await _db.DesignDecisionRecords
            .Include(r => r.Alternatives)
            .Include(r => r.Evidence)
            .Include(r => r.StandardLinks)
            .Include(r => r.ComponentPatternLinks)
            .Include(r => r.RelatedRecords)
            .Include(r => r.Comments)
            .Include(r => r.InsightClassifications)
            .Include(r => r.RecommendedFollowUps)
            .Include(r => r.GitHubIssueLinks)
            .Include(r => r.AuditEvents)
            .Include(r => r.ProductLinks)
            .Include(r => r.WorkItemLinks)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Reference == reference && r.DeletedAt == null, ct);

        if (record is null)
        {
            return NotFound();
        }

        var productIds = record.ProductLinks.Select(p => p.FipsProductId).Distinct().ToList();
        var workItemIds = record.WorkItemLinks.Select(w => w.WorkItemId).Distinct().ToList();

        var products = await _db.CMDBProducts.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new DdrDetailViewModel.LinkedProductRow { ProductId = p.Id, Title = p.Title })
            .ToListAsync(ct);

        var workItems = await _db.Projects.AsNoTracking()
            .Where(p => workItemIds.Contains(p.Id))
            .Select(p => new DdrDetailViewModel.LinkedWorkItemRow { WorkItemId = p.Id, Title = p.Title })
            .ToListAsync(ct);

        var today = DateTime.UtcNow.Date;
        var openStatuses = new[] { "Proposed", "In use", "Approved", "Under review" };

        var canReview = await _permissions.IsCentralOperationsAdminOrSuperAdminAsync(GetUserEmail());

        var model = new DdrDetailViewModel
        {
            Record = record,
            Alternatives = record.Alternatives.OrderBy(a => a.SortOrder).ToList(),
            Evidence = record.Evidence.ToList(),
            Standards = record.StandardLinks.ToList(),
            ComponentsAndPatterns = record.ComponentPatternLinks.ToList(),
            Related = record.RelatedRecords.ToList(),
            Comments = record.Comments.OrderByDescending(c => c.CreatedAt).ToList(),
            InsightClassifications = record.InsightClassifications.OrderByDescending(c => c.CreatedAt).ToList(),
            RecommendedFollowUps = record.RecommendedFollowUps.OrderByDescending(f => f.CreatedAt).ToList(),
            GitHubIssueLinks = record.GitHubIssueLinks.ToList(),
            AuditEvents = record.AuditEvents.OrderByDescending(a => a.CreatedAt).ToList(),
            LinkedProducts = products,
            LinkedWorkItems = workItems,
            CanEdit = string.Equals(record.CreatedBy, GetUserEmail(), StringComparison.OrdinalIgnoreCase) || canReview,
            CanReview = canReview,
            ReviewOverdue = record.ReviewDate is { } d && d < today && openStatuses.Contains(record.Status),
        };

        return View("~/Views/Modern/DesignDecisionRecords/Detail.cshtml", model);
    }

    [HttpGet("new")]
    public async Task<IActionResult> New(Guid? productId, int? workItemId, int? step, string? reference, CancellationToken ct = default)
    {
        SetChrome("ddr-new");

        DdrCreateViewModel model;
        if (!string.IsNullOrWhiteSpace(reference))
        {
            var fromDraft = await MapDraftToCreateViewModelAsync(reference.Trim(), ct);
            if (fromDraft is null)
                return NotFound();
            model = fromDraft;
            ViewBag.DdrWizardShowCompletion = true;
        }
        else
        {
            model = new DdrCreateViewModel
            {
                PreFilledProductId = productId,
                PreFilledWorkItemId = workItemId,
                Status = "Draft",
            };
            if (productId is { } pid) model.LinkedProductIds.Add(pid);
            if (workItemId is { } wid) model.LinkedWorkItemIds.Add(wid);
            ViewBag.DdrWizardShowCompletion = false;
        }

        var w = step ?? 1;
        if (w < 1) w = 1;
        if (w > DdrCreateWizardHelper.StepCount) w = DdrCreateWizardHelper.StepCount;
        ViewBag.WizardStep = w;

        await PopulateCreateOptionsAsync(model, ct);
        return View("~/Views/Modern/DesignDecisionRecords/New.cshtml", model);
    }

    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> New(
        DdrCreateViewModel input,
        int? wizardStep,
        int? reviewDateDay,
        int? reviewDateMonth,
        int? reviewDateYear,
        CancellationToken ct = default)
    {
        SetChrome("ddr-new");

        ParseLinkedIdsFromForm(input);

        GovUkDateBinding.BindGovUkDate(ModelState, nameof(input.ReviewDate), reviewDateDay, reviewDateMonth, reviewDateYear,
            required: false, out var reviewUtc);
        if (!ModelState.ContainsKey(nameof(input.ReviewDate)))
            input.ReviewDate = reviewUtc;

        var isSubmit = string.Equals((input.Action ?? "draft").Trim(), "submit", StringComparison.OrdinalIgnoreCase);

        var ws = wizardStep ?? 1;
        if (ws < 1) ws = 1;
        if (ws > DdrCreateWizardHelper.StepCount) ws = DdrCreateWizardHelper.StepCount;
        ViewBag.WizardStep = ws;
        ViewBag.DdrWizardShowCompletion = !string.IsNullOrWhiteSpace(input.EditingReference);

        if (!string.IsNullOrWhiteSpace(input.EditingReference))
            return await SaveExistingDraftOrSubmitAsync(input, isSubmit, ws, ct);

        if (isSubmit)
        {
            EnsureAtLeastOneLinkedScope(input);
            ApplyConditionalValidation(input);
            if (!DdrCreateWizardHelper.AllSectionsComplete(input))
            {
                ModelState.AddModelError(string.Empty,
                    "Complete all sections before submitting your DDR. Use the progress step to see what is missing.");
            }
        }
        else
        {
            ModelState.Clear();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateOptionsAsync(input, ct);
            return View("~/Views/Modern/DesignDecisionRecords/New.cshtml", input);
        }

        var status = isSubmit
            ? (string.IsNullOrWhiteSpace(input.Status) || input.Status == "Draft" ? "Proposed" : input.Status!)
            : "Draft";

        var now = DateTime.UtcNow;
        var email = GetUserEmail();
        var displayName = GetUserDisplayName();

        var categoryResolved = isSubmit
            ? input.Category!
            : TruncateUtf16(string.IsNullOrWhiteSpace(input.Category) ? "Other" : input.Category.Trim(), 80);
        var shortTitleResolved = isSubmit
            ? input.ShortTitle!
            : TruncateUtf16(string.IsNullOrWhiteSpace(input.ShortTitle) ? "Draft (untitled)" : input.ShortTitle.Trim(), 120);

        var record = new DesignDecisionRecord
        {
            Reference = await GenerateReferenceAsync(ct),
            Category = categoryResolved,
            AuthorDisplayName = displayName,
            ShortTitle = shortTitleResolved,
            ContextProblemStatement = input.ContextProblemStatement ?? string.Empty,
            Decision = input.Decision ?? string.Empty,
            Rationale = input.Rationale ?? string.Empty,
            ConsequencesTradeoffs = input.ConsequencesTradeoffs ?? string.Empty,
            DeviationFlag = input.DeviationFlag,
            DeviationType = input.DeviationFlag ? input.DeviationType : null,
            DeviationDetails = input.DeviationFlag ? input.DeviationDetails : null,
            ApprovalRoute = input.DeviationFlag ? input.ApprovalRoute : null,
            ApprovedBy = input.ApprovedBy,
            Status = status,
            ReviewTrigger = input.ReviewTrigger ?? string.Empty,
            ReviewDate = input.ReviewDate,
            RetrospectiveRecord = input.RetrospectiveRecord,
            OriginalDecisionDate = input.RetrospectiveRecord ? input.OriginalDecisionDate : null,
            RetrospectiveContext = input.RetrospectiveRecord ? input.RetrospectiveContext : null,
            CurrentValidity = input.RetrospectiveRecord ? input.CurrentValidity : null,
            CurrentValidityRationale = input.RetrospectiveRecord ? input.CurrentValidityRationale : null,
            MessageToDesignOps = input.MessageToDesignOps,
            CreatedAt = now,
            CreatedBy = email,
            UpdatedAt = now,
            UpdatedBy = email,
            SubmittedAt = isSubmit ? now : null,
            SubmittedBy = isSubmit ? email : null,
        };

        AppendAlternativesEvidenceStandardsAndLinks(record, input, now, email);

        record.AuditEvents.Add(new DdrAuditEvent
        {
            EventType = isSubmit ? "Submitted" : "Created",
            CreatedAt = now,
            CreatedBy = email,
        });

        _db.DesignDecisionRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        TempData["DdrCreatedReference"] = record.Reference;
        TempData["DdrCreatedAction"] = isSubmit ? "submitted" : "saved as draft";

        if (isSubmit)
            return RedirectToAction(nameof(Confirmation), new { reference = record.Reference });

        TempData["DdrDraftSaved"] = true;
        return RedirectToAction(nameof(New), new { reference = record.Reference, step = ws });
    }

    [HttpGet("{reference}/confirmation")]
    public async Task<IActionResult> Confirmation(string reference, CancellationToken ct = default)
    {
        SetChrome("ddr-register");

        var record = await _db.DesignDecisionRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Reference == reference && r.DeletedAt == null, ct);
        if (record is null) return NotFound();

        ViewBag.DdrAction = TempData["DdrCreatedAction"] as string ?? "saved";
        return View("~/Views/Modern/DesignDecisionRecords/Confirmation.cshtml", record);
    }

    /// <summary>Soft-delete a draft — only the original author (drafter) may delete.</summary>
    [HttpPost("{reference}/delete-draft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDraft(string reference, CancellationToken ct = default)
    {
        var email = GetUserEmail();
        var record = await _db.DesignDecisionRecords
            .FirstOrDefaultAsync(r => r.Reference == reference && r.DeletedAt == null, ct);

        if (record is null
            || !string.Equals(record.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.CreatedBy, email, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var now = DateTime.UtcNow;
        record.DeletedAt = now;
        record.DeletedBy = email;
        record.UpdatedAt = now;
        record.UpdatedBy = email;
        record.AuditEvents.Add(new DdrAuditEvent
        {
            EventType = "Draft deleted",
            CreatedAt = now,
            CreatedBy = email,
        });

        await _db.SaveChangesAsync(ct);

        TempData["DdrDraftDeleted"] = true;
        return RedirectToAction(nameof(Register));
    }

    [HttpPost("{reference}/comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(string reference, string commentText, string? commentType, CancellationToken ct = default)
    {
        var record = await _db.DesignDecisionRecords
            .FirstOrDefaultAsync(r => r.Reference == reference && r.DeletedAt == null, ct);
        if (record is null) return NotFound();

        if (string.IsNullOrWhiteSpace(commentText))
        {
            TempData["DdrCommentError"] = "Enter a comment before saving.";
            return RedirectToAction(nameof(Detail), new { reference });
        }

        var trimmedComment = commentText.Trim();
        if (trimmedComment.Length > 4000)
        {
            TempData["DdrCommentError"] = "Comment must be 4,000 characters or fewer.";
            return RedirectToAction(nameof(Detail), new { reference });
        }

        var now = DateTime.UtcNow;
        var email = GetUserEmail();
        record.Comments.Add(new DdrComment
        {
            CommentText = trimmedComment,
            CommentType = string.IsNullOrWhiteSpace(commentType) ? "Comment" : commentType,
            CreatedBy = email,
            CreatedByName = GetUserDisplayName(),
            CreatedAt = now,
        });
        record.AuditEvents.Add(new DdrAuditEvent
        {
            EventType = "Comment added",
            CreatedAt = now,
            CreatedBy = email,
        });
        record.UpdatedAt = now;
        record.UpdatedBy = email;
        await _db.SaveChangesAsync(ct);

        return RedirectToAction(nameof(Detail), new { reference });
    }

    /// <summary>Generates the next <c>DDR-####</c> reference. Simple sequential generator
    /// keyed on existing rows. Concurrency is rare for this MVP and the unique index on
    /// <c>Reference</c> guards against duplicates.</summary>
    private async Task<string> GenerateReferenceAsync(CancellationToken ct)
    {
        var existing = await _db.DesignDecisionRecords.AsNoTracking()
            .Select(r => r.Reference)
            .ToListAsync(ct);
        var maxSeq = existing
            .Select(r =>
            {
                if (r.StartsWith("DDR-", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(r.Substring(4), out var n)) return n;
                return 0;
            })
            .DefaultIfEmpty(0)
            .Max();
        return $"DDR-{maxSeq + 1:D4}";
    }

    /// <summary>Apply ddr.md §11 conditional validation that DataAnnotations cannot express.</summary>
    private void ApplyConditionalValidation(DdrCreateViewModel input)
    {
        if (input.DeviationFlag)
        {
            if (string.IsNullOrWhiteSpace(input.DeviationType))
                ModelState.AddModelError(nameof(input.DeviationType), "Select the type of deviation.");
            if (string.IsNullOrWhiteSpace(input.DeviationDetails))
                ModelState.AddModelError(nameof(input.DeviationDetails), "Explain what the decision deviates from and why.");
            else if (input.DeviationDetails.Length is < 50 or > 3000)
                ModelState.AddModelError(nameof(input.DeviationDetails), "Deviation details must be between 50 and 3,000 characters.");
            if (string.IsNullOrWhiteSpace(input.ApprovalRoute))
                ModelState.AddModelError(nameof(input.ApprovalRoute), "Select the approval route.");
            if (input.ReviewDate is null)
                ModelState.AddModelError(nameof(input.ReviewDate), "Set a review date for this deviation.");
        }

        if (input.ReviewDate is { } d && d.Date < DateTime.UtcNow.Date)
            ModelState.AddModelError(nameof(input.ReviewDate), "Review date must be today or in the future.");

        if (input.RetrospectiveRecord)
        {
            if (string.IsNullOrWhiteSpace(input.RetrospectiveContext))
                ModelState.AddModelError(nameof(input.RetrospectiveContext), "Explain why this historic decision is being recorded now.");
            else if (input.RetrospectiveContext.Length is < 50 or > 2000)
                ModelState.AddModelError(nameof(input.RetrospectiveContext), "Retrospective context must be between 50 and 2,000 characters.");
            if (string.IsNullOrWhiteSpace(input.CurrentValidity))
                ModelState.AddModelError(nameof(input.CurrentValidity), "Select whether the decision is still valid.");
            if (!string.IsNullOrWhiteSpace(input.CurrentValidity)
                && (input.CurrentValidity == "Partially valid" || input.CurrentValidity == "No longer valid" || input.CurrentValidity == "Unknown")
                && string.IsNullOrWhiteSpace(input.CurrentValidityRationale))
            {
                ModelState.AddModelError(nameof(input.CurrentValidityRationale),
                    "Explain why the historic decision has limited or no current validity.");
            }
        }

        // Alternatives — at least one is required (BR-validation §11).
        var altLines = (input.AlternativesText ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        if (altLines.Count == 0)
            ModelState.AddModelError(nameof(input.AlternativesText), "Add at least one alternative considered.");
        else if (altLines.Count > 10)
            ModelState.AddModelError(nameof(input.AlternativesText), "Add at most 10 alternatives.");
        else if (altLines.Any(l => l.Length < 20))
            ModelState.AddModelError(nameof(input.AlternativesText), "Each alternative must be at least 20 characters.");
        else if (altLines.Any(l => l.Length > 1500))
            ModelState.AddModelError(nameof(input.AlternativesText), "Each alternative must be 1,500 characters or fewer.");
    }

    /// <summary>BR-001: a DDR must be linked to at least one product or work item.</summary>
    private void EnsureAtLeastOneLinkedScope(DdrCreateViewModel input)
    {
        if (input.LinkedProductIds.Count == 0 && input.LinkedWorkItemIds.Count == 0)
        {
            ModelState.AddModelError(nameof(input.LinkedProductIds), "Select a product or work item.");
        }
    }

    private async Task<IActionResult> SaveExistingDraftOrSubmitAsync(DdrCreateViewModel input, bool isSubmit, int ws, CancellationToken ct)
    {
        var email = GetUserEmail();
        var refKey = input.EditingReference!.Trim();
        var record = await _db.DesignDecisionRecords
            .Include(r => r.Alternatives)
            .Include(r => r.Evidence)
            .Include(r => r.StandardLinks)
            .Include(r => r.ProductLinks)
            .Include(r => r.WorkItemLinks)
            .FirstOrDefaultAsync(r => r.Reference == refKey && r.DeletedAt == null, ct);

        if (record is null
            || !string.Equals(record.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.CreatedBy, email, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        if (isSubmit)
        {
            EnsureAtLeastOneLinkedScope(input);
            ApplyConditionalValidation(input);
            if (!DdrCreateWizardHelper.AllSectionsComplete(input))
            {
                ModelState.AddModelError(string.Empty,
                    "Complete all sections before submitting your DDR. Use the progress step to see what is missing.");
            }
        }
        else
        {
            ModelState.Clear();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateOptionsAsync(input, ct);
            return View("~/Views/Modern/DesignDecisionRecords/New.cshtml", input);
        }

        var now = DateTime.UtcNow;
        var displayName = GetUserDisplayName();

        var categoryResolved = isSubmit
            ? input.Category!
            : TruncateUtf16(string.IsNullOrWhiteSpace(input.Category) ? "Other" : input.Category.Trim(), 80);
        var shortTitleResolved = isSubmit
            ? input.ShortTitle!
            : TruncateUtf16(string.IsNullOrWhiteSpace(input.ShortTitle) ? "Draft (untitled)" : input.ShortTitle.Trim(), 120);

        record.Category = categoryResolved;
        record.ShortTitle = shortTitleResolved;
        record.AuthorDisplayName = displayName;
        record.ContextProblemStatement = input.ContextProblemStatement ?? string.Empty;
        record.Decision = input.Decision ?? string.Empty;
        record.Rationale = input.Rationale ?? string.Empty;
        record.ConsequencesTradeoffs = input.ConsequencesTradeoffs ?? string.Empty;
        record.DeviationFlag = input.DeviationFlag;
        record.DeviationType = input.DeviationFlag ? input.DeviationType : null;
        record.DeviationDetails = input.DeviationFlag ? input.DeviationDetails : null;
        record.ApprovalRoute = input.DeviationFlag ? input.ApprovalRoute : null;
        record.ApprovedBy = input.ApprovedBy;
        record.Status = isSubmit
            ? (string.IsNullOrWhiteSpace(input.Status) || input.Status == "Draft" ? "Proposed" : input.Status!)
            : "Draft";
        record.ReviewTrigger = input.ReviewTrigger ?? string.Empty;
        record.ReviewDate = input.ReviewDate;
        record.RetrospectiveRecord = input.RetrospectiveRecord;
        record.OriginalDecisionDate = input.RetrospectiveRecord ? input.OriginalDecisionDate : null;
        record.RetrospectiveContext = input.RetrospectiveRecord ? input.RetrospectiveContext : null;
        record.CurrentValidity = input.RetrospectiveRecord ? input.CurrentValidity : null;
        record.CurrentValidityRationale = input.RetrospectiveRecord ? input.CurrentValidityRationale : null;
        record.MessageToDesignOps = input.MessageToDesignOps;
        record.UpdatedAt = now;
        record.UpdatedBy = email;
        record.SubmittedAt = isSubmit ? now : null;
        record.SubmittedBy = isSubmit ? email : null;

        foreach (var x in record.Alternatives.ToList())
            _db.Remove(x);
        record.Alternatives.Clear();
        foreach (var x in record.Evidence.ToList())
            _db.Remove(x);
        record.Evidence.Clear();
        foreach (var x in record.StandardLinks.ToList())
            _db.Remove(x);
        record.StandardLinks.Clear();
        foreach (var x in record.ProductLinks.ToList())
            _db.Remove(x);
        record.ProductLinks.Clear();
        foreach (var x in record.WorkItemLinks.ToList())
            _db.Remove(x);
        record.WorkItemLinks.Clear();

        AppendAlternativesEvidenceStandardsAndLinks(record, input, now, email);

        record.AuditEvents.Add(new DdrAuditEvent
        {
            EventType = isSubmit ? "Submitted" : "Updated",
            CreatedAt = now,
            CreatedBy = email,
        });

        await _db.SaveChangesAsync(ct);

        TempData["DdrCreatedReference"] = record.Reference;
        TempData["DdrCreatedAction"] = isSubmit ? "submitted" : "saved as draft";

        if (isSubmit)
            return RedirectToAction(nameof(Confirmation), new { reference = record.Reference });

        TempData["DdrDraftSaved"] = true;
        return RedirectToAction(nameof(New), new { reference = record.Reference, step = ws });
    }

    private async Task<DdrCreateViewModel?> MapDraftToCreateViewModelAsync(string reference, CancellationToken ct)
    {
        var email = GetUserEmail();
        var record = await _db.DesignDecisionRecords
            .AsNoTracking()
            .Include(r => r.Alternatives)
            .Include(r => r.Evidence)
            .Include(r => r.StandardLinks)
            .Include(r => r.ProductLinks)
            .Include(r => r.WorkItemLinks)
            .FirstOrDefaultAsync(r => r.Reference == reference && r.DeletedAt == null, ct);

        if (record is null
            || !string.Equals(record.Status, "Draft", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(record.CreatedBy, email, StringComparison.OrdinalIgnoreCase))
            return null;

        var altText = string.Join(Environment.NewLine,
            record.Alternatives.OrderBy(a => a.SortOrder).Select(a => a.AlternativeText));

        var evidenceLines = record.Evidence.Select(ev =>
        {
            if (string.IsNullOrWhiteSpace(ev.EvidenceUrl))
                return ev.EvidenceTitle;
            return $"{ev.EvidenceTitle} — {ev.EvidenceUrl}";
        });

        var standardLines = record.StandardLinks
            .Select(s => s.StandardReference ?? s.StandardTitle ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s));

        return new DdrCreateViewModel
        {
            EditingReference = record.Reference,
            Status = "Draft",
            Category = record.Category,
            ShortTitle = record.ShortTitle,
            ContextProblemStatement = record.ContextProblemStatement,
            Decision = record.Decision,
            Rationale = record.Rationale,
            ConsequencesTradeoffs = record.ConsequencesTradeoffs,
            AlternativesText = altText,
            EvidenceText = string.Join(Environment.NewLine, evidenceLines),
            StandardsText = string.Join(Environment.NewLine, standardLines),
            DeviationFlag = record.DeviationFlag,
            DeviationType = record.DeviationType,
            DeviationDetails = record.DeviationDetails,
            ApprovalRoute = record.ApprovalRoute,
            ApprovedBy = record.ApprovedBy,
            ReviewTrigger = record.ReviewTrigger,
            ReviewDate = record.ReviewDate,
            ReviewDateDay = record.ReviewDate?.Day,
            ReviewDateMonth = record.ReviewDate?.Month,
            ReviewDateYear = record.ReviewDate?.Year,
            RetrospectiveRecord = record.RetrospectiveRecord,
            OriginalDecisionDate = record.OriginalDecisionDate,
            RetrospectiveContext = record.RetrospectiveContext,
            CurrentValidity = record.CurrentValidity,
            CurrentValidityRationale = record.CurrentValidityRationale,
            MessageToDesignOps = record.MessageToDesignOps,
            LinkedProductIds = record.ProductLinks.Select(p => p.FipsProductId).ToList(),
            LinkedWorkItemIds = record.WorkItemLinks.Select(w => w.WorkItemId).ToList(),
        };
    }

    private static void AppendAlternativesEvidenceStandardsAndLinks(
        DesignDecisionRecord record,
        DdrCreateViewModel input,
        DateTime now,
        string email)
    {
        var altLines = (input.AlternativesText ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var altIndex = 0;
        foreach (var alt in altLines)
        {
            record.Alternatives.Add(new DdrAlternative
            {
                AlternativeText = alt,
                SortOrder = altIndex++,
            });
        }

        foreach (var line in (input.EvidenceText ?? string.Empty)
                     .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            string? url = null;
            string title = trimmed;
            var emDash = trimmed.IndexOf('—');
            if (emDash > 0)
            {
                title = trimmed.Substring(0, emDash).Trim();
                url = trimmed.Substring(emDash + 1).Trim();
            }
            else if (trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = trimmed;
                title = trimmed;
            }

            record.Evidence.Add(new DdrEvidence
            {
                EvidenceTitle = title.Length > 200 ? title[..200] : title,
                EvidenceUrl = url,
            });
        }

        foreach (var line in (input.StandardsText ?? string.Empty)
                     .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            record.StandardLinks.Add(new DdrStandardLink
            {
                StandardReference = trimmed.Length > 120 ? trimmed[..120] : trimmed,
                StandardTitle = trimmed.Length > 255 ? trimmed[..255] : trimmed,
            });
        }

        foreach (var pid in input.LinkedProductIds.Distinct())
        {
            record.ProductLinks.Add(new DdrProductLink
            {
                FipsProductId = pid,
                CreatedAt = now,
                CreatedBy = email,
            });
        }

        foreach (var wid in input.LinkedWorkItemIds.Distinct())
        {
            record.WorkItemLinks.Add(new DdrWorkItemLink
            {
                WorkItemId = wid,
                CreatedAt = now,
                CreatedBy = email,
            });
        }
    }

    private static string TruncateUtf16(string s, int maxChars) =>
        s.Length <= maxChars ? s : s[..maxChars];

    /// <summary>The form posts back two hidden fields containing comma-separated IDs from the picker.</summary>
    private void ParseLinkedIdsFromForm(DdrCreateViewModel input)
    {
        var posted = HttpContext.Request.Form;
        if (posted.TryGetValue("LinkedProductIdsCsv", out var pCsv))
        {
            input.LinkedProductIds = pCsv.ToString()
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
        }
        if (posted.TryGetValue("LinkedWorkItemIdsCsv", out var wCsv))
        {
            input.LinkedWorkItemIds = wCsv.ToString()
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
                .Where(n => n > 0)
                .Distinct()
                .ToList();
        }
    }

    private async Task PopulateCreateOptionsAsync(DdrCreateViewModel model, CancellationToken ct)
    {
        model.ProductOptions = await _db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active || p.Status == CMDBProductStatus.New)
            .OrderBy(p => p.Title)
            .Select(p => new DdrCreateViewModel.ProductOption { Id = p.Id, Title = p.Title })
            .Take(2000)
            .ToListAsync(ct);

        model.WorkItemOptions = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .Select(p => new DdrCreateViewModel.WorkItemOption { Id = p.Id, Title = p.Title })
            .Take(2000)
            .ToListAsync(ct);

        if (model.PreFilledProductId is { } pid && string.IsNullOrEmpty(model.PreFilledProductTitle))
        {
            model.PreFilledProductTitle = (await _db.CMDBProducts.AsNoTracking()
                .Where(p => p.Id == pid).Select(p => p.Title).FirstOrDefaultAsync(ct)) ?? "Selected product";
        }
        if (model.PreFilledWorkItemId is { } wid && string.IsNullOrEmpty(model.PreFilledWorkItemTitle))
        {
            model.PreFilledWorkItemTitle = (await _db.Projects.AsNoTracking()
                .Where(p => p.Id == wid).Select(p => p.Title).FirstOrDefaultAsync(ct)) ?? "Selected work item";
        }
    }
}
