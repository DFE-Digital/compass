using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Compass;

/// <summary>
/// Copies production data into the development database.
/// Production is opened for reads only. Every write runs on the development connection.
/// </summary>
public static class ProductionToDevelopmentRefresh
{
    public const string ConfirmationPhrase = "REFRESH DEVELOPMENT FROM PRODUCTION";

    private static readonly HashSet<string> SkippedTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "__EFMigrationsHistory",
        "sysdiagrams"
    };

    public static bool IsProductionEnvironmentName(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
            return false;

        var name = environmentName.Trim();
        return name.Equals("Production", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Prod", StringComparison.OrdinalIgnoreCase)
            || name.Contains("production", StringComparison.OrdinalIgnoreCase);
    }

    public static void EnsureEnvironmentIsNotProduction(string targetEnvironment)
    {
        if (IsProductionEnvironmentName(targetEnvironment))
        {
            throw new InvalidOperationException(
                "Refusing to write. The target is a production environment, and production cannot be updated.");
        }
    }

    public static void EnsureDifferentDatabases(string sourceConnectionString, string targetConnectionString)
    {
        var source = new SqlConnectionStringBuilder(sourceConnectionString);
        var target = new SqlConnectionStringBuilder(targetConnectionString);

        if (string.IsNullOrWhiteSpace(source.InitialCatalog) || string.IsNullOrWhiteSpace(target.InitialCatalog))
        {
            throw new InvalidOperationException("Both connection strings must name a database (Initial Catalog).");
        }

        if (string.Equals(DatabaseKey(source), DatabaseKey(target), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Source and target are the same database. Refusing to continue so production cannot be overwritten.");
        }
    }

    public static async Task RunAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var sourceConnectionString = ResolveConnectionString("Production", "COMPASS_PRODUCTION_SQL");
        var targetConnectionString = ResolveConnectionString("Development", "COMPASS_DEVELOPMENT_SQL");
        EnsureDifferentDatabases(sourceConnectionString, targetConnectionString);

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
            ? "Dry run: production will be read, and development will not be changed."
            : "Refreshing development from production. Production will not be written to.");
        Console.WriteLine($"Source database: {sourceBuilder.InitialCatalog} on {NormaliseServer(sourceBuilder.DataSource)}");
        Console.WriteLine($"Target database: {targetBuilder.InitialCatalog} on {NormaliseServer(targetBuilder.DataSource)}");

        await using var source = new SqlConnection(sourceBuilder.ConnectionString);
        await using var target = new SqlConnection(targetBuilder.ConnectionString);
        await source.OpenAsync(cancellationToken);
        await target.OpenAsync(cancellationToken);

        var sourceIdentity = await ReadDatabaseIdentityAsync(source, cancellationToken);
        var targetIdentity = await ReadDatabaseIdentityAsync(target, cancellationToken);
        if (string.Equals(sourceIdentity, targetIdentity, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The open connections resolved to the same database. Refusing to continue.");
        }

        var tables = await ListCommonTablesAsync(source, target, cancellationToken);
        Console.WriteLine($"Tables to copy: {tables.Count}");

        if (dryRun)
        {
            foreach (var table in tables)
            {
                var count = await CountRowsAsync(source, table, cancellationToken);
                Console.WriteLine($"  {table.Schema}.{table.Name}: {count} production rows");
            }

            Console.WriteLine("Dry run finished. Development was not changed.");
            return;
        }

        var foreignKeys = await ReadForeignKeysAsync(target, cancellationToken);
        await DisableTriggersAsync(target, tables, cancellationToken);
        await DropForeignKeysAsync(target, foreignKeys, cancellationToken);

        var copied = 0;
        try
        {
            foreach (var table in tables)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var columns = await CommonInsertableColumnsAsync(source, target, table, cancellationToken);
                if (columns.Count == 0)
                {
                    Console.WriteLine($"  skip {table.Schema}.{table.Name}: no insertable columns in common");
                    continue;
                }

                var rows = await CountRowsAsync(source, table, cancellationToken);
                await DeleteTargetRowsAsync(target, table, cancellationToken);
                await BulkCopyAsync(source, target, table, columns, cancellationToken);
                await ReseedIdentityAsync(target, table, columns, cancellationToken);
                copied++;
                Console.WriteLine($"  {table.Schema}.{table.Name}: {rows} rows");
            }
        }
        finally
        {
            await RecreateForeignKeysAsync(target, foreignKeys);
            await EnableTriggersAsync(target, tables);
        }

        Console.WriteLine($"Development refresh finished. {copied} tables copied. Production was not modified.");
    }

    internal static string DatabaseKey(SqlConnectionStringBuilder builder) =>
        NormaliseServer(builder.DataSource) + "|" + builder.InitialCatalog.Trim();

    internal static string NormaliseServer(string dataSource)
    {
        var server = dataSource.Trim();
        if (server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            server = server[4..];

        var comma = server.IndexOf(',');
        if (comma >= 0)
            server = server[..comma];

        return server.Trim().ToLowerInvariant();
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

    private static async Task<List<SqlTable>> ListCommonTablesAsync(
        SqlConnection source,
        SqlConnection target,
        CancellationToken cancellationToken)
    {
        var sourceTables = await ListTablesAsync(source, cancellationToken);
        var targetTables = await ListTablesAsync(target, cancellationToken);
        var targetSet = targetTables.Select(t => t.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return sourceTables.Where(t => targetSet.Contains(t.Key)).ToList();
    }

    private static async Task<List<SqlTable>> ListTablesAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.name, t.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
            ORDER BY s.name, t.name
            """;

        var tables = new List<SqlTable>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var table = new SqlTable(reader.GetString(0), reader.GetString(1));
            if (!SkippedTables.Contains(table.Name))
                tables.Add(table);
        }

        return tables;
    }

    private static async Task<long> CountRowsAsync(SqlConnection connection, SqlTable table, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"SELECT COUNT_BIG(*) FROM {table.QualifiedName}", connection);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    private static async Task<List<SqlColumn>> CommonInsertableColumnsAsync(
        SqlConnection source,
        SqlConnection target,
        SqlTable table,
        CancellationToken cancellationToken)
    {
        var sourceColumns = await ListInsertableColumnsAsync(source, table, cancellationToken);
        var targetColumns = await ListInsertableColumnsAsync(target, table, cancellationToken);
        var targetNames = targetColumns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return sourceColumns.Where(c => targetNames.Contains(c.Name)).ToList();
    }

    private static async Task<List<SqlColumn>> ListInsertableColumnsAsync(
        SqlConnection connection,
        SqlTable table,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.name, c.is_identity
            FROM sys.columns c
            INNER JOIN sys.tables t ON t.object_id = c.object_id
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE s.name = @schema
              AND t.name = @table
              AND c.is_computed = 0
              AND ty.name NOT IN (N'timestamp', N'rowversion')
            ORDER BY c.column_id
            """;

        var columns = new List<SqlColumn>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = table.Schema });
        command.Parameters.Add(new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = table.Name });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columns.Add(new SqlColumn(reader.GetString(0), reader.GetBoolean(1)));

        return columns;
    }

    private static async Task DeleteTargetRowsAsync(SqlConnection target, SqlTable table, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand($"DELETE FROM {table.QualifiedName}", target)
        {
            CommandTimeout = 0
        };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BulkCopyAsync(
        SqlConnection source,
        SqlConnection target,
        SqlTable table,
        IReadOnlyList<SqlColumn> columns,
        CancellationToken cancellationToken)
    {
        var selectList = string.Join(", ", columns.Select(c => Quote(c.Name)));
        await using var read = new SqlCommand($"SELECT {selectList} FROM {table.QualifiedName}", source)
        {
            CommandTimeout = 0
        };
        await using var reader = await read.ExecuteReaderAsync(cancellationToken);

        using var bulk = new SqlBulkCopy(target, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock, null)
        {
            DestinationTableName = table.QualifiedName,
            BulkCopyTimeout = 0,
            BatchSize = 2000
        };
        foreach (var column in columns)
            bulk.ColumnMappings.Add(column.Name, column.Name);

        await bulk.WriteToServerAsync(reader, cancellationToken);
    }

    private static async Task ReseedIdentityAsync(
        SqlConnection target,
        SqlTable table,
        IReadOnlyList<SqlColumn> columns,
        CancellationToken cancellationToken)
    {
        var identity = columns.FirstOrDefault(c => c.IsIdentity);
        if (identity == null)
            return;

        var sql = $"""
            DECLARE @max bigint = (SELECT ISNULL(MAX({Quote(identity.Name)}), 0) FROM {table.QualifiedName});
            IF @max > 0
                DBCC CHECKIDENT (N'{table.QualifiedName.Replace("'", "''", StringComparison.Ordinal)}', RESEED, @max);
            """;
        await using var command = new SqlCommand(sql, target) { CommandTimeout = 0 };
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<List<ForeignKeyDefinition>> ReadForeignKeysAsync(
        SqlConnection target,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                fk.name,
                SCHEMA_NAME(pt.schema_id),
                pt.name,
                SCHEMA_NAME(rt.schema_id),
                rt.name,
                fk.delete_referential_action_desc,
                fk.update_referential_action_desc,
                pc.name,
                rc.name,
                fkc.constraint_column_id
            FROM sys.foreign_keys fk
            INNER JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
            INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
            INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            INNER JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
            INNER JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
            ORDER BY fk.name, fkc.constraint_column_id
            """;

        var grouped = new Dictionary<string, ForeignKeyDefinition>(StringComparer.OrdinalIgnoreCase);
        await using var command = new SqlCommand(sql, target);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(0);
            if (!grouped.TryGetValue(name, out var fk))
            {
                fk = new ForeignKeyDefinition(
                    name,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    new List<(string Parent, string Referenced)>());
                grouped[name] = fk;
            }

            fk.Columns.Add((reader.GetString(7), reader.GetString(8)));
        }

        return grouped.Values.ToList();
    }

    private static async Task DropForeignKeysAsync(
        SqlConnection target,
        IReadOnlyList<ForeignKeyDefinition> foreignKeys,
        CancellationToken cancellationToken)
    {
        foreach (var fk in foreignKeys)
        {
            var sql = $"ALTER TABLE {Qualify(fk.ParentSchema, fk.ParentTable)} DROP CONSTRAINT {Quote(fk.Name)}";
            await using var command = new SqlCommand(sql, target) { CommandTimeout = 0 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task RecreateForeignKeysAsync(SqlConnection target, IReadOnlyList<ForeignKeyDefinition> foreignKeys)
    {
        foreach (var fk in foreignKeys)
        {
            var parentColumns = string.Join(", ", fk.Columns.Select(c => Quote(c.Parent)));
            var referencedColumns = string.Join(", ", fk.Columns.Select(c => Quote(c.Referenced)));
            var sql = $"""
                ALTER TABLE {Qualify(fk.ParentSchema, fk.ParentTable)} WITH NOCHECK
                ADD CONSTRAINT {Quote(fk.Name)}
                FOREIGN KEY ({parentColumns})
                REFERENCES {Qualify(fk.ReferencedSchema, fk.ReferencedTable)} ({referencedColumns})
                ON DELETE {SqlReferentialAction(fk.OnDelete)} ON UPDATE {SqlReferentialAction(fk.OnUpdate)}
                """;
            try
            {
                await using var command = new SqlCommand(sql, target) { CommandTimeout = 0 };
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  warning: could not restore foreign key {fk.Name}: {ex.Message}");
            }
        }
    }

    private static async Task DisableTriggersAsync(
        SqlConnection target,
        IReadOnlyList<SqlTable> tables,
        CancellationToken cancellationToken)
    {
        foreach (var table in tables)
            await ExecuteOptionalAsync(target, $"DISABLE TRIGGER ALL ON {table.QualifiedName}", cancellationToken);
    }

    private static async Task EnableTriggersAsync(SqlConnection target, IReadOnlyList<SqlTable> tables)
    {
        foreach (var table in tables)
            await ExecuteOptionalAsync(target, $"ENABLE TRIGGER ALL ON {table.QualifiedName}", CancellationToken.None);
    }

    private static async Task ExecuteOptionalAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException)
        {
            // Tables without triggers raise an error. Leave them as they are.
        }
    }

    private static string SqlReferentialAction(string action) =>
        action.Replace("_", " ", StringComparison.Ordinal);

    private static string Quote(string identifier) => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string Qualify(string schema, string table) => Quote(schema) + "." + Quote(table);

    private sealed record SqlTable(string Schema, string Name)
    {
        public string Key => Schema + "." + Name;
        public string QualifiedName => Qualify(Schema, Name);
    }

    private sealed record SqlColumn(string Name, bool IsIdentity);

    private sealed record ForeignKeyDefinition(
        string Name,
        string ParentSchema,
        string ParentTable,
        string ReferencedSchema,
        string ReferencedTable,
        string OnDelete,
        string OnUpdate,
        List<(string Parent, string Referenced)> Columns);
}
