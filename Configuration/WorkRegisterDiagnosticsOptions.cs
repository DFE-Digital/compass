namespace Compass.Configuration;

/// <summary>When enabled, logs work register (All work / Manage work) load size and timing to help tune performance.</summary>
public sealed class WorkRegisterDiagnosticsOptions
{
    public const string SectionName = "WorkRegisterDiagnostics";

    /// <summary>Master switch for file perf logging and related diagnostics. Off by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Log estimated JSON size of the view model sent to the Razor view.</summary>
    public bool LogViewModelJsonSize { get; set; } = true;

    /// <summary>Log HTML response body size for register actions.</summary>
    public bool LogHtmlResponseSize { get; set; } = true;

    /// <summary>Append request/response and load timings to this file (relative to content root).</summary>
    public string LogFilePath { get; set; } = "logs/log.log";

    /// <summary>Log service API request/response JSON to the perf log file.</summary>
    public bool LogApiData { get; set; } = true;

    /// <summary>Log each EF SQL command (buffered and flushed once per request). Disable for faster local runs.</summary>
    public bool LogSqlCommands { get; set; } = true;

    /// <summary>Maximum characters written per JSON payload block in the perf log.</summary>
    public int MaxJsonLogChars { get; set; } = 100_000;
}
