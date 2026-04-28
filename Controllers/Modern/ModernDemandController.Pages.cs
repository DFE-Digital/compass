using System.Text.Json;
using Compass.Models;
using Compass.Models.DemandPipeline;
using Compass.Models.Modern.Work;
using Compass.Services.DemandPipeline;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernDemandController
{
    [HttpGet("submit")]
    [HttpGet("/ModernDemand/Submit")]
    public async Task<IActionResult> Submit(Guid? businessCaseId, Guid? id)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";
        await PopulateDemandSubmitLookupsAsync();

        DemandPipelineRequest model;

        if (id.HasValue)
        {
            var existing = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id.Value);
            if (existing == null)
                return NotFound();

            model = existing;
            ViewBag.DemandEditFromRequest = !string.Equals(existing.Status, "Draft", StringComparison.OrdinalIgnoreCase);
            if (existing.BusinessCaseId.HasValue)
                ViewBag.LinkedBc = await _db.DemandPipelineBusinessCases.AsNoTracking().FirstOrDefaultAsync(b => b.Id == existing.BusinessCaseId.Value);
        }
        else
        {
            model = new DemandPipelineRequest
            {
                Status = "Draft",
                StatutoryDriver = null,
                FundingProvided = null,
                HeadcountProvided = null,
                IsNewDigitalService = null,
                IsPublicFacing = null,
                BusinessCaseId = businessCaseId
            };

            if (businessCaseId.HasValue)
            {
                var bc = await _db.DemandPipelineBusinessCases.AsNoTracking().FirstOrDefaultAsync(b => b.Id == businessCaseId.Value);
                if (bc != null)
                {
                    model.Title = bc.Title;
                    model.Description = bc.ProblemStatement;
                    model.DepartmentGroup = bc.DepartmentGroup;
                    model.PortfolioId = bc.PortfolioId;
                    model.GovernmentDepartmentId = bc.GovernmentDepartmentId;
                    model.SroUserId = bc.SroUserId;
                    model.Sro = bc.Sro;
                    model.TargetDeliveryDate = bc.TargetSubmissionDate;
                    model.ExpectedBenefits = bc.Benefits;
                    model.PreviousResearch = bc.Evidence;
                    model.StatutoryDriver = bc.StatutoryDriver;
                    model.PriorityOutcomeId = bc.PriorityOutcomeId;
                    model.MissionPillarId = bc.MissionPillarId;
                    model.PriorityOutcomeIds = bc.PriorityOutcomeIds;
                    model.MissionPillarIds = bc.MissionPillarIds;
                    model.FundingProvided = string.Equals(bc.FundingPosition, "Yes", StringComparison.OrdinalIgnoreCase)
                        ? true
                        : string.Equals(bc.FundingPosition, "No", StringComparison.OrdinalIgnoreCase)
                            ? false
                            : null;
                    model.FundingProvidedDetails = bc.FundingComments;
                    model.HeadcountProvided = bc.HeadcountIdentified;
                    ViewBag.LinkedBc = bc;
                }
            }
        }

        return View("~/Views/Modern/Demand/Submit.cshtml", model);
    }

    [HttpGet("request/{id:guid}/edit")]
    public Task<IActionResult> RequestEdit(Guid id) => Submit(businessCaseId: null, id: id);

    [HttpPost("submit")]
    [HttpPost("/ModernDemand/Submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        DemandPipelineRequest input,
        string? command,
        int[]? priorityOutcomeIds,
        int[]? missionPillarIds,
        string? _,
        string? __)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";

        var isSubmit = string.Equals(command, "submit", StringComparison.OrdinalIgnoreCase);
        var isDraftSave = !isSubmit;

        input.PriorityOutcomeIds = JoinIntList(priorityOutcomeIds);
        input.MissionPillarIds = JoinIntList(missionPillarIds);

        ValidateDemandSubmitInput(input, isSubmit);

        if (!ModelState.IsValid)
        {
            await PopulateDemandSubmitLookupsAsync();
            if (input.BusinessCaseId.HasValue)
                ViewBag.LinkedBc = await _db.DemandPipelineBusinessCases.AsNoTracking().FirstOrDefaultAsync(b => b.Id == input.BusinessCaseId.Value);
            return View("~/Views/Modern/Demand/Submit.cshtml", input);
        }

        var now = DateTime.UtcNow;
        var actor = User.Identity?.Name;
        var submittedByDisplay = await ResolveUserDisplayNameAsync(input.SubmittedByUserId, fallback: actor);
        var sroDisplay = await ResolveUserDisplayNameAsync(input.SroUserId, fallback: input.Sro);

        DemandPipelineRequest demand;
        if (input.Id != Guid.Empty)
        {
            demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == input.Id) ?? new DemandPipelineRequest { Id = input.Id };
            if (_db.Entry(demand).State == EntityState.Detached)
                _db.DemandPipelineRequests.Add(demand);
        }
        else
        {
            demand = new DemandPipelineRequest
            {
                Id = Guid.NewGuid(),
                Reference = await BuildDemandReferenceAsync(),
                CreatedAt = now,
                CreatedBy = actor
            };
            _db.DemandPipelineRequests.Add(demand);
        }

        demand.BusinessCaseId = input.BusinessCaseId;
        demand.Title = input.Title?.Trim() ?? string.Empty;
        demand.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        demand.SubmittedByUserId = input.SubmittedByUserId;
        demand.SubmittedBy = submittedByDisplay;
        demand.SroUserId = input.SroUserId;
        demand.Sro = sroDisplay;
        demand.DepartmentGroup = string.IsNullOrWhiteSpace(input.DepartmentGroup) ? null : input.DepartmentGroup.Trim();
        demand.PortfolioId = input.PortfolioId;
        demand.GovernmentDepartmentId = input.GovernmentDepartmentId;
        demand.PreviousResearch = string.IsNullOrWhiteSpace(input.PreviousResearch) ? null : input.PreviousResearch.Trim();
        demand.TargetDeliveryDate = input.TargetDeliveryDate;
        demand.ManifestoCommitment = string.IsNullOrWhiteSpace(input.ManifestoCommitment) ? null : input.ManifestoCommitment.Trim();
        demand.ExpectedBenefits = string.IsNullOrWhiteSpace(input.ExpectedBenefits) ? null : input.ExpectedBenefits.Trim();
        demand.RiskIfNotDelivered = string.IsNullOrWhiteSpace(input.RiskIfNotDelivered) ? null : input.RiskIfNotDelivered.Trim();
        demand.StatutoryDriver = input.StatutoryDriver;
        demand.PriorityOutcomeId = input.PriorityOutcomeId;
        demand.MissionPillarId = input.MissionPillarId;
        demand.PriorityOutcomeIds = input.PriorityOutcomeIds;
        demand.MissionPillarIds = input.MissionPillarIds;
        demand.FundingProvided = input.FundingProvided;
        demand.FundingProvidedDetails = string.IsNullOrWhiteSpace(input.FundingProvidedDetails) ? null : input.FundingProvidedDetails.Trim();
        demand.HeadcountProvided = input.HeadcountProvided;
        demand.HeadcountProvidedDetails = string.IsNullOrWhiteSpace(input.HeadcountProvidedDetails) ? null : input.HeadcountProvidedDetails.Trim();
        demand.IsNewDigitalService = input.IsNewDigitalService;
        demand.DigitalServiceChangeDetails = string.IsNullOrWhiteSpace(input.DigitalServiceChangeDetails) ? null : input.DigitalServiceChangeDetails.Trim();
        demand.IsPublicFacing = input.IsPublicFacing;

        // Only set status for new demands or those still in Draft.
        // Demands already advanced through the pipeline keep their current status.
        var isNew = string.IsNullOrEmpty(demand.Status) || demand.Status == "Draft";
        if (isNew)
        {
            demand.Status = isSubmit
                ? (input.BusinessCaseId.HasValue ? "Active" : "Submitted")
                : "Draft";
        }
        demand.SubmittedDate = isSubmit ? (demand.SubmittedDate ?? input.SubmittedDate ?? now) : demand.SubmittedDate;
        demand.UpdatedAt = now;
        demand.UpdatedBy = actor;

        await _db.SaveChangesAsync();

        if (isSubmit)
            return RedirectToAction(nameof(Submitted), new { id = demand.Id });

        TempData["Message"] = "Draft saved.";
        return RedirectToAction(nameof(Register), new { tab = "drafts" });
    }

    [HttpGet("submitted")]
    [HttpGet("/ModernDemand/Submitted")]
    public async Task<IActionResult> Submitted(Guid id)
    {
        var demand = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";
        return View("~/Views/Modern/Demand/Submitted.cshtml", demand);
    }

    [HttpGet("businesscase/new")]
    [HttpGet("BusinessCaseNew")]
    [HttpGet("/ModernDemand/BusinessCaseNew")]
    public async Task<IActionResult> BusinessCaseNew()
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-businesscase";
        await PopulateBusinessCaseLookupsAsync();

        var model = new DemandPipelineBusinessCase
        {
            Stage = "Idea",
            Status = "Active"
        };

        return View("~/Views/Modern/Demand/BusinessCaseNew.cshtml", model);
    }

    [HttpPost("businesscase/new")]
    [HttpPost("BusinessCaseNew")]
    [HttpPost("/ModernDemand/BusinessCaseNew")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BusinessCaseNew(
        DemandPipelineBusinessCase input,
        string? command,
        int[]? selectedPriorityOutcomeIds,
        int[]? selectedMissionPillarIds)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-businesscase";

        input.PriorityOutcomeIds = JoinIntList(selectedPriorityOutcomeIds);
        input.MissionPillarIds = JoinIntList(selectedMissionPillarIds);
        var isSubmit = string.Equals(command, "submit", StringComparison.OrdinalIgnoreCase);

        ValidateBusinessCaseInput(input, isSubmit);

        if (!ModelState.IsValid)
        {
            await PopulateBusinessCaseLookupsAsync();
            return View("~/Views/Modern/Demand/BusinessCaseNew.cshtml", input);
        }

        var now = DateTime.UtcNow;
        var actor = User.Identity?.Name;

        input.Id = Guid.NewGuid();
        input.Reference = await BuildBusinessCaseReferenceAsync();
        input.CreatedAt = now;
        input.CreatedBy = actor;
        input.UpdatedAt = now;
        input.UpdatedBy = actor;
        input.Status = "Active";
        input.Lead = await ResolveUserDisplayNameAsync(input.LeadUserId, fallback: actor);
        input.Sro = await ResolveUserDisplayNameAsync(input.SroUserId, fallback: input.Sro);

        _db.DemandPipelineBusinessCases.Add(input);
        await _db.SaveChangesAsync();

        TempData["Message"] = isSubmit ? "Business case submitted." : "Business case saved.";
        return RedirectToAction(nameof(BusinessCase));
    }

    private async Task PopulateDemandSubmitLookupsAsync()
    {
        var orgGroups = await _db.OrganizationalGroups.AsNoTracking()
            .Where(g => g.IsActive)
            .OrderBy(g => g.Name)
            .ToListAsync();

        ViewBag.Portfolios = orgGroups
            .Select(g => new Portfolio { Id = g.Id, Name = g.Name, IsActive = true })
            .ToList();

        ViewBag.DepartmentGroups = await _db.DemandPipelineRequests.AsNoTracking()
            .Where(d => !string.IsNullOrWhiteSpace(d.DepartmentGroup))
            .Select(d => d.DepartmentGroup!)
            .Union(_db.DemandPipelineBusinessCases.AsNoTracking()
                .Where(b => !string.IsNullOrWhiteSpace(b.DepartmentGroup))
                .Select(b => b.DepartmentGroup!))
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        ViewBag.PriorityOutcomes = await _db.Objectives.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Status == "active")
            .OrderBy(o => o.Title)
            .ToListAsync();

        ViewBag.MissionPillars = await _db.Missions.AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Title)
            .ToListAsync();
    }

    private async Task PopulateBusinessCaseLookupsAsync()
    {
        await PopulateDemandSubmitLookupsAsync();
        ViewBag.DepartmentGroupOptions = ViewBag.DepartmentGroups;
    }

    private static string JoinIntList(int[]? ids)
        => ids == null || ids.Length == 0
            ? string.Empty
            : string.Join(',', ids.Where(x => x > 0).Distinct().OrderBy(x => x));

    private void ValidateDemandSubmitInput(DemandPipelineRequest model, bool isSubmit)
    {
        if (!isSubmit)
            return;

        if (model.SubmittedByUserId == null)
            ModelState.AddModelError(nameof(model.SubmittedByUserId), "Select a primary contact.");
        if (string.IsNullOrWhiteSpace(model.DepartmentGroup))
            ModelState.AddModelError(nameof(model.DepartmentGroup), "Select a business area.");
        if (model.SroUserId == null)
            ModelState.AddModelError(nameof(model.SroUserId), "Select the SRO.");
        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "Enter a title.");
        if (string.IsNullOrWhiteSpace(model.Description))
            ModelState.AddModelError(nameof(model.Description), "Enter a request overview.");
        if (!model.TargetDeliveryDate.HasValue)
            ModelState.AddModelError(nameof(model.TargetDeliveryDate), "Enter a target delivery date.");
        if (model.StatutoryDriver == null)
            ModelState.AddModelError(nameof(model.StatutoryDriver), "Select whether this is a legal or statutory requirement.");
        if (model.StatutoryDriver == true && string.IsNullOrWhiteSpace(model.ManifestoCommitment))
            ModelState.AddModelError(nameof(model.ManifestoCommitment), "Enter the supporting manifesto commitment or statute law.");
        if (string.IsNullOrWhiteSpace(model.ExpectedBenefits))
            ModelState.AddModelError(nameof(model.ExpectedBenefits), "Enter expected benefits.");
        if (string.IsNullOrWhiteSpace(model.RiskIfNotDelivered))
            ModelState.AddModelError(nameof(model.RiskIfNotDelivered), "Enter the risk if not delivered.");
        if (model.FundingProvided == null)
            ModelState.AddModelError(nameof(model.FundingProvided), "Select whether funding is provided.");
        if (model.FundingProvided == true && string.IsNullOrWhiteSpace(model.FundingProvidedDetails))
            ModelState.AddModelError(nameof(model.FundingProvidedDetails), "Enter funding details.");
        if (model.HeadcountProvided == null)
            ModelState.AddModelError(nameof(model.HeadcountProvided), "Select whether headcount is provided.");
        if (model.HeadcountProvided == true && string.IsNullOrWhiteSpace(model.HeadcountProvidedDetails))
            ModelState.AddModelError(nameof(model.HeadcountProvidedDetails), "Enter headcount details.");
        if (model.IsNewDigitalService == null)
            ModelState.AddModelError(nameof(model.IsNewDigitalService), "Select whether this changes an existing digital service or product.");
        if (model.IsNewDigitalService == true && string.IsNullOrWhiteSpace(model.DigitalServiceChangeDetails))
            ModelState.AddModelError(nameof(model.DigitalServiceChangeDetails), "Enter details of products or services.");
        if (model.IsPublicFacing == null)
            ModelState.AddModelError(nameof(model.IsPublicFacing), "Select whether this is public-facing.");
    }

    private void ValidateBusinessCaseInput(DemandPipelineBusinessCase model, bool isSubmit)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            ModelState.AddModelError(nameof(model.Title), "Enter a title.");
        if (string.IsNullOrWhiteSpace(model.DepartmentGroup))
            ModelState.AddModelError(nameof(model.DepartmentGroup), "Select a business area.");
        if (model.LeadUserId == null)
            ModelState.AddModelError(nameof(model.LeadUserId), "Select a business case lead.");
        if (model.SroUserId == null)
            ModelState.AddModelError(nameof(model.SroUserId), "Select an SRO.");
        if (string.IsNullOrWhiteSpace(model.Stage))
            ModelState.AddModelError(nameof(model.Stage), "Select a stage.");

        if (!isSubmit)
            return;

        if (string.IsNullOrWhiteSpace(model.ProblemStatement))
            ModelState.AddModelError(nameof(model.ProblemStatement), "Enter a brief description.");
        if (!model.TargetSubmissionDate.HasValue)
            ModelState.AddModelError(nameof(model.TargetSubmissionDate), "Enter an anticipated demand submission date.");
        if (string.IsNullOrWhiteSpace(model.Benefits))
            ModelState.AddModelError(nameof(model.Benefits), "Enter expected value or benefits.");
        if (model.StatutoryDriver == null)
            ModelState.AddModelError(nameof(model.StatutoryDriver), "Select whether this is a legal or statutory requirement.");
        if (string.IsNullOrWhiteSpace(model.FundingPosition))
            ModelState.AddModelError(nameof(model.FundingPosition), "Select whether funding is confirmed.");
        if (model.HeadcountIdentified == null)
            ModelState.AddModelError(nameof(model.HeadcountIdentified), "Select whether headcount is identified.");
        if (string.IsNullOrWhiteSpace(model.SubjectToInvestco))
            ModelState.AddModelError(nameof(model.SubjectToInvestco), "Select whether this is subject to investco.");
    }

    private async Task<string> ResolveUserDisplayNameAsync(int? userId, string? fallback)
    {
        if (!userId.HasValue)
            return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
        if (user == null)
            return string.IsNullOrWhiteSpace(fallback) ? string.Empty : fallback.Trim();

        return !string.IsNullOrWhiteSpace(user.Name) ? user.Name : (user.Email ?? string.Empty);
    }

    private async Task<string> BuildBusinessCaseReferenceAsync()
    {
        var existing = await _db.DemandPipelineBusinessCases.AsNoTracking()
            .Select(x => x.Reference)
            .ToListAsync();

        var max = ExtractMaxReferenceNumber(existing);
        return $"BC-{max + 1:D5}";
    }

    private async Task<string> BuildDemandReferenceAsync()
    {
        var existing = await _db.DemandPipelineRequests.AsNoTracking()
            .Select(x => x.Reference)
            .ToListAsync();

        var max = ExtractMaxReferenceNumber(existing);
        return $"DR-{max + 1:D5}";
    }

    private static int ExtractMaxReferenceNumber(IEnumerable<string?> references)
    {
        var max = 0;
        foreach (var value in references)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var parsed) && parsed > max)
                max = parsed;
        }

        return max;
    }

    [HttpGet("explore")]
    [HttpGet("/ModernDemand/Explore")]
    public async Task<IActionResult> Explore()
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";

        var list = await _db.DemandPipelineRequests.AsNoTracking()
            .Where(d => d.Status == "ExploratoryReview")
            .OrderByDescending(d => d.UpdatedAt)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();

        return View("~/Views/Modern/Demand/Explore.cshtml", list);
    }

    [HttpGet("scoring")]
    [HttpGet("/ModernDemand/Scoring")]
    public async Task<IActionResult> Scoring()
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";

        var list = await _db.DemandPipelineRequests.AsNoTracking()
            .Where(d => d.Status == "Scoring" || d.Status == "Scored")
            .OrderByDescending(d => d.UpdatedAt)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();

        return View("~/Views/Modern/Demand/Scoring.cshtml", list);
    }

    [HttpGet("triage")]
    [HttpGet("/ModernDemand/Triage")]
    public async Task<IActionResult> Triage(Guid? meetingId)
    {
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";

        var queue = await _db.DemandPipelineRequests.AsNoTracking()
            .Where(d => d.Status == "Scored" || d.Status == "TriagePending" || d.Status == "Triage Pending")
            .OrderByDescending(d => d.UpdatedAt)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();

        var assignedDemands = await _db.DemandPipelineRequests.AsNoTracking()
            .Where(d => d.TriageMeetingId.HasValue)
            .OrderByDescending(d => d.UpdatedAt)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync();

        var meetings = await _db.DemandPipelineTriageMeetings.AsNoTracking()
            .OrderByDescending(m => m.MeetingDate)
            .ThenBy(m => m.StartTime)
            .ToListAsync();

        var counts = assignedDemands
            .GroupBy(d => d.TriageMeetingId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var vm = new DemandTriageHubViewModel
        {
            TriageQueue = queue,
            SelectedMeetingId = meetingId,
            Meetings = meetings
                .Select(m => new TriageMeetingRowViewModel
                {
                    Meeting = m,
                    DemandCount = counts.TryGetValue(m.Id, out var c) ? c : 0
                })
                .ToList()
        };

        if (meetingId.HasValue)
        {
            vm.MeetingDemands = assignedDemands
                .Where(d => d.TriageMeetingId == meetingId.Value)
                .ToList();
        }

        return View("~/Views/Modern/Demand/Triage.cshtml", vm);
    }

    [HttpGet("request/{id:guid}")]
    [HttpGet("/ModernDemand/RequestDetail/{id:guid}")]
    public async Task<IActionResult> RequestDetail(Guid id, string? tab)
    {
        var demand = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-detail";
        ViewBag.Tab = string.IsNullOrWhiteSpace(tab) ? "request" : tab.Trim().ToLowerInvariant();
        ViewBag.CanEditDemandRequestInfo = demand.Status != "Progressed to delivery"
            && demand.Status != "Closed - Progressed to delivery"
            && demand.Status != "Closed";

        await PopulateRequestDetailViewBagsAsync(demand);

        return View("~/Views/Modern/Demand/RequestDetail.cshtml", demand);
    }

    [HttpPost("request/{id:guid}/workflow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestWorkflowTransition(Guid id, string transition)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        switch ((transition ?? string.Empty).Trim())
        {
            case "to-explore":
                demand.Status = "ExploratoryReview";
                break;
            case "to-scoring":
                demand.Status = "Scoring";
                break;
            case "to-scored":
                demand.Status = "Scored";
                demand.ScoredAt = DateTime.UtcNow;
                demand.ScoredBy = User.Identity?.Name;
                break;
            case "to-triage":
                demand.Status = "TriagePending";
                break;
            case "to-triaged":
                demand.Status = "Triaged";
                demand.TriagedAt = DateTime.UtcNow;
                demand.TriagedBy = User.Identity?.Name;
                break;
            case "to-progressed":
                demand.Status = "Progressed to delivery";
                break;
            case "to-returned":
                demand.Status = "Returned";
                break;
            case "to-rejected":
                demand.Status = "Rejected";
                break;
            case "to-paused":
                demand.Status = "Paused";
                break;
            default:
                TempData["Error"] = "Unknown workflow transition.";
                return RedirectToAction(nameof(RequestDetail), new { id });
        }

        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Demand stage updated.";
        return RedirectToAction(nameof(RequestDetail), new { id });
    }

    [HttpPost("request/{id:guid}/assign-triage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAssignTriageMeeting(Guid id, Guid? triageMeetingId)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        if (triageMeetingId.HasValue)
        {
            var exists = await _db.DemandPipelineTriageMeetings.AsNoTracking().AnyAsync(m => m.Id == triageMeetingId.Value);
            if (!exists)
            {
                TempData["Error"] = "Triage meeting not found.";
                return RedirectToAction(nameof(RequestDetail), new { id, tab = "triage" });
            }
        }

        demand.TriageMeetingId = triageMeetingId;
        if ((demand.Status == "Scored" || demand.Status == "TriagePending" || demand.Status == "Triage Pending") && triageMeetingId.HasValue)
            demand.Status = "TriagePending";
        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;

        await _db.SaveChangesAsync();
        TempData["Message"] = triageMeetingId.HasValue ? "Triage meeting assigned." : "Triage meeting removed.";
        return RedirectToAction(nameof(RequestDetail), new { id, tab = "triage" });
    }

    [HttpPost("request/{id:guid}/record-triage")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestRecordTriageOutcome(
        Guid id,
        string? createWorkForDelivery,
        string? withoutDeliveryOutcome,
        int? triageStageLookupId,
        int? businessAreaId,
        int? triagePrimaryContactUserId,
        string? triageOutcomeNarrative)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        var deliveryYes = string.Equals(createWorkForDelivery, "yes", StringComparison.OrdinalIgnoreCase);
        var deliveryNo = string.Equals(createWorkForDelivery, "no", StringComparison.OrdinalIgnoreCase);

        if (!deliveryYes && !deliveryNo)
        {
            TempData["Error"] = "Choose whether work should be created for delivery.";
            return RedirectToAction(nameof(RequestDetail), new { id, tab = "triage" });
        }

        if (string.IsNullOrWhiteSpace(triageOutcomeNarrative))
        {
            TempData["Error"] = "Enter triage comments.";
            return RedirectToAction(nameof(RequestDetail), new { id, tab = "triage" });
        }

        demand.TriageOutcomeNarrative = triageOutcomeNarrative.Trim();
        demand.TriageStageLookupId = triageStageLookupId;
        demand.TriageAssignedBusinessAreaId = businessAreaId;
        demand.TriagePrimaryContactUserId = triagePrimaryContactUserId;
        demand.TriagedAt = DateTime.UtcNow;
        demand.TriagedBy = User.Identity?.Name;

        if (deliveryYes)
        {
            var project = new Project
            {
                ProjectCode = BuildProjectCode(),
                Title = demand.Title,
                Aim = demand.Description,
                TargetDeliveryDate = demand.TargetDeliveryDate,
                BusinessAreaId = businessAreaId,
                PrimaryContactUserId = triagePrimaryContactUserId,
                PipelineDemandRequestId = demand.Id,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            demand.TriageCreatedProjectId = project.Id;
            demand.TriageOutcome = DemandTriageDecisions.ProgressedToDelivery;
            demand.Status = "Progressed to delivery";
        }
        else
        {
            var outcome = (withoutDeliveryOutcome ?? "active").Trim().ToLowerInvariant();
            switch (outcome)
            {
                case "rejected":
                    demand.TriageOutcome = DemandTriageDecisions.Rejected;
                    demand.Status = "Rejected";
                    break;
                case "paused":
                    demand.TriageOutcome = DemandTriageDecisions.Paused;
                    demand.Status = "Paused";
                    break;
                default:
                    demand.TriageOutcome = DemandTriageDecisions.Active;
                    demand.Status = "Triaged";
                    break;
            }
        }

        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();

        TempData["Message"] = "Triage outcome recorded.";
        return RedirectToAction(nameof(RequestDetail), new { id, tab = "triage" });
    }

    [HttpPost("request/{id:guid}/scoring/draft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestScoringDraft(
        Guid id,
        string? scoringAnswersJson,
        int? scoreStrategic,
        int? scoreUrgency,
        int? scoreFunding,
        int? scoreRice,
        string? scoringAssessmentNotes,
        string? scoringConcernsNotes)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        await ApplyScoringAsync(demand, scoringAnswersJson, scoreStrategic, scoreUrgency, scoreFunding, scoreRice, scoringAssessmentNotes, scoringConcernsNotes, false);
        TempData["Message"] = "Scoring draft saved.";
        return RedirectToAction(nameof(RequestDetail), new { id, tab = "scoring" });
    }

    [HttpPost("request/{id:guid}/scoring/autosave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestScoringAutoSave(
        Guid id,
        [FromForm] string? scoringAnswersJson,
        [FromForm] int? scoreStrategic,
        [FromForm] int? scoreUrgency,
        [FromForm] int? scoreFunding,
        [FromForm] int? scoreRice,
        [FromForm] string? scoringAssessmentNotes,
        [FromForm] string? scoringConcernsNotes)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        await ApplyScoringAsync(demand, scoringAnswersJson, scoreStrategic, scoreUrgency, scoreFunding, scoreRice, scoringAssessmentNotes, scoringConcernsNotes, false);
        return Json(new { ok = true });
    }

    [HttpPost("request/{id:guid}/scoring/complete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestScoringComplete(
        Guid id,
        string? scoringAnswersJson,
        int? scoreStrategic,
        int? scoreUrgency,
        int? scoreFunding,
        int? scoreRice,
        string? scoringAssessmentNotes,
        string? scoringConcernsNotes,
        bool? confirmScoringComplete)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        if (confirmScoringComplete != true)
        {
            TempData["Error"] = "Confirm the scoring review before finalising.";
            return RedirectToAction(nameof(RequestDetail), new { id, tab = "scoring" });
        }

        if (string.IsNullOrWhiteSpace(scoringAssessmentNotes))
        {
            TempData["Error"] = "Add overall assessment notes before finalising.";
            return RedirectToAction(nameof(RequestDetail), new { id, tab = "scoring" });
        }

        await ApplyScoringAsync(demand, scoringAnswersJson, scoreStrategic, scoreUrgency, scoreFunding, scoreRice, scoringAssessmentNotes, scoringConcernsNotes, true);
        TempData["Message"] = "Scoring finalised.";
        return RedirectToAction(nameof(RequestDetail), new { id, tab = "scoring" });
    }

    [HttpPost("request/{id:guid}/explore/review")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExploreReview(
        Guid id,
        string? command,
        string? exploreRelatedLinksJson,
        string? exploreLinksToExistingWork,
        string? exploreResearchAndInsights,
        string? exploreAimClarification,
        string? explorePolicies,
        string? exploreUserGroups,
        string? exploreFeasibility,
        string? exploreRecommendation,
        string? exploreNotes,
        List<int>? exploreUniversalBarrierIds)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        await SaveExploreFieldsAsync(
            demand,
            exploreRelatedLinksJson,
            exploreLinksToExistingWork,
            exploreResearchAndInsights,
            exploreAimClarification,
            explorePolicies,
            exploreUserGroups,
            exploreFeasibility,
            exploreRecommendation,
            exploreNotes,
            exploreUniversalBarrierIds);

        var cmd = (command ?? string.Empty).Trim().ToLowerInvariant();
        if (cmd == "progress")
        {
            demand.Status = "Scoring";
            TempData["Message"] = "Explore complete. Demand moved to Scoring.";
        }
        else if (cmd == "return")
        {
            demand.Status = "Returned";
            TempData["Message"] = "Demand returned to submitter.";
        }
        else
        {
            TempData["Message"] = "Explore notes saved.";
        }

        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(RequestDetail), new { id, tab = "explore" });
    }

    public sealed class ExploreAutosaveRequest
    {
        public string? ExploreRelatedLinksJson { get; set; }
        public string? ExploreLinksToExistingWork { get; set; }
        public string? ExploreResearchAndInsights { get; set; }
        public string? ExploreAimClarification { get; set; }
        public string? ExplorePolicies { get; set; }
        public string? ExploreUserGroups { get; set; }
        public string? ExploreFeasibility { get; set; }
        public string? ExploreRecommendation { get; set; }
        public string? ExploreNotes { get; set; }
        public List<int>? ExploreUniversalBarrierIds { get; set; }
    }

    [HttpPost("request/{id:guid}/explore/autosave")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExploreAutosave(Guid id, [FromBody] ExploreAutosaveRequest req)
    {
        var demand = await _db.DemandPipelineRequests.FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return Json(new { ok = false, error = "Demand not found" });

        await SaveExploreFieldsAsync(
            demand,
            req.ExploreRelatedLinksJson,
            req.ExploreLinksToExistingWork,
            req.ExploreResearchAndInsights,
            req.ExploreAimClarification,
            req.ExplorePolicies,
            req.ExploreUserGroups,
            req.ExploreFeasibility,
            req.ExploreRecommendation,
            req.ExploreNotes,
            req.ExploreUniversalBarrierIds);

        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();

        return Json(new { ok = true, savedAt = DateTime.Now.ToString("HH:mm") });
    }

    [HttpGet("request/explore/search-work")]
    public async Task<IActionResult> ExploreSearchWork(string q)
    {
        var term = (q ?? string.Empty).Trim();
        if (term.Length < 2) return Json(new { results = Array.Empty<object>() });

        var list = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted &&
                        ((p.Title != null && p.Title.Contains(term)) ||
                         (p.ProjectCode != null && p.ProjectCode.Contains(term))))
            .OrderBy(p => p.Title)
            .Take(12)
            .Select(p => new
            {
                projectId = p.Id,
                projectCode = p.ProjectCode,
                title = p.Title,
                label = string.IsNullOrWhiteSpace(p.ProjectCode) ? p.Title : p.ProjectCode + " - " + p.Title
            })
            .ToListAsync();

        return Json(new { results = list });
    }

    [HttpGet("request/explore/search-services")]
    public async Task<IActionResult> ExploreSearchServices(string q)
    {
        var term = (q ?? string.Empty).Trim();
        if (term.Length < 2) return Json(new { results = Array.Empty<object>() });

        var list = await _db.CMDBProducts.AsNoTracking()
            .Where(s => (s.Title != null && s.Title.Contains(term)) || (s.CMDBID != null && s.CMDBID.Contains(term)))
            .OrderBy(s => s.Title)
            .Take(12)
            .Select(s => new
            {
                cmdbProductId = s.Id,
                serviceId = (int?)null,
                fipsId = s.CMDBID,
                displayName = s.Title,
                label = string.IsNullOrWhiteSpace(s.CMDBID) ? s.Title : s.CMDBID + " - " + s.Title
            })
            .ToListAsync();

        return Json(new { results = list });
    }

    [HttpGet("request/{id:guid}/risk-issue/add")]
    public async Task<IActionResult> RequestAddRiskIssueForm(Guid id, string? entryType)
    {
        var demand = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        var isIssue = string.Equals(entryType, "Issue", StringComparison.OrdinalIgnoreCase);
        ViewBag.DemandRequestId = id;
        ViewBag.DemandReference = demand.Reference ?? "Demand";
        ViewBag.DemandTitle = demand.Title ?? string.Empty;
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";
        ViewBag.DirectorateOptions = await _db.Divisions.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new RiskIssueNamedIntOption { Id = d.Id, Name = d.Name ?? "" })
            .ToListAsync();

        var model = new DemandPipelineRiskIssue
        {
            DemandPipelineRequestId = id,
            EntryType = isIssue ? "Issue" : "Risk"
        };

        return View("~/Views/Modern/Demand/RiskIssueAdd.cshtml", model);
    }

    [HttpPost("request/{id:guid}/risk-issue/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAddRiskIssue(
        Guid id, string? entryType, string? title, string? description,
        string? impactOnDelivery, string? priority, string? tier,
        int? directorateId, int? ownerUserId, DateTime? targetResolutionDate,
        string? mitigationOrAction)
    {
        var demand = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (demand == null) return NotFound();

        var type = string.Equals(entryType, "Issue", StringComparison.OrdinalIgnoreCase) ? "Issue" : "Risk";

        // Validate required fields
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title)) errors.Add("Enter a title.");
        if (string.IsNullOrWhiteSpace(description)) errors.Add("Enter a description.");
        if (string.IsNullOrWhiteSpace(impactOnDelivery)) errors.Add("Enter the impact on delivery.");
        if (!directorateId.HasValue) errors.Add("Select a directorate.");
        if (string.IsNullOrWhiteSpace(mitigationOrAction))
            errors.Add(type == "Issue" ? "Enter an action plan." : "Enter a mitigation plan.");

        if (errors.Count > 0)
        {
            foreach (var err in errors)
                ModelState.AddModelError("", err);

            ViewBag.DemandRequestId = id;
            ViewBag.DemandReference = demand.Reference ?? "Demand";
            ViewBag.DemandTitle = demand.Title ?? string.Empty;
            ViewBag.MainNavSection = "demand";
            ViewBag.SubNavItem = "demand-dashboard";
            ViewBag.DirectorateOptions = await _db.Divisions.AsNoTracking()
                .Where(d => d.IsActive).OrderBy(d => d.Name)
                .Select(d => new RiskIssueNamedIntOption { Id = d.Id, Name = d.Name ?? "" })
                .ToListAsync();

            var formModel = new DemandPipelineRiskIssue
            {
                DemandPipelineRequestId = id,
                EntryType = type,
                Title = title ?? string.Empty,
                Description = description,
                ImpactOnDelivery = impactOnDelivery,
                Priority = priority,
                Tier = tier,
                DirectorateId = directorateId,
                OwnerUserId = ownerUserId,
                TargetResolutionDate = targetResolutionDate,
                MitigationOrAction = mitigationOrAction
            };
            if (ownerUserId.HasValue)
            {
                var owner = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerUserId.Value);
                if (owner != null) { ViewBag.OwnerName = owner.Name; ViewBag.OwnerEmail = owner.Email; }
            }
            return View("~/Views/Modern/Demand/RiskIssueAdd.cshtml", formModel);
        }

        _db.DemandPipelineRiskIssues.Add(new DemandPipelineRiskIssue
        {
            Id = Guid.NewGuid(),
            DemandPipelineRequestId = id,
            EntryType = type,
            Title = title!.Trim(),
            Description = description?.Trim(),
            ImpactOnDelivery = impactOnDelivery?.Trim(),
            Priority = priority,
            Tier = tier,
            DirectorateId = directorateId,
            OwnerUserId = ownerUserId,
            TargetResolutionDate = targetResolutionDate,
            MitigationOrAction = mitigationOrAction?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
            UpdatedBy = User.Identity?.Name
        });

        await _db.SaveChangesAsync();
        TempData["Message"] = type + " added.";
        return RedirectToAction(nameof(RequestDetail), new { id, tab = type == "Issue" ? "issues" : "risks" });
    }

    [HttpGet("request/{id:guid}/risk-issue/{riskIssueId:guid}/edit")]
    public async Task<IActionResult> RequestEditRiskIssue(Guid id, Guid riskIssueId)
    {
        var row = await _db.DemandPipelineRiskIssues.AsNoTracking().FirstOrDefaultAsync(r => r.Id == riskIssueId && r.DemandPipelineRequestId == id);
        if (row == null) return NotFound();

        var demand = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        ViewBag.DemandRequestId = id;
        ViewBag.DemandReference = demand?.Reference ?? "Demand";
        ViewBag.DemandTitle = demand?.Title ?? string.Empty;
        ViewBag.MainNavSection = "demand";
        ViewBag.SubNavItem = "demand-dashboard";
        ViewBag.DirectorateOptions = await _db.Divisions.AsNoTracking()
            .Where(d => d.IsActive).OrderBy(d => d.Name)
            .Select(d => new RiskIssueNamedIntOption { Id = d.Id, Name = d.Name ?? "" })
            .ToListAsync();
        if (row.OwnerUserId.HasValue)
        {
            var owner = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == row.OwnerUserId.Value);
            if (owner != null) { ViewBag.OwnerName = owner.Name; ViewBag.OwnerEmail = owner.Email; }
        }

        return View("~/Views/Modern/Demand/RiskIssueEdit.cshtml", row);
    }

    [HttpPost("request/{id:guid}/risk-issue/{riskIssueId:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestEditRiskIssue(
        Guid id, Guid riskIssueId, string? entryType, string? title, string? description,
        string? impactOnDelivery, string? priority, string? tier,
        int? directorateId, int? ownerUserId, DateTime? targetResolutionDate,
        string? mitigationOrAction)
    {
        var row = await _db.DemandPipelineRiskIssues.FirstOrDefaultAsync(r => r.Id == riskIssueId && r.DemandPipelineRequestId == id);
        if (row == null) return NotFound();

        var type = string.Equals(entryType, "Issue", StringComparison.OrdinalIgnoreCase) ? "Issue" : "Risk";

        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title)) errors.Add("Enter a title.");
        if (string.IsNullOrWhiteSpace(description)) errors.Add("Enter a description.");
        if (string.IsNullOrWhiteSpace(impactOnDelivery)) errors.Add("Enter the impact on delivery.");
        if (!directorateId.HasValue) errors.Add("Select a directorate.");
        if (string.IsNullOrWhiteSpace(mitigationOrAction))
            errors.Add(type == "Issue" ? "Enter an action plan." : "Enter a mitigation plan.");

        if (errors.Count > 0)
        {
            foreach (var err in errors)
                ModelState.AddModelError("", err);

            var demand = await _db.DemandPipelineRequests.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            ViewBag.DemandRequestId = id;
            ViewBag.DemandReference = demand?.Reference ?? "Demand";
            ViewBag.DemandTitle = demand?.Title ?? string.Empty;
            ViewBag.MainNavSection = "demand";
            ViewBag.SubNavItem = "demand-dashboard";
            ViewBag.DirectorateOptions = await _db.Divisions.AsNoTracking()
                .Where(d => d.IsActive).OrderBy(d => d.Name)
                .Select(d => new RiskIssueNamedIntOption { Id = d.Id, Name = d.Name ?? "" })
                .ToListAsync();

            row.EntryType = type;
            row.Title = title ?? string.Empty;
            row.Description = description;
            row.ImpactOnDelivery = impactOnDelivery;
            row.Priority = priority;
            row.Tier = tier;
            row.DirectorateId = directorateId;
            row.OwnerUserId = ownerUserId;
            row.TargetResolutionDate = targetResolutionDate;
            row.MitigationOrAction = mitigationOrAction;
            if (ownerUserId.HasValue)
            {
                var owner = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ownerUserId.Value);
                if (owner != null) { ViewBag.OwnerName = owner.Name; ViewBag.OwnerEmail = owner.Email; }
            }
            return View("~/Views/Modern/Demand/RiskIssueEdit.cshtml", row);
        }

        row.EntryType = type;
        row.Title = title!.Trim();
        row.Description = description?.Trim();
        row.ImpactOnDelivery = impactOnDelivery?.Trim();
        row.Priority = priority;
        row.Tier = tier;
        row.DirectorateId = directorateId;
        row.OwnerUserId = ownerUserId;
        row.TargetResolutionDate = targetResolutionDate;
        row.MitigationOrAction = mitigationOrAction?.Trim();
        row.UpdatedAt = DateTime.UtcNow;
        row.UpdatedBy = User.Identity?.Name;

        await _db.SaveChangesAsync();
        TempData["Message"] = "Risk/issue updated.";
        var editTab = string.Equals(row.EntryType, "Issue", StringComparison.OrdinalIgnoreCase) ? "issues" : "risks";
        return RedirectToAction(nameof(RequestDetail), new { id, tab = editTab });
    }

    [HttpPost("request/{id:guid}/risk-issue/{riskIssueId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDeleteRiskIssue(Guid id, Guid riskIssueId)
    {
        var row = await _db.DemandPipelineRiskIssues.FirstOrDefaultAsync(r => r.Id == riskIssueId && r.DemandPipelineRequestId == id);
        var deleteTab = row != null && string.Equals(row.EntryType, "Issue", StringComparison.OrdinalIgnoreCase) ? "issues" : "risks";
        if (row != null)
        {
            _db.DemandPipelineRiskIssues.Remove(row);
            await _db.SaveChangesAsync();
            TempData["Message"] = "Risk/issue deleted.";
        }

        return RedirectToAction(nameof(RequestDetail), new { id, tab = deleteTab });
    }

    private async Task PopulateRequestDetailViewBagsAsync(DemandPipelineRequest demand)
    {
        var linkedBc = demand.BusinessCaseId.HasValue
            ? await _db.DemandPipelineBusinessCases.AsNoTracking().FirstOrDefaultAsync(b => b.Id == demand.BusinessCaseId.Value)
            : null;
        ViewBag.LinkedBusinessCase = linkedBc;

        // Resolve Priority Outcome IDs to names
        string? priorityOutcomeName = null;
        if (!string.IsNullOrWhiteSpace(demand.PriorityOutcomeIds))
        {
            var poIds = demand.PriorityOutcomeIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : (int?)null)
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (poIds.Count > 0)
            {
                var poNames = await _db.Objectives.AsNoTracking().Where(o => poIds.Contains(o.Id)).Select(o => o.Title).ToListAsync();
                priorityOutcomeName = poNames.Count > 0 ? string.Join(", ", poNames) : demand.PriorityOutcomeIds;
            }
        }
        ViewBag.PriorityOutcomeName = priorityOutcomeName;

        // Resolve Mission Pillar IDs to names
        string? missionPillarName = null;
        if (!string.IsNullOrWhiteSpace(demand.MissionPillarIds))
        {
            var mpIds = demand.MissionPillarIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : (int?)null)
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (mpIds.Count > 0)
            {
                var mpNames = await _db.Missions.AsNoTracking().Where(m => mpIds.Contains(m.Id)).Select(m => m.Title).ToListAsync();
                missionPillarName = mpNames.Count > 0 ? string.Join(", ", mpNames) : demand.MissionPillarIds;
            }
        }
        ViewBag.MissionPillarName = missionPillarName;

        ViewBag.PortfolioName = null;

        var stages = await _db.DemandPipelineStages.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
        if (stages.Count == 0)
            stages = await _db.DemandPipelineStages.AsNoTracking().OrderBy(s => s.DisplayOrder).ToListAsync();
        ViewBag.PipelineStages = stages;
        ViewBag.PipelineStageIndex = StatusToPipelineIndex(demand.Status, stages.Count);

        ViewBag.DemandRiskIssues = await _db.DemandPipelineRiskIssues.AsNoTracking()
            .Where(r => r.DemandPipelineRequestId == demand.Id)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
        ViewBag.DemandCanEditRisksIssues = demand.Status != "Closed - Progressed to delivery" && demand.Status != "Progressed to delivery";

        var answers = DemandScoringFrameworkService.ParseAnswersJson(demand.ScoringAnswersJson)
                      ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var framework = await _demandScoringFramework.LoadActiveFrameworkAsync();
        ViewBag.ScoringPageVm = new DemandScoringFrameworkPageViewModel
        {
            Demand = demand,
            Framework = framework,
            Answers = answers,
            PriorityOutcomeName = ViewBag.PriorityOutcomeName,
            MissionPillarName = ViewBag.MissionPillarName,
            PortfolioName = ViewBag.PortfolioName
        };

        var isClosedForExploreEdit = demand.Status == "Draft"
                         || demand.Status == "Progressed to delivery"
                         || demand.Status == "Closed - Progressed to delivery";
        ViewBag.ExploreCanSaveFields = !isClosedForExploreEdit;
        ViewBag.ExploreCanWorkflow = demand.Status == "ExploratoryReview";

        ViewBag.ExploreRelatedLinksList = ParseRelatedLinks(demand.ExploreRelatedLinksJson);
        var selectedBarrierIds = await _db.DemandPipelineRequestUniversalBarriers.AsNoTracking()
            .Where(x => x.DemandPipelineRequestId == demand.Id)
            .Select(x => x.UniversalBarrierLookupId)
            .ToListAsync();
        ViewBag.SelectedExploreUniversalBarrierIds = selectedBarrierIds;
        ViewBag.ExploreUniversalBarrierOptions = await _db.UniversalBarrierLookups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        ViewBag.SelectedExploreUniversalBarrierRows = await _db.UniversalBarrierLookups.AsNoTracking()
            .Where(x => selectedBarrierIds.Contains(x.Id))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        ViewBag.TriageMeetingsForAssignment = await _db.DemandPipelineTriageMeetings.AsNoTracking()
            .Where(m => m.Status == "Scheduled")
            .OrderBy(m => m.MeetingDate)
            .ThenBy(m => m.StartTime)
            .ToListAsync();
        ViewBag.AssignedTriageMeeting = demand.TriageMeetingId.HasValue
            ? await _db.DemandPipelineTriageMeetings.AsNoTracking().FirstOrDefaultAsync(m => m.Id == demand.TriageMeetingId.Value)
            : null;

        var canRecordOutcome = demand.Status == "Scored"
                               || demand.Status == "TriagePending"
                               || demand.Status == "Triage Pending"
                               || demand.Status == "Triaged";
        ViewBag.CanRecordTriageOutcome = canRecordOutcome;
        ViewBag.BusinessAreasForTriage = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();
        ViewBag.TriageOutcomeStagesForForm = await _db.DemandTriageOutcomeStages.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .ToListAsync();

        if (demand.TriageStageLookupId.HasValue)
        {
            var st = await _db.DemandTriageOutcomeStages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == demand.TriageStageLookupId.Value);
            ViewBag.TriageOutcomeStageLabel = st?.Label;
        }

        if (demand.TriageAssignedBusinessAreaId.HasValue)
        {
            var ba = await _db.BusinessAreaLookups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == demand.TriageAssignedBusinessAreaId.Value);
            ViewBag.TriageAssignedBusinessAreaName = ba?.Name;
        }

        if (demand.TriagePrimaryContactUserId.HasValue)
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == demand.TriagePrimaryContactUserId.Value);
            ViewBag.TriagePrimaryContactDisplay = user != null ? (user.Name + " (" + user.Email + ")") : null;
        }

        if (demand.TriageCreatedProjectId.HasValue)
        {
            var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == demand.TriageCreatedProjectId.Value);
            ViewBag.TriageCreatedProjectId = project?.Id;
            ViewBag.TriageCreatedProjectCode = project?.ProjectCode;
            ViewBag.TriageCreatedProjectTitle = project?.Title;
        }

        ViewBag.WorkflowTransitions = GetWorkflowTransitions(demand.Status);

        if (!string.IsNullOrWhiteSpace(demand.CreatedBy))
        {
            var createdBy = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == demand.CreatedBy || u.Name == demand.CreatedBy);
            ViewBag.CreatedByDisplayName = createdBy?.Name;
        }
    }

    private async Task ApplyScoringAsync(
        DemandPipelineRequest demand,
        string? scoringAnswersJson,
        int? scoreStrategic,
        int? scoreUrgency,
        int? scoreFunding,
        int? scoreRice,
        string? scoringAssessmentNotes,
        string? scoringConcernsNotes,
        bool finalise)
    {
        var framework = await _demandScoringFramework.LoadActiveFrameworkAsync();

        var answers = DemandScoringFrameworkService.ParseAnswersJson(scoringAnswersJson);
        DemandScoringEvaluationResult eval;

        if (answers != null && answers.Count > 0)
        {
            eval = _demandScoringFramework.Evaluate(framework, answers);
            demand.ScoringAnswersJson = scoringAnswersJson;
        }
        else
        {
            eval = _demandScoringFramework.EvaluateFromLegacyInts(framework, scoreStrategic, scoreUrgency, scoreFunding, scoreRice);
        }

        demand.ScoreStrategic = eval.ScoreStrategic;
        demand.ScoreUrgency = eval.ScoreUrgency;
        demand.ScoreFunding = eval.ScoreFunding;
        demand.ScoreRice = eval.ScoreRice;
        demand.TotalScore = eval.Scaled100;
        demand.SuggestedBand = eval.BandCode;
        demand.ScoringAssessmentNotes = string.IsNullOrWhiteSpace(scoringAssessmentNotes) ? null : scoringAssessmentNotes.Trim();
        demand.ScoringConcernsNotes = string.IsNullOrWhiteSpace(scoringConcernsNotes) ? null : scoringConcernsNotes.Trim();

        if (finalise)
        {
            demand.Status = "Scored";
            demand.ScoredAt = DateTime.UtcNow;
            demand.ScoredBy = User.Identity?.Name;
        }
        else if (demand.Status != "Scored")
        {
            demand.Status = "Scoring";
        }

        demand.UpdatedAt = DateTime.UtcNow;
        demand.UpdatedBy = User.Identity?.Name;
        await _db.SaveChangesAsync();
    }

    private async Task SaveExploreFieldsAsync(
        DemandPipelineRequest demand,
        string? exploreRelatedLinksJson,
        string? exploreLinksToExistingWork,
        string? exploreResearchAndInsights,
        string? exploreAimClarification,
        string? explorePolicies,
        string? exploreUserGroups,
        string? exploreFeasibility,
        string? exploreRecommendation,
        string? exploreNotes,
        List<int>? exploreUniversalBarrierIds)
    {
        demand.ExploreRelatedLinksJson = string.IsNullOrWhiteSpace(exploreRelatedLinksJson) ? "[]" : exploreRelatedLinksJson.Trim();
        demand.ExploreLinksToExistingWork = string.IsNullOrWhiteSpace(exploreLinksToExistingWork) ? null : exploreLinksToExistingWork.Trim();
        demand.ExploreResearchAndInsights = string.IsNullOrWhiteSpace(exploreResearchAndInsights) ? null : exploreResearchAndInsights.Trim();
        demand.ExploreAimClarification = string.IsNullOrWhiteSpace(exploreAimClarification) ? null : exploreAimClarification.Trim();
        demand.ExplorePolicies = string.IsNullOrWhiteSpace(explorePolicies) ? null : explorePolicies.Trim();
        demand.ExploreUserGroups = string.IsNullOrWhiteSpace(exploreUserGroups) ? null : exploreUserGroups.Trim();
        demand.ExploreFeasibility = string.IsNullOrWhiteSpace(exploreFeasibility) ? null : exploreFeasibility.Trim();
        demand.ExploreRecommendation = string.IsNullOrWhiteSpace(exploreRecommendation) ? null : exploreRecommendation.Trim();
        demand.ExploreNotes = string.IsNullOrWhiteSpace(exploreNotes) ? null : exploreNotes.Trim();
        demand.ExploreCompletedAt = DateTime.UtcNow;
        demand.ExploreCompletedBy = User.Identity?.Name;

        var selected = (exploreUniversalBarrierIds ?? new List<int>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        var existing = await _db.DemandPipelineRequestUniversalBarriers
            .Where(x => x.DemandPipelineRequestId == demand.Id)
            .ToListAsync();
        _db.DemandPipelineRequestUniversalBarriers.RemoveRange(existing);

        foreach (var bid in selected)
        {
            _db.DemandPipelineRequestUniversalBarriers.Add(new DemandPipelineRequestUniversalBarrier
            {
                DemandPipelineRequestId = demand.Id,
                UniversalBarrierLookupId = bid
            });
        }
    }

    private static List<(string Id, string Label)> GetWorkflowTransitions(string? status)
    {
        return status switch
        {
            "Submitted" or "Active" => new List<(string, string)> { ("to-explore", "Move to Explore"), ("to-returned", "Return to submitter"), ("to-rejected", "Reject") },
            "ExploratoryReview" => new List<(string, string)> { ("to-scoring", "Move to Scoring"), ("to-returned", "Return to submitter"), ("to-rejected", "Reject") },
            "Scoring" => new List<(string, string)> { ("to-scored", "Mark as Scored"), ("to-returned", "Return to submitter") },
            "Scored" => new List<(string, string)> { ("to-triage", "Move to Triage"), ("to-paused", "Pause") },
            "TriagePending" or "Triage Pending" => new List<(string, string)> { ("to-triaged", "Mark Triaged"), ("to-progressed", "Progress to delivery"), ("to-rejected", "Reject"), ("to-paused", "Pause") },
            "Triaged" => new List<(string, string)> { ("to-progressed", "Progress to delivery"), ("to-paused", "Pause") },
            "Returned" => new List<(string, string)> { ("to-explore", "Move to Explore"), ("to-rejected", "Reject") },
            "Paused" => new List<(string, string)> { ("to-explore", "Resume to Explore"), ("to-scoring", "Resume to Scoring") },
            _ => new List<(string, string)>()
        };
    }

    private static int StatusToPipelineIndex(string? status, int stageCount)
    {
        if (stageCount <= 0) return 0;

        var idx = status switch
        {
            "Submitted" or "Active" or "ExploratoryReview" or "Scoring" or "Scored" or "TriagePending" or "Triage Pending" => 3,
            "Triaged" or "Rejected" or "Paused" => 4,
            "Closed" or "Progressed to delivery" or "Closed - Progressed to delivery" => stageCount - 1,
            _ => 3
        };

        if (idx < 0) idx = 0;
        if (idx >= stageCount) idx = stageCount - 1;
        return idx;
    }

    private static List<ExploreRelatedLinkDto> ParseRelatedLinks(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<ExploreRelatedLinkDto>();
        try
        {
            var data = JsonSerializer.Deserialize<List<ExploreRelatedLinkDto>>(json);
            return data ?? new List<ExploreRelatedLinkDto>();
        }
        catch
        {
            return new List<ExploreRelatedLinkDto>();
        }
    }

    private static string BuildProjectCode()
    {
        var stamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
        return ("DEM" + stamp).Substring(0, Math.Min(20, ("DEM" + stamp).Length));
    }
}
