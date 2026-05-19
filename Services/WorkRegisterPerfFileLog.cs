using System.Text.Json;
using System.Text.Json.Serialization;
using Compass.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Compass.Services;

/// <summary>Appends work-register performance diagnostics to a dedicated log file (default <c>logs/log.log</c>).</summary>
public sealed class WorkRegisterPerfFileLog
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly bool _enabled;
    private readonly string _path;
    private readonly int _maxJsonChars;

    public WorkRegisterPerfFileLog(IOptions<WorkRegisterDiagnosticsOptions> options, IHostEnvironment env)
    {
        var opt = options.Value;
        _enabled = opt.Enabled;
        _maxJsonChars = opt.MaxJsonLogChars > 0 ? opt.MaxJsonLogChars : 100_000;
        var relative = string.IsNullOrWhiteSpace(opt.LogFilePath) ? "logs/log.log" : opt.LogFilePath;
        _path = Path.IsPathRooted(relative)
            ? relative
            : Path.Combine(env.ContentRootPath, relative);
    }

    public bool IsEnabled => _enabled;

    public int MaxJsonChars => _maxJsonChars;

    public string LogFilePath => _path;

    public void Write(string message)
    {
        if (!_enabled)
            return;

        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC {message}{Environment.NewLine}";
        Gate.Wait();
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(_path, line);
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task WriteAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!_enabled)
            return;

        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC {message}{Environment.NewLine}";
        await Gate.WaitAsync(cancellationToken);
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.AppendAllTextAsync(_path, line, cancellationToken);
        }
        finally
        {
            Gate.Release();
        }
    }

    public void WriteBlock(string heading, string body)
    {
        if (!_enabled)
            return;

        Write($"---------- {heading} ----------");
        foreach (var line in body.ReplaceLineEndings("\n").Split('\n'))
            Write(line);
        Write("----------");
    }

    public void WriteJsonBlock(string heading, object? payload)
    {
        if (!_enabled || payload == null)
            return;

        try
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            if (json.Length > _maxJsonChars)
                json = json[.._maxJsonChars] + Environment.NewLine + $"... truncated ({json.Length - _maxJsonChars} chars omitted)";
            WriteBlock(heading, json);
        }
        catch (Exception ex)
        {
            WriteBlock(heading, $"(serialization failed: {ex.Message})");
        }
    }

    public void WriteSqlSummary(IReadOnlyList<WorkRegisterSqlLogEntry> entries)
    {
        if (!_enabled || entries.Count == 0)
            return;

        var totalMs = entries.Sum(e => e.DurationMs);
        Write($"---------- API RESPONSE (SQL summary) commands={entries.Count} totalSqlMs={totalMs:F1} ----------");
        foreach (var entry in entries)
        {
            var sql = entry.CommandText.Length > 160
                ? entry.CommandText[..160] + "…"
                : entry.CommandText;
            Write($"  {entry.DurationMs,7:F1}ms [{entry.ResultNote}] {sql}");
        }

        Write("----------");
    }
}
