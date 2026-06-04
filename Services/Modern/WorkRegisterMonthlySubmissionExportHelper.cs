using Compass.Data;
using Compass.Models;
using Compass.Models.Modern.Work;
using Compass.Services;
using Compass.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

/// <summary>Shared monthly submission period columns and status for work register Excel exports.</summary>
public static class WorkRegisterMonthlySubmissionExportHelper
{
    public const int DefaultMinReportYear = 2026;

    public static async Task<List<SubmissionTrendMonthColumn>> LoadPeriodColumnsAsync(
        CompassDbContext db,
        int endYear,
        int endMonth,
        int minReportYear,
        CancellationToken cancellationToken = default)
    {
        var enGb = System.Globalization.CultureInfo.GetCultureInfo("en-GB");
        var endDate = new DateTime(endYear, endMonth, 1);

        var cycleId = await db.WorkReportingCycles.AsNoTracking()
            .Where(c => c.Code == WorkReportingMonthlyCycleCodes.MonthlyWorkUpdates)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (cycleId.HasValue)
        {
            var explicitPeriods = await db.WorkReportingCyclePeriods.AsNoTracking()
                .Where(p => p.ReportingCycleId == cycleId.Value && p.IsActive)
                .OrderBy(p => p.PeriodStart)
                .ToListAsync(cancellationToken);

            if (explicitPeriods.Count > 0)
            {
                return explicitPeriods
                    .Where(p => p.PeriodStart <= endDate)
                    .Select(p => new SubmissionTrendMonthColumn
                    {
                        Year = p.PeriodStart.Year,
                        Month = p.PeriodStart.Month,
                        Label = !string.IsNullOrWhiteSpace(p.PeriodLabel)
                            ? p.PeriodLabel.Trim()
                            : p.PeriodStart.ToString("MMM yyyy", enGb)
                    })
                    .ToList();
            }
        }

        var columns = new List<SubmissionTrendMonthColumn>();
        for (var d = new DateTime(minReportYear, 1, 1); d <= endDate; d = d.AddMonths(1))
        {
            columns.Add(new SubmissionTrendMonthColumn
            {
                Year = d.Year,
                Month = d.Month,
                Label = d.ToString("MMM yyyy", enGb)
            });
        }

        return columns;
    }

    public static bool IsProjectInReportingScopeForMonth(Project project, int year, int month)
    {
        if (project.IsDeleted)
            return false;
        if (string.Equals(project.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            return false;

        var hasPeriodUpdate = project.MonthlyUpdates?.Any(u => u.Year == year && u.Month == month) == true;
        if (hasPeriodUpdate)
            return true;

        return string.Equals(project.Status, "Active", StringComparison.OrdinalIgnoreCase)
            || string.Equals(project.Status, "Paused", StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolvePeriodSubmissionStatus(Project project, int year, int month)
    {
        if (!IsProjectInReportingScopeForMonth(project, year, month))
            return "Not in scope";

        var update = project.MonthlyUpdates?.FirstOrDefault(u => u.Year == year && u.Month == month);
        return update?.SubmittedAt != null ? "Submitted" : "Not submitted";
    }

    public static Dictionary<int, List<string>> BuildPeriodStatusesByProject(
        IEnumerable<Project> projects,
        IReadOnlyList<SubmissionTrendMonthColumn> periodColumns)
    {
        var result = new Dictionary<int, List<string>>();
        foreach (var project in projects)
        {
            result[project.Id] = periodColumns
                .Select(col => ResolvePeriodSubmissionStatus(project, col.Year, col.Month))
                .ToList();
        }

        return result;
    }

    public static async Task<List<SubmissionTrendMonthColumn>> EnrichRegisterRowsWithMonthlyPeriodsAsync(
        CompassDbContext db,
        IMonthlyUpdateService monthlyUpdateService,
        IEnumerable<WorkRegisterRow> rows,
        CancellationToken cancellationToken = default)
    {
        var rowList = rows.ToList();
        if (rowList.Count == 0)
            return new List<SubmissionTrendMonthColumn>();

        var (reportYear, reportMonth) = monthlyUpdateService.ResolveDashboardReportingPeriod(DateTime.UtcNow);
        var periodColumns = await LoadPeriodColumnsAsync(
            db,
            reportYear,
            reportMonth,
            DefaultMinReportYear,
            cancellationToken);

        if (periodColumns.Count == 0)
            return periodColumns;

        var projectIds = rowList.Select(r => r.Id).ToList();
        var projects = await db.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Include(p => p.MonthlyUpdates)
            .ToListAsync(cancellationToken);

        var statusesByProject = BuildPeriodStatusesByProject(projects, periodColumns);
        foreach (var row in rowList)
        {
            if (statusesByProject.TryGetValue(row.Id, out var statuses))
                row.MonthlyPeriodStatuses = statuses;
        }

        return periodColumns;
    }
}
