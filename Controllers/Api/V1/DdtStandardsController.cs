using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Api.V1;

/// <summary>
/// API controller for DDT Standards management.
/// Provides RESTful API endpoints for creating, reading, updating, and managing DDT Standards.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class DdtStandardsController : ControllerBase
{
    private readonly CompassDbContext _context;
    private readonly ILogger<DdtStandardsController> _logger;

    public DdtStandardsController(CompassDbContext context, ILogger<DdtStandardsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all DDT Standards with optional filtering
    /// </summary>
    [HttpGet]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetStandards(
        [FromQuery] string? search = null,
        [FromQuery] string? stage = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? published = null,
        [FromQuery] string? phase = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Where(s => !s.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => 
                s.Title.Contains(search) || 
                (s.Summary != null && s.Summary.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(s => s.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Categories.Any(c => c.Category.Name == category));
        }

        if (published.HasValue)
        {
            query = query.Where(s => s.IsPublished == published.Value);
        }

        if (!string.IsNullOrWhiteSpace(phase))
        {
            query = query.Where(s => s.Phases.Any(p => p.Enabled && p.PhaseLookup.Name == phase));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var standards = await query
            .OrderByDescending(s => s.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.StandardUuid,
                s.LegacyId,
                s.Title,
                s.Slug,
                s.Summary,
                s.Version,
                s.PreviousVersion,
                s.Stage,
                s.IsPublished,
                s.PublishedAt,
                s.FirstPublished,
                s.LastUpdated,
                s.DraftCreated,
                s.LegalStandard,
                s.LegalBasis,
                s.ValidityPeriod,
                s.GovernanceApproval,
                s.IsModified,
                Creator = s.CreatorUser != null ? new
                {
                    s.CreatorUser.Id,
                    s.CreatorUser.Name,
                    s.CreatorUser.Email
                } : null,
                Owners = s.Owners.Select(o => new
                {
                    o.User.Id,
                    o.User.Name,
                    o.User.Email,
                    o.Role
                }).ToList(),
                Contacts = s.Contacts.Select(c => new
                {
                    c.User.Id,
                    c.User.Name,
                    c.User.Email
                }).ToList(),
                Categories = s.Categories.Select(c => c.Category.Name).ToList(),
                SubCategories = s.SubCategories.Select(sc => sc.SubCategory.Name).ToList(),
                Phases = s.Phases.Where(p => p.Enabled).Select(p => new
                {
                    p.PhaseLookup.Id,
                    p.PhaseLookup.Name
                }).ToList(),
                ValidationRules = s.ValidationRules.Where(r => r.IsActive).Select(r => new
                {
                    r.RuleId,
                    r.Name,
                    r.Type,
                    r.ValidationType,
                    r.Validator,
                    r.Severity
                }).ToList(),
                s.CreatedAt,
                s.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = standards,
            pagination = new
            {
                currentPage = page,
                pageSize,
                totalPages,
                totalRecords
            }
        });
    }

    /// <summary>
    /// Get a single DDT Standard by ID
    /// </summary>
    [HttpGet("{id}")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetStandard(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Standard with ID {id} not found"
                }
            });
        }

        return Ok(new
        {
            standard.Id,
            standard.StandardUuid,
            standard.LegacyId,
            standard.Title,
            standard.Slug,
            standard.Summary,
            standard.Purpose,
            standard.HowToMeet,
            standard.Governance,
            standard.Version,
            standard.PreviousVersion,
            standard.Stage,
            standard.IsPublished,
            standard.PublishedAt,
            standard.FirstPublished,
            standard.LastUpdated,
            standard.DraftCreated,
            standard.LegalStandard,
            standard.LegalBasis,
            standard.ValidityPeriod,
            standard.RelatedGuidance,
            standard.GovernanceApproval,
            standard.IsModified,
            Creator = standard.CreatorUser != null ? new
            {
                standard.CreatorUser.Id,
                standard.CreatorUser.Name,
                standard.CreatorUser.Email
            } : null,
            Owners = standard.Owners.Select(o => new
            {
                o.User.Id,
                o.User.Name,
                o.User.Email,
                o.Role
            }).ToList(),
            Contacts = standard.Contacts.Select(c => new
            {
                c.User.Id,
                c.User.Name,
                c.User.Email
            }).ToList(),
            Categories = standard.Categories.Select(c => c.Category.Name).ToList(),
            SubCategories = standard.SubCategories.Select(sc => sc.SubCategory.Name).ToList(),
            Phases = standard.Phases.Where(p => p.Enabled).Select(p => new
            {
                p.PhaseLookup.Id,
                p.PhaseLookup.Name,
                p.PhaseLookup.Description
            }).ToList(),
            ValidationRules = standard.ValidationRules.Where(r => r.IsActive).Select(r => new
            {
                r.Id,
                r.RuleId,
                r.Name,
                r.Description,
                r.Type,
                r.Priority,
                r.Category,
                r.ValidationType,
                r.Validator,
                r.Config,
                r.Severity
            }).ToList(),
            Versions = standard.Versions.OrderByDescending(v => v.CreatedAt).Select(v => new
            {
                v.Id,
                v.VersionNumber,
                v.PreviousVersion,
                v.VersionType,
                v.ChangeSummary,
                v.IsBreakingChange,
                v.Status,
                v.CreatedAt,
                v.PublishedAt,
                CreatedBy = v.CreatedByUser != null ? new
                {
                    v.CreatedByUser.Id,
                    v.CreatedByUser.Name,
                    v.CreatedByUser.Email
                } : null
            }).ToList(),
            Comments = standard.Comments.OrderByDescending(c => c.CreatedAt).Select(c => new
            {
                c.Id,
                c.Title,
                c.Comments,
                c.CommentType,
                c.CreatedAt,
                User = new
                {
                    c.User.Id,
                    c.User.Name,
                    c.User.Email
                }
            }).ToList(),
            standard.CreatedAt,
            standard.UpdatedAt
        });
    }

    /// <summary>
    /// Get a DDT Standard by UUID
    /// </summary>
    [HttpGet("uuid/{uuid}")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetStandardByUuid(string uuid)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .FirstOrDefaultAsync(s => s.StandardUuid == uuid && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Standard with UUID {uuid} not found"
                }
            });
        }

        return await GetStandard(standard.Id);
    }

    /// <summary>
    /// Create a new DDT Standard
    /// </summary>
    [HttpPost]
    [RequireApiPermission("DdtStandards", "create")]
    public async Task<IActionResult> CreateStandard([FromBody] DdtStandardCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "VALIDATION_ERROR",
                    message = "Invalid request data",
                    details = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                }
            });
        }

        // Check for duplicate title
        var existing = await _context.DdtStandards
            .FirstOrDefaultAsync(s => s.Title == dto.Title && !s.IsDeleted);
        
        if (existing != null)
        {
            return Conflict(new
            {
                error = new
                {
                    code = "DUPLICATE_TITLE",
                    message = "A standard with this title already exists"
                }
            });
        }

        var standard = new DdtStandard
        {
            StandardUuid = Guid.NewGuid().ToString(),
            Title = dto.Title.Trim(),
            Slug = GenerateSlug(dto.Title),
            Summary = dto.Summary,
            Purpose = dto.Purpose,
            HowToMeet = dto.HowToMeet,
            Governance = dto.Governance,
            LegalBasis = dto.LegalBasis,
            LegalStandard = dto.LegalStandard,
            ValidityPeriod = dto.ValidityPeriod,
            RelatedGuidance = dto.RelatedGuidance,
            Stage = "Draft",
            Version = "0.1.0",
            DraftCreated = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (dto.CreatorUserId.HasValue)
        {
            standard.CreatorUserId = dto.CreatorUserId.Value;
        }

        _context.DdtStandards.Add(standard);
        await _context.SaveChangesAsync();

        // Add categories (list of category IDs)
        if (dto.Categories != null && dto.Categories.Any())
        {
            foreach (var categoryIdStr in dto.Categories)
            {
                if (int.TryParse(categoryIdStr, out var categoryId))
                {
                    var categoryExists = await _context.StandardCategories.AnyAsync(c => c.Id == categoryId);
                    if (categoryExists)
                    {
                        _context.DdtStandardCategories.Add(new DdtStandardCategory
                        {
                            DdtStandardId = standard.Id,
                            CategoryId = categoryId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        // Add sub-categories (list of sub-category IDs)
        if (dto.SubCategories != null && dto.SubCategories.Any())
        {
            foreach (var subCategoryIdStr in dto.SubCategories)
            {
                if (int.TryParse(subCategoryIdStr, out var subCategoryId))
                {
                    var subCategoryExists = await _context.StandardSubCategories.AnyAsync(sc => sc.Id == subCategoryId);
                    if (subCategoryExists)
                    {
                        _context.DdtStandardSubCategories.Add(new DdtStandardSubCategory
                        {
                            DdtStandardId = standard.Id,
                            SubCategoryId = subCategoryId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        // Add phases
        if (dto.PhaseIds != null && dto.PhaseIds.Any())
        {
            foreach (var phaseId in dto.PhaseIds)
            {
                _context.DdtStandardPhases.Add(new DdtStandardPhase
                {
                    DdtStandardId = standard.Id,
                    PhaseLookupId = phaseId,
                    Enabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Add owners
        if (dto.OwnerUserIds != null && dto.OwnerUserIds.Any())
        {
            foreach (var userId in dto.OwnerUserIds)
            {
                _context.DdtStandardOwners.Add(new DdtStandardOwner
                {
                    DdtStandardId = standard.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Add contacts
        if (dto.ContactUserIds != null && dto.ContactUserIds.Any())
        {
            foreach (var userId in dto.ContactUserIds)
            {
                _context.DdtStandardContacts.Add(new DdtStandardContact
                {
                    DdtStandardId = standard.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStandard), new { id = standard.Id }, new
        {
            standard.Id,
            standard.StandardUuid,
            standard.Title,
            standard.Slug,
            standard.Version,
            standard.Stage,
            standard.CreatedAt
        });
    }

    /// <summary>
    /// Update a DDT Standard
    /// </summary>
    [HttpPut("{id}")]
    [RequireApiPermission("DdtStandards", "update")]
    public async Task<IActionResult> UpdateStandard(int id, [FromBody] DdtStandardUpdateDto dto)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases)
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Standard with ID {id} not found"
                }
            });
        }

        // Update basic fields
        if (dto.Title != null)
        {
            standard.Title = dto.Title.Trim();
            standard.Slug = GenerateSlug(dto.Title);
            standard.IsModified = true;
        }

        if (dto.Summary != null) standard.Summary = dto.Summary;
        if (dto.Purpose != null) standard.Purpose = dto.Purpose;
        if (dto.HowToMeet != null) standard.HowToMeet = dto.HowToMeet;
        if (dto.Governance != null) standard.Governance = dto.Governance;
        if (dto.LegalBasis != null) standard.LegalBasis = dto.LegalBasis;
        if (dto.ValidityPeriod.HasValue) standard.ValidityPeriod = dto.ValidityPeriod;
        if (dto.RelatedGuidance != null) standard.RelatedGuidance = dto.RelatedGuidance;
        if (dto.LegalStandard.HasValue) standard.LegalStandard = dto.LegalStandard.Value;

        standard.LastUpdated = DateTime.UtcNow;
        standard.UpdatedAt = DateTime.UtcNow;

        // Update categories (list of category IDs)
        if (dto.Categories != null)
        {
            _context.DdtStandardCategories.RemoveRange(standard.Categories);
            foreach (var categoryIdStr in dto.Categories)
            {
                if (int.TryParse(categoryIdStr, out var categoryId))
                {
                    var categoryExists = await _context.StandardCategories.AnyAsync(c => c.Id == categoryId);
                    if (categoryExists)
                    {
                        _context.DdtStandardCategories.Add(new DdtStandardCategory
                        {
                            DdtStandardId = standard.Id,
                            CategoryId = categoryId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        // Update sub-categories (list of sub-category IDs)
        if (dto.SubCategories != null)
        {
            _context.DdtStandardSubCategories.RemoveRange(standard.SubCategories);
            foreach (var subCategoryIdStr in dto.SubCategories)
            {
                if (int.TryParse(subCategoryIdStr, out var subCategoryId))
                {
                    var subCategoryExists = await _context.StandardSubCategories.AnyAsync(sc => sc.Id == subCategoryId);
                    if (subCategoryExists)
                    {
                        _context.DdtStandardSubCategories.Add(new DdtStandardSubCategory
                        {
                            DdtStandardId = standard.Id,
                            SubCategoryId = subCategoryId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        // Update phases
        if (dto.PhaseIds != null)
        {
            _context.DdtStandardPhases.RemoveRange(standard.Phases);
            foreach (var phaseId in dto.PhaseIds)
            {
                _context.DdtStandardPhases.Add(new DdtStandardPhase
                {
                    DdtStandardId = standard.Id,
                    PhaseLookupId = phaseId,
                    Enabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Update owners
        if (dto.OwnerUserIds != null)
        {
            _context.DdtStandardOwners.RemoveRange(standard.Owners);
            foreach (var userId in dto.OwnerUserIds)
            {
                _context.DdtStandardOwners.Add(new DdtStandardOwner
                {
                    DdtStandardId = standard.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        // Update contacts
        if (dto.ContactUserIds != null)
        {
            _context.DdtStandardContacts.RemoveRange(standard.Contacts);
            foreach (var userId in dto.ContactUserIds)
            {
                _context.DdtStandardContacts.Add(new DdtStandardContact
                {
                    DdtStandardId = standard.Id,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Standard updated successfully" });
    }

    /// <summary>
    /// Delete (soft delete) a DDT Standard
    /// </summary>
    [HttpDelete("{id}")]
    [RequireApiPermission("DdtStandards", "delete")]
    public async Task<IActionResult> DeleteStandard(int id)
    {
        var standard = await _context.DdtStandards.FindAsync(id);
        if (standard == null || standard.IsDeleted)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Standard with ID {id} not found"
                }
            });
        }

        standard.IsDeleted = true;
        standard.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Get validation rules for a standard
    /// </summary>
    [HttpGet("{id}/validation-rules")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetValidationRules(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.ValidationRules)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Standard with ID {id} not found"
                }
            });
        }

        var rules = standard.ValidationRules
            .Where(r => r.IsActive)
            .Select(r => new
            {
                r.Id,
                r.RuleId,
                r.Name,
                r.Description,
                r.Type,
                r.Priority,
                r.Category,
                r.ValidationType,
                r.Validator,
                r.Config,
                r.Severity,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToList();

        return Ok(new { data = rules });
    }

    /// <summary>
    /// Generate URL-friendly slug from title
    /// </summary>
    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var slug = title.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", " ").Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

        return slug;
    }
}

/// <summary>
/// DTO for creating a DDT Standard
/// </summary>
public class DdtStandardCreateDto
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Summary { get; set; }

    public string? Purpose { get; set; }
    public string? HowToMeet { get; set; }
    public string? Governance { get; set; }
    public string? LegalBasis { get; set; }
    public bool LegalStandard { get; set; } = false;
    public int? ValidityPeriod { get; set; }
    public string? RelatedGuidance { get; set; }
    public int? CreatorUserId { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? SubCategories { get; set; }
    public List<int>? PhaseIds { get; set; }
    public List<int>? OwnerUserIds { get; set; }
    public List<int>? ContactUserIds { get; set; }
}

/// <summary>
/// DTO for updating a DDT Standard
/// </summary>
public class DdtStandardUpdateDto
{
    [MaxLength(500)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Summary { get; set; }

    public string? Purpose { get; set; }
    public string? HowToMeet { get; set; }
    public string? Governance { get; set; }
    public string? LegalBasis { get; set; }
    public bool? LegalStandard { get; set; }
    public int? ValidityPeriod { get; set; }
    public string? RelatedGuidance { get; set; }
    public List<string>? Categories { get; set; }
    public List<string>? SubCategories { get; set; }
    public List<int>? PhaseIds { get; set; }
    public List<int>? OwnerUserIds { get; set; }
    public List<int>? ContactUserIds { get; set; }
}

