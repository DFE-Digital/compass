using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Compass;

/// <summary>
/// Copies service census tables from production into development.
/// Production is opened for reads only. Every write runs on the development connection.
/// </summary>
public static class CensusProductionToDevelopmentRefresh
{
    public const string ConfirmationPhrase = "REFRESH CENSUS FROM PRODUCTION";

    /// <summary>Child-before-parent for deletes; reversed for inserts.</summary>
    private static readonly string[] CensusTables =
    [
        "ServiceCensusAnswers",
        "ServiceCensusContactReviews",
        "ServiceCensusTaskStates",
        "ServiceCensusResponses",
        "CensusTechnologyLookups",
        "CensusIntegrationLookups"
    ];

    public static async Task RunAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var sourceConnectionString = ResolveConnectionString("Production", "COMPASS_PRODUCTION_SQL");
        var targetConnectionString = ResolveConnectionString("Development", "COMPASS_DEVELOPMENT_SQL");
        ProductionToDevelopmentRefresh.EnsureDifferentDatabases(sourceConnectionString, targetConnectionString);

        var sourceBuilder = new SqlConnectionStringBuilder(sourceConnectionString)
        {
            ApplicationIntent = ApplicationIntent.ReadOnly,
            ConnectTimeout = 30
        };
        var targetBuilder = new SqlConnectionStringBuilder(targetConnectionString)
        {
            ConnectTimeout = 30
        };

        Console.WriteLine(dryRun
            ? "Dry run: census tables will be read from production; development will not be changed."
            : "Refreshing development census tables from production. Production will not be written to.");
        Console.WriteLine($"Source database: {sourceBuilder.InitialCatalog} on {ProductionToDevelopmentRefresh.NormaliseServer(sourceBuilder.DataSource)}");
        Console.WriteLine($"Target database: {targetBuilder.InitialCatalog} on {ProductionToDevelopmentRefresh.NormaliseServer(targetBuilder.DataSource)}");

        await using var source = new SqlConnection(sourceBuilder.ConnectionString);
        await using var target = new SqlConnection(targetBuilder.ConnectionString);
        await source.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);

        var sourceIdentity = await ReadDatabaseIdentityAsync(source, cancellationToken);
        var targetIdentity = await ReadDatabaseIdentityAsync(target, cancellationToken);
        if (string.Equals(sourceIdentity, targetIdentity, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The open connections resolved to the same database. Refusing to continue.");

        foreach (var table in CensusTables)
        {
            if (!await TableExistsAsync(source, table, cancellationToken))
                throw new InvalidOperationException($"Production is missing table dbo.{table}.");
            if (!await TableExistsAsync(target, table, cancellationToken))
                throw new InvalidOperationException($"Development is missing table dbo.{table}.");
        }

        Console.WriteLine("Census tables:");
        foreach (var table in CensusTables)
        {
            var count = await CountRowsAsync(source, table, cancellationToken);
            Console.WriteLine($"  dbo.{table}: {count} production rows");
        }

        if (dryRun)
        {
            Console.WriteLine("Dry run finished. Development was not changed.");
            return;
        }

        var orphaned = await CountOrphanedResponsesAsync(source, target, cancellationToken);
        if (orphaned > 0)
        {
            Console.WriteLine(
                $"Warning: {orphaned} production census responses reference products that are not in development. Those rows will be skipped.");
        }

        var columnsByTable = new Dictionary<string, List<SqlColumn>>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in CensusTables)
        {
            var columns = await CommonInsertableColumnsAsync(source, target, table, cancellationToken);
            columnsByTable[table] = columns;
        }

        var productIds = await LoadProductIdsAsync(target, null, cancellationToken);

        await using var tx = (SqlTransaction)await target.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var table in CensusTables)
            {
                await DisableTriggersAsync(target, tx, table, cancellationToken);
                await DeleteTargetRowsAsync(target, tx, table, cancellationToken);
            }

            // Lookups first, then response tree (parent before children).
            foreach (var table in CensusTables.Reverse())
            {
                var columns = columnsByTable[table];
                if (columns.Count == 0)
                {
                    Console.WriteLine($"  skip dbo.{table}: no insertable columns in common");
                    continue;
                }

                var rows = table == "ServiceCensusResponses"
                    ? await BulkCopyResponsesWithProductsAsync(source, target, tx, columns, productIds, cancellationToken)
                    : table is "ServiceCensusTaskStates" or "ServiceCensusAnswers" or "ServiceCensusContactReviews"
                        ? await BulkCopyDependentAsync(source, target, tx, table, columns, cancellationToken)
                        : await BulkCopyAllAsync(source, target, tx, table, columns, cancellationToken);

                await ReseedIdentityAsync(target, tx, table, columns, cancellationToken);
                Console.WriteLine($"  dbo.{table}: {rows} rows copied");
            }

            foreach (var table in CensusTables)
                await EnableTriggersAsync(target, tx, table);

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            throw;
        }

        Console.WriteLine("Census refresh finished. Production was not modified.");
    }

    private static string ResolveConnectionString(string environmentName, string environmentVariable)
    {
        var fromEnvironment = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No connection string for {environmentName}. Set {environmentVariable}, or DefaultConnection in appsettings.{environmentName}.json.");
        }

        return connectionString;
    }

    private static async Task<string> ReadDatabaseIdentityAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT @@SERVERNAME + N'|' + DB_NAME()";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string table, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("""
            SELECT 1
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE s.name = N'dbo' AND t.name = @table
            """, connection);
        command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = table });
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value != null && value != DBNull.Value;
    }

    private static async Task<long> CountRowsAsync(SqlConnection connection, string table, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"SELECT COUNT_BIG(*) FROM {Qualify(table)}", connection);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static async Task<HashSet<Guid>> LoadProductIdsAsync(
        SqlConnection target, SqlTransaction? tx, CancellationToken cancellationToken)
    {
        var productIds = new HashSet<Guid>();
        await using var command = new SqlCommand("SELECT Id FROM dbo.CMDBProducts", target, tx);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            productIds.Add(reader.GetGuid(0));
        return productIds;
    }

    private static async Task<long> CountOrphanedResponsesAsync(
        SqlConnection source, SqlConnection target, CancellationToken cancellationToken)
    {
        var productIds = await LoadProductIdsAsync(target, null, cancellationToken);

        long orphaned = 0;
        await using var command = new SqlCommand("SELECT ProductId FROM dbo.ServiceCensusResponses", source);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!productIds.Contains(reader.GetGuid(0)))
                orphaned++;
        }

        return orphaned;
    }

    private static async Task<List<SqlColumn>> CommonInsertableColumnsAsync(
        SqlConnection source, SqlConnection target, string table, CancellationToken cancellationToken)
    {
        var sourceColumns = await ListInsertableColumnsAsync(source, table, cancellationToken);
        var targetColumns = await ListInsertableColumnsAsync(target, table, cancellationToken);
        var targetNames = targetColumns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return sourceColumns.Where(c => targetNames.Contains(c.Name)).ToList();
    }

    private static async Task<List<SqlColumn>> ListInsertableColumnsAsync(
        SqlConnection connection, string table, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.name, c.is_identity
            FROM sys.columns c
            INNER JOIN sys.tables t ON t.object_id = c.object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE s.name = N'dbo'
              AND t.name = @table
              AND c.is_computed = 0
              AND ty.name NOT IN (N'timestamp', N'rowversion')
            ORDER BY c.column_id
            """;

        var columns = new List<SqlColumn>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = table });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add(new SqlColumn(reader.GetString(0), reader.GetBoolean(1)));
        return columns;
    }

    private static async Task DeleteTargetRowsAsync(
        SqlConnection target, SqlTransaction tx, string table, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"DELETE FROM {Qualify(table)}", target, tx) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> BulkCopyAllAsync(
        SqlConnection source,
        SqlConnection target,
        SqlTransaction tx,
        string table,
        IReadOnlyList<SqlColumn> columns,
        CancellationToken cancellationToken)
    {
        var selectList = string.Join(", ", columns.Select(c => Quote(c.Name)));
        await using var read = new SqlCommand($"SELECT {selectList} FROM {Qualify(table)}", source) { CommandTimeout = 0 };
        await using var reader = await read.ExecuteReaderAsync(cancellationToken);
        return await WriteBulkAsync(target, tx, table, columns, reader, cancellationToken);
    }

    private static async Task<long> BulkCopyResponsesWithProductsAsync(
        SqlConnection source,
        SqlConnection target,
        SqlTransaction tx,
        IReadOnlyList<SqlColumn> columns,
        HashSet<Guid> productIds,
        CancellationToken cancellationToken)
    {
        var selectList = string.Join(", ", columns.Select(c => "r." + Quote(c.Name)));
        // Only copy responses whose product already exists in development.
        await using var read = new SqlCommand($"""
            SELECT {selectList}
            FROM dbo.ServiceCensusResponses r
            """, source) { CommandTimeout = 0 };
        await using var reader = await read.ExecuteReaderAsync(cancellationToken);

        var table = new DataTable();
        foreach (var column in columns)
            table.Columns.Add(column.Name, typeof(object));

        var productOrdinal = -1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), "ProductId", StringComparison.OrdinalIgnoreCase))
            {
                productOrdinal = i;
                break;
            }
        }

        long kept = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (productOrdinal >= 0)
            {
                var productId = reader.GetGuid(productOrdinal);
                if (!productIds.Contains(productId))
                    continue;
            }

            var row = table.NewRow();
            for (var i = 0; i < columns.Count; i++)
                row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            table.Rows.Add(row);
            kept++;
        }

        if (kept == 0)
            return 0;

        using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints, tx)
        {
            DestinationTableName = Qualify("ServiceCensusResponses"),
            BulkCopyTimeout = 0,
            BatchSize = 2000
        };
        foreach (var column in columns)
            bulk.ColumnMappings.Add(column.Name, column.Name);
        await bulk.WriteToServerAsync(table, cancellationToken);
        return kept;
    }

    private static async Task<long> BulkCopyDependentAsync(
        SqlConnection source,
        SqlConnection target,
        SqlTransaction tx,
        string table,
        IReadOnlyList<SqlColumn> columns,
        CancellationToken cancellationToken)
    {
        // Keep dependents only for responses that landed in development.
        var responseIds = new HashSet<Guid>();
        await using (var command = new SqlCommand("SELECT Id FROM dbo.ServiceCensusResponses", target, tx))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                responseIds.Add(reader.GetGuid(0));
        }

        if (responseIds.Count == 0)
            return 0;

        if (table == "ServiceCensusTaskStates")
        {
            var selectList = string.Join(", ", columns.Select(c => Quote(c.Name)));
            await using var read = new SqlCommand($"SELECT {selectList} FROM {Qualify(table)}", source) { CommandTimeout = 0 };
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            return await WriteFilteredAsync(target, tx, table, columns, reader, "ResponseId", responseIds, cancellationToken);
        }

        if (table == "ServiceCensusContactReviews")
        {
            var selectList = string.Join(", ", columns.Select(c => Quote(c.Name)));
            await using var read = new SqlCommand($"SELECT {selectList} FROM {Qualify(table)}", source) { CommandTimeout = 0 };
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            return await WriteFilteredAsync(target, tx, table, columns, reader, "ResponseId", responseIds, cancellationToken);
        }

        // Answers: keep rows whose TaskState belongs to a copied response.
        var taskStateIds = new HashSet<Guid>();
        await using (var command = new SqlCommand("SELECT Id FROM dbo.ServiceCensusTaskStates", target, tx))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                taskStateIds.Add(reader.GetGuid(0));
        }

        if (taskStateIds.Count == 0)
            return 0;

        {
            var selectList = string.Join(", ", columns.Select(c => Quote(c.Name)));
            await using var read = new SqlCommand($"SELECT {selectList} FROM {Qualify(table)}", source) { CommandTimeout = 0 };
            await using var reader = await read.ExecuteReaderAsync(cancellationToken);
            return await WriteFilteredAsync(target, tx, table, columns, reader, "TaskStateId", taskStateIds, cancellationToken);
        }
    }

    private static async Task<long> WriteFilteredAsync(
        SqlConnection target,
        SqlTransaction tx,
        string table,
        IReadOnlyList<SqlColumn> columns,
        SqlDataReader reader,
        string filterColumn,
        HashSet<Guid> allowedIds,
        CancellationToken cancellationToken)
    {
        var filterOrdinal = -1;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), filterColumn, StringComparison.OrdinalIgnoreCase))
            {
                filterOrdinal = i;
                break;
            }
        }

        if (filterOrdinal < 0)
            throw new InvalidOperationException($"Column {filterColumn} not found on {table}.");

        var data = new DataTable();
        foreach (var column in columns)
            data.Columns.Add(column.Name, typeof(object));

        long kept = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetGuid(filterOrdinal);
            if (!allowedIds.Contains(id))
                continue;

            var row = data.NewRow();
            for (var i = 0; i < columns.Count; i++)
                row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
            data.Rows.Add(row);
            kept++;
        }

        if (kept == 0)
            return 0;

        using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints, tx)
        {
            DestinationTableName = Qualify(table),
            BulkCopyTimeout = 0,
            BatchSize = 2000
        };
        foreach (var column in columns)
            bulk.ColumnMappings.Add(column.Name, column.Name);
        await bulk.WriteToServerAsync(data, cancellationToken);
        return kept;
    }

    private static async Task<long> WriteBulkAsync(
        SqlConnection target,
        SqlTransaction tx,
        string table,
        IReadOnlyList<SqlColumn> columns,
        SqlDataReader reader,
        CancellationToken cancellationToken)
    {
        long before = 0;
        using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints, tx)
        {
            DestinationTableName = Qualify(table),
            BulkCopyTimeout = 0,
            BatchSize = 2000,
            NotifyAfter = 5000
        };
        bulk.SqlRowsCopied += (_, e) => before = e.RowsCopied;
        foreach (var column in columns)
            bulk.ColumnMappings.Add(column.Name, column.Name);
        await bulk.WriteToServerAsync(reader, cancellationToken);

        // SqlBulkCopy does not always raise NotifyAfter for small batches; recount from target.
        await using var count = new SqlCommand($"SELECT COUNT_BIG(*) FROM {Qualify(table)}", target, tx);
        var value = await count.ExecuteScalarAsync(cancellationToken);
        _ = before;
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static async Task ReseedIdentityAsync(
        SqlConnection target,
        SqlTransaction tx,
        string table,
        IReadOnlyList<SqlColumn> columns,
        CancellationToken cancellationToken)
    {
        var identity = columns.FirstOrDefault(c => c.IsIdentity);
        if (identity == null)
            return;

        var sql = $"""
            DECLARE @max bigint = (SELECT ISNULL(MAX({Quote(identity.Name)}), 0) FROM {Qualify(table)});
            IF @max > 0
                DBCC CHECKIDENT (N'{Qualify(table).Replace("'", "''", StringComparison.Ordinal)}', RESEED, @max);
            """;
        await using var command = new SqlCommand(sql, target, tx) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DisableTriggersAsync(
        SqlConnection target, SqlTransaction tx, string table, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new SqlCommand($"DISABLE TRIGGER ALL ON {Qualify(table)}", target, tx) { CommandTimeout = 0 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException)
        {
            // No triggers.
        }
    }

    private static async Task EnableTriggersAsync(SqlConnection target, SqlTransaction tx, string table)
    {
        try
        {
            await using var command = new SqlCommand($"ENABLE TRIGGER ALL ON {Qualify(table)}", target, tx) { CommandTimeout = 0 };
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // No triggers.
        }
    }

    private static string Quote(string identifier) => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string Qualify(string table) => "[dbo]." + Quote(table);

    private sealed record SqlColumn(string Name, bool IsIdentity);
}
