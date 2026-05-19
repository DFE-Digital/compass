using System.Data.Common;
using System.Linq;
using Compass.Configuration;
using Compass.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace Compass.Infrastructure;

/// <summary>Logs EF Core SQL commands as API request/response while <see cref="WorkRegisterDiagnosticsScope"/> is active.</summary>
public sealed class WorkRegisterSqlDiagnosticsInterceptor : DbCommandInterceptor
{
    private readonly WorkRegisterPerfFileLog _fileLog;
    private readonly WorkRegisterDiagnosticsOptions _options;

    public WorkRegisterSqlDiagnosticsInterceptor(
        WorkRegisterPerfFileLog fileLog,
        IOptions<WorkRegisterDiagnosticsOptions> options)
    {
        _fileLog = fileLog;
        _options = options.Value;
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogResponse(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogResponse(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        LogResponse(command, eventData, result);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogResponse(command, eventData, result);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        LogResponse(command, eventData, result);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogResponse(command, eventData, result);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogResponse(DbCommand command, CommandExecutedEventData eventData, object? result = null)
    {
        if (!_options.LogSqlCommands || !WorkRegisterDiagnosticsScope.IsActive)
            return;

        var ms = eventData.Duration.TotalMilliseconds;
        var resultNote = result switch
        {
            null => "reader",
            int n => $"rowsAffected={n}",
            _ => $"scalar={Truncate(result.ToString(), 80)}"
        };
        var sql = command.CommandText?.ReplaceLineEndings(" ") ?? "";
        WorkRegisterDiagnosticsScope.RecordSql(ms, sql, resultNote);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max] + "…";
}
