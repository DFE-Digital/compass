using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Authorization;
using Compass.Services;
using Compass.ViewModels.Admin;

namespace Compass.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IApiTokenService _apiTokenService;

    private static readonly IReadOnlyList<RaidLookupDefinition> _raidLookupDefinitions = new List<RaidLookupDefinition>
    {
        CreateLookupDefinition<ActionStatus>("action-statuses", "Action statuses", "Workflow states shown on every action."),
        CreateLookupDefinition<ActionPriority>("action-priorities", "Action priorities", "Priority options shared across action listings."),
        CreateLookupDefinition<ActionType>("action-types", "Action types", "Helps teams categorise actions for reporting."),
        CreateLookupDefinition<ActionCategory>("action-categories", "Action categories", "Used to slice actions by category."),
        CreateLookupDefinition<ActionImpactLevel>("action-impact-levels", "Action impact levels", "Impact level choices aligned with RAID reporting."),
        CreateLookupDefinition<ActionReminderFrequency>("action-reminder-frequencies", "Action reminder frequencies", "Determines how often reminders fire for actions."),
        CreateLookupDefinition<ActionEscalationThreshold>("action-escalation-thresholds", "Action escalation thresholds", "Number of days before escalation is triggered."),
        CreateLookupDefinition<IssueStatus>("issue-statuses", "Issue statuses", "Issue workflow states."),
        CreateLookupDefinition<IssuePriority>("issue-priorities", "Issue priorities", "Priority options for issues."),
        CreateLookupDefinition<IssueSeverity>("issue-severities", "Issue severities", "Severity scale mapped to RAID reporting."),
        CreateLookupDefinition<IssueCategory>("issue-categories", "Issue categories", "Issue categorisation used in dashboards."),
        CreateLookupDefinition<DecisionStatus>("decision-statuses", "Decision statuses", "Status values for decisions."),
        CreateLookupDefinition<DecisionPriority>("decision-priorities", "Decision priorities", "Decision priority labels."),
        CreateLookupDefinition<DecisionOutcome>("decision-outcomes", "Decision outcomes", "Possible outcomes recorded when a decision is made."),
        CreateLookupDefinition<DecisionImplementationStatus>("decision-implementation-statuses", "Decision implementation statuses", "Tracks implementation progress."),
        CreateLookupDefinition<RiskStatus>("risk-statuses", "Risk statuses", "Core risk workflow states."),
        CreateLookupDefinition<RiskPriority>("risk-priorities", "Risk priorities", "Priority scale applied to risks."),
        CreateLookupDefinition<RiskLikelihood>("risk-likelihoods", "Risk likelihoods", "Likelihood scale used to calculate scores."),
        CreateLookupDefinition<RiskImpactLevel>("risk-impact-levels", "Risk impact levels", "Impact scale for risks."),
        CreateLookupDefinition<RiskProximity>("risk-proximities", "Risk proximities", "Timeline bands for when a risk may materialise."),
        CreateLookupDefinition<RiskCategory>("risk-categories", "Risk categories", "Categorisation for risk libraries."),
        CreateLookupDefinition<RaidEvidenceType>("raid-evidence-types", "Evidence types", "Shared evidence/documentation types."),
        CreateLookupDefinition<GovernanceBoard>("governance-boards", "Governance boards", "Committees and boards used for RAID escalation.")
    };

    private static RaidLookupDefinition CreateLookupDefinition<TLookup>(string key, string label, string? description = null)
        where TLookup : RaidLookupBase, new() =>
        new(
            key,
            label,
            ctx => ctx.Set<TLookup>().Cast<RaidLookupBase>(),
            () => new TLookup(),
            description);


    public AdminController(CompassDbContext context, ILogger<AdminController> logger, IApiTokenService apiTokenService)
    {
        _context = context;
        _logger = logger;
        _apiTokenService = apiTokenService;
    }

    // GET: Admin/Index
    public IActionResult Index()
    {
        return View("~/Views/Admin/Index.cshtml");
    }

    // ==================== RAID SETTINGS ====================

    public async Task<IActionResult> RaidSettings(string? lookupKey = null, int? editId = null)
    {
        var descriptor = ResolveRaidLookupDefinition(lookupKey) ?? _raidLookupDefinitions.First();
        var viewModel = await BuildRaidSettingsViewModelAsync(descriptor);

        if (editId.HasValue)
        {
            var entity = await descriptor.Query(_context)
                .FirstOrDefaultAsync(x => x.Id == editId.Value);

            if (entity == null)
            {
                TempData["ErrorMessage"] = "The selected entry could not be found.";
            }
            else
            {
                viewModel.EditEntry = new RaidLookupEditInputModel
                {
                    Id = entity.Id,
                    LookupKey = descriptor.Key,
                    Code = entity.Code,
                    Label = entity.Label,
                    Description = entity.Description,
                    SortOrder = entity.SortOrder,
                    IsActive = entity.IsActive
                };
            }
        }

        return View("~/Views/Admin/Settings/RaidSettings.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRaidLookup([Bind(Prefix = "NewEntry")] RaidLookupEditInputModel input)
    {
        var descriptor = ResolveRaidLookupDefinition(input.LookupKey) ?? _raidLookupDefinitions.First();

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildRaidSettingsViewModelAsync(descriptor, input);
            ViewData["ActiveRaidModal"] = "create";
            return View("~/Views/Admin/Settings/RaidSettings.cshtml", invalidViewModel);
        }

        var entity = descriptor.Factory();

        entity.Code = input.Code.Trim();
        entity.Label = input.Label.Trim();
        entity.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        entity.SortOrder = input.SortOrder;
        entity.IsActive = input.IsActive;
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.Add(entity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Added '{entity.Label}' to {descriptor.Label.ToLowerInvariant()}.";
        return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRaidLookup([Bind(Prefix = "EditEntry")] RaidLookupEditInputModel input)
    {
        if (!input.Id.HasValue)
        {
            TempData["ErrorMessage"] = "Invalid RAID lookup identifier.";
            return RedirectToAction(nameof(RaidSettings));
        }

        var descriptor = ResolveRaidLookupDefinition(input.LookupKey) ?? _raidLookupDefinitions.First();

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildRaidSettingsViewModelAsync(descriptor, null, input);
            ViewData["ActiveRaidModal"] = "edit";
            return View("~/Views/Admin/Settings/RaidSettings.cshtml", invalidViewModel);
        }

        var entity = await descriptor.Query(_context)
            .FirstOrDefaultAsync(x => x.Id == input.Id.Value);

        if (entity == null)
        {
            TempData["ErrorMessage"] = "Unable to find the selected RAID lookup entry.";
            return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
        }

        entity.Code = input.Code.Trim();
        entity.Label = input.Label.Trim();
        entity.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        entity.SortOrder = input.SortOrder;
        entity.IsActive = input.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Updated '{entity.Label}'.";
        return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRaidLookup(string lookupKey, int id)
    {
        var descriptor = ResolveRaidLookupDefinition(lookupKey);
        if (descriptor == null)
        {
            TempData["ErrorMessage"] = "Unknown RAID lookup.";
            return RedirectToAction(nameof(RaidSettings));
        }

        var entity = await descriptor.Query(_context)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
        {
            TempData["ErrorMessage"] = "The selected entry could not be found.";
            return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
        }

        try
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Deleted '{entity.Label}'.";
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Failed to delete RAID lookup {LookupKey} {LookupId}", descriptor.Key, id);
            TempData["ErrorMessage"] = "Unable to delete this entry because it is currently in use.";
        }

        return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedRaidLookupDefaults(string lookupKey)
    {
        var descriptor = ResolveRaidLookupDefinition(lookupKey);
        if (descriptor == null)
        {
            TempData["ErrorMessage"] = "Unknown RAID lookup.";
            return RedirectToAction(nameof(RaidSettings));
        }

        if (!RaidLookupSeedData.TryGetValues(descriptor.Key, out var seeds) || seeds.Count == 0)
        {
            TempData["ErrorMessage"] = "There are no recommended values for this lookup.";
            return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
        }

        var existingCodes = await descriptor.Query(_context)
            .Select(x => x.Code.ToLower())
            .ToListAsync();

        var itemsToAdd = seeds
            .Where(seed => !existingCodes.Contains(seed.Code.ToLowerInvariant()))
            .ToList();

        if (!itemsToAdd.Any())
        {
            TempData["SuccessMessage"] = "All recommended values already exist for this lookup.";
            return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
        }

        foreach (var seed in itemsToAdd)
        {
            var entity = descriptor.Factory();
            entity.Code = seed.Code;
            entity.Label = seed.Label;
            entity.Description = seed.Description;
            entity.SortOrder = seed.SortOrder;
            entity.IsActive = true;
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;

            _context.Add(entity);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Added {itemsToAdd.Count} recommended value{(itemsToAdd.Count == 1 ? string.Empty : "s")}.";
        return RedirectToAction(nameof(RaidSettings), new { lookupKey = descriptor.Key });
    }

    // GET: Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderBy(u => u.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/User/Users.cshtml", users);
    }

    // GET: Admin/CreateUser
    public IActionResult CreateUser()
    {
        return View("~/Views/Admin/User/CreateUser.cshtml");
    }

    // POST: Admin/CreateUser
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(User user)
    {
        if (ModelState.IsValid)
        {
            try
            {
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"User '{user.Name}' has been created successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
            }
        }
        
        return View("~/Views/Admin/User/CreateUser.cshtml", user);
    }

    // ==================== USER SATISFACTION (USS) ADMIN ====================

    public IActionResult UserSatisfaction()
    {
        return View("~/Views/Admin/UserSatisfaction/Index.cshtml");
    }

    public async Task<IActionResult> ResponseScales()
    {
        var scales = await _context.ResponseScales
            .Include(s => s.Options.OrderBy(o => o.Ordinal))
            .OrderBy(s => s.Name)
            .ToListAsync();
        
        ViewBag.Scales = scales;
        return View("~/Views/Admin/UserSatisfaction/ResponseScales.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateResponseScale(string name, string? description, SurveyInputType inputType, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Scale name is required.";
            return RedirectToAction(nameof(ResponseScales));
        }
        
        var scale = new ResponseScale
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            InputType = inputType,
            IsDefault = isDefault,
            CreatedUtc = DateTime.UtcNow
        };
        
        _context.ResponseScales.Add(scale);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Response scale created.";
        return RedirectToAction(nameof(ResponseScales));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddScaleOption(Guid scaleId, string value, string label, int ordinal)
    {
        if (scaleId == Guid.Empty || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(label))
        {
            TempData["ErrorMessage"] = "Scale, value and label are required.";
            return RedirectToAction(nameof(ResponseScales));
        }
        
        _context.ResponseScaleOptions.Add(new ResponseScaleOption
        {
            ResponseScaleId = scaleId,
            Value = value.Trim(),
            Label = label.Trim(),
            Ordinal = ordinal,
            Active = true
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Option added.";
        return RedirectToAction(nameof(ResponseScales));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScaleOption(Guid optionId, string label, int ordinal, bool active)
    {
        var option = await _context.ResponseScaleOptions.FindAsync(optionId);
        if (option == null)
        {
            TempData["ErrorMessage"] = "Option not found.";
            return RedirectToAction(nameof(ResponseScales));
        }
        
        option.Label = label.Trim();
        option.Ordinal = ordinal;
        option.Active = active;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Option updated.";
        return RedirectToAction(nameof(ResponseScales));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteResponseScale(Guid scaleId)
    {
        var scale = await _context.ResponseScales
            .Include(s => s.Options)
            .FirstOrDefaultAsync(s => s.ResponseScaleId == scaleId);
        
        if (scale == null)
        {
            TempData["ErrorMessage"] = "Scale not found.";
            return RedirectToAction(nameof(ResponseScales));
        }
        
        // Check if any questions use this scale
        var questionsUsingScale = await _context.SurveyQuestions
            .AnyAsync(q => q.ResponseScaleId == scaleId);
        
        if (questionsUsingScale)
        {
            TempData["ErrorMessage"] = "Cannot delete scale as it is in use by questions.";
            return RedirectToAction(nameof(ResponseScales));
        }
        
        _context.ResponseScales.Remove(scale);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Scale deleted.";
        return RedirectToAction(nameof(ResponseScales));
    }

    public async Task<IActionResult> UserSatisfactionQuestions()
    {
        // Get or create default template
        var template = await _context.SurveyTemplates
            .OrderByDescending(t => t.IsDefault)
            .ThenByDescending(t => t.CreatedUtc)
            .FirstOrDefaultAsync();
        
        if (template == null)
        {
            // Create default template if none exists
            template = new SurveyTemplate
            {
                Name = "Default USS Template",
                Version = 1,
                IsDefault = true,
                CreatedUtc = DateTime.UtcNow
            };
            _context.SurveyTemplates.Add(template);
            await _context.SaveChangesAsync();
        }
        
        var questions = await _context.SurveyQuestions
            .Include(q => q.ResponseScale)
            .Include(q => q.Options.OrderBy(o => o.Ordinal))
            .Where(q => q.SurveyTemplateId == template.SurveyTemplateId)
            .OrderBy(q => q.Ordinal)
            .ToListAsync();
        
        var scales = await _context.ResponseScales
            .Include(s => s.Options.OrderBy(o => o.Ordinal))
            .OrderBy(s => s.Name)
            .ToListAsync();
        
        ViewBag.Questions = questions;
        ViewBag.SelectedTemplateId = template.SurveyTemplateId;
        ViewBag.ResponseScales = scales;
        
        return View("~/Views/Admin/UserSatisfaction/Questions.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUssTemplate(string name, int version, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Template name is required.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        var template = new SurveyTemplate
        {
            Name = name.Trim(),
            Version = version > 0 ? version : 1,
            IsDefault = isDefault,
            CreatedUtc = DateTime.UtcNow
        };
        _context.SurveyTemplates.Add(template);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Template created.";
        return RedirectToAction(nameof(UserSatisfactionQuestions), new { templateId = template.SurveyTemplateId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUssQuestion(string? templateId, string code, string title, string? description, bool mandatory, int weight, int ordinal, string inputType, string? responseScaleId)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
        {
            TempData["ErrorMessage"] = "Code and title are required.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        
        if (!Enum.TryParse<SurveyInputType>(inputType, true, out var inputTypeEnum))
        {
            TempData["ErrorMessage"] = "Invalid input type.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        
        // Get or create default template
        Guid templateIdGuid;
        if (string.IsNullOrWhiteSpace(templateId) || !Guid.TryParse(templateId, out templateIdGuid) || templateIdGuid == Guid.Empty)
        {
            var template = await _context.SurveyTemplates
                .OrderByDescending(t => t.IsDefault)
                .ThenByDescending(t => t.CreatedUtc)
                .FirstOrDefaultAsync();
            
            if (template == null)
            {
                // Create default template if none exists
                template = new SurveyTemplate
                {
                    Name = "Default USS Template",
                    Version = 1,
                    IsDefault = true,
                    CreatedUtc = DateTime.UtcNow
                };
                _context.SurveyTemplates.Add(template);
                await _context.SaveChangesAsync();
            }
            templateIdGuid = template.SurveyTemplateId;
        }
        
        Guid? responseScaleGuid = null;
        if (!string.IsNullOrWhiteSpace(responseScaleId) && Guid.TryParse(responseScaleId, out var parsedScaleId) && parsedScaleId != Guid.Empty)
        {
            responseScaleGuid = parsedScaleId;
        }
        
        var question = new SurveyQuestion
        {
            SurveyTemplateId = templateIdGuid,
            Code = code.Trim(),
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Mandatory = mandatory,
            Weight = weight,
            Ordinal = ordinal,
            InputType = inputTypeEnum,
            ResponseScaleId = responseScaleGuid,
            Active = true
        };
        _context.SurveyQuestions.Add(question);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Question added.";
        
        // If input type is Select, redirect with question ID so options can be added
        if (inputTypeEnum == SurveyInputType.Select)
        {
            TempData["NewQuestionId"] = question.SurveyQuestionId.ToString();
        }
        
        return RedirectToAction(nameof(UserSatisfactionQuestions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUssQuestion(Guid questionId, string title, string? description, bool mandatory, int weight, int ordinal, SurveyInputType inputType, bool active, string? responseScaleId)
    {
        var q = await _context.SurveyQuestions.FindAsync(questionId);
        if (q == null)
        {
            TempData["ErrorMessage"] = "Question not found.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        q.Title = title.Trim();
        q.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        q.Mandatory = mandatory;
        q.Weight = weight;
        q.Ordinal = ordinal;
        q.InputType = inputType;
        q.Active = active;
        
        if (!string.IsNullOrWhiteSpace(responseScaleId) && Guid.TryParse(responseScaleId, out var parsedScaleId) && parsedScaleId != Guid.Empty)
        {
            q.ResponseScaleId = parsedScaleId;
        }
        else
        {
            q.ResponseScaleId = null;
        }
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Question updated.";
        return RedirectToAction(nameof(UserSatisfactionQuestions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestionOption(Guid questionId, string value, string label, int ordinal, int? score)
    {
        var question = await _context.SurveyQuestions.FindAsync(questionId);
        if (question == null)
        {
            TempData["ErrorMessage"] = "Question not found.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(label))
        {
            TempData["ErrorMessage"] = "Value and label are required.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        
        _context.SurveyOptions.Add(new SurveyOption
        {
            SurveyQuestionId = questionId,
            Value = value.Trim(),
            Label = label.Trim(),
            Ordinal = ordinal,
            Score = score,
            Active = true
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Option added.";
        return RedirectToAction(nameof(UserSatisfactionQuestions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestionOption(Guid optionId, string label, int ordinal, int? score, bool active)
    {
        var option = await _context.SurveyOptions.FindAsync(optionId);
        if (option == null)
        {
            TempData["ErrorMessage"] = "Option not found.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        
        option.Label = label.Trim();
        option.Ordinal = ordinal;
        option.Score = score;
        option.Active = active;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Option updated.";
        return RedirectToAction(nameof(UserSatisfactionQuestions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestionOption(Guid optionId)
    {
        var option = await _context.SurveyOptions.FindAsync(optionId);
        if (option == null)
        {
            TempData["ErrorMessage"] = "Option not found.";
            return RedirectToAction(nameof(UserSatisfactionQuestions));
        }
        
        _context.SurveyOptions.Remove(option);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Option deleted.";
        return RedirectToAction(nameof(UserSatisfactionQuestions));
    }

    public async Task<IActionResult> UserSatisfactionResponses(string? fipsId = null, DateTime? from = null, DateTime? to = null)
    {
        ViewBag.Services = await _context.Services.OrderBy(s => s.FipsId).ToListAsync();
        var query = _context.SurveyResponses
            .Include(r => r.SurveyInstance)
            .ThenInclude(si => si.Service)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(fipsId))
        {
            query = query.Where(r => r.SurveyInstance!.Service!.FipsId == fipsId);
        }
        if (from.HasValue)
        {
            query = query.Where(r => r.SubmittedUtc >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(r => r.SubmittedUtc <= to.Value);
        }

        var list = await query.OrderByDescending(r => r.SubmittedUtc).Take(200).ToListAsync();
        var n = list.Count;
        var avg = n > 0 ? Math.Round(list.Average(r => (double)r.UssComputed), 1) : 0;
        var median = n > 0 ? Math.Round(list.Select(r => (double)r.UssComputed).OrderBy(x => x).ElementAt(n / 2), 1) : 0;
        ViewBag.Summary = new { n, avg, median };
        return View("~/Views/Admin/UserSatisfaction/Responses.cshtml", list);
    }

    // GET: Admin/EditUser/5
    public async Task<IActionResult> EditUser(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/User/EditUser.cshtml", user);
    }

    // POST: Admin/EditUser/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, User user)
    {
        if (id != user.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                
                _context.Update(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"User '{user.Name}' has been updated successfully.";
                return RedirectToAction(nameof(Users));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                ModelState.AddModelError("", "An error occurred while updating the user. Please try again.");
            }
        }
        
        return View("~/Views/Admin/User/EditUser.cshtml", user);
    }

    // GET: Admin/DeleteUser/5
    public async Task<IActionResult> DeleteUser(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/User/DeleteUser.cshtml", user);
    }

    // POST: Admin/DeleteUser/5
    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"User '{user.Name}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            TempData["ErrorMessage"] = "An error occurred while deleting the user. Please try again.";
        }

        return RedirectToAction(nameof(Users));
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }

    // ==================== STRATEGIC OBJECTIVES ====================

    // GET: Admin/Objectives
    public async Task<IActionResult> Objectives()
    {
        var objectives = await _context.Objectives
            .Include(o => o.OwnerUser)
            .Include(o => o.ThemeSroUser)
            .Include(o => o.OutcomeSroUser)
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        ViewBag.Users = new SelectList(
            await _context.Users.OrderBy(u => u.Name).ToListAsync(),
            "Id",
            "Name"
        );
        
        return View("~/Views/Admin/Objective/Index.cshtml", objectives);
    }

    // GET: Admin/ObjectiveDetails/5
    public async Task<IActionResult> ObjectiveDetails(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var objective = await _context.Objectives
            .Include(o => o.OwnerUser)
            .Include(o => o.ThemeSroUser)
            .Include(o => o.OutcomeSroUser)
            .Include(o => o.Risks.Where(r => !r.IsDeleted))
            .Include(o => o.Issues.Where(i => !i.IsDeleted))
            .Include(o => o.Milestones.Where(m => !m.IsDeleted))
            .Include(o => o.Actions.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (objective == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Objective/Details.cshtml", objective);
    }

    // GET: Admin/CreateObjective
    public async Task<IActionResult> CreateObjective()
    {
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        return View("~/Views/Admin/Objective/Create.cshtml");
    }

    // POST: Admin/CreateObjective
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateObjective([Bind("Title,Theme,Description,OwnerUserId,ThemeSroUserId,OutcomeSroUserId,Status")] Objective objective)
    {
        if (ModelState.IsValid)
        {
            try
            {
                objective.CreatedAt = DateTime.UtcNow;
                objective.UpdatedAt = DateTime.UtcNow;
                objective.IsDeleted = false;
                
                _context.Add(objective);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Priority outcome '{objective.Title}' has been created successfully.";
                return RedirectToAction(nameof(Objectives));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating objective");
                ModelState.AddModelError("", "An error occurred while creating the objective. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        return View("~/Views/Admin/Objective/Create.cshtml", objective);
    }

    // GET: Admin/EditObjective/5
    public async Task<IActionResult> EditObjective(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var objective = await _context.Objectives.FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
        if (objective == null)
        {
            return NotFound();
        }

        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        return View("~/Views/Admin/Objective/Edit.cshtml", objective);
    }

    // POST: Admin/EditObjective/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditObjective(int id, [Bind("Id,Title,Theme,Description,OwnerUserId,ThemeSroUserId,OutcomeSroUserId,Status")] Objective objective)
    {
        if (id != objective.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingObjective = await _context.Objectives.FindAsync(id);
                if (existingObjective == null || existingObjective.IsDeleted)
                {
                    return NotFound();
                }

                existingObjective.Title = objective.Title;
                existingObjective.Theme = objective.Theme;
                existingObjective.Description = objective.Description;
                existingObjective.OwnerUserId = objective.OwnerUserId;
                existingObjective.ThemeSroUserId = objective.ThemeSroUserId;
                existingObjective.OutcomeSroUserId = objective.OutcomeSroUserId;
                existingObjective.Status = objective.Status;
                existingObjective.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Priority outcome '{objective.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Objectives));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ObjectiveExists(objective.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating objective");
                ModelState.AddModelError("", "An error occurred while updating the objective. Please try again.");
            }
        }
        
        ViewBag.Users = new SelectList(await _context.Users.OrderBy(u => u.Name).ToListAsync(), "Id", "Name");
        return View("~/Views/Admin/Objective/Edit.cshtml", objective);
    }

    // GET: Admin/DeleteObjective/5
    public async Task<IActionResult> DeleteObjective(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var objective = await _context.Objectives
            .Include(o => o.OwnerUser)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            
        if (objective == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Objective/Delete.cshtml", objective);
    }

    // POST: Admin/DeleteObjective/5
    [HttpPost, ActionName("DeleteObjective")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteObjectiveConfirmed(int id)
    {
        try
        {
            var objective = await _context.Objectives
                .Include(o => o.Risks)
                .Include(o => o.Issues)
                .Include(o => o.Milestones)
                .Include(o => o.Actions)
                .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);
                
            if (objective == null)
            {
                TempData["ErrorMessage"] = "Priority outcome not found.";
                return RedirectToAction(nameof(Objectives));
            }

            // Check for related items
            var relatedItemsCount = objective.Risks.Count + objective.Issues.Count + 
                                   objective.Milestones.Count + objective.Actions.Count;
                                   
            if (relatedItemsCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{objective.Title}' because it has {relatedItemsCount} related item(s). Please remove or reassign all related items before deleting.";
                return RedirectToAction(nameof(ObjectiveDetails), new { id = id });
            }

            objective.IsDeleted = true;
            objective.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"Priority outcome '{objective.Title}' has been deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting objective");
            TempData["ErrorMessage"] = "An error occurred while deleting the objective. Please try again.";
        }
        
        return RedirectToAction(nameof(Objectives));
    }

    private bool ObjectiveExists(int id)
    {
        return _context.Objectives.Any(e => e.Id == id && !e.IsDeleted);
    }

    // ========================================
    // SETTINGS
    // ========================================

    // GET: Admin/Settings
    public async Task<IActionResult> Settings()
    {
        // Load all lookup data for tabbed interface
        ViewBag.RiskTypes = await _context.RiskTypes.OrderBy(rt => rt.Name).ToListAsync();
        ViewBag.RiskTiers = await _context.RiskTiers.OrderBy(rt => rt.SortOrder).ThenBy(rt => rt.Name).ToListAsync();
        ViewBag.ActionSources = await _context.ActionSources.OrderBy(a_s => a_s.SortOrder).ThenBy(a_s => a_s.Name).ToListAsync();
        ViewBag.WcagCriteria = await _context.WcagCriteria.OrderBy(w => w.Criterion).ToListAsync();
        ViewBag.BusinessAreas = await _context.BusinessAreaLookups.OrderBy(ba => ba.SortOrder).ThenBy(ba => ba.Name).ToListAsync();
        ViewBag.Phases = await _context.PhaseLookups.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToListAsync();
        ViewBag.GddRoles = await _context.GddRoles.OrderBy(r => r.RoleFamily).ThenBy(r => r.RoleName).ThenBy(r => r.RoleLevel).ToListAsync();
        ViewBag.Skills = await _context.Skills.OrderBy(s => s.SkillName).ToListAsync();
        ViewBag.KpiCategories = await _context.KpiCategories.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync();
        
        return View("~/Views/Admin/Settings/Index.cshtml");
    }

    // ========================================
    // SETTINGS - KPI Categories
    // ========================================

    public async Task<IActionResult> KpiCategories()
    {
        var categories = await _context.KpiCategories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        return View("~/Views/Admin/Settings/KpiCategories.cshtml", categories);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateKpiCategory([Bind("Name,Code,Description,SortOrder,IsActive")] KpiCategory category)
    {
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            TempData["ErrorMessage"] = "Name is required.";
            return RedirectToAction(nameof(KpiCategories));
        }

        try
        {
            category.Name = category.Name.Trim();
            category.Code = SanitiseKpiCategoryCode(category.Code, category.Name);
            category.Description = string.IsNullOrWhiteSpace(category.Description) ? null : category.Description.Trim();
            category.SortOrder = await NormaliseKpiCategorySortOrderAsync(category.SortOrder);
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            var codeExists = await _context.KpiCategories.AnyAsync(c => c.Code == category.Code);
            if (codeExists)
            {
                TempData["ErrorMessage"] = $"A KPI category with code '{category.Code}' already exists.";
                return RedirectToAction(nameof(KpiCategories));
            }

            _context.KpiCategories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"KPI category '{category.Name}' created.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI category");
            TempData["ErrorMessage"] = "An error occurred while creating the KPI category.";
        }

        return RedirectToAction(nameof(KpiCategories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateKpiCategory(int id, string name, string? code, string? description, int sortOrder, bool isActive)
    {
        var category = await _context.KpiCategories.FindAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "KPI category not found.";
            return RedirectToAction(nameof(KpiCategories));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Name is required.";
            return RedirectToAction(nameof(KpiCategories));
        }

        try
        {
            var normalisedCode = SanitiseKpiCategoryCode(code, name);
            var duplicate = await _context.KpiCategories.AnyAsync(c => c.Code == normalisedCode && c.Id != id);
            if (duplicate)
            {
                TempData["ErrorMessage"] = $"A KPI category with code '{normalisedCode}' already exists.";
                return RedirectToAction(nameof(KpiCategories));
            }

            category.Name = name.Trim();
            category.Code = normalisedCode;
            category.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            if (sortOrder > 0)
            {
                category.SortOrder = sortOrder;
            }
            else if (category.SortOrder == 0)
            {
                category.SortOrder = await NormaliseKpiCategorySortOrderAsync(sortOrder);
            }
            category.IsActive = isActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"KPI category '{category.Name}' updated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI category {KpiCategoryId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the KPI category.";
        }

        return RedirectToAction(nameof(KpiCategories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteKpiCategory(int id)
    {
        var category = await _context.KpiCategories.FindAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "KPI category not found.";
            return RedirectToAction(nameof(KpiCategories));
        }

        try
        {
            var codePrefix = $"{category.Code}-";
            var kpiUsage = await _context.Kpis.CountAsync(k => k.Code != null && k.Code.StartsWith(codePrefix));
            if (kpiUsage > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{category.Name}' because it is used by {kpiUsage} KPI(s). Consider deactivating it instead.";
                return RedirectToAction(nameof(KpiCategories));
            }

            _context.KpiCategories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"KPI category '{category.Name}' deleted.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KPI category {KpiCategoryId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the KPI category.";
        }

        return RedirectToAction(nameof(KpiCategories));
    }

    // ========================================
    // SETTINGS - Risk Types
    // ========================================

    // GET: Admin/RiskTypes
    public async Task<IActionResult> RiskTypes()
    {
        var riskTypes = await _context.RiskTypes
            .OrderBy(rt => rt.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/RiskTypes.cshtml", riskTypes);
    }

    // GET: Admin/CreateRiskType
    public IActionResult CreateRiskType()
    {
        return View("~/Views/Admin/Settings/CreateRiskType.cshtml");
    }

    // POST: Admin/CreateRiskType
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRiskType([Bind("Code,Name,Description,Summary,IsActive")] RiskType riskType)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists
                if (await _context.RiskTypes.AnyAsync(rt => rt.Code == riskType.Code))
                {
                    ModelState.AddModelError("Code", "A risk type with this code already exists.");
                }
                else
                {
                    riskType.CreatedAt = DateTime.UtcNow;
                    riskType.UpdatedAt = DateTime.UtcNow;
                    _context.Add(riskType);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk type '{riskType.Name}' has been created successfully.";
                    return RedirectToAction(nameof(RiskTypes));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating risk type");
                ModelState.AddModelError("", "An error occurred while creating the risk type. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateRiskType.cshtml", riskType);
    }

    // GET: Admin/EditRiskType/5
    public async Task<IActionResult> EditRiskType(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskType = await _context.RiskTypes.FindAsync(id);
        if (riskType == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditRiskType.cshtml", riskType);
    }

    // POST: Admin/EditRiskType/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRiskType(int id, [Bind("Id,Code,Name,Description,Summary,IsActive")] RiskType riskType)
    {
        if (id != riskType.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists for a different record
                if (await _context.RiskTypes.AnyAsync(rt => rt.Code == riskType.Code && rt.Id != id))
                {
                    ModelState.AddModelError("Code", "A risk type with this code already exists.");
                }
                else
                {
                    var existingRiskType = await _context.RiskTypes.FindAsync(id);
                    if (existingRiskType == null)
                    {
                        return NotFound();
                    }

                    existingRiskType.Code = riskType.Code;
                    existingRiskType.Name = riskType.Name;
                    existingRiskType.Description = riskType.Description;
                    existingRiskType.Summary = riskType.Summary;
                    existingRiskType.IsActive = riskType.IsActive;
                    existingRiskType.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk type '{riskType.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(RiskTypes));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RiskTypeExists(riskType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating risk type");
                ModelState.AddModelError("", "An error occurred while updating the risk type. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/EditRiskType.cshtml", riskType);
    }

    // GET: Admin/DeleteRiskType/5
    public async Task<IActionResult> DeleteRiskType(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskType = await _context.RiskTypes.FindAsync(id);
        if (riskType == null)
        {
            return NotFound();
        }

        // Check if any risks are using this type
        var riskCount = await _context.RiskRiskTypes.CountAsync(rrt => rrt.RiskTypeId == id);
        ViewBag.RiskCount = riskCount;

        return View("~/Views/Admin/Settings/DeleteRiskType.cshtml", riskType);
    }

    // POST: Admin/DeleteRiskType/5
    [HttpPost, ActionName("DeleteRiskType")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRiskTypeConfirmed(int id)
    {
        try
        {
            var riskType = await _context.RiskTypes.FindAsync(id);
            if (riskType != null)
            {
                // Check if any risks are using this type
                var riskCount = await _context.RiskRiskTypes.CountAsync(rrt => rrt.RiskTypeId == id);
                if (riskCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete risk type '{riskType.Name}' as it is being used by {riskCount} risk(s). Please reassign those risks first.";
                }
                else
                {
                    _context.RiskTypes.Remove(riskType);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk type '{riskType.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting risk type");
            TempData["ErrorMessage"] = "An error occurred while deleting the risk type. Please try again.";
        }
        
        return RedirectToAction(nameof(RiskTypes));
    }

    private bool RiskTypeExists(int id)
    {
        return _context.RiskTypes.Any(e => e.Id == id);
    }

    // ========================================
    // SETTINGS - Risk Tiers
    // ========================================

    // GET: Admin/RiskTiers
    public async Task<IActionResult> RiskTiers()
    {
        var riskTiers = await _context.RiskTiers
            .OrderBy(rt => rt.SortOrder)
            .ThenBy(rt => rt.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/RiskTiers.cshtml", riskTiers);
    }

    // GET: Admin/CreateRiskTier
    public IActionResult CreateRiskTier()
    {
        return View("~/Views/Admin/Settings/CreateRiskTier.cshtml");
    }

    // POST: Admin/CreateRiskTier
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRiskTier([Bind("Code,Name,Description,Summary,SortOrder,IsActive")] RiskTier riskTier)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists
                if (await _context.RiskTiers.AnyAsync(rt => rt.Code == riskTier.Code))
                {
                    ModelState.AddModelError("Code", "A risk tier with this code already exists.");
                }
                else
                {
                    riskTier.CreatedAt = DateTime.UtcNow;
                    riskTier.UpdatedAt = DateTime.UtcNow;
                    _context.Add(riskTier);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk tier '{riskTier.Name}' has been created successfully.";
                    return RedirectToAction(nameof(RiskTiers));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating risk tier");
                ModelState.AddModelError("", "An error occurred while creating the risk tier. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateRiskTier.cshtml", riskTier);
    }

    // GET: Admin/EditRiskTier/5
    public async Task<IActionResult> EditRiskTier(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskTier = await _context.RiskTiers.FindAsync(id);
        if (riskTier == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditRiskTier.cshtml", riskTier);
    }

    // POST: Admin/EditRiskTier/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRiskTier(int id, [Bind("Id,Code,Name,Description,Summary,SortOrder,IsActive")] RiskTier riskTier)
    {
        if (id != riskTier.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists for a different record
                if (await _context.RiskTiers.AnyAsync(rt => rt.Code == riskTier.Code && rt.Id != id))
                {
                    ModelState.AddModelError("Code", "A risk tier with this code already exists.");
                }
                else
                {
                    var existingRiskTier = await _context.RiskTiers.FindAsync(id);
                    if (existingRiskTier == null)
                    {
                        return NotFound();
                    }

                    existingRiskTier.Code = riskTier.Code;
                    existingRiskTier.Name = riskTier.Name;
                    existingRiskTier.Description = riskTier.Description;
                    existingRiskTier.Summary = riskTier.Summary;
                    existingRiskTier.SortOrder = riskTier.SortOrder;
                    existingRiskTier.IsActive = riskTier.IsActive;
                    existingRiskTier.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk tier '{riskTier.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(RiskTiers));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RiskTierExists(riskTier.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating risk tier");
                ModelState.AddModelError("", "An error occurred while updating the risk tier. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/EditRiskTier.cshtml", riskTier);
    }

    // GET: Admin/DeleteRiskTier/5
    public async Task<IActionResult> DeleteRiskTier(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var riskTier = await _context.RiskTiers.FindAsync(id);
        if (riskTier == null)
        {
            return NotFound();
        }

        // Check if any risks are using this tier
        var riskCount = await _context.Risks.CountAsync(r => r.RiskTierId == id && !r.IsDeleted);
        ViewBag.RiskCount = riskCount;

        return View("~/Views/Admin/Settings/DeleteRiskTier.cshtml", riskTier);
    }

    // POST: Admin/DeleteRiskTier/5
    [HttpPost, ActionName("DeleteRiskTier")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRiskTierConfirmed(int id)
    {
        try
        {
            var riskTier = await _context.RiskTiers.FindAsync(id);
            if (riskTier != null)
            {
                // Check if any risks are using this tier
                var riskCount = await _context.Risks.CountAsync(r => r.RiskTierId == id && !r.IsDeleted);
                if (riskCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete risk tier '{riskTier.Name}' as it is being used by {riskCount} risk(s). Please reassign those risks first.";
                }
                else
                {
                    _context.RiskTiers.Remove(riskTier);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Risk tier '{riskTier.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting risk tier");
            TempData["ErrorMessage"] = "An error occurred while deleting the risk tier. Please try again.";
        }
        
        return RedirectToAction(nameof(RiskTiers));
    }

    private bool RiskTierExists(int id)
    {
        return _context.RiskTiers.Any(e => e.Id == id);
    }

    private static string SanitiseKpiCategoryCode(string? value, string? fallbackName)
    {
        var source = string.IsNullOrWhiteSpace(value) ? fallbackName : value;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "KPI";
        }

        var filtered = new string(source.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(filtered) ? "KPI" : filtered;
    }

    private async Task<int> NormaliseKpiCategorySortOrderAsync(int sortOrder)
    {
        if (sortOrder > 0)
        {
            return sortOrder;
        }

        var maxSortOrder = await _context.KpiCategories.Select(c => (int?)c.SortOrder).MaxAsync() ?? 0;
        return maxSortOrder + 10;
    }

    // ========================================
    // SETTINGS - Action Sources
    // ========================================

    // GET: Admin/ActionSources
    public async Task<IActionResult> ActionSources()
    {
        var actionSources = await _context.ActionSources
            .OrderBy(a_s => a_s.SortOrder)
            .ThenBy(a_s => a_s.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/ActionSources.cshtml", actionSources);
    }

    // GET: Admin/DeliveryPriorities
    public async Task<IActionResult> DeliveryPriorities()
    {
        var priorities = await _context.DeliveryPriorities
            .OrderBy(dp => dp.SortOrder)
            .ThenBy(dp => dp.Name)
            .ToListAsync();

        return View("~/Views/Admin/Settings/DeliveryPriorities.cshtml", priorities);
    }

    // POST: Admin/CreateDeliveryPriority
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeliveryPriority([Bind("Name,Summary,Description,SortOrder,IsActive")] DeliveryPriority deliveryPriority)
    {
        if (ModelState.IsValid)
        {
            var normalisedName = deliveryPriority.Name.Trim();
            if (await _context.DeliveryPriorities
                    .AnyAsync(dp => dp.Name.ToLower() == normalisedName.ToLower()))
            {
                ModelState.AddModelError("Name", "A delivery priority with this name already exists.");
            }
            else
            {
                deliveryPriority.Name = normalisedName;
                deliveryPriority.CreatedAt = DateTime.UtcNow;
                deliveryPriority.UpdatedAt = DateTime.UtcNow;

                if (deliveryPriority.SortOrder == 0)
                {
                    var nextSortOrder = await _context.DeliveryPriorities
                        .Select(dp => (int?)dp.SortOrder)
                        .MaxAsync() ?? 0;
                    deliveryPriority.SortOrder = nextSortOrder + 1;
                }

                _context.DeliveryPriorities.Add(deliveryPriority);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Delivery priority '{deliveryPriority.Name}' has been created.";
                return RedirectToAction(nameof(DeliveryPriorities));
            }
        }

        TempData["ErrorMessage"] = "Unable to create delivery priority. Please fix the errors and try again.";
        return await DeliveryPriorities();
    }

    // POST: Admin/EditDeliveryPriority
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeliveryPriority(int id, [Bind("Id,Name,Summary,Description,SortOrder,IsActive")] DeliveryPriority deliveryPriority)
    {
        if (id != deliveryPriority.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var existingPriority = await _context.DeliveryPriorities.FindAsync(id);
            if (existingPriority == null)
            {
                return NotFound();
            }

            var normalisedName = deliveryPriority.Name.Trim();
            var duplicateExists = await _context.DeliveryPriorities
                .AnyAsync(dp => dp.Id != id && dp.Name.ToLower() == normalisedName.ToLower());
            if (duplicateExists)
            {
                ModelState.AddModelError("Name", "A delivery priority with this name already exists.");
            }
            else
            {
                existingPriority.Name = normalisedName;
                existingPriority.Summary = deliveryPriority.Summary;
                existingPriority.Description = deliveryPriority.Description;
                existingPriority.SortOrder = deliveryPriority.SortOrder;
                existingPriority.IsActive = deliveryPriority.IsActive;
                existingPriority.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Delivery priority '{existingPriority.Name}' has been updated.";
                return RedirectToAction(nameof(DeliveryPriorities));
            }
        }

        TempData["ErrorMessage"] = "Unable to update delivery priority. Please fix the errors and try again.";
        return await DeliveryPriorities();
    }

    // GET: Admin/CreateActionSource
    public IActionResult CreateActionSource()
    {
        return View("~/Views/Admin/Settings/CreateActionSource.cshtml");
    }

    // POST: Admin/CreateActionSource
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateActionSource([Bind("Code,Name,Description,Summary,SortOrder,IsActive")] ActionSource actionSource)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.ActionSources.AnyAsync(a_s => a_s.Code == actionSource.Code))
                {
                    ModelState.AddModelError("Code", "An action source with this code already exists.");
                }
                else
                {
                    actionSource.CreatedAt = DateTime.UtcNow;
                    actionSource.UpdatedAt = DateTime.UtcNow;
                    _context.Add(actionSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Action source '{actionSource.Name}' has been created successfully.";
                    return RedirectToAction(nameof(ActionSources));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action source");
                ModelState.AddModelError("", "An error occurred while creating the action source. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateActionSource.cshtml", actionSource);
    }

    // GET: Admin/EditActionSource/5
    public async Task<IActionResult> EditActionSource(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actionSource = await _context.ActionSources.FindAsync(id);
        if (actionSource == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditActionSource.cshtml", actionSource);
    }

    // POST: Admin/EditActionSource/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditActionSource(int id, [Bind("Id,Code,Name,Description,Summary,SortOrder,IsActive")] ActionSource actionSource)
    {
        if (id != actionSource.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.ActionSources.AnyAsync(a_s => a_s.Code == actionSource.Code && a_s.Id != id))
                {
                    ModelState.AddModelError("Code", "An action source with this code already exists.");
                }
                else
                {
                    var existingActionSource = await _context.ActionSources.FindAsync(id);
                    if (existingActionSource == null)
                    {
                        return NotFound();
                    }

                    existingActionSource.Code = actionSource.Code;
                    existingActionSource.Name = actionSource.Name;
                    existingActionSource.Description = actionSource.Description;
                    existingActionSource.Summary = actionSource.Summary;
                    existingActionSource.SortOrder = actionSource.SortOrder;
                    existingActionSource.IsActive = actionSource.IsActive;
                    existingActionSource.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Action source '{actionSource.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(ActionSources));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActionSourceExists(actionSource.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating action source");
                ModelState.AddModelError("", "An error occurred while updating the action source. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/EditActionSource.cshtml", actionSource);
    }

    // GET: Admin/DeleteActionSource/5
    public async Task<IActionResult> DeleteActionSource(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var actionSource = await _context.ActionSources.FindAsync(id);
        if (actionSource == null)
        {
            return NotFound();
        }

        var actionCount = await _context.Actions.CountAsync(a => a.ActionSourceId == id && !a.IsDeleted);
        ViewBag.ActionCount = actionCount;

        return View("~/Views/Admin/Settings/DeleteActionSource.cshtml", actionSource);
    }

    // POST: Admin/DeleteActionSource/5
    [HttpPost, ActionName("DeleteActionSource")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteActionSourceConfirmed(int id)
    {
        try
        {
            var actionSource = await _context.ActionSources.FindAsync(id);
            if (actionSource != null)
            {
                var actionCount = await _context.Actions.CountAsync(a => a.ActionSourceId == id && !a.IsDeleted);
                if (actionCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete action source '{actionSource.Name}' as it is being used by {actionCount} action(s). Please reassign those actions first.";
                }
                else
                {
                    _context.ActionSources.Remove(actionSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Action source '{actionSource.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting action source");
            TempData["ErrorMessage"] = "An error occurred while deleting the action source. Please try again.";
        }
        
        return RedirectToAction(nameof(ActionSources));
    }

    private bool ActionSourceExists(int id)
    {
        return _context.ActionSources.Any(e => e.Id == id);
    }

    // API Token Management

    public async Task<IActionResult> ApiTokens()
    {
        var tokens = await _apiTokenService.GetAllTokensAsync();
        return View("~/Views/Admin/ApiTokens/Index.cshtml", tokens);
    }

    public IActionResult CreateApiToken()
    {
        return View("~/Views/Admin/ApiTokens/Create.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateApiToken(string name, string? description, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Token name is required.";
            return RedirectToAction(nameof(CreateApiToken));
        }

        try
        {
            var userEmail = User.Identity?.Name ?? "unknown";
            var token = await _apiTokenService.CreateTokenAsync(name, description ?? string.Empty, userEmail, expiresAt);
            
            TempData["SuccessMessage"] = "API token created successfully. Make sure to copy the token now - you won't be able to see it again!";
            TempData["NewToken"] = token.Token;
            
            return RedirectToAction(nameof(ConfigurePermissions), new { id = token.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API token");
            TempData["ErrorMessage"] = "An error occurred while creating the API token.";
            return RedirectToAction(nameof(CreateApiToken));
        }
    }

    public async Task<IActionResult> ConfigurePermissions(int id)
    {
        var token = await _apiTokenService.GetByIdAsync(id);
        if (token == null)
        {
            TempData["ErrorMessage"] = "API token not found.";
            return RedirectToAction(nameof(ApiTokens));
        }

        var permissions = await _apiTokenService.GetPermissionsAsync(id);

        var resources = new[] { "Risks", "Issues", "Actions", "Milestones", "PerformanceMetrics", "EnterpriseMetrics", "FunctionalStandards", "AccessibilityIssues", "SurveysAdmin", "UserSatisfactionQuestions", "UserSatisfactionResponses" };
        
        ViewBag.Token = token;
        ViewBag.Permissions = permissions;
        ViewBag.Resources = resources;

        return View("~/Views/Admin/ApiTokens/ConfigurePermissions.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(int id, Dictionary<string, string> permissions)
    {
        try
        {
            var permissionsDict = new Dictionary<string, (bool read, bool create, bool update, bool delete)>();

            foreach (var resource in new[] { "Risks", "Issues", "Actions", "Milestones", "PerformanceMetrics", "EnterpriseMetrics", "FunctionalStandards", "AccessibilityIssues", "SurveysAdmin", "UserSatisfactionQuestions", "UserSatisfactionResponses" })
            {
                var read = permissions.ContainsKey($"{resource}_read") && permissions[$"{resource}_read"] == "on";
                var create = permissions.ContainsKey($"{resource}_create") && permissions[$"{resource}_create"] == "on";
                var update = permissions.ContainsKey($"{resource}_update") && permissions[$"{resource}_update"] == "on";
                var delete = permissions.ContainsKey($"{resource}_delete") && permissions[$"{resource}_delete"] == "on";

                if (read || create || update || delete)
                {
                    permissionsDict[resource] = (read, create, update, delete);
                }
            }

            await _apiTokenService.SetPermissionsAsync(id, permissionsDict);

            TempData["SuccessMessage"] = "Permissions updated successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving API token permissions");
            TempData["ErrorMessage"] = "An error occurred while saving permissions.";
        }

        return RedirectToAction(nameof(ConfigurePermissions), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecycleApiToken(int id)
    {
        try
        {
            var token = await _apiTokenService.GetByIdAsync(id);
            if (token == null)
            {
                TempData["ErrorMessage"] = "API token not found.";
                return RedirectToAction(nameof(ApiTokens));
            }

            // Generate new token value
            var newToken = await _apiTokenService.RecycleTokenAsync(id);
            
            TempData["SuccessMessage"] = "API token recycled successfully. Make sure to copy the new token now - you won't be able to see it again!";
            TempData["NewToken"] = newToken;
            
            return RedirectToAction(nameof(ConfigurePermissions), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recycling API token");
            TempData["ErrorMessage"] = "An error occurred while recycling the token.";
            return RedirectToAction(nameof(ConfigurePermissions), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleApiToken(int id)
    {
        try
        {
            var token = await _apiTokenService.GetByIdAsync(id);
            if (token != null)
            {
                var newStatus = !token.IsActive;
                await _apiTokenService.UpdateTokenStatusAsync(id, newStatus);
                TempData["SuccessMessage"] = $"API token {(newStatus ? "activated" : "suspended")} successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling API token status");
            TempData["ErrorMessage"] = "An error occurred while updating the token status.";
        }

        // Check if we came from ConfigurePermissions
        var referer = Request.Headers["Referer"].ToString();
        if (referer.Contains("ConfigurePermissions"))
        {
            return RedirectToAction(nameof(ConfigurePermissions), new { id });
        }

        return RedirectToAction(nameof(ApiTokens));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteApiToken(int id)
    {
        try
        {
            await _apiTokenService.DeleteTokenAsync(id);
            TempData["SuccessMessage"] = "API token deleted successfully.";
            return RedirectToAction(nameof(ApiTokens));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API token");
            TempData["ErrorMessage"] = "An error occurred while deleting the token.";
            
            // Check if we came from ConfigurePermissions
            var referer = Request.Headers["Referer"].ToString();
            if (referer.Contains("ConfigurePermissions"))
            {
                return RedirectToAction(nameof(ConfigurePermissions), new { id });
            }
            
            return RedirectToAction(nameof(ApiTokens));
        }
    }

    public async Task<IActionResult> ApiLogs(int? tokenId = null)
    {
        var query = _context.ApiRequestLogs
            .Include(l => l.ApiToken)
            .OrderByDescending(l => l.RequestTimestamp)
            .AsQueryable();

        if (tokenId.HasValue)
        {
            query = query.Where(l => l.ApiTokenId == tokenId.Value);
        }

        var logs = await query.Take(1000).ToListAsync();

        ViewBag.Tokens = await _apiTokenService.GetAllTokensAsync();
        ViewBag.SelectedTokenId = tokenId;

        return View("~/Views/Admin/ApiTokens/Logs.cshtml", logs);
    }

    // ========================================
    // MISSIONS MANAGEMENT
    // ========================================

    // GET: Admin/Missions
    public async Task<IActionResult> Missions()
    {
        var missions = await _context.Missions
            .Include(m => m.OwnerUser)
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
        
        return View("~/Views/Admin/Mission/Index.cshtml", missions);
    }

    // GET: Admin/CreateMission
    public async Task<IActionResult> CreateMission()
    {
        ViewBag.OwnerUsers = await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();
        
        return View("~/Views/Admin/Mission/Create.cshtml");
    }

    // POST: Admin/CreateMission
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMission([Bind("Title,Description,Theme,OwnerUserId,StartDate,EndDate,Status")] Mission mission)
    {
        if (ModelState.IsValid)
        {
            try
            {
                mission.CreatedAt = DateTime.UtcNow;
                mission.UpdatedAt = DateTime.UtcNow;
                _context.Add(mission);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Mission '{mission.Title}' has been created successfully.";
                return RedirectToAction(nameof(Missions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission");
                TempData["ErrorMessage"] = "An error occurred while creating the mission. Please try again.";
            }
        }

        ViewBag.OwnerUsers = await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();
        
        return View("~/Views/Admin/Mission/Create.cshtml", mission);
    }

    // GET: Admin/EditMission/5
    public async Task<IActionResult> EditMission(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var mission = await _context.Missions.FindAsync(id);
        if (mission == null || mission.IsDeleted)
        {
            return NotFound();
        }

        ViewBag.OwnerUsers = await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();

        return View("~/Views/Admin/Mission/Edit.cshtml", mission);
    }

    // POST: Admin/EditMission/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMission(int id, [Bind("Id,Title,Description,Theme,OwnerUserId,StartDate,EndDate,Status,CreatedAt")] Mission mission)
    {
        if (id != mission.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                mission.UpdatedAt = DateTime.UtcNow;
                _context.Update(mission);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Mission '{mission.Title}' has been updated successfully.";
                return RedirectToAction(nameof(Missions));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission");
                TempData["ErrorMessage"] = "An error occurred while updating the mission. Please try again.";
            }
        }

        ViewBag.OwnerUsers = await _context.Users
            .OrderBy(u => u.Name)
            .Select(u => new { u.Id, u.Name })
            .ToListAsync();

        return View("~/Views/Admin/Mission/Edit.cshtml", mission);
    }

    // POST: Admin/DeleteMission/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMission(int id)
    {
        try
        {
            var mission = await _context.Missions.FindAsync(id);
            if (mission != null)
            {
                mission.IsDeleted = true;
                mission.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Mission '{mission.Title}' has been deleted successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mission");
            TempData["ErrorMessage"] = "An error occurred while deleting the mission. Please try again.";
        }

        return RedirectToAction(nameof(Missions));
    }

    // ========================================
    // FUNDING SOURCES MANAGEMENT
    // ========================================

    // GET: Admin/FundingSources
    public async Task<IActionResult> FundingSources()
    {
        var fundingSources = await _context.FundingSources
            .OrderBy(fs => fs.SortOrder)
            .ThenBy(fs => fs.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/FundingSource/Index.cshtml", fundingSources);
    }

    // GET: Admin/CreateFundingSource
    public IActionResult CreateFundingSource()
    {
        return View("~/Views/Admin/FundingSource/Create.cshtml");
    }

    // POST: Admin/CreateFundingSource
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFundingSource([Bind("Code,Name,Description,SortOrder,IsActive")] FundingSource fundingSource)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists
                if (await _context.FundingSources.AnyAsync(fs => fs.Code == fundingSource.Code))
                {
                    ModelState.AddModelError("Code", "A funding source with this code already exists.");
                }
                else
                {
                    fundingSource.CreatedAt = DateTime.UtcNow;
                    fundingSource.UpdatedAt = DateTime.UtcNow;
                    _context.Add(fundingSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Funding source '{fundingSource.Name}' has been created successfully.";
                    return RedirectToAction(nameof(FundingSources));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating funding source");
                TempData["ErrorMessage"] = "An error occurred while creating the funding source. Please try again.";
            }
        }

        return View("~/Views/Admin/FundingSource/Create.cshtml", fundingSource);
    }

    // GET: Admin/EditFundingSource/5
    public async Task<IActionResult> EditFundingSource(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var fundingSource = await _context.FundingSources.FindAsync(id);
        if (fundingSource == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/FundingSource/Edit.cshtml", fundingSource);
    }

    // POST: Admin/EditFundingSource/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditFundingSource(int id, [Bind("Id,Code,Name,Description,SortOrder,IsActive,CreatedAt")] FundingSource fundingSource)
    {
        if (id != fundingSource.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Check if code already exists (excluding current record)
                if (await _context.FundingSources.AnyAsync(fs => fs.Code == fundingSource.Code && fs.Id != id))
                {
                    ModelState.AddModelError("Code", "A funding source with this code already exists.");
                }
                else
                {
                    fundingSource.UpdatedAt = DateTime.UtcNow;
                    _context.Update(fundingSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Funding source '{fundingSource.Name}' has been updated successfully.";
                    return RedirectToAction(nameof(FundingSources));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating funding source");
                TempData["ErrorMessage"] = "An error occurred while updating the funding source. Please try again.";
            }
        }

        return View("~/Views/Admin/FundingSource/Edit.cshtml", fundingSource);
    }

    // POST: Admin/DeleteFundingSource/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFundingSource(int id)
    {
        try
        {
            var fundingSource = await _context.FundingSources.FindAsync(id);
            if (fundingSource != null)
            {
                // Check if any projects are using this funding source
                var projectsUsingSource = await _context.ProjectFundingAllocations.AnyAsync(pfa => pfa.FundingSourceId == id);
                if (projectsUsingSource)
                {
                    TempData["ErrorMessage"] = $"Cannot delete funding source '{fundingSource.Name}' because it is being used by one or more projects.";
                }
                else
                {
                    _context.FundingSources.Remove(fundingSource);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Funding source '{fundingSource.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting funding source");
            TempData["ErrorMessage"] = "An error occurred while deleting the funding source. Please try again.";
        }

        return RedirectToAction(nameof(FundingSources));
    }

    // ========================================
    // SETTINGS - WCAG Criteria
    // ========================================

    // GET: Admin/WcagCriteria
    public async Task<IActionResult> WcagCriteria()
    {
        var wcagCriteria = await _context.WcagCriteria
            .OrderBy(w => w.Criterion)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/WcagCriteria.cshtml", wcagCriteria);
    }

    // GET: Admin/CreateWcagCriterion
    public IActionResult CreateWcagCriterion()
    {
        return View("~/Views/Admin/Settings/CreateWcagCriterion.cshtml");
    }

    // POST: Admin/CreateWcagCriterion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateWcagCriterion([Bind("Criterion,Title,Description,Url,Level,Version,SortOrder,IsActive")] WcagCriterion wcagCriterion)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.WcagCriteria.AnyAsync(w => w.Criterion == wcagCriterion.Criterion && w.Version == wcagCriterion.Version))
                {
                    ModelState.AddModelError("Criterion", "A WCAG criterion with this reference and version already exists.");
                }
                else
                {
                    wcagCriterion.CreatedAt = DateTime.UtcNow;
                    wcagCriterion.UpdatedAt = DateTime.UtcNow;
                    _context.Add(wcagCriterion);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"WCAG criterion '{wcagCriterion.Criterion} - {wcagCriterion.Title}' has been created successfully.";
                    return RedirectToAction(nameof(WcagCriteria));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating WCAG criterion");
                ModelState.AddModelError("", "An error occurred while creating the WCAG criterion. Please try again.");
            }
        }
        
        return View("~/Views/Admin/Settings/CreateWcagCriterion.cshtml", wcagCriterion);
    }

    // GET: Admin/EditWcagCriterion/5
    public async Task<IActionResult> EditWcagCriterion(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wcagCriterion = await _context.WcagCriteria.FindAsync(id);
        if (wcagCriterion == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditWcagCriterion.cshtml", wcagCriterion);
    }

    // POST: Admin/EditWcagCriterion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditWcagCriterion(int id, [Bind("Id,Criterion,Title,Description,Url,Level,Version,SortOrder,IsActive")] WcagCriterion wcagCriterion)
    {
        if (id != wcagCriterion.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (await _context.WcagCriteria.AnyAsync(w => w.Criterion == wcagCriterion.Criterion && w.Version == wcagCriterion.Version && w.Id != id))
                {
                    ModelState.AddModelError("Criterion", "A WCAG criterion with this reference and version already exists.");
                }
                else
                {
                    var existingCriterion = await _context.WcagCriteria.FindAsync(id);
                    if (existingCriterion == null)
                    {
                        return NotFound();
                    }

                    existingCriterion.Criterion = wcagCriterion.Criterion;
                    existingCriterion.Title = wcagCriterion.Title;
                    existingCriterion.Description = wcagCriterion.Description;
                    existingCriterion.Url = wcagCriterion.Url;
                    existingCriterion.Level = wcagCriterion.Level;
                    existingCriterion.Version = wcagCriterion.Version;
                    existingCriterion.SortOrder = wcagCriterion.SortOrder;
                    existingCriterion.IsActive = wcagCriterion.IsActive;
                    existingCriterion.UpdatedAt = DateTime.UtcNow;
                    
                    _context.Update(existingCriterion);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"WCAG criterion '{wcagCriterion.Criterion} - {wcagCriterion.Title}' has been updated successfully.";
                    return RedirectToAction(nameof(WcagCriteria));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating WCAG criterion");
                TempData["ErrorMessage"] = "An error occurred while updating the WCAG criterion. Please try again.";
            }
        }

        return View("~/Views/Admin/Settings/EditWcagCriterion.cshtml", wcagCriterion);
    }

    // GET: Admin/DeleteWcagCriterion/5
    public async Task<IActionResult> DeleteWcagCriterion(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var wcagCriterion = await _context.WcagCriteria.FindAsync(id);
        if (wcagCriterion == null)
        {
            return NotFound();
        }

        // Check if any issues are using this criterion
        var issueCount = await _context.IssueWcagCriteria.CountAsync(iwc => iwc.WcagCriterionId == id);
        ViewBag.IssueCount = issueCount;

        return View("~/Views/Admin/Settings/DeleteWcagCriterion.cshtml", wcagCriterion);
    }

    // POST: Admin/DeleteWcagCriterion/5
    [HttpPost, ActionName("DeleteWcagCriterion")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteWcagCriterionConfirmed(int id)
    {
        try
        {
            var wcagCriterion = await _context.WcagCriteria.FindAsync(id);
            if (wcagCriterion != null)
            {
                // Check if any issues are using this criterion
                var issuesUsingCriterion = await _context.IssueWcagCriteria.AnyAsync(iwc => iwc.WcagCriterionId == id);
                if (issuesUsingCriterion)
                {
                    TempData["ErrorMessage"] = $"Cannot delete WCAG criterion '{wcagCriterion.Criterion} - {wcagCriterion.Title}' because it is being used by one or more accessibility issues.";
                }
                else
                {
                    _context.WcagCriteria.Remove(wcagCriterion);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"WCAG criterion '{wcagCriterion.Criterion} - {wcagCriterion.Title}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting WCAG criterion");
            TempData["ErrorMessage"] = "An error occurred while deleting the WCAG criterion. Please try again.";
        }

        return RedirectToAction(nameof(WcagCriteria));
    }

    // GET: Admin/SearchWcagCriteria (for autocomplete)
    [HttpGet]
    public async Task<IActionResult> SearchWcagCriteria(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(new { results = new object[0] });
        }

        var criteria = await _context.WcagCriteria
            .Where(w => w.IsActive && 
                       (w.Criterion.Contains(q) || 
                        w.Title.Contains(q)))
            .OrderBy(w => w.Criterion)
            .Take(20)
            .Select(w => new
            {
                id = w.Id,
                criterion = w.Criterion,
                title = w.Title,
                level = w.Level,
                version = w.Version,
                text = $"{w.Criterion} - {w.Title} (Level {w.Level})"
            })
            .ToListAsync();

        return Json(new { results = criteria });
    }

    // ========================================
    // SETTINGS - Business Areas
    // ========================================

    // GET: Admin/BusinessAreas
    public async Task<IActionResult> BusinessAreas()
    {
        var businessAreas = await _context.BusinessAreaLookups
            .OrderBy(ba => ba.SortOrder)
            .ThenBy(ba => ba.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/BusinessAreas.cshtml", businessAreas);
    }

    // POST: Admin/CreateBusinessArea
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBusinessArea([Bind("Name,Description,SortOrder,IsActive")] BusinessAreaLookup businessArea)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if name already exists
                if (await _context.BusinessAreaLookups.AnyAsync(ba => ba.Name == businessArea.Name))
                {
                    TempData["ErrorMessage"] = "A business area with this name already exists.";
                }
                else
                {
                    businessArea.CreatedAt = DateTime.UtcNow;
                    businessArea.UpdatedAt = DateTime.UtcNow;
                    _context.Add(businessArea);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Business area '{businessArea.Name}' has been created successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating business area");
                TempData["ErrorMessage"] = "An error occurred while creating the business area. Please try again.";
            }
        }

        return RedirectToAction(nameof(BusinessAreas));
    }

    // POST: Admin/EditBusinessArea
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditBusinessArea(int id, [Bind("Id,Name,Description,SortOrder,IsActive")] BusinessAreaLookup businessArea)
    {
        if (id != businessArea.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Check if name already exists for a different record
                if (await _context.BusinessAreaLookups.AnyAsync(ba => ba.Name == businessArea.Name && ba.Id != id))
                {
                    TempData["ErrorMessage"] = "A business area with this name already exists.";
                }
                else
                {
                    var existingBusinessArea = await _context.BusinessAreaLookups.FindAsync(id);
                    if (existingBusinessArea == null)
                    {
                        return NotFound();
                    }

                    existingBusinessArea.Name = businessArea.Name;
                    existingBusinessArea.Description = businessArea.Description;
                    existingBusinessArea.SortOrder = businessArea.SortOrder;
                    existingBusinessArea.IsActive = businessArea.IsActive;
                    existingBusinessArea.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Business area '{businessArea.Name}' has been updated successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating business area");
                TempData["ErrorMessage"] = "An error occurred while updating the business area. Please try again.";
            }
        }

        return RedirectToAction(nameof(BusinessAreas));
    }

    // POST: Admin/DeleteBusinessArea
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBusinessArea(int id)
    {
        try
        {
            var businessArea = await _context.BusinessAreaLookups.FindAsync(id);
            if (businessArea != null)
            {
                // Check if any projects are using this business area
                var projectCount = await _context.Projects.CountAsync(p => p.BusinessArea == businessArea.Name && !p.IsDeleted);
                if (projectCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete business area '{businessArea.Name}' as it is being used by {projectCount} project(s).";
                }
                else
                {
                    _context.BusinessAreaLookups.Remove(businessArea);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Business area '{businessArea.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting business area");
            TempData["ErrorMessage"] = "An error occurred while deleting the business area. Please try again.";
        }

        return RedirectToAction(nameof(BusinessAreas));
    }

    // ========================================
    // SETTINGS - Phases
    // ========================================

    // GET: Admin/Phases
    public async Task<IActionResult> Phases()
    {
        var phases = await _context.PhaseLookups
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/Phases.cshtml", phases);
    }

    // POST: Admin/CreatePhase
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreatePhase([Bind("Name,Description,SortOrder,IsActive")] PhaseLookup phase)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Check if name already exists
                if (await _context.PhaseLookups.AnyAsync(p => p.Name == phase.Name))
                {
                    TempData["ErrorMessage"] = "A phase with this name already exists.";
                }
                else
                {
                    phase.CreatedAt = DateTime.UtcNow;
                    phase.UpdatedAt = DateTime.UtcNow;
                    _context.Add(phase);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Phase '{phase.Name}' has been created successfully.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating phase");
                TempData["ErrorMessage"] = "An error occurred while creating the phase. Please try again.";
            }
        }

        return RedirectToAction(nameof(Phases));
    }

    // POST: Admin/EditPhase
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPhase(int id, string name, string? description, int sortOrder, bool isActive = false)
    {
        _logger.LogInformation("EditPhase POST called - ID: {Id}, Name: {Name}, Description: {Description}, SortOrder: {SortOrder}, IsActive: {IsActive}", 
            id, name, description, sortOrder, isActive);
        
        // Also check form values directly if model binding failed
        var formId = Request.Form["id"].ToString();
        var formName = Request.Form["name"].ToString();
        var formDescription = Request.Form["description"].ToString();
        var formSortOrder = Request.Form["sortOrder"].ToString();
        var formIsActive = Request.Form["isActive"].ToString();
        
        _logger.LogInformation("Form values - id: {FormId}, name: {FormName}, description: {FormDescription}, sortOrder: {FormSortOrder}, isActive: {FormIsActive}", 
            formId, formName, formDescription, formSortOrder, formIsActive);
        
        try
        {
            // Use form values if model binding didn't work
            if (id == 0 && !string.IsNullOrEmpty(formId) && int.TryParse(formId, out int parsedId))
            {
                id = parsedId;
            }
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(formName))
            {
                name = formName;
            }
            if (string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(formDescription))
            {
                description = formDescription;
            }
            if (sortOrder == 0 && !string.IsNullOrEmpty(formSortOrder) && int.TryParse(formSortOrder, out int parsedSortOrder))
            {
                sortOrder = parsedSortOrder;
            }
            if (!string.IsNullOrEmpty(formIsActive))
            {
                isActive = formIsActive == "true" || formIsActive.Contains("true");
            }
            
            var existingPhase = await _context.PhaseLookups.FindAsync(id);
            if (existingPhase == null)
            {
                _logger.LogWarning("Phase not found with ID: {Id}", id);
                TempData["ErrorMessage"] = "Phase not found.";
                return RedirectToAction(nameof(Phases));
            }

            // Check if name already exists for a different record
            if (await _context.PhaseLookups.AnyAsync(p => p.Name == name && p.Id != id))
            {
                TempData["ErrorMessage"] = "A phase with this name already exists.";
            }
            else
            {
                _logger.LogInformation("Updating phase {Id} - Name: {Name}, IsActive: {IsActive}", id, name, isActive);
                
                existingPhase.Name = name;
                existingPhase.Description = description;
                existingPhase.SortOrder = sortOrder;
                existingPhase.IsActive = isActive;
                existingPhase.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Phase '{name}' has been updated successfully.";
                _logger.LogInformation("Phase {Id} updated successfully", id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating phase {PhaseId}", id);
            TempData["ErrorMessage"] = "An error occurred while updating the phase. Please try again.";
        }

        return RedirectToAction(nameof(Settings));
    }

    // POST: Admin/DeletePhase
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhase(int id)
    {
        try
        {
            var phase = await _context.PhaseLookups.FindAsync(id);
            if (phase != null)
            {
                // Check if any projects are using this phase
                var projectCount = await _context.Projects.CountAsync(p => p.Phase == phase.Name && !p.IsDeleted);
                if (projectCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete phase '{phase.Name}' as it is being used by {projectCount} project(s).";
                }
                else
                {
                    _context.PhaseLookups.Remove(phase);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Phase '{phase.Name}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting phase");
            TempData["ErrorMessage"] = "An error occurred while deleting the phase. Please try again.";
        }

        return RedirectToAction(nameof(Phases));
    }

    // ========================================
    // SETTINGS - GDD Roles
    // ========================================

    // GET: Admin/GddRoles
    public async Task<IActionResult> GddRoles()
    {
        var roles = await _context.GddRoles
            .OrderBy(r => r.RoleFamily)
            .ThenBy(r => r.SortOrder)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/GddRoles.cshtml", roles);
    }

    // GET: Admin/CreateGddRole
    public IActionResult CreateGddRole()
    {
        return View("~/Views/Admin/Settings/CreateGddRole.cshtml");
    }

    // POST: Admin/CreateGddRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGddRole([Bind("RoleFamily,RoleName,RoleLevel,Description,DisplayName,IsActive,SortOrder")] GddRole role)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.GddRoles.Add(role);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"GDD Role '{role.DisplayName}' has been created successfully.";
                _logger.LogInformation("GDD Role created: {DisplayName}", role.DisplayName);
                return RedirectToAction(nameof(GddRoles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating GDD role");
                TempData["ErrorMessage"] = "An error occurred while creating the GDD role. Please try again.";
            }
        }

        return View("~/Views/Admin/Settings/CreateGddRole.cshtml", role);
    }

    // GET: Admin/EditGddRole/5
    public async Task<IActionResult> EditGddRole(int id)
    {
        var role = await _context.GddRoles.FindAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditGddRole.cshtml", role);
    }

    // POST: Admin/EditGddRole/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditGddRole(int id, [Bind("Id,RoleFamily,RoleName,RoleLevel,Description,DisplayName,IsActive,SortOrder")] GddRole role)
    {
        if (id != role.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingRole = await _context.GddRoles.FindAsync(id);
                if (existingRole != null)
                {
                    existingRole.RoleFamily = role.RoleFamily;
                    existingRole.RoleName = role.RoleName;
                    existingRole.RoleLevel = role.RoleLevel;
                    existingRole.Description = role.Description;
                    existingRole.DisplayName = role.DisplayName;
                    existingRole.IsActive = role.IsActive;
                    existingRole.SortOrder = role.SortOrder;
                    existingRole.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"GDD Role '{role.DisplayName}' has been updated successfully.";
                    _logger.LogInformation("GDD Role {Id} updated successfully", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating GDD role {RoleId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the GDD role. Please try again.";
            }

            return RedirectToAction(nameof(GddRoles));
        }

        return View("~/Views/Admin/Settings/EditGddRole.cshtml", role);
    }

    // POST: Admin/DeleteGddRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGddRole(int id)
    {
        try
        {
            var role = await _context.GddRoles.FindAsync(id);
            if (role != null)
            {
                // Check if any staff role returns are using this role
                var usageCount = await _context.StaffRoleReturns.CountAsync(srr => srr.GddRoleId == id);
                if (usageCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete GDD role '{role.DisplayName}' as it is being used by {usageCount} staff role return(s).";
                }
                else
                {
                    _context.GddRoles.Remove(role);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"GDD Role '{role.DisplayName}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting GDD role");
            TempData["ErrorMessage"] = "An error occurred while deleting the GDD role. Please try again.";
        }

        return RedirectToAction(nameof(GddRoles));
    }

    // ========================================
    // SETTINGS - Skills
    // ========================================

    // GET: Admin/Skills
    public async Task<IActionResult> Skills()
    {
        var skills = await _context.Skills
            .OrderBy(s => s.SkillName)
            .ToListAsync();
        
        return View("~/Views/Admin/Settings/Skills.cshtml", skills);
    }

    // GET: Admin/CreateSkill
    public IActionResult CreateSkill()
    {
        return View("~/Views/Admin/Settings/CreateSkill.cshtml");
    }

    // POST: Admin/CreateSkill
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSkill([Bind("SkillName,Description,Category,IsActive,SortOrder")] Skill skill)
    {
        if (ModelState.IsValid)
        {
            try
            {
                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Skill '{skill.SkillName}' has been created successfully.";
                _logger.LogInformation("Skill created: {SkillName}", skill.SkillName);
                return RedirectToAction(nameof(Skills));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating skill");
                TempData["ErrorMessage"] = "An error occurred while creating the skill. Please try again.";
            }
        }

        return View("~/Views/Admin/Settings/CreateSkill.cshtml", skill);
    }

    // GET: Admin/EditSkill/5
    public async Task<IActionResult> EditSkill(int id)
    {
        var skill = await _context.Skills.FindAsync(id);
        if (skill == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/Settings/EditSkill.cshtml", skill);
    }

    // POST: Admin/EditSkill/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSkill(int id, [Bind("Id,SkillName,Description,Category,IsActive,SortOrder")] Skill skill)
    {
        if (id != skill.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingSkill = await _context.Skills.FindAsync(id);
                if (existingSkill != null)
                {
                    existingSkill.SkillName = skill.SkillName;
                    existingSkill.Description = skill.Description;
                    existingSkill.Category = skill.Category;
                    existingSkill.IsActive = skill.IsActive;
                    existingSkill.SortOrder = skill.SortOrder;
                    existingSkill.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Skill '{skill.SkillName}' has been updated successfully.";
                    _logger.LogInformation("Skill {Id} updated successfully", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating skill {SkillId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating the skill. Please try again.";
            }

            return RedirectToAction(nameof(Skills));
        }

        return View("~/Views/Admin/Settings/EditSkill.cshtml", skill);
    }

    // POST: Admin/DeleteSkill
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        try
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill != null)
            {
                // Check if any staff role returns are using this skill
                var usageCount = await _context.StaffRoleReturnSkills.CountAsync(srs => srs.SkillId == id);
                if (usageCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete skill '{skill.SkillName}' as it is being used by {usageCount} staff role return(s).";
                }
                else
                {
                    _context.Skills.Remove(skill);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = $"Skill '{skill.SkillName}' has been deleted successfully.";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill");
            TempData["ErrorMessage"] = "An error occurred while deleting the skill. Please try again.";
        }

        return RedirectToAction(nameof(Skills));
    }

    // ========================================
    // DATA MANAGEMENT
    // ========================================

    // GET: Admin/ClearPerformanceReturns
    [HttpGet]
    public IActionResult ClearPerformanceReturns()
    {
        return View("~/Views/Admin/ClearPerformanceReturns.cshtml");
    }

    // POST: Admin/ClearPerformanceReturns
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearPerformanceReturnsConfirmed()
    {
        try
        {
            _logger.LogWarning("Starting to clear all performance returns - initiated by {User}", User.Identity?.Name);

            // Count before deletion
            var returnsCount = await _context.ProductReturns.CountAsync();
            var valuesCount = await _context.ProductMetricValues.CountAsync();

            _logger.LogInformation("Deleting {ReturnsCount} ProductReturns and {ValuesCount} ProductMetricValues", returnsCount, valuesCount);

            // Delete all product metric values first (they reference ProductReturns)
            _context.ProductMetricValues.RemoveRange(_context.ProductMetricValues);
            await _context.SaveChangesAsync();

            // Delete all product returns
            _context.ProductReturns.RemoveRange(_context.ProductReturns);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Successfully cleared all performance returns - {ReturnsCount} returns and {ValuesCount} values deleted", returnsCount, valuesCount);

            TempData["SuccessMessage"] = $"Successfully cleared {returnsCount} performance returns and {valuesCount} metric values. System will now start from October 2025.";
            return RedirectToAction("Index", "PerformanceMetric");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing performance returns");
            TempData["ErrorMessage"] = "An error occurred while clearing performance returns. Please try again.";
            return RedirectToAction(nameof(ClearPerformanceReturns));
        }
    }

    // DEMAND MANAGEMENT TRIAGE MEETINGS

    public async Task<IActionResult> TriageMeetings()
    {
        var meetings = await _context.TriageMeetings
            .Include(tm => tm.DemandRequests)
            .OrderByDescending(tm => tm.StartAt)
            .ToListAsync();

        return View("~/Views/Admin/TriageMeeting/Index.cshtml", meetings);
    }

    public IActionResult CreateTriageMeeting()
    {
        var model = new TriageMeeting
        {
            StartAt = DateTime.UtcNow.Date.AddHours(9),
            EndAt = DateTime.UtcNow.Date.AddHours(10)
        };

        return View("~/Views/Admin/TriageMeeting/Create.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTriageMeeting(TriageMeeting model)
    {
        if (model.EndAt <= model.StartAt)
        {
            ModelState.AddModelError(nameof(model.EndAt), "End time must be after the start time.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/TriageMeeting/Create.cshtml", model);
        }

        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;

        _context.TriageMeetings.Add(model);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Triage meeting created.";
        return RedirectToAction(nameof(TriageMeetings));
    }

    public async Task<IActionResult> EditTriageMeeting(int id)
    {
        var meeting = await _context.TriageMeetings.FindAsync(id);
        if (meeting == null)
        {
            return NotFound();
        }

        return View("~/Views/Admin/TriageMeeting/Edit.cshtml", meeting);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTriageMeeting(int id, TriageMeeting model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (model.EndAt <= model.StartAt)
        {
            ModelState.AddModelError(nameof(model.EndAt), "End time must be after the start time.");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/TriageMeeting/Edit.cshtml", model);
        }

        var meeting = await _context.TriageMeetings.FindAsync(id);
        if (meeting == null)
        {
            return NotFound();
        }

        meeting.Title = model.Title?.Trim() ?? string.Empty;
        meeting.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        meeting.Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim();
        meeting.StartAt = model.StartAt;
        meeting.EndAt = model.EndAt;
        meeting.IsActive = model.IsActive;
        meeting.UpdatedAt = DateTime.UtcNow;

        _context.TriageMeetings.Update(meeting);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Triage meeting updated.";
        return RedirectToAction(nameof(TriageMeetings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTriageMeeting(int id)
    {
        var meeting = await _context.TriageMeetings
            .Include(tm => tm.DemandRequests)
            .FirstOrDefaultAsync(tm => tm.Id == id);

        if (meeting == null)
        {
            return NotFound();
        }

        if (meeting.DemandRequests.Any())
        {
            TempData["ErrorMessage"] = "Cannot delete a triage meeting that has demand requests assigned.";
            return RedirectToAction(nameof(TriageMeetings));
        }

        _context.TriageMeetings.Remove(meeting);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Triage meeting deleted.";
        return RedirectToAction(nameof(TriageMeetings));
    }

    private RaidLookupDefinition? ResolveRaidLookupDefinition(string? key) =>
        _raidLookupDefinitions.FirstOrDefault(d =>
            string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));

    private async Task<RaidLookupListViewModel> BuildRaidSettingsViewModelAsync(
        RaidLookupDefinition descriptor,
        RaidLookupEditInputModel? newEntry = null,
        RaidLookupEditInputModel? editEntry = null)
    {
        var items = await descriptor.Query(_context)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Label)
            .Select(x => new RaidLookupListItemViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Label = x.Label,
                Description = x.Description,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync();

        var defaultSort = items.Any() ? items.Max(i => i.SortOrder) + 10 : 0;

        return new RaidLookupListViewModel
        {
            CurrentLookupKey = descriptor.Key,
            CurrentLookupLabel = descriptor.Label,
            CurrentLookupDescription = descriptor.Description,
            Lookups = _raidLookupDefinitions
                .Select(d => new RaidLookupSelectorViewModel
                {
                    Key = d.Key,
                    Label = d.Label
                })
                .ToList(),
            Items = items,
            NewEntry = newEntry ?? new RaidLookupEditInputModel
            {
                LookupKey = descriptor.Key,
                SortOrder = defaultSort,
                IsActive = true
            },
            EditEntry = editEntry,
            CanSeedDefaults = RaidLookupSeedData.Definitions.ContainsKey(descriptor.Key)
        };
    }

    private record RaidLookupDefinition(
        string Key,
        string Label,
        Func<CompassDbContext, IQueryable<RaidLookupBase>> Query,
        Func<RaidLookupBase> Factory,
        string? Description);

}
