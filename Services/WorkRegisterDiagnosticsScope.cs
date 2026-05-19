namespace Compass.Services;

/// <summary>When active, EF SQL commands are buffered for a single flush per HTTP request.</summary>
public sealed class WorkRegisterDiagnosticsScope : IDisposable
{
    private static readonly AsyncLocal<int> Depth = new();
    private static readonly AsyncLocal<List<WorkRegisterSqlLogEntry>?> SqlBuffer = new();

    public static bool IsActive => Depth.Value > 0;

    public static IDisposable Begin()
    {
        Depth.Value++;
        if (Depth.Value == 1)
            SqlBuffer.Value = new List<WorkRegisterSqlLogEntry>();
        return new WorkRegisterDiagnosticsScope();
    }

    public static void RecordSql(double durationMs, string commandText, string? resultNote)
    {
        if (Depth.Value <= 0)
            return;

        SqlBuffer.Value ??= new List<WorkRegisterSqlLogEntry>();
        SqlBuffer.Value.Add(new WorkRegisterSqlLogEntry(durationMs, commandText, resultNote));
    }

    /// <summary>Returns buffered SQL entries and clears the buffer (call once per request).</summary>
    public static IReadOnlyList<WorkRegisterSqlLogEntry>? TakeSqlBuffer()
    {
        var buffer = SqlBuffer.Value;
        SqlBuffer.Value = null;
        return buffer;
    }

    public void Dispose()
    {
        if (Depth.Value > 0)
            Depth.Value--;
    }
}

public readonly record struct WorkRegisterSqlLogEntry(double DurationMs, string CommandText, string? ResultNote);
