using System.Globalization;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Compass.Services;

public class ProjectImportService : IProjectImportService
{
    private readonly CompassDbContext _context;
    private readonly ILogger<ProjectImportService> _logger;
    private readonly IUserDirectoryService _userDirectoryService;

    public ProjectImportService(
        CompassDbContext context,
        ILogger<ProjectImportService> logger,
        IUserDirectoryService userDirectoryService)
    {
        _context = context;
        _logger = logger;
        _userDirectoryService = userDirectoryService;
    }

    public async Task<ProjectImportPreview> PreviewImportAsync(Stream csvStream, CancellationToken cancellationToken = default)
    {
        var preview = new ProjectImportPreview();
        var rows = new List<CsvProjectRow>();

        try
        {
            // Read the stream into memory so we can read it multiple times
            using var memoryStream = new MemoryStream();
            await csvStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            // First pass: Read headers
            using var reader1 = new StreamReader(memoryStream, Encoding.UTF8, leaveOpen: true);
            using var csv1 = new CsvReader(reader1, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null
            });

            if (await csv1.ReadAsync())
            {
                csv1.ReadHeader();
                var headers = csv1.HeaderRecord ?? Array.Empty<string>();
                preview.CsvColumns = headers.Select(h => h?.Trim() ?? string.Empty).Where(h => !string.IsNullOrEmpty(h)).ToList();
                
                // Build field mapping
                BuildFieldMapping(preview, headers);
            }

            // Second pass: Read data rows
            memoryStream.Position = 0;
            using var reader2 = new StreamReader(memoryStream, Encoding.UTF8);
            using var csv2 = new CsvReader(reader2, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null
            });

            var records = csv2.GetRecords<dynamic>();
            int rowNumber = 1;

            foreach (var record in records)
            {
                var row = MapDynamicToCsvRow(record, rowNumber);
                rows.Add(row);
                
                // Enhanced validation
                List<ProjectImportError> errors = ValidateRow(row, preview);
                preview.ValidationErrors.AddRange(errors);
                
                rowNumber++;
            }

            preview.Rows = rows;
            
            // Validate required columns
            ValidateRequiredColumns(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing CSV import");
            preview.ValidationErrors.Add(new ProjectImportError
            {
                RowNumber = 0,
                Field = "File",
                Message = $"Error reading CSV file: {ex.Message}"
            });
        }

        return preview;
    }

    private void BuildFieldMapping(ProjectImportPreview preview, string[] headers)
    {
        // Define expected CSV column names and their mappings to Project fields
        var columnMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Deliverable", "Project Title" },
            { "DeliverableID", "Historic BuRT ID" },
            { "CurrentStatusUpdate", "Status Update" },
            { "SRO", "Senior Responsible Officer" },
            { "CurrentDeliveryPhase", "Phase" },
            { "DiscStartDate", "Discovery Start Date (Planned)" },
            { "AlphaStartDate", "Alpha Start Date (Planned)" },
            { "PrivateBetaStartDate", "Private Beta Start Date (Planned)" },
            { "PublicBetaStartDate", "Public Beta Start Date (Planned)" },
            { "ActivityType", "Activity Type" },
            { "Directorate", "Directorate (Business Area)" },
            { "PolicyArea", "Policy Area (Business Area)" },
            { "Group(BudgetOwner)", "Budget Owner (Business Area)" },
            { "RiskAppetite", "Risk Appetite" },
            { "UsersOfService", "Service Users" },
            { "ExternalInternal", "Internal/External" },
            { "PMO Contact", "PMO Contact" },
            { "PurposeBenefits", "Aim" },
            { "CurrentRAG", "RAG Status" }
        };

        foreach (var header in headers)
        {
            var trimmedHeader = header?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(trimmedHeader))
                continue;

            if (columnMappings.TryGetValue(trimmedHeader, out var projectField))
            {
                preview.FieldMapping[trimmedHeader] = projectField;
                preview.MappedColumns.Add(trimmedHeader);
            }
            else
            {
                preview.UnmappedColumns.Add(trimmedHeader);
            }
        }
    }

    private void ValidateRequiredColumns(ProjectImportPreview preview)
    {
        var requiredColumns = new[] { "Deliverable" };
        
        foreach (var required in requiredColumns)
        {
            if (!preview.MappedColumns.Any(c => string.Equals(c, required, StringComparison.OrdinalIgnoreCase)))
            {
                preview.MissingRequiredColumns.Add(required);
                preview.ValidationErrors.Add(new ProjectImportError
                {
                    RowNumber = 0,
                    Field = "File",
                    Message = $"Required column '{required}' not found in CSV file"
                });
            }
        }
    }

    public async Task<ProjectImportResult> ImportProjectsFromCsvAsync(Stream csvStream, int? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var result = new ProjectImportResult();

        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null
            });

            var records = csv.GetRecords<dynamic>();
            int rowNumber = 1;

            foreach (var record in records)
            {
                result.TotalRows++;
                var csvRow = MapDynamicToCsvRow(record, rowNumber);

                try
                {
                    List<ProjectImportError> validationErrors = ValidateRow(csvRow, null);
                    
                    // Only block import if Deliverable (Title) is missing - this is critical
                    var criticalErrors = validationErrors.Where(e => e.Field == "Deliverable").ToList();
                    if (criticalErrors.Any())
                    {
                        result.Errors.AddRange(validationErrors);
                        result.ErrorCount++;
                        rowNumber++;
                        continue;
                    }
                    
                    // Log non-critical validation errors as warnings but still import
                    if (validationErrors.Any())
                    {
                        result.Errors.AddRange(validationErrors);
                        result.Warnings.AddRange(validationErrors.Select(e => $"Row {e.RowNumber}: {e.Message}"));
                    }

                    await ImportProjectRowAsync(csvRow, currentUserId, cancellationToken);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing row {RowNumber}", rowNumber);
                    result.Errors.Add(new ProjectImportError
                    {
                        RowNumber = rowNumber,
                        Field = "General",
                        Message = $"Error importing project: {ex.Message}"
                    });
                    result.ErrorCount++;
                }

                rowNumber++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing CSV file");
            result.Errors.Add(new ProjectImportError
            {
                RowNumber = 0,
                Field = "File",
                Message = $"Error reading CSV file: {ex.Message}"
            });
            result.ErrorCount = result.TotalRows;
        }

        return result;
    }

    private CsvProjectRow MapDynamicToCsvRow(dynamic record, int rowNumber)
    {
        var row = new CsvProjectRow { RowNumber = rowNumber };
        
        var dict = (IDictionary<string, object>)record;
        
        row.Deliverable = GetValue(dict, "Deliverable");
        row.DeliverableID = GetValue(dict, "DeliverableID");
        row.CurrentStatusUpdate = GetValue(dict, "CurrentStatusUpdate");
        row.SRO = GetValue(dict, "SRO");
        row.CurrentDeliveryPhase = GetValue(dict, "CurrentDeliveryPhase");
        row.DiscStartDate = GetValue(dict, "DiscStartDate");
        row.AlphaStartDate = GetValue(dict, "AlphaStartDate");
        row.PrivateBetaStartDate = GetValue(dict, "PrivateBetaStartDate");
        row.PublicBetaStartDate = GetValue(dict, "PublicBetaStartDate");
        row.ActivityType = GetValue(dict, "ActivityType");
        row.Directorate = GetValue(dict, "Directorate");
        row.PolicyArea = GetValue(dict, "PolicyArea");
        row.BudgetOwner = GetValue(dict, "Group(BudgetOwner)");
        row.RiskAppetite = GetValue(dict, "RiskAppetite");
        row.ServiceUsers = GetValue(dict, "UsersOfService");
        row.ExternalInternal = GetValue(dict, "ExternalInternal");
        row.PmoContact = GetValue(dict, "PMO Contact");
        row.PurposeBenefits = GetValue(dict, "PurposeBenefits");
        row.CurrentRAG = GetValue(dict, "CurrentRAG");
        
        return row;
    }

    private string? GetValue(IDictionary<string, object> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value?.ToString()?.Trim();
        }
        return null;
    }

    private List<ProjectImportError> ValidateRow(CsvProjectRow row, ProjectImportPreview? preview = null)
    {
        var errors = new List<ProjectImportError>();

        if (string.IsNullOrWhiteSpace(row.Deliverable))
        {
            errors.Add(new ProjectImportError
            {
                RowNumber = row.RowNumber,
                Field = "Deliverable",
                Message = "Deliverable (Title) is required"
            });
        }

        // Validate date formats
        if (!string.IsNullOrWhiteSpace(row.DiscStartDate) && !TryParseDate(row.DiscStartDate, out _))
        {
            errors.Add(new ProjectImportError
            {
                RowNumber = row.RowNumber,
                Field = "DiscStartDate",
                Message = $"Invalid date format: '{row.DiscStartDate}'. Expected formats: DD/MM/YYYY, YYYY-MM-DD, or DD-MM-YYYY"
            });
        }

        if (!string.IsNullOrWhiteSpace(row.AlphaStartDate) && !TryParseDate(row.AlphaStartDate, out _))
        {
            errors.Add(new ProjectImportError
            {
                RowNumber = row.RowNumber,
                Field = "AlphaStartDate",
                Message = $"Invalid date format: '{row.AlphaStartDate}'. Expected formats: DD/MM/YYYY, YYYY-MM-DD, or DD-MM-YYYY"
            });
        }

        if (!string.IsNullOrWhiteSpace(row.PrivateBetaStartDate) && !TryParseDate(row.PrivateBetaStartDate, out _))
        {
            errors.Add(new ProjectImportError
            {
                RowNumber = row.RowNumber,
                Field = "PrivateBetaStartDate",
                Message = $"Invalid date format: '{row.PrivateBetaStartDate}'. Expected formats: DD/MM/YYYY, YYYY-MM-DD, or DD-MM-YYYY"
            });
        }

        if (!string.IsNullOrWhiteSpace(row.PublicBetaStartDate) && !TryParseDate(row.PublicBetaStartDate, out _))
        {
            errors.Add(new ProjectImportError
            {
                RowNumber = row.RowNumber,
                Field = "PublicBetaStartDate",
                Message = $"Invalid date format: '{row.PublicBetaStartDate}'. Expected formats: DD/MM/YYYY, YYYY-MM-DD, or DD-MM-YYYY"
            });
        }

        // Validate phase values
        if (!string.IsNullOrWhiteSpace(row.CurrentDeliveryPhase))
        {
            var validPhases = new[] { "Discovery", "Alpha", "Private Beta", "Public Beta", "Live" };
            if (!validPhases.Any(p => p.Equals(row.CurrentDeliveryPhase, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add(new ProjectImportError
                {
                    RowNumber = row.RowNumber,
                    Field = "CurrentDeliveryPhase",
                    Message = $"Invalid phase value: '{row.CurrentDeliveryPhase}'. Valid values: {string.Join(", ", validPhases)}"
                });
            }
        }

        return errors;
    }

    private async Task ImportProjectRowAsync(CsvProjectRow csvRow, int? currentUserId, CancellationToken cancellationToken)
    {
        // Check if project already exists by HistoricBuRTId first, then by Title
        Project? project = null;
        if (!string.IsNullOrWhiteSpace(csvRow.DeliverableID))
        {
            project = await _context.Projects
                .FirstOrDefaultAsync(p => p.HistoricBuRTId == csvRow.DeliverableID, cancellationToken);
        }

        // If not found by HistoricBuRTId, check by Title
        if (project == null && !string.IsNullOrWhiteSpace(csvRow.Deliverable))
        {
            project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Title == csvRow.Deliverable && !p.IsDeleted, cancellationToken);
        }

        if (project == null)
        {
            project = new Project
            {
                Title = csvRow.Deliverable ?? "Untitled Project",
                HistoricBuRTId = csvRow.DeliverableID,
                Phase = csvRow.CurrentDeliveryPhase,
                ServiceUsers = csvRow.ServiceUsers,
                Aim = csvRow.PurposeBenefits,
                RagStatus = csvRow.CurrentRAG,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Parse Internal/External
            if (!string.IsNullOrWhiteSpace(csvRow.ExternalInternal))
            {
                var externalInternal = csvRow.ExternalInternal.ToLowerInvariant();
                project.IsInternal = externalInternal.Contains("internal");
                project.IsExternal = externalInternal.Contains("external");
            }

            // Parse phase dates
            ParsePhaseDates(csvRow, project);

            // Set Activity Type
            if (!string.IsNullOrWhiteSpace(csvRow.ActivityType))
            {
                var activityType = await _context.ActivityTypeLookups
                    .FirstOrDefaultAsync(at => at.Name == csvRow.ActivityType && at.IsActive, cancellationToken);
                if (activityType != null)
                {
                    project.ActivityTypeLookupId = activityType.Id;
                }
            }

            // Set Risk Appetite
            if (!string.IsNullOrWhiteSpace(csvRow.RiskAppetite))
            {
                var riskAppetite = await _context.RiskAppetiteLookups
                    .FirstOrDefaultAsync(ra => ra.Name == csvRow.RiskAppetite && ra.IsActive, cancellationToken);
                if (riskAppetite != null)
                {
                    project.RiskAppetiteLookupId = riskAppetite.Id;
                }
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Update existing project
            project.Title = csvRow.Deliverable ?? project.Title;
            project.Phase = csvRow.CurrentDeliveryPhase ?? project.Phase;
            project.ServiceUsers = csvRow.ServiceUsers ?? project.ServiceUsers;
            if (!string.IsNullOrWhiteSpace(csvRow.PurposeBenefits))
            {
                project.Aim = csvRow.PurposeBenefits;
            }
            if (!string.IsNullOrWhiteSpace(csvRow.CurrentRAG))
            {
                project.RagStatus = csvRow.CurrentRAG;
            }
            project.UpdatedAt = DateTime.UtcNow;
            
            ParsePhaseDates(csvRow, project);
            
            // Update Activity Type
            if (!string.IsNullOrWhiteSpace(csvRow.ActivityType))
            {
                var activityType = await _context.ActivityTypeLookups
                    .FirstOrDefaultAsync(at => at.Name == csvRow.ActivityType && at.IsActive, cancellationToken);
                if (activityType != null)
                {
                    project.ActivityTypeLookupId = activityType.Id;
                }
            }
            
            // Update Risk Appetite
            if (!string.IsNullOrWhiteSpace(csvRow.RiskAppetite))
            {
                var riskAppetite = await _context.RiskAppetiteLookups
                    .FirstOrDefaultAsync(ra => ra.Name == csvRow.RiskAppetite && ra.IsActive, cancellationToken);
                if (riskAppetite != null)
                {
                    project.RiskAppetiteLookupId = riskAppetite.Id;
                }
            }
            
            // Update Internal/External
            if (!string.IsNullOrWhiteSpace(csvRow.ExternalInternal))
            {
                var externalInternal = csvRow.ExternalInternal.ToLowerInvariant();
                project.IsInternal = externalInternal.Contains("internal");
                project.IsExternal = externalInternal.Contains("external");
            }
            
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Add status update if provided
        if (!string.IsNullOrWhiteSpace(csvRow.CurrentStatusUpdate) && currentUserId.HasValue)
        {
            var statusUpdate = new ProjectStatusUpdate
            {
                ProjectId = project.Id,
                Narrative = csvRow.CurrentStatusUpdate,
                CreatedByUserId = currentUserId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsBulkImport = true
            };
            _context.ProjectStatusUpdates.Add(statusUpdate);
        }

        // Add SROs - DISABLED: Not matching people correctly
        // if (!string.IsNullOrWhiteSpace(csvRow.SRO))
        // {
        //     await AddSrosAsync(project, csvRow.SRO, cancellationToken);
        // }

        // Add Directorates (Business Areas) - try PolicyArea first, then Directorate, then BudgetOwner
        if (!string.IsNullOrWhiteSpace(csvRow.PolicyArea))
        {
            await AddDirectoratesAsync(project, csvRow.PolicyArea, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(csvRow.Directorate))
        {
            await AddDirectoratesAsync(project, csvRow.Directorate, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(csvRow.BudgetOwner))
        {
            await AddDirectoratesAsync(project, csvRow.BudgetOwner, cancellationToken);
        }

        // Add Budget Owners - try PolicyArea first, then BudgetOwner
        if (!string.IsNullOrWhiteSpace(csvRow.PolicyArea))
        {
            await AddBudgetOwnersAsync(project, csvRow.PolicyArea, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(csvRow.BudgetOwner))
        {
            await AddBudgetOwnersAsync(project, csvRow.BudgetOwner, cancellationToken);
        }

        // Add PMO Contacts - DISABLED: Not matching people correctly
        // if (!string.IsNullOrWhiteSpace(csvRow.PmoContact))
        // {
        //     await AddPmoContactsAsync(project, csvRow.PmoContact, cancellationToken);
        // }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AddSrosAsync(Project project, string sroValue, CancellationToken cancellationToken)
    {
        // Try to find user by email or name
        var sroEmails = sroValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        foreach (var sroEmail in sroEmails)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == sroEmail || u.Name.Contains(sroEmail), cancellationToken);

            if (user != null)
            {
                // Check if relationship already exists in database
                var existsInDb = await _context.ProjectSeniorResponsibleOfficers
                    .AnyAsync(psro => psro.ProjectId == project.Id && psro.UserId == user.Id, cancellationToken);

                // Also check if it's already being added in this transaction (change tracker)
                var existsInContext = _context.ChangeTracker.Entries<ProjectSeniorResponsibleOfficer>()
                    .Any(e => e.Entity.ProjectId == project.Id && e.Entity.UserId == user.Id && e.State == Microsoft.EntityFrameworkCore.EntityState.Added);

                if (!existsInDb && !existsInContext)
                {
                    _context.ProjectSeniorResponsibleOfficers.Add(new ProjectSeniorResponsibleOfficer
                    {
                        ProjectId = project.Id,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }

    private async Task AddDirectoratesAsync(Project project, string directorateValue, CancellationToken cancellationToken)
    {
        // Directorates are now Business Areas
        var directorateNames = directorateValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        foreach (var directorateName in directorateNames)
        {
            var businessArea = await FindBusinessAreaAsync(directorateName, cancellationToken);

            if (businessArea != null)
            {
                // Check if relationship already exists in database
                var existsInDb = await _context.ProjectDirectorates
                    .AnyAsync(pd => pd.ProjectId == project.Id && pd.BusinessAreaLookupId == businessArea.Id, cancellationToken);

                // Also check if it's already being added in this transaction (change tracker)
                var existsInContext = _context.ChangeTracker.Entries<ProjectDirectorate>()
                    .Any(e => e.Entity.ProjectId == project.Id && e.Entity.BusinessAreaLookupId == businessArea.Id && e.State == Microsoft.EntityFrameworkCore.EntityState.Added);

                if (!existsInDb && !existsInContext)
                {
                    _context.ProjectDirectorates.Add(new ProjectDirectorate
                    {
                        ProjectId = project.Id,
                        BusinessAreaLookupId = businessArea.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                _logger.LogWarning("Could not find Business Area match for '{DirectorateName}' in row {RowNumber}", directorateName, project.Id);
            }
        }
    }

    private async Task AddBudgetOwnersAsync(Project project, string budgetOwnerValue, CancellationToken cancellationToken)
    {
        var budgetOwnerNames = budgetOwnerValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        foreach (var budgetOwnerName in budgetOwnerNames)
        {
            var businessArea = await FindBusinessAreaAsync(budgetOwnerName, cancellationToken);

            if (businessArea != null)
            {
                // Check if relationship already exists in database
                var existsInDb = await _context.ProjectBudgetOwners
                    .AnyAsync(pbo => pbo.ProjectId == project.Id && pbo.BusinessAreaLookupId == businessArea.Id, cancellationToken);

                // Also check if it's already being added in this transaction (change tracker)
                var existsInContext = _context.ChangeTracker.Entries<ProjectBudgetOwner>()
                    .Any(e => e.Entity.ProjectId == project.Id && e.Entity.BusinessAreaLookupId == businessArea.Id && e.State == Microsoft.EntityFrameworkCore.EntityState.Added);

                if (!existsInDb && !existsInContext)
                {
                    _context.ProjectBudgetOwners.Add(new ProjectBudgetOwner
                    {
                        ProjectId = project.Id,
                        BusinessAreaLookupId = businessArea.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                _logger.LogWarning("Could not find Business Area match for '{BudgetOwnerName}' in row {RowNumber}", budgetOwnerName, project.Id);
            }
        }
    }

    private async Task<BusinessAreaLookup?> FindBusinessAreaAsync(string searchValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchValue))
        {
            return null;
        }

        var normalizedSearch = searchValue.Trim();

        // Get all active business areas
        var allBusinessAreas = await _context.BusinessAreaLookups
            .Where(ba => ba.IsActive)
            .ToListAsync(cancellationToken);

        // Try exact match (case-sensitive)
        var exactMatch = allBusinessAreas.FirstOrDefault(ba => ba.Name == normalizedSearch);
        if (exactMatch != null)
        {
            return exactMatch;
        }

        // Try case-insensitive exact match
        var caseInsensitiveMatch = allBusinessAreas.FirstOrDefault(ba => 
            ba.Name.Equals(normalizedSearch, StringComparison.OrdinalIgnoreCase));
        if (caseInsensitiveMatch != null)
        {
            return caseInsensitiveMatch;
        }

        // Try contains match (case-insensitive)
        var containsMatch = allBusinessAreas.FirstOrDefault(ba => 
            ba.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
            normalizedSearch.Contains(ba.Name, StringComparison.OrdinalIgnoreCase));
        if (containsMatch != null)
        {
            return containsMatch;
        }

        // Try removing common prefixes/suffixes and matching
        var normalizedWithoutPrefixes = normalizedSearch
            .Replace("Directorate", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Division", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Group", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (!string.IsNullOrWhiteSpace(normalizedWithoutPrefixes) && normalizedWithoutPrefixes != normalizedSearch)
        {
            var prefixMatch = allBusinessAreas.FirstOrDefault(ba => 
                ba.Name.Contains(normalizedWithoutPrefixes, StringComparison.OrdinalIgnoreCase) ||
                normalizedWithoutPrefixes.Contains(ba.Name, StringComparison.OrdinalIgnoreCase));
            if (prefixMatch != null)
            {
                return prefixMatch;
            }
        }

        return null;
    }

    private async Task AddPmoContactsAsync(Project project, string pmoContactValue, CancellationToken cancellationToken)
    {
        var pmoEmails = pmoContactValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();

        foreach (var pmoEmail in pmoEmails)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == pmoEmail || u.Name.Contains(pmoEmail), cancellationToken);

            if (user != null)
            {
                // Check if relationship already exists in database
                var existsInDb = await _context.ProjectPmoContacts
                    .AnyAsync(ppc => ppc.ProjectId == project.Id && ppc.UserId == user.Id, cancellationToken);

                // Also check if it's already being added in this transaction (change tracker)
                var existsInContext = _context.ChangeTracker.Entries<ProjectPmoContact>()
                    .Any(e => e.Entity.ProjectId == project.Id && e.Entity.UserId == user.Id && e.State == Microsoft.EntityFrameworkCore.EntityState.Added);

                if (!existsInDb && !existsInContext)
                {
                    _context.ProjectPmoContacts.Add(new ProjectPmoContact
                    {
                        ProjectId = project.Id,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }

    private void ParsePhaseDates(CsvProjectRow csvRow, Project project)
    {
        // Parse Discovery dates
        if (TryParseDate(csvRow.DiscStartDate, out var discStart))
        {
            project.DiscoveryStartDatePlanned = discStart;
        }

        // Parse Alpha dates
        if (TryParseDate(csvRow.AlphaStartDate, out var alphaStart))
        {
            project.AlphaStartDatePlanned = alphaStart;
        }

        // Parse Private Beta dates
        if (TryParseDate(csvRow.PrivateBetaStartDate, out var privateBetaStart))
        {
            project.PrivateBetaStartDatePlanned = privateBetaStart;
        }

        // Parse Public Beta dates
        if (TryParseDate(csvRow.PublicBetaStartDate, out var publicBetaStart))
        {
            project.PublicBetaStartDatePlanned = publicBetaStart;
        }
    }

    /// <summary>
    /// Tries to parse a date string using multiple formats, including DD/MM/YYYY (UK format)
    /// </summary>
    private bool TryParseDate(string? dateString, out DateTime result)
    {
        result = default;
        
        if (string.IsNullOrWhiteSpace(dateString))
        {
            return false;
        }

        var trimmed = dateString.Trim();

        // Try common date formats
        var formats = new[]
        {
            "dd/MM/yyyy",      // UK format: 19/07/2022
            "d/M/yyyy",        // UK format without leading zeros: 9/7/2022
            "dd-MM-yyyy",      // UK format with dashes: 19-07-2022
            "d-M-yyyy",        // UK format with dashes, no leading zeros
            "yyyy-MM-dd",      // ISO format: 2022-07-19
            "yyyy/MM/dd",      // ISO format with slashes
            "MM/dd/yyyy",      // US format: 07/19/2022
            "M/d/yyyy",        // US format without leading zeros
            "dd.MM.yyyy",      // European format with dots
            "d.M.yyyy"         // European format with dots, no leading zeros
        };

        // Try parsing with UK culture first (DD/MM/YYYY)
        var ukCulture = CultureInfo.GetCultureInfo("en-GB");
        if (DateTime.TryParseExact(trimmed, formats, ukCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        // Try parsing with invariant culture
        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        // Fall back to standard parsing (uses system culture)
        if (DateTime.TryParse(trimmed, ukCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        return false;
    }
}

