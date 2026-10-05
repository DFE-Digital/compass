using System.Text.RegularExpressions;
using Compass.Attributes;
using Compass.Data;
using Compass.Models.ServiceSchema;
using Compass.Services.ServiceSchema;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

[Authorize]
[RequireAdmin]
[Route("modern/admin/service-schema")]
public class ModernAdminServiceSchemaController : Controller
{
    private static readonly Regex CodePattern = new("^[A-Z0-9_]+$", RegexOptions.Compiled);
    private readonly CompassDbContext _db;

    public ModernAdminServiceSchemaController(CompassDbContext db) => _db = db;

    private string Actor => User.Identity?.Name ?? "admin";

    [HttpGet("")]
    public IActionResult Index() => RedirectToAction(nameof(Questions));

    [HttpGet("lookups")]
    public async Task<IActionResult> Lookups(CancellationToken ct)
    {
        SetChrome();
        var sets = await _db.ServiceSchemaLookupSets.AsNoTracking()
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .Select(s => new ServiceSchemaLookupSetRow
            {
                Key = s.Key,
                Name = s.Name,
                Description = s.Description,
                ActiveCount = s.Values.Count(v => v.IsActive),
                TotalCount = s.Values.Count
            })
            .ToListAsync(ct);
        return View("~/Views/Modern/Admin/ServiceSchema/Lookups.cshtml", new ServiceSchemaLookupAdminPage { Sets = sets });
    }

    [HttpGet("lookups/{key}")]
    public async Task<IActionResult> Lookup(string key, CancellationToken ct)
    {
        SetChrome();
        var set = await _db.ServiceSchemaLookupSets.AsNoTracking()
            .Include(s => s.Values)
            .FirstOrDefaultAsync(s => s.Key == key, ct);
        if (set == null)
            return NotFound();

        return View("~/Views/Modern/Admin/ServiceSchema/Lookup.cshtml", new ServiceSchemaLookupDetailPage
        {
            Key = set.Key,
            Name = set.Name,
            Description = set.Description,
            Values = set.Values
                .OrderBy(v => v.SortOrder).ThenBy(v => v.Label)
                .Select(v => new ServiceSchemaLookupValueRow
                {
                    Id = v.Id,
                    Code = v.Code,
                    Label = v.Label,
                    Description = v.Description,
                    SortOrder = v.SortOrder,
                    IsActive = v.IsActive
                })
                .ToList()
        });
    }

    [HttpPost("lookups/{key}/values")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLookupValue(string key, string? code, string? label, string? description, CancellationToken ct)
    {
        var set = await _db.ServiceSchemaLookupSets.Include(s => s.Values).FirstOrDefaultAsync(s => s.Key == key, ct);
        if (set == null)
            return NotFound();

        var normalised = (code ?? "").Trim().ToUpperInvariant();
        var display = (label ?? "").Trim();
        if (!CodePattern.IsMatch(normalised) || normalised.Length > 80)
            return LookupBack(key, "Enter a code using capital letters, numbers and underscores.");
        if (string.IsNullOrWhiteSpace(display) || display.Length > 200)
            return LookupBack(key, "Enter a label up to 200 characters.");
        if (set.Values.Any(v => string.Equals(v.Code, normalised, StringComparison.OrdinalIgnoreCase)))
            return LookupBack(key, "That code already exists. Rename the label, or merge into the existing value.");

        var now = DateTime.UtcNow;
        set.Values.Add(new ServiceSchemaLookupValue
        {
            Code = normalised,
            Label = display,
            Description = Trim(description, 1000),
            SortOrder = set.Values.Select(v => v.SortOrder).DefaultIfEmpty(0).Max() + 1,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        set.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Lookup value added.";
        return RedirectToAction(nameof(Lookup), new { key });
    }

    [HttpPost("lookup-values/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateLookupValue(int id, string? label, string? description, int sortOrder, bool isActive, CancellationToken ct)
    {
        var value = await _db.ServiceSchemaLookupValues.Include(v => v.LookupSet).FirstOrDefaultAsync(v => v.Id == id, ct);
        if (value == null)
            return NotFound();

        var display = (label ?? "").Trim();
        if (string.IsNullOrWhiteSpace(display) || display.Length > 200)
            return LookupBack(value.LookupSet.Key, "Enter a label up to 200 characters.");

        value.Label = display;
        value.Description = Trim(description, 1000);
        value.SortOrder = sortOrder;
        value.IsActive = isActive;
        value.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Lookup value updated. Existing records keep this code.";
        return RedirectToAction(nameof(Lookup), new { key = value.LookupSet.Key });
    }

    [HttpPost("lookup-values/{id:int}/merge")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MergeLookupValue(int id, int targetId, CancellationToken ct)
    {
        var source = await _db.ServiceSchemaLookupValues.Include(v => v.LookupSet).FirstOrDefaultAsync(v => v.Id == id, ct);
        var target = await _db.ServiceSchemaLookupValues.Include(v => v.LookupSet).FirstOrDefaultAsync(v => v.Id == targetId, ct);
        if (source == null || target == null || source.LookupSetId != target.LookupSetId || source.Id == target.Id)
            return LookupBack(source?.LookupSet.Key, "Choose a different value in the same list to merge into.");

        var domains = ServiceSchemaCatalog.DomainsUsingLookup(source.LookupSet.Key);
        var entries = await _db.CensusEntries
            .Where(e => domains.Contains(e.DomainKey) && (e.LookupCode == source.Code || e.SecondaryLookupCode == source.Code))
            .ToListAsync(ct);
        foreach (var entry in entries)
        {
            if (entry.LookupCode == source.Code)
                entry.LookupCode = target.Code;
            if (entry.SecondaryLookupCode == source.Code)
                entry.SecondaryLookupCode = target.Code;
            entry.UpdatedAt = DateTime.UtcNow;
            entry.UpdatedBy = Actor;
            entry.RowVersion++;
        }

        source.IsActive = false;
        source.UpdatedAt = DateTime.UtcNow;
        source.Description = Trim($"Merged into {target.Code}. {source.Description}", 1000);
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = $"Merged {source.Code} into {target.Code}. The old code stays on record and cannot be chosen for new entries.";
        return RedirectToAction(nameof(Lookup), new { key = source.LookupSet.Key });
    }

    [HttpGet("service-lines")]
    public async Task<IActionResult> ServiceLines(CancellationToken ct)
    {
        SetChrome();
        ViewBag.SchemaNav = "service-lines";
        ViewBag.Statuses = await ActiveChoicesAsync("entity_status", ct);
        var rows = await _db.CensusServiceLines.AsNoTracking()
            .Where(s => s.RemovedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new CensusRecordRow
            {
                Id = s.Id,
                Reference = s.Reference,
                Name = s.Name,
                Summary = s.Description,
                StatusCode = s.StatusCode
            })
            .ToListAsync(ct);
        return View("~/Views/Modern/Admin/ServiceSchema/Records.cshtml", rows);
    }

    [HttpPost("service-lines")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveServiceLine(string? reference, string? name, string? description, string? boundaryIn, string? boundaryOut, string? statusCode, CancellationToken ct)
    {
        var error = await SaveLineOrServiceAsync(isService: false, reference, name, description, boundaryIn, boundaryOut, statusCode, serviceType: null, ct);
        if (error != null)
        {
            TempData["AdminError"] = error;
            return RedirectToAction(nameof(ServiceLines));
        }
        TempData["AdminMessage"] = "Service line saved.";
        return RedirectToAction(nameof(ServiceLines));
    }

    [HttpGet("services")]
    public async Task<IActionResult> Services(CancellationToken ct)
    {
        SetChrome();
        ViewBag.SchemaNav = "services";
        ViewBag.ServiceTypes = await ActiveChoicesAsync("service_type", ct);
        ViewBag.Statuses = await ActiveChoicesAsync("entity_status", ct);
        ViewBag.ServiceLines = await _db.CensusServiceLines.AsNoTracking()
            .Where(s => s.RemovedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceSchemaChoice { Value = s.Id.ToString(), Label = s.Reference + " — " + s.Name })
            .ToListAsync(ct);
        var rows = await _db.CensusServices.AsNoTracking()
            .Where(s => s.RemovedAt == null)
            .OrderBy(s => s.Name)
            .Select(s => new CensusRecordRow
            {
                Id = s.Id,
                Reference = s.Reference,
                Name = s.Name,
                Summary = s.PurposeNarrative,
                StatusCode = s.StatusCode
            })
            .ToListAsync(ct);
        return View("~/Views/Modern/Admin/ServiceSchema/Records.cshtml", rows);
    }

    [HttpPost("services")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveService(
        string? reference, string? name, string? description, string? boundaryIn, string? boundaryOut,
        string? statusCode, string? serviceTypeCode, Guid? serviceLineId, CancellationToken ct)
    {
        var error = await SaveLineOrServiceAsync(isService: true, reference, name, description, boundaryIn, boundaryOut, statusCode, serviceTypeCode, ct);
        if (error != null)
        {
            TempData["AdminError"] = error;
            return RedirectToAction(nameof(Services));
        }

        if (serviceLineId != null)
        {
            var savedReference = reference!.Trim().ToUpperInvariant();
            var service = await _db.CensusServices.FirstAsync(s => s.Reference == savedReference, ct);
            var lineOk = await _db.CensusServiceLines.AnyAsync(l => l.Id == serviceLineId && l.RemovedAt == null, ct);
            if (lineOk)
            {
                _db.CensusServiceLineServices.Add(new CensusServiceLineService
                {
                    ServiceId = service.Id,
                    ServiceLineId = serviceLineId.Value,
                    RelationshipCode = "PART_OF",
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
            }
        }

        TempData["AdminMessage"] = "Service saved.";
        return RedirectToAction(nameof(Services));
    }

    [HttpGet("catalogue/{kind}")]
    public async Task<IActionResult> Catalogue(string kind, CancellationToken ct)
    {
        var meta = ServiceSchemaCatalog.CatalogueKinds.FirstOrDefault(k => k.Kind == kind);
        if (meta.Kind == null)
            return NotFound();
        SetChrome();
        ViewBag.SchemaNav = kind;
        var items = await _db.CensusCatalogueItems.AsNoTracking()
            .Where(c => c.Kind == kind && c.RemovedAt == null)
            .OrderBy(c => c.Name)
            .Select(c => new CensusRecordRow
            {
                Id = c.Id,
                Reference = c.Reference,
                Name = c.Name,
                Summary = c.Statement,
                StatusCode = c.StatusCode
            })
            .ToListAsync(ct);
        return View("~/Views/Modern/Admin/ServiceSchema/Catalogue.cshtml", new CatalogueAdminPage
        {
            Kind = meta.Kind,
            Label = meta.Label,
            LookupSetKey = meta.LookupSetKey,
            LookupValues = await ActiveChoicesAsync(meta.LookupSetKey, ct),
            Items = items
        });
    }

    [HttpPost("catalogue/{kind}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCatalogueItem(string kind, string? reference, string? name, string? statement, string? lookupCode, CancellationToken ct)
    {
        var meta = ServiceSchemaCatalog.CatalogueKinds.FirstOrDefault(k => k.Kind == kind);
        if (meta.Kind == null)
            return NotFound();

        var refCode = (reference ?? "").Trim().ToUpperInvariant();
        var display = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(refCode))
            refCode = Guid.NewGuid().ToString("N").ToUpperInvariant();
        if (!CodePattern.IsMatch(refCode) || refCode.Length > 40 || display.Length is < 1 or > 300)
        {
            TempData["AdminError"] = "Enter a name. A reference can be left blank.";
            return RedirectToAction(nameof(Catalogue), new { kind });
        }

        if (await _db.CensusCatalogueItems.AnyAsync(c => c.Kind == kind && c.Reference == refCode, ct))
        {
            TempData["AdminError"] = "That reference is already used.";
            return RedirectToAction(nameof(Catalogue), new { kind });
        }

        if (!string.IsNullOrWhiteSpace(meta.LookupSetKey) &&
            !string.IsNullOrWhiteSpace(lookupCode) &&
            !await _db.ServiceSchemaLookupValues.AnyAsync(v => v.IsActive && v.Code == lookupCode && v.LookupSet.Key == meta.LookupSetKey, ct))
        {
            TempData["AdminError"] = "Choose an active classification.";
            return RedirectToAction(nameof(Catalogue), new { kind });
        }

        var now = DateTime.UtcNow;
        _db.CensusCatalogueItems.Add(new CensusCatalogueItem
        {
            Kind = kind,
            Reference = refCode,
            Name = display,
            Statement = Trim(statement, 4000),
            LookupCode = string.IsNullOrWhiteSpace(lookupCode) ? null : lookupCode.Trim(),
            StatusCode = "ACTIVE",
            CreatedAt = now,
            CreatedBy = Actor,
            UpdatedAt = now,
            UpdatedBy = Actor
        });
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Shared item added. Register entries can link to it without copying the definition.";
        return RedirectToAction(nameof(Catalogue), new { kind });
    }

    [HttpPost("catalogue/{kind}/{id:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveCatalogueItem(string kind, Guid id, CancellationToken ct)
    {
        var meta = ServiceSchemaCatalog.CatalogueKinds.FirstOrDefault(k => k.Kind == kind);
        if (meta.Kind == null)
            return NotFound();

        var item = await _db.CensusCatalogueItems.FirstOrDefaultAsync(
            c => c.Id == id && c.Kind == kind && c.RemovedAt == null, ct);
        if (item == null)
            return NotFound();

        var now = DateTime.UtcNow;
        item.RemovedAt = now;
        item.StatusCode = "REMOVED";
        item.UpdatedAt = now;
        item.UpdatedBy = Actor;
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = $"Removed {item.Name}. It can no longer be chosen. Responses already recorded keep their text.";
        return RedirectToAction(nameof(Catalogue), new { kind });
    }

    private async Task<string?> SaveLineOrServiceAsync(
        bool isService, string? reference, string? name, string? description, string? boundaryIn, string? boundaryOut,
        string? statusCode, string? serviceType, CancellationToken ct)
    {
        var refCode = (reference ?? "").Trim().ToUpperInvariant();
        var display = (name ?? "").Trim();
        if (refCode.Length is < 2 or > 40 || display.Length is < 1 or > 200)
            return "Enter a reference up to 40 characters and a name.";
        if (string.IsNullOrWhiteSpace(statusCode))
            statusCode = "DRAFT";
        var statusOk = await _db.ServiceSchemaLookupValues.AnyAsync(
            v => v.IsActive && v.Code == statusCode && v.LookupSet.Key == "entity_status", ct);
        if (!statusOk)
            return "Choose a status.";

        var now = DateTime.UtcNow;
        if (!isService)
        {
            if (await _db.CensusServiceLines.AnyAsync(s => s.Reference == refCode, ct))
                return "That reference is already used.";
            _db.CensusServiceLines.Add(new CensusServiceLine
            {
                Reference = refCode,
                Name = display,
                Description = Trim(description, 4000),
                BoundaryIn = Trim(boundaryIn, 4000),
                BoundaryOut = Trim(boundaryOut, 4000),
                StatusCode = statusCode,
                CreatedAt = now,
                CreatedBy = Actor,
                UpdatedAt = now,
                UpdatedBy = Actor
            });
        }
        else
        {
            if (await _db.CensusServices.AnyAsync(s => s.Reference == refCode, ct))
                return "That reference is already used.";
            if (!string.IsNullOrWhiteSpace(serviceType) &&
                !await _db.ServiceSchemaLookupValues.AnyAsync(v => v.IsActive && v.Code == serviceType && v.LookupSet.Key == "service_type", ct))
                return "Choose a service type.";
            _db.CensusServices.Add(new CensusService
            {
                Reference = refCode,
                Name = display,
                PurposeNarrative = Trim(description, 4000),
                BoundaryIn = Trim(boundaryIn, 4000),
                BoundaryOut = Trim(boundaryOut, 4000),
                ServiceTypeCode = string.IsNullOrWhiteSpace(serviceType) ? null : serviceType,
                StatusCode = statusCode,
                CreatedAt = now,
                CreatedBy = Actor,
                UpdatedAt = now,
                UpdatedBy = Actor
            });
        }

        await _db.SaveChangesAsync(ct);
        return null;
    }

    [HttpGet("questions")]
    public async Task<IActionResult> Questions(CancellationToken ct)
    {
        SetChrome();
        ViewBag.SchemaNav = "questions";
        await ServiceSchemaLayout.EnsureDefaultsAsync(_db, ct);
        var areas = await _db.ServiceSchemaAreaConfigs.AsNoTracking()
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .ToListAsync(ct);
        var questions = await _db.ServiceSchemaQuestions.AsNoTracking()
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Heading)
            .ToListAsync(ct);
        return View("~/Views/Modern/Admin/ServiceSchema/Questions.cshtml", new ServiceSchemaQuestionsAdminPage
        {
            Areas = areas.Select(area => new ServiceSchemaAreaAdminRow
            {
                Key = area.Key,
                Name = area.Name,
                Summary = area.Summary,
                SortOrder = area.SortOrder,
                Questions = questions
                    .Where(q => string.Equals(q.AreaKey, area.Key, StringComparison.OrdinalIgnoreCase))
                    .Select(q => new ServiceSchemaQuestionAdminRow
                    {
                        Id = q.Id,
                        Key = q.Key,
                        Heading = q.Heading,
                        SortOrder = q.SortOrder,
                        IsActive = q.IsActive
                    })
                    .ToList()
            }).ToList()
        });
    }

    [HttpPost("questions/order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuestionOrder(
        Dictionary<string, int>? areaOrder,
        Dictionary<string, string>? areaName,
        Dictionary<string, string>? areaSummary,
        Dictionary<int, int>? questionOrder,
        CancellationToken ct)
    {
        var areas = await _db.ServiceSchemaAreaConfigs.ToListAsync(ct);
        foreach (var area in areas)
        {
            if (areaOrder != null && areaOrder.TryGetValue(area.Key, out var order))
                area.SortOrder = order;
            if (areaName != null && areaName.TryGetValue(area.Key, out var name))
            {
                var trimmed = name.Trim();
                if (trimmed.Length is < 1 or > 200)
                {
                    TempData["AdminError"] = "Enter an area name up to 200 characters.";
                    return RedirectToAction(nameof(Questions));
                }
                area.Name = trimmed;
            }
            if (areaSummary != null && areaSummary.TryGetValue(area.Key, out var summary))
                area.Summary = Trim(summary, 4000) ?? "";
        }

        if (questionOrder != null)
        {
            var questions = await _db.ServiceSchemaQuestions.ToListAsync(ct);
            foreach (var question in questions)
            {
                if (questionOrder.TryGetValue(question.Id, out var order))
                    question.SortOrder = order;
            }
        }

        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Service schema order saved.";
        return RedirectToAction(nameof(Questions));
    }

    [HttpGet("questions/new")]
    public async Task<IActionResult> QuestionCreate(string? section, CancellationToken ct)
    {
        SetChrome();
        ViewBag.SchemaNav = "questions";
        await ServiceSchemaLayout.EnsureDefaultsAsync(_db, ct);
        var areas = await AreaChoicesAsync(ct);
        var areaKey = areas.Any(a => string.Equals(a.Value, section, StringComparison.OrdinalIgnoreCase))
            ? section!
            : areas.FirstOrDefault()?.Value ?? "";
        return View("~/Views/Modern/Admin/ServiceSchema/Question.cshtml", new ServiceSchemaQuestionEditPage
        {
            AreaKey = areaKey,
            TitleLabel = "Title",
            NarrativeLabel = "How this applies",
            IsActive = true,
            Areas = areas
        });
    }

    [HttpPost("questions/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuestion(
        string? section, string? heading, string? help, string? titleLabel, string? narrativeLabel, bool isActive, CancellationToken ct)
    {
        var areas = await AreaChoicesAsync(ct);
        var areaKey = (section ?? "").Trim();
        if (areas.All(a => !string.Equals(a.Value, areaKey, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AdminError"] = "Choose an area for this question.";
            return RedirectToAction(nameof(QuestionCreate));
        }

        var display = (heading ?? "").Trim();
        var title = (titleLabel ?? "").Trim();
        var narrative = (narrativeLabel ?? "").Trim();
        if (display.Length is < 1 or > 200 || title.Length is < 1 or > 200 || narrative.Length is < 1 or > 200)
        {
            TempData["AdminError"] = "Enter a question, a title label, and a description label, each up to 200 characters.";
            return RedirectToAction(nameof(QuestionCreate), new { section = areaKey });
        }

        var key = await UniqueQuestionKeyAsync(display, ct);
        var sort = await _db.ServiceSchemaQuestions
            .Where(q => q.AreaKey == areaKey)
            .Select(q => (int?)q.SortOrder)
            .MaxAsync(ct) ?? 0;
        _db.ServiceSchemaQuestions.Add(new ServiceSchemaQuestion
        {
            Key = key,
            AreaKey = areaKey,
            Heading = display,
            Help = Trim(help, 4000) ?? "",
            TitleLabel = title,
            NarrativeLabel = narrative,
            SortOrder = sort + 1,
            IsActive = isActive,
            IsBuiltIn = false,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = Actor
        });
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Question added.";
        return RedirectToAction(nameof(Questions));
    }

    [HttpGet("questions/{id:int}")]
    public async Task<IActionResult> Question(int id, CancellationToken ct)
    {
        SetChrome();
        ViewBag.SchemaNav = "questions";
        var question = await _db.ServiceSchemaQuestions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id, ct);
        if (question == null)
            return NotFound();
        return View("~/Views/Modern/Admin/ServiceSchema/Question.cshtml", await EditPageAsync(question, ct));
    }

    [HttpPost("questions/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(
        int id, string? section, string? heading, string? help, string? titleLabel, string? narrativeLabel, int sortOrder, bool isActive, CancellationToken ct)
    {
        var question = await _db.ServiceSchemaQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (question == null)
            return NotFound();

        var areas = await AreaChoicesAsync(ct);
        var areaKey = (section ?? "").Trim();
        if (areas.All(a => !string.Equals(a.Value, areaKey, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["AdminError"] = "Choose an area for this question.";
            return RedirectToAction(nameof(Question), new { id });
        }

        var display = (heading ?? "").Trim();
        var title = (titleLabel ?? "").Trim();
        var narrative = (narrativeLabel ?? "").Trim();
        if (display.Length is < 1 or > 200 || title.Length is < 1 or > 200 || narrative.Length is < 1 or > 200)
        {
            TempData["AdminError"] = "Enter a question, a title label, and a description label, each up to 200 characters.";
            return RedirectToAction(nameof(Question), new { id });
        }

        question.AreaKey = areaKey;
        question.Heading = display;
        question.Help = Trim(help, 4000) ?? "";
        question.TitleLabel = title;
        question.NarrativeLabel = narrative;
        question.SortOrder = sortOrder;
        question.IsActive = isActive;
        question.UpdatedAt = DateTime.UtcNow;
        question.UpdatedBy = Actor;
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = isActive
            ? "Question updated."
            : "Question hidden. Responses already saved stay stored.";
        return RedirectToAction(nameof(Questions));
    }

    [HttpPost("questions/{id:int}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveQuestion(int id, CancellationToken ct)
    {
        var question = await _db.ServiceSchemaQuestions.FirstOrDefaultAsync(q => q.Id == id, ct);
        if (question == null)
            return NotFound();
        if (question.IsBuiltIn)
        {
            TempData["AdminError"] = "Built-in questions can be hidden. They cannot be deleted.";
            return RedirectToAction(nameof(Question), new { id });
        }

        var used = await QuestionHasResponsesAsync(question.Key, ct);
        if (used)
        {
            question.IsActive = false;
            question.UpdatedAt = DateTime.UtcNow;
            question.UpdatedBy = Actor;
            await _db.SaveChangesAsync(ct);
            TempData["AdminMessage"] = "This question has responses, so it is hidden. The responses stay stored.";
            return RedirectToAction(nameof(Questions));
        }

        _db.ServiceSchemaQuestions.Remove(question);
        await _db.SaveChangesAsync(ct);
        TempData["AdminMessage"] = "Question removed.";
        return RedirectToAction(nameof(Questions));
    }

    private async Task<ServiceSchemaQuestionEditPage> EditPageAsync(ServiceSchemaQuestion question, CancellationToken ct) =>
        new()
        {
            Id = question.Id,
            Key = question.Key,
            AreaKey = question.AreaKey,
            Heading = question.Heading,
            Help = question.Help,
            TitleLabel = question.TitleLabel,
            NarrativeLabel = question.NarrativeLabel,
            SortOrder = question.SortOrder,
            IsActive = question.IsActive,
            IsBuiltIn = question.IsBuiltIn,
            HasResponses = await QuestionHasResponsesAsync(question.Key, ct),
            Areas = await AreaChoicesAsync(ct)
        };

    private async Task<List<ServiceSchemaChoice>> AreaChoicesAsync(CancellationToken ct) =>
        await _db.ServiceSchemaAreaConfigs.AsNoTracking()
            .OrderBy(a => a.SortOrder).ThenBy(a => a.Name)
            .Select(a => new ServiceSchemaChoice { Value = a.Key, Label = a.Name })
            .ToListAsync(ct);

    private async Task<bool> QuestionHasResponsesAsync(string key, CancellationToken ct) =>
        await _db.CensusEntries.AnyAsync(e => e.DomainKey == key && e.RemovedAt == null, ct)
        || await _db.CensusSectionDeclarations.AnyAsync(d => d.SectionKey == key, ct);

    private async Task<string> UniqueQuestionKeyAsync(string heading, CancellationToken ct)
    {
        var slug = new string(heading.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        slug = slug.Trim('-');
        if (slug.Length > 36)
            slug = slug[..36].Trim('-');
        if (string.IsNullOrEmpty(slug))
            slug = "question";

        var candidate = slug;
        var suffix = 2;
        while (await _db.ServiceSchemaQuestions.AnyAsync(q => q.Key == candidate, ct) || ServiceSchemaAreas.FindTopic(candidate) != null)
        {
            var suffixText = "-" + suffix;
            var stem = slug.Length + suffixText.Length > 40 ? slug[..(40 - suffixText.Length)] : slug;
            candidate = stem + suffixText;
            suffix++;
        }
        return candidate;
    }

    private async Task<List<ServiceSchemaChoice>> ActiveChoicesAsync(string setKey, CancellationToken ct) =>
        await _db.ServiceSchemaLookupValues.AsNoTracking()
            .Where(v => v.IsActive && v.LookupSet.Key == setKey)
            .OrderBy(v => v.SortOrder)
            .Select(v => new ServiceSchemaChoice { Value = v.Code, Label = v.Label })
            .ToListAsync(ct);

    private IActionResult LookupBack(string? key, string message)
    {
        TempData["AdminError"] = message;
        return string.IsNullOrWhiteSpace(key)
            ? RedirectToAction(nameof(Lookups))
            : RedirectToAction(nameof(Lookup), new { key });
    }

    private void SetChrome()
    {
        ViewBag.MainNavSection = "admin";
        ViewBag.SubNavItem = "admin-service-schema";
    }

    private static string? Trim(string? value, int max)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
