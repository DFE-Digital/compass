using System.Security.Claims;
using Compass.Data;
using Compass.Models;
using Compass.Models.Raid;
using Compass.Models.Modern.Work;
using Compass.Services.Fips;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

public sealed class RaidIssueEditorFormService(CompassDbContext db) : IRaidIssueEditorFormService
{
    private readonly record struct RaidAssociationBind(string StoredKind, int? ProjectId, int? PrimaryProductId);

    public async Task PrepareIssueEditorLookupsAsync(
        Controller controller,
        int? ownerUserId,
        int? sroUserId,
        CancellationToken cancellationToken)
    {
        controller.ViewBag.IssueSeverityOptions = await db.IssueSeverities.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new RiskIssueNamedIntOption { Id = x.Id, Name = x.Label }).ToListAsync(cancellationToken);
        controller.ViewBag.IssuePriorityOptions = await db.IssuePriorities.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new RiskIssueNamedIntOption { Id = x.Id, Name = x.Label }).ToListAsync(cancellationToken);
        controller.ViewBag.IssueStatusOptions = await db.IssueStatuses.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new RiskIssueNamedIntOption { Id = x.Id, Name = x.Label }).ToListAsync(cancellationToken);
        controller.ViewBag.IssueCategoryOptions = await db.IssueCategories.AsNoTracking()
            .Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new RiskIssueNamedIntOption { Id = x.Id, Name = x.Label }).ToListAsync(cancellationToken);
        controller.ViewBag.ProjectOptions = await RaidEditorProjectOptionsFullAsync(cancellationToken);
        controller.ViewBag.FipsProductOptions = await FipsProductRaidQuery.BuildActiveServiceRegisterSelectOptionsForRaidAsync(db, cancellationToken);
        await PopulateRaidUserPickersAsync(controller, ownerUserId, sroUserId, cancellationToken);
        await LoadRaidDivisionBusinessAreaOptionsAsync(controller, cancellationToken);
    }

    public async Task<Issue?> TryCreateIssueFromEditorFormAsync(
        ModelStateDictionary modelState,
        ClaimsPrincipal user,
        ModernRaidIssueEditorForm form,
        int? forceWorkProjectId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
            modelState.AddModelError(nameof(form.Title), "Enter a title.");

        RaidAssociationBind? a;
        if (forceWorkProjectId is int forcedPid && forcedPid > 0)
        {
            var projectOk = await db.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == forcedPid && !p.IsDeleted, cancellationToken);
            if (!projectOk)
            {
                modelState.AddModelError(nameof(ModernRaidIssueEditorForm.ProjectId), "Select a valid work item.");
                a = null;
            }
            else
            {
                a = new RaidAssociationBind(RaidAssociationKinds.WorkItem, forcedPid, null);
            }
        }
        else
        {
            a = await TryBindRaidAssociationAsync(
                modelState,
                form.AssociationKind,
                form.ProjectId,
                form.PrimaryProductId,
                nameof(ModernRaidIssueEditorForm.ProjectId),
                nameof(ModernRaidIssueEditorForm.PrimaryProductId),
                cancellationToken);
        }

        if (!form.SeverityId.HasValue || form.SeverityId <= 0)
            modelState.AddModelError(nameof(form.SeverityId), "Select severity.");
        if (!form.PriorityId.HasValue || form.PriorityId <= 0)
            modelState.AddModelError(nameof(form.PriorityId), "Select priority.");

        form.IssueCategoryIds ??= new List<int>();
        form.DivisionIds ??= new List<int>();
        form.BusinessAreaLookupIds ??= new List<int>();
        form.AssuranceItems ??= new List<IssueAssuranceItemForm>();

        if (!RaidDateFormHelper.TryOptionalDate(form.TargetResolutionDay, form.TargetResolutionMonth, form.TargetResolutionYear, "TargetResolution", modelState, out var targetResolutionDt))
            return null;

        if (a is not { } assoc || !modelState.IsValid)
            return null;

        var issueStatusId = form.StatusId ?? await GetDefaultRaidIssueStatusIdAsync(cancellationToken);
        var sevRow = form.SeverityId.HasValue
            ? await db.IssueSeverities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == form.SeverityId.Value, cancellationToken)
            : null;
        var priRow = form.PriorityId.HasValue
            ? await db.IssuePriorities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == form.PriorityId.Value, cancellationToken)
            : null;
        var stRow = issueStatusId.HasValue
            ? await db.IssueStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == issueStatusId.Value, cancellationToken)
            : null;

        var now = DateTime.UtcNow;
        var createdByUserId = await ResolveCurrentUserIdAsync(user, cancellationToken);
        var issue = new Issue
        {
            ProjectId = assoc.ProjectId,
            PrimaryProductId = assoc.PrimaryProductId,
            RaidAssociationKind = assoc.StoredKind,
            Title = TruncateRaid(form.Title.Trim(), RaidFieldLimits.TitleMaxLength),
            Description = RaidFieldLimits.NormalizeNarrative(form.Description) ?? "",
            StatusId = issueStatusId,
            SeverityId = form.SeverityId,
            PriorityId = form.PriorityId,
            OwnerUserId = form.OwnerUserId > 0 ? form.OwnerUserId : null,
            SroUserId = form.SroUserId > 0 ? form.SroUserId : null,
            Severity = TruncateLowerRaid(sevRow?.Label ?? "medium", 10),
            Priority = priRow != null ? TruncateRaid(priRow.Label, 10) : null,
            Status = TruncateLowerRaid(stRow?.Label ?? "open", 20),
            TargetResolutionDate = targetResolutionDt,
            Workaround = RaidFieldLimits.NormalizeNarrative(form.Workaround),
            DetailedCause = RaidFieldLimits.NormalizeNarrative(form.DetailedCause),
            AssuranceArrangements = RaidFieldLimits.NormalizeNarrative(form.AssuranceArrangements),
            DetectedDate = DateTime.UtcNow.Date,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync(cancellationToken);
        await PersistIssueCategoryLinksAsync(issue, form.IssueCategoryIds, cancellationToken);
        await PersistIssueDivisionLinksAsync(issue, form.DivisionIds, cancellationToken);
        await PersistIssueBusinessAreaLinksAsync(issue, form.BusinessAreaLookupIds, cancellationToken);
        if (!await PersistIssueAssuranceEventsAsync(issue.Id, form.AssuranceItems, modelState, cancellationToken))
            return null;
        await db.SaveChangesAsync(cancellationToken);
        return issue;
    }

    public async Task<Issue?> TryCreateIssueFromRiskAsync(
        ModelStateDictionary modelState,
        ClaimsPrincipal user,
        ModernRaidMakeIssueFromRiskForm form,
        CancellationToken cancellationToken)
    {
        if (form.RiskId <= 0)
        {
            modelState.AddModelError(nameof(form.RiskId), "Invalid risk.");
            return null;
        }

        var risk = await db.Risks
            .Include(r => r.RiskDivisions)
            .Include(r => r.RiskBusinessAreas)
            .Include(r => r.RiskPriority)
            .FirstOrDefaultAsync(r => r.Id == form.RiskId && !r.IsDeleted, cancellationToken);
        if (risk == null)
        {
            modelState.AddModelError(string.Empty, "Risk not found.");
            return null;
        }

        var alreadyMaterialised = await db.Issues.AsNoTracking()
            .AnyAsync(i => !i.IsDeleted && i.SourceRiskId == form.RiskId, cancellationToken);
        if (alreadyMaterialised)
        {
            modelState.AddModelError(string.Empty, "An issue has already been raised from this risk.");
            return null;
        }

        var after = (form.RiskAfterIssue ?? "close").Trim().ToLowerInvariant();
        if (after is not ("close" or "keep_open"))
            modelState.AddModelError(nameof(form.RiskAfterIssue), "Choose whether to close the risk or keep it open.");

        if (!modelState.IsValid)
            return null;

        var descParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(risk.Description))
            descParts.Add(risk.Description.Trim());
        if (!string.IsNullOrWhiteSpace(risk.ImpactIfRealised))
            descParts.Add("Impact if realised:\n" + risk.ImpactIfRealised.Trim());
        if (!string.IsNullOrWhiteSpace(form.MaterialisationNote))
            descParts.Add(form.MaterialisationNote.Trim());

        var severityId = await ResolveIssueSeverityIdForRiskScoreAsync(risk.RiskScore, cancellationToken);
        var priorityId = await ResolveIssuePriorityIdFromRiskAsync(risk, cancellationToken);
        var issueStatusId = await GetDefaultRaidIssueStatusIdAsync(cancellationToken);
        var sevRow = severityId.HasValue
            ? await db.IssueSeverities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == severityId.Value, cancellationToken)
            : null;
        var priRow = priorityId.HasValue
            ? await db.IssuePriorities.AsNoTracking().FirstOrDefaultAsync(x => x.Id == priorityId.Value, cancellationToken)
            : null;
        var stRow = issueStatusId.HasValue
            ? await db.IssueStatuses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == issueStatusId.Value, cancellationToken)
            : null;

        var now = DateTime.UtcNow;
        var createdByUserId = await ResolveCurrentUserIdAsync(user, cancellationToken);
        var issue = new Issue
        {
            ProjectId = risk.ProjectId,
            PrimaryProductId = risk.PrimaryProductId,
            RaidAssociationKind = risk.RaidAssociationKind,
            Title = risk.Title.Trim(),
            Description = RaidFieldLimits.NormalizeNarrative(
                descParts.Count > 0 ? string.Join("\n\n", descParts) : risk.Title) ?? risk.Title,
            DetailedCause = RaidFieldLimits.NormalizeNarrative(risk.Cause),
            StatusId = issueStatusId,
            SeverityId = severityId,
            PriorityId = priorityId,
            OwnerUserId = risk.OwnerUserId,
            SroUserId = risk.SroUserId,
            Severity = TruncateLowerRaid(sevRow?.Label ?? "medium", 10),
            Priority = priRow != null ? TruncateRaid(priRow.Label, 10) : null,
            Status = TruncateLowerRaid(stRow?.Label ?? "open", 20),
            SourceRiskId = risk.Id,
            RiskId = risk.Id,
            DetectedDate = now.Date,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync(cancellationToken);

        db.IssueRisks.Add(new IssueRisk { IssueId = issue.Id, RiskId = risk.Id });

        var divisionIds = risk.RiskDivisions.Select(x => x.DivisionId).Where(i => i > 0).Distinct().ToList();
        var baIds = risk.RiskBusinessAreas.Select(x => x.BusinessAreaLookupId).Where(i => i > 0).Distinct().ToList();
        await PersistIssueDivisionLinksAsync(issue, divisionIds, cancellationToken);
        await PersistIssueBusinessAreaLinksAsync(issue, baIds, cancellationToken);

        if (after == "close")
        {
            var closeNow = DateTime.UtcNow;
            risk.ClosedDate = closeNow;
            risk.UpdatedAt = closeNow;
            var matStatusId = await ResolveRiskStatusIdByCodeAsync("MAT", cancellationToken)
                ?? await ResolveRiskStatusIdByCodeAsync("CLOSED", cancellationToken);
            if (matStatusId.HasValue)
            {
                risk.RiskStatusId = matStatusId;
                var statusRow = await db.RiskStatuses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == matStatusId.Value, cancellationToken);
                if (statusRow != null)
                    risk.Status = TruncateLowerRaid(statusRow.Label, 20);
            }
            if (!string.IsNullOrWhiteSpace(form.MaterialisationNote))
            {
                var noteLine = $"Materialised as issue I-{issue.Id:D4}: {form.MaterialisationNote.Trim()}";
                risk.Notes = string.IsNullOrWhiteSpace(risk.Notes)
                    ? noteLine
                    : risk.Notes.Trim() + "\n\n" + noteLine;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return issue;
    }

    private async Task<int?> ResolveIssueSeverityIdForRiskScoreAsync(int riskScore, CancellationToken cancellationToken)
    {
        var code = riskScore >= 16 ? "MAJ" : riskScore >= 11 ? "MED" : "MIN";
        return await db.IssueSeverities.AsNoTracking()
            .Where(x => x.IsActive && x.Code.ToLower() == code.ToLower())
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int?> ResolveIssuePriorityIdFromRiskAsync(Risk risk, CancellationToken cancellationToken)
    {
        if (risk.RiskPriorityId is > 0 && risk.RiskPriority != null)
        {
            var label = risk.RiskPriority.Label.Trim();
            var byLabel = await db.IssuePriorities.AsNoTracking()
                .Where(x => x.IsActive && x.Label.ToLower() == label.ToLower())
                .OrderBy(x => x.SortOrder)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (byLabel.HasValue)
                return byLabel;
        }

        var code = risk.RiskScore >= 16 ? "CRIT" : risk.RiskScore >= 11 ? "HIGH" : risk.RiskScore >= 6 ? "MED" : "LOW";
        return await db.IssuePriorities.AsNoTracking()
            .Where(x => x.IsActive && x.Code.ToLower() == code.ToLower())
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int?> ResolveRiskStatusIdByCodeAsync(string code, CancellationToken cancellationToken) =>
        await db.RiskStatuses.AsNoTracking()
            .Where(x => x.IsActive && x.Code.ToLower() == code.ToLower())
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<List<RiskIssueNamedIntOption>> RaidEditorProjectOptionsFullAsync(CancellationToken cancellationToken) =>
        await db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .Select(p => new RiskIssueNamedIntOption { Id = p.Id, Name = p.Title ?? "" })
            .ToListAsync(cancellationToken);

    private async Task PopulateRaidUserPickersAsync(
        Controller controller,
        int? ownerUserId,
        int? sroUserId,
        CancellationToken cancellationToken)
    {
        var ids = new List<int>();
        if (ownerUserId is > 0) ids.Add(ownerUserId.Value);
        if (sroUserId is > 0) ids.Add(sroUserId.Value);
        ids = ids.Distinct().ToList();

        Dictionary<int, User> map = new();
        if (ids.Count > 0)
            map = await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken);

        static string? DisplayName(User u) => string.IsNullOrWhiteSpace(u.Name) ? u.Email : u.Name;

        User? OwnerRow() => ownerUserId is > 0 && map.TryGetValue(ownerUserId.Value, out var u) ? u : null;
        User? SroRow() => sroUserId is > 0 && map.TryGetValue(sroUserId.Value, out var u) ? u : null;

        controller.ViewBag.OwnerUserPicker = new UserPickerViewModel
        {
            FieldName = "OwnerUserId",
            Label = "Owner",
            DefaultUserId = ownerUserId,
            DefaultName = OwnerRow() is { } o ? DisplayName(o) : null,
            DefaultEmail = OwnerRow()?.Email,
            InputIdSuffix = "owner",
            UseGovUkStyling = true
        };

        controller.ViewBag.SroUserPicker = new UserPickerViewModel
        {
            FieldName = "SroUserId",
            Label = "Senior responsible officer (SRO)",
            DefaultUserId = sroUserId,
            DefaultName = SroRow() is { } s ? DisplayName(s) : null,
            DefaultEmail = SroRow()?.Email,
            InputIdSuffix = "sro",
            UseGovUkStyling = true
        };
    }

    private async Task LoadRaidDivisionBusinessAreaOptionsAsync(Controller controller, CancellationToken cancellationToken)
    {
        controller.ViewBag.DivisionOptions = await db.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new RiskIssueNamedIntOption { Id = d.Id, Name = d.Name })
            .ToListAsync(cancellationToken);
        controller.ViewBag.BusinessAreaOptions = await db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new RiskIssueNamedIntOption { Id = b.Id, Name = b.Name })
            .ToListAsync(cancellationToken);
    }

    private async Task<RaidAssociationBind?> TryBindRaidAssociationAsync(
        ModelStateDictionary modelState,
        string? uiAssociationKind,
        int? projectIdForm,
        int? primaryProductIdForm,
        string projectFieldKey,
        string productFieldKey,
        CancellationToken cancellationToken)
    {
        var k = (uiAssociationKind ?? "").Trim().ToLowerInvariant();
        if (k == "organization")
            k = "organisation";

        if (string.IsNullOrEmpty(k))
        {
            modelState.AddModelError("AssociationKind",
                "Select whether this is associated with a work item, a service register product, or organisation.");
            return null;
        }

        switch (k)
        {
            case "product":
                var primaryProductId = primaryProductIdForm is > 0 ? primaryProductIdForm : null;
                if (!primaryProductId.HasValue)
                {
                    modelState.AddModelError(productFieldKey, "Select a service register product.");
                    return null;
                }

                var productOk = await db.Services.AsNoTracking()
                    .AnyAsync(s => s.ServiceId == primaryProductId.Value && s.IsActive, cancellationToken);
                if (!productOk)
                {
                    modelState.AddModelError(productFieldKey, "Select a valid service register product.");
                    return null;
                }

                return new RaidAssociationBind(RaidAssociationKinds.Product, null, primaryProductId);

            case "organisation":
                return new RaidAssociationBind(RaidAssociationKinds.Organisation, null, null);

            case "work":
                var projectId = projectIdForm is > 0 ? projectIdForm : null;
                if (!projectId.HasValue)
                {
                    modelState.AddModelError(projectFieldKey, "Select a work item.");
                    return null;
                }

                var projectOk = await db.Projects.AsNoTracking()
                    .AnyAsync(p => p.Id == projectId.Value && !p.IsDeleted, cancellationToken);
                if (!projectOk)
                {
                    modelState.AddModelError(projectFieldKey, "Select a valid work item.");
                    return null;
                }

                return new RaidAssociationBind(RaidAssociationKinds.WorkItem, projectId, null);

            default:
                modelState.AddModelError("AssociationKind",
                    "Select whether this is associated with a work item, a service register product, or organisation.");
                return null;
        }
    }

    private async Task<int?> GetDefaultRaidIssueStatusIdAsync(CancellationToken cancellationToken)
    {
        var id = await db.IssueStatuses.AsNoTracking()
            .Where(x => x.IsActive && x.Code.ToLower() == "open")
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (id.HasValue)
            return id;
        return await db.IssueStatuses.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string TruncateRaid(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Trim();
        return t.Length <= max ? t : t[..max];
    }

    private static string TruncateLowerRaid(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Trim().ToLowerInvariant();
        return t.Length <= max ? t : t[..max];
    }

    private async Task<int?> ResolveCurrentUserIdAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var email = user.Identity?.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst("preferred_username")?.Value;
        if (string.IsNullOrWhiteSpace(email))
            return null;
        email = email.Trim();
        return await db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == email.ToLower())
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task PersistIssueCategoryLinksAsync(Issue issue, List<int>? selectedIds, CancellationToken cancellationToken)
    {
        var ids = (selectedIds ?? new List<int>()).Where(i => i > 0).Distinct().ToList();
        var rows = await db.IssueIssueCategories.Where(x => x.IssueId == issue.Id).ToListAsync(cancellationToken);
        db.IssueIssueCategories.RemoveRange(rows);
        foreach (var cid in ids)
            db.IssueIssueCategories.Add(new IssueIssueCategory { IssueId = issue.Id, IssueCategoryId = cid });
        issue.IssueCategoryId = ids.Count > 0 ? ids[0] : null;
    }

    private async Task PersistIssueDivisionLinksAsync(Issue issue, List<int>? selectedIds, CancellationToken cancellationToken)
    {
        var ids = (selectedIds ?? new List<int>()).Where(i => i > 0).Distinct().ToList();
        var rows = await db.IssueDivisions.Where(x => x.IssueId == issue.Id).ToListAsync(cancellationToken);
        db.IssueDivisions.RemoveRange(rows);
        foreach (var divId in ids)
            db.IssueDivisions.Add(new IssueDivision { IssueId = issue.Id, DivisionId = divId });
    }

    private async Task PersistIssueBusinessAreaLinksAsync(Issue issue, List<int>? selectedIds, CancellationToken cancellationToken)
    {
        var ids = (selectedIds ?? new List<int>()).Where(i => i > 0).Distinct().ToList();
        var rows = await db.IssueBusinessAreas.Where(x => x.IssueId == issue.Id).ToListAsync(cancellationToken);
        db.IssueBusinessAreas.RemoveRange(rows);
        foreach (var baId in ids)
            db.IssueBusinessAreas.Add(new IssueBusinessArea { IssueId = issue.Id, BusinessAreaLookupId = baId });
    }

    private async Task<bool> PersistIssueAssuranceEventsAsync(
        int issueId,
        List<IssueAssuranceItemForm>? items,
        ModelStateDictionary modelState,
        CancellationToken cancellationToken)
    {
        items ??= new List<IssueAssuranceItemForm>();
        var existing = await db.IssueAssuranceEvents.Where(x => x.IssueId == issueId).ToListAsync(cancellationToken);
        db.IssueAssuranceEvents.RemoveRange(existing);

        var sortOrder = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var it = items[i];
            var title = (it.Title ?? "").Trim();
            var kind = (it.EventKind ?? "").Trim();
            var decision = (it.DecisionSummary ?? "").Trim();
            var hasDatePart = it.EventDay.HasValue || it.EventMonth.HasValue || it.EventYear.HasValue;
            var hasContent = !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(kind)
                || !string.IsNullOrWhiteSpace(decision) || hasDatePart;
            if (!hasContent)
                continue;

            var dateKey = $"AssuranceItems[{i}].EventDate";
            if (!RaidDateFormHelper.TryOptionalDate(it.EventDay, it.EventMonth, it.EventYear, dateKey, modelState, out var eventDt))
                return false;

            if (string.IsNullOrWhiteSpace(title))
            {
                modelState.AddModelError($"AssuranceItems[{i}].Title", "Enter a short name for each assurance event (or remove the empty row).");
                return false;
            }

            var kindNorm = string.IsNullOrWhiteSpace(kind) ? "review" : kind;
            if (kindNorm.Length > 50)
                kindNorm = kindNorm[..50];
            var titleSafe = title.Length > 500 ? title[..500] : title;

            db.IssueAssuranceEvents.Add(new IssueAssuranceEvent
            {
                IssueId = issueId,
                EventKind = kindNorm,
                Title = titleSafe,
                EventDate = eventDt,
                DecisionSummary = RaidFieldLimits.NormalizeNarrative(decision),
                SortOrder = sortOrder++
            });
        }

        return true;
    }
}
