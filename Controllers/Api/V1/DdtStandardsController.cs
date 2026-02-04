using System.ComponentModel.DataAnnotations;
using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Authorization;
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
    /// Get DDT Standards by stage with optional filtering
    /// </summary>
    [HttpGet("by-stage")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetStandardsByStage(
        [FromQuery] string stage,
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] int? creatorId = null,
        [FromQuery] int? ownerId = null,
        [FromQuery] int? contactId = null,
        [FromQuery] bool? legalStandard = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "MISSING_STAGE",
                    message = "Stage parameter is required"
                }
            });
        }

        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        // Map view names to stage names (for backward compatibility)
        var stageMap = new Dictionary<string, string>
        {
            { "drafts", "Draft" },
            { "draft", "Draft" },
            { "in-review", "For Approval" },
            { "for-approval", "Awaiting Publication" },
            { "published", "Published" },
            { "unpublished", "Unpublished" }
        };

        var stageName = stageMap.ContainsKey(stage.ToLowerInvariant()) 
            ? stageMap[stage.ToLowerInvariant()] 
            : stage; // Use as-is if not in map

        // Validate stage name
        var validStages = new[] { "Draft", "For Approval", "Awaiting Publication", "Published", "Unpublished" };
        if (!validStages.Contains(stageName))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_STAGE",
                    message = $"Invalid stage. Valid stages are: {string.Join(", ", validStages)}"
                }
            });
        }

        // Base query for all standards in this stage
        var query = _context.DdtStandards
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Stage == stageName)
            .AsQueryable();

        // For Published stage, also require IsPublished == true
        if (stageName == "Published")
        {
            query = query.Where(s => s.IsPublished);
        }

        // Load related data for display
        query = query
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => 
                s.Title.Contains(search) || 
                (s.Summary != null && s.Summary.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Categories.Any(c => c.Category.Name == category));
        }

        if (creatorId.HasValue)
        {
            query = query.Where(s => s.CreatorUserId == creatorId.Value);
        }

        if (ownerId.HasValue)
        {
            query = query.Where(s => s.Owners.Any(o => o.UserId == ownerId.Value));
        }

        if (contactId.HasValue)
        {
            query = query.Where(s => s.Contacts.Any(c => c.UserId == contactId.Value));
        }

        if (legalStandard.HasValue)
        {
            query = query.Where(s => s.LegalStandard == legalStandard.Value);
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
            },
            stage = stageName
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
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
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

        return FormatStandardResponse(standard);
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
    /// Get a published DDT Standard by ID (only returns published versions)
    /// </summary>
    [HttpGet("by-id/{id}")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetPublishedStandardById(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.IsPublished && s.Stage == "Published");

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Published standard with ID {id} not found"
                }
            });
        }

        return FormatStandardResponse(standard);
    }

    /// <summary>
    /// Get a published DDT Standard by Legacy ID (only returns published versions)
    /// If multiple published standards exist with the same Legacy ID, returns the latest version
    /// </summary>
    [HttpGet("by-legacy-id/{legacyId}")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetPublishedStandardByLegacyId(string legacyId)
    {
        var standards = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .Where(s => s.LegacyId == legacyId && !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .ToListAsync();

        if (!standards.Any())
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Published standard with Legacy ID {legacyId} not found"
                }
            });
        }

        // If multiple published standards exist, return the one with the highest version
        var standard = standards.OrderByDescending(s => ParseVersion(s.Version)).First();

        return FormatStandardResponse(standard);
    }

    /// <summary>
    /// Get a published DDT Standard by Slug (only returns published versions)
    /// If multiple published standards exist with the same slug, returns the latest version
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetPublishedStandardBySlug(string slug)
    {
        var standards = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .Where(s => s.Slug == slug && !s.IsDeleted && s.IsPublished && s.Stage == "Published")
            .ToListAsync();

        if (!standards.Any())
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Published standard with slug {slug} not found"
                }
            });
        }

        // If multiple published standards exist, return the one with the highest version
        var standard = standards.OrderByDescending(s => ParseVersion(s.Version)).First();

        return FormatStandardResponse(standard);
    }

    /// <summary>
    /// Helper method to format standard response
    /// </summary>
    private IActionResult FormatStandardResponse(DdtStandard standard)
    {
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
            Categories = standard.Categories
                .Where(c => c.Category != null)
                .Select(c => new
                {
                    Id = c.Category!.Id,
                    Name = c.Category.Name,
                    Description = c.Category.Description,
                    SubCategories = standard.SubCategories
                        .Where(sc => sc.SubCategory != null && sc.SubCategory.CategoryId == c.Category.Id)
                        .Select(sc => new
                        {
                            Id = sc.SubCategory!.Id,
                            Name = sc.SubCategory.Name,
                            Description = sc.SubCategory.Description
                        })
                        .ToList()
                })
                .ToList(),
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
            LegacyReference = await GenerateLegacyReferenceAsync(),
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
    /// Submit a standard for review
    /// </summary>
    [HttpPost("{id}/SubmitForReview")]
    [RequireApiPermission("DdtStandards", "update")]
    public async Task<IActionResult> SubmitForReview(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Products)
            .Include(s => s.Exceptions)
            .Include(s => s.ParentStandard)
                .ThenInclude(p => p.Products)
            .Include(s => s.ParentStandard)
                .ThenInclude(p => p.Exceptions)
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

        if (standard.Stage != "Draft")
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "INVALID_STAGE",
                    message = "Only draft standards can be submitted for review."
                }
            });
        }

        // Only increment version if this is a new submission (not a resubmission after rejection)
        // and if it has a parent standard (was created from "Make a change")
        bool shouldIncrementVersion = standard.ParentStandardId.HasValue && 
                                      standard.ParentStandard != null &&
                                      !standard.Version.Contains("-resubmit");

        if (shouldIncrementVersion && standard.ParentStandard != null)
        {
            var parentStandard = standard.ParentStandard;
            var versionParts = (parentStandard.Version ?? string.Empty).Split('.');
            
            if (versionParts.Length == 3 && 
                int.TryParse(versionParts[0], out var major) &&
                int.TryParse(versionParts[1], out var minor) &&
                int.TryParse(versionParts[2], out var patch))
            {
                // Check what changed to determine version increment
                bool productsChanged = false;
                bool exceptionsChanged = false;
                bool descriptionChanged = false;
                bool otherChanged = false;

                // Compare products
                var parentProductIds = parentStandard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                var currentProductIds = standard.Products.Select(p => p.StandardProductId).OrderBy(x => x).ToList();
                if (!parentProductIds.SequenceEqual(currentProductIds))
                {
                    productsChanged = true;
                }

                // Compare exceptions
                var parentExceptionIds = await _context.DdtStandardExceptions
                    .Where(e => e.DdtStandardId == parentStandard.Id && e.Status == "Active")
                    .Select(e => e.Id)
                    .OrderBy(x => x)
                    .ToListAsync();
                var currentExceptionIds = await _context.DdtStandardExceptions
                    .Where(e => e.DdtStandardId == standard.Id && e.Status == "Active")
                    .Select(e => e.Id)
                    .OrderBy(x => x)
                    .ToListAsync();
                if (!parentExceptionIds.SequenceEqual(currentExceptionIds))
                {
                    exceptionsChanged = true;
                }

                // Compare description (Summary field)
                if (parentStandard.Summary != standard.Summary)
                {
                    descriptionChanged = true;
                }

                // Check for other changes
                if (parentStandard.Purpose != standard.Purpose ||
                    parentStandard.HowToMeet != standard.HowToMeet ||
                    parentStandard.Criteria != standard.Criteria ||
                    parentStandard.Governance != standard.Governance ||
                    parentStandard.LegalBasis != standard.LegalBasis ||
                    parentStandard.RelatedGuidance != standard.RelatedGuidance ||
                    parentStandard.Title != standard.Title)
                {
                    otherChanged = true;
                }

                // Increment version based on change type
                if (productsChanged || exceptionsChanged)
                {
                    major++;
                    minor = 0;
                    patch = 0;
                }
                else if (descriptionChanged)
                {
                    patch++;
                }
                else if (otherChanged)
                {
                    minor++;
                    patch = 0;
                }

                var newVersion = $"{major}.{minor}.{patch}";
                standard.PreviousVersion = standard.Version;
                standard.Version = newVersion;
            }
        }

        standard.Stage = "Under Review";
        standard.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Standard submitted for review successfully",
            standard = new
            {
                standard.Id,
                standard.Title,
                standard.Version,
                standard.Stage,
                standard.UpdatedAt
            }
        });
    }

    /// <summary>
    /// Get all published DDT Standards (convenience endpoint)
    /// This endpoint is equivalent to /api/v1/DdtStandards/by-stage?stage=published
    /// Public endpoint - no authentication required
    /// </summary>
    [HttpGet]
    [Route("/api/DdtStandards/Published")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublishedStandards(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] int? creatorId = null,
        [FromQuery] int? ownerId = null,
        [FromQuery] int? contactId = null,
        [FromQuery] bool? legalStandard = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        // Use the by-stage endpoint logic for consistency
        return await GetStandardsByStage("Published", search, category, creatorId, ownerId, contactId, legalStandard, page, pageSize);
    }

    /// <summary>
    /// Get a published DDT Standard by ID
    /// Only returns published standards - requires bearer token authentication
    /// </summary>
    [HttpGet]
    [Route("/api/DdtStandards/{id:int}")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetPublishedStandardByIdPublic(int id)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.CreatorUser)
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories).ThenInclude(c => c.Category)
            .Include(s => s.SubCategories).ThenInclude(sc => sc.SubCategory)
            .Include(s => s.Phases).ThenInclude(p => p.PhaseLookup)
            .Include(s => s.ValidationRules)
            .Include(s => s.Versions).ThenInclude(v => v.CreatedByUser)
            .Include(s => s.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted && s.IsPublished && s.Stage == "Published");

        if (standard == null)
        {
            return NotFound(new
            {
                error = new
                {
                    code = "NOT_FOUND",
                    message = $"Published standard with ID {id} not found"
                }
            });
        }

        return FormatStandardResponse(standard);
    }


    /// <summary>
    /// Get all exemptions (exceptions) for DDT Standards
    /// </summary>
    [HttpGet]
    [Route("/api/DdtStandards/Exceptions")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetExemptions(
        [FromQuery] int? standardId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.DdtStandardExceptions
            .AsNoTracking()
            .Include(e => e.DdtStandard)
            .Include(e => e.StandardProduct)
            .Include(e => e.GrantedByUser)
            .Include(e => e.CreatedByUser)
            .AsQueryable();

        // Apply filters
        if (standardId.HasValue)
        {
            query = query.Where(e => e.DdtStandardId == standardId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => 
                e.Title.Contains(search) || 
                (e.Description != null && e.Description.Contains(search)) ||
                (e.Reason != null && e.Reason.Contains(search)));
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var exemptions = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.Reason,
                e.Status,
                e.GrantedAt,
                e.ExpiresAt,
                e.Notes,
                Standard = new
                {
                    e.DdtStandard.Id,
                    e.DdtStandard.Title,
                    e.DdtStandard.Slug,
                    e.DdtStandard.Version
                },
                Product = e.StandardProduct != null ? new
                {
                    e.StandardProduct.Id,
                    e.StandardProduct.Name
                } : null,
                FipsProductId = e.FipsProductId,
                GrantedBy = e.GrantedByUser != null ? new
                {
                    e.GrantedByUser.Id,
                    e.GrantedByUser.Name,
                    e.GrantedByUser.Email
                } : null,
                CreatedBy = e.CreatedByUser != null ? new
                {
                    e.CreatedByUser.Id,
                    e.CreatedByUser.Name,
                    e.CreatedByUser.Email
                } : null,
                e.CreatedAt,
                e.UpdatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            data = exemptions,
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
    /// Get all approved products for DDT Standards
    /// </summary>
    [HttpGet]
    [Route("/api/DdtStandards/ApprovedProducts")]
    [RequireApiPermission("DdtStandards", "read")]
    public async Task<IActionResult> GetApprovedProducts(
        [FromQuery] string? search = null,
        [FromQuery] string? approvalStatus = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;
        if (page < 1) page = 1;

        var query = _context.StandardProducts
            .AsNoTracking()
            .Include(sp => sp.CreatedByUser)
            .Include(sp => sp.ReviewedByUser)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(sp => 
                sp.Name.Contains(search) || 
                (sp.Description != null && sp.Description.Contains(search)) ||
                (sp.Provider != null && sp.Provider.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(approvalStatus))
        {
            query = query.Where(sp => sp.ApprovalStatus == approvalStatus);
        }

        var totalRecords = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        var products = await query
            .OrderBy(sp => sp.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sp => new
            {
                sp.Id,
                sp.Name,
                sp.Description,
                sp.Provider,
                sp.Version,
                sp.ApprovalStatus,
                CreatedBy = sp.CreatedByUser != null ? new
                {
                    sp.CreatedByUser.Id,
                    sp.CreatedByUser.Name,
                    sp.CreatedByUser.Email
                } : null,
                ReviewedBy = sp.ReviewedByUser != null ? new
                {
                    sp.ReviewedByUser.Id,
                    sp.ReviewedByUser.Name,
                    sp.ReviewedByUser.Email
                } : null,
                sp.CreatedAt,
                sp.UpdatedAt
            })
            .ToListAsync();

        // Get standards for each product (only published)
        var productIds = products.Select(p => p.Id).ToList();
        var productStandards = await _context.DdtStandardProducts
            .AsNoTracking()
            .Include(dsp => dsp.DdtStandard)
            .Where(dsp => productIds.Contains(dsp.StandardProductId) && 
                         dsp.DdtStandard.IsPublished && 
                         dsp.DdtStandard.Stage == "Published" && 
                         !dsp.DdtStandard.IsDeleted)
            .ToListAsync();

        var productsWithStandards = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Provider,
            p.Version,
            p.ApprovalStatus,
            p.CreatedBy,
            p.ReviewedBy,
            p.CreatedAt,
            p.UpdatedAt,
            Standards = new
            {
                Approved = productStandards
                    .Where(ps => ps.StandardProductId == p.Id && ps.ProductType == "Approved")
                    .Select(ps => new
                    {
                        ps.DdtStandard.Id,
                        ps.DdtStandard.Title,
                        ps.DdtStandard.Slug,
                        ps.DdtStandard.Version
                    })
                    .OrderBy(s => s.Title)
                    .ToList(),
                Tolerated = productStandards
                    .Where(ps => ps.StandardProductId == p.Id && ps.ProductType == "Tolerated")
                    .Select(ps => new
                    {
                        ps.DdtStandard.Id,
                        ps.DdtStandard.Title,
                        ps.DdtStandard.Slug,
                        ps.DdtStandard.Version
                    })
                    .OrderBy(s => s.Title)
                    .ToList()
            }
        }).ToList();

        return Ok(new
        {
            data = productsWithStandards,
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
    /// Generate URL-friendly slug from title
    /// </summary>
    /// <summary>
    /// Generate LegacyReference for a standard
    /// Format: STD-{number}
    /// For new standards, find the highest number and increment
    /// </summary>
    private async Task<string> GenerateLegacyReferenceAsync()
    {
        // For new standards, find the highest LegacyReference number and increment
        var allLegacyReferences = await _context.DdtStandards
            .AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.LegacyReference) && s.LegacyReference.StartsWith("STD-"))
            .Select(s => s.LegacyReference)
            .ToListAsync();

        int maxNumber = 0;
        foreach (var refStr in allLegacyReferences)
        {
            if (refStr != null && refStr.StartsWith("STD-"))
            {
                var numberPart = refStr.Substring(4); // Remove "STD-"
                if (int.TryParse(numberPart, out var number) && number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        return $"STD-{maxNumber + 1}";
    }

    /// <summary>
    /// Parse semantic version string (e.g., "1.0.5") into a comparable tuple
    /// Returns (major, minor, patch) for sorting purposes
    /// </summary>
    private static (int major, int minor, int patch) ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return (0, 0, 0);
        }

        var parts = version.Trim().Split('.');
        if (parts.Length >= 3 &&
            int.TryParse(parts[0], out var major) &&
            int.TryParse(parts[1], out var minor) &&
            int.TryParse(parts[2], out var patch))
        {
            return (major, minor, patch);
        }
        else if (parts.Length == 2 &&
                 int.TryParse(parts[0], out major) &&
                 int.TryParse(parts[1], out minor))
        {
            return (major, minor, 0);
        }
        else if (parts.Length == 1 &&
                 int.TryParse(parts[0], out major))
        {
            return (major, 0, 0);
        }

        // If parsing fails, return (0, 0, 0) which will sort lowest
        return (0, 0, 0);
    }

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

