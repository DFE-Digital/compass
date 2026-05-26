using Compass.Models;
using Compass.Services.Fips;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernRaidController
{
    // ── Register Dashboard ───────────────────────────────────────

    [HttpGet("")]
    [HttpGet("index")]
    [HttpGet("dashboard")]
    [HttpGet("registers")]
    [HttpGet("/ModernRaid")]
    [HttpGet("/ModernRaid/Index")]
    [HttpGet("/ModernRaid/Dashboard")]
    public async Task<IActionResult> Registers(CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-registers");

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);

        var registers = await GetAccessibleRegistersAsync(userId, cancellationToken);

        var registerIds = registers.Select(r => r.Id).ToList();

        var riskCounts = await _db.RaidRegisterRisks
            .Where(rr => registerIds.Contains(rr.RaidRegisterId) && !rr.Risk.IsDeleted && rr.Risk.Status != "closed")
            .GroupBy(rr => rr.RaidRegisterId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var issueCounts = await _db.RaidRegisterIssues
            .Where(ri => registerIds.Contains(ri.RaidRegisterId) && !ri.Issue.IsDeleted && ri.Issue.Status != "closed")
            .GroupBy(ri => ri.RaidRegisterId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var assumptionCounts = await _db.RaidRegisterAssumptions
            .Where(ra => registerIds.Contains(ra.RaidRegisterId) && !ra.Assumption.IsDeleted)
            .GroupBy(ra => ra.RaidRegisterId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var dependencyCounts = await _db.RaidRegisterDependencies
            .Where(rd => registerIds.Contains(rd.RaidRegisterId) && rd.Dependency.Status != "Resolved" && rd.Dependency.Status != "Cancelled")
            .GroupBy(rd => rd.RaidRegisterId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var nearMissCounts = await _db.RaidRegisterNearMisses
            .Where(rn => registerIds.Contains(rn.RaidRegisterId) && !rn.NearMiss.IsDeleted)
            .GroupBy(rn => rn.RaidRegisterId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var vm = new RaidRegisterDashboardViewModel
        {
            Registers = registers.Select(r => new RaidRegisterCardViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                DirectorateName = r.DirectorateLookup?.Name,
                BusinessAreaName = r.BusinessAreaLookup?.Name,
                OwnerName = r.Users
                    .Where(u => u.Role == RaidRegisterRole.Owner)
                    .Select(u => u.User?.Name)
                    .FirstOrDefault(),
                UpdatedAt = r.UpdatedAt,
                OpenRiskCount = riskCounts.GetValueOrDefault(r.Id),
                OpenIssueCount = issueCounts.GetValueOrDefault(r.Id),
                OpenAssumptionCount = assumptionCounts.GetValueOrDefault(r.Id),
                OpenDependencyCount = dependencyCounts.GetValueOrDefault(r.Id),
                OpenNearMissCount = nearMissCounts.GetValueOrDefault(r.Id),
                TotalItemCount = riskCounts.GetValueOrDefault(r.Id)
                    + issueCounts.GetValueOrDefault(r.Id)
                    + assumptionCounts.GetValueOrDefault(r.Id)
                    + dependencyCounts.GetValueOrDefault(r.Id)
                    + nearMissCounts.GetValueOrDefault(r.Id),
                WorkItemNames = r.WorkItems
                    .Select(w => w.Project?.Title ?? "Unknown")
                    .OrderBy(n => n).ToList(),
                ServiceNames = r.Services
                    .Select(s => s.FipsService?.DisplayName ?? "Unknown")
                    .OrderBy(n => n).ToList()
            }).ToList()
        };

        return View("~/Views/Modern/Raid/Registers/Index.cshtml", vm);
    }

    // ── Register Detail ──────────────────────────────────────────

    [HttpGet("registers/{id:int}")]
    public async Task<IActionResult> RegisterDetail(int id, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-registers");

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        var email = (User.Identity?.Name ?? "").Trim().ToLower();

        var register = await _db.RaidRegisters
            .AsNoTracking()
            .Include(r => r.DirectorateLookup)
            .Include(r => r.BusinessAreaLookup)
            .Include(r => r.CreatedByUser)
            .Include(r => r.Users).ThenInclude(u => u.User)
            .Include(r => r.WorkItems).ThenInclude(w => w.Project)
            .Include(r => r.Services).ThenInclude(s => s.FipsService)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (register == null) return NotFound();

        if (!CanAccessRegister(register, userId, email))
            return Forbid();

        var currentUserRole = register.Users
            .Where(u => userId.HasValue && u.UserId == userId.Value)
            .Select(u => (RaidRegisterRole?)u.Role)
            .FirstOrDefault();
        if (register.CreatedByUserId == userId)
            currentUserRole = RaidRegisterRole.Owner;

        var risks = await _db.RaidRegisterRisks.AsNoTracking()
            .Where(rr => rr.RaidRegisterId == id && !rr.Risk.IsDeleted)
            .Select(rr => new RaidRegisterRiskRow
            {
                Id = rr.Risk.Id,
                Reference = $"R-{rr.Risk.Id:D4}",
                Title = rr.Risk.Title,
                Status = rr.Risk.RiskStatus != null ? rr.Risk.RiskStatus.Label : rr.Risk.Status,
                Owner = rr.Risk.OwnerUser != null ? rr.Risk.OwnerUser.Name : rr.Risk.OwnerEmail,
                InherentScore = rr.Risk.InherentScore,
                Tier = rr.Risk.RiskTier != null ? rr.Risk.RiskTier.Name : null
            }).ToListAsync(cancellationToken);

        var issues = await _db.RaidRegisterIssues.AsNoTracking()
            .Where(ri => ri.RaidRegisterId == id && !ri.Issue.IsDeleted)
            .Select(ri => new RaidRegisterIssueRow
            {
                Id = ri.Issue.Id,
                Reference = $"I-{ri.Issue.Id:D4}",
                Title = ri.Issue.Title,
                Status = ri.Issue.StatusLookup != null ? ri.Issue.StatusLookup.Label : ri.Issue.Status,
                Severity = ri.Issue.SeverityLookup != null ? ri.Issue.SeverityLookup.Label : ri.Issue.Severity,
                Owner = ri.Issue.OwnerUser != null ? ri.Issue.OwnerUser.Name : null
            }).ToListAsync(cancellationToken);

        var assumptions = await _db.RaidRegisterAssumptions.AsNoTracking()
            .Where(ra => ra.RaidRegisterId == id && !ra.Assumption.IsDeleted)
            .Select(ra => new RaidRegisterAssumptionRow
            {
                Id = ra.Assumption.Id,
                Description = ra.Assumption.Description,
                Status = ra.Assumption.StatusLookup != null ? ra.Assumption.StatusLookup.Label : null,
                Criticality = ra.Assumption.CriticalityLookup != null ? ra.Assumption.CriticalityLookup.Label : null,
                Owner = ra.Assumption.OwnerUser != null ? ra.Assumption.OwnerUser.Name : null
            }).ToListAsync(cancellationToken);

        var dependencies = await _db.RaidRegisterDependencies.AsNoTracking()
            .Where(rd => rd.RaidRegisterId == id)
            .Select(rd => new RaidRegisterDependencyRow
            {
                Id = rd.Dependency.Id,
                Description = rd.Dependency.Description,
                LinkType = rd.Dependency.LinkTypeLookup != null ? rd.Dependency.LinkTypeLookup.Label : rd.Dependency.DependencyType,
                Status = rd.Dependency.Status,
                Owner = rd.Dependency.OwnerUser != null ? rd.Dependency.OwnerUser.Name : null
            }).ToListAsync(cancellationToken);

        var nearMisses = await _db.RaidRegisterNearMisses.AsNoTracking()
            .Where(rn => rn.RaidRegisterId == id && !rn.NearMiss.IsDeleted)
            .Select(rn => new RaidRegisterNearMissRow
            {
                Id = rn.NearMiss.Id,
                Reference = rn.NearMiss.Reference,
                Impact = rn.NearMiss.Impact,
                Status = rn.NearMiss.StatusLookup != null ? rn.NearMiss.StatusLookup.Label : null,
                Seriousness = rn.NearMiss.SeriousnessLookup != null ? rn.NearMiss.SeriousnessLookup.Label : null
            }).ToListAsync(cancellationToken);

        var vm = new RaidRegisterDetailViewModel
        {
            Id = register.Id,
            Name = register.Name,
            Description = register.Description,
            DirectorateName = register.DirectorateLookup?.Name,
            BusinessAreaName = register.BusinessAreaLookup?.Name,
            CreatedAt = register.CreatedAt,
            UpdatedAt = register.UpdatedAt,
            CreatedByName = register.CreatedByUser?.Name ?? register.CreatedByUser?.Email ?? "Unknown",
            CurrentUserRole = currentUserRole ?? RaidRegisterRole.Viewer,
            OpenRiskCount = risks.Count(r => r.Status != "closed" && r.Status != "Closed"),
            OpenIssueCount = issues.Count(i => i.Status != "closed" && i.Status != "Closed"),
            OpenAssumptionCount = assumptions.Count,
            OpenDependencyCount = dependencies.Count(d => d.Status != "Resolved" && d.Status != "Cancelled"),
            OpenNearMissCount = nearMisses.Count,
            Risks = risks,
            Issues = issues,
            Assumptions = assumptions,
            Dependencies = dependencies,
            NearMisses = nearMisses,
            WorkItemNames = register.WorkItems.Select(w => w.Project?.Title ?? $"Work item #{w.ProjectId}").ToList(),
            ServiceNames = register.Services.Select(s => s.FipsService?.DisplayName ?? $"Service #{s.FipsServiceId}").ToList(),
            Users = register.Users.Select(u => new RaidRegisterUserRow
            {
                UserId = u.UserId,
                Email = u.User?.Email ?? "",
                DisplayName = u.User?.Name,
                Role = u.Role
            }).ToList()
        };

        return View("~/Views/Modern/Raid/Registers/Detail.cshtml", vm);
    }

    // ── Onboarding Wizard ────────────────────────────────────────

    [HttpGet("registers/create")]
    public async Task<IActionResult> RegisterCreate(int step = 1, int? id = null, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-registers");
        step = Math.Clamp(step, 1, 6);

        RaidRegister? existing = null;
        if (id.HasValue)
        {
            existing = await _db.RaidRegisters
                .Include(r => r.WorkItems)
                .Include(r => r.Services)
                .Include(r => r.Users).ThenInclude(u => u.User)
                .FirstOrDefaultAsync(r => r.Id == id.Value && !r.IsDeleted, cancellationToken);
        }

        var vm = new RaidRegisterOnboardingViewModel
        {
            RegisterId = existing?.Id,
            Step = step,
            Name = existing?.Name ?? "",
            Description = existing?.Description,
            DirectorateLookupId = existing?.DirectorateLookupId,
            BusinessAreaLookupId = existing?.BusinessAreaLookupId,
            SelectedWorkItemIds = existing?.WorkItems.Select(w => w.ProjectId).ToList() ?? new(),
            SelectedServiceIds = existing?.Services.Select(s => s.FipsServiceId).ToList() ?? new(),
            RegisterUsers = existing?.Users.Select(u => new RaidRegisterUserRow
            {
                UserId = u.UserId,
                Email = u.User?.Email ?? "",
                DisplayName = u.User?.Name,
                Role = u.Role
            }).ToList() ?? new()
        };

        await PopulateOnboardingOptionsAsync(vm, cancellationToken);

        return View("~/Views/Modern/Raid/Registers/Create.cshtml", vm);
    }

    [HttpPost("registers/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterCreatePost(
        int step,
        RaidRegisterOnboardingViewModel vm,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-registers");

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        if (!userId.HasValue)
        {
            TempData["Error"] = "We could not match your sign-in to a user. Try again or contact support.";
            return RedirectToAction(nameof(Registers));
        }

        // Step 1: Create or update the register draft
        RaidRegister register;
        if (vm.RegisterId.HasValue)
        {
            register = await _db.RaidRegisters
                .Include(r => r.WorkItems)
                .Include(r => r.Services)
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.Id == vm.RegisterId.Value && !r.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Register not found.");
        }
        else
        {
            register = new RaidRegister
            {
                CreatedByUserId = userId.Value,
                CreatedAt = DateTime.UtcNow
            };
            _db.RaidRegisters.Add(register);
        }

        switch (step)
        {
            case 1:
                if (string.IsNullOrWhiteSpace(vm.Name))
                {
                    ModelState.AddModelError("Name", "Enter a name for this register");
                    await PopulateOnboardingOptionsAsync(vm, cancellationToken);
                    return View("~/Views/Modern/Raid/Registers/Create.cshtml", vm);
                }
                register.Name = vm.Name.Trim();
                register.Description = vm.Description?.Trim();
                register.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(RegisterCreate), new { step = 2, id = register.Id });

            case 2:
                register.DirectorateLookupId = vm.DirectorateLookupId;
                register.BusinessAreaLookupId = vm.BusinessAreaLookupId;
                register.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(RegisterCreate), new { step = 3, id = register.Id });

            case 3:
                SyncRegisterWorkItems(register, vm.SelectedWorkItemIds);
                register.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(RegisterCreate), new { step = 4, id = register.Id });

            case 4:
                SyncRegisterServices(register, vm.SelectedServiceIds);
                register.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(RegisterCreate), new { step = 5, id = register.Id });

            case 5:
                register.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return RedirectToAction(nameof(RegisterCreate), new { step = 6, id = register.Id });

            case 6:
                // Ensure creator is an Owner in the users table
                if (!register.Users.Any(u => u.UserId == userId.Value))
                {
                    register.Users.Add(new RaidRegisterUser
                    {
                        UserId = userId.Value,
                        Role = RaidRegisterRole.Owner,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                register.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                TempData["SuccessMessage"] = $"RAID register \"{register.Name}\" has been created.";
                return RedirectToAction(nameof(RegisterDetail), new { id = register.Id });

            default:
                return RedirectToAction(nameof(RegisterCreate), new { step = 1, id = register.Id });
        }
    }

    // ── Register Settings (edit scope) ───────────────────────────

    [HttpGet("registers/{id:int}/settings")]
    public async Task<IActionResult> RegisterSettings(int id, CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-registers");

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        var register = await _db.RaidRegisters
            .Include(r => r.WorkItems)
            .Include(r => r.Services)
            .Include(r => r.Users).ThenInclude(u => u.User)
            .Include(r => r.DirectorateLookup)
            .Include(r => r.BusinessAreaLookup)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (register == null) return NotFound();
        if (!IsRegisterOwnerOrManager(register, userId))
            return Forbid();

        var vm = new RaidRegisterOnboardingViewModel
        {
            RegisterId = register.Id,
            Step = 1,
            Name = register.Name,
            Description = register.Description,
            DirectorateLookupId = register.DirectorateLookupId,
            BusinessAreaLookupId = register.BusinessAreaLookupId,
            DirectorateName = register.DirectorateLookup?.Name,
            BusinessAreaName = register.BusinessAreaLookup?.Name,
            SelectedWorkItemIds = register.WorkItems.Select(w => w.ProjectId).ToList(),
            SelectedServiceIds = register.Services.Select(s => s.FipsServiceId).ToList(),
            RegisterUsers = register.Users.Select(u => new RaidRegisterUserRow
            {
                UserId = u.UserId,
                Email = u.User?.Email ?? "",
                DisplayName = u.User?.Name,
                Role = u.Role
            }).ToList()
        };

        await PopulateOnboardingOptionsAsync(vm, cancellationToken);

        vm.SelectedWorkItemNames = vm.WorkItemOptions?
            .Where(o => vm.SelectedWorkItemIds.Contains(o.Id))
            .Select(o => o.Name).ToList() ?? new List<string>();
        vm.SelectedServiceNames = vm.ServiceOptions?
            .Where(o => vm.SelectedServiceIds.Contains(o.Id))
            .Select(o => o.Name).ToList() ?? new List<string>();

        return View("~/Views/Modern/Raid/Registers/Settings.cshtml", vm);
    }

    [HttpPost("registers/{id:int}/settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterSettingsPost(
        int id,
        string section,
        RaidRegisterOnboardingViewModel vm,
        CancellationToken cancellationToken = default)
    {
        SetRaidChrome("raid-registers");

        var userId = await ResolveCurrentUserIdAsync(cancellationToken);
        var register = await _db.RaidRegisters
            .Include(r => r.WorkItems)
            .Include(r => r.Services)
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (register == null) return NotFound();
        if (!IsRegisterOwnerOrManager(register, userId))
            return Forbid();

        switch (section)
        {
            case "details":
                if (string.IsNullOrWhiteSpace(vm.Name))
                {
                    TempData["ErrorMessage"] = "Enter a name for this register.";
                    return RedirectToAction(nameof(RegisterSettings), new { id });
                }
                register.Name = vm.Name.Trim();
                register.Description = vm.Description?.Trim();
                break;

            case "scope":
                register.DirectorateLookupId = vm.DirectorateLookupId;
                register.BusinessAreaLookupId = vm.BusinessAreaLookupId;
                break;

            case "workitems":
                SyncRegisterWorkItems(register, vm.SelectedWorkItemIds);
                break;

            case "services":
                SyncRegisterServices(register, vm.SelectedServiceIds);
                break;

            case "users":
                SyncRegisterUsers(register, vm.RegisterUsers, userId!.Value);
                break;
        }

        register.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Register settings updated.";
        return RedirectToAction(nameof(RegisterSettings), new { id });
    }

    // ── Helpers ──────────────────────────────────────────────────

    private async Task<List<RaidRegister>> GetAccessibleRegistersAsync(int? userId, CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
            return new List<RaidRegister>();

        var uid = userId.Value;

        // Resolve the user's org-structure business area IDs (hybrid access)
        var adminBaIds = await _businessAreaAdmins.GetAdministeredBusinessAreaLookupIdsAsync(uid, cancellationToken);
        var leaderBaIds = await _businessAreaLeadership.GetLeadershipBusinessAreaLookupIdsAsync(uid, cancellationToken);
        var orgBaIds = adminBaIds.Union(leaderBaIds).Distinct().ToList();

        return await _db.RaidRegisters.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Include(r => r.DirectorateLookup)
            .Include(r => r.BusinessAreaLookup)
            .Include(r => r.Users).ThenInclude(u => u.User)
            .Include(r => r.WorkItems).ThenInclude(w => w.Project)
            .Include(r => r.Services).ThenInclude(s => s.FipsService)
            .Where(r =>
                r.CreatedByUserId == uid
                || r.Users.Any(u => u.UserId == uid)
                || (r.BusinessAreaLookupId != null && orgBaIds.Contains(r.BusinessAreaLookupId.Value))
            )
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    private static bool CanAccessRegister(RaidRegister register, int? userId, string emailLower)
    {
        if (!userId.HasValue) return false;
        if (register.CreatedByUserId == userId.Value) return true;
        if (register.Users.Any(u => u.UserId == userId.Value)) return true;
        return false;
    }

    private static bool IsRegisterOwnerOrManager(RaidRegister register, int? userId)
    {
        if (!userId.HasValue) return false;
        if (register.CreatedByUserId == userId.Value) return true;
        return register.Users.Any(u =>
            u.UserId == userId.Value &&
            (u.Role == RaidRegisterRole.Owner || u.Role == RaidRegisterRole.Manager));
    }

    private async Task PopulateOnboardingOptionsAsync(RaidRegisterOnboardingViewModel vm, CancellationToken cancellationToken)
    {
        vm.DirectorateOptions = await _db.DirectorateLookups.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new SelectOption(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        vm.BusinessAreaOptions = await _db.BusinessAreaLookups.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
            .Select(b => new SelectOption(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        vm.WorkItemOptions = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Title)
            .Select(p => new SelectOption(p.Id, p.Title ?? $"Work item #{p.Id}"))
            .ToListAsync(cancellationToken);

        vm.ServiceOptions = await FipsProductRaidQuery
            .BuildActiveServiceRegisterSelectOptionsForRaidAsync(_db, cancellationToken)
            .ContinueWith(t => t.Result.Select(o => new SelectOption(o.Id, o.Name)).ToList(), cancellationToken);

        // Populate review step names
        if (vm.Step == 6 && vm.RegisterId.HasValue)
        {
            if (vm.DirectorateLookupId.HasValue)
                vm.DirectorateName = vm.DirectorateOptions.FirstOrDefault(d => d.Id == vm.DirectorateLookupId)?.Name;
            if (vm.BusinessAreaLookupId.HasValue)
                vm.BusinessAreaName = vm.BusinessAreaOptions.FirstOrDefault(b => b.Id == vm.BusinessAreaLookupId)?.Name;

            vm.SelectedWorkItemNames = vm.WorkItemOptions
                .Where(o => vm.SelectedWorkItemIds.Contains(o.Id))
                .Select(o => o.Name).ToList();
            vm.SelectedServiceNames = vm.ServiceOptions
                .Where(o => vm.SelectedServiceIds.Contains(o.Id))
                .Select(o => o.Name).ToList();
        }
    }

    private static void SyncRegisterWorkItems(RaidRegister register, List<int> selectedIds)
    {
        var existing = register.WorkItems.Select(w => w.ProjectId).ToHashSet();
        var desired = selectedIds.ToHashSet();

        foreach (var toRemove in register.WorkItems.Where(w => !desired.Contains(w.ProjectId)).ToList())
            register.WorkItems.Remove(toRemove);

        foreach (var toAdd in desired.Where(id => !existing.Contains(id)))
            register.WorkItems.Add(new RaidRegisterWorkItem { ProjectId = toAdd });
    }

    private static void SyncRegisterServices(RaidRegister register, List<int> selectedIds)
    {
        var existing = register.Services.Select(s => s.FipsServiceId).ToHashSet();
        var desired = selectedIds.ToHashSet();

        foreach (var toRemove in register.Services.Where(s => !desired.Contains(s.FipsServiceId)).ToList())
            register.Services.Remove(toRemove);

        foreach (var toAdd in desired.Where(id => !existing.Contains(id)))
            register.Services.Add(new RaidRegisterService { FipsServiceId = toAdd });
    }

    private static void SyncRegisterUsers(RaidRegister register, List<RaidRegisterUserRow>? rows, int currentUserId)
    {
        if (rows == null) return;

        var desired = rows
            .Where(r => r.UserId != currentUserId)
            .ToDictionary(r => r.UserId, r => r.Role);

        foreach (var toRemove in register.Users
            .Where(u => u.UserId != currentUserId && !desired.ContainsKey(u.UserId))
            .ToList())
            register.Users.Remove(toRemove);

        foreach (var existing in register.Users.Where(u => desired.ContainsKey(u.UserId)))
            existing.Role = desired[existing.UserId];

        var existingIds = register.Users.Select(u => u.UserId).ToHashSet();
        foreach (var (uid, role) in desired.Where(kv => !existingIds.Contains(kv.Key)))
            register.Users.Add(new RaidRegisterUser
            {
                UserId = uid,
                Role = role,
                CreatedAt = DateTime.UtcNow
            });
    }
}
