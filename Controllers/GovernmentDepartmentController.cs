using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using System.Text.Json;

namespace Compass.Controllers
{
    [Route("Admin/[controller]")]
    public class GovernmentDepartmentController : Controller
    {
        private readonly CompassDbContext _context;
        private readonly ILogger<GovernmentDepartmentController> _logger;
        private readonly HttpClient _httpClient;

        public GovernmentDepartmentController(CompassDbContext context, ILogger<GovernmentDepartmentController> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        // GET: Admin/GovernmentDepartment
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? search, string? format, string? status, int page = 1, int pageSize = 20)
        {
            // Base query - include closed departments if filtering by status=closed
            var query = _context.GovernmentDepartments
                .Include(d => d.ParentDepartment)
                .Include(d => d.ChildDepartments.Where(c => !c.IsDeleted))
                .Where(d => !d.IsDeleted);

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "closed")
                {
                    query = query.Where(d => d.ClosedAt != null);
                }
                else if (status.ToLower() == "live")
                {
                    query = query.Where(d => d.ClosedAt == null && d.GovukStatus == "live");
                }
                else if (status.ToLower() == "exempt")
                {
                    query = query.Where(d => d.ClosedAt == null && d.GovukStatus == "exempt");
                }
            }
            else
            {
                // Default: only show active (non-closed) departments
                query = query.Where(d => d.ClosedAt == null);
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => d.Title.Contains(search) || 
                                        (d.Abbreviation != null && d.Abbreviation.Contains(search)));
            }

            // Apply format filter
            if (!string.IsNullOrWhiteSpace(format))
            {
                query = query.Where(d => d.Format == format);
            }

            var totalCount = await query.CountAsync();

            var departments = await query
                .OrderBy(d => d.Title)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get distinct formats for filter dropdown
            ViewBag.Formats = await _context.GovernmentDepartments
                .Where(d => !d.IsDeleted && d.ClosedAt == null && d.Format != null)
                .Select(d => d.Format)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentFormat = format;
            ViewBag.CurrentStatus = status;

            return View("~/Views/Admin/GovernmentDepartment/Index.cshtml", departments);
        }

        // POST: Admin/GovernmentDepartment/Sync
        [HttpPost("Sync")]
        public async Task<IActionResult> Sync()
        {
            try
            {
                var syncResult = await SyncGovernmentDepartments();
                
                return Json(new 
                { 
                    success = true, 
                    added = syncResult.Added, 
                    updated = syncResult.Updated, 
                    errors = syncResult.Errors,
                    message = $"Sync completed successfully. Added: {syncResult.Added}, Updated: {syncResult.Updated}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing government departments");
                return Json(new 
                { 
                    success = false, 
                    message = "Error syncing government departments: " + ex.Message 
                });
            }
        }

        // GET: Admin/GovernmentDepartment/Details/5
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.GovernmentDepartments
                .Include(d => d.ParentDepartment)
                .Include(d => d.ChildDepartments.Where(c => !c.IsDeleted && c.ClosedAt == null))
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (department == null)
            {
                return NotFound();
            }

            return View("~/Views/Admin/GovernmentDepartment/Details.cshtml", department);
        }

        // GET: Admin/GovernmentDepartment/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.GovernmentDepartments.FindAsync(id);
            if (department == null || department.IsDeleted)
            {
                return NotFound();
            }

            ViewBag.ParentDepartments = await _context.GovernmentDepartments
                .Where(d => !d.IsDeleted && d.ClosedAt == null && d.Id != id)
                .OrderBy(d => d.Title)
                .ToListAsync();
            
            return View("~/Views/Admin/GovernmentDepartment/Edit.cshtml", department);
        }

        // POST: Admin/GovernmentDepartment/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GovernmentDepartment department)
        {
            if (id != department.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    department.UpdatedAt = DateTime.UtcNow;
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Government department updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GovernmentDepartmentExists(department.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ParentDepartments = await _context.GovernmentDepartments
                .Where(d => !d.IsDeleted && d.ClosedAt == null && d.Id != id)
                .OrderBy(d => d.Title)
                .ToListAsync();
            
            return View(department);
        }

        // GET: Admin/GovernmentDepartment/Hierarchy
        [HttpGet("Hierarchy")]
        public async Task<IActionResult> Hierarchy()
        {
            var rootDepartments = await _context.GovernmentDepartments
                .Include(d => d.ChildDepartments.Where(c => !c.IsDeleted && c.ClosedAt == null))
                .Where(d => !d.IsDeleted && d.ClosedAt == null && d.ParentDepartmentId == null)
                .OrderBy(d => d.Title)
                .ToListAsync();

            return View("~/Views/Admin/GovernmentDepartment/Hierarchy.cshtml", rootDepartments);
        }

        // POST: Admin/GovernmentDepartment/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.GovernmentDepartments.FindAsync(id);
            if (department != null)
            {
                // Soft delete
                department.IsDeleted = true;
                department.UpdatedAt = DateTime.UtcNow;
                
                // Also soft delete child departments
                var childDepartments = await _context.GovernmentDepartments
                    .Where(d => d.ParentDepartmentId == id)
                    .ToListAsync();
                
                foreach (var child in childDepartments)
                {
                    child.IsDeleted = true;
                    child.UpdatedAt = DateTime.UtcNow;
                }
                
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Government department deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        // API endpoint for autocomplete
        [HttpGet("Search")]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { results = new object[0] });
            }

            var departments = await _context.GovernmentDepartments
                .Where(d => !d.IsDeleted && 
                           d.ClosedAt == null &&
                           d.ParentDepartmentId == null && // Only parent departments for autocomplete
                           d.Title.Contains(q))
                .OrderBy(d => d.Title)
                .Take(10)
                .Select(d => new { 
                    id = d.Id, 
                    text = d.Title,
                    abbreviation = d.Abbreviation 
                })
                .ToListAsync();

            return Json(new { results = departments });
        }

        // API endpoint to get a single department by ID
        [HttpGet("Get")]
        public async Task<IActionResult> Get(int? id)
        {
            if (id == null)
            {
                return Json(new { error = "ID is required" });
            }

            var department = await _context.GovernmentDepartments
                .Where(d => d.Id == id && !d.IsDeleted)
                .Select(d => new { 
                    id = d.Id, 
                    title = d.Title,
                    text = d.Title,
                    abbreviation = d.Abbreviation 
                })
                .FirstOrDefaultAsync();

            if (department == null)
            {
                return Json(new { error = "Department not found" });
            }

            return Json(department);
        }

        private async Task<(int Added, int Updated, int Errors)> SyncGovernmentDepartments()
        {
            int added = 0, updated = 0, errors = 0;
            int page = 1;
            const int pageSize = 20;

            try
            {
                _logger.LogInformation("Starting government departments sync from GOV.UK API");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                while (true)
                {
                    var url = $"https://www.gov.uk/api/organisations?page={page}";
                    _logger.LogInformation("Fetching page {Page} from {Url}", page, url);
                    
                    var response = await _httpClient.GetStringAsync(url);
                    var data = JsonSerializer.Deserialize<GovUkApiResponse>(response, jsonOptions);

                    if (data?.Results == null || !data.Results.Any())
                    {
                        _logger.LogInformation("No more results found at page {Page}", page);
                        break;
                    }

                    _logger.LogInformation("Processing {Count} organizations from page {Page}", data.Results.Count, page);

                    foreach (var org in data.Results)
                    {
                        try
                        {
                            // Only process organizations that aren't closed (include all statuses: live, exempt, etc.)
                            if (org.Details?.ClosedAt != null)
                            {
                                _logger.LogDebug("Skipping organization {Title} - ClosedAt: {ClosedAt}", 
                                    org.Title, org.Details?.ClosedAt);
                                continue;
                            }

                            var existingDept = await _context.GovernmentDepartments
                                .FirstOrDefaultAsync(d => d.GovukId == org.Id);

                            if (existingDept == null)
                            {
                                // Create new department
                                var newDept = new GovernmentDepartment
                                {
                                    Title = org.Title,
                                    Abbreviation = org.Details?.Abbreviation,
                                    Format = org.Format,
                                    GovukStatus = org.Details?.GovukStatus,
                                    ClosedAt = org.Details?.ClosedAt,
                                    WebUrl = org.WebUrl,
                                    AnalyticsIdentifier = org.AnalyticsIdentifier,
                                    GovukId = org.Id,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    LastSyncedAt = DateTime.UtcNow
                                };

                                _context.GovernmentDepartments.Add(newDept);
                                added++;
                                _logger.LogInformation("Added new department: {Title}", org.Title);
                            }
                            else
                            {
                                // Update existing department - only update API-sourced fields, preserve manual edits
                                existingDept.Title = org.Title;
                                existingDept.Abbreviation = org.Details?.Abbreviation;
                                existingDept.Format = org.Format;
                                existingDept.GovukStatus = org.Details?.GovukStatus;
                                existingDept.ClosedAt = org.Details?.ClosedAt;
                                existingDept.WebUrl = org.WebUrl;
                                existingDept.AnalyticsIdentifier = org.AnalyticsIdentifier;
                                existingDept.UpdatedAt = DateTime.UtcNow;
                                existingDept.LastSyncedAt = DateTime.UtcNow;
                                // Note: We do NOT update Description or ParentDepartmentId as these may have been manually set

                                updated++;
                                _logger.LogDebug("Updated department: {Title}", org.Title);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing organization: {OrgId}", org.Id);
                            errors++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved changes for page {Page}. Added: {Added}, Updated: {Updated}", page, added, updated);

                    // Check if we've reached the last page
                    if (page >= data.Pages)
                    {
                        _logger.LogInformation("Reached final page {Page} of {Total}", page, data.Pages);
                        break;
                    }

                    page++;
                }

                // Now process parent-child relationships
                await ProcessParentChildRelationships();
                
                _logger.LogInformation("Sync completed. Total Added: {Added}, Updated: {Updated}, Errors: {Errors}", added, updated, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during government departments sync");
                throw;
            }

            return (added, updated, errors);
        }

        private async Task ProcessParentChildRelationships()
        {
            _logger.LogInformation("Processing parent-child relationships");
            
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Get all departments from database
                var allDepartments = await _context.GovernmentDepartments
                    .Where(d => !d.IsDeleted)
                    .ToListAsync();

                int relationshipsProcessed = 0;

                // For each department, fetch its details from the API to get parent relationships
                foreach (var dept in allDepartments)
                {
                    if (string.IsNullOrEmpty(dept.GovukId))
                        continue;

                    try
                    {
                        var url = dept.GovukId;
                        var response = await _httpClient.GetStringAsync(url);
                        var orgData = JsonSerializer.Deserialize<GovUkOrganization>(response, jsonOptions);

                        if (orgData?.ParentOrganisations != null && orgData.ParentOrganisations.Any())
                        {
                            // Get the first parent (most orgs only have one parent)
                            var parentGovukId = orgData.ParentOrganisations.First().Id;
                            
                            // Find the parent in our database
                            var parentDept = allDepartments.FirstOrDefault(d => d.GovukId == parentGovukId);
                            
                            // Only update parent if not manually set or if it's different from API
                            if (parentDept != null && dept.ParentDepartmentId == null)
                            {
                                dept.ParentDepartmentId = parentDept.Id;
                                dept.UpdatedAt = DateTime.UtcNow;
                                relationshipsProcessed++;
                                
                                _logger.LogInformation("Linked {Child} to parent {Parent}", dept.Title, parentDept.Title);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error processing relationships for {Department}", dept.Title);
                    }
                }

                if (relationshipsProcessed > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Processed {Count} parent-child relationships", relationshipsProcessed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing parent-child relationships");
            }
        }

        private bool GovernmentDepartmentExists(int id)
        {
            return _context.GovernmentDepartments.Any(e => e.Id == id);
        }
    }

    // DTOs for GOV.UK API
    public class GovUkApiResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("results")]
        public List<GovUkOrganization> Results { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("pages")]
        public int Pages { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class GovUkOrganization
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("format")]
        public string Format { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("web_url")]
        public string WebUrl { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("analytics_identifier")]
        public string AnalyticsIdentifier { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("details")]
        public GovUkOrganizationDetails? Details { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("parent_organisations")]
        public List<GovUkOrganizationReference> ParentOrganisations { get; set; } = new();
        
        [System.Text.Json.Serialization.JsonPropertyName("child_organisations")]
        public List<GovUkOrganizationReference> ChildOrganisations { get; set; } = new();
    }

    public class GovUkOrganizationDetails
    {
        [System.Text.Json.Serialization.JsonPropertyName("abbreviation")]
        public string? Abbreviation { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("govuk_status")]
        public string? GovukStatus { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("closed_at")]
        public DateTime? ClosedAt { get; set; }
    }

    public class GovUkOrganizationReference
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [System.Text.Json.Serialization.JsonPropertyName("web_url")]
        public string WebUrl { get; set; } = string.Empty;
    }
}
