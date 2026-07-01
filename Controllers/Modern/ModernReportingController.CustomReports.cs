using System.Text.Json;
using System.Text.Json.Serialization;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Models.Modern.Work;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernReportingController
{
    private static readonly JsonSerializerOptions _crJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [HttpGet("custom-reports")]
    public async Task<IActionResult> CustomReports(CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var myReports = await _context.CustomReports
            .AsNoTracking()
            .Include(r => r.Owner)
            .Where(r => r.OwnerUserId == currentUser.Id)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);

        var publicReports = await _context.CustomReports
            .AsNoTracking()
            .Include(r => r.Owner)
            .Where(r => r.Visibility == CustomReportVisibility.Public && r.OwnerUserId != currentUser.Id)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);

        var model = new CustomReportsDashboardViewModel
        {
            MyReports = myReports.Select(r => MapListItem(r, currentUser.Id)).ToList(),
            PublicReports = publicReports.Select(r => MapListItem(r, currentUser.Id)).ToList()
        };

        return View("~/Views/Modern/Reporting/CustomReports.cshtml", model);
    }

    [HttpGet("custom-reports/create")]
    public async Task<IActionResult> CustomReportCreate(CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var model = await BuildBuilderViewModelAsync(null, cancellationToken);
        if (model == null) return Unauthorized();

        return View("~/Views/Modern/Reporting/CustomReportBuilder.cshtml", model);
    }

    [HttpPost("custom-reports/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomReportCreatePost(
        string name,
        string? description,
        string visibility,
        string? reportingPeriod,
        string? selectedWorkItemIds,
        string? selectedServiceRegisterIds,
        string? sectionsJson,
        CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var parsedVisibility = Enum.TryParse<CustomReportVisibility>(visibility, true, out var v)
            ? v : CustomReportVisibility.Private;

        var validationError = await ValidateReportNameAsync(name, currentUser.Id, null, parsedVisibility, cancellationToken);
        if (validationError != null)
        {
            ModelState.AddModelError("Name", validationError);
            var model = await BuildBuilderViewModelAsync(null, cancellationToken);
            if (model == null) return Unauthorized();
            model.Name = name;
            model.Description = description;
            model.Visibility = parsedVisibility;
            return View("~/Views/Modern/Reporting/CustomReportBuilder.cshtml", model);
        }

        var definition = BuildTemplateDefinition(
            reportingPeriod,
            selectedWorkItemIds,
            selectedServiceRegisterIds,
            sectionsJson);

        var report = new CustomReport
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            OwnerUserId = currentUser.Id,
            DataSource = CustomReportDataSource.WorkProjects,
            Visibility = parsedVisibility,
            DefinitionJson = JsonSerializer.Serialize(definition, _crJsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CustomReports.Add(report);
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(CustomReportView), new { id = report.Id });
    }

    [HttpGet("custom-reports/{id:int}")]
    public async Task<IActionResult> CustomReportView(int id, int? year, int? month, CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var report = await _context.CustomReports
            .AsNoTracking()
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report == null) return NotFound();

        if (!CanViewReport(report, currentUser.Id))
            return Forbid();

        var definition = DeserializeTemplate(report.DefinitionJson);
        var now = DateTime.UtcNow;
        var reportYear = year ?? now.Year;
        var reportMonth = month ?? now.Month;

        var model = await BuildReportViewAsync(report, definition, currentUser.Id, reportYear, reportMonth, cancellationToken);

        return View("~/Views/Modern/Reporting/CustomReportView.cshtml", model);
    }

    [HttpGet("custom-reports/{id:int}/edit")]
    public async Task<IActionResult> CustomReportEdit(int id, CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var report = await _context.CustomReports
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == currentUser.Id, cancellationToken);

        if (report == null) return NotFound();

        var model = await BuildBuilderViewModelAsync(report, cancellationToken);
        if (model == null) return Unauthorized();

        return View("~/Views/Modern/Reporting/CustomReportBuilder.cshtml", model);
    }

    [HttpPost("custom-reports/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomReportEditPost(
        int id,
        string name,
        string? description,
        string visibility,
        string? reportingPeriod,
        string? selectedWorkItemIds,
        string? selectedServiceRegisterIds,
        string? sectionsJson,
        CancellationToken cancellationToken = default)
    {
        SetNav("reporting-custom-reports");

        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var report = await _context.CustomReports
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == currentUser.Id, cancellationToken);

        if (report == null) return NotFound();

        var parsedVisibility = Enum.TryParse<CustomReportVisibility>(visibility, true, out var v)
            ? v : CustomReportVisibility.Private;

        var validationError = await ValidateReportNameAsync(name, currentUser.Id, id, parsedVisibility, cancellationToken);
        if (validationError != null)
        {
            ModelState.AddModelError("Name", validationError);
            var model = await BuildBuilderViewModelAsync(report, cancellationToken);
            if (model == null) return Unauthorized();
            model.Name = name;
            model.Description = description;
            model.Visibility = parsedVisibility;
            return View("~/Views/Modern/Reporting/CustomReportBuilder.cshtml", model);
        }

        var definition = BuildTemplateDefinition(
            reportingPeriod,
            selectedWorkItemIds,
            selectedServiceRegisterIds,
            sectionsJson);

        report.Name = name.Trim();
        report.Description = description?.Trim();
        report.Visibility = parsedVisibility;
        report.DefinitionJson = JsonSerializer.Serialize(definition, _crJsonOptions);
        report.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(CustomReportView), new { id = report.Id });
    }

    [HttpPost("custom-reports/{id:int}/duplicate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomReportDuplicate(int id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var source = await _context.CustomReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (source == null) return NotFound();
        if (!CanViewReport(source, currentUser.Id)) return Forbid();

        var baseName = $"{source.Name} (copy)";
        var candidateName = baseName;
        var counter = 1;
        while (await _context.CustomReports.AnyAsync(
            r => r.OwnerUserId == currentUser.Id && r.Name == candidateName, cancellationToken))
        {
            counter++;
            candidateName = $"{baseName} {counter}";
        }

        var duplicate = new CustomReport
        {
            Name = candidateName,
            Description = source.Description,
            OwnerUserId = currentUser.Id,
            DataSource = source.DataSource,
            Visibility = CustomReportVisibility.Private,
            DefinitionJson = source.DefinitionJson,
            DefaultFilterJson = source.DefaultFilterJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CustomReports.Add(duplicate);
        await _context.SaveChangesAsync(cancellationToken);

        return RedirectToAction(nameof(CustomReportEdit), new { id = duplicate.Id });
    }

    [HttpPost("custom-reports/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CustomReportDelete(int id, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return Unauthorized();

        var report = await _context.CustomReports
            .Include(r => r.Shares)
            .FirstOrDefaultAsync(r => r.Id == id && r.OwnerUserId == currentUser.Id, cancellationToken);

        if (report == null) return NotFound();

        _context.CustomReportShares.RemoveRange(report.Shares);
        _context.CustomReports.Remove(report);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Report \"{report.Name}\" has been deleted.";
        return RedirectToAction(nameof(CustomReports));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<User?> GetOrCreateCurrentUserAsync(CancellationToken cancellationToken)
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrEmpty(email)) return null;
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    private static CustomReportListItem MapListItem(CustomReport r, int currentUserId) => new()
    {
        Id = r.Id,
        Name = r.Name,
        Description = r.Description,
        OwnerName = r.Owner?.Name ?? "Unknown",
        Visibility = r.Visibility,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        IsOwner = r.OwnerUserId == currentUserId
    };

    private static bool CanViewReport(CustomReport report, int userId)
    {
        if (report.OwnerUserId == userId) return true;
        if (report.Visibility == CustomReportVisibility.Public) return true;
        return false;
    }

    private async Task<CustomReportBuilderViewModel?> BuildBuilderViewModelAsync(
        CustomReport? existing, CancellationToken cancellationToken)
    {
        var currentUser = await GetOrCreateCurrentUserAsync(cancellationToken);
        if (currentUser == null) return null;

        var workItems = await _context.Projects
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Status != "Cancelled")
            .OrderBy(p => p.Title)
            .Select(p => new ScopePickerItem
            {
                Id = p.Id,
                Name = p.Title,
                Status = p.Status,
                BusinessArea = p.BusinessAreaLookup != null ? p.BusinessAreaLookup.Name : null
            })
            .ToListAsync(cancellationToken);

        var srProducts = await _context.CMDBProducts
            .AsNoTracking()
            .Include(p => p.BusinessAreas)
                .ThenInclude(ba => ba.FipsBusinessArea)
            .Where(p => p.Status == CMDBProductStatus.Active)
            .OrderBy(p => p.Title)
            .ToListAsync(cancellationToken);

        var serviceRegisterItems = srProducts.Select(p => new ScopePickerItem
        {
            Id = p.UniqueID,
            Name = p.Title,
            Status = p.Status.ToString(),
            BusinessArea = p.BusinessAreas.FirstOrDefault()?.FipsBusinessArea?.Name
        }).ToList();

        var availableSections = CustomReportSectionTypes.All
            .Select(s => new AvailableSectionItem { SectionType = s.Key, Label = s.Label, Group = s.Group })
            .ToList();

        var model = new CustomReportBuilderViewModel
        {
            AvailableWorkItems = workItems,
            AvailableServiceRegisterItems = serviceRegisterItems,
            AvailableSections = availableSections
        };

        if (existing != null)
        {
            model.ReportId = existing.Id;
            model.Name = existing.Name;
            model.Description = existing.Description;
            model.Visibility = existing.Visibility;

            var definition = DeserializeTemplate(existing.DefinitionJson);
            model.ReportingPeriod = definition.ReportingPeriod;
            model.SelectedWorkItemIds = definition.Scope.WorkItemIds;
            model.SelectedServiceRegisterIds = definition.Scope.ServiceRegisterIds;
            model.Sections = definition.Sections
                .OrderBy(s => s.SortOrder)
                .Select(s =>
                {
                    var meta = CustomReportSectionTypes.All.FirstOrDefault(a => a.Key == s.SectionType);
                    return new CustomReportSectionItem
                    {
                        SectionType = s.SectionType,
                        Label = meta.Label ?? s.SectionType,
                        Group = meta.Group ?? "",
                        SortOrder = s.SortOrder,
                        IsVisible = s.IsVisible
                    };
                })
                .ToList();
        }

        return model;
    }

    private static CustomReportTemplateDefinition DeserializeTemplate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new CustomReportTemplateDefinition();

        try
        {
            return JsonSerializer.Deserialize<CustomReportTemplateDefinition>(json, _crJsonOptions)
                ?? new CustomReportTemplateDefinition();
        }
        catch
        {
            return new CustomReportTemplateDefinition();
        }
    }

    private static CustomReportTemplateDefinition BuildTemplateDefinition(
        string? reportingPeriod,
        string? selectedWorkItemIds,
        string? selectedServiceRegisterIds,
        string? sectionsJson)
    {
        var workIds = ParseIntList(selectedWorkItemIds);
        var srIds = ParseIntList(selectedServiceRegisterIds);

        List<CustomReportTemplateSection> sections;
        if (!string.IsNullOrWhiteSpace(sectionsJson))
        {
            try
            {
                sections = JsonSerializer.Deserialize<List<CustomReportTemplateSection>>(sectionsJson, _crJsonOptions)
                    ?? new List<CustomReportTemplateSection>();
            }
            catch
            {
                sections = new List<CustomReportTemplateSection>();
            }
        }
        else
        {
            sections = new List<CustomReportTemplateSection>();
        }

        return new CustomReportTemplateDefinition
        {
            ReportingPeriod = reportingPeriod ?? "calendar-month",
            Scope = new CustomReportScope
            {
                WorkItemIds = workIds,
                ServiceRegisterIds = srIds
            },
            Sections = sections
        };
    }

    private static List<int> ParseIntList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var v) ? v : 0)
            .Where(v => v > 0)
            .Distinct()
            .ToList();
    }

    private async Task<string?> ValidateReportNameAsync(
        string name, int userId, int? existingReportId,
        CustomReportVisibility visibility, CancellationToken cancellationToken)
    {
        var trimmed = name?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(trimmed))
            return "Enter a report name.";

        if (trimmed.Length > 200)
            return "Report name must be 200 characters or fewer.";

        var duplicateForUser = await _context.CustomReports.AnyAsync(
            r => r.OwnerUserId == userId
                && r.Name == trimmed
                && (existingReportId == null || r.Id != existingReportId),
            cancellationToken);

        if (duplicateForUser)
            return "You already have a report with this name. Choose a different name.";

        if (visibility == CustomReportVisibility.Public)
        {
            var duplicatePublic = await _context.CustomReports.AnyAsync(
                r => r.Visibility == CustomReportVisibility.Public
                    && r.Name == trimmed
                    && (existingReportId == null || r.Id != existingReportId),
                cancellationToken);

            if (duplicatePublic)
                return "A public report with this name already exists. Choose a different name or set the report to private.";
        }

        return null;
    }

    private async Task<CustomReportViewViewModel> BuildReportViewAsync(
        CustomReport report,
        CustomReportTemplateDefinition definition,
        int currentUserId,
        int reportYear,
        int reportMonth,
        CancellationToken cancellationToken)
    {
        var model = new CustomReportViewViewModel
        {
            ReportId = report.Id,
            Name = report.Name,
            Description = report.Description,
            OwnerName = report.Owner?.Name ?? "Unknown",
            IsOwner = report.OwnerUserId == currentUserId,
            Visibility = report.Visibility,
            CreatedAt = report.CreatedAt,
            ReportingPeriod = definition.ReportingPeriod,
            ReportYear = reportYear,
            ReportMonth = reportMonth,
            MonthName = new DateTime(reportYear, reportMonth, 1).ToString("MMMM yyyy")
        };

        model.Sections = definition.Sections
            .Where(s => s.IsVisible)
            .OrderBy(s => s.SortOrder)
            .Select(s =>
            {
                var meta = CustomReportSectionTypes.All.FirstOrDefault(a => a.Key == s.SectionType);
                return new CustomReportSectionItem
                {
                    SectionType = s.SectionType,
                    Label = meta.Label ?? s.SectionType,
                    Group = meta.Group ?? "",
                    SortOrder = s.SortOrder,
                    IsVisible = s.IsVisible
                };
            })
            .ToList();

        var workItemIds = definition.Scope.WorkItemIds;
        var serviceRegisterIds = definition.Scope.ServiceRegisterIds;

        if (workItemIds.Count > 0)
        {
            var projects = await _context.Projects
                .AsNoTracking()
                .Include(p => p.BusinessAreaLookup)
                .Include(p => p.RagStatusLookup)
                .Include(p => p.DeliveryPriority)
                .Include(p => p.MonthlyUpdates)
                .Include(p => p.RagHistory)
                    .ThenInclude(h => h.RagStatusLookup)
                .Include(p => p.Milestones)
                .Where(p => workItemIds.Contains(p.Id) && !p.IsDeleted)
                .ToListAsync(cancellationToken);

            string GetRag(Project p) => p.RagStatusLookup?.Name ?? p.RagStatusName ?? "Not set";
            string GetPriority(Project p) => p.DeliveryPriority?.Name ?? "Not set";

            model.WorkItems = projects.Select(p => new ScopeWorkItem
            {
                Id = p.Id,
                Title = p.Title,
                Status = p.Status,
                Rag = GetRag(p),
                Priority = GetPriority(p),
                BusinessArea = p.BusinessAreaLookup?.Name
            }).OrderBy(w => w.Title).ToList();

            model.RagPriorityRows = projects.Select(p => new RagPriorityRow
            {
                ProjectId = p.Id,
                Title = p.Title,
                Rag = GetRag(p),
                Priority = GetPriority(p),
                PreviousRag = p.RagHistory?
                    .OrderByDescending(h => h.ChangedAt)
                    .Skip(1)
                    .Select(h => h.RagStatusLookup?.Name ?? h.RagStatus)
                    .FirstOrDefault(),
                BusinessArea = p.BusinessAreaLookup?.Name
            }).OrderBy(r => r.Title).ToList();

            model.MilestoneRows = projects
                .SelectMany(p => (p.Milestones ?? Enumerable.Empty<Milestone>())
                    .Select(ms => new MilestoneRow
                    {
                        ProjectId = p.Id,
                        ProjectTitle = p.Title,
                        MilestoneTitle = ms.Name,
                        DueDate = ms.DueDate,
                        Status = ms.Status
                    }))
                .OrderBy(ms => ms.DueDate ?? DateTime.MaxValue)
                .ToList();

            model.PathToGreenRows = projects
                .Where(p =>
                {
                    var rag = GetRag(p);
                    return !string.IsNullOrWhiteSpace(rag) && !string.Equals(rag, "Green", StringComparison.OrdinalIgnoreCase) && rag != "Not set";
                })
                .Select(p =>
                {
                    var latestPtg = p.RagHistory?
                        .OrderByDescending(h => h.ChangedAt)
                        .Select(h => h.PathToGreen)
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                    return new PathToGreenRow
                    {
                        ProjectId = p.Id,
                        Title = p.Title,
                        Rag = GetRag(p),
                        PathToGreen = latestPtg ?? p.PathToGreen
                    };
                })
                .OrderBy(r => r.Title)
                .ToList();

            model.MonthlyUpdateRows = projects.Select(p =>
            {
                var update = p.MonthlyUpdates?
                    .FirstOrDefault(u => u.Year == reportYear && u.Month == reportMonth);
                return new MonthlyUpdateRow
                {
                    ProjectId = p.Id,
                    Title = p.Title,
                    Rag = GetRag(p),
                    UpdateSummary = update?.Narrative,
                    SubmittedAt = update?.SubmittedAt
                };
            }).OrderBy(r => r.Title).ToList();

            var ragGroups = projects
                .GroupBy(p => GetRag(p))
                .Select(g => new RagSummaryRow
                {
                    Label = g.Key,
                    Count = g.Count(),
                    CssClass = Compass.Models.Modern.Work.WorkBadgeCss.RagCompactBadgeClass(g.Key)
                })
                .OrderBy(r => RagSortKey(r.Label))
                .ToList();
            model.RagSummaryRows = ragGroups;

            model.ResourcingRows = projects.Select(p => new ResourcingRow
            {
                ProjectId = p.Id,
                Title = p.Title,
                PermFte = p.TotalPermFte,
                MspFte = p.TotalMspFte,
                BusinessArea = p.BusinessAreaLookup?.Name
            }).OrderBy(r => r.Title).ToList();

            model.RiskCount = await _context.Risks
                .AsNoTracking()
                .CountAsync(r => r.ProjectId.HasValue && workItemIds.Contains(r.ProjectId.Value)
                    && r.Status != "closed" && r.Status != "Closed", cancellationToken);

            model.IssueCount = await _context.Issues
                .AsNoTracking()
                .CountAsync(i => i.ProjectId.HasValue && workItemIds.Contains(i.ProjectId.Value)
                    && i.Status != "closed" && i.Status != "Closed", cancellationToken);
        }

        if (serviceRegisterIds.Count > 0)
        {
            var products = await _context.CMDBProducts
                .AsNoTracking()
                .Include(p => p.Phase)
                .Include(p => p.BusinessAreas)
                    .ThenInclude(ba => ba.FipsBusinessArea)
                .Where(p => serviceRegisterIds.Contains(p.UniqueID))
                .ToListAsync(cancellationToken);

            model.ServiceRegisterItems = products.Select(p => new ScopeServiceRegisterItem
            {
                Id = p.UniqueID,
                Name = p.Title,
                Status = p.Status.ToString(),
                Phase = p.Phase?.Name,
                BusinessArea = p.BusinessAreas.FirstOrDefault()?.FipsBusinessArea?.Name
            }).OrderBy(s => s.Name).ToList();
        }

        if (model.Sections.Any(s => s.SectionType == CustomReportSectionTypes.Intelligence))
        {
            model.IntelligenceSummary = BuildIntelligenceSummary(model);
        }

        return model;
    }

    private static string BuildIntelligenceSummary(CustomReportViewViewModel model)
    {
        var parts = new List<string>();

        if (model.WorkItems.Count > 0)
        {
            parts.Add($"This report covers {model.WorkItems.Count} work item{(model.WorkItems.Count != 1 ? "s" : "")}.");

            var ragCounts = model.WorkItems
                .GroupBy(w => string.IsNullOrWhiteSpace(w.Rag) ? "Not set" : w.Rag)
                .OrderBy(g => RagSortKey(g.Key));
            var ragParts = ragCounts.Select(g => $"{g.Count()} {g.Key}");
            parts.Add($"RAG breakdown: {string.Join(", ", ragParts)}.");

            var redAmber = model.WorkItems.Count(w =>
                string.Equals(w.Rag, "Red", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(w.Rag, "Amber/Red", StringComparison.OrdinalIgnoreCase));
            if (redAmber > 0)
                parts.Add($"{redAmber} work item{(redAmber != 1 ? "s are" : " is")} rated Red or Amber/Red and may need attention.");
        }

        if (model.ServiceRegisterItems.Count > 0)
        {
            parts.Add($"The scope includes {model.ServiceRegisterItems.Count} service register entr{(model.ServiceRegisterItems.Count != 1 ? "ies" : "y")}.");
        }

        if (model.RiskCount > 0)
            parts.Add($"There are {model.RiskCount} open risk{(model.RiskCount != 1 ? "s" : "")} across the selected work items.");

        if (model.IssueCount > 0)
            parts.Add($"There are {model.IssueCount} open issue{(model.IssueCount != 1 ? "s" : "")} across the selected work items.");

        var lateMilestones = model.MilestoneRows.Count(m =>
            m.DueDate.HasValue && m.DueDate.Value < DateTime.UtcNow &&
            !string.Equals(m.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(m.Status, "Done", StringComparison.OrdinalIgnoreCase));
        if (lateMilestones > 0)
            parts.Add($"{lateMilestones} milestone{(lateMilestones != 1 ? "s are" : " is")} overdue.");

        var ptgCount = model.PathToGreenRows.Count;
        if (ptgCount > 0)
            parts.Add($"{ptgCount} work item{(ptgCount != 1 ? "s have" : " has")} a path to green narrative.");

        return string.Join(" ", parts);
    }

    private static int RagSortKey(string rag) => (rag?.Trim() ?? "") switch
    {
        var r when r.Equals("Green", StringComparison.OrdinalIgnoreCase) => 0,
        var r when r.Equals("Amber/Green", StringComparison.OrdinalIgnoreCase) => 1,
        var r when r.Equals("Amber", StringComparison.OrdinalIgnoreCase) => 2,
        var r when r.Equals("Amber/Red", StringComparison.OrdinalIgnoreCase) => 3,
        var r when r.Equals("Red", StringComparison.OrdinalIgnoreCase) => 4,
        _ => 10
    };
}
