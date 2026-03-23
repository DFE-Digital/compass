using System.Collections.Concurrent;
using System.Reflection;
using ClosedXML.Excel;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Dynamic.Core;
using RaidAction = Compass.Models.Action;

namespace Compass;

/// <summary>
/// Exports a repeatable raw data workbook from the legacy Compass database.
/// Run with: dotnet run --export-migration-workbook [--environment Development|Production] [--output path/to/file.xlsx]
/// </summary>
public static class MigrationWorkbookExport
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ExportablePropertiesCache = new();

    public static async Task RunAsync(string environment, string? outputPath = null)
    {
        Console.WriteLine($"=== Exporting Compass migration workbook ({environment}) ===\n");

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Error: DefaultConnection string not found in configuration.");
            return;
        }

        var options = new DbContextOptionsBuilder<CompassDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new CompassDbContext(options);

        Console.WriteLine("Testing connection...");
        try
        {
            await db.Database.CanConnectAsync();
            Console.WriteLine($"✓ Connected to Azure SQL ({environment})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to connect to Azure SQL: {ex.Message}");
            return;
        }

        var finalOutputPath = ResolveOutputPath(outputPath);
        var outputDirectory = Path.GetDirectoryName(finalOutputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var exports = new List<(string SheetName, int RowCount)>();

        using var workbook = new XLWorkbook();
        AddReadMeSheet(workbook, environment, finalOutputPath);

        exports.Add(await ExportEntitySheetAsync(workbook, "Projects", db.Projects.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Users", db.Users.AsNoTracking()));

        exports.Add(await ExportEntitySheetAsync(workbook, "BusinessAreas", db.BusinessAreaLookups.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Phases", db.PhaseLookups.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ActivityTypes", db.ActivityTypeLookups.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "DirectoratesLookup", db.DirectorateLookups.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "RiskAppetite", db.RiskAppetiteLookups.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "RagStatuses", db.RagStatusLookups.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "DeliveryPriorities", db.DeliveryPriorities.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "RiskTiers", db.RiskTiers.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "RiskTypes", db.RiskTypes.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ActionSources", db.ActionSources.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Divisions", db.Divisions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Missions", db.Missions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Objectives", db.Objectives.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "OrganizationalGroups", db.OrganizationalGroups.AsNoTracking()));

        exports.Add(await ExportEntitySheetAsync(workbook, "RagHistory", db.ProjectRagHistories.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Successes", db.ProjectSuccesses.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Outcomes", db.ProjectOutcomes.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Needs", db.ProjectNeeds.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ProblemStatements", db.ProjectProblemStatements.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ProblemStatementHist", db.ProjectProblemStatementHistories.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ProjectMissions", db.ProjectMissions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ResourceFunding", db.ProjectResourceFundings.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ResourceFundingHist", db.ProjectResourceFundingHistories.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ProjectContacts", db.ProjectContacts.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ProjectObjectives", db.ProjectObjectives.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ProjectProducts", db.ProjectProducts.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "StatusUpdates", db.ProjectStatusUpdates.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "MonthlyUpdates", db.ProjectMonthlyUpdates.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "MonthlyNarratives", db.MonthlyUpdateNarratives.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "WeeklySuccess", db.ProjectWeeklySuccessUpdates.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "SROs", db.ProjectSeniorResponsibleOfficers.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ServiceOwners", db.ProjectServiceOwners.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Directorates", db.ProjectDirectorates.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "BudgetOwners", db.ProjectBudgetOwners.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "PMOContacts", db.ProjectPmoContacts.AsNoTracking()));

        exports.Add(await ExportEntitySheetAsync(workbook, "Milestones", db.Milestones.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "MilestoneUpdates", db.MilestoneUpdates.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Risks", db.Risks.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Issues", db.Issues.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Actions", db.Set<RaidAction>().AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Decisions", db.Decisions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Comments", db.Comments.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "Dependencies", db.Dependencies.AsNoTracking()));

        exports.Add(await ExportEntitySheetAsync(workbook, "RiskActions", db.RiskActions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "RiskRiskTypes", db.RiskRiskTypes.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "IssueActions", db.IssueActions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "RiskDecisions", db.RiskDecisions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "IssueDecisions", db.IssueDecisions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "IssueRisks", db.IssueRisks.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "ActionDecisions", db.ActionDecisions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "MilestoneActions", db.MilestoneActions.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "MilestoneRisks", db.MilestoneRisks.AsNoTracking()));
        exports.Add(await ExportEntitySheetAsync(workbook, "MilestoneIssues", db.MilestoneIssues.AsNoTracking()));

        AddSummarySheet(workbook, exports);

        try
        {
            workbook.SaveAs(finalOutputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to save workbook: {ex.Message}");
            return;
        }

        Console.WriteLine("\nWorkbook export completed successfully.");
        Console.WriteLine($"Output: {finalOutputPath}");
    }

    private static async Task<(string SheetName, int RowCount)> ExportEntitySheetAsync<TEntity>(
        IXLWorkbook workbook,
        string sheetName,
        IQueryable<TEntity> query) where TEntity : class
    {
        var orderedQuery = OrderForExport(query);
        var items = await orderedQuery.ToListAsync();
        WriteEntitySheet(workbook, sheetName, items);

        Console.WriteLine($"  • {sheetName}: {items.Count:n0} rows");
        return (sheetName, items.Count);
    }

    private static IQueryable<TEntity> OrderForExport<TEntity>(IQueryable<TEntity> query) where TEntity : class
    {
        var properties = GetExportableProperties(typeof(TEntity));
        if (properties.Length == 0)
        {
            return query;
        }

        var idProperty = properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));
        if (idProperty != null)
        {
            return query.OrderBy("Id");
        }

        var orderClause = string.Join(", ", properties.Select(p => p.Name));
        return string.IsNullOrWhiteSpace(orderClause) ? query : query.OrderBy(orderClause);
    }

    private static void WriteEntitySheet<TEntity>(IXLWorkbook workbook, string sheetName, IReadOnlyCollection<TEntity> items)
    {
        var worksheet = workbook.Worksheets.Add(sheetName);
        var properties = GetExportableProperties(typeof(TEntity));

        for (var i = 0; i < properties.Length; i++)
        {
            var header = worksheet.Cell(1, i + 1);
            header.Value = properties[i].Name;
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");
            header.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var row = 2;
        foreach (var item in items)
        {
            for (var i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var cell = worksheet.Cell(row, i + 1);
                var value = property.GetValue(item);
                WriteCellValue(cell, value);
            }

            row++;
        }

        worksheet.SheetView.FreezeRows(1);
        if (worksheet.RangeUsed() is not null)
        {
            worksheet.RangeUsed()!.SetAutoFilter();
        }

        worksheet.Columns().AdjustToContents();
        foreach (var column in worksheet.ColumnsUsed())
        {
            if (column.Width > 60)
            {
                column.Width = 60;
            }
        }
    }

    private static void WriteCellValue(IXLCell cell, object? value)
    {
        if (value is null)
        {
            cell.Clear();
            return;
        }

        switch (value)
        {
            case DateTime dateTime:
                cell.Value = dateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case DateTimeOffset dateTimeOffset:
                cell.Value = dateTimeOffset.UtcDateTime;
                cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
                break;
            case DateOnly dateOnly:
                cell.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = "yyyy-mm-dd";
                break;
            case TimeOnly timeOnly:
                cell.Value = timeOnly.ToTimeSpan();
                break;
            case Enum enumValue:
                cell.Value = enumValue.ToString();
                break;
            case Guid guid:
                cell.Value = guid.ToString();
                break;
            case byte[] bytes:
                cell.Value = Convert.ToBase64String(bytes);
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    private static PropertyInfo[] GetExportableProperties(Type type)
    {
        return ExportablePropertiesCache.GetOrAdd(type, static t =>
        {
            return t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(IsExportableProperty)
                .OrderBy(p => p.MetadataToken)
                .ToArray();
        });
    }

    private static bool IsExportableProperty(PropertyInfo property)
    {
        if (property.GetCustomAttribute<NotMappedAttribute>() is not null)
        {
            return false;
        }

        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (propertyType == typeof(string) ||
            propertyType == typeof(bool) ||
            propertyType == typeof(int) ||
            propertyType == typeof(long) ||
            propertyType == typeof(short) ||
            propertyType == typeof(decimal) ||
            propertyType == typeof(double) ||
            propertyType == typeof(float) ||
            propertyType == typeof(DateTime) ||
            propertyType == typeof(DateTimeOffset) ||
            propertyType == typeof(DateOnly) ||
            propertyType == typeof(TimeOnly) ||
            propertyType == typeof(Guid) ||
            propertyType.IsEnum)
        {
            return true;
        }

        if (propertyType == typeof(byte[]))
        {
            return false;
        }

        return false;
    }

    private static string ResolveOutputPath(string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return Path.GetFullPath(outputPath);
        }

        var defaultFileName = $"compass-migration-workbook-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
        return Path.GetFullPath(Path.Combine("exports", defaultFileName));
    }

    private static void AddReadMeSheet(IXLWorkbook workbook, string environment, string outputPath)
    {
        var worksheet = workbook.Worksheets.Add("ReadMe");

        worksheet.Cell(1, 1).Value = "Area";
        worksheet.Cell(1, 2).Value = "Guidance";
        worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");

        var rows = new[]
        {
            ("Export type", "Raw source extract from the legacy Compass database."),
            ("Environment", environment),
            ("Output file", outputPath),
            ("Legacy IDs", "The `Id` column in each sheet is the source identifier to carry into the migration mapping."),
            ("Parent links", "Child sheets keep their original foreign keys such as `ProjectId`, `RiskId`, `IssueId`, and `MilestoneId`."),
            ("Lookup sheets", "Use lookup sheets like `BusinessAreas`, `Phases`, `RagStatuses`, `RiskTiers`, and `Users` to resolve IDs and names."),
            ("Migration hint", "Use `Projects` as the parent work-item source and then attach related sheets in a second pass."),
            ("Deleted rows", "This export is a full raw dump of the source tables, so soft-deleted rows are retained if they exist.")
        };

        var row = 2;
        foreach (var (area, guidance) in rows)
        {
            worksheet.Cell(row, 1).Value = area;
            worksheet.Cell(row, 2).Value = guidance;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        worksheet.Column(2).Width = Math.Min(worksheet.Column(2).Width, 90);
        worksheet.Column(2).Style.Alignment.WrapText = true;
        worksheet.SheetView.FreezeRows(1);
    }

    private static void AddSummarySheet(IXLWorkbook workbook, IReadOnlyCollection<(string SheetName, int RowCount)> exports)
    {
        var worksheet = workbook.Worksheets.Add("Summary");
        worksheet.Cell(1, 1).Value = "Sheet";
        worksheet.Cell(1, 2).Value = "Rows";
        worksheet.Range(1, 1, 1, 2).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f3f5");

        var row = 2;
        foreach (var export in exports)
        {
            worksheet.Cell(row, 1).Value = export.SheetName;
            worksheet.Cell(row, 2).Value = export.RowCount;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        worksheet.SheetView.FreezeRows(1);
    }
}
