using System.Globalization;
using Compass.Helpers;
using Compass.Models;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

public partial class ModernManageController
{
    // ── Service lines — pickers (JSON for autocomplete) ───────────────────

    [HttpGet("service-lines/pick-products")]
    public async Task<IActionResult> ServiceLinePickProducts([FromQuery] string? q, CancellationToken ct)
    {
        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Fips, User))
            return new JsonResult(new { error = "FIPS is not available." }) { StatusCode = 403 };
        if (!await IsCentralOperationsAdminAsync(ct))
            return new JsonResult(new { error = "Access denied." }) { StatusCode = 403 };

        var term = (q ?? "").Trim();
        if (term.Length < 2)
            return Json(new { results = Array.Empty<object>() });

        var results = await _context.CMDBProducts
            .AsNoTracking()
            .Where(p =>
                p.Status != CMDBProductStatus.Rejected &&
                ((p.Title != null && p.Title.Contains(term)) ||
                 (p.CMDBID != null && p.CMDBID.Contains(term))))
            .OrderBy(p => p.Title)
            .Take(20)
            .Select(p => new
            {
                id = p.Id.ToString(),
                title = p.Title,
            })
            .ToListAsync(ct);

        return Json(new { results });
    }

    [HttpGet("service-lines/pick-work")]
    public async Task<IActionResult> ServiceLinePickWork([FromQuery] string? q, CancellationToken ct)
    {
        if (!await _globalFeatureToggle.IsFeatureEnabledForPrincipalAsync(FeatureCodes.Fips, User))
            return new JsonResult(new { error = "FIPS is not available." }) { StatusCode = 403 };
        if (!await IsCentralOperationsAdminAsync(ct))
            return new JsonResult(new { error = "Access denied." }) { StatusCode = 403 };

        var term = (q ?? "").Trim();
        if (term.Length < 2)
            return Json(new { results = Array.Empty<object>() });

        var results = await _context.Projects
            .AsNoTracking()
            .Where(p =>
                !p.IsDeleted &&
                ((p.Title != null && p.Title.Contains(term)) || p.ProjectCode.Contains(term)))
            .OrderBy(p => p.Title)
            .Take(20)
            .Select(p => new
            {
                id = p.Id,
                title = p.Title ?? "",
                subtitle = "WI-" + p.Id.ToString("D8", CultureInfo.InvariantCulture),
            })
            .ToListAsync(ct);

        return Json(new { results });
    }

    // ── Service lines ─────────────────────────────────────────────────────

    [HttpGet("service-lines")]
    public async Task<IActionResult> ServiceLines(CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-service-lines");

        var canEdit = await IsCentralOperationsAdminAsync(ct);
        var rows = await _context.ServiceLines
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ServiceLineListRow
            {
                Id = s.Id,
                Slug = s.Slug,
                Name = s.Name,
                Directorates = s.ServiceLineDivisions.Count,
                BusinessAreas = s.ServiceLineBusinessAreas.Count,
                Products = s.ServiceLineProducts.Count,
                WorkItems = s.ServiceLineProjects.Count,
                UpdatedAt = s.UpdatedAt,
            })
            .ToListAsync(ct);

        return View("ServiceLines/Index", new ServiceLineListViewModel
        {
            Rows = rows,
            CanEdit = canEdit,
        });
    }

    [HttpGet("service-lines/new")]
    public async Task<IActionResult> ServiceLineNew(CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await IsCentralOperationsAdminAsync(ct))
            return Forbid();

        SetNav("manage-service-lines");
        return View("ServiceLines/Form", await BuildServiceLineFormViewModelAsync(isNew: true, null, null, ct));
    }

    [HttpPost("service-lines")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceLineCreate([FromForm] ServiceLineFormInput input, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await IsCentralOperationsAdminAsync(ct))
            return Forbid();

        if (!ModelState.IsValid)
        {
            SetNav("manage-service-lines");
            return View("ServiceLines/Form", await BuildServiceLineFormViewModelAsync(
                isNew: true, null, input, ct));
        }

        if (!await ValidateServiceLineFormAsync(input, ct))
        {
            SetNav("manage-service-lines");
            return View("ServiceLines/Form", await BuildServiceLineFormViewModelAsync(
                isNew: true, null, input, ct));
        }

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var name = input.Name.Trim();
        var baseSlug = ServiceLineSlugHelper.GenerateBaseSlug(name);
        var slug = await ServiceLineSlugHelper.EnsureUniqueSlugAsync(_context, baseSlug, null, ct);
        var sl = new ServiceLine
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        _context.ServiceLines.Add(sl);
        AddServiceLineJunctions(sl, input);
        await _context.SaveChangesAsync(ct);
        TempData["Success"] = "Service line created.";
        return Redirect(Url.Action(nameof(ServiceLineDetail), "ModernManage", new { slug })!);
    }

    [HttpGet("service-lines/{id:guid}")]
    public async Task<IActionResult> ServiceLineDetailRedirectByGuid(Guid id, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        var s = await _context.ServiceLines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Slug })
            .FirstOrDefaultAsync(ct);
        if (s == null)
            return NotFound();
        return RedirectPermanent(Url.Action(nameof(ServiceLineDetail), "ModernManage", new { slug = s.Slug })!);
    }

    [HttpGet("service-lines/{id:guid}/edit")]
    public async Task<IActionResult> ServiceLineEditRedirectByGuid(Guid id, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await IsCentralOperationsAdminAsync(ct))
            return Forbid();
        var s = await _context.ServiceLines
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.Slug })
            .FirstOrDefaultAsync(ct);
        if (s == null)
            return NotFound();
        return RedirectPermanent(Url.Action(nameof(ServiceLineEdit), "ModernManage", new { slug = s.Slug })!);
    }

    [HttpGet("service-lines/{slug}")]
    public async Task<IActionResult> ServiceLineDetail(string slug, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;

        SetNav("manage-service-lines");

        var canEdit = await IsCentralOperationsAdminAsync(ct);

        var sl = await _context.ServiceLines
            .AsNoTracking()
            .Include(s => s.ServiceLineDivisions).ThenInclude(d => d.Division)
            .Include(s => s.ServiceLineBusinessAreas).ThenInclude(b => b.BusinessAreaLookup)
            .Include(s => s.ServiceLineProducts).ThenInclude(p => p.CMDBProduct).ThenInclude(c => c.Phase)
            .Include(s => s.ServiceLineProducts).ThenInclude(p => p.CMDBProduct).ThenInclude(c => c.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea).ThenInclude(f => f.BusinessAreaLookup)
            .Include(s => s.ServiceLineProjects).ThenInclude(p => p.Project).ThenInclude(w => w.PhaseLookup)
            .Include(s => s.ServiceLineProjects).ThenInclude(p => p.Project).ThenInclude(w => w.BusinessAreaLookup)
            .FirstOrDefaultAsync(s => s.Slug == slug, ct);

        if (sl == null)
            return NotFound();

        var vm = new ServiceLineDetailViewModel
        {
            ServiceLine = sl,
            Divisions = sl.ServiceLineDivisions
                .Select(x => x.Division)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Name)
                .ToList(),
            BusinessAreas = sl.ServiceLineBusinessAreas
                .Select(x => x.BusinessAreaLookup)
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Name)
                .ToList(),
            Products = sl.ServiceLineProducts
                .Select(x => x.CMDBProduct)
                .OrderBy(p => p.Title)
                .ToList(),
            Projects = sl.ServiceLineProjects
                .Select(x => x.Project)
                .OrderBy(p => p.Title)
                .ToList(),
            CanEdit = canEdit,
        };

        return View("ServiceLines/Detail", vm);
    }

    [HttpGet("service-lines/{slug}/edit")]
    public async Task<IActionResult> ServiceLineEdit(string slug, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await IsCentralOperationsAdminAsync(ct))
            return Forbid();

        SetNav("manage-service-lines");

        var sl = await _context.ServiceLines
            .AsNoTracking()
            .Include(s => s.ServiceLineDivisions)
            .Include(s => s.ServiceLineBusinessAreas)
            .Include(s => s.ServiceLineProducts)
            .Include(s => s.ServiceLineProjects)
            .FirstOrDefaultAsync(s => s.Slug == slug, ct);
        if (sl == null)
            return NotFound();

        return View("ServiceLines/Form", await BuildServiceLineFormViewModelAsync(
            isNew: false, sl, null, ct));
    }

    [HttpPost("service-lines/{slug}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceLineUpdate(string slug, [FromForm] ServiceLineFormInput input, CancellationToken ct)
    {
        var disabled = await RequireFipsDatabaseAsync();
        if (disabled != null)
            return disabled;
        if (!await IsCentralOperationsAdminAsync(ct))
            return Forbid();

        var sl = await _context.ServiceLines
            .Include(s => s.ServiceLineDivisions)
            .Include(s => s.ServiceLineBusinessAreas)
            .Include(s => s.ServiceLineProducts)
            .Include(s => s.ServiceLineProjects)
            .FirstOrDefaultAsync(s => s.Slug == slug, ct);
        if (sl == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            SetNav("manage-service-lines");
            return View("ServiceLines/Form", await BuildServiceLineFormViewModelAsync(
                isNew: false, sl, input, ct));
        }

        if (!await ValidateServiceLineFormAsync(input, ct))
        {
            SetNav("manage-service-lines");
            return View("ServiceLines/Form", await BuildServiceLineFormViewModelAsync(
                isNew: false, sl, input, ct));
        }

        var newName = input.Name.Trim();
        if (!string.Equals(sl.Name, newName, StringComparison.Ordinal))
        {
            var baseSlug = ServiceLineSlugHelper.GenerateBaseSlug(newName);
            sl.Slug = await ServiceLineSlugHelper.EnsureUniqueSlugAsync(_context, baseSlug, sl.Id, ct);
        }
        sl.Name = newName;
        sl.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        sl.UpdatedAt = DateTime.UtcNow;

        sl.ServiceLineDivisions.Clear();
        sl.ServiceLineBusinessAreas.Clear();
        sl.ServiceLineProducts.Clear();
        sl.ServiceLineProjects.Clear();
        AddServiceLineJunctions(sl, input);
        await _context.SaveChangesAsync(ct);
        TempData["Success"] = "Service line updated.";
        return Redirect(Url.Action(nameof(ServiceLineDetail), "ModernManage", new { slug = sl.Slug })!);
    }

    private void AddServiceLineJunctions(ServiceLine sl, ServiceLineFormInput input)
    {
        var divisionIds = input.DivisionIds?.Distinct() ?? Array.Empty<int>();
        foreach (var d in divisionIds)
        {
            sl.ServiceLineDivisions.Add(new ServiceLineDivision
            {
                ServiceLineId = sl.Id,
                DivisionId = d,
            });
        }

        var balIds = input.BusinessAreaLookupIds?.Distinct() ?? Array.Empty<int>();
        foreach (var b in balIds)
        {
            sl.ServiceLineBusinessAreas.Add(new ServiceLineBusinessArea
            {
                ServiceLineId = sl.Id,
                BusinessAreaLookupId = b,
            });
        }

        var productIds = input.ProductIds?.Distinct() ?? Array.Empty<Guid>();
        foreach (var p in productIds)
        {
            sl.ServiceLineProducts.Add(new ServiceLineProduct
            {
                ServiceLineId = sl.Id,
                CMDBProductId = p,
            });
        }

        var projectIds = input.ProjectIds?.Distinct() ?? Array.Empty<int>();
        foreach (var p in projectIds)
        {
            sl.ServiceLineProjects.Add(new ServiceLineProject
            {
                ServiceLineId = sl.Id,
                ProjectId = p,
            });
        }
    }

    private async Task<bool> ValidateServiceLineFormAsync(ServiceLineFormInput input, CancellationToken ct)
    {
        var dIds = input.DivisionIds?.Distinct() ?? Array.Empty<int>();
        if (dIds.Any())
        {
            var found = await _context.Divisions
                .AsNoTracking()
                .Where(d => dIds.Contains(d.Id))
                .CountAsync(ct);
            if (found != dIds.Count())
            {
                ModelState.AddModelError(string.Empty, "One or more directorate selections are invalid.");
                return false;
            }
        }

        var baIds = input.BusinessAreaLookupIds?.Distinct() ?? Array.Empty<int>();
        if (baIds.Any())
        {
            var found = await _context.BusinessAreaLookups
                .Where(b => baIds.Contains(b.Id))
                .CountAsync(ct);
            if (found != baIds.Count())
            {
                ModelState.AddModelError(string.Empty, "One or more business area selections are invalid.");
                return false;
            }
        }

        var pIds = input.ProductIds?.Distinct() ?? Array.Empty<Guid>();
        if (pIds.Any())
        {
            var found = await _context.CMDBProducts
                .Where(p => pIds.Contains(p.Id))
                .CountAsync(ct);
            if (found != pIds.Count())
            {
                ModelState.AddModelError(string.Empty, "One or more product selections are invalid.");
                return false;
            }
        }

        var wIds = input.ProjectIds?.Distinct() ?? Array.Empty<int>();
        if (wIds.Any())
        {
            var found = await _context.Projects
                .Where(p => wIds.Contains(p.Id) && !p.IsDeleted)
                .CountAsync(ct);
            if (found != wIds.Count())
            {
                ModelState.AddModelError(string.Empty, "One or more work item selections are invalid.");
                return false;
            }
        }

        return true;
    }

    private async Task<ServiceLineFormViewModel> BuildServiceLineFormViewModelAsync(
        bool isNew, ServiceLine? fromDb, ServiceLineFormInput? fromPost, CancellationToken ct)
    {
        IReadOnlyList<int> sDiv;
        IReadOnlyList<int> sBal;
        IReadOnlyList<Guid> sPr;
        IReadOnlyList<int> sW;
        string name;
        string? desc;

        if (fromPost != null)
        {
            name = fromPost.Name;
            desc = fromPost.Description;
            sDiv = (fromPost.DivisionIds ?? Array.Empty<int>()).Distinct().ToList();
            sBal = (fromPost.BusinessAreaLookupIds ?? Array.Empty<int>()).Distinct().ToList();
            sPr = (fromPost.ProductIds ?? Array.Empty<Guid>()).Distinct().ToList();
            sW = (fromPost.ProjectIds ?? Array.Empty<int>()).Distinct().ToList();
        }
        else if (fromDb != null)
        {
            name = fromDb.Name;
            desc = fromDb.Description;
            sDiv = fromDb.ServiceLineDivisions.Select(d => d.DivisionId).ToList();
            sBal = fromDb.ServiceLineBusinessAreas.Select(b => b.BusinessAreaLookupId).ToList();
            sPr = fromDb.ServiceLineProducts.Select(p => p.CMDBProductId).ToList();
            sW = fromDb.ServiceLineProjects.Select(p => p.ProjectId).ToList();
        }
        else
        {
            name = "";
            desc = null;
            sDiv = Array.Empty<int>();
            sBal = Array.Empty<int>();
            sPr = Array.Empty<Guid>();
            sW = Array.Empty<int>();
        }

        var divOpts = (await _context.Divisions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new ServiceLineFormOption
            {
                Value = d.Id.ToString(),
                Text = d.Name,
            })
            .ToListAsync(ct))
            .ToList();
        {
            var haveDiv = new HashSet<string>(divOpts.Select(d => d.Value), StringComparer.Ordinal);
            if (sDiv is { Count: > 0 })
            {
                var missingD = sDiv.Where(id => !haveDiv.Contains(id.ToString(CultureInfo.InvariantCulture))).ToArray();
                if (missingD.Length > 0)
                {
                    var extraD = await _context.Divisions
                        .AsNoTracking()
                        .Where(d => missingD.Contains(d.Id))
                        .Select(d => new ServiceLineFormOption
                        {
                            Value = d.Id.ToString(),
                            Text = d.Name + (d.IsActive ? "" : " (inactive)"),
                        })
                        .ToListAsync(ct);
                    divOpts.AddRange(extraD);
                }
            }
        }

        var balOpts = (await _context.BusinessAreaLookups
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.SortOrder)
            .ThenBy(b => b.Name)
            .Select(b => new ServiceLineFormOption
            {
                Value = b.Id.ToString(),
                Text = b.Name,
            })
            .ToListAsync(ct))
            .ToList();
        {
            var haveBal = new HashSet<string>(balOpts.Select(b => b.Value), StringComparer.Ordinal);
            if (sBal is { Count: > 0 })
            {
                var missingB = sBal.Where(id => !haveBal.Contains(id.ToString(CultureInfo.InvariantCulture))).ToArray();
                if (missingB.Length > 0)
                {
                    var extraB = await _context.BusinessAreaLookups
                        .AsNoTracking()
                        .Where(b => missingB.Contains(b.Id))
                        .Select(b => new ServiceLineFormOption
                        {
                            Value = b.Id.ToString(),
                            Text = b.Name + (b.IsActive ? "" : " (inactive)"),
                        })
                        .ToListAsync(ct);
                    balOpts.AddRange(extraB);
                }
            }
        }

        IReadOnlyList<ServiceLinePickedItem> initialProducts;
        if (sPr is { Count: 0 })
        {
            initialProducts = Array.Empty<ServiceLinePickedItem>();
        }
        else
        {
            var rows = await _context.CMDBProducts
                .AsNoTracking()
                .Where(p => sPr.Contains(p.Id))
                .OrderBy(p => p.Title)
                .Select(p => new { p.Id, p.Title })
                .ToListAsync(ct);
            initialProducts = rows
                .Select(p => new ServiceLinePickedItem { Id = p.Id.ToString("D", CultureInfo.InvariantCulture), Label = p.Title ?? "" })
                .ToList();
        }

        IReadOnlyList<ServiceLinePickedItem> initialWork;
        if (sW is { Count: 0 })
        {
            initialWork = Array.Empty<ServiceLinePickedItem>();
        }
        else
        {
            var rows = await _context.Projects
                .AsNoTracking()
                .Where(p => sW.Contains(p.Id))
                .OrderBy(p => p.Title)
                .Select(p => new { p.Id, p.ProjectCode, p.Title, p.IsDeleted })
                .ToListAsync(ct);
            initialWork = rows
                .Select(p => new ServiceLinePickedItem
                {
                    Id = p.Id.ToString(CultureInfo.InvariantCulture),
                    Label = (p.Title ?? "") + (p.IsDeleted ? " (archived)" : ""),
                    Subtitle = "WI-" + p.Id.ToString("D8", CultureInfo.InvariantCulture),
                })
                .ToList();
        }

        return new ServiceLineFormViewModel
        {
            IsNew = isNew,
            Id = fromDb?.Id,
            Slug = fromDb?.Slug,
            Name = name,
            Description = desc,
            SelectedDivisionIds = sDiv,
            SelectedBusinessAreaLookupIds = sBal,
            SelectedProductIds = sPr,
            SelectedProjectIds = sW,
            DivisionOptions = divOpts,
            BusinessAreaOptions = balOpts,
            InitialProducts = initialProducts,
            InitialWork = initialWork,
        };
    }
}
