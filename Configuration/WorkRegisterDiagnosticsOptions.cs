namespace Compass.Configuration;

/// <summary>When enabled, logs work register (All work / Manage work) load size and timing to help tune performance.</summary>
public sealed class WorkRegisterDiagnosticsOptions
{
    public const string SectionName = "WorkRegisterDiagnostics";

    /// <summary>Master switch. Defaults to on in Development when unset.</summary>
    public bool Enabled { get; set; }

    /// <summary>Log estimated JSON size of the view model sent to the Razor view.</summary>
    public bool LogViewModelJsonSize { get; set; } = true;

    /// <summary>Log HTML response body size for register actions.</summary>
    public bool LogHtmlResponseSize { get; set; } = true;
}
