using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Compass.Data;
using Compass.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Compass.Attributes;
using Compass.Services;
using System.Security.Claims;

namespace Compass.Controllers.Admin;

/// <summary>
/// Admin controller for managing Standards Configuration (Categories and Sub-categories)
/// </summary>
[Authorize]
[RequireAdmin]
[Area("Admin")]
public class StandardsConfigController : Controller
{
    private readonly CompassDbContext _context;
    private readonly ILogger<StandardsConfigController> _logger;
    private readonly IStandardsCmsApiService _standardsCmsApiService;

    public StandardsConfigController(
        CompassDbContext context, 
        ILogger<StandardsConfigController> logger,
        IStandardsCmsApiService standardsCmsApiService)
    {
        _context = context;
        _logger = logger;
        _standardsCmsApiService = standardsCmsApiService;
    }

    /// <summary>
    /// Index - Categories management
    /// </summary>
    public async Task<IActionResult> Index(int? categoryId)
    {
        var categories = await _context.StandardCategories
            .Include(c => c.SubCategories)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.SelectedCategoryId = categoryId;

        if (categoryId.HasValue)
        {
            var category = await _context.StandardCategories
                .Include(c => c.SubCategories.OrderBy(sc => sc.SortOrder).ThenBy(sc => sc.Name))
                .FirstOrDefaultAsync(c => c.Id == categoryId.Value);
            ViewBag.SelectedCategory = category;
        }

        return View();
    }

    /// <summary>
    /// Create category
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string? description, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Category name is required.";
            return RedirectToAction(nameof(Index));
        }

        var category = new StandardCategory
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StandardCategories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{category.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Update category
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int id, string name, string? description, int sortOrder, bool isActive)
    {
        var category = await _context.StandardCategories.FindAsync(id);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToAction(nameof(Index));
        }

        category.Name = name.Trim();
        category.Description = description?.Trim();
        category.SortOrder = sortOrder;
        category.IsActive = isActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{category.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Delete category
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.StandardCategories
            .Include(c => c.SubCategories)
            .Include(c => c.DdtStandardCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToAction(nameof(Index));
        }

        if (category.DdtStandardCategories.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because it is used by {category.DdtStandardCategories.Count} standard(s).";
            return RedirectToAction(nameof(Index));
        }

        if (category.SubCategories.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete category '{category.Name}' because it has {category.SubCategories.Count} sub-category(ies). Please delete sub-categories first.";
            return RedirectToAction(nameof(Index));
        }

        _context.StandardCategories.Remove(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Category '{category.Name}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Create sub-category
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubCategory(int categoryId, string name, string? description, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Sub-category name is required.";
            return RedirectToAction(nameof(Index), new { categoryId });
        }

        var category = await _context.StandardCategories.FindAsync(categoryId);
        if (category == null)
        {
            TempData["ErrorMessage"] = "Category not found.";
            return RedirectToAction(nameof(Index));
        }

        var subCategory = new StandardSubCategory
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.StandardSubCategories.Add(subCategory);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Sub-category '{subCategory.Name}' created successfully.";
        return RedirectToAction(nameof(Index), new { categoryId });
    }

    /// <summary>
    /// Update sub-category
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSubCategory(int id, string name, string? description, int sortOrder, bool isActive)
    {
        var subCategory = await _context.StandardSubCategories.FindAsync(id);
        if (subCategory == null)
        {
            TempData["ErrorMessage"] = "Sub-category not found.";
            return RedirectToAction(nameof(Index), new { categoryId = subCategory?.CategoryId });
        }

        subCategory.Name = name.Trim();
        subCategory.Description = description?.Trim();
        subCategory.SortOrder = sortOrder;
        subCategory.IsActive = isActive;
        subCategory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Sub-category '{subCategory.Name}' updated successfully.";
        return RedirectToAction(nameof(Index), new { categoryId = subCategory.CategoryId });
    }

    /// <summary>
    /// Delete sub-category
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubCategory(int id)
    {
        var subCategory = await _context.StandardSubCategories
            .Include(sc => sc.DdtStandardSubCategories)
            .FirstOrDefaultAsync(sc => sc.Id == id);

        if (subCategory == null)
        {
            TempData["ErrorMessage"] = "Sub-category not found.";
            return RedirectToAction(nameof(Index));
        }

        if (subCategory.DdtStandardSubCategories.Any())
        {
            TempData["ErrorMessage"] = $"Cannot delete sub-category '{subCategory.Name}' because it is used by {subCategory.DdtStandardSubCategories.Count} standard(s).";
            return RedirectToAction(nameof(Index), new { categoryId = subCategory.CategoryId });
        }

        var categoryId = subCategory.CategoryId;
        _context.StandardSubCategories.Remove(subCategory);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Sub-category '{subCategory.Name}' deleted successfully.";
        return RedirectToAction(nameof(Index), new { categoryId });
    }

    /// <summary>
    /// Seed categories and sub-categories from DfE Standards Manual
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedCategories()
    {
        try
        {
            var seedData = GetSeedData();
            var sortOrder = 0;

            foreach (var categoryData in seedData)
            {
                // Check if category exists
                var existingCategory = await _context.StandardCategories
                    .FirstOrDefaultAsync(c => c.Name == categoryData.Name);

                StandardCategory category;
                if (existingCategory == null)
                {
                    category = new StandardCategory
                    {
                        Name = categoryData.Name,
                        Description = categoryData.Description,
                        SortOrder = sortOrder++,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.StandardCategories.Add(category);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    category = existingCategory;
                }

                // Add sub-categories
                var subSortOrder = 0;
                foreach (var subCategoryData in categoryData.SubCategories)
                {
                    var existingSubCategory = await _context.StandardSubCategories
                        .FirstOrDefaultAsync(sc => sc.CategoryId == category.Id && sc.Name == subCategoryData.Name);

                    if (existingSubCategory == null)
                    {
                        var subCategory = new StandardSubCategory
                        {
                            CategoryId = category.Id,
                            Name = subCategoryData.Name,
                            Description = subCategoryData.Description,
                            SortOrder = subSortOrder++,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.StandardSubCategories.Add(subCategory);
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Categories and sub-categories seeded successfully from DfE Standards Manual.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding categories");
            TempData["ErrorMessage"] = "An error occurred while seeding categories.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Get seed data from DfE Standards Manual categories
    /// Source: https://create-and-manage-standards.education.gov.uk/guidance/categories
    /// </summary>
    private List<CategorySeedData> GetSeedData()
    {
        return new List<CategorySeedData>
        {
            new("Architecture", "Standards related to the design and structure of systems, including enterprise architecture, systems integration, and security architecture.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Business architecture", "Defines and aligns the strategic vision, processes, and organisational structure to achieve business goals effectively."),
                    new("Data architecture", "Focuses on designing and managing the structure, storage, and integration of data to support business needs."),
                    new("Enterprise architecture", "Provides a comprehensive framework for aligning IT strategy with business objectives, ensuring consistency across systems and services."),
                    new("Network architecture", "Designs and implements communication networks, ensuring reliable, scalable, and secure connectivity."),
                    new("Security architecture", "Develops frameworks and systems to protect an organisation's assets from cyber threats and ensure compliance with security standards."),
                    new("Solution architecture", "Designs specific technical solutions to address business requirements, integrating systems and technologies effectively."),
                    new("Technical architecture", "Defines the technical standards, tools, and infrastructure required to deliver IT solutions efficiently and consistently.")
                }
            },
            new("Benefits", "Standards defining the management and realisation of benefits from projects and programmes."),
            new("Commercial", "Standards governing procurement, supplier management, and contract negotiations."),
            new("Continuity", "Standards for ensuring business continuity, disaster recovery, and resilience planning."),
            new("Data management", "Standards governing data management, including data quality, governance, analysis, and security.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Analytics engineering", "Focuses on designing, building, and maintaining analytics systems and pipelines to process and analyse data efficiently."),
                    new("Data analysis", "Involves examining data sets to identify trends, draw conclusions, and support decision-making."),
                    new("Data engineering", "Concerned with building and optimising systems for collecting, storing, and analysing data at scale."),
                    new("Data ethicist", "Ensures ethical considerations in the collection, storage, and use of data, addressing issues such as privacy, consent, and bias."),
                    new("Data governance", "Focuses on establishing and maintaining policies, processes, and standards to ensure the integrity, security, and quality of data."),
                    new("Data science", "Combines programming, statistics, and domain expertise to extract actionable insights from complex data sets."),
                    new("Machine learning", "Develops and applies algorithms that allow systems to learn from and make predictions or decisions based on data."),
                    new("Performance analysis", "Involves evaluating and measuring the effectiveness of systems, services, and processes to optimise performance and outcomes.")
                }
            },
            new("Finance", "Standards for financial planning, management, reporting, and governance."),
            new("Information and Data Security", "Standards for the security and management of data, its control, retention, and disposal.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Archiving and record management", "Manages the storage, retrieval, and preservation of records and archives to ensure accessibility and compliance with regulations."),
                    new("Classification", "Applies categorisation and labelling systems to organise information and ensure appropriate access and security levels."),
                    new("Data ethics", "Ensures the ethical use of data by addressing issues such as bias, consent, privacy, and accountability in data handling."),
                    new("Data handling", "Manages the proper collection, storage, processing, and sharing of data to maintain quality and compliance."),
                    new("Data inventory", "Maintains a comprehensive register of an organisation's data assets to support governance, compliance, and usage optimisation."),
                    new("Data protection", "Ensures that personal and sensitive data is safeguarded in compliance with laws and standards, such as GDPR."),
                    new("Data retention and disposal", "Manages the lifecycle of data, ensuring it is retained as long as necessary and disposed of securely when no longer required."),
                    new("Encryption", "Implements encryption techniques to secure data and protect it from unauthorised access or breaches."),
                    new("Personal data", "Focuses on the management and protection of personal information to comply with data privacy regulations and uphold user trust."),
                    new("Risk management", "Identifies, assesses, and mitigates risks associated with data to safeguard organisational assets and ensure resilience.")
                }
            },
            new("IT Operations", "Standards for the management and operation of IT systems, covering areas such as infrastructure, cloud services, and system monitoring.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Application operations", "Manages the day-to-day running, monitoring, and optimisation of software applications to ensure continuity and performance."),
                    new("Business relationship", "Focuses on fostering effective communication and alignment between IT services and business stakeholders to meet organisational goals."),
                    new("Change and release management", "Oversees the controlled planning, testing, and deployment of IT changes to minimise disruption and maintain system stability."),
                    new("Command and control", "Coordinates and directs IT operations, particularly in high-pressure scenarios, to ensure swift resolution of issues and effective incident response."),
                    new("End user computing", "Provides and manages the tools, devices, and support required for end users to perform their roles efficiently."),
                    new("IT service management", "Delivers and supports IT services based on established frameworks and best practices, such as ITIL, to ensure quality and efficiency."),
                    new("Incident management", "Handles unplanned disruptions to IT services, aiming to restore normal operations as quickly as possible and minimise impact."),
                    new("Infrastructure operations", "Manages and maintains the hardware, networks, and systems underpinning IT services, ensuring reliability and scalability."),
                    new("Problem management", "Identifies and addresses the root causes of incidents to prevent recurrence and improve overall system reliability."),
                    new("Service desk", "Provides a single point of contact for users to report issues, request services, and receive assistance with IT systems."),
                    new("Service transition", "Manages the process of deploying new or changed services into operational use, ensuring smooth handover and minimal disruption.")
                }
            },
            new("Product and Delivery", "Standards related to product management, agile delivery, and project management frameworks.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Business analysis", "Identifies business needs, analyses processes, and proposes solutions to help organisations achieve their objectives effectively."),
                    new("Delivery management", "Ensures the successful delivery of projects and services by coordinating teams, managing risks, and maintaining focus on outcomes."),
                    new("Portfolio management", "Oversees and prioritises a collection of projects and programmes to align with organisational strategies and maximise value."),
                    new("Product management", "Focuses on the lifecycle of a product, from conception to delivery, ensuring it meets user needs and business goals."),
                    new("Programme delivery", "Manages the delivery of complex programmes, ensuring alignment with organisational objectives and successful outcomes across multiple projects."),
                    new("Service ownership", "Accountable for the end-to-end delivery, quality, and performance of a service, ensuring it meets user and business needs.")
                }
            },
            new("QA and Development", "Standards focusing on software development, testing, quality assurance, and continuous integration/continuous delivery (CI/CD) practices.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("DevOps", "Combines software development and IT operations to streamline delivery, automate workflows, and improve system reliability."),
                    new("Frontend development", "Focuses on creating user interfaces and ensuring a seamless user experience through web and application development."),
                    new("Quality Assurance Testing", "Ensures that software meets specified standards and functions as intended through systematic testing and defect identification."),
                    new("Software development", "Designs, codes, and maintains software applications to meet user needs and organisational requirements."),
                    new("Test engineering", "Develops and implements testing frameworks, tools, and processes to ensure the robustness and reliability of software systems."),
                    new("Test management", "Oversees the planning, execution, and reporting of testing activities, ensuring alignment with project timelines and quality standards.")
                }
            },
            new("Risk", "Standards for risk management, including identifying, assessing, and mitigating risks in projects and operations."),
            new("Security", "Standards for ensuring the physical and cyber security of organisational assets and infrastructure."),
            new("Sustainability", "Standards promoting sustainable practices, including environmental responsibility and energy efficiency."),
            new("Technical", "Standards for the technical implementation of products and services.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Security managements", "Ensuring platforms and services are secure."),
                    new("Technical management", "Ensures platforms and services meet technical requirements.")
                }
            },
            new("User-Centred Design and Accessibility", "Standards around user experience, accessibility, user research, and service design.")
            {
                SubCategories = new List<SubCategorySeedData>
                {
                    new("Accessibility", "Responsible for ensuring that digital services are accessible to all users, including those with disabilities."),
                    new("Content design", "Ensures that content we are creating and managing content is clear, accurate, and easy to understand."),
                    new("Interaction design", "How we design user interfaces and interactions, including styles, components, user journeys and prototyping."),
                    new("Service design", "Designing services that are easy to use, accessible, and meet user needs."),
                    new("User research", "Standards to ensure high levels of quality, ethics and safety in user research activities.")
                }
            }
        };
    }

    private class CategorySeedData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<SubCategorySeedData> SubCategories { get; set; } = new();

        public CategorySeedData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    private class SubCategorySeedData
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public SubCategorySeedData(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Migrate categories from CMS
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MigrateCategoriesFromCms(bool skipExisting = true)
    {
        try
        {
            _logger.LogInformation("Starting migration of categories from CMS");

            var cmsCategories = await _standardsCmsApiService.GetCategoriesAsync(cacheDuration: null);

            if (!cmsCategories.Any())
            {
                TempData["ErrorMessage"] = "No categories found in CMS.";
                return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
            }

            int migrated = 0;
            int skipped = 0;
            int errors = 0;
            var sortOrder = 0;

            foreach (var cmsCategory in cmsCategories)
            {
                try
                {
                    if (skipExisting)
                    {
                        var existing = await _context.StandardCategories
                            .FirstOrDefaultAsync(c => c.Name == cmsCategory.Title);
                        if (existing != null)
                        {
                            _logger.LogInformation("Skipping category '{Name}' - already exists", cmsCategory.Title);
                            skipped++;
                            continue;
                        }
                    }

                    var category = new StandardCategory
                    {
                        Name = cmsCategory.Title,
                        Description = cmsCategory.Description,
                        SortOrder = sortOrder++,
                        IsActive = cmsCategory.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.StandardCategories.Add(category);
                    await _context.SaveChangesAsync();
                    migrated++;
                    _logger.LogInformation("Migrated category '{Name}' (ID: {Id})", category.Name, category.Id);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error migrating category '{Title}'", cmsCategory.Title);
                }
            }

            var message = $"Categories migration completed: {migrated} migrated, {skipped} skipped, {errors} errors.";
            if (errors > 0)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during categories migration");
            TempData["ErrorMessage"] = $"Categories migration failed: {ex.Message}";
            return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
        }
    }

    /// <summary>
    /// Migrate sub-categories from CMS
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MigrateSubCategoriesFromCms(bool skipExisting = true)
    {
        try
        {
            _logger.LogInformation("Starting migration of sub-categories from CMS");

            // Get all published standards to extract unique sub-category IDs
            var standards = await _standardsCmsApiService.GetStandardsAsync(
                published: true,
                cacheDuration: null
            );

            // Collect unique sub-category IDs
            var subCategoryIds = new HashSet<int>();
            foreach (var standard in standards)
            {
                if (standard.SubCategories != null)
                {
                    foreach (var subCat in standard.SubCategories)
                    {
                        subCategoryIds.Add(subCat.Id);
                    }
                }
            }

            if (!subCategoryIds.Any())
            {
                TempData["ErrorMessage"] = "No sub-categories found in CMS standards.";
                return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
            }

            _logger.LogInformation("Found {Count} unique sub-category IDs, fetching from CMS with category relations", subCategoryIds.Count);

            // Fetch sub-categories directly from CMS API with category relations populated
            var cmsSubCategories = await _standardsCmsApiService.GetSubCategoriesByIdsAsync(
                subCategoryIds.ToList(),
                cacheDuration: null
            );

            if (!cmsSubCategories.Any())
            {
                TempData["ErrorMessage"] = "No sub-categories retrieved from CMS API. Check logs for details.";
                return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
            }

            _logger.LogInformation("Retrieved {Count} sub-categories from CMS API", cmsSubCategories.Count);

            int migrated = 0;
            int skipped = 0;
            int errors = 0;

            foreach (var cmsSubCategory in cmsSubCategories)
            {
                try
                {
                    var cmsCategory = cmsSubCategory.Category;

                    if (cmsCategory == null)
                    {
                        _logger.LogWarning("Sub-category '{Title}' (ID: {Id}) has no category relation, skipping", 
                            cmsSubCategory.Title, cmsSubCategory.Id);
                        skipped++;
                        continue;
                    }

                    // Find the category in our database
                    var category = await _context.StandardCategories
                        .FirstOrDefaultAsync(c => c.Name == cmsCategory.Title);

                    if (category == null)
                    {
                        _logger.LogWarning("Category '{CategoryName}' not found for sub-category '{SubCategoryName}'. Please migrate categories first.", 
                            cmsCategory.Title, cmsSubCategory.Title);
                        skipped++;
                        continue;
                    }

                    if (skipExisting)
                    {
                        var existing = await _context.StandardSubCategories
                            .FirstOrDefaultAsync(sc => sc.CategoryId == category.Id && sc.Name == cmsSubCategory.Title);
                        if (existing != null)
                        {
                            _logger.LogInformation("Skipping sub-category '{Name}' - already exists", cmsSubCategory.Title);
                            skipped++;
                            continue;
                        }
                    }

                    var subCategory = new StandardSubCategory
                    {
                        CategoryId = category.Id,
                        Name = cmsSubCategory.Title,
                        Description = null, // CMS doesn't provide description for sub-categories
                        SortOrder = 0,
                        IsActive = cmsSubCategory.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.StandardSubCategories.Add(subCategory);
                    await _context.SaveChangesAsync();
                    migrated++;
                    _logger.LogInformation("Migrated sub-category '{Name}' under category '{CategoryName}' (ID: {Id})", 
                        subCategory.Name, category.Name, subCategory.Id);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error migrating sub-category '{Title}' (ID: {Id})", 
                        cmsSubCategory.Title, cmsSubCategory.Id);
                }
            }

            var message = $"Sub-categories migration completed: {migrated} migrated, {skipped} skipped, {errors} errors.";
            if (errors > 0)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            _logger.LogInformation("Sub-categories migration completed: {Migrated} migrated, {Skipped} skipped, {Errors} errors", 
                migrated, skipped, errors);

            return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sub-categories migration");
            TempData["ErrorMessage"] = $"Sub-categories migration failed: {ex.Message}";
            return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
        }
    }

    /// <summary>
    /// Migrate products from CMS (from approved and tolerated products in standards)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MigrateProductsFromCms(bool skipExisting = true)
    {
        try
        {
            _logger.LogInformation("Starting migration of products from CMS");

            // Get all published standards to extract products
            var standards = await _standardsCmsApiService.GetStandardsAsync(
                published: true,
                cacheDuration: null
            );

            // Extract unique products
            var productMap = new Dictionary<string, StandardProductDto>();

            foreach (var standard in standards)
            {
                if (standard.ApprovedProducts != null)
                {
                    foreach (var product in standard.ApprovedProducts)
                    {
                        var key = product.Title.ToLowerInvariant();
                        if (!productMap.ContainsKey(key))
                        {
                            productMap[key] = product;
                        }
                    }
                }

                if (standard.ToleratedProducts != null)
                {
                    foreach (var product in standard.ToleratedProducts)
                    {
                        var key = product.Title.ToLowerInvariant();
                        if (!productMap.ContainsKey(key))
                        {
                            productMap[key] = product;
                        }
                    }
                }
            }

            if (!productMap.Any())
            {
                TempData["ErrorMessage"] = "No products found in CMS standards.";
                return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
            }

            int migrated = 0;
            int skipped = 0;
            int errors = 0;

            // Get current user for CreatedByUserId
            var userEmail = User.Identity?.Name 
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? string.Empty;
            var currentUser = !string.IsNullOrEmpty(userEmail)
                ? await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower())
                : null;

            foreach (var kvp in productMap)
            {
                try
                {
                    var cmsProduct = kvp.Value;

                    if (skipExisting)
                    {
                        var existing = await _context.StandardProducts
                            .FirstOrDefaultAsync(sp => sp.Name == cmsProduct.Title);
                        if (existing != null)
                        {
                            _logger.LogInformation("Skipping product '{Name}' - already exists", cmsProduct.Title);
                            skipped++;
                            continue;
                        }
                    }

                    var product = new StandardProduct
                    {
                        Name = cmsProduct.Title,
                        Description = cmsProduct.UseCase,
                        Provider = cmsProduct.Vendor,
                        Version = cmsProduct.Version,
                        ApprovalStatus = "Approved", // Default to approved since they're in published standards
                        CreatedByUserId = currentUser?.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.StandardProducts.Add(product);
                    await _context.SaveChangesAsync();
                    migrated++;
                    _logger.LogInformation("Migrated product '{Name}' (ID: {Id})", product.Name, product.Id);
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error migrating product '{Title}'", kvp.Value.Title);
                }
            }

            var message = $"Products migration completed: {migrated} migrated, {skipped} skipped, {errors} errors.";
            if (errors > 0)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during products migration");
            TempData["ErrorMessage"] = $"Products migration failed: {ex.Message}";
            return RedirectToAction(nameof(Index), "StandardsConfig", new { area = "Admin" });
        }
    }
}

