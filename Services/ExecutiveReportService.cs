using Compass.Data;
using Compass.Models.Fips;
using Compass.Services.Aiss;
using Compass.ViewModels;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

/// <summary>Builds the executive report from work items, the service register, commission returns, and AISS.</summary>
public sealed class ExecutiveReportService
{
    private readonly CompassDbContext _db;
    private readonly CommissionReportingAnalyticsService _performance;
    private readonly IAissSummaryService _aiss;
    private readonly ILogger<ExecutiveReportService> _logger;

    public ExecutiveReportService(
        CompassDbContext db,
        CommissionReportingAnalyticsService performance,
        IAissSummaryService aiss,
        ILogger<ExecutiveReportService> logger)
    {
        _db = db;
        _performance = performance;
        _aiss = aiss;
        _logger = logger;
    }

    public async Task<ExecutiveReportViewModel> BuildAsync(CancellationToken cancellationToken = default)
    {
        var model = new ExecutiveReportViewModel { GeneratedAtUtc = DateTime.UtcNow };
        var accessibilityTask = _aiss.GetSummaryAsync(cancellationToken);

        try
        {
            model.Work = await BuildWorkAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Executive report: failed to load work summary");
            model.WorkError = "Work summary could not be loaded.";
        }

        try
        {
            model.ServiceRegister = await BuildServiceRegisterAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Executive report: failed to load service register summary");
            model.ServiceRegisterError = "Service register summary could not be loaded.";
        }

        try
        {
            var (performance, performanceError) = await BuildPerformanceAsync(cancellationToken);
            model.Performance = performance;
            model.PerformanceError = performanceError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Executive report: failed to load performance summary");
            model.PerformanceError = "Performance returns summary could not be loaded.";
        }

        try
        {
            var (summary, error) = await accessibilityTask;
            if (summary == null)
            {
                model.AccessibilityError = error ?? "Accessibility summary was not available.";
            }
            else
            {
                model.Accessibility = MapAccessibility(summary);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Executive report: failed to load accessibility summary");
            model.AccessibilityError = "Accessibility summary could not be loaded.";
        }

        return model;
    }

    private async Task<ExecutiveWorkSummary> BuildWorkAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Projects.AsNoTracking()
            .Where(p => !p.IsDeleted && (p.Status == "Active" || p.Status == "Paused"))
            .Select(p => new
            {
                Status = p.Status ?? "",
                Phase = p.PhaseLookup != null && p.PhaseLookup.Name != "" ? p.PhaseLookup.Name : "Not set",
                PhaseSort = p.PhaseLookup != null ? p.PhaseLookup.SortOrder : int.MaxValue,
                Priority = p.DeliveryPriority != null && p.DeliveryPriority.Name != "" ? p.DeliveryPriority.Name : "Not set",
                PrioritySort = p.DeliveryPriority != null ? p.DeliveryPriority.SortOrder : int.MaxValue
            })
            .ToListAsync(cancellationToken);

        var outcomeTitles = await _db.Objectives.AsNoTracking()
            .Where(o => !o.IsDeleted)
            .OrderBy(o => o.Title)
            .Select(o => o.Title)
            .ToListAsync(cancellationToken);

        var links = await _db.ProjectObjectives.AsNoTracking()
            .Where(po => !po.Objective.IsDeleted
                && !po.Project.IsDeleted
                && (po.Project.Status == "Active" || po.Project.Status == "Paused"))
            .Select(po => new { po.ProjectId, po.Objective.Title })
            .ToListAsync(cancellationToken);

        var inProgress = items.Count;
        var linkedProjectIds = links.Select(l => l.ProjectId).Distinct().Count();
        var countByOutcome = links
            .GroupBy(l => l.Title, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ProjectId).Distinct().Count(), StringComparer.OrdinalIgnoreCase);

        return new ExecutiveWorkSummary
        {
            ActiveCount = items.Count(i => i.Status == "Active"),
            PausedCount = items.Count(i => i.Status == "Paused"),
            LinkedToPriorityOutcomeCount = linkedProjectIds,
            NotLinkedToPriorityOutcomeCount = Math.Max(0, inProgress - linkedProjectIds),
            Priorities = items
                .GroupBy(i => i.Priority, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Label = g.Key, Sort = g.Min(x => x.PrioritySort), Count = g.Count() })
                .OrderBy(g => g.Label.Equals("Not set", StringComparison.OrdinalIgnoreCase) ? int.MaxValue : g.Sort)
                .ThenBy(g => g.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => CountRow(g.Label, g.Count, inProgress))
                .ToList(),
            PriorityOutcomes = outcomeTitles
                .Select(title => CountRow(title, countByOutcome.GetValueOrDefault(title), inProgress))
                .OrderByDescending(r => r.Count)
                .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Phases = items
                .GroupBy(i => i.Phase, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Label = g.Key, Sort = g.Min(x => x.PhaseSort), Count = g.Count() })
                .OrderBy(g => g.Label.Equals("Not set", StringComparison.OrdinalIgnoreCase) ? int.MaxValue : g.Sort)
                .ThenBy(g => g.Label, StringComparer.OrdinalIgnoreCase)
                .Select(g => CountRow(g.Label, g.Count, inProgress))
                .ToList()
        };
    }

    private async Task<ExecutiveServiceRegisterSummary> BuildServiceRegisterAsync(CancellationToken cancellationToken)
    {
        var products = await _db.CMDBProducts.AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.UniqueID,
                p.Title,
                p.Status,
                p.IsEnterpriseService,
                HasPhase = p.PhaseId != null,
                PhaseName = p.Phase != null ? p.Phase.Name : null
            })
            .ToListAsync(cancellationToken);

        var businessAreaNames = (await _db.CMDBProductBusinessAreas.AsNoTracking()
            .Select(ba => new { ba.CMDBProductId, Name = ba.FipsBusinessArea.Name })
            .ToListAsync(cancellationToken))
            .GroupBy(ba => ba.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Name).Where(n => !string.IsNullOrWhiteSpace(n)).Cast<string>().ToList());

        var typeIds = (await _db.CMDBProductTypes.AsNoTracking()
            .Select(t => t.CMDBProductId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
        var channelIds = (await _db.CMDBProductChannels.AsNoTracking()
            .Select(c => c.CMDBProductId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
        var userGroupIds = (await _db.CMDBProductUserGroups.AsNoTracking()
            .Select(g => g.CMDBProductId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();
        var contacts = await _db.CMDBProductContacts.AsNoTracking()
            .Select(c => new { c.CMDBProductId, Role = c.FipsContactRole.Name })
            .ToListAsync(cancellationToken);

        var rolesByProduct = contacts
            .GroupBy(c => c.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Role).Where(n => !string.IsNullOrWhiteSpace(n)).ToList());

        var active = products.Where(p => p.Status == CMDBProductStatus.Active).ToList();
        var total = active.Count;

        bool HasRole(Guid productId, string role) =>
            rolesByProduct.TryGetValue(productId, out var names)
            && names.Any(n => string.Equals(n, role, StringComparison.OrdinalIgnoreCase));

        bool HasAnyRole(Guid productId) =>
            rolesByProduct.TryGetValue(productId, out var names) && names.Count > 0;

        var fields = new (string Field, string Hint, Func<int> Complete)[]
        {
            ("Service owner", "Service owner contact", () => active.Count(p => HasRole(p.Id, "Service Owner"))),
            ("Information asset owner", "Information asset owner contact", () => active.Count(p => HasRole(p.Id, "Information Asset Owner"))),
            ("Roles", "At least one contact role", () => active.Count(p => HasAnyRole(p.Id))),
            ("Type", "At least one type", () => active.Count(p => typeIds.Contains(p.Id))),
            ("Channels", "At least one channel", () => active.Count(p => channelIds.Contains(p.Id))),
            ("User groups", "At least one user group", () => active.Count(p => userGroupIds.Contains(p.Id))),
            ("Phase", "Phase recorded", () => active.Count(p => p.HasPhase))
        };

        var rows = fields.Select(f =>
        {
            var complete = f.Complete();
            return new ExecutiveFieldCompletionRow
            {
                Field = f.Field,
                Hint = f.Hint,
                Complete = complete,
                Missing = Math.Max(0, total - complete),
                CompletionPercent = Percent(complete, total)
            };
        }).ToList();

        var criteriaMet = rows.Sum(r => r.Complete);
        var overall = total == 0
            ? 0m
            : Math.Round(criteriaMet * 100m / (total * rows.Count), 1, MidpointRounding.AwayFromZero);

        ExecutiveServiceListRow MapService(Guid id, int uniqueId, string title, string? phaseName, IEnumerable<string> businessAreaNames) =>
            new()
            {
                ProductId = id,
                UniqueId = uniqueId,
                Title = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim(),
                Phase = string.IsNullOrWhiteSpace(phaseName) ? "Not set" : phaseName.Trim(),
                BusinessAreas = !businessAreaNames.Any()
                    ? "Not set"
                    : string.Join(", ", businessAreaNames
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            };

        var activeServices = active
            .OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.UniqueID)
            .Select(p => MapService(
                p.Id,
                p.UniqueID,
                p.Title,
                p.PhaseName,
                businessAreaNames.GetValueOrDefault(p.Id) ?? new List<string>()))
            .ToList();
        var enterpriseServices = active
            .Where(p => p.IsEnterpriseService)
            .OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.UniqueID)
            .Select(p => MapService(
                p.Id,
                p.UniqueID,
                p.Title,
                p.PhaseName,
                businessAreaNames.GetValueOrDefault(p.Id) ?? new List<string>()))
            .ToList();

        return new ExecutiveServiceRegisterSummary
        {
            ActiveCount = total,
            NewCount = products.Count(p => p.Status == CMDBProductStatus.New),
            RetiredCount = products.Count(p => p.Status == CMDBProductStatus.Inactive),
            RejectedCount = products.Count(p => p.Status == CMDBProductStatus.Rejected),
            EnterpriseCount = enterpriseServices.Count,
            OverallCompletionPercent = overall,
            DataQuality = rows,
            ActiveServices = activeServices,
            EnterpriseServices = enterpriseServices
        };
    }

    private async Task<(ExecutivePerformanceSummary Summary, string? Error)> BuildPerformanceAsync(CancellationToken cancellationToken)
    {
        var page = await _performance.BuildPerformancePageAsync(null, null, null, cancellationToken);
        if (!string.IsNullOrWhiteSpace(page.LoadError))
        {
            return (new ExecutivePerformanceSummary(), page.LoadError);
        }

        var commissions = page.Commissions
            .Where(c => c.ReturnRatePercent > 0)
            .OrderByDescending(c => c.DueDate)
            .Select(c => new ExecutiveCommissionRow
            {
                CommissionId = c.CommissionId,
                Name = c.Name,
                DueDate = c.DueDate,
                ProductsInScope = c.ProductsInScope,
                ReturnRatePercent = c.ReturnRatePercent,
                MetricCompletionPercent = c.MetricCompletionPercent
            })
            .ToList();

        var weight = commissions.Sum(c => c.ProductsInScope);
        decimal Weighted(Func<ExecutiveCommissionRow, decimal> value)
        {
            if (commissions.Count == 0) return 0m;
            if (weight <= 0) return Math.Round(commissions.Average(value), 1, MidpointRounding.AwayFromZero);
            return Math.Round(commissions.Sum(c => value(c) * c.ProductsInScope) / weight, 1, MidpointRounding.AwayFromZero);
        }

        var recent = commissions.Take(3).ToList();
        var breakdowns = await _performance.BuildBusinessAreaBreakdownsAsync(
            recent.Select(c => c.CommissionId).ToList(),
            cancellationToken);
        var breakdownById = breakdowns.ToDictionary(b => b.CommissionId);

        var rounds = recent.Select(c =>
        {
            breakdownById.TryGetValue(c.CommissionId, out var breakdown);
            var areas = (breakdown?.BusinessAreas ?? new List<ModernReportingPerformanceBusinessAreaRow>())
                .Select(ba => new ExecutiveBusinessAreaCompletionRow
                {
                    BusinessArea = string.IsNullOrWhiteSpace(ba.BusinessArea) ? "Unassigned" : ba.BusinessArea,
                    Total = ba.Total,
                    Returned = ba.Returned,
                    ReturnRatePercent = ba.ReturnRatePercent,
                    MetricCompletionPercent = ba.MetricCompletionPercent
                })
                .OrderBy(ba => ba.ReturnRatePercent)
                .ThenBy(ba => ba.BusinessArea, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new ExecutiveCommissionBusinessAreaTab
            {
                CommissionId = c.CommissionId,
                Name = c.Name,
                DueDate = c.DueDate,
                BusinessAreas = areas
            };
        }).ToList();

        return (new ExecutivePerformanceSummary
        {
            CommissionCount = commissions.Count,
            AverageReturnRatePercent = Weighted(c => c.ReturnRatePercent),
            AverageMetricCompletionPercent = Weighted(c => c.MetricCompletionPercent),
            Commissions = commissions,
            BusinessAreaRounds = rounds
        }, null);
    }

    private static ExecutiveAccessibilitySummary MapAccessibility(AissPlatformSummary summary)
    {
        var criteria = summary.IssueCriteria;
        var conformance = new (string Label, AissCriterionTrio? Trio)[]
        {
            ("WCAG A", criteria?.A),
            ("WCAG AA", criteria?.Aa),
            ("WCAG AAA", criteria?.Aaa),
            ("Best practice", criteria?.Bp),
            ("Usability (UX)", criteria?.Ux),
            ("Other", criteria?.Other)
        };

        return new ExecutiveAccessibilitySummary
        {
            Loaded = true,
            Onboarded = summary.Services?.Onboarded ?? 0,
            StatementInstalled = summary.Services?.Installed ?? 0,
            OpenIssues = summary.Issues?.Open ?? 0,
            OverdueIssues = summary.Issues?.Overdue ?? 0,
            ClosedIssues = summary.Issues?.Closed ?? 0,
            ActiveProductCount = summary.Compass?.ActiveProductCount ?? 0,
            ProductsNotOnboarded = summary.Compass?.ProductsNotOnboardedInThisApp ?? 0,
            CompassNote = string.IsNullOrWhiteSpace(summary.Compass?.Error) ? null : summary.Compass!.Error,
            Conformance = conformance.Select(c => new ExecutiveCriterionRow
            {
                Criterion = c.Label,
                Open = c.Trio?.Open ?? 0,
                Overdue = c.Trio?.Overdue ?? 0,
                Closed = c.Trio?.Closed ?? 0
            }).ToList(),
            BusinessAreas = (summary.ByBusinessArea ?? new List<AissByBusinessAreaRow>())
                .Select(ba => new ExecutiveAccessibilityAreaRow
                {
                    BusinessArea = string.IsNullOrWhiteSpace(ba.BusinessArea) ? "Not set" : ba.BusinessArea!,
                    Open = ba.Open,
                    Overdue = ba.Overdue,
                    Closed = ba.Closed
                })
                .OrderByDescending(ba => ba.Overdue)
                .ThenByDescending(ba => ba.Open)
                .ThenBy(ba => ba.BusinessArea, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    private static ExecutiveCountRow CountRow(string label, int count, int total) => new()
    {
        Label = label,
        Count = count,
        Percent = Percent(count, total)
    };

    private static decimal Percent(int count, int total) =>
        total <= 0 ? 0m : Math.Round(count * 100m / total, 1, MidpointRounding.AwayFromZero);
}
