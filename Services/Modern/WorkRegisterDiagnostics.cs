using System.Diagnostics;
using System.Text.Json;
using Compass.Configuration;
using Compass.Models;
using Compass.Models.Modern.Work;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compass.Services.Modern;

/// <summary>Timing and payload metrics for <see cref="ModernWorkService.BuildWorkRegisterAsync"/>.</summary>
public sealed class WorkRegisterDiagnosticsCollector
{
    private readonly ILogger _logger;
    private readonly WorkRegisterDiagnosticsOptions _options;
    private readonly bool _enabled;
    private readonly Stopwatch _total = Stopwatch.StartNew();
    private readonly List<(string Phase, long Ms, string? Detail)> _phases = new();
    private WorkRegisterEntityLoadStats? _entityStats;

    public string LoadPath { get; set; } = "unknown";
    public bool IsMyWork { get; set; }
    public string? RegisterTab { get; set; }
    public int? RegisterPage { get; set; }
    public string? MonthlyUpdateFilter { get; set; }

    public WorkRegisterDiagnosticsCollector(
        ILogger logger,
        IOptions<WorkRegisterDiagnosticsOptions> options,
        IHostEnvironment env)
    {
        _logger = logger;
        _options = options.Value;
        _enabled = _options.Enabled || env.IsDevelopment();
    }

    public bool IsEnabled => _enabled;

    public void Phase(string name, long elapsedMs, string? detail = null) =>
        _phases.Add((name, elapsedMs, detail));

    public IDisposable TimePhase(string name) => new PhaseTimer(this, name);

    public void RecordEntityLoad(IReadOnlyList<Project> projects)
    {
        _entityStats = WorkRegisterEntityLoadStats.FromProjects(projects);
    }

    public void Complete(WorkRegisterViewModel vm)
    {
        if (!IsEnabled)
            return;

        _total.Stop();
        var rowCount = vm.RegisterIsPaginated
            ? vm.RegisterPageRows.Count
            : vm.ActivePaused.Count + vm.Completed.Count + vm.Cancelled.Count;

        long? pageRowsJsonBytes = null;
        long? fullVmJsonBytes = null;
        if (_options.LogViewModelJsonSize)
        {
            pageRowsJsonBytes = EstimateJsonBytes(vm.RegisterPageRows);
            fullVmJsonBytes = EstimateJsonBytes(new
            {
                vm.RegisterPageRows,
                vm.ActivePaused,
                vm.Completed,
                vm.Cancelled,
                vm.Portfolios,
                vm.BusinessAreas,
                vm.Directorates,
                vm.DeliveryPhaseOptions,
                vm.RagOptions,
                vm.PriorityOptions,
                vm.PrimaryContactFilterOptions,
                vm.TagFilterOptions,
                vm.ActiveFilterChips,
            });
        }

        var phaseSummary = string.Join("; ", _phases.Select(p =>
            string.IsNullOrEmpty(p.Detail) ? $"{p.Phase}={p.Ms}ms" : $"{p.Phase}={p.Ms}ms ({p.Detail})"));

        _logger.LogInformation(
            "WorkRegister load [{LoadPath}] mine={IsMyWork} tab={Tab} page={Page} monthlyFilter={MonthlyFilter} " +
            "totalMs={TotalMs} rowsOnPage={RowCount} registerTotal={RegisterTotal} " +
            "active={Active} paused={Paused} completed={Completed} cancelled={Cancelled} " +
            "pageRowsJson~{PageRowsJsonKb:F1}KB fullVmJson~{FullVmJsonKb:F1}KB | phases: {Phases}",
            LoadPath,
            IsMyWork,
            RegisterTab ?? "(none)",
            RegisterPage?.ToString() ?? "(none)",
            string.IsNullOrWhiteSpace(MonthlyUpdateFilter) ? "(none)" : MonthlyUpdateFilter,
            _total.ElapsedMilliseconds,
            rowCount,
            vm.RegisterTotalCount,
            vm.ActiveCount,
            vm.PausedCount,
            vm.CompletedCount,
            vm.CancelledCount,
            BytesToKb(pageRowsJsonBytes),
            BytesToKb(fullVmJsonBytes),
            phaseSummary);

        if (_entityStats is { } es)
        {
            _logger.LogInformation(
                "WorkRegister EF graph loaded for page: projects={Projects} milestones={Milestones} " +
                "monthlyUpdates={MonthlyUpdates} projectContacts={Contacts} sros={Sros} tags={Tags} " +
                "directorateLinks={DirectorateLinks} (~{EstimatedGraphKb:F1} KB rough JSON if fully serialized)",
                es.ProjectCount,
                es.MilestoneCount,
                es.MonthlyUpdateCount,
                es.ProjectContactCount,
                es.SeniorResponsibleOfficerCount,
                es.TagLinkCount,
                es.DirectorateLinkCount,
                es.EstimatedSerializedKb);
        }
    }

    private static long? EstimateJsonBytes<T>(T value)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(value).LongLength;
        }
        catch
        {
            return null;
        }
    }

    private static double? BytesToKb(long? bytes) => bytes.HasValue ? bytes.Value / 1024.0 : null;

    private sealed class PhaseTimer : IDisposable
    {
        private readonly WorkRegisterDiagnosticsCollector _owner;
        private readonly string _name;
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        public PhaseTimer(WorkRegisterDiagnosticsCollector owner, string name)
        {
            _owner = owner;
            _name = name;
        }

        public void Dispose()
        {
            _sw.Stop();
            _owner.Phase(_name, _sw.ElapsedMilliseconds);
        }
    }
}

public sealed class WorkRegisterEntityLoadStats
{
    public int ProjectCount { get; init; }
    public int MilestoneCount { get; init; }
    public int MonthlyUpdateCount { get; init; }
    public int ProjectContactCount { get; init; }
    public int SeniorResponsibleOfficerCount { get; init; }
    public int TagLinkCount { get; init; }
    public int DirectorateLinkCount { get; init; }
    public double EstimatedSerializedKb { get; init; }

    public static WorkRegisterEntityLoadStats FromProjects(IReadOnlyList<Project> projects)
    {
        var milestones = 0;
        var monthly = 0;
        var contacts = 0;
        var sros = 0;
        var tags = 0;
        var dirs = 0;
        foreach (var p in projects)
        {
            milestones += p.Milestones?.Count ?? 0;
            monthly += p.MonthlyUpdates?.Count ?? 0;
            contacts += p.ProjectContacts?.Count ?? 0;
            sros += p.SeniorResponsibleOfficers?.Count ?? 0;
            tags += p.ProjectWorkItemTags?.Count ?? 0;
            dirs += p.Directorates?.Count ?? 0;
        }

        // Rough order-of-magnitude for “how much came back from SQL” (not exact).
        var roughBytes = projects.Count * 2048L
            + milestones * 256L
            + monthly * 512L
            + contacts * 128L
            + sros * 128L
            + tags * 64L
            + dirs * 64L;

        return new WorkRegisterEntityLoadStats
        {
            ProjectCount = projects.Count,
            MilestoneCount = milestones,
            MonthlyUpdateCount = monthly,
            ProjectContactCount = contacts,
            SeniorResponsibleOfficerCount = sros,
            TagLinkCount = tags,
            DirectorateLinkCount = dirs,
            EstimatedSerializedKb = roughBytes / 1024.0,
        };
    }
}
