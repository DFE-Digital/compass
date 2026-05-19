using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Compass.Configuration;
using Compass.Services;
using Compass.Models;
using Compass.Models.Modern.Work;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Compass.Services.Modern;

/// <summary>Timing and payload metrics for <see cref="ModernWorkService.BuildWorkRegisterAsync"/>.</summary>
public sealed class WorkRegisterDiagnosticsCollector
{
    private readonly ILogger _logger;
    private readonly WorkRegisterDiagnosticsOptions _options;
    private readonly WorkRegisterPerfFileLog? _fileLog;
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
        WorkRegisterPerfFileLog? fileLog = null)
    {
        _logger = logger;
        _options = options.Value;
        _fileLog = fileLog;
        _enabled = _options.Enabled;
    }

    public bool IsEnabled => _enabled;

    public void Phase(string name, long elapsedMs, string? detail = null) =>
        _phases.Add((name, elapsedMs, detail));

    public IDisposable TimePhase(string name) => new PhaseTimer(this, name);

    public void RecordEntityLoad(IReadOnlyList<Project> projects)
    {
        _entityStats = WorkRegisterEntityLoadStats.FromProjects(projects);
    }

    public void LogApiRequest(object requestPayload)
    {
        if (!IsEnabled || !_options.LogApiData)
            return;

        _fileLog?.WriteJsonBlock("API REQUEST (BuildWorkRegisterAsync)", requestPayload);
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

        var loadMessage =
            $"LOAD [{LoadPath}] mine={IsMyWork} tab={RegisterTab ?? "(none)"} page={RegisterPage?.ToString() ?? "(none)"} " +
            $"monthlyFilter={(string.IsNullOrWhiteSpace(MonthlyUpdateFilter) ? "(none)" : MonthlyUpdateFilter)} " +
            $"totalMs={_total.ElapsedMilliseconds} rowsOnPage={rowCount} registerTotal={vm.RegisterTotalCount} " +
            $"active={vm.ActiveCount} paused={vm.PausedCount} completed={vm.CompletedCount} cancelled={vm.CancelledCount} " +
            $"pageRowsJson~{FormatKb(pageRowsJsonBytes)}KB fullVmJson~{FormatKb(fullVmJsonBytes)}KB | phases: {phaseSummary}";

        _logger.LogInformation("{LoadMessage}", loadMessage);
        _fileLog?.Write(loadMessage);

        if (_options.LogApiData)
            _fileLog?.WriteJsonBlock("API RESPONSE (BuildWorkRegisterAsync)", BuildApiResponsePayload(vm));

        if (_entityStats is { } es)
        {
            var entityMessage =
                $"EF graph: projects={es.ProjectCount} milestones={es.MilestoneCount} " +
                $"monthlyUpdates={es.MonthlyUpdateCount} projectContacts={es.ProjectContactCount} " +
                $"sros={es.SeniorResponsibleOfficerCount} tags={es.TagLinkCount} " +
                $"directorateLinks={es.DirectorateLinkCount} (~{es.EstimatedSerializedKb:F1} KB rough JSON if fully serialized)";
            _logger.LogInformation("WorkRegister {EntityMessage}", entityMessage);
            _fileLog?.Write(entityMessage);
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

    private static string FormatKb(long? bytes) =>
        BytesToKb(bytes)?.ToString("F1", CultureInfo.InvariantCulture) ?? "?";

    private static object BuildApiResponsePayload(WorkRegisterViewModel vm)
    {
        if (vm.RegisterIsPaginated)
        {
            return new
            {
                vm.RegisterIsPaginated,
                vm.RegisterTab,
                vm.RegisterPage,
                vm.RegisterPageSize,
                vm.RegisterTotalCount,
                vm.RegisterPageCount,
                vm.RegisterDisplayRowStart,
                vm.RegisterDisplayRowEnd,
                vm.ActiveCount,
                vm.PausedCount,
                vm.CompletedCount,
                vm.CancelledCount,
                vm.RagRedCount,
                filterOptionCounts = new
                {
                    portfolios = vm.Portfolios.Count,
                    businessAreas = vm.BusinessAreas.Count,
                    directorates = vm.Directorates.Count,
                    primaryContacts = vm.PrimaryContactFilterOptions.Count,
                    tags = vm.TagFilterOptions.Count,
                },
                vm.RegisterPageRows,
                vm.ActiveFilterChips,
            };
        }

        return new
        {
            vm.RegisterIsPaginated,
            vm.RegisterTab,
            vm.ActiveCount,
            vm.PausedCount,
            vm.CompletedCount,
            vm.CancelledCount,
            vm.RegisterPageRows,
            vm.ActivePaused,
            vm.Completed,
            vm.Cancelled,
            vm.ActiveFilterChips,
        };
    }

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
